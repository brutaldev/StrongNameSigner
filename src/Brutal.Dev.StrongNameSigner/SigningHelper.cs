using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Mono.Cecil;
using Mono.Security.Cryptography;

namespace Brutal.Dev.StrongNameSigner
{
  /// <summary>
  /// Static helper class for easily getting assembly information and strong-name signing .NET assemblies.
  /// </summary>
  public static class SigningHelper
  {
    private static readonly ConcurrentDictionary<string, KeyValuePair<string, AssemblyInfo>> AssemblyInfoCache = new ConcurrentDictionary<string, KeyValuePair<string, AssemblyInfo>>(StringComparer.OrdinalIgnoreCase);

    private static byte[] keyPairCache;

    /// <summary>
    /// Generates a 4096 bit the strong-name key pair that can be written to an SNK file.
    /// </summary>
    /// <returns>A strong-name key pair array.</returns>
    public static byte[] GenerateStrongNameKeyPair()
    {
      using (var provider = new RSACryptoServiceProvider(4096))
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
        throw new ArgumentNullException(nameof(assemblyPath));
      }

      // Make sure the file actually exists.
      if (!File.Exists(assemblyPath))
      {
        throw new FileNotFoundException($"Could not find provided assembly file '{assemblyPath}'.", assemblyPath);
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

      var keyPair = GetStrongNameKeyPair(keyPath, keyFilePassword);

      string outputFile = Path.Combine(Path.GetFullPath(outputPath), Path.GetFileName(assemblyPath));
      using (var outputFileMgr = new OutputFileManager(assemblyPath, outputFile))
      {
        // Get the assembly info and go from there.
        var info = GetAssemblyInfo(assemblyPath);

        // Don't sign assemblies with a strong-name signature.
        if (info.IsSigned)
        {
          // If the target directory is different from the input...
          if (!outputFileMgr.IsInPlaceReplace)
          {
            // ...just copy the source file to the destination.
            outputFileMgr.CopySourceToFinalOutput();
          }

          return GetAssemblyInfo(outputFile);
        }

        if (outputFileMgr.IsInPlaceReplace)
        {
          outputFileMgr.CreateBackup();
        }

        using (var ad = AssemblyDefinition.ReadAssembly(assemblyPath, GetReadParameters(assemblyPath, probingPaths)))
        {
          ad.Name.HashAlgorithm = AssemblyHashAlgorithm.SHA1;
          ad.Name.PublicKey = GetPublicKey(keyPair);
          ad.Name.HasPublicKey = true;
          ad.Name.Attributes &= AssemblyAttributes.PublicKey;

          ad.Write(outputFileMgr.IntermediateAssemblyPath, new WriterParameters() { StrongNameKeyBlob = keyPair, WriteSymbols = outputFileMgr.HasSymbols });
        }

        AssemblyInfoCache.TryRemove(assemblyPath, out var _);

        outputFileMgr.Commit();

        return GetAssemblyInfo(outputFile);
      }
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
        throw new ArgumentNullException(nameof(assemblyPath));
      }

      // Make sure the file actually exists.
      if (!File.Exists(assemblyPath))
      {
        throw new FileNotFoundException($"Could not find provided assembly file '{assemblyPath}'.", assemblyPath);
      }

      var a = new KeyValuePair<string, AssemblyInfo>(null, null);
      if (AssemblyInfoCache.ContainsKey(assemblyPath) && AssemblyInfoCache.TryGetValue(assemblyPath, out a) &&
          !GetFileMD5Hash(assemblyPath).Equals(a.Key, StringComparison.OrdinalIgnoreCase))  // Check if the file contents have changed.
      {
        AssemblyInfoCache.TryRemove(assemblyPath, out var _);

        // Overwrite with a blank version.
        a = new KeyValuePair<string, AssemblyInfo>(null, null);
      }

      if (a.Value == null)
      {
        using (var definition = AssemblyDefinition.ReadAssembly(assemblyPath, GetReadParameters(assemblyPath, probingPaths)))
        {
          IClrStrongName clrStrongName = null;

          try
          {
            var runtimeInterface = RuntimeEnvironment.GetRuntimeInterfaceAsObject(new Guid("B79B0ACD-F5CD-409b-B5A5-A16244610B92"), new Guid("9FD93CCF-3280-4391-B3A9-96E1CDE77C8D"));

            if (runtimeInterface != null)
            {
              clrStrongName = runtimeInterface as IClrStrongName;
            }
          }
          catch (InvalidCastException)
          {
            // Nothing to do here, cannot create the runtime interface so will skip verification.
          }
          catch (PlatformNotSupportedException)
          {
            // Nothing to do here, this only works in Windows.
          }

          var strongNameVerified = false;
          var strongNameVerificationResult = clrStrongName?.StrongNameSignatureVerificationEx(assemblyPath, true, out var verificationForced);
          strongNameVerified = !strongNameVerificationResult.HasValue || strongNameVerificationResult == 0;

          var info = new AssemblyInfo()
          {
            FilePath = Path.GetFullPath(assemblyPath),
            DotNetVersion = GetDotNetVersion(definition.MainModule.Runtime),
            SigningType = !definition.MainModule.Attributes.HasFlag(ModuleAttributes.StrongNameSigned) ? StrongNameType.NotSigned : strongNameVerified ? StrongNameType.Signed : StrongNameType.DelaySigned,
            IsManagedAssembly = definition.MainModule.Attributes.HasFlag(ModuleAttributes.ILOnly),
            Is64BitOnly = definition.MainModule.Architecture == TargetArchitecture.AMD64 || definition.MainModule.Architecture == TargetArchitecture.IA64,
            Is32BitOnly = definition.MainModule.Attributes.HasFlag(ModuleAttributes.Required32Bit) && !definition.MainModule.Attributes.HasFlag(ModuleAttributes.Preferred32Bit),
            Is32BitPreferred = definition.MainModule.Attributes.HasFlag(ModuleAttributes.Preferred32Bit)
          };

          a = new KeyValuePair<string, AssemblyInfo>(GetFileMD5Hash(assemblyPath), info);
          AssemblyInfoCache.TryAdd(assemblyPath, a);
        }
      }

