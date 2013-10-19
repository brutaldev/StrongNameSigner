using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace Brutal.Dev.StrongNameSigner.External
{
  [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Dasm", Justification = "Referring to an executable name.")]
  internal sealed class ILDasm : ExternalProcess
  {
    private readonly string ildasmExePath = PathHelper.FindPathForWindowsSdkTool("ildasm.exe");

    private bool binaryOutput = true;

    public ILDasm(AssemblyInfo assemblyInfo)
      : base(false)
    {
      if (assemblyInfo == null)
      {
        throw new ArgumentNullException("assemblyInfo");
      }

      AssemblyInfo = assemblyInfo;

      WorkingPath = Path.Combine(Path.GetTempPath(), Path.GetFileNameWithoutExtension(assemblyInfo.FilePath));

      ILFilePath = Path.Combine(WorkingPath, Path.GetFileNameWithoutExtension(assemblyInfo.FilePath)) + ".il";

      BinaryILFilePath = Path.Combine(WorkingPath, Path.GetFileNameWithoutExtension(assemblyInfo.FilePath)) + ".binary.il";

      Directory.CreateDirectory(WorkingPath);
    }

    internal ILDasm()
      : base(true)
    {
    }

    public override string Executable
    {
      get { return ildasmExePath; }
    }
    
    public override string Arguments
    {
      get
      {
        if (TestMode)
        {
          return "/?";
        }

        return string.Format(CultureInfo.InvariantCulture, 
          "{0} /NOBAR /UNICODE /TYPELIST /ALL \"{1}\" /OUT=\"{2}\"",
          binaryOutput ? string.Empty : "/CAVERBAL", 
          AssemblyInfo.FilePath, 
          binaryOutput ? BinaryILFilePath : ILFilePath).TrimStart();
      }
    }

    public AssemblyInfo AssemblyInfo { get; private set; }

    public string BinaryILFilePath { get; private set; }

    public string ILFilePath { get; private set; }
    
    public string WorkingPath { get; private set; }

    public override bool Run(Action<string> outputHandler)
    {
      binaryOutput = false;
      if (base.Run(outputHandler) && Output.IndexOf("cannot disassemble", StringComparison.OrdinalIgnoreCase) == -1)
      {
        binaryOutput = true;
        return base.Run(outputHandler) && Output.IndexOf("cannot disassemble", StringComparison.OrdinalIgnoreCase) == -1;
      }

      return false;
    }

    protected override void Dispose(bool disposing)
    {
      base.Dispose(disposing);

      if (disposing)
      {
        if (Directory.Exists(WorkingPath))
        {
          Directory.Delete(WorkingPath, true);
        }
      }
    }
  }
}
