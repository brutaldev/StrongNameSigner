using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using PowerArgs;
using C = System.Console;

namespace Brutal.Dev.StrongNameSigner.Console
{
  internal static class Program
  {
    private static LogLevel currentLogLevel = LogLevel.Default;

    private static int Main(string[] args)
    {
      try
      {
        var parsed = Args.Parse<Options>(args);

        if (args.Length == 0 || parsed == null || parsed.Help)
        {
          PrintHeader();
          ArgUsage.GenerateUsageFromTemplate(typeof(Options));
        }
        else
        {
          parsed.Validate();

          currentLogLevel = parsed.LogLevel;

          if (currentLogLevel <= LogLevel.Verbose)
          {
            PrintHeader();
          }

          var stats = SignAssemblies(parsed);

          PrintMessage(null, LogLevel.Summary);
          PrintMessage(".NET Assembly Strong-Name Signer Summary", LogLevel.Summary);
          PrintMessage(string.Format("{0} file(s) were strong-name signed.", stats.NumberOfSignedFiles), LogLevel.Summary);
          PrintMessage(string.Format("{0} references(s) were fixed.", stats.NumberOfFixedReferences), LogLevel.Summary);
        }
      }
      catch (ArgException ex)
      {
        PrintMessageColor(ex.Message, LogLevel.Silent, ConsoleColor.Red);

        ArgUsage.GenerateUsageFromTemplate(typeof(Options));

        return 2;
      }
      catch (Exception ex)
      {
        PrintMessageColor(ex.ToString(), LogLevel.Silent, ConsoleColor.Red);

        return 1;
      }
      finally
      {
        if (Debugger.IsAttached)
        {
          C.ReadKey(true);
        }
      }

      return 0;
    }

    private static void PrintHeader()
    {
      C.WriteLine("-----------------------------------------------------------");
      C.WriteLine("---- Brutal Developer .NET Assembly Strong-Name Signer ----");
      C.WriteLine("-----------------------------------------------------------");
      C.WriteLine(((AssemblyDescriptionAttribute)Assembly.GetEntryAssembly().GetCustomAttributes(typeof(AssemblyDescriptionAttribute), false)[0]).Description);
    }

    private static void PrintMessage(string message, LogLevel minLogLevel)
    {
      if (currentLogLevel <= minLogLevel)
      {
        if (string.IsNullOrEmpty(message))
        {
          C.WriteLine();
        }
        else
        {
          C.WriteLine(message);
        }
      }
    }

    private static void PrintMessageColor(string message, LogLevel minLogLevel, ConsoleColor color)
    {
      C.ForegroundColor = color;
      PrintMessage(message, minLogLevel);
      C.ResetColor();      
    }

    private static Stats SignAssemblies(Options options)
    {
      int signedFiles = 0;
      int referenceFixes = 0;

      HashSet<string> filesToSign = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
      
      if (!string.IsNullOrWhiteSpace(options.InputDirectory))
      {
        foreach (var inputDir in options.InputDirectory.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries))
        {
          foreach (var file in Directory.GetFiles(inputDir, "*.*", SearchOption.AllDirectories)
            .Where(f => Path.GetExtension(f).Equals(".exe", StringComparison.OrdinalIgnoreCase) ||
                        Path.GetExtension(f).Equals(".dll", StringComparison.OrdinalIgnoreCase)))
          {
            filesToSign.Add(file);
          }
        }
      }
      else
      {
        // We can assume from validation that there will be a single file.
        filesToSign.Add(options.AssemblyFile);
      }

      var processedAssemblyPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
      var signedAssemblyPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

      foreach (var filePath in filesToSign)
      {
        var signedAssembly = SignSingleAssembly(filePath, options.KeyFile, options.OutputDirectory, options.Password);
        if (signedAssembly != null)
        {
          processedAssemblyPaths.Add(signedAssembly.FilePath);
          signedAssemblyPaths.Add(signedAssembly.FilePath);
          signedFiles++;
        }
        else
        {
          processedAssemblyPaths.Add(filePath);
        }
      }

      var referencesToFix = new HashSet<string>(processedAssemblyPaths, StringComparer.OrdinalIgnoreCase);
      foreach (var filePath in processedAssemblyPaths)
      {
        // Go through all the references excluding the file we are working on.
        foreach (var referencePath in referencesToFix.Where(r => !r.Equals(filePath)))
        {
          if (FixSingleAssemblyReference(filePath, referencePath, options.KeyFile, options.Password, filesToSign.Select(f => Path.GetDirectoryName(f)).Distinct().ToArray()))
          {
            referenceFixes++;
          }
        }
      }

