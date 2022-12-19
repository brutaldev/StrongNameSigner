using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using PowerArgs;
using C = System.Console;

namespace Brutal.Dev.StrongNameSigner.Console
{
  internal static class Program
  {
    private static LogLevel currentLogLevel = LogLevel.Default;

    private static int Main(string[] args)
    {
      if (bool.TryParse(Environment.GetEnvironmentVariable("SNS_DISABLE_CONSOLE_SIGNING"), out var disabled) && disabled)
      {
        PrintMessageColor(".NET Assembly Strong-Name Signer is disabled via the SNS_DISABLE_CONSOLE_SIGNING environment variable.", LogLevel.Default, ConsoleColor.Red);
        return 0;
      }

      try
      {
        SigningHelper.Log = message => PrintMessage(message, LogLevel.Default);

        var parsed = Args.Parse<Options>(args);

        if (args.Length == 0 || parsed?.Help != false)
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

          SignAssemblies(parsed);
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
          C.WriteLine("Press any key to exit...");
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

    private static void SignAssemblies(Options options)
    {
      var filesToSign = new HashSet<string>();

      if (!string.IsNullOrWhiteSpace(options.InputDirectory))
      {
        foreach (var inputDir in options.InputDirectory.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries))
        {
          if (inputDir.Contains("*"))
          {
            string firstWildCardPart = inputDir.Substring(0, inputDir.IndexOf("*"));
            string searchPath = firstWildCardPart.Substring(0, firstWildCardPart.LastIndexOf(Path.DirectorySeparatorChar));

            foreach (var dir in Directory.GetDirectories(searchPath, "*", SearchOption.AllDirectories)
              .Where(d => Regex.IsMatch(d, "^" + Regex.Escape(inputDir).Replace("\\*", ".*").Replace("\\?", ".") + "$", RegexOptions.IgnoreCase)))
            {
              foreach (var file in GetFilesToSign(dir))
              {
                filesToSign.Add(file);
              }
            }
          }
          else
          {
            foreach (var file in GetFilesToSign(inputDir))
            {
              filesToSign.Add(file);
            }
          }
        }
      }
      else
      {
        // We can assume from validation that there will be a single file.
        filesToSign.Add(options.AssemblyFile);
      }

      var probingPaths = filesToSign.Select(f => Path.GetDirectoryName(f)).Distinct().ToArray();
      var basePath = string.Empty;

      if (options.KeepStructure)
      {
        if (!string.IsNullOrWhiteSpace(options.InputDirectory))
        {
          basePath = Path.GetFullPath(options.InputDirectory);
        }
        else
        {
          basePath = Path.GetDirectoryName(options.AssemblyFile);
        }
      }

      var assemblyInputOutputPaths = new List<InputOutputFilePair>();
      foreach (var filePath in filesToSign)
      {
        var fullFilePath = Path.GetFullPath(filePath);
        var outputDirectory = options.OutputDirectory;

        if (options.KeepStructure)
        {
          outputDirectory = Path.GetDirectoryName(fullFilePath)?.Replace(basePath, outputDirectory);
        }

        string outputFilePath = string.IsNullOrWhiteSpace(outputDirectory) ? Path.GetDirectoryName(filePath) : outputDirectory;
        assemblyInputOutputPaths.Add(new InputOutputFilePair(fullFilePath, Path.Combine(Path.GetFullPath(outputFilePath), Path.GetFileName(filePath))));
      }

      SigningHelper.SignAssemblies(assemblyInputOutputPaths, options.KeyFile, options.Password, probingPaths);
    }

    private static IEnumerable<string> GetFilesToSign(string directory)
    {
      var filesToSign = new HashSet<string>();

      foreach (var file in Directory.GetFiles(directory, "*.*", SearchOption.AllDirectories)
              .Where(f => Path.GetExtension(f).Equals(".exe", StringComparison.OrdinalIgnoreCase) ||
                          Path.GetExtension(f).Equals(".dll", StringComparison.OrdinalIgnoreCase)))
      {
        filesToSign.Add(file);
      }

      return filesToSign;
    }
  }
}
