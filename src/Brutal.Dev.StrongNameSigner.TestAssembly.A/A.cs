using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Brutal.Dev.StrongNameSigner.TestAssembly.A
{
  public class A
  {
    public A()
    {
      var b = new B.B();

      b.Secret();
    }
  }
}
