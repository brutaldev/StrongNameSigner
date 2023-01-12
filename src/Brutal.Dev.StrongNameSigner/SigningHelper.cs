using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
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
    private const string Size = @"[\u0080-\u00FF]{0,4}[\u0000-\u0079]";

    private static readonly Regex BamlRegex = new(
      @"(?<marker>\u001C)(?<totalsize>" + Size +
      ")(?<id>..)(?<size>" + Size +
      @")(?<name>(?:\w+\.)*\w+), Version=(?<version>(?:\d+\.){3}\d+), Culture=(?<culture>(?:\w|\-)+), PublicKeyToken=(?<token>null|(?:\d|[abcdef]){16})",
      RegexOptions.CultureInvariant | RegexOptions.Singleline);

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

      SignAssemblies(new[] { new InputOutputFilePair(assemblyPath, outputFile) }, keyFilePath, keyFilePassword, probingPaths);

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
      => SignAssemblies(assemblyPaths.Select(path => new InputOutputFilePair(path, path)), keyFilePath, keyFilePassword, probingPaths);

    /// <summary>
    /// Signs the assembly at the specified path with your own strong-name key file.
    /// </summary>
    /// <param name="assemblyInputOutputPaths">The input and output paths to all the assemblies you want to strong-name sign and their references to fix.</param>
    /// <exception cref="System.IO.FileNotFoundException">Could not find one of the provided assembly files.
    /// or
    /// Could not find provided strong-name key file.</exception>
    /// <exception cref="System.BadImageFormatException">One or more files are not a .NET managed assemblies.</exception>
    public static void SignAssemblies(IEnumerable<InputOutputFilePair> assemblyInputOutputPaths) => SignAssemblies(assemblyInputOutputPaths, string.Empty, string.Empty);

    /// <summary>
    /// Signs the assembly at the specified path with your own strong-name key file.
    /// </summary>
    /// <param name="assemblyInputOutputPaths">The input and output paths to all the assemblies you want to strong-name sign and their references to fix.</param>
    /// <param name="keyFilePath">The path to the strong-name key file you want to use (.snk or .pfx).</param>
    /// <exception cref="System.IO.FileNotFoundException">Could not find one of the provided assembly files.
    /// or
    /// Could not find provided strong-name key file.</exception>
    /// <exception cref="System.BadImageFormatException">One or more files are not a .NET managed assemblies.</exception>
    public static void SignAssemblies(IEnumerable<InputOutputFilePair> assemblyInputOutputPaths, string keyFilePath) => SignAssemblies(assemblyInputOutputPaths, keyFilePath, string.Empty);

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
    public static void SignAssemblies(IEnumerable<InputOutputFilePair> assemblyInputOutputPaths, string keyFilePath, string keyFilePassword, params string[] probingPaths)
    {
      // If no logger has been set, just use the console.
      Log ??= Console.WriteLine;

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

      Log("1. Loading assemblies...");

      // Convert all paths into AssemblyInfo objects.
      var allAssemblies = new HashSet<AssemblyInfo>();

      // File locking issues in Mono.Cecil are a real pain in the ass so let's create a working directory of files to process, then copy them back when we're done.
      var tempFilePathToInputOutputFilePairMap = new Dictionary<string, InputOutputFilePair>();
      var tempPath = Path.Combine(Path.GetTempPath(), "StrongNameSigner");
      Directory.CreateDirectory(tempPath);

      foreach (var inputOutpuFilePair in assemblyInputOutputPaths)
      {
        try
        {
          var tempFilePath = Path.Combine(tempPath, $"{Path.GetFileNameWithoutExtension(inputOutpuFilePair.InputFilePath)}.{Guid.NewGuid()}{Path.GetExtension(inputOutpuFilePair.InputFilePath)}");
          File.Copy(inputOutpuFilePair.InputFilePath, tempFilePath, true);

          if (inputOutpuFilePair.HasSymbols)
          {
            File.Copy(inputOutpuFilePair.InputPdbPath, Path.ChangeExtension(tempFilePath, ".pdb"), true);
          }

          tempFilePathToInputOutputFilePairMap.Add(tempFilePath, inputOutpuFilePair);

          allAssemblies.Add(new AssemblyInfo(tempFilePath, probingPaths));
        }
        catch (BadImageFormatException ex)
        {
          Log($"   Unsupported assembly '{inputOutpuFilePair.InputFilePath}': {ex.Message}");
        }
        catch (Exception ex)
        {
          Log($"   Failed to load assembly '{inputOutpuFilePair.InputFilePath}': {ex.Message}");
        }
      }

      Log("2. Checking assembly references...");

      try
      {
        // Start with assemblies that are not signed.
        var assembliesToProcess = new HashSet<AssemblyInfo>(allAssemblies.Where(a => !a.IsSigned));

        var keyPair = GetStrongNameKeyPair(keyFilePath, keyFilePassword);
        var publicKey = GetPublicKey(keyPair);
        var token = GetPublicKeyToken(publicKey);

        // Add references that need to be updated and signed.
        var set = new HashSet<string>(assembliesToProcess.Select(x => x.Definition.Name.Name));

        foreach (var assembly in allAssemblies)
        {
          Log($"   Checking assembly references in '{tempFilePathToInputOutputFilePairMap[assembly.FilePath].InputFilePath}'.");

          foreach (var reference in assembly.Definition.MainModule.AssemblyReferences
            .Where(reference => set.Contains(reference.Name)))
          {
            reference.PublicKey = publicKey;
            assembliesToProcess.Add(assembly);
          }
        }

        Log("3. Strong-name unsigned assemblies...");

        // Strong-name sign all the unsigned assemblies.
        foreach (var assembly in assembliesToProcess)
        {
          Log($"   Signing assembly '{tempFilePathToInputOutputFilePairMap[assembly.FilePath].InputFilePath}'.");

          var name = assembly.Definition.Name;
          name.HashAlgorithm = AssemblyHashAlgorithm.SHA1;
          name.PublicKey = publicKey;
          name.HasPublicKey = true;
          name.Attributes |= AssemblyAttributes.PublicKey;
        }

        Log("4. Fix InternalVisibleToAttribute references...");

        // Fix InternalVisibleToAttribute.
        foreach (var assembly in allAssemblies)
        {
          foreach (var constructorArguments in assembly.Definition.CustomAttributes
            .Where(attr => attr.AttributeType.FullName == typeof(InternalsVisibleToAttribute).FullName)
            .Select(attr => attr.ConstructorArguments)
            .ToList())
          {
            var argument = constructorArguments[0];
            if (argument.Type == assembly.Definition.MainModule.TypeSystem.String)
            {
              var originalAssemblyName = (string)argument.Value;
              var signedAssembly = assembliesToProcess.FirstOrDefault(a => a.Definition.Name.Name == originalAssemblyName);

              if (signedAssembly != null)
              {
                Log($"   Fixing {signedAssembly.Definition.Name.Name} friend reference in assembly '{tempFilePathToInputOutputFilePairMap[assembly.FilePath].InputFilePath}'.");

                var assemblyName = signedAssembly.Definition.Name.Name + ", PublicKey=" + BitConverter.ToString(signedAssembly.Definition.Name.PublicKey).Replace("-", string.Empty);
                var updatedArgument = new CustomAttributeArgument(argument.Type, assemblyName);

                constructorArguments.Clear();
                constructorArguments.Add(updatedArgument);
              }
            }
          }
        }

        Log("4a. Fix CustomAttributes with Type references...");

        // Fix CustomAttributes with Type references.
        foreach (var assembly in allAssemblies)
        {
          foreach (var constructorArguments in assembly.Definition.CustomAttributes
            .Select(attr => attr.ConstructorArguments)
            .ToList())
          {
            foreach (var argument in constructorArguments.ToArray())
            {
              if (argument.Type.FullName == "System.Type" &&
                argument.Value is TypeReference typeRef)
              {
                
                var signedAssembly = assembliesToProcess.FirstOrDefault(a => a.Definition.Name.Name == typeRef.Scope.Name);

                if (signedAssembly != null)
                {
                  Log($"   Fixing {signedAssembly.Definition.Name.Name} reference in CustomAttribute in assembly '{tempFilePathToInputOutputFilePairMap[assembly.FilePath].InputFilePath}'.");

                  var updatedTypeRef = signedAssembly.Definition.MainModule.GetType(typeRef.FullName);

                  var updatedArgument = new CustomAttributeArgument(argument.Type, updatedTypeRef);
                  var idx = constructorArguments.IndexOf(argument);
                  constructorArguments.RemoveAt(idx);
                  constructorArguments.Insert(idx, updatedArgument);
                }

              }
            }
          }
        }



        Log("5. Fix BAML references...");

        // Fix BAML references.
        foreach (var assembly in allAssemblies)
        {
          foreach (var resources in assembly.Definition.Modules.Select(module => module.Resources))
          {
            var resArray = resources.ToArray();
            for (var resIndex = 0; resIndex < resArray.Length; resIndex++)
            {
              var resource = resArray[resIndex];
              if (resource.ResourceType == ResourceType.Embedded)
              {
                if (!resource.Name.EndsWith(".g.resources"))
                {
                  continue;
                }

                var embededResource = (EmbeddedResource)resource;
                var modifyResource = false;

                using var memoryStream = new MemoryStream();
                using var writer = new ResourceWriter(memoryStream);

                using var resourceStream = embededResource.GetResourceStream();
                using var reader = new ResourceReader(resourceStream);

                foreach (var entry in reader.OfType<DictionaryEntry>().ToArray())
                {
                  var resourceName = entry.Key.ToString();

                  if (resourceName.EndsWith(".baml", StringComparison.InvariantCulture) && entry.Value is Stream bamlStream)
                  {
                    var br = new BinaryReader(bamlStream);
                    var datab = br.ReadBytes((int)br.BaseStream.Length);

                    var charList = datab.Select(b => (char)b).ToList();
                    var data = new string(charList.ToArray());
                    var elementsToReplace = new List<Match>();

                    foreach (Match match in BamlRegex.Matches(data))
                    {
                      var name = match.Groups["name"].Value;
                      if (assembliesToProcess.Any(x => x.Definition.Name.Name == name))
                      {
                        elementsToReplace.Add(match);
                      }
                    }

                    if (elementsToReplace.Count != 0)
                    {
                      assembliesToProcess.Add(assembly);
                      modifyResource = true;

                      FixBinaryBaml(token, writer, resourceName, charList, elementsToReplace);
                    }
                    else
                    {
                      bamlStream.Position = 0;
                      writer.AddResource(resourceName, bamlStream);
                    }
                  }
                  else
                  {
                    writer.AddResource(resourceName, entry.Value);
                  }
                }

                if (modifyResource)
                {
                  Log($"   Replacing BAML entry in assembly '{tempFilePathToInputOutputFilePairMap[assembly.FilePath].InputFilePath}'.");

                  resources.RemoveAt(resIndex);
                  writer.Generate();
                  var array = memoryStream.ToArray();
                  memoryStream.Position = 0;

                  var newEmbeded = new EmbeddedResource(resource.Name, resource.Attributes, array);
                  resources.Insert(resIndex, newEmbeded);
                }
              }
            }
          }
        }

        Log("6. Save assembly changes...");

        // Write all updated assemblies.
        foreach (var assembly in assembliesToProcess.Where(a => !a.Definition.Name.IsRetargetable))
        {
          var inputOutpuFilePair = tempFilePathToInputOutputFilePairMap[assembly.FilePath];

          if (inputOutpuFilePair.IsSameFile)
          {
            File.Copy(inputOutpuFilePair.InputFilePath, inputOutpuFilePair.BackupAssemblyPath, true);

            if (inputOutpuFilePair.HasSymbols)
            {
              File.Copy(inputOutpuFilePair.InputPdbPath, inputOutpuFilePair.BackupPdbPath, true);
            }
          }

          Log($"   Saving changes to assembly '{inputOutpuFilePair.OutputFilePath}'.");

          try
          {
            assembly.Save(inputOutpuFilePair.OutputFilePath, keyPair);
          }
          catch (NotSupportedException ex)
          {
            Log($"   Failed to save assembly '{inputOutpuFilePair.OutputFilePath}': {ex.Message}");

            if (inputOutpuFilePair.IsSameFile)
            {
              // Restore the backup that would have been created above.
              File.Copy(inputOutpuFilePair.BackupAssemblyPath, inputOutpuFilePair.InputFilePath, true);
              File.Delete(inputOutpuFilePair.BackupAssemblyPath);

              if (inputOutpuFilePair.HasSymbols)
              {
                File.Copy(inputOutpuFilePair.BackupPdbPath, inputOutpuFilePair.InputPdbPath, true);
                File.Delete(inputOutpuFilePair.BackupPdbPath);
              }
            }
          }
          finally
          {
            assembly.Dispose();
          }
        }
      }
      finally
      {
        Log("7. Cleanup...");

        tempFilePathToInputOutputFilePairMap.Clear();

        foreach (var assembly in allAssemblies)
        {
          assembly.Dispose();
        }

        try
        {
          Directory.Delete(tempPath, true);
        }
        catch (Exception ex)
        {
          Log($"   Failed to delete temp working directory '{tempPath}': {ex.Message}");
        }
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

    private static string GetPublicKeyToken(byte[] publicKey)
    {
      using var csp = new SHA1CryptoServiceProvider();
      var hash = csp.ComputeHash(publicKey);
      var token = new byte[8];
      for (var i = 0; i < 8; i++)
      {
        token[i] = hash[hash.Length - (i + 1)];
      }

      return string.Concat(token.Select(x => x.ToString("x2")));
    }

    private static void FixBinaryBaml(string publicKeyToken, ResourceWriter rw, string resourceName, List<char> charList, List<Match> elementsToReplace)
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

      var mst = new MemoryStream(buffer.ToArray());
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
