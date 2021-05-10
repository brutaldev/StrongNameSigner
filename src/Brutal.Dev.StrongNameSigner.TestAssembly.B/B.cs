namespace Brutal.Dev.StrongNameSigner.TestAssembly.B
{
  public class B
  {
    public B()
    {
    }

    internal string Secret()
    {
      return "Test call using InternalsVisibleToAttribute";
    }
  }
}
