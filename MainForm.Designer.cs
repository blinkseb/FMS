namespace FMS
{
    partial class MainForm
    {
        /// <summary>
        /// Variable nécessaire au concepteur.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Nettoyage des ressources utilisées.
        /// </summary>
        /// <param name="disposing">true si les ressources managées doivent être supprimées ; sinon, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Code généré par le Concepteur Windows Form

        /// <summary>
        /// Méthode requise pour la prise en charge du concepteur - ne modifiez pas
        /// le contenu de cette méthode avec l'éditeur de code.
        /// </summary>
        private void InitializeComponent()
        {
      this.txtFolder = new System.Windows.Forms.TextBox();
      this.button1 = new System.Windows.Forms.Button();
      this.folderBrowser = new System.Windows.Forms.FolderBrowserDialog();
      this.btnStart = new System.Windows.Forms.Button();
      this.txtLog = new System.Windows.Forms.TextBox();
      this.btnDump = new System.Windows.Forms.Button();
      this.btnDumpAndSort = new System.Windows.Forms.Button();
      this.SuspendLayout();
      // 
      // txtFolder
      // 
      this.txtFolder.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
      this.txtFolder.Location = new System.Drawing.Point(12, 12);
      this.txtFolder.Name = "txtFolder";
      this.txtFolder.Size = new System.Drawing.Size(700, 20);
      this.txtFolder.TabIndex = 0;
      this.txtFolder.TextChanged += new System.EventHandler(this.txtFolder_TextChanged);
      // 
      // button1
      // 
      this.button1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
      this.button1.Location = new System.Drawing.Point(718, 10);
      this.button1.Name = "button1";
      this.button1.Size = new System.Drawing.Size(61, 23);
      this.button1.TabIndex = 1;
      this.button1.Text = "Search";
      this.button1.UseVisualStyleBackColor = true;
      this.button1.Click += new System.EventHandler(this.button1_Click_1);
      // 
      // folderBrowser
      // 
      this.folderBrowser.RootFolder = System.Environment.SpecialFolder.MyComputer;
      this.folderBrowser.ShowNewFolderButton = false;
      // 
      // btnStart
      // 
      this.btnStart.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
      this.btnStart.Enabled = false;
      this.btnStart.Location = new System.Drawing.Point(785, 9);
      this.btnStart.Name = "btnStart";
      this.btnStart.Size = new System.Drawing.Size(56, 24);
      this.btnStart.TabIndex = 2;
      this.btnStart.Text = "Start";
      this.btnStart.UseVisualStyleBackColor = true;
      this.btnStart.Click += new System.EventHandler(this.btnStart_Click);
      // 
      // txtLog
      // 
      this.txtLog.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
      this.txtLog.Font = new System.Drawing.Font("Lucida Console", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
      this.txtLog.Location = new System.Drawing.Point(12, 62);
      this.txtLog.Multiline = true;
      this.txtLog.Name = "txtLog";
      this.txtLog.ReadOnly = true;
      this.txtLog.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
      this.txtLog.Size = new System.Drawing.Size(829, 352);
      this.txtLog.TabIndex = 3;
      // 
      // btnDump
      // 
      this.btnDump.Enabled = false;
      this.btnDump.Location = new System.Drawing.Point(12, 32);
      this.btnDump.Name = "btnDump";
      this.btnDump.Size = new System.Drawing.Size(88, 24);
      this.btnDump.TabIndex = 4;
      this.btnDump.Text = "Dump FAT";
      this.btnDump.UseVisualStyleBackColor = true;
      this.btnDump.Click += new System.EventHandler(this.btnDump_Click);
      // 
      // btnDumpAndSort
      // 
      this.btnDumpAndSort.Enabled = false;
      this.btnDumpAndSort.Location = new System.Drawing.Point(106, 32);
      this.btnDumpAndSort.Name = "btnDumpAndSort";
      this.btnDumpAndSort.Size = new System.Drawing.Size(101, 24);
      this.btnDumpAndSort.TabIndex = 5;
      this.btnDumpAndSort.Text = "Dump sorted FAT";
      this.btnDumpAndSort.UseVisualStyleBackColor = true;
      this.btnDumpAndSort.Click += new System.EventHandler(this.button2_Click);
      // 
      // MainForm
      // 
      this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
      this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
      this.ClientSize = new System.Drawing.Size(853, 426);
      this.Controls.Add(this.btnDumpAndSort);
      this.Controls.Add(this.btnDump);
      this.Controls.Add(this.txtLog);
      this.Controls.Add(this.btnStart);
      this.Controls.Add(this.button1);
      this.Controls.Add(this.txtFolder);
      this.MaximizeBox = false;
      this.Name = "MainForm";
      this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
      this.Text = "FMS";
      this.ResumeLayout(false);
      this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox txtFolder;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.FolderBrowserDialog folderBrowser;
        private System.Windows.Forms.Button btnStart;
        private System.Windows.Forms.TextBox txtLog;
        private System.Windows.Forms.Button btnDump;
        private System.Windows.Forms.Button btnDumpAndSort;

    }
}

