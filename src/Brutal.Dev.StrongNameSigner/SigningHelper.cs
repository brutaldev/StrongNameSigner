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
    /// Provide a message logging method. If this is not set then the console will be used.
    /// </summary>
    public static Action<string> Log { get; set; }

    /// <summary>
    /// Signs the assembly at the specified path.
    /// </summary>
    /// <param name="assemblyPath">The path to the assembly you want to strong-name sign.</param>
    /// <returns>The assembly information of the new strong-name signed assembly.</returns>
    public static AssemblyInfo SignAssembly(string assemblyPath) => SignAssembly(assemblyPath, string.Empty, string.Empty, string.Empty);

    /// <summary>
    /// Signs the assembly at the specified path with your own strong-name key file.
    /// </summary>
    /// <param name="assemblyPath">The path to the assembly you want to strong-name sign.</param>
    /// <param name="keyFilePath">The path to the strong-name key file you want to use (.snk or.pfx).</param>
    /// <returns>The assembly information of the new strong-name signed assembly.</returns>
    public static AssemblyInfo SignAssembly(string assemblyPath, string keyFilePath) => SignAssembly(assemblyPath, keyFilePath, string.Empty, string.Empty);

    /// <summary>
    /// Signs the assembly at the specified path with your own strong-name key file.
    /// </summary>
    /// <param name="assemblyPath">The path to the assembly you want to strong-name sign.</param>
    /// <param name="keyFilePath">The path to the strong-name key file you want to use (.snk or .pfx).</param>
    /// <param name="outputPath">The directory path where the strong-name signed assembly will be copied to.</param>
    /// <returns>
    /// The assembly information of the new strong-name signed assembly.
    /// </returns>
    /// <exception cref="System.ArgumentNullException">assemblyPath parameter was not provided.</exception>
    /// <exception cref="System.IO.FileNotFoundException">Could not find provided assembly file.
    /// or
    /// Could not find provided strong-name key file.</exception>
    /// <exception cref="System.BadImageFormatException">The file is not a .NET managed assembly.</exception>
    public static AssemblyInfo SignAssembly(string assemblyPath, string keyFilePath, string outputPath) => SignAssembly(assemblyPath, keyFilePath, outputPath, string.Empty);

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

      var outputFile = Path.Combine(Path.GetFullPath(outputPath), Path.GetFileName(assemblyPath));

      SignAssemblies(new[] { new InputOutputPair(assemblyPath, outputFile) }, keyFilePath, keyFilePassword, probingPaths);

      return new AssemblyInfo(outputFile, probingPaths);
    }

    /// <summary>
    /// Signs the assembly at the specified path with your own strong-name key file.
    /// </summary>
    /// <param name="assemblyPaths">The paths to all the assemblies you want to strong-name sign and their references to fix.</param>
    /// <exception cref="System.IO.FileNotFoundException">Could not find one of the provided assembly files.
    /// or
    /// Could not find provided strong-name key file.</exception>
    /// <exception cref="System.BadImageFormatException">One or more files are not a .NET managed assemblies.</exception>
    public static void SignAssemblies(IEnumerable<string> assemblyPaths) => SignAssemblies(assemblyPaths, string.Empty, string.Empty);

    /// <summary>
    /// Signs the assembly at the specified path with your own strong-name key file.
    /// </summary>
    /// <param name="assemblyPaths">The paths to all the assemblies you want to strong-name sign and their references to fix.</param>
    /// <param name="keyFilePath">The path to the strong-name key file you want to use (.snk or .pfx).</param>
    /// <exception cref="System.IO.FileNotFoundException">Could not find one of the provided assembly files.
    /// or
    /// Could not find provided strong-name key file.</exception>
    /// <exception cref="System.BadImageFormatException">One or more files are not a .NET managed assemblies.</exception>
    public static void SignAssemblies(IEnumerable<string> assemblyPaths, string keyFilePath) => SignAssemblies(assemblyPaths, keyFilePath, string.Empty);

    /// <summary>
    /// Signs the assembly at the specified path with your own strong-name key file.
    /// </summary>
    /// <param name="assemblyPaths">The paths to all the assemblies you want to strong-name sign and their references to fix.</param>
    /// <param name="keyFilePath">The path to the strong-name key file you want to use (.snk or .pfx).</param>
    /// <param name="keyFilePassword">The password for the provided strong-name key file.</param>
    /// <param name="probingPaths">Additional paths to probe for references.</param>
    /// <exception cref="System.IO.FileNotFoundException">Could not find one of the provided assembly files.
    /// or
    /// Could not find provided strong-name key file.</exception>
    /// <exception cref="System.BadImageFormatException">One or more files are not a .NET managed assemblies.</exception>
    public static void SignAssemblies(IEnumerable<string> assemblyPaths, string keyFilePath, string keyFilePassword, params string[] probingPaths)
      => SignAssemblies(assemblyPaths.Select(path => new InputOutputPair(path, path)), keyFilePath, keyFilePassword, probingPaths);

    /// <summary>
    /// Signs the assembly at the specified path with your own strong-name key file.
    /// </summary>
    /// <param name="assemblyInputOutputPaths">The input and output paths to all the assemblies you want to strong-name sign and their references to fix.</param>
    /// <exception cref="System.IO.FileNotFoundException">Could not find one of the provided assembly files.
    /// or
    /// Could not find provided strong-name key file.</exception>
    /// <exception cref="System.BadImageFormatException">One or more files are not a .NET managed assemblies.</exception>
    public static void SignAssemblies(IEnumerable<InputOutputPair> assemblyInputOutputPaths) => SignAssemblies(assemblyInputOutputPaths, string.Empty, string.Empty);

    /// <summary>
    /// Signs the assembly at the specified path with your own strong-name key file.
    /// </summary>
    /// <param name="assemblyInputOutputPaths">The input and output paths to all the assemblies you want to strong-name sign and their references to fix.</param>
    /// <param name="keyFilePath">The path to the strong-name key file you want to use (.snk or .pfx).</param>
    /// <exception cref="System.IO.FileNotFoundException">Could not find one of the provided assembly files.
    /// or
    /// Could not find provided strong-name key file.</exception>
    /// <exception cref="System.BadImageFormatException">One or more files are not a .NET managed assemblies.</exception>
    public static void SignAssemblies(IEnumerable<InputOutputPair> assemblyInputOutputPaths, string keyFilePath) => SignAssemblies(assemblyInputOutputPaths, keyFilePath, string.Empty);

    /// <summary>
    /// Signs the assembly at the specified path with your own strong-name key file.
    /// </summary>
    /// <param name="assemblyInputOutputPaths">The input and output paths to all the assemblies you want to strong-name sign and their references to fix.</param>
    /// <param name="keyFilePath">The path to the strong-name key file you want to use (.snk or .pfx).</param>
    /// <param name="keyFilePassword">The password for the provided strong-name key file.</param>
    /// <param name="probingPaths">Additional paths to probe for references.</param>
    /// <exception cref="System.IO.FileNotFoundException">Could not find one of the provided assembly files.
    /// or
    /// Could not find provided strong-name key file.</exception>
    /// <exception cref="System.BadImageFormatException">One or more files are not a .NET managed assemblies.</exception>
    public static void SignAssemblies(IEnumerable<InputOutputPair> assemblyInputOutputPaths, string keyFilePath, string keyFilePassword, params string[] probingPaths)
    {
      // If no logger has been set, just use the console.
      if (Log == null)
      {
        Log = message => Console.WriteLine($"SNS: {message}");
      }

      // Verify assembly paths were passed in.
      if (assemblyInputOutputPaths?.Any() != true)
      {
        Log("No assembly paths were provided.");
        return;
      }

      // Make sure the files actually exist.
      foreach (var assemblyInputPath in assemblyInputOutputPaths.Select(aio => aio.InputFilePath))
      {
        if (!File.Exists(assemblyInputPath))
        {
          throw new FileNotFoundException($"Could not find provided input assembly file '{assemblyInputPath}'.", assemblyInputPath);
        }
      }

      // Convert all path into AssemblyInfo objects.
      var assemblies = new List<AssemblyInfo>();
      foreach (var filePath in assemblyInputOutputPaths)
      {
        try
        {
          assemblies.Add(new AssemblyInfo(filePath.InputFilePath, probingPaths));
        }
        catch (Exception ex)
        {
          Log(ex.ToString());
        }
      }

      var unignedAssemblies = assemblies.Where(a => !a.IsSigned).ToList();

      var keyPair = GetStrongNameKeyPair(keyFilePath, keyFilePassword);
      var publicKey = GetPublicKey(keyPair);

      // Add references that need to be updated and signed.
      var set = new HashSet<string>(unignedAssemblies.Select(x => x.Definition.Name.Name));

      foreach (var assembly in assemblies)
      {
        Log($"Checking assembly references in '{assembly.FilePath}'.");

        foreach (var reference in assembly.Definition.MainModule.AssemblyReferences
          .Where(reference => set.Contains(reference.Name)))
        {
          reference.PublicKey = publicKey;
          unignedAssemblies.Add(assembly);
        }
      }

      // Strong-name sign all the unsigned assemblies.
      foreach (var assembly in unignedAssemblies)
      {
        Log($"Signing assembly '{assembly.FilePath}'.");

        var name = assembly.Definition.Name;
        name.HashAlgorithm = AssemblyHashAlgorithm.SHA1;
        name.PublicKey = publicKey;
        name.HasPublicKey = true;
        name.Attributes |= AssemblyAttributes.PublicKey;
      }

      // Fix InternalVisibleToAttribute.
      foreach (var assembly in unignedAssemblies)
      {
        foreach (var attribute in assembly.Definition.CustomAttributes
          .Where(attr => attr.AttributeType.FullName == typeof(InternalsVisibleToAttribute).FullName)
          .ToList())
        {
          var argument = attribute.ConstructorArguments[0];
          if (argument.Type == assembly.Definition.MainModule.TypeSystem.String)
          {
            var originalAssemblyName = (string)argument.Value;
            var signedAssembly = unignedAssemblies.Find(a => a.Definition.Name.Name == originalAssemblyName);

            if (signedAssembly == null)
            {
              Log($"Removing invalid friend reference from assembly '{assembly.FilePath}'.");

              assembly.Definition.CustomAttributes.Remove(attribute);
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

      // Fix BAML references.
      

      // Write all updated assemblies.
      foreach (var assembly in unignedAssemblies.Where(a => !a.Definition.Name.IsRetargetable))
      {
        using var outputFileMgr = new OutputFileManager(assembly.FilePath, assemblyInputOutputPaths.First(a => Path.GetFullPath(a.InputFilePath) == assembly.FilePath).OutFilePath);

        if (outputFileMgr.IsInPlaceReplace)
        {
          outputFileMgr.CreateBackup();
        }

        Log($"Saving changes to assembly '{assembly.FilePath}'...");

        assembly.Save(outputFileMgr.IntermediateAssemblyPath, keyPair);
        assembly.Dispose();

        outputFileMgr.Commit();
      }
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
