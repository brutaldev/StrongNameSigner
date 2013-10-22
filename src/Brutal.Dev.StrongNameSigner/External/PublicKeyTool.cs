using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Brutal.Dev.StrongNameSigner.External
{
  internal sealed class PublicKeyTool : ExternalProcess
  {
    private readonly string signToolExePath = PathHelper.FindPathForWindowsSdkTool("sn.exe");

    public PublicKeyTool(string assemblyPath)
      : base(false)
    {
      if (assemblyPath == null)
      {
        throw new ArgumentNullException("assemblyPath");
      }

      if (!File.Exists(assemblyPath))
      {
        throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, "The file '{0}' does not exist.", assemblyPath), "assemblyPath");
      }

      AssemblyPath = Path.GetFullPath(assemblyPath);
      PublicKeyToken = string.Empty;
    }

    internal PublicKeyTool()
      : base(true)
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

        return "-Tp \"" + AssemblyPath + "\"";
      }
    }

    private string AssemblyPath { get; set; }

    public string PublicKeyToken { get; private set; }

    public override bool Run(Action<string> outputHandler)
    {
      PublicKeyToken = string.Empty;

      if (base.Run(outputHandler) && Output.IndexOf("does not represent a strongly named assembly", StringComparison.OrdinalIgnoreCase) == -1)
      {
        PublicKeyToken = GetPublicKeyTokenFromOutput(Output);

        return !string.IsNullOrEmpty(PublicKeyToken);
      }

      return false;
    }

    private static string GetPublicKeyTokenFromOutput(string output)
    {
      var match = Regex.Match(output, @"Public key token is\s*(?<token>.*$)", RegexOptions.Multiline | RegexOptions.IgnoreCase);
      if (match != null && match.Captures.Count == 1)
      {
        return match.Groups["token"].Value.TrimEnd().ToUpperInvariant();
      }

      return string.Empty;
    }
  }
}
