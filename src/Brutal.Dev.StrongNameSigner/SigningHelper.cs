using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Mono.Cecil;

namespace Brutal.Dev.StrongNameSigner
{
  /// <summary>
  /// Static helper class for easily getting assembly information and strong-name signing .NET assemblies.
  /// </summary>
  public static class SigningHelper
  {
    /// <summary>
    /// Generates a 1024 bit the strong-name key pair that can be written to an SNK file.
    /// </summary>
    /// <returns>A strong-name key pair array.</returns>
    public static byte[] GenerateStrongNameKeyPair()
    { 
      using (var provider = new RSACryptoServiceProvider(1024, new CspParameters() { KeyNumber = 2 }))
      {
        return provider.ExportCspBlob(!provider.PublicOnly);
      }
    }

    /// <summary>
    /// Signs the assembly at the specified path.
    /// </summary>
    /// <param name="assemblyPath">The path to the assembly you want to strong-name sign.</param>
    /// <returns>The assembly information of the new strong-name signed assembly.</returns>
    public static AssemblyInfo SignAssembly(string assemblyPath)
    {
      return SignAssembly(assemblyPath, string.Empty, string.Empty, string.Empty);
    }

    /// <summary>
    /// Signs the assembly at the specified path with your own strong-name key file.
    /// </summary>
    /// <param name="assemblyPath">The path to the assembly you want to strong-name sign.</param>
    /// <param name="keyPath">The path to the strong-name key file you want to use (.snk or.pfx).</param>
    /// <returns>The assembly information of the new strong-name signed assembly.</returns>
    public static AssemblyInfo SignAssembly(string assemblyPath, string keyPath)
    {
      return SignAssembly(assemblyPath, keyPath, string.Empty, string.Empty);
    }

    /// <summary>
    /// Signs the assembly at the specified path with your own strong-name key file.
    /// </summary>
    /// <param name="assemblyPath">The path to the assembly you want to strong-name sign.</param>
    /// <param name="keyPath">The path to the strong-name key file you want to use (.snk or .pfx).</param>
    /// <param name="outputPath">The directory path where the strong-name signed assembly will be copied to.</param>
    /// <returns>The assembly information of the new strong-name signed assembly.</returns>
    /// <exception cref="System.ArgumentNullException">
    /// assemblyPath parameter was not provided.
    /// </exception>
    /// <exception cref="System.IO.FileNotFoundException">
    /// Could not find provided assembly file.
    /// or
    /// Could not find provided strong-name key file file.
    /// </exception>
    /// <exception cref="System.BadImageFormatException">
    /// The file is not a .NET managed assembly.
    /// </exception>
    public static AssemblyInfo SignAssembly(string assemblyPath, string keyPath, string outputPath)
    {
      return SignAssembly(assemblyPath, keyPath, outputPath, string.Empty);
    }

    /// <summary>
    /// Signs the assembly at the specified path with your own strong-name key file.
    /// </summary>
    /// <param name="assemblyPath">The path to the assembly you want to strong-name sign.</param>
    /// <param name="keyPath">The path to the strong-name key file you want to use (.snk or .pfx).</param>
    /// <param name="outputPath">The directory path where the strong-name signed assembly will be copied to.</param>
    /// <param name="keyFilePassword">The password for the provided strong-name key file.</param>
    /// <returns>The assembly information of the new strong-name signed assembly.</returns>
    /// <exception cref="System.ArgumentNullException">
    /// assemblyPath parameter was not provided.
    /// </exception>
    /// <exception cref="System.IO.FileNotFoundException">
    /// Could not find provided assembly file.
    /// or
    /// Could not find provided strong-name key file file.
    /// </exception>
    /// <exception cref="System.BadImageFormatException">
    /// The file is not a .NET managed assembly.
    /// </exception>
    public static AssemblyInfo SignAssembly(string assemblyPath, string keyPath, string outputPath, string keyFilePassword)
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

      string outputFile = Path.Combine(Path.GetFullPath(outputPath), Path.GetFileName(assemblyPath));

      // Get the assembly info and go from there.
      AssemblyInfo info = GetAssemblyInfo(assemblyPath);

      // Don't sign assemblies with a strong-name signature.
      if (info.IsSigned)
      {
        if (!outputFile.Equals(Path.GetFullPath(assemblyPath), StringComparison.OrdinalIgnoreCase))
        {
          File.Copy(assemblyPath, outputFile, true);
        }

        return info;
      }

      byte[] keyPairArray = null;

      if (!string.IsNullOrEmpty(keyPath))
      {
        if (!string.IsNullOrEmpty(keyFilePassword))
        {
          var cert = new X509Certificate2(keyPath, keyFilePassword, X509KeyStorageFlags.Exportable);

          var provider = cert.PrivateKey as RSACryptoServiceProvider;
          if (provider == null)
          {
            throw new InvalidOperationException("The key file is not password protected or the incorrect password was provided.");
          }
          
          keyPairArray = provider.ExportCspBlob(true);
        }
        else
        {
          keyPairArray = File.ReadAllBytes(keyPath); 
        }
      }
      else
      {
        keyPairArray = GenerateStrongNameKeyPair();
      }

