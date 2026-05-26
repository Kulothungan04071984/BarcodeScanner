using System.Drawing;
using System.Windows.Forms;

namespace BarcodeScanner
{
    partial class FinalBarcode
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
            this.components = new System.ComponentModel.Container();
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Text = "FinalBarcode";
            this.Size = new Size(1200, 860);
            this.MinimumSize = new Size(960, 700);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = C_BG;
            this.ForeColor = C_TEXT;
            this.Font = new Font("Segoe UI", 9.5f);
            this.DoubleBuffered = true;

            BuildSidebar();
            BuildMain();

            this.Controls.Add(panelMain);
            this.Controls.Add(panelSidebar);
        }

        #endregion
    }
}