      return a.Value;
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
        throw new ArgumentNullException(nameof(assemblyPath));
      }

      if (string.IsNullOrWhiteSpace(referenceAssemblyPath))
      {
        throw new ArgumentNullException(nameof(referenceAssemblyPath));
      }

      // Make sure the file actually exists.
      if (!File.Exists(assemblyPath))
      {
        throw new FileNotFoundException($"Could not find provided assembly file '{assemblyPath}'.", assemblyPath);
      }

      if (!File.Exists(referenceAssemblyPath))
      {
        throw new FileNotFoundException($"Could not find provided reference assembly file '{referenceAssemblyPath}'.", referenceAssemblyPath);
      }

      bool fixApplied = false;
      var keyPair = GetStrongNameKeyPair(keyPath, keyFilePassword);

      using (var fileManagerA = new OutputFileManager(assemblyPath, assemblyPath))
      using (var fileManagerB = new OutputFileManager(referenceAssemblyPath, referenceAssemblyPath))
      {
        using (var a = AssemblyDefinition.ReadAssembly(assemblyPath, GetReadParameters(assemblyPath, probingPaths)))
        using (var b = AssemblyDefinition.ReadAssembly(referenceAssemblyPath, GetReadParameters(referenceAssemblyPath, probingPaths)))
        {
          var assemblyReference = a.MainModule.AssemblyReferences.FirstOrDefault(r => r.Name.Equals(b.Name.Name, StringComparison.OrdinalIgnoreCase));

          // Found a matching reference, let's set the public key token.
          if (assemblyReference != null && BitConverter.ToString(assemblyReference.PublicKeyToken) != BitConverter.ToString(b.Name.PublicKeyToken))
          {
            assemblyReference.PublicKeyToken = b.Name.PublicKeyToken ?? Array.Empty<byte>();
            assemblyReference.Version = b.Name.Version;

            if (!a.Name.IsRetargetable)
            {
              a.Name.HashAlgorithm = AssemblyHashAlgorithm.SHA1;
              a.Name.PublicKey = GetPublicKey(keyPair);
              a.Name.HasPublicKey = true;
              a.Name.Attributes &= AssemblyAttributes.PublicKey;

              a.Write(fileManagerA.IntermediateAssemblyPath, new WriterParameters { StrongNameKeyBlob = keyPair, WriteSymbols = File.Exists(Path.ChangeExtension(assemblyPath, ".pdb")) });
            }

            AssemblyInfoCache.TryRemove(assemblyPath, out var _);

            fixApplied = true;
          }

          var friendReference = b.CustomAttributes.SingleOrDefault(attr => attr.AttributeType.FullName == typeof(InternalsVisibleToAttribute).FullName &&
            attr.ConstructorArguments[0].Value.ToString() == a.Name.Name);

          if (friendReference != null && a.Name.HasPublicKey)
          {
            // Add the public key to the attribute.
            var typeRef = friendReference.ConstructorArguments[0].Type;
            friendReference.ConstructorArguments.Clear();
            friendReference.ConstructorArguments.Add(new CustomAttributeArgument(typeRef, a.Name.Name + ", PublicKey=" + BitConverter.ToString(a.Name.PublicKey).Replace("-", string.Empty)));

            if (!b.Name.IsRetargetable)
            {
              b.Name.HashAlgorithm = AssemblyHashAlgorithm.SHA1;
              b.Name.PublicKey = GetPublicKey(keyPair);
              b.Name.HasPublicKey = true;
              b.Name.Attributes &= AssemblyAttributes.PublicKey;

              // Save and resign.
              b.Write(fileManagerB.IntermediateAssemblyPath, new WriterParameters { StrongNameKeyBlob = keyPair, WriteSymbols = File.Exists(Path.ChangeExtension(referenceAssemblyPath, ".pdb")) });
            }

            AssemblyInfoCache.TryRemove(assemblyPath, out var _);

            fixApplied = true;
          }
        }

        fileManagerA.Commit();
        fileManagerB.Commit();
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
        throw new ArgumentNullException(nameof(assemblyPath));
      }

