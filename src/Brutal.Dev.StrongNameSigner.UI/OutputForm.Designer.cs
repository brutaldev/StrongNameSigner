namespace Brutal.Dev.StrongNameSigner.UI
{
  partial class OutputForm
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
      this.textBoxLog = new System.Windows.Forms.TextBox();
      this.SuspendLayout();
      // 
      // textBoxLog
      // 
      this.textBoxLog.BackColor = System.Drawing.SystemColors.Window;
      this.textBoxLog.Dock = System.Windows.Forms.DockStyle.Fill;
      this.textBoxLog.Location = new System.Drawing.Point(0, 0);
      this.textBoxLog.Multiline = true;
      this.textBoxLog.Name = "textBoxLog";
      this.textBoxLog.ReadOnly = true;
      this.textBoxLog.ScrollBars = System.Windows.Forms.ScrollBars.Both;
      this.textBoxLog.Size = new System.Drawing.Size(673, 494);
      this.textBoxLog.TabIndex = 0;
      // 
      // OutputForm
      // 
      this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
      this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
      this.ClientSize = new System.Drawing.Size(673, 494);
      this.Controls.Add(this.textBoxLog);
      this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
      this.MaximizeBox = false;
      this.MinimizeBox = false;
      this.MinimumSize = new System.Drawing.Size(400, 400);
      this.Name = "OutputForm";
      this.ShowInTaskbar = false;
      this.Text = "Output Log";
      this.ResumeLayout(false);
      this.PerformLayout();

    }

    #endregion

    private System.Windows.Forms.TextBox textBoxLog;
  }
}