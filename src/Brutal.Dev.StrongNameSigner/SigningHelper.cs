using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Mono.Cecil;

namespace Brutal.Dev.StrongNameSigner
{
  /// <summary>
  /// Static helper class for easily getting assembly information and strong-name signing .NET assemblies.
  /// </summary>
  public static class SigningHelper
  {
    private static readonly ConcurrentDictionary<string, KeyValuePair<string, AssemblyInfo>> AssemblyInfoCache = new ConcurrentDictionary<string, KeyValuePair<string, AssemblyInfo>>(StringComparer.OrdinalIgnoreCase);

    private static byte[] keyPairCache = null;

    /// <summary>
    /// Generates a 1024 bit the strong-name key pair that can be written to an SNK file.
    /// </summary>
    /// <returns>A strong-name key pair array.</returns>
    public static byte[] GenerateStrongNameKeyPair()
    {
#pragma warning disable S4426 // Cryptographic keys should not be too short
      using (var provider = new RSACryptoServiceProvider(1024, new CspParameters() { KeyNumber = 2 }))
#pragma warning restore S4426 // Cryptographic keys should not be too short
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
      using (OutputFileManager outputFileMgr = new OutputFileManager(assemblyPath, outputFile))
      {
        // Get the assembly info and go from there.

        AssemblyInfo info = GetAssemblyInfo(assemblyPath);

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
          ad.Write(outputFileMgr.IntermediateAssemblyPath, new WriterParameters() { StrongNameKeyPair = GetStrongNameKeyPair(keyPath, keyFilePassword), WriteSymbols = outputFileMgr.HasSymbols });
        }

        AssemblyInfoCache.TryRemove(assemblyPath, out KeyValuePair<string, AssemblyInfo> _);

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
        throw new ArgumentNullException("assemblyPath");
      }

      // Make sure the file actually exists.
      if (!File.Exists(assemblyPath))
      {
        throw new FileNotFoundException("Could not find provided assembly file.", assemblyPath);
      }

      var a = new KeyValuePair<string, AssemblyInfo>(null, null);
      if (AssemblyInfoCache.ContainsKey(assemblyPath) && AssemblyInfoCache.TryGetValue(assemblyPath, out a) &&
          !GetFileMD5Hash(assemblyPath).Equals(a.Key, StringComparison.OrdinalIgnoreCase))  // Check if the file contents have changed.
      {
        AssemblyInfoCache.TryRemove(assemblyPath, out a);

        // Overwrite with a blank version.
        a = new KeyValuePair<string, AssemblyInfo>(null, null);
      }

