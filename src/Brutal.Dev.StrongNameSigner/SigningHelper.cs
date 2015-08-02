using System;
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
    /// Could not find provided strong-name key file.
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
      else
      {
        // Create the directory structure.
        Directory.CreateDirectory(outputPath);
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

      if (outputFile.Equals(Path.GetFullPath(assemblyPath), StringComparison.OrdinalIgnoreCase))
      {
        // Make a backup before overwriting.
        File.Copy(outputFile, outputFile + ".unsigned", true);
      }

      try
      {
        AssemblyDefinition.ReadAssembly(assemblyPath, GetReadParameters(assemblyPath, probingPaths))
          .Write(outputFile, new WriterParameters() { StrongNameKeyPair = GetStrongNameKeyPair(keyPath, keyFilePassword) });
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
    /// <param name="probingPaths">Additional paths to probe for references.</param>
    /// <returns>
    /// The assembly information.
    /// </returns>
    /// <exception cref="System.ArgumentNullException">assemblyPath parameter was not provided.</exception>
    /// <exception cref="System.IO.FileNotFoundException">Could not find provided assembly file.</exception>
    public static AssemblyInfo GetAssemblyInfo(string assemblyPath, params string[] probingPaths)
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

      var a = AssemblyDefinition.ReadAssembly(assemblyPath, GetReadParameters(assemblyPath, probingPaths));

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
    /// <returns><c>true</c> if an assembly reference was found and fixed, <c>false</c> if no reference was found.</returns>
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
      return FixAssemblyReference(assemblyPath, referenceAssemblyPath, string.Empty, string.Empty);
    }

    /// <summary>
    /// Fixes an assembly reference.
    /// </summary>
    /// <param name="assemblyPath">The path to the assembly you want to fix a reference for.</param>
    /// <param name="referenceAssemblyPath">The path to the reference assembly path you want to fix in the first assembly.</param>
    /// <param name="keyPath">The path to the strong-name key file you want to use (.snk or .pfx).</param>
    /// <param name="keyFilePassword">The password for the provided strong-name key file.</param>
    /// <param name="probingPaths">Additional paths to probe for references.</param>
    /// <returns>
    ///   <c>true</c> if an assembly reference was found and fixed, <c>false</c> if no reference was found.
    /// </returns>
    /// <exception cref="System.ArgumentNullException">assemblyPath was not provided.
    /// or
    /// referenceAssemblyPath was not provided.</exception>
    /// <exception cref="System.IO.FileNotFoundException">Could not find provided assembly file.
    /// or
    /// Could not find provided reference assembly file.</exception>
    public static bool FixAssemblyReference(string assemblyPath, string referenceAssemblyPath, string keyPath, string keyFilePassword, params string[] probingPaths)
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

      bool fixApplied = false;
      var a = AssemblyDefinition.ReadAssembly(assemblyPath, GetReadParameters(assemblyPath, probingPaths));
      var b = AssemblyDefinition.ReadAssembly(referenceAssemblyPath, GetReadParameters(referenceAssemblyPath, probingPaths));
      
      var assemblyReference = a.MainModule.AssemblyReferences.FirstOrDefault(r => r.Name == b.Name.Name);

      if (assemblyReference != null)
      {
        // Found a matching reference, let's set the public key token.
        if (BitConverter.ToString(assemblyReference.PublicKeyToken) != BitConverter.ToString(b.Name.PublicKeyToken))
        {
          assemblyReference.PublicKeyToken = b.Name.PublicKeyToken ?? new byte[0];
          assemblyReference.Version = b.Name.Version;

          // Save and resign.
          a.Write(assemblyPath, new WriterParameters { StrongNameKeyPair = GetStrongNameKeyPair(keyPath, keyFilePassword) });

          fixApplied = true;
        }
      }

      var friendReference = b.CustomAttributes.SingleOrDefault(attr => attr.AttributeType.FullName == typeof(InternalsVisibleToAttribute).FullName &&
        attr.ConstructorArguments[0].Value.ToString() == a.Name.Name);
      
      if (friendReference != null && a.Name.HasPublicKey)
      {
        // Add the public key to the attribute.
        var typeRef = friendReference.ConstructorArguments[0].Type;
        friendReference.ConstructorArguments.Clear();
        friendReference.ConstructorArguments.Add(new CustomAttributeArgument(typeRef, a.Name.Name + ", PublicKey=" + BitConverter.ToString(a.Name.PublicKey).Replace("-", string.Empty)));

        // Save and resign.
        b.Write(referenceAssemblyPath, new WriterParameters { StrongNameKeyPair = GetStrongNameKeyPair(keyPath, keyFilePassword) });

        fixApplied = true;
      }

      return fixApplied;
    }

    /// <summary>
    /// Removes any friend assembly references (InternalsVisibleTo attributes) that do not have public keys.
    /// </summary>
    /// <param name="assemblyPath">The path to the assembly you want to remove friend references from.</param>
    /// <returns><c>true</c> if any invalid friend references were found and fixed, <c>false</c> if no invalid friend references was found.</returns>
    /// <exception cref="System.ArgumentNullException">
    /// assemblyPath was not provided.
    /// </exception>
    /// <exception cref="System.IO.FileNotFoundException">
    /// Could not find provided assembly file.
    /// </exception>
    public static bool RemoveInvalidFriendAssemblies(string assemblyPath)
    {
      return RemoveInvalidFriendAssemblies(assemblyPath, string.Empty, string.Empty);
    }

    /// <summary>
    /// Removes any friend assembly references (InternalsVisibleTo attributes) that do not have public keys.
    /// </summary>
    /// <param name="assemblyPath">The path to the assembly you want to remove friend references from.</param>
    /// <param name="keyPath">The path to the strong-name key file you want to use (.snk or .pfx).</param>
    /// <param name="keyFilePassword">The password for the provided strong-name key file.</param>
    /// <param name="probingPaths">Additional paths to probe for references.</param>
    /// <returns>
    ///   <c>true</c> if any invalid friend references were found and fixed, <c>false</c> if no invalid friend references was found.
    /// </returns>
    /// <exception cref="System.ArgumentNullException">assemblyPath was not provided.</exception>
    /// <exception cref="System.IO.FileNotFoundException">Could not find provided assembly file.</exception>
    public static bool RemoveInvalidFriendAssemblies(string assemblyPath, string keyPath, string keyFilePassword, params string[] probingPaths)
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

      bool fixApplied = false;
      var a = AssemblyDefinition.ReadAssembly(assemblyPath, GetReadParameters(assemblyPath, probingPaths));

      var ivtAttributes = a.CustomAttributes.Where(attr => attr.AttributeType.FullName == typeof(InternalsVisibleToAttribute).FullName).ToList();

      foreach (var friendReference in ivtAttributes)
      {
        // Find any without a public key defined.
        if (friendReference.HasConstructorArguments && friendReference.ConstructorArguments.Any(ca => ca.Value != null && ca.Value.ToString().IndexOf("PublicKey=", StringComparison.Ordinal) == -1))
        {
          a.CustomAttributes.Remove(friendReference);
          fixApplied = true;
        }
      }

      if (fixApplied)
      {
        // Save and resign.
        a.Write(assemblyPath, new WriterParameters { StrongNameKeyPair = GetStrongNameKeyPair(keyPath, keyFilePassword) });
      }

      return fixApplied;
    }

    private static ReaderParameters GetReadParameters(string assemblyPath, string[] probingPaths)
    {
      var resolver = new DefaultAssemblyResolver();

      if (!string.IsNullOrEmpty(assemblyPath) && File.Exists(assemblyPath))
      {
        resolver.AddSearchDirectory(Path.GetDirectoryName(assemblyPath));
      }

      if (probingPaths != null)
      {
        foreach (var searchDir in probingPaths)
        {
          if (Directory.Exists(searchDir))
          {
            resolver.AddSearchDirectory(searchDir);
          }
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

    private static StrongNameKeyPair GetStrongNameKeyPair(string keyPath, string keyFilePassword)
    {
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
  }
}
