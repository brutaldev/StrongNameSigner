using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Mono.Cecil;

namespace Brutal.Dev.StrongNameSigner
{
  /// <summary>
  /// Expose .NET assembly information.
  /// </summary>
  /// <seealso cref="IDisposable" />
  [Serializable]
  public sealed class AssemblyInfo : IEquatable<AssemblyInfo>, IDisposable
  {
    private readonly Lazy<AssemblyDefinition> modifiedDefintion;

    private bool isDisposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="AssemblyInfo"/> class.
    /// </summary>
    /// <param name="assemblyPath">The path to the assembly to load information for.</param>
    /// <param name="probingPaths">Additional paths to probe for references.</param>
    public AssemblyInfo(string assemblyPath, params string[] probingPaths)
    {
      FilePath = Path.GetFullPath(assemblyPath);

      using var currentDefinition = AssemblyDefinition.ReadAssembly(FilePath, GetReadParameters(FilePath, probingPaths));

      DotNetVersion = GetDotNetVersion(currentDefinition.MainModule.Runtime);
      IsManagedAssembly = currentDefinition.MainModule.Attributes.HasFlag(ModuleAttributes.ILOnly);
      Is64BitOnly = currentDefinition.MainModule.Architecture == TargetArchitecture.AMD64 || currentDefinition.MainModule.Architecture == TargetArchitecture.IA64;
      Is32BitOnly = currentDefinition.MainModule.Attributes.HasFlag(ModuleAttributes.Required32Bit) && !currentDefinition.MainModule.Attributes.HasFlag(ModuleAttributes.Preferred32Bit);
      Is32BitPreferred = currentDefinition.MainModule.Attributes.HasFlag(ModuleAttributes.Preferred32Bit);

      RefreshSigningType(currentDefinition);

      modifiedDefintion = new Lazy<AssemblyDefinition>(() => AssemblyDefinition.ReadAssembly(FilePath, GetReadParameters(FilePath, probingPaths)));
    }

    /// <inheritdoc/>
    ~AssemblyInfo()
    {
      Dispose(disposing: false);
    }

    /// <summary>
    /// Gets the full file path of the assembly.
    /// </summary>
    /// <value>
    /// The full file path of the assembly.
    /// </value>
    public string FilePath { get; }

    /// <summary>
    /// Gets the assembly definition.
    /// </summary>
    /// <value>
    /// The assembly definition.
    /// </value>
    public AssemblyDefinition Definition => modifiedDefintion.Value;

    /// <summary>
    /// Gets the .NET version that this assembly was built for, this will be the version of the CLR that is targeted.
    /// </summary>
    /// <value>
    /// The .NET version of the CLR this assembly will use.
    /// </value>
    public string DotNetVersion { get; }

    /// <summary>
    /// Determine the type of signing that is in place in the assembly.
    /// </summary>
    public StrongNameType SigningType { get; private set; }

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
    public bool IsAnyCpu => IsManagedAssembly && !Is32BitOnly && !Is64BitOnly;

    /// <summary>
    /// Saves the assembly to disk with any modifications made.
    /// </summary>
    /// <param name="assemblyPath">The full file path to write the assembly information to.</param>
    /// <param name="keyPair">The key pair.</param>
    public void Save(string assemblyPath, byte[] keyPair)
    {
      if (modifiedDefintion.IsValueCreated && !isDisposed)
      {
        modifiedDefintion.Value.Write(assemblyPath, new WriterParameters { StrongNameKeyBlob = keyPair, WriteSymbols = File.Exists(Path.ChangeExtension(FilePath, ".pdb")) });

        if (assemblyPath == FilePath)
        {
          RefreshSigningType(modifiedDefintion.Value);
        }
      }
    }

    /// <inheritdoc/>
    public bool Equals(AssemblyInfo other) => other?.FilePath.Equals(FilePath) == true;

    /// <inheritdoc/>
    public override bool Equals(object obj) => Equals(obj as AssemblyInfo);

    /// <inheritdoc/>
    public override int GetHashCode() => Tuple.Create(FilePath).GetHashCode();

    /// <inheritdoc/>
    public override string ToString() => FilePath;

