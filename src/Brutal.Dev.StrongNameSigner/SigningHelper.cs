using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
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
    private static byte[] keyPairCache = null;

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
      return SignAssembly(assemblyPath, String.Empty, String.Empty, String.Empty);
    }

    /// <summary>
    /// Signs the assembly at the specified path with your own strong-name key file.
    /// </summary>
    /// <param name="assemblyPath">The path to the assembly you want to strong-name sign.</param>
    /// <param name="keyPath">The path to the strong-name key file you want to use (.snk or.pfx).</param>
    /// <returns>The assembly information of the new strong-name signed assembly.</returns>
    public static AssemblyInfo SignAssembly(string assemblyPath, string keyPath)
    {
      return SignAssembly(assemblyPath, keyPath, String.Empty, String.Empty);
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
    /// Could not find provided strong-name key file.
    /// </exception>
    /// <exception cref="System.BadImageFormatException">
    /// The file is not a .NET managed assembly.
    /// </exception>
    public static AssemblyInfo SignAssembly(string assemblyPath, string keyPath, string outputPath)
    {
      return SignAssembly(assemblyPath, keyPath, outputPath, String.Empty);
    }

    /// <summary>
    /// Signs the assembly at the specified path with your own strong-name key file.
    /// </summary>
    /// <param name="assemblyPath">The path to the assembly you want to strong-name sign.</param>
    /// <param name="keyPath">The path to the strong-name key file you want to use (.snk or .pfx).</param>
    /// <param name="outputPath">The directory path where the strong-name signed assembly will be copied to.</param>
    /// <param name="keyFilePassword">The password for the provided strong-name key file.</param>
    /// <param name="probingPaths">Additional paths to probe for references.</param>
    /// <returns>
    /// The assembly information of the new strong-name signed assembly.
    /// </returns>
    /// <exception cref="System.ArgumentNullException">assemblyPath parameter was not provided.</exception>
    /// <exception cref="System.IO.FileNotFoundException">Could not find provided assembly file.
    /// or
    /// Could not find provided strong-name key file.</exception>
    /// <exception cref="System.BadImageFormatException">The file is not a .NET managed assembly.</exception>
    public static AssemblyInfo SignAssembly(string assemblyPath, string keyPath, string outputPath, string keyFilePassword, params string[] probingPaths)
    {
      var db = new AssemblyCollection(PrintMessageColor);
      db.AddFromFile(assemblyPath, outputPath);
      db.Sign(keyPath, keyFilePassword, PrintMessageColor);
      return db.Assemblies.First().Info;
    }

    public static AssemblyInfo GetAssemblyInfo(string assemblyPath, AssemblyDefinition a)
    {
      return new AssemblyInfo()
      {
        FilePath = Path.GetFullPath(assemblyPath),
        DotNetVersion = GetDotNetVersion(a.MainModule.Runtime),
        IsSigned = a.MainModule.Attributes.HasFlag(ModuleAttributes.StrongNameSigned),
        IsManagedAssembly = a.MainModule.Attributes.HasFlag(ModuleAttributes.ILOnly),
        Is64BitOnly =
          a.MainModule.Architecture == TargetArchitecture.AMD64 || a.MainModule.Architecture == TargetArchitecture.IA64,
        Is32BitOnly =
          a.MainModule.Attributes.HasFlag(ModuleAttributes.Required32Bit) &&
          !a.MainModule.Attributes.HasFlag(ModuleAttributes.Preferred32Bit),
        Is32BitPreferred = a.MainModule.Attributes.HasFlag(ModuleAttributes.Preferred32Bit)
      };
    }

    /// <summary>
    /// Fixes an assembly reference.
    /// </summary>
    /// <param name="downstreamAssembly"></param>
    /// <param name="upstreamAssembly"></param>
    /// <returns>
    ///   <c>true</c> if an assembly reference was found and fixed, <c>false</c> if no reference was found.
    /// </returns>
    /// <exception cref="System.ArgumentNullException">assemblyPath was not provided.
    /// or
    /// upstreamAssemblyPath was not provided.</exception>
    /// <exception cref="System.IO.FileNotFoundException">Could not find provided assembly file.
    /// or
    /// Could not find provided reference assembly file.</exception>
    public static bool FixAssemblyReference(AssemblyDefinition downstreamAssembly, AssemblyDefinition upstreamAssembly)
    {

      bool fixApplied = false;

      var a2bReference = downstreamAssembly.MainModule.AssemblyReferences.FirstOrDefault(r => r.Name == upstreamAssembly.Name.Name);

      //var strongNameKeyPair = GetStrongNameKeyPair(keyPath, keyFilePassword);

      if (a2bReference != null)
      {
        // Found a matching reference, let's set the public key token.
        if (BitConverter.ToString(a2bReference.PublicKeyToken) != BitConverter.ToString(upstreamAssembly.Name.PublicKeyToken))
        {
          a2bReference.PublicKeyToken = upstreamAssembly.Name.PublicKeyToken ?? new byte[0];
          a2bReference.Version = upstreamAssembly.Name.Version;

          // Save and resign.
          //downstreamAssembly.Write(downstreamAssemblyPath, new WriterParameters { StrongNameKeyPair = strongNameKeyPair });

          fixApplied = true;
        }
      }

      var b2aReference = upstreamAssembly.CustomAttributes.SingleOrDefault(attr => attr.AttributeType.FullName == typeof(InternalsVisibleToAttribute).FullName &&
        attr.ConstructorArguments[0].Value.ToString() == downstreamAssembly.Name.Name);
      
      if (b2aReference != null && downstreamAssembly.Name.HasPublicKey)
      {
        // Add the public key to the attribute.
        var typeRef = b2aReference.ConstructorArguments[0].Type;
        b2aReference.ConstructorArguments.Clear();
        b2aReference.ConstructorArguments.Add(new CustomAttributeArgument(typeRef, downstreamAssembly.Name.Name + ", PublicKey=" + BitConverter.ToString(downstreamAssembly.Name.PublicKey).Replace("-", String.Empty)));

        // Save and resign.
        // upstreamAssembly.Write(upstreamAssemblyPath, new WriterParameters { StrongNameKeyPair = strongNameKeyPair });

        fixApplied = true;
      }

      return fixApplied;
    }

    public static ReaderParameters GetReadParameters(string assemblyPath, params string[] probingPaths)
    {
      var resolver = new DefaultAssemblyResolver();


      resolver.AddSearchDirectory(Path.GetDirectoryName(assemblyPath));

      if (probingPaths != null)
      {
        foreach (var searchDir in probingPaths)
        {
          resolver.AddSearchDirectory(searchDir);
        }
      }

      return new ReaderParameters() { AssemblyResolver = resolver };
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

    public static StrongNameKeyPair GetStrongNameKeyPair(string keyPath, string keyFilePassword)
    {
      if (!String.IsNullOrEmpty(keyPath))
      {
        if (!String.IsNullOrEmpty(keyFilePassword))
        {
          var cert = new X509Certificate2(keyPath, keyFilePassword, X509KeyStorageFlags.Exportable);

          var provider = cert.PrivateKey as RSACryptoServiceProvider;
          if (provider == null)
          {
            throw new InvalidOperationException("The key file is not password protected or the incorrect password was provided.");
          }

          return new StrongNameKeyPair(provider.ExportCspBlob(true));
        }
        else
        {
          return new StrongNameKeyPair(File.ReadAllBytes(keyPath));
        }
      }
      else
      {
        // Only cache generated keys so all signed assemblies use the same public key.
        if (keyPairCache != null)
        {
          return new StrongNameKeyPair(keyPairCache);
        }

        keyPairCache = GenerateStrongNameKeyPair();

        return new StrongNameKeyPair(keyPairCache);
      }
    }

    public static void FileWriteTransaction(string path, Action action )
    {
      var backup = path + ".backup";
      if (File.Exists(path))
      {
        // Make a backup before overwriting.
        File.Copy(path, backup, true);
      }

      try
      {
        action();
      }
      catch (Exception)
      {
        if (File.Exists(backup))
          File.Copy(backup, path, true);
        throw;
      }
      finally
      {
        if (File.Exists(backup))
          File.Delete(backup);
      }
      
    }

    public static void PrintMessageColor(string message, LogLevel minLogLevel, ConsoleColor? color)
    {
      if(color==null)
        Console.ResetColor();
      else
        Console.ForegroundColor = color.Value;
      PrintMessage(message, minLogLevel);
      Console.ResetColor();      
    }

    public static void PrintMessage(string message, LogLevel minLogLevel)
    {
      if (currentLogLevel <= minLogLevel)
      {
        if (String.IsNullOrEmpty(message))
        {
          Console.WriteLine();
        }
        else
        {
          Console.WriteLine(message);
        }
      }
    }

    public static LogLevel currentLogLevel = LogLevel.Default;

    /// <summary>
    /// Gets .NET assembly information.
    /// </summary>
    /// <param name="assemblyPath">The path to an assembly you want to get information from.</param>
    /// <param name="probingPaths">Additional paths to probe for references.</param>
    /// <returns>
    /// The assembly information.
    /// </returns>
    /// <exception cref="System.ArgumentNullException">assemblyPath parameter was not provided.</exception>
    /// <exception cref="System.IO.FileNotFoundException">Could not find provided assembly file.</exception>
    public static AssemblyInfo GetAssemblyInfo(string assemblyPath, params string[] probingPaths)
    {
      // Verify assembly path was passed in.
      if (String.IsNullOrWhiteSpace(assemblyPath))
      {
        throw new ArgumentNullException("assemblyPath");
      }

      // Make sure the file actually exists.
      if (!File.Exists(assemblyPath))
      {
        throw new FileNotFoundException("Could not find provided assembly file.", assemblyPath);
      }

      var a = AssemblyDefinition.ReadAssembly(assemblyPath, SigningHelper.GetReadParameters(assemblyPath, probingPaths));

      return SigningHelper.GetAssemblyInfo(assemblyPath, a);
    }
  }
}
