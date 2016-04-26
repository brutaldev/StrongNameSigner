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

          SigningHelper.currentLogLevel = parsed.LogLevel;

          if (SigningHelper.currentLogLevel <= LogLevel.Verbose)
          {
            PrintHeader();
          }

          var stats = SignAssemblies(parsed);

          SigningHelper.PrintMessage(null, LogLevel.Summary);
          SigningHelper.PrintMessage(".NET Assembly Strong-Name Signer Summary", LogLevel.Summary);
          SigningHelper.PrintMessage(string.Format("{0} file(s) were strong-name signed.", stats.NumberOfSignedFiles), LogLevel.Summary);
          SigningHelper.PrintMessage(string.Format("{0} references(s) were fixed.", stats.NumberOfFixedReferences), LogLevel.Summary);
        }
      }
      catch (ArgException ex)
      {
        SigningHelper.PrintMessageColor(ex.Message, LogLevel.Silent, ConsoleColor.Red);

        ArgUsage.GenerateUsageFromTemplate(typeof(Options));

        return 2;
      }
      catch (Exception ex)
      {
        SigningHelper.PrintMessageColor(ex.ToString(), LogLevel.Silent, ConsoleColor.Red);

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

    private static Stats SignAssemblies(Options options)
    {
      var filesToSign = FilesToSign(options, new AssemblyCollection(SigningHelper.PrintMessageColor));
      var stats = filesToSign.Sign(options.KeyFile, options.Password, SigningHelper.PrintMessageColor);
      return stats;
    }

    private static AssemblyCollection FilesToSign(Options options, AssemblyCollection assemblies)
    {
      if (!Directory.Exists(options.OutputDirectory))
        Directory.CreateDirectory(options.OutputDirectory);

      if (!string.IsNullOrWhiteSpace(options.InputDirectory))
      {
        foreach (var inputDir in options.InputDirectory.Split(new char[] {'|'}, StringSplitOptions.RemoveEmptyEntries))
        {
          assemblies.AddFromPath(inputDir, options.OutputDirectory);
        }
      }
      else
      {
        // We can assume from validation that there will be a single file.
        assemblies.AddFromFile(options.AssemblyFile, options.OutputDirectory);
      }

      return assemblies;
    }
  }
}
