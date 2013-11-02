using System;
using System.Globalization;
using System.IO;
using Brutal.Dev.StrongNameSigner.External;

namespace Brutal.Dev.StrongNameSigner
{
  /// <summary>
  /// Static helper class for easily getting assembly information and strong-name signing .NET assemblies.
  /// </summary>
  public static class SigningHelper
  {
    /// <summary>
    /// Signs the assembly at the specified path.
    /// </summary>
    /// <param name="assemblyPath">The path to the assembly you want to strong-name sign.</param>
    /// <returns>The assembly information of the new strong-name signed assembly.</returns>
    public static AssemblyInfo SignAssembly(string assemblyPath)
    {
      return SignAssembly(assemblyPath, string.Empty, string.Empty, null);
    }

    /// <summary>
    /// Signs the assembly at the specified path.
    /// </summary>
    /// <param name="assemblyPath">The path to the assembly you want to strong-name sign.</param>
    /// <param name="outputHandler">A method to handle external application output.</param>
    /// <returns>The assembly information of the new strong-name signed assembly.</returns>
    public static AssemblyInfo SignAssembly(string assemblyPath, Action<string> outputHandler)
    {
      return SignAssembly(assemblyPath, string.Empty, string.Empty, outputHandler);
    }

    /// <summary>
    /// Signs the assembly at the specified path with your own strong-name key file.
    /// </summary>
    /// <param name="assemblyPath">The path to the assembly you want to strong-name sign.</param>
    /// <param name="keyPath">The path to the strong-name key file you want to use (.snk or .pfx).</param>
    /// <returns>The assembly information of the new strong-name signed assembly.</returns>
    public static AssemblyInfo SignAssembly(string assemblyPath, string keyPath)
    {
      return SignAssembly(assemblyPath, keyPath, string.Empty, null);
    }

    /// <summary>
    /// Signs the assembly at the specified path with your own strong-name key file.
    /// </summary>
    /// <param name="assemblyPath">The path to the assembly you want to strong-name sign.</param>
    /// <param name="keyPath">The path to the strong-name key file you want to use (.snk or.pfx).</param>
    /// <param name="outputHandler">A method to handle external application output.</param>
    /// <returns>The assembly information of the new strong-name signed assembly.</returns>
    public static AssemblyInfo SignAssembly(string assemblyPath, string keyPath, Action<string> outputHandler)
    {
      return SignAssembly(assemblyPath, keyPath, string.Empty, outputHandler);
    }

    /// <summary>
    /// Signs the assembly at the specified path with your own strong-name key file.
    /// </summary>
    /// <param name="assemblyPath">The path to the assembly you want to strong-name sign.</param>
    /// <param name="keyPath">The path to the strong-name key file you want to use (.snk or .pfx).</param>
    /// <param name="outputPath">The directory path where the strong-name signed assembly will be copied to.</param>
    /// <returns>The assembly information of the new strong-name signed assembly.</returns>
    public static AssemblyInfo SignAssembly(string assemblyPath, string keyPath, string outputPath)
    {
      return SignAssembly(assemblyPath, keyPath, outputPath, null);
    }

    /// <summary>
    /// Signs the assembly at the specified path with your own strong-name key file.
    /// </summary>
    /// <param name="assemblyPath">The path to the assembly you want to strong-name sign.</param>
    /// <param name="keyPath">The path to the strong-name key file you want to use (.snk or .pfx).</param>
    /// <param name="outputPath">The directory path where the strong-name signed assembly will be copied to.</param>
    /// <param name="outputHandler">A method to handle external application output.</param>
    /// <returns>The assembly information of the new strong-name signed assembly.</returns>
    /// <exception cref="System.ArgumentNullException">
    /// assemblyPath parameter was not provided.
    /// </exception>
    /// <exception cref="System.IO.FileNotFoundException">
    /// Could not find provided assembly file.
    /// or
    /// Could not find provided strong-name key file file.
    /// </exception>
    /// <exception cref="System.InvalidOperationException">
    /// An error was detected when using an external tool, check the output log information for details on the error.
    /// </exception>
    /// <exception cref="Brutal.Dev.StrongNameSigner.AlreadySignedException">
    /// The assembly is already strong-name signed.
    /// </exception>
    public static AssemblyInfo SignAssembly(string assemblyPath, string keyPath, string outputPath, Action<string> outputHandler)
    {
      // Verify assembly path was passed in.
      if (string.IsNullOrWhiteSpace(assemblyPath))
      {
        throw new ArgumentNullException("assemblyPath");
      }

      // Make sure the file actually exists.
      if (!File.Exists(assemblyPath))
      {
        throw new FileNotFoundException("Could not find provided assembly file.", assemblyPath);
      }

      if (string.IsNullOrWhiteSpace(outputPath))
      {
        // Overwrite the file if no output path is provided.
        outputPath = Path.GetDirectoryName(assemblyPath);
      }

      // Get the assembly info and go from there.
      AssemblyInfo info = GetAssemblyInfo(assemblyPath);

      // Don't sign assemblies with a strong-name signature.
      if (info.IsSigned)
      {
        throw new AlreadySignedException(string.Format(CultureInfo.CurrentCulture, "The assembly '{0}' is already strong-name signed.", assemblyPath));
      }

      // Disassemble
      using (var ildasm = new ILDasm(info))
      {
        if (!ildasm.Run(outputHandler))
        {
          throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, "ILDASM failed to execute for assembly '{0}'.", assemblyPath));
        }

