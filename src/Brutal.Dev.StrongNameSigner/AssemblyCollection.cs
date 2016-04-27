using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Brutal.Dev.StrongNameSigner
{
  public class AssemblyCollection
  {
    public List<AssemblyHolder> Assemblies { get; } = new List<AssemblyHolder>();

    private string[] _ProbingPaths;
    private readonly Action<string, LogLevel, ConsoleColor?> _Log;

    public AssemblyCollection(Action<string, LogLevel, ConsoleColor?> log)
    {
      Debug.Assert(log != null, "log != null");
      _Log = log;
    }

    public string[] ProbingPaths => _ProbingPaths ??
                                    (_ProbingPaths =
                                      Enumerable.Distinct<string>(Assemblies.Select(file => Path.GetDirectoryName(file.SourcePath))).ToArray());

    public void Add(string sourceDirectory, string targetDirectory, string relativePath)
    {
      _ProbingPaths = null;
      Assemblies.Add(new AssemblyHolder(sourceDirectory, targetDirectory, relativePath, () => _ProbingPaths, _Log));
    }

    public void AddFromPath(string sourceDirectory, string targetDirectory)
    {
      foreach (var file in Directory.GetFiles(sourceDirectory, "*.*", SearchOption.AllDirectories)
        .Where(f => f != null)
        .Where(f => Path.GetExtension(f).Equals(".exe", StringComparison.OrdinalIgnoreCase) ||
                    Path.GetExtension(f).Equals(".dll", StringComparison.OrdinalIgnoreCase)))
      {
        Add(sourceDirectory, targetDirectory, file.Remove(0, sourceDirectory.Length).TrimStart('\\'));
      }
    }
    public void AddFromFile(string sourceFile, string targetDirectory)
    {
      if(!File.Exists(sourceFile))
        throw new FileNotFoundException();

      var dir = Path.GetDirectoryName(sourceFile);
      Add(dir, targetDirectory, Path.GetFileName(sourceFile));
    }

    public Stats Sign(string keyFile, string password, Action<string, LogLevel, ConsoleColor?> log)
    {
      var resignedAssemblyPipes = Assemblies
        .Where(assembly => assembly.SignIfNeeded(keyFile, password))
        .ToList();

      int fixedAssemblies = 0;

      foreach (var assembly in Assemblies)
      {
        foreach (var resignedAssembly in resignedAssemblyPipes.Where(p=>p.TargetPath!=assembly.TargetPath) )
        {
          if( assembly.FixReferenceTo(resignedAssembly.Assembly))
            fixedAssemblies++;
        }
      }

      foreach (var resignedAssemblyPipe in resignedAssemblyPipes)
      {
        resignedAssemblyPipe.RemoveInvalidFriendReferences();
      }

      foreach (var a in Assemblies)
      {
        if(a.Dirty)
          a.Write(keyFile, password);
      }

      return new Stats()
      {
        NumberOfSignedFiles = resignedAssemblyPipes.Count,
        NumberOfFixedReferences = fixedAssemblies
      };

    }
  }
}