    /// <inheritdoc/>
    public void Dispose()
    {
      Dispose(disposing: true);
      GC.SuppressFinalize(this);
    }

    private void RefreshSigningType(AssemblyDefinition defintion)
    {
      if (!defintion.MainModule.Attributes.HasFlag(ModuleAttributes.StrongNameSigned))
      {
        SigningType = StrongNameType.NotSigned;
      }
      else
      {
        IClrStrongName clrStrongName = null;

        try
        {
          var runtimeInterface = RuntimeEnvironment.GetRuntimeInterfaceAsObject(new Guid("B79B0ACD-F5CD-409b-B5A5-A16244610B92"), new Guid("9FD93CCF-3280-4391-B3A9-96E1CDE77C8D"));

          if (runtimeInterface != null)
          {
            clrStrongName = runtimeInterface as IClrStrongName;
          }
        }
        catch (InvalidCastException)
        {
          // Nothing to do here, cannot create the runtime interface so will skip verification.
        }
        catch (PlatformNotSupportedException)
        {
          // Nothing to do here, this only works in Windows.
        }

        var strongNameVerificationResult = clrStrongName?.StrongNameSignatureVerificationEx(FilePath, true, out _);
        bool strongNameVerified = !strongNameVerificationResult.HasValue || strongNameVerificationResult == 0;

        if (strongNameVerified)
        {
          SigningType = StrongNameType.Signed;
        }
        else
        {
          SigningType = StrongNameType.DelaySigned;
        }
      }
    }

    private static string GetDotNetVersion(TargetRuntime runtime)
    {
      return runtime switch
      {
        TargetRuntime.Net_1_0 => "1.0.3705",
        TargetRuntime.Net_1_1 => "1.1.4322",
        TargetRuntime.Net_2_0 => "2.0.50727",
        TargetRuntime.Net_4_0 => "4.0.30319",
        _ => "UNKNOWN",
      };
    }

    private static ReaderParameters GetReadParameters(string assemblyPath, string[] probingPaths)
    {
      using var resolver = new DefaultAssemblyResolver();
      if (!string.IsNullOrEmpty(assemblyPath) && File.Exists(assemblyPath))
      {
        resolver.AddSearchDirectory(Path.GetDirectoryName(assemblyPath));
      }

      if (probingPaths != null)
      {
        foreach (var searchDir in probingPaths.Where(Directory.Exists))
        {
          resolver.AddSearchDirectory(searchDir);
        }
      }

      // Add well known locations.
      resolver.RemoveSearchDirectory("C:\\Windows\\Microsoft.NET\\assembly");
      resolver.RemoveSearchDirectory("C:\\Windows\\assembly");
      resolver.RemoveSearchDirectory("C:\\Windows\\Microsoft.NET\\Framework64\\v4.0.30319");
      resolver.RemoveSearchDirectory("C:\\Windows\\Microsoft.NET\\Framework\\v4.0.30319");
      
      resolver.AddSearchDirectory("C:\\Windows\\Microsoft.NET\\assembly");
      resolver.AddSearchDirectory("C:\\Windows\\assembly");
      resolver.AddSearchDirectory("C:\\Windows\\Microsoft.NET\\Framework64\\v4.0.30319");
      resolver.AddSearchDirectory("C:\\Windows\\Microsoft.NET\\Framework\\v4.0.30319");

      ReaderParameters readParams;

      try
      {
        readParams = new ReaderParameters
        {
          InMemory = true,
          ReadingMode = ReadingMode.Deferred,
          AssemblyResolver = resolver,
          ReadSymbols = File.Exists(Path.ChangeExtension(assemblyPath, ".pdb"))
        };
      }
      catch (InvalidOperationException)
      {
        readParams = new ReaderParameters
        {
          InMemory = true,
          ReadingMode = ReadingMode.Deferred,
          AssemblyResolver = resolver
        };
      }

      return readParams;
    }

    private void Dispose(bool disposing)
    {
      if (!isDisposed)
      {
        if (disposing && modifiedDefintion.IsValueCreated)
        {
          modifiedDefintion.Value.Dispose();
        }

        isDisposed = true;
      }
    }
  }
}
