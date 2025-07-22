using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

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
      var attributes = assembly.GetCustomAttributes(false);
    }

    public void TestCustomAttributesOnTypes()
    {
      var attributes = type.GetCustomAttributes(false);
    }

    public void TestCustomAttributesOnMethods()
    {
      var methodAttributes = type.GetMethod("MethodAttributeCheck").GetCustomAttributes(false);
    }

    public void TestCustomAttributesOnParameters()
    {
      var method = type.GetMethod("ParameterAttributeCheck");
      var attributes = method.GetParameters()[0].GetCustomAttributes(false);
    }

    public void TestCustomAttributesOnReturnParameters()
    {
      var method = type.GetMethod("ReturnParameterCheck");
      var returnParameterAttributes = method.ReturnParameter.GetCustomAttributes(false);
    }

    public void TestCustomAttributesOnProperties()
    {
      var propertyAttributes = type.GetProperty("BProperty").GetCustomAttributes(false);
    }

    public void TestCustomAttributesOnFields()
    {
      var fieldAttributes = type.GetField("BField").GetCustomAttributes(false);
    }

    public void TestCustomAttributesOnEvents()
    {
      var eventAttributes = type.GetEvent("MyEvent").GetCustomAttributes(false);
    }

    public void TestCustomAttributesOnEventMethods()
    {
      var eventMethod = type.GetEvent("MyEvent").GetAddMethod();
      var eventMethodAttributes = eventMethod.GetCustomAttributes(false);
    }

    public void TestCustomAttributesOnEventFields()
    {
      var field = type.GetField("MyEvent", BindingFlags.NonPublic | BindingFlags.Instance);
      var fieldAttributes = field.GetCustomAttributes(false);
    }

    public void TestCustomAttributesOnConstructors()
    {
      var constructorAttributes = type.GetConstructor(new Type[] { }).GetCustomAttributes(false);
    }

    public void TestCustomAttributesOnConstructorParameters()
    {
      var constructor = type.GetConstructor(new Type[] { typeof(int) });
      var parameters = constructor.GetParameters();
      var parameterAttributes = parameters[0].GetCustomAttributes(false);
    }
  }
}
