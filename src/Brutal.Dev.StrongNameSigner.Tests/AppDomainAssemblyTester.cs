using System;
using System.Reflection;

namespace Brutal.Dev.StrongNameSigner.Tests
{
  internal class AppDomainAssemblyTester : MarshalByRefObject
  {
    private Assembly assembly;
    private Type type;

    public void LoadAssembly(string path)
    {
      assembly = Assembly.LoadFrom(path);
      type = assembly.GetType("Brutal.Dev.StrongNameSigner.TestAssembly.A.A");
    }

    public void TestCustomAttributesOnAssembly()
    {
      assembly.GetCustomAttributes(false);
    }

    public void TestCustomAttributesOnTypes()
    {
      type.GetCustomAttributes(false);
    }

    public void TestCustomAttributesOnMethods()
    {
      type.GetMethod("MethodAttributeCheck").GetCustomAttributes(false);
    }

    public void TestCustomAttributesOnParameters()
    {
      var method = type.GetMethod("ParameterAttributeCheck");
      method.GetParameters()[0].GetCustomAttributes(false);
    }

    public void TestCustomAttributesOnReturnParameters()
    {
      var method = type.GetMethod("ReturnParameterCheck");
      method.ReturnParameter.GetCustomAttributes(false);
    }

    public void TestCustomAttributesOnProperties()
    {
      type.GetProperty("BProperty").GetCustomAttributes(false);
    }

    public void TestCustomAttributesOnFields()
    {
      type.GetField("BField").GetCustomAttributes(false);
    }

    public void TestCustomAttributesOnEvents()
    {
      type.GetEvent("MyEvent").GetCustomAttributes(false);
    }

    public void TestCustomAttributesOnEventMethods()
    {
      var eventMethod = type.GetEvent("MyEvent").GetAddMethod();
      eventMethod.GetCustomAttributes(false);
    }

    public void TestCustomAttributesOnEventFields()
    {
      var field = type.GetField("MyEvent", BindingFlags.NonPublic | BindingFlags.Instance);
      field.GetCustomAttributes(false);
    }

    public void TestCustomAttributesOnConstructors() => type.GetConstructor(new Type[] { }).GetCustomAttributes(false);

    public void TestCustomAttributesOnConstructorParameters()
    {
      var constructor = type.GetConstructor(new Type[] { typeof(int) });
      var parameters = constructor.GetParameters();
      parameters[0].GetCustomAttributes(false);
    }
  }
}
