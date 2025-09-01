using System;
using System.Collections;
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

      SignAssemblies([new InputOutputFilePair(assemblyPath, outputFile)], keyFilePath, keyFilePassword, probingPaths);

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
    public static bool SignAssemblies(IEnumerable<string> assemblyPaths) => SignAssemblies(assemblyPaths, string.Empty, string.Empty);

    /// <summary>
    /// Signs the assembly at the specified path with your own strong-name key file.
    /// </summary>
    /// <param name="assemblyPaths">The paths to all the assemblies you want to strong-name sign and their references to fix.</param>
    /// <param name="keyFilePath">The path to the strong-name key file you want to use (.snk or .pfx).</param>
    /// <exception cref="System.IO.FileNotFoundException">Could not find one of the provided assembly files.
    /// or
    /// Could not find provided strong-name key file.</exception>
    /// <exception cref="System.BadImageFormatException">One or more files are not a .NET managed assemblies.</exception>
    public static bool SignAssemblies(IEnumerable<string> assemblyPaths, string keyFilePath) => SignAssemblies(assemblyPaths, keyFilePath, string.Empty);

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
    public static bool SignAssemblies(IEnumerable<string> assemblyPaths, string keyFilePath, string keyFilePassword, params string[] probingPaths)
      => SignAssemblies(assemblyPaths.Select(path => new InputOutputFilePair(path, path)), keyFilePath, keyFilePassword, probingPaths);

    /// <summary>
    /// Signs the assembly at the specified path with your own strong-name key file.
    /// </summary>
    /// <param name="assemblyInputOutputPaths">The input and output paths to all the assemblies you want to strong-name sign and their references to fix.</param>
    /// <exception cref="System.IO.FileNotFoundException">Could not find one of the provided assembly files.
    /// or
    /// Could not find provided strong-name key file.</exception>
    /// <exception cref="System.BadImageFormatException">One or more files are not a .NET managed assemblies.</exception>
    public static bool SignAssemblies(IEnumerable<InputOutputFilePair> assemblyInputOutputPaths) => SignAssemblies(assemblyInputOutputPaths, string.Empty, string.Empty);

    /// <summary>
    /// Signs the assembly at the specified path with your own strong-name key file.
    /// </summary>
    /// <param name="assemblyInputOutputPaths">The input and output paths to all the assemblies you want to strong-name sign and their references to fix.</param>
    /// <param name="keyFilePath">The path to the strong-name key file you want to use (.snk or .pfx).</param>
    /// <exception cref="System.IO.FileNotFoundException">Could not find one of the provided assembly files.
    /// or
    /// Could not find provided strong-name key file.</exception>
    /// <exception cref="System.BadImageFormatException">One or more files are not a .NET managed assemblies.</exception>
    public static bool SignAssemblies(IEnumerable<InputOutputFilePair> assemblyInputOutputPaths, string keyFilePath) => SignAssemblies(assemblyInputOutputPaths, keyFilePath, string.Empty);

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
    public static bool SignAssemblies(IEnumerable<InputOutputFilePair> assemblyInputOutputPaths, string keyFilePath, string keyFilePassword, params string[] probingPaths)
    {
      // If no logger has been set, just use the console.
      Log ??= Console.WriteLine;

      // Verify assembly paths were passed in.
      if (assemblyInputOutputPaths?.Any() != true)
      {
        Log("No assembly paths were provided.");
        return false;
      }

      var hasErrors = false;
      var step = 1;

      // Make sure the files actually exist.
      foreach (var assemblyInputPath in assemblyInputOutputPaths.Select(aio => aio.InputFilePath))
      {
        if (!File.Exists(assemblyInputPath))
        {
          throw new FileNotFoundException($"Could not find provided input assembly file '{assemblyInputPath}'.", assemblyInputPath);
        }
      }

      Log($"{step++}. Loading assemblies...");

      // Convert all paths into AssemblyInfo objects.
      var allAssemblies = new HashSet<AssemblyInfo>();

      // File locking issues in Mono.Cecil are a real pain in the ass so let's create a working directory of files to process, then copy them back when we're done.
      var tempFilePathToInputOutputFilePairMap = new Dictionary<string, InputOutputFilePair>();
      var tempPath = Path.Combine(Path.GetTempPath(), "StrongNameSigner-" + Guid.NewGuid().ToString());
      Directory.CreateDirectory(tempPath);

      foreach (var inputOutputFilePair in assemblyInputOutputPaths)
      {
        try
        {
          var tempFilePath = Path.Combine(tempPath, $"{Path.GetFileNameWithoutExtension(inputOutputFilePair.InputFilePath)}.{Guid.NewGuid()}{Path.GetExtension(inputOutputFilePair.InputFilePath)}");
          File.Copy(inputOutputFilePair.InputFilePath, tempFilePath, true);

          if (inputOutputFilePair.HasSymbols)
          {
            File.Copy(inputOutputFilePair.InputPdbPath, Path.ChangeExtension(tempFilePath, ".pdb"), true);
          }

          tempFilePathToInputOutputFilePairMap.Add(tempFilePath, inputOutputFilePair);

          allAssemblies.Add(new AssemblyInfo(tempFilePath, probingPaths));
        }
        catch (BadImageFormatException ex)
        {
          Log($"   Unsupported assembly '{inputOutputFilePair.InputFilePath}': {ex.Message}");
          hasErrors = true;
        }
        catch (Exception ex)
        {
          Log($"   Failed to load assembly '{inputOutputFilePair.InputFilePath}': {ex.Message}");
          hasErrors = true;
        }
      }

      Log($"{step++}. Checking assembly references...");

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

        Log($"{step++}. Strong-name unsigned assemblies...");

        // Strong-name sign all the unsigned assemblies.
        foreach (var assembly in assembliesToProcess.Where(a => !a.IsSigned))
        {
          Log($"   Signing assembly '{tempFilePathToInputOutputFilePairMap[assembly.FilePath].InputFilePath}'.");

          var name = assembly.Definition.Name;
          name.HashAlgorithm = AssemblyHashAlgorithm.SHA1;
          name.PublicKey = publicKey;
          name.HasPublicKey = true;
          name.Attributes |= AssemblyAttributes.PublicKey;
        }

        Log($"{step++}. Fix InternalVisibleToAttribute references...");

        // Fix InternalVisibleToAttribute.
        foreach (var assembly in allAssemblies)
        {
          foreach (var internalVisibleArg in assembly.Definition.CustomAttributes
            .Where(attr => attr.AttributeType.FullName == typeof(InternalsVisibleToAttribute).FullName && attr.HasConstructorArguments)
            .Select(attr => new { Attribute = attr, Arguments = attr.ConstructorArguments })
            .ToList())
          {
            var constructorArguments = internalVisibleArg.Arguments;
            var argument = constructorArguments[0];
            if (argument.Type == assembly.Definition.MainModule.TypeSystem.String)
            {
              var originalAssemblyName = (string)argument.Value;
              var signedAssembly = assembliesToProcess.FirstOrDefault(a => a.Definition.Name.Name == originalAssemblyName);

              if (signedAssembly is not null)
              {
                Log($"   Fixing {signedAssembly.Definition.Name.Name} friend reference in assembly '{tempFilePathToInputOutputFilePairMap[assembly.FilePath].InputFilePath}'.");

                var assemblyName = signedAssembly.Definition.Name.Name + ", PublicKey=" + BitConverter.ToString(signedAssembly.Definition.Name.PublicKey).Replace("-", string.Empty);
                var updatedArgument = new CustomAttributeArgument(argument.Type, assemblyName);

                constructorArguments.Clear();
                constructorArguments.Add(updatedArgument);
              }
              else if (!originalAssemblyName.Contains("PublicKey"))
              {
                Log($"   Removing invalid friend reference from assembly '{assembly.FilePath}'.");
                assembly.Definition.CustomAttributes.Remove(internalVisibleArg.Attribute);
              }
            }
          }
        }

        Log($"{step++}. Fix CustomAttributes with Type references...");

        // Fix CustomAttributes with Type references.
        FixCustomAttributes(allAssemblies, tempFilePathToInputOutputFilePairMap, ref hasErrors);

        Log($"{step++}. Fix BAML references...");

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

                var embeddedResource = (EmbeddedResource)resource;
                var modifyResource = false;

                using var memoryStream = new MemoryStream();
                using var writer = new ResourceWriter(memoryStream);

                using var resourceStream = embeddedResource.GetResourceStream();
                using var reader = new ResourceReader(resourceStream);

                foreach (var entry in reader.OfType<DictionaryEntry>().ToArray())
                {
                  var resourceName = entry.Key.ToString();

                  if (resourceName.EndsWith(".baml", StringComparison.InvariantCulture) && entry.Value is Stream bamlStream)
                  {
                    var br = new BinaryReader(bamlStream);
                    var dataBytes = br.ReadBytes((int)br.BaseStream.Length);

                    var charList = dataBytes.Select(b => (char)b).ToList();
                    var data = new string([.. charList]);
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

                  var newEmbedded = new EmbeddedResource(resource.Name, resource.Attributes, array);
                  resources.Insert(resIndex, newEmbedded);
                }
              }
            }
          }
        }

        Log($"{step++}. Save assembly changes...");

        // Write all updated assemblies.
        foreach (var assembly in assembliesToProcess.Where(a => !a.Definition.Name.IsRetargetable))
        {
          var inputOutputFilePair = tempFilePathToInputOutputFilePairMap[assembly.FilePath];

          if (inputOutputFilePair.IsSameFile)
          {
            try
            {
              File.Copy(inputOutputFilePair.InputFilePath, inputOutputFilePair.BackupAssemblyPath, true);

              if (inputOutputFilePair.HasSymbols)
              {
                File.Copy(inputOutputFilePair.InputPdbPath, inputOutputFilePair.BackupPdbPath, true);
              }
            }
            catch (IOException ioex)
            {
              Log($"   Failed to backup assembly '{inputOutputFilePair.InputFilePath}': {ioex.Message}");
              hasErrors = true;
            }
          }

          Log($"   Saving changes to assembly '{inputOutputFilePair.OutputFilePath}'.");

          try
          {
            assembly.Save(inputOutputFilePair.OutputFilePath, keyPair);
          }
          catch (Exception ex)
          {
            Log($"   Failed to save assembly '{inputOutputFilePair.OutputFilePath}': {ex.Message}");
            hasErrors = true;

            if (inputOutputFilePair.IsSameFile)
            {
              try
              {
                // Restore the backup that would have been created above.
                File.Copy(inputOutputFilePair.BackupAssemblyPath, inputOutputFilePair.InputFilePath, true);
                File.Delete(inputOutputFilePair.BackupAssemblyPath);
              }
              catch (IOException ioex)
              {
                Log($"   Failed to restore assembly '{inputOutputFilePair.InputFilePath} from backup '{inputOutputFilePair.BackupAssemblyPath}': {ioex.Message}");
              }

              if (inputOutputFilePair.HasSymbols)
              {
                try
                {
                  File.Copy(inputOutputFilePair.BackupPdbPath, inputOutputFilePair.InputPdbPath, true);
                  File.Delete(inputOutputFilePair.BackupPdbPath);
                }
                catch (IOException ioex)
                {
                  Log($"   Failed to restore PDB '{inputOutputFilePair.InputPdbPath} from backup '{inputOutputFilePair.BackupPdbPath}': {ioex.Message}");
                }
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
        Log($"{step++}. Cleanup...");

        tempFilePathToInputOutputFilePairMap.Clear();

        foreach (var assembly in allAssemblies)
        {
          assembly.Dispose();
        }

        try
        {
          Directory.Delete(tempPath, true);
        }
        catch (IOException ioex)
        {
          Log($"   Failed to delete temp working directory '{tempPath}': {ioex.Message}");
          hasErrors = true;
        }
      }

      return !hasErrors;
    }

    private static void FixCustomAttributes(HashSet<AssemblyInfo> allAssemblies, Dictionary<string, InputOutputFilePair> tempFilePathToInputOutputFilePairMap, ref bool hasErrors)
    {
      var assembliesByName = new Dictionary<string, List<AssemblyInfo>>();
      foreach (var assembly in allAssemblies)
      {
        if (!assembliesByName.TryGetValue(assembly.Definition.Name.Name, out var value))
        {
          assembliesByName.Add(assembly.Definition.Name.Name, [assembly]);
        }
        else
        {
          value.Add(assembly);
        }
      }

      foreach (var assembly in allAssemblies)
      {
        var types = assembly.Definition.Modules
          .SelectMany(m => m.GetTypes())
          .ToList();

        var methods = types
          .SelectMany(t => t.Methods)
          .ToList();

        // Assembly-level custom attributes
        FixAttributes(assembly.Definition.CustomAttributes, assembly, tempFilePathToInputOutputFilePairMap, assembliesByName, ref hasErrors);

        // Module-level custom attributes
        FixAttributes(assembly.Definition.Modules
          .Where(m => m.HasCustomAttributes)
          .SelectMany(m => m.CustomAttributes), assembly, tempFilePathToInputOutputFilePairMap, assembliesByName, ref hasErrors);

        // Type-level custom attributes
        FixAttributes(types
          .Where(t => t.HasCustomAttributes)
          .SelectMany(t => t.CustomAttributes), assembly, tempFilePathToInputOutputFilePairMap, assembliesByName, ref hasErrors);

        // Method-level custom attributes
        FixAttributes(methods
          .Where(m => m.HasCustomAttributes)
          .SelectMany(m => m.CustomAttributes), assembly, tempFilePathToInputOutputFilePairMap, assembliesByName, ref hasErrors);

        // Parameter-level custom attributes
        FixAttributes(methods
          .SelectMany(m => m.Parameters)
          .Where(p => p.HasCustomAttributes)
          .SelectMany(p => p.CustomAttributes), assembly, tempFilePathToInputOutputFilePairMap, assembliesByName, ref hasErrors);

        // Method return type custom attributes
        FixAttributes(methods
          .Where(m => m.MethodReturnType.HasCustomAttributes)
          .SelectMany(m => m.MethodReturnType.CustomAttributes), assembly, tempFilePathToInputOutputFilePairMap, assembliesByName, ref hasErrors);

        // Field-level custom attributes
        FixAttributes(types
          .SelectMany(t => t.Fields)
          .Where(f => f.HasCustomAttributes)
          .SelectMany(f => f.CustomAttributes), assembly, tempFilePathToInputOutputFilePairMap, assembliesByName, ref hasErrors);

        // Event-level custom attributes
        FixAttributes(types
          .SelectMany(t => t.Events)
          .Where(e => e.HasCustomAttributes)
          .SelectMany(e => e.CustomAttributes), assembly, tempFilePathToInputOutputFilePairMap, assembliesByName, ref hasErrors);

        // Property-level custom attributes
        FixAttributes(types
          .SelectMany(t => t.Properties)
          .Where(p => p.HasCustomAttributes)
          .SelectMany(p => p.CustomAttributes), assembly, tempFilePathToInputOutputFilePairMap, assembliesByName, ref hasErrors);
      }
    }

    private static void FixAttributes(
        IEnumerable<CustomAttribute> customAttributes,
        AssemblyInfo assembly,
        Dictionary<string, InputOutputFilePair> tempFilePathToInputOutputFilePairMap,
        Dictionary<string, List<AssemblyInfo>> assembliesByName,
        ref bool hasErrors)
    {
      foreach (var customAttribute in customAttributes)
      {
        try
        {
          if (customAttribute.HasConstructorArguments)
          {
            foreach (var argument in customAttribute.ConstructorArguments.ToArray())
            {
              if (argument.Type.FullName == "System.Type" && argument.Value is TypeReference typeRef && assembliesByName.TryGetValue(typeRef.Scope.Name, out var value))
              {
                foreach (var signedAssembly in value.Select(sa => sa.Definition))
                {
                  Log($"   Fixing {signedAssembly.Name.Name} reference in CustomAttribute in assembly '{tempFilePathToInputOutputFilePairMap[assembly.FilePath].InputFilePath}'.");

                  signedAssembly.MainModule.GetType(typeRef.FullName);

                  // Import the type reference into the current module, so it gets the correct scope.
                  // Without this import, the type reference in the ILASM will only point to the type (like "Brutal.Dev.StrongNameSigner.TestAssembly.A.A")
                  // instead of the full assembly-qualified name (like "Brutal.Dev.StrongNameSigner.TestAssembly.A.A, Brutal.Dev.StrongNameSigner.TestAssembly.A, Version=1.0.0.0, PublicKeyToken=...").
                  var importedTypeRef = assembly.Definition.MainModule.ImportReference(signedAssembly.MainModule.GetType(typeRef.FullName));

                  var updatedArgument = new CustomAttributeArgument(argument.Type, importedTypeRef);

                  var idx = customAttribute.ConstructorArguments.IndexOf(argument);
                  customAttribute.ConstructorArguments.RemoveAt(idx);
                  customAttribute.ConstructorArguments.Insert(idx, updatedArgument);
                }
              }
            }
          }
        }
        catch (AssemblyResolutionException ex)
        {
          Log($"   Failed to check custom attribute '{customAttribute.AttributeType.FullName}' in assembly '{tempFilePathToInputOutputFilePairMap[assembly.FilePath].InputFilePath}': {ex.Message}");
          hasErrors = true;
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
        if (keyPairCache is not null)
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
      elementsToReplace = [.. elementsToReplace.OrderBy(x => x.Index)];

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

#pragma warning disable S127 // "for" loop stop conditions should be invariant
          i += match.Length - 1;
#pragma warning restore S127 // "for" loop stop conditions should be invariant

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
      return [.. list];
    }
  }
}
