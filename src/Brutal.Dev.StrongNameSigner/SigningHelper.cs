using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Resources;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;
using Mono.Cecil;
using Mono.Security.Cryptography;

namespace Brutal.Dev.StrongNameSigner
{
  /// <summary>
  /// Static helper class for easily getting assembly information and strong-name signing .NET assemblies.
  /// </summary>
  public static class SigningHelper
  {
    private static byte[] keyPairCache;

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
    /// <param name="keyFilePath">The path to the strong-name key file you want to use (.snk or.pfx).</param>
    /// <returns>The assembly information of the new strong-name signed assembly.</returns>
    public static AssemblyInfo SignAssembly(string assemblyPath, string keyFilePath)
    {
      return SignAssembly(assemblyPath, keyFilePath, string.Empty, string.Empty);
    }

    /// <summary>
    /// Signs the assembly at the specified path with your own strong-name key file.
    /// </summary>
    /// <param name="assemblyPath">The path to the assembly you want to strong-name sign.</param>
    /// <param name="keyFilePath">The path to the strong-name key file you want to use (.snk or .pfx).</param>
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
    public static AssemblyInfo SignAssembly(string assemblyPath, string keyFilePath, string outputPath)
    {
      return SignAssembly(assemblyPath, keyFilePath, outputPath, string.Empty);
    }

    /// <summary>
    /// Signs the assembly at the specified path with your own strong-name key file.
    /// </summary>
    /// <param name="assemblyPath">The path to the assembly you want to strong-name sign.</param>
    /// <param name="keyFilePath">The path to the strong-name key file you want to use (.snk or .pfx).</param>
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
    public static AssemblyInfo SignAssembly(string assemblyPath, string keyFilePath, string outputPath, string keyFilePassword, params string[] probingPaths)
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

      var keyPair = GetStrongNameKeyPair(keyFilePath, keyFilePassword);
      var publicKey = GetPublicKey(keyPair);

      using var targetAssembly = new AssemblyInfo(assemblyPath, probingPaths);
      var assemblies = new List<AssemblyInfo>(1) { targetAssembly };
      var unignedAssemblies = assemblies.Where(a => !a.IsSigned).ToList();

      // Add references that need to be updated and signed.
      var set = new HashSet<string>(unignedAssemblies.Select(x => x.Definition.Name.Name));

      foreach (var assembly in assemblies)
      {
        foreach (var reference in assembly.Definition.MainModule.AssemblyReferences
          .Where(reference => set.Contains(reference.Name)))
        {
          reference.PublicKey = publicKey;
          unignedAssemblies.Add(assembly);
        }
      }

      // Strong-name sign all the unsigned assemblies.
      foreach (var assemblyInfo in unignedAssemblies)
      {
        var name = assemblyInfo.Definition.Name;
        name.HashAlgorithm = AssemblyHashAlgorithm.SHA1;
        name.PublicKey = publicKey;
        name.HasPublicKey = true;
        name.Attributes &= AssemblyAttributes.PublicKey;
      }

      // Fix InternalVisibleToAttribute.
      foreach (var assemblyDefinition in unignedAssemblies.ConvertAll(a => a.Definition))
      {
        foreach (var attribute in assemblyDefinition.CustomAttributes
          .Where(attr => attr.AttributeType.FullName == typeof(InternalsVisibleToAttribute).FullName)
          .ToList())
        {
          var argument = attribute.ConstructorArguments[0];
          if (argument.Type == assemblyDefinition.MainModule.TypeSystem.String)
          {
            var originalAssemblyName = (string)argument.Value;
            var signedAssembly = unignedAssemblies.Find(a => a.Definition.Name.Name == originalAssemblyName);
            if (signedAssembly == null)
            {
              assemblyDefinition.CustomAttributes.Remove(attribute);
            }
            else
            {
              var assemblyName = signedAssembly.Definition.Name.Name + ", PublicKey=" + BitConverter.ToString(signedAssembly.Definition.Name.PublicKey).Replace("-", string.Empty);
              var updatedArgument = new CustomAttributeArgument(argument.Type, assemblyName);

              attribute.ConstructorArguments.Clear();
              attribute.ConstructorArguments.Add(updatedArgument);
            }
          }
        }
      }

