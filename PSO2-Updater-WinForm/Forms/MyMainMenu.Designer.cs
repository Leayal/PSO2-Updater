namespace PSO2_Updater_WinForm.Forms
{
    partial class MyMainMenu
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
            this.panelMenu = new System.Windows.Forms.Panel();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.button_cachebrowse = new System.Windows.Forms.Button();
            this.textBox_cache = new System.Windows.Forms.TextBox();
            this.checkBox_useCache = new System.Windows.Forms.CheckBox();
            this.button_checkfiles = new System.Windows.Forms.Button();
            this.button_checkupdate = new System.Windows.Forms.Button();
            this.button_pso2browse = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.textBox_pso2directory = new System.Windows.Forms.TextBox();
            this.panelProgress = new System.Windows.Forms.Panel();
            this.panel2 = new System.Windows.Forms.Panel();
            this.progressBar1 = new System.Windows.Forms.ProgressBar();
            this.label_progress = new System.Windows.Forms.Label();
            this.buttonCancel = new System.Windows.Forms.Button();
            this.panelMenu.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.panelProgress.SuspendLayout();
            this.panel2.SuspendLayout();
            this.SuspendLayout();
            // 
            // panelMenu
            // 
            this.panelMenu.Controls.Add(this.groupBox1);
            this.panelMenu.Controls.Add(this.button_checkfiles);
            this.panelMenu.Controls.Add(this.button_checkupdate);
            this.panelMenu.Controls.Add(this.button_pso2browse);
            this.panelMenu.Controls.Add(this.label1);
            this.panelMenu.Controls.Add(this.textBox_pso2directory);
            this.panelMenu.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelMenu.Location = new System.Drawing.Point(0, 0);
            this.panelMenu.Name = "panelMenu";
            this.panelMenu.Size = new System.Drawing.Size(404, 120);
            this.panelMenu.TabIndex = 0;
            // 
            // groupBox1
            // 
            this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox1.Controls.Add(this.button_cachebrowse);
            this.groupBox1.Controls.Add(this.textBox_cache);
            this.groupBox1.Controls.Add(this.checkBox_useCache);
            this.groupBox1.Location = new System.Drawing.Point(15, 37);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(377, 42);
            this.groupBox1.TabIndex = 5;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Options";
            // 
            // button_cachebrowse
            // 
            this.button_cachebrowse.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.button_cachebrowse.Enabled = false;
            this.button_cachebrowse.Location = new System.Drawing.Point(343, 11);
            this.button_cachebrowse.Name = "button_cachebrowse";
            this.button_cachebrowse.Size = new System.Drawing.Size(30, 23);
            this.button_cachebrowse.TabIndex = 6;
            this.button_cachebrowse.Text = "...";
            this.button_cachebrowse.UseVisualStyleBackColor = true;
            this.button_cachebrowse.Click += new System.EventHandler(this.button_cachebrowse_Click);
            // 
            // textBox_cache
            // 
            this.textBox_cache.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.textBox_cache.Enabled = false;
            this.textBox_cache.Location = new System.Drawing.Point(136, 13);
            this.textBox_cache.Name = "textBox_cache";
            this.textBox_cache.Size = new System.Drawing.Size(201, 20);
            this.textBox_cache.TabIndex = 6;
            this.textBox_cache.TextChanged += new System.EventHandler(this.textBox_cache_TextChanged);
            // 
            // checkBox_useCache
            // 
            this.checkBox_useCache.AutoSize = true;
            this.checkBox_useCache.Location = new System.Drawing.Point(6, 16);
            this.checkBox_useCache.Name = "checkBox_useCache";
            this.checkBox_useCache.Size = new System.Drawing.Size(132, 17);
            this.checkBox_useCache.TabIndex = 0;
            this.checkBox_useCache.Text = "Use Checksum Cache";
            this.checkBox_useCache.UseVisualStyleBackColor = true;
            this.checkBox_useCache.CheckedChanged += new System.EventHandler(this.checkBox_useCache_CheckedChanged);
            // 
            // button_checkfiles
            // 
            this.button_checkfiles.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.button_checkfiles.Location = new System.Drawing.Point(226, 85);
            this.button_checkfiles.Name = "button_checkfiles";
            this.button_checkfiles.Size = new System.Drawing.Size(130, 23);
            this.button_checkfiles.TabIndex = 4;
            this.button_checkfiles.Text = "Check files";
            this.button_checkfiles.UseVisualStyleBackColor = true;
            this.button_checkfiles.Click += new System.EventHandler(this.button_checkfiles_Click);
            // 
            // button_checkupdate
            // 
            this.button_checkupdate.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.button_checkupdate.Location = new System.Drawing.Point(48, 85);
            this.button_checkupdate.Name = "button_checkupdate";
            this.button_checkupdate.Size = new System.Drawing.Size(130, 23);
            this.button_checkupdate.TabIndex = 3;
            this.button_checkupdate.Text = "Check for Updates";
            this.button_checkupdate.UseVisualStyleBackColor = true;
            this.button_checkupdate.Click += new System.EventHandler(this.button_checkupdate_Click);
            // 
            // button_pso2browse
            // 
            this.button_pso2browse.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.button_pso2browse.Location = new System.Drawing.Point(362, 10);
            this.button_pso2browse.Name = "button_pso2browse";
            this.button_pso2browse.Size = new System.Drawing.Size(30, 23);
            this.button_pso2browse.TabIndex = 2;
            this.button_pso2browse.Text = "...";
            this.button_pso2browse.UseVisualStyleBackColor = true;
            this.button_pso2browse.Click += new System.EventHandler(this.button_pso2browse_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 15);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(80, 13);
            this.label1.TabIndex = 1;
            this.label1.Text = "PSO2 Directory";
            // 
            // textBox_pso2directory
            // 
            this.textBox_pso2directory.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.textBox_pso2directory.Location = new System.Drawing.Point(94, 11);
            this.textBox_pso2directory.Name = "textBox_pso2directory";
            this.textBox_pso2directory.Size = new System.Drawing.Size(262, 20);
            this.textBox_pso2directory.TabIndex = 0;
            this.textBox_pso2directory.TextChanged += new System.EventHandler(this.textBox_pso2directory_TextChanged);
            // 
            // panelProgress
            // 
            this.panelProgress.Controls.Add(this.panel2);
            this.panelProgress.Controls.Add(this.buttonCancel);
            this.panelProgress.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelProgress.Location = new System.Drawing.Point(0, 0);
            this.panelProgress.Name = "panelProgress";
            this.panelProgress.Size = new System.Drawing.Size(404, 120);
            this.panelProgress.TabIndex = 1;
            // 
            // panel2
            // 
            this.panel2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.panel2.Controls.Add(this.progressBar1);
            this.panel2.Controls.Add(this.label_progress);
            this.panel2.Location = new System.Drawing.Point(12, 24);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(380, 46);
            this.panel2.TabIndex = 3;
            // 
            // progressBar1
            // 
            this.progressBar1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.progressBar1.Location = new System.Drawing.Point(3, 22);
            this.progressBar1.Name = "progressBar1";
            this.progressBar1.Size = new System.Drawing.Size(374, 19);
            this.progressBar1.TabIndex = 1;
            // 
            // label_progress
            // 
            this.label_progress.AutoSize = true;
            this.label_progress.Location = new System.Drawing.Point(3, 6);
            this.label_progress.Name = "label_progress";
            this.label_progress.Size = new System.Drawing.Size(52, 13);
            this.label_progress.TabIndex = 0;
            this.label_progress.Text = "Preparing";
            // 
            // buttonCancel
            // 
            this.buttonCancel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonCancel.Location = new System.Drawing.Point(12, 85);
            this.buttonCancel.Name = "buttonCancel";
            this.buttonCancel.Size = new System.Drawing.Size(380, 23);
            this.buttonCancel.TabIndex = 2;
            this.buttonCancel.Text = "Cancel";
            this.buttonCancel.UseVisualStyleBackColor = true;
            this.buttonCancel.Click += new System.EventHandler(this.buttonCancel_Click);
            // 
            // MyMainMenu
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(404, 120);
            this.Controls.Add(this.panelMenu);
            this.Controls.Add(this.panelProgress);
            this.MinimumSize = new System.Drawing.Size(420, 159);
            this.Name = "MyMainMenu";
            this.Text = "PSO2 Updater WinForm";
            this.panelMenu.ResumeLayout(false);
            this.panelMenu.PerformLayout();
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.panelProgress.ResumeLayout(false);
            this.panel2.ResumeLayout(false);
            this.panel2.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel panelMenu;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox textBox_pso2directory;
        private System.Windows.Forms.Button button_pso2browse;
        private System.Windows.Forms.Button button_checkfiles;
        private System.Windows.Forms.Button button_checkupdate;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.CheckBox checkBox_useCache;
        private System.Windows.Forms.TextBox textBox_cache;
        private System.Windows.Forms.Button button_cachebrowse;
        private System.Windows.Forms.Panel panelProgress;
        private System.Windows.Forms.ProgressBar progressBar1;
        private System.Windows.Forms.Label label_progress;
        private System.Windows.Forms.Button buttonCancel;
        private System.Windows.Forms.Panel panel2;
    }
}

