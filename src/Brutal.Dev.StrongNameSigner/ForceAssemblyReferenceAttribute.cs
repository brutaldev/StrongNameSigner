using System;
using Brutal.Dev.StrongNameSigner;

// These assemblies are used by Cecil, and reading assemblies with symbols without these DLL's present
// will cause an error ("No Symbols Found"). So to ensure that these are actually referenced by 
// StrongNameSigner and copied along to the output directory as well as the UnitTests when running 
// them, we use this "hack".
[assembly: ForceAssemblyReference(typeof(Mono.Cecil.Pdb.NativePdbReader))]
[assembly: ForceAssemblyReference(typeof(Mono.Cecil.Mdb.MdbReader))]
[assembly: ForceAssemblyReference(typeof(Mono.Cecil.Rocks.TypeDefinitionRocks))]

namespace Brutal.Dev.StrongNameSigner
{
  /// <summary>
  /// Attribute used to force an assembly reference to a specific assembly to be actually referenced and copied locally.
  /// </summary>
  [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
  internal sealed class ForceAssemblyReferenceAttribute : Attribute
  {
    // https://stackoverflow.com/questions/15816769/dependent-dll-is-not-getting-copied-to-the-build-output-folder-in-visual-studio
    public ForceAssemblyReferenceAttribute(Type forcedType)
    {
      void noop(Type _) { }
      noop(forcedType);
    }
  }
}
