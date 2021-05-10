using System;
using System.Windows.Forms;
using Brutal.Dev.StrongNameSigner.TestAssembly.Properties;

namespace Brutal.Dev.StrongNameSigner.TestAssembly
{
  public partial class TestForm : Form
  {
    public TestForm()
    {
      InitializeComponent();
    }

    private void TestForm_Load(object sender, EventArgs e)
    {
      labelSetting1.Text = Settings.Default.Test1.ToString();
      labelSetting2.Text = Settings.Default.Test2;
    }
  }
}