      // Make sure the file actually exists.
      if (!File.Exists(assemblyPath))
      {
        throw new FileNotFoundException($"Could not find provided assembly file '{assemblyPath}'.", assemblyPath);
      }

      bool fixApplied = false;
      var keyPair = GetStrongNameKeyPair(keyPath, keyFilePassword);

      using (var outFileMgr = new OutputFileManager(assemblyPath, assemblyPath))
      {
        using (var a = AssemblyDefinition.ReadAssembly(assemblyPath, GetReadParameters(assemblyPath, probingPaths)))
        {
          var ivtAttributes = a.CustomAttributes.Where(attr => attr.AttributeType.FullName == typeof(InternalsVisibleToAttribute).FullName).ToList();

          foreach (var friendReference in ivtAttributes)
          {
            // Find any without a public key defined.
            if (friendReference.HasConstructorArguments && friendReference.ConstructorArguments.Any(ca => ca.Value?.ToString().IndexOf("PublicKey=", StringComparison.Ordinal) == -1))
            {
              a.CustomAttributes.Remove(friendReference);
              fixApplied = true;
            }
          }

          if (fixApplied)
          {
            if (!a.Name.IsRetargetable)
            {
              a.Name.HashAlgorithm = AssemblyHashAlgorithm.SHA1;
              a.Name.PublicKey = GetPublicKey(keyPair);
              a.Name.HasPublicKey = true;
              a.Name.Attributes &= AssemblyAttributes.PublicKey;

              a.Write(outFileMgr.IntermediateAssemblyPath, new WriterParameters { StrongNameKeyBlob = keyPair, WriteSymbols = File.Exists(Path.ChangeExtension(assemblyPath, ".pdb")) });
            }

            AssemblyInfoCache.TryRemove(assemblyPath, out var _);
          }
        }

        outFileMgr.Commit();
      }

      return fixApplied;
    }

    internal static byte[] GetStrongNameKeyPair(string keyPath, string keyFilePassword)
    {
      if (!string.IsNullOrEmpty(keyPath))
      {
        if (!string.IsNullOrEmpty(keyFilePassword))
        {
          var cert = new X509Certificate2(keyPath, keyFilePassword, X509KeyStorageFlags.Exportable);

          if (!(cert.PrivateKey is RSACryptoServiceProvider provider))
          {
            throw new InvalidOperationException("The key file is not password protected or the incorrect password was provided.");
          }

          return provider.ExportCspBlob(true);
        }
        else
        {
          return File.ReadAllBytes(keyPath);
        }
      }
      else
      {
        // Only cache generated keys so all signed assemblies use the same public key.
        if (keyPairCache != null)
        {
          return keyPairCache;
        }

        keyPairCache = GenerateStrongNameKeyPair();

        return keyPairCache;
      }
    }

    private static ReaderParameters GetReadParameters(string assemblyPath, string[] probingPaths)
    {
      using (var resolver = new DefaultAssemblyResolver())
      {
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

        ReaderParameters readParams;
        try
        {
          readParams = new ReaderParameters() { AssemblyResolver = resolver, ReadSymbols = File.Exists(Path.ChangeExtension(assemblyPath, ".pdb")) };
        }
        catch (InvalidOperationException)
        {
          readParams = new ReaderParameters() { AssemblyResolver = resolver };
        }

        return readParams;
      }
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

    private static string GetFileMD5Hash(string filePath)
    {
      if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
      {
        return string.Empty;
      }

      var sb = new StringBuilder();
      using (var md5 = MD5.Create())
      {
        byte[] bytes = File.ReadAllBytes(filePath);
        byte[] encoded = md5.ComputeHash(bytes);

        for (int i = 0; i < encoded.Length; i++)
        {
          sb.Append(encoded[i].ToString("X2", CultureInfo.InvariantCulture));
        }
      }

      return sb.ToString();
    }

    // https://raw.githubusercontent.com/atykhyy/cecil/master/Mono.Security.Cryptography/CryptoService.cs
    private static byte[] GetPublicKey(byte[] keyBlob)
    {
      using (var rsa = CryptoConvert.FromCapiKeyBlob(keyBlob))
      {
        var cspBlob = CryptoConvert.ToCapiPublicKeyBlob(rsa);
        var publicKey = new byte[12 + cspBlob.Length];
        Buffer.BlockCopy(cspBlob, 0, publicKey, 12, cspBlob.Length);
        // The first 12 bytes are documented at:
        // http://msdn.microsoft.com/library/en-us/cprefadd/html/grfungethashfromfile.asp
        // ALG_ID - Signature
        publicKey[1] = 36;
        // ALG_ID - Hash
        publicKey[4] = 4;
        publicKey[5] = 128;
        // Length of Public Key (in bytes)
        publicKey[8] = (byte)(cspBlob.Length >> 0);
        publicKey[9] = (byte)(cspBlob.Length >> 8);
        publicKey[10] = (byte)(cspBlob.Length >> 16);
        publicKey[11] = (byte)(cspBlob.Length >> 24);

        return publicKey;
      }
    }
  }
}