      // Remove all InternalsVisibleTo attributes without public keys from the processed assemblies. Signed assemblies cannot have unsigned friend assemblies.
      foreach (var filePath in signedAssemblyPaths)
      {
        if (RemoveInvalidFriendAssemblyReferences(filePath, options.KeyFile, options.Password, filesToSign.Select(f => Path.GetDirectoryName(f)).Distinct().ToArray()))
        {
          referenceFixes++;
        }
      }

      return new Stats()
      {
        NumberOfSignedFiles = signedFiles,
        NumberOfFixedReferences = referenceFixes
      };
    }

    private static AssemblyInfo SignSingleAssembly(string assemblyPath, string keyPath, string outputDirectory, string password)
    {
      try
      {
        PrintMessage(null, LogLevel.Verbose);
        PrintMessage(string.Format("Strong-name signing '{0}'...", assemblyPath), LogLevel.Verbose);

        var info = SigningHelper.GetAssemblyInfo(assemblyPath);
        if (!info.IsSigned)
        {
          info = SigningHelper.SignAssembly(assemblyPath, keyPath, outputDirectory, password);

          PrintMessageColor(string.Format("'{0}' was strong-name signed successfully.", info.FilePath), LogLevel.Changes, ConsoleColor.Green);

          return info;
        }
        else
        {
          PrintMessage("Already strong-name signed...", LogLevel.Verbose);
        }
      }
      catch (BadImageFormatException bife)
      {
        PrintMessageColor(string.Format("Warning: {0}", bife.Message), LogLevel.Silent, ConsoleColor.Yellow);
      }
      catch (Exception ex)
      {
        PrintMessageColor(string.Format("Error: {0}", ex.Message), LogLevel.Silent, ConsoleColor.Red);
      }

      return null;
    }

    private static bool FixSingleAssemblyReference(string assemblyPath, string referencePath, string keyFile, string keyFilePassword, params string[] probingPaths)
    {
      try
      {
        PrintMessage(null, LogLevel.Verbose);
        PrintMessage(string.Format("Fixing references to '{1}' in '{0}'...", assemblyPath, referencePath), LogLevel.Verbose);

        var info = SigningHelper.GetAssemblyInfo(assemblyPath);
        if (SigningHelper.FixAssemblyReference(assemblyPath, referencePath, keyFile, keyFilePassword, probingPaths))
        {
          PrintMessageColor(string.Format("References to '{1}' in '{0}' were fixed successfully.", assemblyPath, referencePath), LogLevel.Changes, ConsoleColor.Green);
          
          return true;
        }
        else
        {
          PrintMessage("No assembly references to fix...", LogLevel.Verbose);
        }
      }
      catch (BadImageFormatException bife)
      {
        PrintMessageColor(string.Format("Warning: {0}", bife.Message), LogLevel.Silent, ConsoleColor.Yellow);
      }
      catch (Exception ex)
      {
        PrintMessageColor(string.Format("Error: {0}", ex.Message), LogLevel.Silent, ConsoleColor.Red);
      }

      return false;
    }

    private static bool RemoveInvalidFriendAssemblyReferences(string assemblyPath, string keyFile, string keyFilePassword, params string[] probingPaths)
    {
      try
      {
        PrintMessage(null, LogLevel.Verbose);
        PrintMessage(string.Format("Removing invalid friend references from '{0}'...", assemblyPath), LogLevel.Verbose);

        var info = SigningHelper.GetAssemblyInfo(assemblyPath);
        if (SigningHelper.RemoveInvalidFriendAssemblies(assemblyPath, keyFile, keyFilePassword, probingPaths))
        {
          PrintMessageColor(string.Format("Invalid friend assemblies removed successfully from '{0}'.", assemblyPath), LogLevel.Changes, ConsoleColor.Green);

          return true;
        }
        else
        {
          PrintMessage("No friend references to fix...", LogLevel.Verbose);
        }
      }
      catch (BadImageFormatException bife)
      {
        PrintMessageColor(string.Format("Warning: {0}", bife.Message), LogLevel.Silent, ConsoleColor.Yellow);
      }
      catch (Exception ex)
      {
        PrintMessageColor(string.Format("Error: {0}", ex.Message), LogLevel.Silent, ConsoleColor.Red);
      }

      return false;
    }
    
    private struct Stats
    {
      public int NumberOfSignedFiles { get; set; }

      public int NumberOfFixedReferences { get; set; }
    }
  }
}
