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
