using System;
using System.Configuration;
using System.Diagnostics;
using System.Windows.Forms;

[assembly: System.Windows.ThemeInfo(System.Windows.ResourceDictionaryLocation.None, System.Windows.ResourceDictionaryLocation.None)]
[assembly: Debuggable(DebuggableAttribute.DebuggingModes.Default)]

namespace Brutal.Dev.StrongNameSigner.TestAssembly
{
  static class Program
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

    /// <summary>
    /// The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main()
    {
      var c = new Check();
      var defValue = c.Value;  // get default value

      if (defValue == MyValues.V1)
      {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new TestForm());
      }
    }
  }
}