      var outputFile = Path.Combine(Path.GetFullPath(outputPath), Path.GetFileName(assemblyPath));

      // Write all updated assemblies.
      foreach (var assemblyInfo in unignedAssemblies.Where(a => !a.Definition.Name.IsRetargetable))
      {
        using var outputFileMgr = new OutputFileManager(assemblyInfo.FilePath, outputFile);

        if (outputFileMgr.IsInPlaceReplace)
        {
          outputFileMgr.CreateBackup();
        }

        assemblyInfo.Save(outputFileMgr.IntermediateAssemblyPath, keyPair);
        assemblyInfo.Dispose();

        outputFileMgr.Commit();
      }

      return new AssemblyInfo(outputFile);
    }

    private static byte[] GenerateStrongNameKeyPair()
    {
      using var provider = new RSACryptoServiceProvider(4096);
      return provider.ExportCspBlob(!provider.PublicOnly);
    }

    private static byte[] GetStrongNameKeyPair(string keyFilePath, string keyFilePassword)
    {
      if (!string.IsNullOrEmpty(keyFilePath))
      {
        if (!string.IsNullOrEmpty(keyFilePassword))
        {
          var cert = new X509Certificate2(keyFilePath, keyFilePassword, X509KeyStorageFlags.Exportable);

          if (cert.PrivateKey is not RSACryptoServiceProvider provider)
          {
            throw new InvalidOperationException("The key file is not password protected or the incorrect password was provided.");
          }

          return provider.ExportCspBlob(true);
        }
        else
        {
          return File.ReadAllBytes(keyFilePath);
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

    // https://raw.githubusercontent.com/atykhyy/cecil/master/Mono.Security.Cryptography/CryptoService.cs
    private static byte[] GetPublicKey(byte[] keyBlob)
    {
      using var rsa = CryptoConvert.FromCapiKeyBlob(keyBlob);
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

    private static void FixBaml(string publicKeyToken, ResourceWriter rw, string resourceName, List<char> charList, List<Match> elementsToReplace)
    {
      elementsToReplace = elementsToReplace.OrderBy(x => x.Index).ToList();

      using var buffer = new MemoryStream();
      using var bufferWriter = new BinaryWriter(buffer);
      for (var i = 0; i < charList.Count; i++)
      {
        if (elementsToReplace.Count > 0 && elementsToReplace[0].Index == i)
        {
          var match = elementsToReplace[0];
          bufferWriter.Write((byte)0x1C);

          var newAssembly =
            string.Format(
              "{0}, Version={1}, Culture={2}, PublicKeyToken={3}",
              match.Groups["name"].Value,
              match.Groups["version"].Value,
              match.Groups["culture"].Value,
              publicKeyToken);

          var length = Get7BitEncoded(newAssembly.Length).Length + newAssembly.Length + 3;
          var totalLength = Get7BitEncoded(length);
          bufferWriter.Write(totalLength);

          var id = match.Groups["id"].Value;
          bufferWriter.Write((byte)id[0]);
          bufferWriter.Write((byte)id[1]);
          bufferWriter.Write(newAssembly);

          i += match.Length - 1;

          elementsToReplace.RemoveAt(0);
        }
        else
        {
          var b = (byte)charList[i];
          bufferWriter.Write(b);
        }
      }

      bufferWriter.Flush();

      using var mst = new MemoryStream(buffer.ToArray());
      rw.AddResource(resourceName, mst);
    }

    private static byte[] Get7BitEncoded(int value)
    {
      var list = new List<byte>();
      var num = (uint)value;

      while (num >= 128U)
      {
        list.Add((byte)(num | 128U));
        num >>= 7;
      }

      list.Add((byte)num);
      return list.ToArray();
    }
  }
}
