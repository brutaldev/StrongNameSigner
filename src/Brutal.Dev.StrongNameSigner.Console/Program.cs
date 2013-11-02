using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using PowerArgs;
using C = System.Console;
using System.Collections.Generic;

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
          int count = SignAssemblies(parsed);

          C.WriteLine();

          if (count == 0)
          {
            C.ForegroundColor = ConsoleColor.Red;
            C.WriteLine("No assemblies were strong-name signed...");
            C.ResetColor();
          }
          else
          {
            C.WriteLine("Successfully strong-name signed {0} assemblies.", count);
          }
        }
      }
      catch (ArgException ex)
      {
        C.ForegroundColor = ConsoleColor.Red;
        C.WriteLine(ex.Message);
        C.ResetColor();

        ArgUsage.GetStyledUsage<Options>().Write();
      }
      catch (Exception ex)
      {
        C.ForegroundColor = ConsoleColor.Red;
        C.WriteLine(ex.ToString());
        C.ResetColor();
        
        return 1;
      }

      if (Debugger.IsAttached)
      {
        C.ReadKey(true);
      }

      return 0;
    }

    private static int SignAssemblies(Options options)
    {
      int signedFiles = 0;
      IEnumerable<string> filesToSign = null;
      string keyFile = options.KeyFile;

      if (string.IsNullOrEmpty(keyFile))
      {
        keyFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".snk");
        File.WriteAllBytes(keyFile, SigningHelper.GenerateStrongNameKeyPair());
      }

      try
      {
        if (!string.IsNullOrWhiteSpace(options.InputDirectory))
        {
          filesToSign = Directory.GetFiles(options.InputDirectory, "*.*", SearchOption.AllDirectories)
            .Where(f => Path.GetExtension(f).Equals(".exe", StringComparison.OrdinalIgnoreCase) ||
                        Path.GetExtension(f).Equals(".dll", StringComparison.OrdinalIgnoreCase));
        }
        else
        {
          // We can assume from validation that there will be a single file.
          filesToSign = new string[] { options.AssemblyFile };
        }

        foreach (var filePath in filesToSign)
        {
          if (SignSingleAssembly(filePath, keyFile, options.OutputDirectory))
          {
            signedFiles++;
          }
        }

        var referencesToFix = new List<string>(filesToSign);
        foreach (var filePath in filesToSign)
        {
          // Go through all the references excluding the file we are working on.
          foreach (var referencePath in referencesToFix.Where(r => !r.Equals(filePath)))
          {
            SigningHelper.FixAssemblyReference(filePath, referencePath, keyFile);
          }
        }
      }
      finally
      {
        if (string.IsNullOrEmpty(options.KeyFile) && File.Exists(keyFile))
        {
          File.Delete(keyFile);
        }
      }
      
      return signedFiles;
    }

    private static bool SignSingleAssembly(string assemblyPath, string keyPath, string outputDirectory)
    {
      try
      {
        C.WriteLine();
        C.WriteLine("Strong-name signing {0}...", assemblyPath);

        var info = SigningHelper.GetAssemblyInfo(assemblyPath);
        if (!info.IsSigned)
        {
          info = SigningHelper.SignAssembly(assemblyPath, keyPath, outputDirectory);

          C.ForegroundColor = ConsoleColor.Green;
          C.WriteLine("{0} was strong-name signed successfully.", info.FilePath);
          C.ResetColor();

          return true;
        }        
      }
      catch (InvalidOperationException ioe)
      {
        C.ForegroundColor = ConsoleColor.Yellow;
        C.WriteLine("Warning: {0}", ioe.Message);
        C.ResetColor();
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
  }
}
