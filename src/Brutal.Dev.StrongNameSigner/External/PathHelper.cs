using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Brutal.Dev.StrongNameSigner.External
{
  internal static class PathHelper
  {
    internal static string FindPathForDotNetFrameworkTool(string executable, string version)
    {
      string[] frameworkPaths = new[]
      {
        @"Microsoft.NET\Framework\v" + version,
        @"Microsoft.NET\Framework\v4.0.30319",
        @"Microsoft.NET\Framework\v2.0.50727",
        @"Microsoft.NET\Framework\v1.1.4322"
      };

      foreach (var possiblePath in frameworkPaths)
      {
        string fullPath = string.Empty;

        if (Environment.Is64BitProcess)
        {
          fullPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), possiblePath.Replace(@"\Framework\", @"\Framework64\"), executable);
          if (File.Exists(fullPath))
          {
            return fullPath;
          }
        }

        fullPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), possiblePath, executable);
        if (File.Exists(fullPath))
        {
          return fullPath;
        }
      }

      return executable;
    }

    internal static string FindPathForWindowsSdkTool(string executable)
    {
      string[] windowsSdkPaths = new[]
      {
        @"Microsoft SDKs\Windows\v8.0A\bin\NETFX 4.0 Tools\",
        @"Microsoft SDKs\Windows\v8.0A\bin\",
        @"Microsoft SDKs\Windows\v8.0\bin\NETFX 4.0 Tools\",
        @"Microsoft SDKs\Windows\v8.0\bin\",
        @"Microsoft SDKs\Windows\v7.1A\bin\NETFX 4.0 Tools\",
        @"Microsoft SDKs\Windows\v7.1A\bin\",
        @"Microsoft SDKs\Windows\v7.0A\bin\NETFX 4.0 Tools\",
        @"Microsoft SDKs\Windows\v7.0A\bin\",
        @"Microsoft SDKs\Windows\v6.1A\bin\",        
        @"Microsoft SDKs\Windows\v6.0A\bin\",
        @"Microsoft SDKs\Windows\v6.0\bin\",
        @"Microsoft.NET\FrameworkSDK\bin"
      };

      foreach (var possiblePath in windowsSdkPaths)
      {
        string fullPath = string.Empty;

        // Check alternate program file paths as well as 64-bit versions.
        if (Environment.Is64BitProcess)
        {
          fullPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), possiblePath, "x64", executable);
          if (File.Exists(fullPath))
          {
            return fullPath;
          }

          fullPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), possiblePath, "x64", executable);
          if (File.Exists(fullPath))
          {
            return fullPath;
          }
        }

        fullPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), possiblePath, executable);
        if (File.Exists(fullPath))
        {
          return fullPath;
        }

        fullPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), possiblePath, executable);
        if (File.Exists(fullPath))
        {
          return fullPath;
        }
      }

      return executable;
    }
  }
}
