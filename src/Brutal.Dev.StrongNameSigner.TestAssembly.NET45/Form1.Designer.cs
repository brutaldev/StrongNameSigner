namespace Brutal.Dev.StrongNameSigner.TestAssembly.NET45
{
  partial class Form1
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
      this.pictureBox1 = new System.Windows.Forms.PictureBox();
      this.labelSetting2 = new System.Windows.Forms.Label();
      this.labelSetting1 = new System.Windows.Forms.Label();
      ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
      this.SuspendLayout();
      // 
      // pictureBox1
      // 
      this.pictureBox1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
      this.pictureBox1.Image = global::Brutal.Dev.StrongNameSigner.TestAssembly.NET45.Properties.Resources.BrutalLogo;
      this.pictureBox1.Location = new System.Drawing.Point(22, 52);
      this.pictureBox1.Name = "pictureBox1";
      this.pictureBox1.Size = new System.Drawing.Size(330, 300);
      this.pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
      this.pictureBox1.TabIndex = 5;
      this.pictureBox1.TabStop = false;
      // 
      // labelSetting2
      // 
      this.labelSetting2.AutoSize = true;
      this.labelSetting2.Location = new System.Drawing.Point(19, 36);
      this.labelSetting2.Name = "labelSetting2";
      this.labelSetting2.Size = new System.Drawing.Size(49, 13);
      this.labelSetting2.TabIndex = 4;
      this.labelSetting2.Text = "Setting 2";
      // 
      // labelSetting1
      // 
      this.labelSetting1.AutoSize = true;
      this.labelSetting1.Location = new System.Drawing.Point(20, 14);
      this.labelSetting1.Name = "labelSetting1";
      this.labelSetting1.Size = new System.Drawing.Size(49, 13);
      this.labelSetting1.TabIndex = 3;
      this.labelSetting1.Text = "Setting 1";
      // 
      // Form1
      // 
      this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
      this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
      this.ClientSize = new System.Drawing.Size(375, 376);
      this.Controls.Add(this.pictureBox1);
      this.Controls.Add(this.labelSetting2);
      this.Controls.Add(this.labelSetting1);
      this.Name = "Form1";
      this.Text = "Form1";
      this.Load += new System.EventHandler(this.Form1_Load);
      ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
      this.ResumeLayout(false);
      this.PerformLayout();

    }

    #endregion

    private System.Windows.Forms.PictureBox pictureBox1;
    private System.Windows.Forms.Label labelSetting2;
    private System.Windows.Forms.Label labelSetting1;

  }
}

