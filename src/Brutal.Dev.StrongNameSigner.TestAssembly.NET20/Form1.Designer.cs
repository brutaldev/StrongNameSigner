namespace Brutal.Dev.StrongNameSigner.TestAssembly
{
  partial class TestForm
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
      this.labelSetting1 = new System.Windows.Forms.Label();
      this.labelSetting2 = new System.Windows.Forms.Label();
      this.pictureBox1 = new System.Windows.Forms.PictureBox();
      ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
      this.SuspendLayout();
      // 
      // labelSetting1
      // 
      this.labelSetting1.AutoSize = true;
      this.labelSetting1.Location = new System.Drawing.Point(13, 13);
      this.labelSetting1.Name = "labelSetting1";
      this.labelSetting1.Size = new System.Drawing.Size(49, 13);
      this.labelSetting1.TabIndex = 0;
      this.labelSetting1.Text = "Setting 1";
      // 
      // labelSetting2
      // 
      this.labelSetting2.AutoSize = true;
      this.labelSetting2.Location = new System.Drawing.Point(12, 35);
      this.labelSetting2.Name = "labelSetting2";
      this.labelSetting2.Size = new System.Drawing.Size(49, 13);
      this.labelSetting2.TabIndex = 1;
      this.labelSetting2.Text = "Setting 2";
      // 
      // pictureBox1
      // 
      this.pictureBox1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
      this.pictureBox1.Image = global::Brutal.Dev.StrongNameSigner.TestAssembly.Properties.Resources.BrutalLogo;
      this.pictureBox1.Location = new System.Drawing.Point(12, 62);
      this.pictureBox1.Name = "pictureBox1";
      this.pictureBox1.Size = new System.Drawing.Size(330, 300);
      this.pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
      this.pictureBox1.TabIndex = 2;
      this.pictureBox1.TabStop = false;
      // 
      // TestForm
      // 
      this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
      this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
      this.ClientSize = new System.Drawing.Size(356, 382);
      this.Controls.Add(this.pictureBox1);
      this.Controls.Add(this.labelSetting2);
      this.Controls.Add(this.labelSetting1);
      this.Name = "TestForm";
      this.Text = "TestForm";
      this.Load += new System.EventHandler(this.TestForm_Load);
      ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
      this.ResumeLayout(false);
      this.PerformLayout();

    }

    #endregion

    private System.Windows.Forms.Label labelSetting1;
    private System.Windows.Forms.Label labelSetting2;
    private System.Windows.Forms.PictureBox pictureBox1;
  }
}

