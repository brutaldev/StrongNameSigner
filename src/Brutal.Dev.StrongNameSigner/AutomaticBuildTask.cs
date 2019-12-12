using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Brutal.Dev.StrongNameSigner
{
  public class AutomaticBuildTask : Microsoft.Build.Utilities.Task
  {
    [Required]
    public ITaskItem[] References { get; set; }

    [Required]
    public ITaskItem OutputPath { get; set; }

    public ITaskItem[] CopyLocalPaths { get; set; }

    [Output]
    public ITaskItem[] SignedAssembliesToReference { get; set; }

    [Output]
    public ITaskItem[] NewCopyLocalFiles { get; set; }

    public override bool Execute()
    {
      var timer = Stopwatch.StartNew();
      bool chagesMade = false;

      try
      {
        Log.LogMessage(MessageImportance.High, "-- Starting Brutal Developer .NET Assembly Strong-Name Signer Task --");

        if (References == null || References.Length == 0)
        {
          return true;
        }

        if (OutputPath == null || string.IsNullOrEmpty(OutputPath.ItemSpec))
        {
          Log.LogError("Task parameter 'OutputPath' not provided.");
          return false;
        }
        
        SignedAssembliesToReference = new ITaskItem[References.Length];

        string signedAssemblyFolder = Path.GetFullPath(Path.Combine(OutputPath.ItemSpec, "StrongNameSigner"));
        if (!Directory.Exists(signedAssemblyFolder))
        {
          Directory.CreateDirectory(signedAssemblyFolder);
        }

        string snkFilePath = Path.Combine(Path.GetDirectoryName(GetType().Assembly.Location), "StrongNameSigner.snk");
        if (!File.Exists(snkFilePath))
        {
          File.WriteAllBytes(snkFilePath, SigningHelper.GenerateStrongNameKeyPair());
        }

        Log.LogMessage(MessageImportance.Normal, "Signed Assembly Directory: {0}", signedAssemblyFolder);
        Log.LogMessage(MessageImportance.Normal, "SNK File Path: {0}", snkFilePath);

        var updatedReferencePaths = new Dictionary<string, string>();
        var processedAssemblyPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var signedAssemblyPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var probingPaths = References.Select(r => Path.GetDirectoryName(r.ItemSpec)).Distinct().ToArray();

        for (int i = 0; i < References.Length; i++)
        {
          var ret = new TaskItem(References[i]);

          if (References[i].ItemSpec.IndexOf("\\Reference Assemblies\\Microsoft\\", StringComparison.OrdinalIgnoreCase) > -1 ||
              References[i].ItemSpec.IndexOf("\\Microsoft.NET\\Framework\\", StringComparison.OrdinalIgnoreCase) > -1 ||
              References[i].ItemSpec.IndexOf("\\netstandard.library\\", StringComparison.OrdinalIgnoreCase) > -1 ||
              References[i].ItemSpec.IndexOf("\\dotnet\\sdk\\", StringComparison.OrdinalIgnoreCase) > -1 ||
              References[i].ItemSpec.IndexOf("\\dotnet\\packs\\", StringComparison.OrdinalIgnoreCase) > -1 ||
              References[i].ItemSpec.IndexOf("\\dotnet\\shared\\Microsoft.", StringComparison.OrdinalIgnoreCase) > -1)
          {
            // Don't attempt to sign and process framework assemblies, they should be signed already, just add them to be copied.
            ret.ItemSpec = References[i].ItemSpec;
            SignedAssembliesToReference[i] = ret;

            if (SignedAssembliesToReference[i].ItemSpec != References[i].ItemSpec)
            {
              updatedReferencePaths[References[i].ItemSpec] = SignedAssembliesToReference[i].ItemSpec;
            }
          }
          else
          {
            var signedAssembly = SigningHelper.GetAssemblyInfo(References[i].ItemSpec);

            // Check if it is currently signed.
            if (!signedAssembly.IsSigned)
            {
              signedAssembly = SignSingleAssembly(References[i].ItemSpec, snkFilePath, signedAssemblyFolder, probingPaths);
              chagesMade = true;
            }

            if (signedAssembly.IsSigned)
            {
              signedAssemblyPaths.Add(signedAssembly.FilePath);
              processedAssemblyPaths.Add(signedAssembly.FilePath);
              ret.ItemSpec = signedAssembly.FilePath;
            }
            else
            {
              processedAssemblyPaths.Add(References[i].ItemSpec);
            }

            SignedAssembliesToReference[i] = ret;

            if (SignedAssembliesToReference[i].ItemSpec != References[i].ItemSpec)
            {
              updatedReferencePaths[References[i].ItemSpec] = SignedAssembliesToReference[i].ItemSpec;
            }
          }
        }

        if (chagesMade)
        {
          var referencesToFix = new HashSet<string>(processedAssemblyPaths, StringComparer.OrdinalIgnoreCase);
          foreach (var filePath in processedAssemblyPaths)
          {
            // Go through all the references excluding the file we are working on.
            foreach (var referencePath in referencesToFix.Where(r => !r.Equals(filePath)))
            {
              FixSingleAssemblyReference(filePath, referencePath, snkFilePath, probingPaths);
            }
          }

          // Remove all InternalsVisibleTo attributes without public keys from the processed assemblies. Signed assemblies cannot have unsigned friend assemblies.
          foreach (var filePath in signedAssemblyPaths)
          {
            RemoveInvalidFriendAssemblyReferences(filePath, snkFilePath, probingPaths);
          }
        }

        if (CopyLocalPaths != null)
        {
          NewCopyLocalFiles = new ITaskItem[CopyLocalPaths.Length];
          for (int i = 0; i < CopyLocalPaths.Length; i++)
          {
            string updatedPath;
            if (updatedReferencePaths.TryGetValue(CopyLocalPaths[i].ItemSpec, out updatedPath))
            {
              NewCopyLocalFiles[i] = new TaskItem(CopyLocalPaths[i]);
              NewCopyLocalFiles[i].ItemSpec = updatedPath;
            }
            else
            {
              NewCopyLocalFiles[i] = CopyLocalPaths[i];
            }
          }
        }

        Log.LogMessage(MessageImportance.High, "-- Finished Brutal Developer .NET Assembly Strong-Name Signer Task -- {0}", timer.Elapsed);

        return true;
      }
      catch (Exception ex)
      {
        Log.LogErrorFromException(ex, true, true, null);
      }

      return false;
    }

    private AssemblyInfo SignSingleAssembly(string assemblyPath, string keyPath, string outputDirectory, params string[] probingPaths)
    {
      try
      {
        Log.LogMessage(MessageImportance.Low, string.Empty);
        Log.LogMessage(MessageImportance.Low, "Strong-name signing '{0}'...", assemblyPath);

        var oldInfo = SigningHelper.GetAssemblyInfo(assemblyPath);
        var newInfo = SigningHelper.SignAssembly(assemblyPath, keyPath, outputDirectory, null, probingPaths);

        if (!oldInfo.IsSigned && newInfo.IsSigned)
        {
          Log.LogMessage(MessageImportance.High, "Strong-name signature applied to '{0}' successfully.", newInfo.FilePath);

          return newInfo;
        }
        else
        {
          Log.LogMessage(MessageImportance.Low, "Strong-name signature already applied to '{0}'...", assemblyPath);
        }
      }
      catch (BadImageFormatException ex)
      {
        Log.LogWarningFromException(ex, true);
      }
      catch (IOException ex)
      {
        Log.LogWarningFromException(ex, false);
      }
      catch (Exception ex)
      {
        Log.LogErrorFromException(ex, true, true, assemblyPath);
      }

      return null;
    }

    private void FixSingleAssemblyReference(string assemblyPath, string referencePath, string keyFile, params string[] probingPaths)
    {
      try
      {
        Log.LogMessage(MessageImportance.Low, string.Empty);
        Log.LogMessage(MessageImportance.Low, "Fixing references to '{1}' in '{0}'...", assemblyPath, referencePath);

        if (SigningHelper.FixAssemblyReference(assemblyPath, referencePath, keyFile, null, probingPaths))
        {
          Log.LogMessage(MessageImportance.High, "References to '{1}' in '{0}' were fixed successfully.", assemblyPath, referencePath);
        }
        else
        {
          Log.LogMessage(MessageImportance.Low, "No assembly references to fix in '{0}'...", assemblyPath);
        }
      }
      catch (BadImageFormatException ex)
      {
        Log.LogWarningFromException(ex, true);
      }
      catch (IOException ex)
      {
        Log.LogWarningFromException(ex, false);
      }
      catch (Exception ex)
      {
        Log.LogErrorFromException(ex, true, true, referencePath);
      }
    }

    private void RemoveInvalidFriendAssemblyReferences(string assemblyPath, string keyFile, params string[] probingPaths)
    {
      try
      {
        Log.LogMessage(MessageImportance.Low, string.Empty);
        Log.LogMessage(MessageImportance.Low, "Removing invalid friend references from '{0}'...", assemblyPath);

        if (SigningHelper.RemoveInvalidFriendAssemblies(assemblyPath, keyFile, null, probingPaths))
        {
          Log.LogMessage(MessageImportance.High, "Invalid friend assemblies removed successfully from '{0}'.", assemblyPath);
        }
        else
        {
          Log.LogMessage(MessageImportance.Low, "No friend references to fix in '{0}'...", assemblyPath);
        }
      }
      catch (BadImageFormatException ex)
      {
        Log.LogWarningFromException(ex, true);
      }
      catch (IOException ex)
      {
        Log.LogWarningFromException(ex, false);
      }
      catch (Exception ex)
      { 
        Log.LogErrorFromException(ex, true, true, assemblyPath);
      }
    }
  }
}
