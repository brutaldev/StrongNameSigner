using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using Microsoft.WindowsAPICodePack.Dialogs;

namespace Brutal.Dev.StrongNameSigner.UI
{
  public partial class MainForm : Form
  {
    private string keyFile = string.Empty;
    private string outputPath = string.Empty;
    private StringBuilder log = new StringBuilder();

    public MainForm()
    {
      InitializeComponent();

      Application.ThreadException += ApplicationThreadException;
      AppDomain.CurrentDomain.UnhandledException += CurrentDomainUnhandledException;
    }

    private static ListViewItem CreateListViewItem(AssemblyInfo info)
    {
      var item = new ListViewItem(new string[]
      {
        Path.GetFileName(info.FilePath),
        info.DotNetVersion,
        (info.IsAnyCpu ? "Any CPU" : info.Is32BitOnly ? "x86" : info.Is64BitOnly ? "x64" : "UNKNOWN") + (info.Is32BitPreferred ? " (x86 preferred)" : string.Empty),
        info.IsSigned ? "Yes" : "No",
        info.FilePath
      });

      item.Tag = info.FilePath;
      item.UseItemStyleForSubItems = false;

      // Update the color of the signed column.
      if (info.IsSigned)
      {
        item.SubItems[3].ForeColor = Color.Green;
      }
      else
      {
        item.SubItems[3].ForeColor = Color.Red;
      }

      return item;
    }

    private void MainForm_Load(object sender, EventArgs e)
    {
      Text = Text + " (" + Application.ProductVersion + ")";

      ResizeColumnWidths();

      textBoxKey.Focus();

      try
      {
        SigningHelper.CheckForRequiredSoftware();
      }
      catch (FileNotFoundException ex)
      {
        MessageBox.Show(ex.Message + Environment.NewLine + "Please ensure you have installed the Windows Software Development Kit (SDK).", "Required Software Missing", MessageBoxButtons.OK, MessageBoxIcon.Error);
        Application.Exit();
      }
    }

    private void ButtonKeyClick(object sender, EventArgs e)
    {
      if (CommonOpenFileDialog.IsPlatformSupported)
      {
        using (CommonOpenFileDialog dialog = new CommonOpenFileDialog())
        {
          dialog.Title = openFileDialogKey.Title;
          dialog.DefaultExtension = openFileDialogKey.DefaultExt;
          dialog.Filters.Add(new CommonFileDialogFilter("Key files", "*.snk;*.pfx"));
          dialog.EnsurePathExists = true;
          dialog.EnsureValidNames = true;
          dialog.IsFolderPicker = false;
          dialog.Multiselect = false;

          if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
          {
            string selectedFolder = dialog.FileName;
            while (!Directory.Exists(selectedFolder))
            {
              // Work around dialog bug in Vista.
              selectedFolder = Directory.GetParent(selectedFolder).FullName; 
            }

            textBoxKey.Text = dialog.FileName;
          }
        }
      }
      else
      {
        if (openFileDialogKey.ShowDialog() == DialogResult.OK)
        {
          textBoxKey.Text = openFileDialogKey.FileName;
        }
      }
    }

    private void ButtonOutputClick(object sender, EventArgs e)
    {
      if (CommonOpenFileDialog.IsPlatformSupported)
      {
        using (CommonOpenFileDialog dialog = new CommonOpenFileDialog())
        {
          dialog.Title = folderBrowserDialogOutput.Description;
          dialog.EnsurePathExists = true;
          dialog.EnsureValidNames = true;
          dialog.IsFolderPicker = true;
          dialog.Multiselect = false;

          if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
          {
            string selectedFolder = dialog.FileName;
            while (!Directory.Exists(selectedFolder))
            {
              // Work around dialog bug in Vista.
              selectedFolder = Directory.GetParent(selectedFolder).FullName;
            }

            textBoxOutput.Text = selectedFolder;
          }
        }
      }
      else
      {
        if (folderBrowserDialogOutput.ShowDialog() == DialogResult.OK)
        {
          textBoxOutput.Text = folderBrowserDialogOutput.SelectedPath;
        }
      }
    }

    private void ButtonCancelClick(object sender, EventArgs e)
    {
      buttonCancel.Enabled = false;
      backgroundWorker.CancelAsync();
    }

    private void ListViewAssembliesSelectedIndexChanged(object sender, EventArgs e)
    {
      buttonRemove.Enabled = listViewAssemblies.SelectedItems.Count > 0;
    }

    private void ListViewAssembliesDragEnter(object sender, DragEventArgs e)
    {
      if (e.Data.GetDataPresent(DataFormats.FileDrop))
      {
        e.Effect = DragDropEffects.Copy;
      }
      else
      {
        e.Effect = DragDropEffects.None;
      }
    }

