using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using PowerArgs;

namespace Brutal.Dev.StrongNameSigner.Console
{
  [ArgExample("StrongNameSigner.Console -a MyUnsignedAssembly.dll", "Strong-name sign a single assembly and overwrite the original, a backup will be created.")]
  [ArgExample("StrongNameSigner.Console -a MyUnsignedAssembly.dll -o \"C:\\Signed\\\"", "Strong-name sign a single assembly called MyUnsignedAssembly.dll and copy the signed version to C:\\Signed.")]
  [ArgExample("StrongNameSigner.Console -a MyUnsignedAssembly.dll -k PersonalKey.snk", "Strong-name sign a single assembly with your own personal strong-name key file.")]
  [ArgExample("StrongNameSigner.Console -in C:\\Unsigned\\|C:\\More\\\" -out \"C:\\Signed\\\"", "Strong-name sign all assemblies (DLL and EXE) found in C:\\Unsigned and C:\\More and copy the signed versions to C:\\Signed.")]
  internal class Options
  {
    [ArgExistingFile]
    [ArgShortcut("a")]
    [ArgDescription("The assembly file to strong-name sign.")]
    public string AssemblyFile { get; set; }

    [ArgExistingFile]
    [ArgShortcut("k")]
    [ArgDescription("A strong-name key file to use. If not specified, one will be generated.")]
    public string KeyFile { get; set; }

    [ArgShortcut("p")]
    [ArgDescription("The password (if any) for the provided strong-name key file.")]
    public string Password { get; set; }

    [ArgDescription("A directory list (pipe | separated) containing assemblies to strong-name sign.")]
    [ArgShortcut("in")]
    public string InputDirectory { get; set; }

    [ArgExistingDirectory]
    [ArgShortcut("out")]
    [ArgDescription("Output directory for strong-name signed assemblies. Defaults to current directory.")]
    public string OutputDirectory { get; set; }

    [HelpHook]
    [ArgShortcut("h")]
    [ArgDescription("Displays options and usage information.")]
    public bool Help { get; set; }

    [ArgShortcut("l")]
    [ArgDescription("The preferred console output logging level. Errors and warnings will always be logged.")]
    public LogLevel LogLevel { get; set; }

    public void Validate()
    {
      if (string.IsNullOrWhiteSpace(AssemblyFile) && string.IsNullOrWhiteSpace(InputDirectory))
      {
        throw new ArgException("Please provide an assembly file or an input directory.");
      }

      if (!string.IsNullOrWhiteSpace(AssemblyFile) && !string.IsNullOrWhiteSpace(InputDirectory))
      {
        throw new ArgException("Both a single assembly file and an input directory cannot be used at the same time.");
      }

      if (!string.IsNullOrWhiteSpace(InputDirectory))
      {
        foreach (var inputDir in InputDirectory.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries))
        {
          if (!Directory.Exists(inputDir))
          {
            throw new ArgException(string.Format("Directory not found: '{0}'", inputDir));
          }
        }
      }
    }
  }
}
