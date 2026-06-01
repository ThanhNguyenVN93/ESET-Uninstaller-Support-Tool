namespace frmesetunins
{
    partial class Form1 : System.Windows.Forms.Form
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null)
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            this.AutoScaleMode       = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.ClientSize          = new System.Drawing.Size(500, 200);
            this.Name                = "Form1";
            this.Text                = "Safe Mode Uninstaller";
            this.StartPosition       = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.BackColor           = System.Drawing.Color.White;
            this.Font                = new System.Drawing.Font("Segoe UI", 9.75F);
            this.FormBorderStyle     = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox         = false;
            this.MinimizeBox         = false;

            this.ResumeLayout(false);
        }
    }
}