    private void ListViewAssembliesDragDrop(object sender, DragEventArgs e)
    {
      var data = e.Data.GetData(DataFormats.FileDrop) as IEnumerable<string>;

      if (data != null)
      {
        var assemblies = data.Where(d => (Path.GetExtension(d).Equals(".exe", StringComparison.OrdinalIgnoreCase) ||
                                     Path.GetExtension(d).Equals(".dll", StringComparison.OrdinalIgnoreCase)) &&
                                     File.Exists(d)).ToList();

        // Add all files in directories.
        var directories = data.Where(d => Directory.Exists(d) && File.GetAttributes(d).HasFlag(FileAttributes.Directory)).ToList();
        directories.ForEach(d => assemblies.AddRange(Directory.GetFiles(d, "*.exe", SearchOption.AllDirectories)));
        directories.ForEach(d => assemblies.AddRange(Directory.GetFiles(d, "*.dll", SearchOption.AllDirectories)));

        foreach (var assembly in assemblies)
        {
          try
          {
            AddAssemblyToList(SigningHelper.GetAssemblyInfo(assembly));
          }
          catch (InvalidOperationException)
          {
            MessageBox.Show(string.Format(CultureInfo.CurrentCulture, "Could not get assembly info for '{0}'. This may not be a .NET assembly.", assembly), "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
          }
        }

        ResizeColumnWidths();

        buttonSign.Enabled = listViewAssemblies.Items.Count > 0;
      }
    }

    private void ListViewAssembliesKeyDown(object sender, KeyEventArgs e)
    {
      if (e.KeyCode == Keys.Delete)
      {
        buttonRemove.PerformClick();
        e.Handled = true;
      }
      else if (e.Modifiers == Keys.Control && e.KeyCode == Keys.A)
      {
        foreach (ListViewItem item in listViewAssemblies.Items)
        {
          item.Selected = true;
        }

        e.Handled = true;
      }
      else if (e.KeyCode == Keys.Insert)
      {
        buttonAdd.PerformClick();
        e.Handled = true;
      }
    }

    private void ButtonAddClick(object sender, EventArgs e)
    {
      if (CommonOpenFileDialog.IsPlatformSupported)
      {
        using (CommonOpenFileDialog dialog = new CommonOpenFileDialog())
        {
          dialog.Title = openFileDialogAssembly.Title;
          dialog.DefaultExtension = openFileDialogAssembly.DefaultExt;
          dialog.Filters.Add(new CommonFileDialogFilter("Assembly files", "*.exe;*.dll"));
          dialog.EnsurePathExists = true;
          dialog.EnsureValidNames = true;
          dialog.IsFolderPicker = false;
          dialog.Multiselect = true;

          if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
          {
            foreach (var file in dialog.FileNames)
            {
              try
              {
                AddAssemblyToList(SigningHelper.GetAssemblyInfo(file));
              }
              catch (InvalidOperationException)
              {
                MessageBox.Show(string.Format(CultureInfo.CurrentCulture, "Could not get assembly info for '{0}'. This may not be a .NET assembly.", file), "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
              }
            }
          }
        }
      }
      else
      {
        if (openFileDialogAssembly.ShowDialog() == DialogResult.OK)
        {
          foreach (var file in openFileDialogAssembly.FileNames)
          {
            try
            {
              AddAssemblyToList(SigningHelper.GetAssemblyInfo(file));
            }
            catch (InvalidOperationException)
            {
              MessageBox.Show(string.Format(CultureInfo.CurrentCulture, "Could not get assembly info for '{0}'. This may not be a .NET assembly.", file), "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
          }
        }
      }

      ResizeColumnWidths();

      buttonSign.Enabled = listViewAssemblies.Items.Count > 0;
    }

    private void ButtonRemoveClick(object sender, EventArgs e)
    {
      for (int i = listViewAssemblies.SelectedIndices.Count - 1; i >= 0; i--)
      {
        listViewAssemblies.Items.RemoveAt(listViewAssemblies.SelectedIndices[i]);
      }

      ResizeColumnWidths();

      buttonSign.Enabled = listViewAssemblies.Items.Count > 0;
    }

    private void ButtonSignClick(object sender, EventArgs e)
    {
      keyFile = string.Empty;
      outputPath = string.Empty;
      log.Clear();

      labelInfo.Text = string.Empty;
      linkLabelLog.Visible = false;
      progressBar.Value = 0;

      if (!string.IsNullOrWhiteSpace(textBoxKey.Text) && !File.Exists(textBoxKey.Text))
      {
        MessageBox.Show(string.Format(CultureInfo.CurrentCulture, "The key file '{0}' does not exist. Leave the field blank to have a key generated for you.", textBoxKey.Text), "Missing Key File", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        textBoxKey.SelectAll();
        textBoxKey.Focus();
      }
      else
      {
        keyFile = textBoxKey.Text.Trim();
      }

      if (!string.IsNullOrWhiteSpace(textBoxOutput.Text) && !Directory.Exists(textBoxOutput.Text))
      {
        MessageBox.Show(string.Format(CultureInfo.CurrentCulture, "The output directory '{0}' does not exist. Leave the field blank to overwrite the existing assemblies, a backup of each file will be made.", textBoxOutput.Text), "Missing Output Directory", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        textBoxOutput.SelectAll();
        textBoxOutput.Focus();
      }
      else
      {
        outputPath = textBoxOutput.Text.Trim();
      }

      var assemblies = new List<string>();
      foreach (ListViewItem item in listViewAssemblies.Items)
      {
        assemblies.Add(item.Tag.ToString());
      }

      if (assemblies.Count > 0)
      {
        EnableControls(false);
        backgroundWorker.RunWorkerAsync(assemblies);
      }
    }

    private void LinkLabelLogLinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
    {
      using (var outputLog = new OutputForm(log.ToString()))
      {
        outputLog.ShowDialog();
      }
    }

    private void BackgroundWorkerDoWork(object sender, DoWorkEventArgs e)
    {
      e.Result = string.Empty;

      int progress = 0;
      int count = 0;
      var assemblies = e.Argument as IEnumerable<string>;

      if (assemblies != null)
      {
        foreach (var assembly in assemblies)
        {
          AssemblyInfo info = null;

          try
          {
#if DEBUG
            System.Threading.Thread.Sleep(2000);
#endif
            info = SigningHelper.SignAssembly(assembly, keyFile, outputPath, s => log.AppendLine(s));
            count++;
          }
          catch (Exception ex)
          {
            log.AppendFormat("Error strong-name signing {0}: {1}", assembly, ex.Message).AppendLine();
          }

          backgroundWorker.ReportProgress(Convert.ToInt32((++progress / (double)assemblies.Count()) * 100), info);

          if (backgroundWorker.CancellationPending)
          {
            e.Cancel = true;
            break;
          }
        }

        e.Result = string.Format(CultureInfo.CurrentCulture, "{0} out of {1} assemblies were strong-name signed successfully.", count, assemblies.Count());
      }
    }

    private void BackgroundWorkerProgressChanged(object sender, ProgressChangedEventArgs e)
    {
      progressBar.Value = e.ProgressPercentage;

      // Update the item in the list with it's new values.
      UpdateAssemblyInList(e.UserState as AssemblyInfo);
    }

    private void BackgroundWorkerRunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
    {
      progressBar.Value = 0;
      labelInfo.Text = string.Empty;
      linkLabelLog.Visible = true;
      EnableControls(true);

      if (!e.Cancelled)
      {
        labelInfo.Text = e.Result.ToString();
      }

      if (e.Error != null)
      {
        MessageBox.Show("An error occurred trying to strong-name sign the assemblies: " + e.Error.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
      }
    }

    private void ApplicationThreadException(object sender, ThreadExceptionEventArgs e)
    {
      MessageBox.Show(e.Exception.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }

    private void CurrentDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
      MessageBox.Show((e.ExceptionObject as Exception).Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

      if (e.IsTerminating)
      {
        Application.Exit();
      }
    }

    private void ResizeColumnWidths()
    {
      columnHeaderFileName.Width = -2;
      columnHeaderVersion.Width = -2;
      columnHeaderFileType.Width = -2;
      columnHeaderIsSigned.Width = -2;
      columnHeaderPath.Width = -2;
    }

    private void EnableControls(bool enabled)
    {
      buttonCancel.Visible = !enabled;
      buttonCancel.Enabled = !enabled;

      textBoxKey.Enabled = enabled;
      textBoxOutput.Enabled = enabled;
      listViewAssemblies.Enabled = enabled;
      buttonKey.Enabled = enabled;
      buttonOutput.Enabled = enabled;
      buttonAdd.Enabled = enabled;
      buttonRemove.Enabled = enabled && listViewAssemblies.SelectedItems.Count > 0;
      buttonSign.Enabled = enabled;
    }

    private void UpdateAssemblyInList(AssemblyInfo info)
    {
      if (info != null)
      {
        for (int i = 0; i < listViewAssemblies.Items.Count; i++)
        {
          if (listViewAssemblies.Items[i].Tag.ToString().Equals(info.FilePath, StringComparison.OrdinalIgnoreCase))
          {
            listViewAssemblies.Items[i] = CreateListViewItem(info);
            break;
          }
        }
      }
    }

    private void AddAssemblyToList(AssemblyInfo info)
    {
      if (info != null && info.IsManagedAssembly)
      {
        listViewAssemblies.Items.Add(CreateListViewItem(info));
      }
    }
  }
}
