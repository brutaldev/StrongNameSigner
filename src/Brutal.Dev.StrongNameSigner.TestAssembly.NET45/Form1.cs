using Brutal.Dev.StrongNameSigner.TestAssembly.NET45.Properties;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Brutal.Dev.StrongNameSigner.TestAssembly.NET45
{
  public partial class Form1 : Form
  {
    public Form1()
    {
      InitializeComponent();
    }

    private void Form1_Load(object sender, EventArgs e)
    {
      labelSetting1.Text = Settings.Default.Test1.ToString();
      labelSetting2.Text = Settings.Default.Test2;
    }
  }
}