        // Check if we have a key
        using (var signtool = new SignTool())
        {
          if (string.IsNullOrEmpty(keyPath))
          {
            if (!signtool.Run(outputHandler))
            {
              throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, "SIGNTOOL failed to create a strong-name key file for '{0}'.", assemblyPath));
            }

            keyPath = signtool.KeyFilePath;
          }
          else if (!File.Exists(keyPath))
          {
            throw new FileNotFoundException("Could not find provided strong-name key file file.", keyPath);
          }

          using (var ilasm = new ILAsm(info, ildasm.BinaryILFilePath, keyPath, outputPath))
          {
            if (!ilasm.Run(outputHandler))
            {
              throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, "ILASM failed to execute for assembly '{0}'.", assemblyPath));
            }

            return GetAssemblyInfo(ilasm.SignedAssemblyPath);
          }
        }
      }
    }

    /// <summary>
    /// Gets .NET assembly information.
    /// </summary>
    /// <param name="assemblyPath">The path to an assembly you want to get information from.</param>
    /// <returns>The assembly information.</returns>
    public static AssemblyInfo GetAssemblyInfo(string assemblyPath)
    {
      return GetAssemblyInfo(assemblyPath, null);
    }

    /// <summary>
    /// Gets .NET assembly information.
    /// </summary>
    /// <param name="assemblyPath">The path to an assembly you want to get information from.</param>
    /// <param name="outputHandler">A method to handle external application output.</param>
    /// <returns>The assembly information.</returns>
    /// <exception cref="System.ArgumentNullException">
    /// assemblyPath parameter was not provided.
    /// </exception>
    /// <exception cref="System.IO.FileNotFoundException">
    /// Could not find provided assembly file.
    /// </exception>
    /// <exception cref="System.InvalidOperationException">
    /// An error was detected when using an external tool, check the output log information for details on the error.
    /// </exception>
    public static AssemblyInfo GetAssemblyInfo(string assemblyPath, Action<string> outputHandler)
    {
      // Verify assembly path was passed in.
      if (string.IsNullOrWhiteSpace(assemblyPath))
      {
        throw new ArgumentNullException("assemblyPath");
      }

      // Make sure the file actually exists.
      if (!File.Exists(assemblyPath))
      {
        throw new FileNotFoundException("Could not find provided assembly file.", assemblyPath);
      }

      using (var corflags = new CorFlags(assemblyPath))
      {
        if (!corflags.Run(outputHandler))
        {
          throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, "CORFLAGS failed to execute for assembly '{0}'.", assemblyPath));
        }

        return corflags.AssemblyInfo;
      }
    }

    /// <summary>
    /// Checks that all the required software is available on the client machine.
    /// </summary>
    /// <exception cref="System.IO.FileNotFoundException">
    /// Could not find a required external application file.
    /// </exception>
    public static void CheckForRequiredSoftware()
    {
      using (var ilasm = new ILAsm())
      {
        if (!File.Exists(ilasm.Executable))
        {
          throw new FileNotFoundException("Could not find required executable 'ILASM.exe'.", ilasm.Executable);
        }
      }

      using (var ildasm = new ILDasm())
      {
        if (!File.Exists(ildasm.Executable))
        {
          throw new FileNotFoundException("Could not find required executable 'ILDASM.exe'.", ildasm.Executable);
        }
      }

      using (var corflags = new CorFlags())
      {
        if (!File.Exists(corflags.Executable))
        {
          throw new FileNotFoundException("Could not find required executable 'CORFLAGS.exe'.", corflags.Executable);
        }
      }

      using (var sn = new SignTool())
      {
        if (!File.Exists(sn.Executable))
        {
          throw new FileNotFoundException("Could not find required executable 'SN.exe'.", sn.Executable);
        }
      }
    }
  }
}
