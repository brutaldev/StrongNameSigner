using System.Windows.Forms;

namespace Brutal.Dev.StrongNameSigner.UI
{
  public partial class OutputForm : Form
  {
    public OutputForm(string logText)
    {
      InitializeComponent();
      textBoxLog.AppendText(logText);
    }
  }
}
