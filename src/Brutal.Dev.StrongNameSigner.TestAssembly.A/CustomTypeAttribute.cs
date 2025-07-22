using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Brutal.Dev.StrongNameSigner.TestAssembly.A
{
  [System.AttributeUsage(AttributeTargets.All, Inherited = false, AllowMultiple = false)]
  public sealed class CustomTypeAttribute : Attribute
  {
    public CustomTypeAttribute(Type type)
    {
      this.Type = type;
    }

    public Type Type { get; private set; }
  }
}