      if (outputFile.Equals(Path.GetFullPath(assemblyPath), StringComparison.OrdinalIgnoreCase))
      {
        // Make a backup before overwriting.
        File.Copy(outputFile, outputFile + ".unsigned", true);
      }

      try
      {
        AssemblyDefinition.ReadAssembly(assemblyPath).Write(outputFile, new WriterParameters() { StrongNameKeyPair = new StrongNameKeyPair(keyPairArray) });
      }
      catch (Exception)
      {
        // Restore the backup if something goes wrong.
        if (outputFile.Equals(Path.GetFullPath(assemblyPath), StringComparison.OrdinalIgnoreCase))
        {
          File.Copy(outputFile + ".unsigned", outputFile, true);
        }

        throw;
      }

      return GetAssemblyInfo(outputFile);
    }

    /// <summary>
    /// Gets .NET assembly information.
    /// </summary>
    /// <param name="assemblyPath">The path to an assembly you want to get information from.</param>
    /// <returns>The assembly information.</returns>
    /// <exception cref="System.ArgumentNullException">
    /// assemblyPath parameter was not provided.
    /// </exception>
    /// <exception cref="System.IO.FileNotFoundException">
    /// Could not find provided assembly file.
    /// </exception>
    public static AssemblyInfo GetAssemblyInfo(string assemblyPath)
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

      var a = AssemblyDefinition.ReadAssembly(assemblyPath);

      return new AssemblyInfo()
      {
        FilePath = Path.GetFullPath(assemblyPath),
        DotNetVersion = GetDotNetVersion(a.MainModule.Runtime),
        IsSigned = a.MainModule.Attributes.HasFlag(ModuleAttributes.StrongNameSigned),
        IsManagedAssembly = a.MainModule.Attributes.HasFlag(ModuleAttributes.ILOnly),
        Is64BitOnly = a.MainModule.Architecture == TargetArchitecture.AMD64 || a.MainModule.Architecture == TargetArchitecture.IA64,
        Is32BitOnly = a.MainModule.Attributes.HasFlag(ModuleAttributes.Required32Bit) && !a.MainModule.Attributes.HasFlag(ModuleAttributes.Preferred32Bit),
        Is32BitPreferred = a.MainModule.Attributes.HasFlag(ModuleAttributes.Preferred32Bit)
      };
    }

    /// <summary>
    /// Fixes an assembly reference.
    /// </summary>
    /// <param name="assemblyPath">The path to the assembly you want to fix a reference for.</param>    
    /// <param name="referenceAssemblyPath">The path to the reference assembly path you want to fix in the first assembly.</param>
    /// <exception cref="System.ArgumentNullException">
    /// assemblyPath was not provided.
    /// or
    /// referenceAssemblyPath was not provided.
    /// </exception>
    /// <exception cref="System.IO.FileNotFoundException">
    /// Could not find provided assembly file.
    /// or
    /// Could not find provided reference assembly file.
    /// </exception>
    public static bool FixAssemblyReference(string assemblyPath, string referenceAssemblyPath)
    {
      // Verify assembly path was passed in.
      if (string.IsNullOrWhiteSpace(assemblyPath))
      {
        throw new ArgumentNullException("assemblyPath");
      }

      if (string.IsNullOrWhiteSpace(referenceAssemblyPath))
      {
        throw new ArgumentNullException("referenceAssemblyPath");
      }

      // Make sure the file actually exists.
      if (!File.Exists(assemblyPath))
      {
        throw new FileNotFoundException("Could not find provided assembly file.", assemblyPath);
      }

      if (!File.Exists(referenceAssemblyPath))
      {
        throw new FileNotFoundException("Could not find provided reference assembly file.", referenceAssemblyPath);
      }

      var a = AssemblyDefinition.ReadAssembly(assemblyPath);
      var b = AssemblyDefinition.ReadAssembly(referenceAssemblyPath);

      var reference = a.MainModule.AssemblyReferences.FirstOrDefault(r => r.Name == b.Name.Name && r.Version == b.Name.Version);

      if (reference != null)
      {
        // Found a matching reference, let's set the public key token.
        if (BitConverter.ToString(reference.PublicKeyToken) != BitConverter.ToString(b.Name.PublicKeyToken))
        {
          reference.PublicKeyToken = b.Name.PublicKeyToken ?? new byte[0];

          a.Write(assemblyPath);

          return true;
        }
      }

      return false;
    }

    private static string GetDotNetVersion(TargetRuntime runtime)
    {
      switch (runtime)
      {
        case TargetRuntime.Net_1_0:
          return "1.0.3705";
        case TargetRuntime.Net_1_1:
          return "1.1.4322";
        case TargetRuntime.Net_2_0:
          return "2.0.50727";
        case TargetRuntime.Net_4_0:
          return "4.0.30319";
      }

      return "UNKNOWN";
    }
  }
}
