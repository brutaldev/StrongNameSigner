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
    private static int Main(string[] args)
    {
      C.WriteLine("-----------------------------------------------------------");
      C.WriteLine("---- Brutal Developer .NET Assembly Strong-Name Signer ----");
      C.WriteLine("-----------------------------------------------------------");
      C.WriteLine(((AssemblyDescriptionAttribute)Assembly.GetEntryAssembly().GetCustomAttributes(typeof(AssemblyDescriptionAttribute), false)[0]).Description);
      C.WriteLine();

      try
      {
        var parsed = Args.Parse<Options>(args);

        if (args.Length == 0 || parsed.Help)
        {
          ArgUsage.GetStyledUsage<Options>().Write();
        }
        else
        {
          parsed.Validate();
          var stats = SignAssemblies(parsed);

          C.WriteLine();
          C.WriteLine("{0} file(s) were strong-name signed.", stats.NumberOfSignedFiles);
          C.WriteLine("{0} references(s) were fixed.", stats.NumberOfFixedReferences);
        }
      }
      catch (ArgException ex)
      {
        C.ForegroundColor = ConsoleColor.Red;
        C.WriteLine(ex.Message);
        C.ResetColor();

        ArgUsage.GetStyledUsage<Options>().Write();

        return 2;
      }
      catch (Exception ex)
      {
        C.ForegroundColor = ConsoleColor.Red;
        C.WriteLine(ex.ToString());
        C.ResetColor();

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
          if (FixSingleAssemblyReference(filePath, referencePath, options.KeyFile, options.Password))
          {
            referenceFixes++;
          }
        }
      }

      // Remove all InternalsVisibleTo attributes without public keys from the processed assemblies. Signed assemblies cannot have unsigned friend assemblies.
      foreach (var filePath in signedAssemblyPaths)
      {
        if (RemoveInvalidFriendAssemblyReferences(filePath, options.KeyFile, options.Password))
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
        C.WriteLine();
        C.WriteLine("Strong-name signing {0}...", assemblyPath);

        var info = SigningHelper.GetAssemblyInfo(assemblyPath);
        if (!info.IsSigned)
        {
          info = SigningHelper.SignAssembly(assemblyPath, keyPath, outputDirectory, password);

          C.ForegroundColor = ConsoleColor.Green;
          C.WriteLine("{0} was strong-name signed successfully!", info.FilePath);
          C.ResetColor();

          return info;
        }
        else
        {
          C.WriteLine("Already strong-name signed...");
        }
      }
      catch (BadImageFormatException bife)
      {
        C.ForegroundColor = ConsoleColor.Yellow;
        C.WriteLine("Warning: {0}", bife.Message);
        C.ResetColor();
      }
      catch (Exception ex)
      {
        C.ForegroundColor = ConsoleColor.Red;
        C.WriteLine("Error: {0}", ex.Message);
        C.ResetColor();
      }

      return null;
    }

    private static bool FixSingleAssemblyReference(string assemblyPath, string referencePath, string keyFile, string keyFilePassword)
    {
      try
      {
        C.WriteLine();
        C.WriteLine("Fixing references to '{1}' in '{0}'...", assemblyPath, referencePath);

        var info = SigningHelper.GetAssemblyInfo(assemblyPath);
        if (SigningHelper.FixAssemblyReference(assemblyPath, referencePath, keyFile, keyFilePassword))
        {
          C.ForegroundColor = ConsoleColor.Green;
          C.WriteLine("References were fixed successfully!");
          C.ResetColor();

          return true;
        }
        else
        {
          C.WriteLine("Nothing to fix...");
        }
      }
      catch (BadImageFormatException bife)
      {
        C.ForegroundColor = ConsoleColor.Yellow;
        C.WriteLine("Warning: {0}", bife.Message);
        C.ResetColor();
      }
      catch (Exception ex)
      {
        C.ForegroundColor = ConsoleColor.Red;
        C.WriteLine("Error: {0}", ex.Message);
        C.ResetColor();
      }

      return false;
    }

    private static bool RemoveInvalidFriendAssemblyReferences(string assemblyPath, string keyFile, string keyFilePassword)
    {
      try
      {
        C.WriteLine();
        C.WriteLine("Removing invalid friend references from '{0}'...", assemblyPath);

        var info = SigningHelper.GetAssemblyInfo(assemblyPath);
        if (SigningHelper.RemoveInvalidFriendAssemblies(assemblyPath, keyFile, keyFilePassword))
        {
          C.ForegroundColor = ConsoleColor.Green;
          C.WriteLine("Invalid friend assemblies removed successfully!");
          C.ResetColor();

          return true;
        }
        else
        {
          C.WriteLine("Nothing to fix...");
        }
      }
      catch (BadImageFormatException bife)
      {
        C.ForegroundColor = ConsoleColor.Yellow;
        C.WriteLine("Warning: {0}", bife.Message);
        C.ResetColor();
      }
      catch (Exception ex)
      {
        C.ForegroundColor = ConsoleColor.Red;
        C.WriteLine("Error: {0}", ex.Message);
        C.ResetColor();
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
