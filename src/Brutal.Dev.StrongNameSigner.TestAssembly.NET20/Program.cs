using System;
using System.Configuration;
using System.Windows.Forms;

namespace Brutal.Dev.StrongNameSigner.TestAssembly
{
  enum MyValues
  {
    V1
  }

  class Check : ConfigurationSection
  {
    const string PropName = "Name";

    // DefaultValue is a property of type object
    [ConfigurationProperty(PropName, DefaultValue = MyValues.V1)]
    public MyValues Value
    {
      get { return (MyValues)this[PropName]; }
    }
  }

  static class Program
  {
    /// <summary>
    /// The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main()
    {
      Check c = new Check();
      MyValues defValue = c.Value;  // get default value

      Application.EnableVisualStyles();
      Application.SetCompatibleTextRenderingDefault(false);
      Application.Run(new TestForm());
    }
  }
}
