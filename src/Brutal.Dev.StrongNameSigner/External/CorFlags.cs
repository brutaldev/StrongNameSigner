using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;

namespace Brutal.Dev.StrongNameSigner.External
{
  [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Cor", Justification = "Referring to an executable name.")]
  [SuppressMessage("Microsoft.Naming", "CA1726:UsePreferredTerms", MessageId = "Flags", Justification = "Referring to an executable name.")]
  internal sealed class CorFlags : ExternalProcess
  {
    private readonly string corflagsExePath = PathHelper.FindPathForWindowsSdkTool("CorFlags.exe");

    public CorFlags(string assemblyPath)
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
    }
    
    internal CorFlags()
      : base(true)
    {
    }
    
    public override string Executable
    {
      get { return corflagsExePath; }
    }
    
    public override string Arguments
    {
      get
      {
        if (TestMode)
        {
          return "/?";
        }

        return "\"" + AssemblyPath + "\"";
      }
    }

    public AssemblyInfo AssemblyInfo { get; private set; }

    private string AssemblyPath { get; set; }

    public override bool Run(Action<string> outputHandler)
    {
      if (base.Run(outputHandler) && Output.IndexOf("corflags : error", StringComparison.OrdinalIgnoreCase) == -1)
      {
        CreateAssemblyInfo();
        return true;
      }

      return false;
    }

    private static string GetDotNetVersionFromOutput(string output)
    {
      var match = Regex.Match(output, @"Version\s*:\sv(?<ver>.*$)", RegexOptions.Multiline | RegexOptions.IgnoreCase);
      if (match != null && match.Captures.Count == 1)
      {
        return match.Groups["ver"].Value.TrimEnd();
      }

      return string.Empty;
    }

    private static bool GetIsSignedFromOutput(string output)
    {
      var match = Regex.Match(output, @"Signed\s*:\s(?<flag>\d{1})", RegexOptions.Multiline | RegexOptions.IgnoreCase);
      if (match != null && match.Captures.Count == 1)
      {
        return Convert.ToByte(match.Groups["flag"].Value, CultureInfo.InvariantCulture) == 1;
      }

      return false;
    }

    private static bool GetIsManagedAssemblyFromOutput(string output)
    {
      var match = Regex.Match(output, @"ILONLY\s*:\s(?<flag>\d{1})", RegexOptions.Multiline | RegexOptions.IgnoreCase);
      if (match != null && match.Captures.Count == 1)
      {
        return Convert.ToByte(match.Groups["flag"].Value, CultureInfo.InvariantCulture) == 1;
      }

      return false;
    }

    private static bool GetIs64BitOnlyFromOutput(string output)
    {
      var match = Regex.Match(output, @"PE\s*:\s(?<pe>.*$)", RegexOptions.Multiline | RegexOptions.IgnoreCase);
      if (match != null && match.Captures.Count == 1)
      {
        return match.Groups["pe"].Value.TrimEnd().Equals("PE32+", StringComparison.OrdinalIgnoreCase);
      }

      return false;
    }

    private static bool GetIs32BitOnlyFromOutput(string output)
    {
      var match = Regex.Match(output, @"32BITREQ\s*:\s(?<flag>\d{1})", RegexOptions.Multiline | RegexOptions.IgnoreCase);
      if (match != null && match.Captures.Count == 1)
      {
        return Convert.ToByte(match.Groups["flag"].Value, CultureInfo.InvariantCulture) == 1;
      }

      return false;
    }

    private static bool GetIs32BitPreferredFromOutput(string output)
    {
      var match = Regex.Match(output, @"32BITPREF\s*:\s(?<flag>\d{1})", RegexOptions.Multiline | RegexOptions.IgnoreCase);
      if (match != null && match.Captures.Count == 1)
      {
        return Convert.ToByte(match.Groups["flag"].Value, CultureInfo.InvariantCulture) == 1;
      }

      return false;
    }

    private void CreateAssemblyInfo()
    {
      if (!string.IsNullOrEmpty(AssemblyPath))
      {
        AssemblyInfo = new AssemblyInfo()
        {
          FilePath = Path.GetFullPath(AssemblyPath),
          DotNetVersion = GetDotNetVersionFromOutput(Output),
          IsSigned = GetIsSignedFromOutput(Output),
          IsManagedAssembly = GetIsManagedAssemblyFromOutput(Output),
          Is64BitOnly = GetIs64BitOnlyFromOutput(Output),
          Is32BitOnly = GetIs32BitOnlyFromOutput(Output),
          Is32BitPreferred = GetIs32BitPreferredFromOutput(Output)
        };
      }
    }    
  }
}
