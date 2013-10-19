using System;
using System.Globalization;
using System.IO;

namespace Brutal.Dev.StrongNameSigner.External
{
  internal sealed class ILAsm : ExternalProcess
  {
    private readonly string ilasmExePath = PathHelper.FindPathForDotNetFrameworkTool("ilasm.exe", "4.0.30319");

    public ILAsm(AssemblyInfo assemblyInfo, string ilFilePath, string keyFilePath, string outputPath)
      : base(false)
    {
      if (assemblyInfo == null)
      {
        throw new ArgumentNullException("assemblyInfo");
      }

      if (ilFilePath == null)
      {
        throw new ArgumentNullException("ilFilePath");
      }

      if (!File.Exists(ilFilePath))
      {
        throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, "The file '{0}' does not exist.", ilFilePath), "ilFilePath");
      }

      if (File.Exists(keyFilePath ?? string.Empty))
      {
        KeyFilePath = keyFilePath;
      }

      ilasmExePath = PathHelper.FindPathForDotNetFrameworkTool("ilasm.exe", assemblyInfo.DotNetVersion);

      AssemblyInfo = assemblyInfo;

      ILFilePath = Path.GetFullPath(ilFilePath);

      if (string.IsNullOrEmpty(outputPath))
      {
        SignedAssemblyPath = assemblyInfo.FilePath;
      }
      else
      {
        Directory.CreateDirectory(outputPath);
        SignedAssemblyPath = Path.Combine(Path.GetFullPath(outputPath), Path.GetFileName(AssemblyInfo.FilePath));        
      }
    }

    internal ILAsm()
      : base(true)
    {
    }

    public override string Executable
    {
      get { return ilasmExePath; }
    }
    
    public override string Arguments
    {
      get
      {
        if (TestMode)
        {
          return "/?";
        }

        string resourceFilePath = Path.Combine(Path.GetDirectoryName(ILFilePath), Path.GetFileNameWithoutExtension(ILFilePath) + ".res");
        string resourceOption = string.Empty;
        if (File.Exists(resourceFilePath))
        {
          resourceOption = string.Format(CultureInfo.InvariantCulture, "/RES=\"{0}\"", resourceFilePath);
        }

        string keyFileOption = string.Empty;
        if (!string.IsNullOrEmpty(KeyFilePath))
        {
          keyFileOption = string.Format(CultureInfo.InvariantCulture, "/KEY=\"{0}\"", KeyFilePath);
        }

        string imageOption = string.Empty;
        if (AssemblyInfo != null && !AssemblyInfo.IsAnyCpu)
        {
          if (AssemblyInfo.Is32BitPreferred)
          {
            imageOption = "/32BITPREFERRED";
          }
          else if (AssemblyInfo.Is64BitOnly)
          {
            imageOption = "/X64";
          }
        }

        return string.Format(CultureInfo.InvariantCulture,
          "/{0} {1} /OUTPUT=\"{2}\" \"{3}\" {4} {5}",
          Path.GetExtension(AssemblyInfo.FilePath).TrimStart('.').ToUpperInvariant(), 
          imageOption,
          SignedAssemblyPath, 
          ILFilePath,
          keyFileOption, 
          resourceOption).TrimEnd();
      }
    }

    public AssemblyInfo AssemblyInfo { get; private set; }

    public string ILFilePath { get; private set; }

    public string KeyFilePath { get; private set; }
        
    public string SignedAssemblyPath { get; private set; }

    public override bool Run(Action<string> outputHandler)
    {
      if (AssemblyInfo != null && AssemblyInfo.FilePath.Equals(SignedAssemblyPath, StringComparison.OrdinalIgnoreCase))
      {
        // Make a backup before overwriting.
        File.Copy(AssemblyInfo.FilePath, AssemblyInfo.FilePath + ".unsigned", true);
      }

      return base.Run(outputHandler);
    }
  }
}
