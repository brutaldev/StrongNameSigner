using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Brutal.Dev.StrongNameSigner.External
{
  internal sealed class SignTool : ExternalProcess
  {
    private readonly string signToolExePath = PathHelper.FindPathForWindowsSdkTool("sn.exe");

    public SignTool(bool testMode)
      : base(testMode)
    {
      KeyFilePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".snk"); 
    }

    internal SignTool()
      : this(false)
    {
    }

    public override string Executable
    {
      get { return signToolExePath; }
    }
    
    public override string Arguments
    {
      get
      {
        if (TestMode)
        {
          return "-?";
        }

        return "-k \"" + KeyFilePath + "\"";
      }
    }

    public string KeyFilePath { get; private set; }

    protected override void Dispose(bool disposing)
    {
      base.Dispose(disposing);

      if (disposing)
      {
        if (File.Exists(KeyFilePath))
        {
          File.Delete(KeyFilePath);
        }
      }
    }
  }
}
