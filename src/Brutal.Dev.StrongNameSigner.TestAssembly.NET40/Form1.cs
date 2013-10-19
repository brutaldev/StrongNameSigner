using Brutal.Dev.StrongNameSigner.TestAssembly.Properties;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

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
