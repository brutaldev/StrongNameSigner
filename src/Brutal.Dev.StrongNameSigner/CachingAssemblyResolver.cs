using Mono.Cecil;

namespace Brutal.Dev.StrongNameSigner
{
  internal class CachingAssemblyResolver : DefaultAssemblyResolver
  {
    public new void RegisterAssembly(AssemblyDefinition assembly)
    {
      base.RegisterAssembly(assembly);
    }
  }
}
