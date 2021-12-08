using System;
using System.Diagnostics.CodeAnalysis;

namespace Brutal.Dev.StrongNameSigner
{
  /// <summary>
  /// Expose .NET assembly information.
  /// </summary>
  [Serializable]
  public sealed class AssemblyInfo
  {
    /// <summary>
    /// Initializes a new instance of the <see cref="AssemblyInfo"/> class.
    /// </summary>
    public AssemblyInfo()
    {
      FilePath = string.Empty;
      DotNetVersion = string.Empty;
    }

    /// <summary>
    /// Gets the full file path of the assembly.
    /// </summary>
    /// <value>
    /// The full file path of the assembly.
    /// </value>
    public string FilePath { get; internal set; }

    /// <summary>
    /// Gets the .NET version that this assembly was built for, this will be the version of the CLR that is targeted.
    /// </summary>
    /// <value>
    /// The .NET version of the CLR this assembly will use.
    /// </value>
    public string DotNetVersion { get; internal set; }

    /// <summary>
    /// Determine the type of signing that is in place in the assembly.
    /// </summary>
    public StrongNameType SigningType { get; internal set; }

    /// <summary>
    /// Gets a value indicating whether this assembly is strong-name signed.
    /// </summary>
    /// <value>
    ///   <c>true</c> if the assembly is strong-name signed; otherwise, <c>false</c>.
    /// </value>
    public bool IsSigned => SigningType == StrongNameType.Signed;

    /// <summary>
    /// Gets a value indicating whether this assembly was built with the 32-bit preferred setting (.NET 4.5).
    /// </summary>
    /// <value>
    ///   <c>true</c> if 32-bit is preferred; otherwise, <c>false</c>.
    /// </value>
    public bool Is32BitPreferred { get; internal set; }

    /// <summary>
    /// Gets a value indicating whether this assembly specifically targets the x86 platform.
    /// </summary>
    /// <value>
    ///   <c>true</c> if assembly targets x86; otherwise, <c>false</c>.
    /// </value>
    public bool Is32BitOnly { get; internal set; }

    /// <summary>
    /// Gets a value indicating whether this assembly specifically targets the x64 platform.
    /// </summary>
    /// <value>
    ///   <c>true</c> if assembly targets x64; otherwise, <c>false</c>.
    /// </value>
    public bool Is64BitOnly { get; internal set; }

    /// <summary>
    /// Gets a value indicating whether this a .NET managed assembly (IL only).
    /// </summary>
    /// <value>
    ///   <c>true</c> if the assembly is managed; otherwise, <c>false</c>.
    /// </value>
    public bool IsManagedAssembly { get; internal set; }

    /// <summary>
    /// Gets a value indicating whether this assembly targets the any CPU platform.
    /// </summary>
    /// <value>
    ///   <c>true</c> if assembly targets any CPU; otherwise, <c>false</c>.
    /// </value>
    public bool IsAnyCpu
    {
      get
      {
        return IsManagedAssembly && !Is32BitOnly && !Is64BitOnly;
      }
    }
  }
}