      if (a.Value == null)
      {
        using (var definition = AssemblyDefinition.ReadAssembly(assemblyPath, GetReadParameters(assemblyPath, probingPaths)))
        {
          var info = new AssemblyInfo()
          {
            FilePath = Path.GetFullPath(assemblyPath),
            DotNetVersion = GetDotNetVersion(definition.MainModule.Runtime),
            IsSigned = definition.MainModule.Attributes.HasFlag(ModuleAttributes.StrongNameSigned),
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

      using (var aMgr = new OutputFileManager(assemblyPath, assemblyPath))
      using (var bMgr = new OutputFileManager(referenceAssemblyPath, referenceAssemblyPath))
      {
        using (var a = AssemblyDefinition.ReadAssembly(assemblyPath, GetReadParameters(assemblyPath, probingPaths)))
        using (var b = AssemblyDefinition.ReadAssembly(referenceAssemblyPath, GetReadParameters(referenceAssemblyPath, probingPaths)))
        {
          var assemblyReference = a.MainModule.AssemblyReferences.FirstOrDefault(r => r.Name.Equals(b.Name.Name, StringComparison.OrdinalIgnoreCase));

          // Found a matching reference, let's set the public key token.
          if (assemblyReference != null && BitConverter.ToString(assemblyReference.PublicKeyToken) != BitConverter.ToString(b.Name.PublicKeyToken))
          {
            assemblyReference.PublicKeyToken = b.Name.PublicKeyToken ?? new byte[0];
            assemblyReference.Version = b.Name.Version;

            a.Write(aMgr.IntermediateAssemblyPath, new WriterParameters { StrongNameKeyPair = GetStrongNameKeyPair(keyPath, keyFilePassword), WriteSymbols = File.Exists(Path.ChangeExtension(assemblyPath, ".pdb")) });

            AssemblyInfoCache.TryRemove(assemblyPath, out KeyValuePair<string, AssemblyInfo> _);

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

            // Save and resign.
            b.Write(bMgr.IntermediateAssemblyPath, new WriterParameters { StrongNameKeyPair = GetStrongNameKeyPair(keyPath, keyFilePassword), WriteSymbols = File.Exists(Path.ChangeExtension(referenceAssemblyPath, ".pdb")) });

            AssemblyInfoCache.TryRemove(assemblyPath, out KeyValuePair<string, AssemblyInfo> _);

            fixApplied = true;
          }
        }

        aMgr.Commit();
        bMgr.Commit();
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
      using (var outFileMgr = new OutputFileManager(assemblyPath, assemblyPath))
      {
        using (var a = AssemblyDefinition.ReadAssembly(assemblyPath, GetReadParameters(assemblyPath, probingPaths)))
        {
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
            a.Write(outFileMgr.IntermediateAssemblyPath, new WriterParameters { StrongNameKeyPair = GetStrongNameKeyPair(keyPath, keyFilePassword), WriteSymbols = File.Exists(Path.ChangeExtension(assemblyPath, ".pdb")) });

            AssemblyInfoCache.TryRemove(assemblyPath, out KeyValuePair<string, AssemblyInfo> _);
          }
        }

        outFileMgr.Commit();
      }

      return fixApplied;
    }

    internal static StrongNameKeyPair GetStrongNameKeyPair(string keyPath, string keyFilePassword)
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

        ReaderParameters readParams = null;

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

    /// <summary>
    /// Utility class that assists in the handling of temporary files during assembly signing. It will create 
    /// a temporary directory to hold generated files during the signing process, and will move these to their
    /// final location upon calling <see cref="Commit"/>.  It also ensures that all temporary/intermediate files
    /// are deleted upon disposing the instance.
    /// </summary>
    /// <seealso cref="System.IDisposable" />
    private sealed class OutputFileManager : IDisposable
    {
      private string tempDir;

      public OutputFileManager(string sourceAssemblyPath, string targetAssemblyPath)
      {
        SourceAssemblyPath = Path.GetFullPath(sourceAssemblyPath);
        TargetAssemblyPath = Path.GetFullPath(targetAssemblyPath);
        IsInPlaceReplace = String.Equals(SourceAssemblyPath, TargetAssemblyPath, StringComparison.Ordinal);

        if (IsInPlaceReplace)
        {
          tempDir = Path.Combine(Path.GetDirectoryName(sourceAssemblyPath), $"StrongNamerTemp.{Process.GetCurrentProcess().Id}.{Path.GetRandomFileName()}");
          Directory.CreateDirectory(tempDir);
          IntermediateAssemblyPath = Path.Combine(tempDir, Path.GetFileName(sourceAssemblyPath));
        }
        else
        {
          IntermediateAssemblyPath = TargetAssemblyPath;
        }

        HasSymbols = File.Exists(Path.ChangeExtension(SourceAssemblyPath, ".pdb"));
      }

      ~OutputFileManager()
      {
        Dispose();
      }

      #region Properties

      private bool UseTemporaryDirectory => tempDir != null;

      /// <summary>
      /// Gets a value indicating whether the source assembly has a matching pdb file.
      /// </summary>
      public bool HasSymbols { get; }

      /// <summary>
      /// Indicates whether the SourceAssembyPath and the TargetAssemblyPath are equal.
      /// </summary>
      public bool IsInPlaceReplace { get; }

      /// <summary>
      /// Gets the path of the source assembly.
      /// </summary>
      public string SourceAssemblyPath { get; }

      /// <summary>
      /// Gets the path to the .pdb file of the source assembly. (Does not check for existance of the file)
      /// </summary>
      public string SourcePdbPath => Path.ChangeExtension(SourceAssemblyPath, ".pdb");

      /// <summary>
      /// Gets the intermediate path to which the new assembly should be written. This may be the same as <see cref="TargetAssemblyPath"/>,
      /// if it is different from <see cref="SourceAssemblyPath"/>. (This property is never equal to <see cref="SourceAssemblyPath"/>).
      /// </summary>
      public string IntermediateAssemblyPath { get; }

      /// <summary>
      /// Gets the intermediate path to which the new .pdb should be written. This may be the same as <see cref="TargetAssemblyPath"/>,
      /// if it is different from <see cref="SourceAssemblyPath"/>.
      /// </summary>
      public string IntermediatePdbPath => Path.ChangeExtension(IntermediateAssemblyPath, ".pdb");

      /// <summary>
      /// Gets the path to where the final output assembly should reside. This may be the same as <see cref="SourceAssemblyPath"/>.
      /// </summary>
      public string TargetAssemblyPath { get; }

      /// <summary>
      /// Gets the path to the .pdb file of the target assembly. (Does not check for existance of the file)
      /// </summary>
      public string TargetPdbPath => Path.ChangeExtension(TargetAssemblyPath, ".pdb");

      /// <summary>
      /// Gets the path to where a backup of the source assembly should be saved.
      /// </summary>
      public string BackupAssemblyPath => SourceAssemblyPath + ".unsigned";

      /// <summary>
      /// Gets the path to where a backup of the source .pdb file should be saved.
      /// </summary>
      public string BackupPdbPath => SourcePdbPath + ".unsigned";

      /// <summary>
      /// This property will be set to true after <see cref="CreateBackup"/> has been called.
      /// </summary>
      public bool HasBackup { get; private set; }

      #endregion

      /// <summary>
      /// Creates a backup of the input files by simply copying them to the <see cref="BackupAssemblyPath"/> and <see cref="BackupPdbPath"/>.
      /// </summary>
      public void CreateBackup()
      {
        CopyFile(SourceAssemblyPath, BackupAssemblyPath, false);
        if (HasSymbols)
          CopyFile(SourcePdbPath, BackupPdbPath, false);

        HasBackup = true;
      }

      /// <summary>
      /// Directly copies <see cref="SourceAssemblyPath"/> and <see cref="SourcePdbPath"/> to <see cref="TargetAssemblyPath"/> 
      /// and <see cref="TargetPdbPath"/> (if the source files exists).
      /// </summary>
      public void CopySourceToFinalOutput()
      {
        CopyFile(SourceAssemblyPath, TargetAssemblyPath, false);
        if (HasSymbols)
          CopyFile(SourcePdbPath, TargetPdbPath, false);
      }

      /// <summary>
      /// Moves the intermediate files to the target locations if a temporary directory was used during generation. 
      /// Otherwise this method does nothing.
      /// </summary>
      public void Commit()
      {
        if (UseTemporaryDirectory)
        {
          try
          {
            // Only move files if the target assembly to move actually was created.
            CopyFile(IntermediateAssemblyPath, TargetAssemblyPath, true);
            if (HasSymbols)
            {
              CopyFile(IntermediatePdbPath, TargetPdbPath, true);
            }
          }
          catch (Exception)
          {
            if (HasBackup)
            {
              // Restore backup
              CopyFile(BackupAssemblyPath, SourceAssemblyPath, true);
              CopyFile(BackupPdbPath, SourcePdbPath, true);
            }
            throw;
          }
        }
        else
        {
          // Nothing to commit if we didn't use a temporary directory.
        }
      }

      public void Dispose()
      {
        GC.SuppressFinalize(this);
        if (tempDir != null)
        {
          try
          {
            Directory.Delete(tempDir, true);
            tempDir = null;
          }
          catch
          {
            // Ignore errors when attempting to clean up temporary directory.
          }
        }
      }

      /// <summary>
      /// Copies or moves a single file if it exists. It will always overwrite the target if it exists (provided that
      /// the source file exists).
      /// </summary>
      /// <param name="source">The source file to copy/move if it exists.</param>
      /// <param name="target">The target file to write to.</param>
      /// <param name="move">if set to <see langword="true" /> the file is moved, otherwise it is copied.</param>
      private static void CopyFile(string source, string target, bool move)
      {
        if (File.Exists(source))
        {
          if (File.Exists(target))
          {
            File.SetAttributes(target, FileAttributes.Normal);
            File.Delete(target);
          }

          if (move)
          {
            File.Move(source, target);
          }
          else
          {
            File.Copy(source, target);
          }
        }
      }
    }
  }
}
