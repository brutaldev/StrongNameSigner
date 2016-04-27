using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using Mono.Cecil;

namespace Brutal.Dev.StrongNameSigner
{
  public class AssemblyHolder
  {
    private readonly Action<string, LogLevel, ConsoleColor?> _Log;
    public Func<string[]> ProbingPathResolver { get; }

    public bool Dirty { get; set; } = false;

    public AssemblyHolder(string sourceDirectory, string targetDirectory, string relativePath, Func<string[]> probingPathResolver, Action<string, LogLevel, ConsoleColor? > log )
    {
      if (string.IsNullOrWhiteSpace(sourceDirectory))
        throw new ArgumentException("Value cannot be null or whitespace.", nameof(sourceDirectory));
      if (string.IsNullOrWhiteSpace(targetDirectory))
        throw new ArgumentException("Value cannot be null or whitespace.", nameof(targetDirectory));
      if (string.IsNullOrWhiteSpace(relativePath))
        throw new ArgumentException("Value cannot be null or whitespace.", nameof(relativePath));

      if(!Directory.Exists(sourceDirectory))
        throw new ArgumentException($"Source directory '{sourceDirectory}' does not exist", nameof(relativePath));

      this._Log = log;
      ProbingPathResolver = probingPathResolver;
      SourceDirectory = sourceDirectory;
      TargetDirectory = targetDirectory;
      RelativePath = relativePath;

      Copy();
      ResetAssemblyLoad(TargetPath);
    }

    public bool FixReferenceTo(AssemblyDefinition upstream)
    {
      try
      {
        _Log(null, LogLevel.Verbose, null);
        _Log($"Fixing references to '{upstream.FullName}' in '{this.Assembly.FullName}'...", LogLevel.Verbose, null);

        if (SigningHelper.FixAssemblyReference(this.Assembly, upstream))
        {
          _Log($"References to '{upstream.FullName}' in '{this.Assembly.FullName}' were fixed successfully.", LogLevel.Changes, ConsoleColor.Green);

          Dirty = true;

          return true;
        }
        else
        {
          _Log("No assembly references to fix...", LogLevel.Verbose,null);
        }
      }
      catch (BadImageFormatException bife)
      {
        _Log($"Warning: {bife.Message}", LogLevel.Silent, ConsoleColor.Yellow);
      }
      catch (Exception ex)
      {
        _Log($"Error: {ex.Message}", LogLevel.Silent, ConsoleColor.Red);
      }

      return false;
    }

    /// <summary>
    /// If the assembly is unsigned then sign it with the key. Save it to
    /// the target path and reload it.
    /// </summary>
    /// <param name="keyPath"></param>
    /// <param name="password"></param>
    /// <returns></returns>
    public bool SignIfNeeded(string keyPath, string password)
    {
      try
      {
        _Log(null, LogLevel.Verbose, null);
        if (!Info.IsSigned)
        {
          _Log($"Strong-name signing '{TargetPath}'...", LogLevel.Verbose, null);
          Write(keyPath, password);
          _Log($"'{Info.FilePath}' was strong-name signed successfully.", LogLevel.Changes, ConsoleColor.Green);
          return true;
        }
        else
        {
          _Log("Already strong-name signed...", LogLevel.Verbose, null);
          return false;
        }
      }
      catch (BadImageFormatException bife)
      {
        _Log($"Warning: {bife.Message}", LogLevel.Silent, ConsoleColor.Yellow);
        return false;
      }
      catch (Exception ex)
      {
        _Log($"Error: {ex.Message}", LogLevel.Silent, ConsoleColor.Red);
        throw;
      }
    }

    private AssemblyDefinition Load(string path)
    {
      try
      {
        return AssemblyDefinition.ReadAssembly(path, SigningHelper.GetReadParameters(SourcePath, ProbingPathResolver()));
      }
      catch (BadImageFormatException e)
      {
        // Probably just a dll we don't care about. Get Lucky!!
        Console.WriteLine(e);
        return null;
      }
    }

    private void Copy()
    {
      if (SourcePath == TargetPath)
        return;

      _Log($"Copying {SourcePath} to {TargetPath} ",LogLevel.Verbose, null);
      var dir = Path.GetDirectoryName(TargetPath);
      if (!Directory.Exists(dir))
        Directory.CreateDirectory(dir);
      File.Copy(SourcePath, TargetPath, true);
    }


    public void Write(string keyFile, string password)
    {
      _Log($"Writing {TargetPath} with keyfile {keyFile} and password {password}", LogLevel.Verbose, null);

      if (!Directory.Exists(TargetDirectory))
      {
        Directory.CreateDirectory(TargetDirectory);
      }

      SigningHelper.FileWriteTransaction(TargetPath, () =>
      {
        Assembly.Write
          (TargetPath
            , new WriterParameters
            {
              StrongNameKeyPair = SigningHelper.GetStrongNameKeyPair(keyFile, password)
              ,
              WriteSymbols = true
            });

        Dirty = false;

      });

      // Any further reads will be from the target path
      ResetAssemblyLoad(TargetPath);
    }
    private void ResetAssemblyLoad(string path)
    {
      _Assembly = new Lazy<AssemblyDefinition>(()=>Load(path));
    }


    private Lazy<AssemblyDefinition> _Assembly;

    public AssemblyInfo Info => SigningHelper.GetAssemblyInfo(TargetPath, Assembly);


    public AssemblyDefinition Assembly => _Assembly.Value;

    public string SourcePath => Path.Combine(SourceDirectory, RelativePath);
    public string TargetPath => Path.Combine(TargetDirectory, RelativePath);

    public string SourceDirectory { get; }
    public string TargetDirectory { get; }
    public string RelativePath { get;  }

    /// <summary>
    /// Removes any friend assembly references (InternalsVisibleTo attributes) that do not have public keys.
    /// </summary>
    public bool RemoveInvalidFriendReferences()
    {
      var ivtAttributes = Assembly.CustomAttributes.Where(attr => attr.AttributeType.FullName == typeof(InternalsVisibleToAttribute).FullName).ToList();
      var fixApplied = false;

      foreach (var friendReference in ivtAttributes)
      {
        // Find any without a public key defined.
        if (!friendReference.HasConstructorArguments ||
            !friendReference.ConstructorArguments.Any(
              ca => ca.Value != null && ca.Value.ToString().IndexOf("PublicKey=", StringComparison.Ordinal) == -1))
          continue;
        Assembly.CustomAttributes.Remove(friendReference);
        Dirty = true;
        fixApplied = true;
      }
      return fixApplied;
    }
  }
}