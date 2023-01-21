namespace Brutal.Dev.StrongNameSigner.UI
{
  partial class MainForm
  {
    /// <summary>
    /// Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;

    /// <summary>
    /// Clean up any resources being used.
    /// </summary>
    /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
    protected override void Dispose(bool disposing)
    {
      if (disposing && (components != null))
      {
        components.Dispose();
      }
      base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    /// <summary>
    /// Required method for Designer support - do not modify
    /// the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
      System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
      this.progressBar = new System.Windows.Forms.ProgressBar();
      this.backgroundWorker = new System.ComponentModel.BackgroundWorker();
      this.textBoxOutput = new System.Windows.Forms.TextBox();
      this.textBoxKey = new System.Windows.Forms.TextBox();
      this.labelKeyInstruction = new System.Windows.Forms.Label();
      this.labelOutputInstruction = new System.Windows.Forms.Label();
      this.openFileDialogKey = new System.Windows.Forms.OpenFileDialog();
      this.listViewAssemblies = new System.Windows.Forms.ListView();
      this.columnHeaderFileName = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
      this.columnHeaderVersion = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
      this.columnHeaderFileType = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
      this.columnHeaderIsSigned = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
      this.columnHeaderPath = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
      this.buttonAdd = new System.Windows.Forms.Button();
      this.buttonRemove = new System.Windows.Forms.Button();
      this.buttonSign = new System.Windows.Forms.Button();
      this.labelOutput = new System.Windows.Forms.Label();
      this.labelKey = new System.Windows.Forms.Label();
      this.buttonKey = new System.Windows.Forms.Button();
      this.buttonOutput = new System.Windows.Forms.Button();
      this.labelAssembliesInstruction = new System.Windows.Forms.Label();
      this.labelInfo = new System.Windows.Forms.Label();
      this.folderBrowserDialogOutput = new System.Windows.Forms.FolderBrowserDialog();
      this.openFileDialogAssembly = new System.Windows.Forms.OpenFileDialog();
      this.buttonCancel = new System.Windows.Forms.Button();
      this.linkLabelLog = new System.Windows.Forms.LinkLabel();
      this.labelPassword = new System.Windows.Forms.Label();
      this.textBoxPassword = new System.Windows.Forms.TextBox();
      this.labelPasswordInstruction = new System.Windows.Forms.Label();
      this.SuspendLayout();
      // 
      // progressBar
      // 
      this.progressBar.Dock = System.Windows.Forms.DockStyle.Bottom;
      this.progressBar.Location = new System.Drawing.Point(0, 678);
      this.progressBar.Margin = new System.Windows.Forms.Padding(4);
      this.progressBar.Name = "progressBar";
      this.progressBar.Size = new System.Drawing.Size(1045, 12);
      this.progressBar.Style = System.Windows.Forms.ProgressBarStyle.Continuous;
      this.progressBar.TabIndex = 16;
      // 
      // backgroundWorker
      // 
      this.backgroundWorker.WorkerReportsProgress = true;
      this.backgroundWorker.WorkerSupportsCancellation = true;
      this.backgroundWorker.DoWork += new System.ComponentModel.DoWorkEventHandler(this.BackgroundWorkerDoWork);
      this.backgroundWorker.ProgressChanged += new System.ComponentModel.ProgressChangedEventHandler(this.BackgroundWorkerProgressChanged);
      this.backgroundWorker.RunWorkerCompleted += new System.ComponentModel.RunWorkerCompletedEventHandler(this.BackgroundWorkerRunWorkerCompleted);
      // 
      // textBoxOutput
      // 
      this.textBoxOutput.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
      this.textBoxOutput.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.Suggest;
      this.textBoxOutput.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.FileSystemDirectories;
      this.textBoxOutput.Location = new System.Drawing.Point(111, 181);
      this.textBoxOutput.Margin = new System.Windows.Forms.Padding(4);
      this.textBoxOutput.Name = "textBoxOutput";
      this.textBoxOutput.Size = new System.Drawing.Size(853, 22);
      this.textBoxOutput.TabIndex = 9;
      // 
      // textBoxKey
      // 
      this.textBoxKey.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
      this.textBoxKey.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.Suggest;
      this.textBoxKey.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.FileSystem;
      this.textBoxKey.Location = new System.Drawing.Point(111, 47);
      this.textBoxKey.Margin = new System.Windows.Forms.Padding(4);
      this.textBoxKey.Name = "textBoxKey";
      this.textBoxKey.Size = new System.Drawing.Size(853, 22);
      this.textBoxKey.TabIndex = 2;
      this.textBoxKey.TextChanged += new System.EventHandler(this.TextBoxKeyTextChanged);
      // 
      // labelKeyInstruction
      // 
      this.labelKeyInstruction.AutoSize = true;
      this.labelKeyInstruction.Location = new System.Drawing.Point(15, 22);
      this.labelKeyInstruction.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
      this.labelKeyInstruction.Name = "labelKeyInstruction";
      this.labelKeyInstruction.Size = new System.Drawing.Size(829, 16);
      this.labelKeyInstruction.TabIndex = 0;
      this.labelKeyInstruction.Text = "Select your own key file to strong-name sign the assemblies with. If you do not p" +
    "rovide one, a key file will be automatically generated for you.";
      // 
      // labelOutputInstruction
      // 
      this.labelOutputInstruction.AutoSize = true;
      this.labelOutputInstruction.Location = new System.Drawing.Point(16, 156);
      this.labelOutputInstruction.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
      this.labelOutputInstruction.Name = "labelOutputInstruction";
      this.labelOutputInstruction.Size = new System.Drawing.Size(824, 16);
      this.labelOutputInstruction.TabIndex = 7;
      this.labelOutputInstruction.Text = "Select the output directory for all your strong-name signed assemblies. If you do" +
    " not provide one, the files will be overwritten (with a backup).";
      // 
      // openFileDialogKey
      // 
      this.openFileDialogKey.Filter = "Key files|*.snk;*.pfx";
      this.openFileDialogKey.Title = "Select key file...";
      // 
      // listViewAssemblies
      // 
      this.listViewAssemblies.AllowDrop = true;
      this.listViewAssemblies.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
      this.listViewAssemblies.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeaderFileName,
            this.columnHeaderVersion,
            this.columnHeaderFileType,
            this.columnHeaderIsSigned,
            this.columnHeaderPath});
      this.listViewAssemblies.FullRowSelect = true;
      this.listViewAssemblies.GridLines = true;
      this.listViewAssemblies.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;
      this.listViewAssemblies.HideSelection = false;
      this.listViewAssemblies.Location = new System.Drawing.Point(16, 246);
      this.listViewAssemblies.Margin = new System.Windows.Forms.Padding(4);
      this.listViewAssemblies.Name = "listViewAssemblies";
      this.listViewAssemblies.Size = new System.Drawing.Size(948, 368);
      this.listViewAssemblies.TabIndex = 12;
      this.listViewAssemblies.UseCompatibleStateImageBehavior = false;
      this.listViewAssemblies.View = System.Windows.Forms.View.Details;
      this.listViewAssemblies.SelectedIndexChanged += new System.EventHandler(this.ListViewAssembliesSelectedIndexChanged);
      this.listViewAssemblies.DragDrop += new System.Windows.Forms.DragEventHandler(this.ListViewAssembliesDragDrop);
      this.listViewAssemblies.DragEnter += new System.Windows.Forms.DragEventHandler(this.ListViewAssembliesDragEnter);
      this.listViewAssemblies.KeyDown += new System.Windows.Forms.KeyEventHandler(this.ListViewAssembliesKeyDown);
      // 
      // columnHeaderFileName
      // 
      this.columnHeaderFileName.Text = "Assembly";
      // 
      // columnHeaderVersion
      // 
      this.columnHeaderVersion.Text = ".NET Version";
      // 
      // columnHeaderFileType
      // 
      this.columnHeaderFileType.Text = "Type";
      // 
      // columnHeaderIsSigned
      // 
      this.columnHeaderIsSigned.Text = "Strong-named";
      // 
      // columnHeaderPath
      // 
      this.columnHeaderPath.Text = "Full Path";
      // 
      // buttonAdd
      // 
      this.buttonAdd.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
      this.buttonAdd.Image = global::Brutal.Dev.StrongNameSigner.UI.Properties.Resources.Add;
      this.buttonAdd.Location = new System.Drawing.Point(973, 246);
      this.buttonAdd.Margin = new System.Windows.Forms.Padding(4);
      this.buttonAdd.Name = "buttonAdd";
      this.buttonAdd.Size = new System.Drawing.Size(53, 49);
      this.buttonAdd.TabIndex = 13;
      this.buttonAdd.UseVisualStyleBackColor = true;
      this.buttonAdd.Click += new System.EventHandler(this.ButtonAddClick);
      // 
      // buttonRemove
      // 
      this.buttonRemove.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
      this.buttonRemove.Enabled = false;
      this.buttonRemove.Image = global::Brutal.Dev.StrongNameSigner.UI.Properties.Resources.Remove;
      this.buttonRemove.Location = new System.Drawing.Point(973, 303);
      this.buttonRemove.Margin = new System.Windows.Forms.Padding(4);
      this.buttonRemove.Name = "buttonRemove";
      this.buttonRemove.Size = new System.Drawing.Size(53, 49);
      this.buttonRemove.TabIndex = 14;
      this.buttonRemove.UseVisualStyleBackColor = true;
      this.buttonRemove.Click += new System.EventHandler(this.ButtonRemoveClick);
      // 
      // buttonSign
      // 
      this.buttonSign.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
      this.buttonSign.Enabled = false;
      this.buttonSign.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
      this.buttonSign.Location = new System.Drawing.Point(16, 623);
      this.buttonSign.Margin = new System.Windows.Forms.Padding(4);
      this.buttonSign.Name = "buttonSign";
      this.buttonSign.Size = new System.Drawing.Size(239, 47);
      this.buttonSign.TabIndex = 12;
      this.buttonSign.Text = "Sign Assemblies";
      this.buttonSign.UseVisualStyleBackColor = true;
      this.buttonSign.Click += new System.EventHandler(this.ButtonSignClick);
      // 
      // labelOutput
      // 
      this.labelOutput.AutoSize = true;
      this.labelOutput.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
      this.labelOutput.Location = new System.Drawing.Point(16, 185);
      this.labelOutput.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
      this.labelOutput.Name = "labelOutput";
      this.labelOutput.Size = new System.Drawing.Size(62, 17);
      this.labelOutput.TabIndex = 8;
      this.labelOutput.Text = "Output:";
      // 
      // labelKey
      // 
      this.labelKey.AutoSize = true;
      this.labelKey.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
      this.labelKey.Location = new System.Drawing.Point(16, 50);
      this.labelKey.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
      this.labelKey.Name = "labelKey";
      this.labelKey.Size = new System.Drawing.Size(71, 17);
      this.labelKey.TabIndex = 1;
      this.labelKey.Text = "Key File:";
      // 
      // buttonKey
      // 
      this.buttonKey.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
      this.buttonKey.Location = new System.Drawing.Point(973, 44);
      this.buttonKey.Margin = new System.Windows.Forms.Padding(4);
      this.buttonKey.Name = "buttonKey";
      this.buttonKey.Size = new System.Drawing.Size(53, 28);
      this.buttonKey.TabIndex = 3;
      this.buttonKey.Text = "...";
      this.buttonKey.UseVisualStyleBackColor = true;
      this.buttonKey.Click += new System.EventHandler(this.ButtonKeyClick);
      // 
      // buttonOutput
      // 
      this.buttonOutput.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
      this.buttonOutput.Location = new System.Drawing.Point(973, 178);
      this.buttonOutput.Margin = new System.Windows.Forms.Padding(4);
      this.buttonOutput.Name = "buttonOutput";
      this.buttonOutput.Size = new System.Drawing.Size(53, 28);
      this.buttonOutput.TabIndex = 10;
      this.buttonOutput.Text = "...";
      this.buttonOutput.UseVisualStyleBackColor = true;
      this.buttonOutput.Click += new System.EventHandler(this.ButtonOutputClick);
      // 
      // labelAssembliesInstruction
      // 
      this.labelAssembliesInstruction.AutoSize = true;
      this.labelAssembliesInstruction.Location = new System.Drawing.Point(16, 222);
      this.labelAssembliesInstruction.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
      this.labelAssembliesInstruction.Name = "labelAssembliesInstruction";
      this.labelAssembliesInstruction.Size = new System.Drawing.Size(721, 16);
      this.labelAssembliesInstruction.TabIndex = 11;
      this.labelAssembliesInstruction.Text = "Drag-and-drop files/directories or use the buttons provided to add and remove ass" +
    "emblies you want to strong-name sign.";
      // 
      // labelInfo
      // 
      this.labelInfo.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
      this.labelInfo.AutoEllipsis = true;
      this.labelInfo.Location = new System.Drawing.Point(273, 640);
      this.labelInfo.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
      this.labelInfo.Name = "labelInfo";
      this.labelInfo.Size = new System.Drawing.Size(564, 16);
      this.labelInfo.TabIndex = 16;
      // 
      // folderBrowserDialogOutput
      // 
      this.folderBrowserDialogOutput.Description = "Select the output folder where strong-named assemblies will be copied to.";
      // 
      // openFileDialogAssembly
      // 
      this.openFileDialogAssembly.AddExtension = false;
      this.openFileDialogAssembly.Filter = "Assembly files|*.exe;*.dll";
      this.openFileDialogAssembly.Multiselect = true;
      this.openFileDialogAssembly.Title = "Select assemblies...";
      // 
      // buttonCancel
      // 
      this.buttonCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
      this.buttonCancel.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
      this.buttonCancel.Location = new System.Drawing.Point(16, 623);
      this.buttonCancel.Margin = new System.Windows.Forms.Padding(4);
      this.buttonCancel.Name = "buttonCancel";
      this.buttonCancel.Size = new System.Drawing.Size(239, 47);
      this.buttonCancel.TabIndex = 15;
      this.buttonCancel.Text = "Cancel";
      this.buttonCancel.UseVisualStyleBackColor = true;
      this.buttonCancel.Visible = false;
      this.buttonCancel.Click += new System.EventHandler(this.ButtonCancelClick);
      // 
      // linkLabelLog
      // 
      this.linkLabelLog.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
      this.linkLabelLog.AutoSize = true;
      this.linkLabelLog.Location = new System.Drawing.Point(845, 640);
      this.linkLabelLog.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
      this.linkLabelLog.Name = "linkLabelLog";
      this.linkLabelLog.Size = new System.Drawing.Size(107, 16);
      this.linkLabelLog.TabIndex = 17;
      this.linkLabelLog.TabStop = true;
      this.linkLabelLog.Text = "Show Output Log";
      this.linkLabelLog.Visible = false;
      this.linkLabelLog.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.LinkLabelLogLinkClicked);
      // 
      // labelPassword
      // 
      this.labelPassword.AutoSize = true;
      this.labelPassword.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
      this.labelPassword.Location = new System.Drawing.Point(16, 118);
      this.labelPassword.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
      this.labelPassword.Name = "labelPassword";
      this.labelPassword.Size = new System.Drawing.Size(82, 17);
      this.labelPassword.TabIndex = 5;
      this.labelPassword.Text = "Password:";
      // 
      // textBoxPassword
      // 
      this.textBoxPassword.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
      this.textBoxPassword.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.Suggest;
      this.textBoxPassword.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.FileSystem;
      this.textBoxPassword.Location = new System.Drawing.Point(111, 114);
      this.textBoxPassword.Margin = new System.Windows.Forms.Padding(4);
      this.textBoxPassword.Name = "textBoxPassword";
      this.textBoxPassword.Size = new System.Drawing.Size(853, 22);
      this.textBoxPassword.TabIndex = 6;
      this.textBoxPassword.UseSystemPasswordChar = true;
      // 
      // labelPasswordInstruction
      // 
      this.labelPasswordInstruction.AutoSize = true;
      this.labelPasswordInstruction.Location = new System.Drawing.Point(15, 90);
      this.labelPasswordInstruction.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
      this.labelPasswordInstruction.Name = "labelPasswordInstruction";
      this.labelPasswordInstruction.Size = new System.Drawing.Size(604, 16);
      this.labelPasswordInstruction.TabIndex = 4;
      this.labelPasswordInstruction.Text = "Provide a password (if any) for the key file that you provide. PFX file are norma" +
    "lly password protected.";
      // 
      // MainForm
      // 
      this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
      this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
      this.ClientSize = new System.Drawing.Size(1045, 690);
      this.Controls.Add(this.textBoxPassword);
      this.Controls.Add(this.labelPasswordInstruction);
      this.Controls.Add(this.linkLabelLog);
      this.Controls.Add(this.buttonCancel);
      this.Controls.Add(this.labelInfo);
      this.Controls.Add(this.labelAssembliesInstruction);
      this.Controls.Add(this.buttonOutput);
      this.Controls.Add(this.buttonKey);
      this.Controls.Add(this.labelKey);
      this.Controls.Add(this.labelOutput);
      this.Controls.Add(this.labelOutputInstruction);
      this.Controls.Add(this.buttonSign);
      this.Controls.Add(this.textBoxKey);
      this.Controls.Add(this.buttonRemove);
      this.Controls.Add(this.buttonAdd);
      this.Controls.Add(this.listViewAssemblies);
      this.Controls.Add(this.labelKeyInstruction);
      this.Controls.Add(this.textBoxOutput);
      this.Controls.Add(this.progressBar);
      this.Controls.Add(this.labelPassword);
      this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
      this.Margin = new System.Windows.Forms.Padding(4);
      this.MinimumSize = new System.Drawing.Size(1034, 500);
      this.Name = "MainForm";
      this.Text = "Brutal Developer .NET Assembly Strong-Name Signer";
      this.Load += new System.EventHandler(this.MainFormLoad);
      this.ResumeLayout(false);
      this.PerformLayout();

    }

    #endregion

    private System.Windows.Forms.ProgressBar progressBar;
    private System.ComponentModel.BackgroundWorker backgroundWorker;
    private System.Windows.Forms.TextBox textBoxOutput;
    private System.Windows.Forms.TextBox textBoxKey;
    private System.Windows.Forms.Label labelKeyInstruction;
    private System.Windows.Forms.Label labelOutputInstruction;
    private System.Windows.Forms.OpenFileDialog openFileDialogKey;
    private System.Windows.Forms.ListView listViewAssemblies;
    private System.Windows.Forms.Label labelKey;
    private System.Windows.Forms.Label labelOutput;
    private System.Windows.Forms.Button buttonSign;
    private System.Windows.Forms.Button buttonRemove;
    private System.Windows.Forms.Button buttonAdd;
    private System.Windows.Forms.Button buttonKey;
    private System.Windows.Forms.Button buttonOutput;
    private System.Windows.Forms.Label labelAssembliesInstruction;
    private System.Windows.Forms.Label labelInfo;
    private System.Windows.Forms.FolderBrowserDialog folderBrowserDialogOutput;
    private System.Windows.Forms.ColumnHeader columnHeaderFileName;
    private System.Windows.Forms.ColumnHeader columnHeaderFileType;
    private System.Windows.Forms.ColumnHeader columnHeaderIsSigned;
    private System.Windows.Forms.ColumnHeader columnHeaderPath;
    private System.Windows.Forms.OpenFileDialog openFileDialogAssembly;
    private System.Windows.Forms.ColumnHeader columnHeaderVersion;
    private System.Windows.Forms.Button buttonCancel;
    private System.Windows.Forms.LinkLabel linkLabelLog;
    private System.Windows.Forms.Label labelPassword;
    private System.Windows.Forms.TextBox textBoxPassword;
    private System.Windows.Forms.Label labelPasswordInstruction;
  }
}

