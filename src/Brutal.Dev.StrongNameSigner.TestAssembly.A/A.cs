using System;

namespace Brutal.Dev.StrongNameSigner.TestAssembly.A
{
  [CustomType(typeof(B.B))]
  public class A
  {
    [CustomType(typeof(B.B))]
    [method: CustomType(typeof(B.B))]
    [field: CustomType(typeof(B.B))]
    public event EventHandler MyEvent;

    [CustomType(typeof(B.B))]
    public B.B BField;

    [CustomType(typeof(B.B))]
    public A()
    {
      var b = new B.B();

      b.Secret();
    }

    public A([CustomType(typeof(B.B))] int i)
    {
    }

    [CustomType(typeof(B.B))]
    public B.B BProperty { get; set; }

    [CustomType(typeof(B.B))]
    public void MethodAttributeCheck()
    {
    }

    public void ParameterAttributeCheck([CustomType(typeof(B.B))] string myParameter)
    {
    }

    [return: CustomType(typeof(B.B))]
    public string ReturnParameterCheck(string myParameter)
    {
      return myParameter;
    }
  }
}
