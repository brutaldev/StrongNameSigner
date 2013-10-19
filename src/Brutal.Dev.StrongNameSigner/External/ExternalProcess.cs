using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace Brutal.Dev.StrongNameSigner.External
{
  internal abstract class ExternalProcess : IDisposable
  {
    private readonly StringBuilder processOutput = new StringBuilder();

    protected internal ExternalProcess(bool testMode)
    {
      TestMode = testMode;
    }

    ~ExternalProcess()
    {
      Dispose(false);
    }

    public abstract string Executable { get; }
    
    public virtual string Arguments
    {
      get
      {
        return null;
      }
    }

    public string Output
    {
      get
      {
        return processOutput.ToString();
      }
    }
    
    public bool TestMode { get; private set; }

    public override string ToString()
    {
      return (Executable ?? string.Empty) + " " + (Arguments ?? string.Empty);
    }
    
    public virtual bool Run(Action<string> outputHandler)
    {
      processOutput.Clear();

      processOutput.Append("EXECUTE: ").AppendLine(ToString()).AppendLine();

      using (var p = new Process())
      {
        p.StartInfo = new ProcessStartInfo(Executable)
        {
          Arguments = Arguments,
          Verb = "runas",
          WindowStyle = ProcessWindowStyle.Hidden,
          CreateNoWindow = true,
          WorkingDirectory = Path.GetDirectoryName(Executable),
          RedirectStandardOutput = true,
          RedirectStandardError = true,
          UseShellExecute = false
        };

        p.Start();
        processOutput.AppendLine(p.StandardOutput.ReadToEnd().Trim());
        processOutput.AppendLine(p.StandardError.ReadToEnd().Trim());

        if (outputHandler != null)
        {
          outputHandler(Output);
        }
        
        return p.WaitForExit(60000);
      }
    }

    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
    }
  }
}
