using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Brutal.Dev.StrongNameSigner
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
  public class AutomaticBuildTask : Microsoft.Build.Utilities.Task
  {
    [Required]
    public ITaskItem[] References { get; set; }

    public ITaskItem OutputPath { get; set; }

    public ITaskItem[] CopyLocalPaths { get; set; }

    public string KeyFile { get; set; }

    public string Password { get; set; }

    [Output]
    public ITaskItem[] SignedAssembliesToReference { get; set; }

    [Output]
    public ITaskItem[] NewCopyLocalFiles { get; set; }

    public override bool Execute()
    {
      var timer = Stopwatch.StartNew();

      try
      {
        SigningHelper.Log = message => Log.LogMessage(MessageImportance.High, message);

        Log.LogMessage(MessageImportance.High, "-- Starting Brutal Developer .NET Assembly Strong-Name Signer Task --");

        if (References == null || References.Length == 0)
        {
          return true;
        }

        if (!string.IsNullOrEmpty(KeyFile) && !File.Exists(KeyFile))
        {
          Log.LogError($"The Key File '{KeyFile}' does not exist.");
          return false;
        }

        SignedAssembliesToReference = new ITaskItem[References.Length];

        var updatedReferencePaths = new Dictionary<string, string>();
        var assembliesToSign = new HashSet<string>();
        var probingPaths = References.Select(r => Path.GetDirectoryName(r.ItemSpec)).Distinct().ToArray();

        for (int i = 0; i < References.Length; i++)
        {
          SignedAssembliesToReference[i] = new TaskItem(References[i]) { ItemSpec = References[i].ItemSpec };

          if (SignedAssembliesToReference[i].ItemSpec != References[i].ItemSpec)
          {
            updatedReferencePaths[References[i].ItemSpec] = SignedAssembliesToReference[i].ItemSpec;
          }

          var sep = Path.DirectorySeparatorChar;
          var nugetPackagesEnvVariable = Environment.GetEnvironmentVariable("NUGET_PACKAGES");
          if (string.IsNullOrEmpty(nugetPackagesEnvVariable))
          {
            // Just use the default check we already use.
            nugetPackagesEnvVariable = $"{sep}.nuget{sep}packages{sep}";
          }

          if (References[i].ItemSpec.IndexOf($"{sep}Reference Assemblies{sep}Microsoft{sep}", StringComparison.OrdinalIgnoreCase) == -1 &&
              References[i].ItemSpec.IndexOf($"{sep}Microsoft.NET{sep}Framework{sep}", StringComparison.OrdinalIgnoreCase) == -1 &&
              References[i].ItemSpec.IndexOf($"{sep}Microsoft{sep}NetFramework{sep}", StringComparison.OrdinalIgnoreCase) == -1 &&
              References[i].ItemSpec.IndexOf($"{sep}NuGetScratch{sep}", StringComparison.OrdinalIgnoreCase) == -1 &&
              References[i].ItemSpec.IndexOf($"{sep}NuGet{sep}Cache{sep}", StringComparison.OrdinalIgnoreCase) == -1 &&
              References[i].ItemSpec.IndexOf($"{sep}NuGet{sep}v3-cache{sep}", StringComparison.OrdinalIgnoreCase) == -1 &&
              References[i].ItemSpec.IndexOf($"{sep}NuGet{sep}plugins-cache{sep}", StringComparison.OrdinalIgnoreCase) == -1 &&
              References[i].ItemSpec.IndexOf($"{sep}netstandard.library{sep}", StringComparison.OrdinalIgnoreCase) == -1 &&
              References[i].ItemSpec.IndexOf($"{sep}.nuget{sep}packages{sep}", StringComparison.OrdinalIgnoreCase) == -1 &&
              References[i].ItemSpec.IndexOf($"{sep}dotnet{sep}sdk{sep}", StringComparison.OrdinalIgnoreCase) == -1 &&
              References[i].ItemSpec.IndexOf($"{sep}dotnet{sep}packs{sep}", StringComparison.OrdinalIgnoreCase) == -1 &&
              References[i].ItemSpec.IndexOf($"{sep}dotnet{sep}shared{sep}Microsoft.", StringComparison.OrdinalIgnoreCase) == -1 &&
              References[i].ItemSpec.IndexOf($"{sep}MSBuild{sep}Microsoft.NET.", StringComparison.OrdinalIgnoreCase) == -1 &&
              References[i].ItemSpec.IndexOf(nugetPackagesEnvVariable, StringComparison.OrdinalIgnoreCase) == -1)
          {
            Log.LogMessage(MessageImportance.High, $"Adding '{References[i].ItemSpec}' for processing.");
            assembliesToSign.Add(References[i].ItemSpec);
          }
          else
          {
            Log.LogMessage(MessageImportance.Normal, $"Ignored '{References[i].ItemSpec}' cached/framework NuGet package.");
          }
        }

        if (string.IsNullOrEmpty(OutputPath?.ItemSpec))
        {
          Log.LogMessage("Task parameter 'OutputPath' not provided - signed files will overwrite source files.");
          SigningHelper.SignAssemblies(assembliesToSign, KeyFile, Password, probingPaths);
        }
        else
        {
          if (!Directory.Exists(OutputPath.ItemSpec))
          {
            Directory.CreateDirectory(OutputPath.ItemSpec);
          }
          var inOutAssemblies = assembliesToSign.Select(assm => new InputOutputFilePair(assm, Path.Combine(OutputPath.ItemSpec, Path.GetFileName(assm))));
          SigningHelper.SignAssemblies(inOutAssemblies, KeyFile, Password, probingPaths);
          Log.LogMessage(MessageImportance.Normal, $"Signing files to output folder '{OutputPath.ItemSpec}'");
        }

        if (CopyLocalPaths != null)
        {
          NewCopyLocalFiles = new ITaskItem[CopyLocalPaths.Length];
          for (int i = 0; i < CopyLocalPaths.Length; i++)
          {
            if (updatedReferencePaths.TryGetValue(CopyLocalPaths[i].ItemSpec, out string updatedPath))
            {
              NewCopyLocalFiles[i] = new TaskItem(CopyLocalPaths[i]) { ItemSpec = updatedPath };
            }
            else
            {
              NewCopyLocalFiles[i] = CopyLocalPaths[i];
            }
          }
        }

        Log.LogMessage(MessageImportance.High, $"-- Finished Brutal Developer .NET Assembly Strong-Name Signer Task in {timer.Elapsed} -- ");

        return true;
      }
      catch (Exception ex)
      {
        Log.LogErrorFromException(ex, true, true, null);
      }

      return false;
    }
  }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}
