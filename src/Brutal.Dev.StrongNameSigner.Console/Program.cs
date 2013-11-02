using System;
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
        // Verify we have everything we need.
        SigningHelper.CheckForRequiredSoftware();

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
      Action<string> verboseOutput = s => C.Write(s);

      if (!string.IsNullOrWhiteSpace(options.InputDirectory))
      {
        foreach (var filePath in Directory.GetFiles(options.InputDirectory, "*.*", SearchOption.AllDirectories)
          .Where(f => Path.GetExtension(f).Equals(".exe", StringComparison.OrdinalIgnoreCase) ||
                      Path.GetExtension(f).Equals(".dll", StringComparison.OrdinalIgnoreCase)))
        {
          if (SignSingleAssembly(filePath, options.KeyFile, options.OutputDirectory))
          {
            signedFiles++;
          }
        }
      }
      else
      {
        // We can assume from validation that there will be a single file.
        if (SignSingleAssembly(options.AssemblyFile, options.KeyFile, options.OutputDirectory))
        {
          signedFiles++;
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

        var info = SigningHelper.SignAssembly(assemblyPath, keyPath, outputDirectory);

        C.ForegroundColor = ConsoleColor.Green;
        C.WriteLine("{0} was strong-name signed successfully.", info.FilePath);
        C.ResetColor();

        return true;
      }
      catch (AlreadySignedException ase)
      {
        C.WriteLine(ase.Message);
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
