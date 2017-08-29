using System;
using System.Collections.Generic;
using System.Threading;
using System.IO;
using System.Windows.Forms;
using Leayal.PSO2.Updater;
using Leayal.PSO2.Updater.Events;

namespace PSO2_Updater_WinForm.Forms
{
    public partial class MyMainMenu : Form
    {
        Leayal.Ini.IniFile config;
        PSO2UpdateManager pso2updatemng;
        private SynchronizationContext syncConctext;

        public MyMainMenu()
        {
            InitializeComponent();
            this.syncConctext = SynchronizationContext.Current;
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            this.Icon = Properties.Resources.icon;
            this.config = new Leayal.Ini.IniFile(Path.Combine(Leayal.AppInfo.AssemblyInfo.DirectoryPath, "config.ini"));
            this.pso2updatemng = new PSO2UpdateManager();

            // Progress handling
            this.pso2updatemng.CurrentStepChanged += PSO2UpdateManager_CurrentStepChanged;
            this.pso2updatemng.CurrentProgressChanged += PSO2UpdateManager_CurrentProgressChanged;
            this.pso2updatemng.CurrentTotalProgressChanged += PSO2UpdateManager_CurrentTotalProgressChanged;

            // Kind of callback
            this.pso2updatemng.HandledException += PSO2UpdateManager_HandledException;
            this.pso2updatemng.PSO2Installed += PSO2UpdateManager_PSO2Installed;
            this.pso2updatemng.PSO2InstallCancelled += PSO2UpdateManager_PSO2InstallCancelled;

            // Load config
            this.textBox_pso2directory.Text = this.config.GetValue("PSO2", "Directory", string.Empty);
            if (!string.IsNullOrEmpty(this.textBox_pso2directory.Text))
                this.textBox_pso2directory.Select(this.textBox_pso2directory.Text.Length, 0);
            
            this.textBox_cache.Text = this.config.GetValue("Cache", "Filepath", string.Empty);
            if (!string.IsNullOrEmpty(this.textBox_cache.Text))
                this.textBox_cache.Select(this.textBox_cache.Text.Length, 0);

            string useCache = this.config.GetValue("Cache", "Use", "0");
            if (string.IsNullOrWhiteSpace(useCache))
                this.checkBox_useCache.Checked = false;
            else if (useCache == "0")
                this.checkBox_useCache.Checked = false;
            else
                this.checkBox_useCache.Checked = true;
        }

        private void PSO2UpdateManager_PSO2InstallCancelled(object sender, PSO2NotifyEventArgs e)
        {
            this.RemoveCacheFromManager();
            this.syncConctext.Post(new SendOrPostCallback(delegate {
                if (this.pendingCloseForm)
                {
                    this.Close();
                    return;
                }
                this.SwitchToProgressPanel(false);
                Leayal.Forms.TaskbarItemInfo.TaskbarProgress.SetState(this.Handle, Leayal.Forms.TaskbarItemInfo.TaskbarProgress.TaskbarStates.NoProgress);
            }), null);
        }

        private void PSO2UpdateManager_PSO2Installed(object sender, PSO2NotifyEventArgs e)
        {
            this.RemoveCacheFromManager();
            this.syncConctext.Post(new SendOrPostCallback(delegate {
                Leayal.Forms.TaskbarItemInfo.TaskbarProgress.SetState(this.Handle, Leayal.Forms.TaskbarItemInfo.TaskbarProgress.TaskbarStates.NoProgress);
                this.SwitchToProgressPanel(false);
                if (e.Cancelled)
                {
                    if (this.pendingCloseForm)
                    {
                        this.Close();
                        return;
                    }
                }
                else
                {
                    if (e.FailedList == null)
                        MessageBox.Show(this, "PSO2 has been downloaded successfully to latest version.", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    else
                    {
                        if (e.FailedList.Count < 3)
                            MessageBox.Show(this, string.Format("PSO2 has been downloaded successfully to latest version. But there are {0} files have failed to be downloaded.", e.FailedList.Count), "Notice", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        else
                            MessageBox.Show(this, string.Format("PSO2 has failed to be downloaded. {0} files have failed to be downloaded.", e.FailedList.Count), "Notice", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }), null);
        }

        private void PSO2UpdateManager_HandledException(object sender, HandledExceptionEventArgs e)
        {
            this.RemoveCacheFromManager();
            this.syncConctext.Post(new SendOrPostCallback(delegate {
                if (this.pendingCloseForm)
                {
                    this.Close();
                    return;
                }
                this.SwitchToProgressPanel(false);
                Leayal.Forms.TaskbarItemInfo.TaskbarProgress.SetState(this.Handle, Leayal.Forms.TaskbarItemInfo.TaskbarProgress.TaskbarStates.NoProgress);
                MessageBox.Show(this, e.Error.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }), null);
        }

        private void PSO2UpdateManager_CurrentTotalProgressChanged(object sender, ProgressEventArgs e)
        {
            this.syncConctext.Post(new SendOrPostCallback(delegate {
                this.progressBar1.Maximum = e.Progress;
            }), null);
        }

        private void PSO2UpdateManager_CurrentProgressChanged(object sender, ProgressEventArgs e)
        {
            this.syncConctext.Post(new SendOrPostCallback(delegate {
                this.progressBar1.Value = e.Progress;
                Leayal.Forms.TaskbarItemInfo.TaskbarProgress.SetValue(this.Handle, this.progressBar1.Value, this.progressBar1.Maximum);
            }), null);
        }

        private void PSO2UpdateManager_CurrentStepChanged(object sender, StepEventArgs e)
        {
            this.syncConctext.Post(new SendOrPostCallback(delegate {
                switch (e.Step)
                {
                    case UpdateStep.PSO2UpdateManager_BuildingFileList:
                        Leayal.Forms.TaskbarItemInfo.TaskbarProgress.SetState(this.Handle, Leayal.Forms.TaskbarItemInfo.TaskbarProgress.TaskbarStates.Indeterminate);
                        this.label_progress.Text = "Preparing file list";
                        break;
                    case UpdateStep.PSO2UpdateManager_DownloadingFileStart:
                        this.label_progress.Text = string.Format("Downloading {0}", e.Value);
                        break;
                    case UpdateStep.PSO2UpdateManager_DownloadingFileEnd:
                        this.label_progress.Text = "Checking and download old/missing files";
                        break;
                    case UpdateStep.PSO2UpdateManager_DownloadingPatchList:
                        Leayal.Forms.TaskbarItemInfo.TaskbarProgress.SetState(this.Handle, Leayal.Forms.TaskbarItemInfo.TaskbarProgress.TaskbarStates.Indeterminate);
                        this.label_progress.Text = string.Format("Downloading list {0}", e.Value);
                        break;
                    case UpdateStep.PSO2Updater_BeginFileCheckAndDownload:
                        Leayal.Forms.TaskbarItemInfo.TaskbarProgress.SetState(this.Handle, Leayal.Forms.TaskbarItemInfo.TaskbarProgress.TaskbarStates.Normal);
                        this.label_progress.Text = "Checking and download old/missing files";
                        break;
                }
            }), null);
        }

        private bool pendingCloseForm;
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            this.config.Save();
            if (!this.pso2updatemng.IsBusy)
                base.OnFormClosing(e);
            else
            {
                e.Cancel = true;
                this.pendingCloseForm = true;
                this.buttonCancel.PerformClick();
            }
        }

        private void button_pso2browse_Click(object sender, EventArgs e)
        {
            using (Leayal.Forms.FolderBrowseDialogEx fbd = new Leayal.Forms.FolderBrowseDialogEx())
            {
                fbd.OKButtonText = "Select";
                if (!string.IsNullOrWhiteSpace(this.textBox_pso2directory.Text))
                    fbd.SelectedDirectory = this.textBox_pso2directory.Text;
                fbd.ShowFiles = false;
                fbd.ShowNewFolderButton = true;
                fbd.ShowTextBox = true;
                fbd.UseNewStyle = true;
                fbd.ValidateTextBox = true;
                fbd.Description = "Select PSO2 directory";
                if (fbd.ShowDialog(this) == DialogResult.OK)
                {
                    if (Helpers.CommonMethods.IsPSO2Folder(fbd.SelectedDirectory))
                        this.textBox_pso2directory.Text = fbd.SelectedDirectory;
                    else if (MessageBox.Show(this, "The selected folder appears to be not a pso2_bin folder.\nAre you sure you still want to continue?", "Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                        this.textBox_pso2directory.Text = fbd.SelectedDirectory;
                    if (!string.IsNullOrEmpty(this.textBox_pso2directory.Text))
                        this.textBox_pso2directory.Select(this.textBox_pso2directory.Text.Length, 0);
                }
            }
        }

        private void textBox_pso2directory_TextChanged(object sender, EventArgs e)
        {
            this.config.SetValue("PSO2", "Directory", this.textBox_pso2directory.Text);
        }

        private void textBox_cache_TextChanged(object sender, EventArgs e)
        {
            this.config.SetValue("Cache", "Filepath", this.textBox_cache.Text);
        }

        private void checkBox_useCache_CheckedChanged(object sender, EventArgs e)
        {
            if (this.checkBox_useCache.Checked)
                this.config.SetValue("Cache", "Use", "1");
            else
                this.config.SetValue("Cache", "Use", "0");

            this.textBox_cache.Enabled = this.checkBox_useCache.Checked;
            this.button_cachebrowse.Enabled = this.checkBox_useCache.Checked;
        }

        private void SwitchToProgressPanel(bool val)
        {
            this.panelMenu.Visible = !val;
            this.panelProgress.Visible = val;
        }

        private void buttonCancel_Click(object sender, EventArgs e)
        {
            if (this.pso2updatemng.IsBusy)
                this.pso2updatemng.CancelAsync();
        }

        private void button_checkupdate_Click(object sender, EventArgs e)
        {
            if (Helpers.CommonMethods.IsPSO2Folder(this.textBox_pso2directory.Text))
                this.StartUpdateGame(this.textBox_pso2directory.Text);
            else if (MessageBox.Show(this, "The selected folder appears to be not a pso2_bin folder.\nAre you sure you still want to continue?", "Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                this.StartUpdateGame(this.textBox_pso2directory.Text);
        }

        private void button_checkfiles_Click(object sender, EventArgs e)
        {
            if (Helpers.CommonMethods.IsPSO2Folder(this.textBox_pso2directory.Text))
                this.StartCheckLocalFiles(this.textBox_pso2directory.Text);
            else if (MessageBox.Show(this, "The selected folder appears to be not a pso2_bin folder.\nAre you sure you still want to continue?", "Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                this.StartCheckLocalFiles(this.textBox_pso2directory.Text);
        }

        private void StartUpdateGame(string pso2directory)
        {
            if (this.checkBox_useCache.Checked && Path.IsPathRooted(this.textBox_cache.Text))
                this.pso2updatemng.ChecksumCache = Leayal.PSO2.Updater.ChecksumCache.ChecksumCache.OpenFromFile(this.textBox_cache.Text);
            this.pso2updatemng.UpdateGame(pso2directory);
            this.SwitchToProgressPanel(true);
        }

        private void StartCheckLocalFiles(string pso2directory)
        {
            if (this.checkBox_useCache.Checked && Path.IsPathRooted(this.textBox_cache.Text))
                this.pso2updatemng.ChecksumCache = Leayal.PSO2.Updater.ChecksumCache.ChecksumCache.OpenFromFile(this.textBox_cache.Text);
            this.pso2updatemng.CheckLocalFiles(pso2directory);
            this.SwitchToProgressPanel(true);
        }

        private void RemoveCacheFromManager()
        {
            if (this.pso2updatemng.ChecksumCache != null)
            {
                this.pso2updatemng.ChecksumCache.Dispose();
                this.pso2updatemng.ChecksumCache = null;
            }
        }

        private void button_cachebrowse_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Multiselect = false;
                ofd.AutoUpgradeEnabled = true;
                ofd.AddExtension = false;
                ofd.CheckFileExists = false;
                ofd.CheckPathExists = false;
                if (!string.IsNullOrWhiteSpace(this.textBox_cache.Text))
                    ofd.FileName = this.textBox_cache.Text;
                else
                    ofd.FileName = "Leave the filename here then press OK to save instead of opening";
                using (Leayal.Forms.DialogFileFilterBuilder builder = new Leayal.Forms.DialogFileFilterBuilder())
                {
                    builder.AppendAllSupportedTypes = Leayal.Forms.AppendOrder.First;
                    builder.Append("Old Checksum Cache Data", "*.leaCheck");
                    builder.Append("Binary Data File", "*.bin");
                    builder.Append("General Data File", "*.dat");
                    builder.Append("All File Types", "*.*");
                    ofd.Filter = builder.ToFileFilterString();
                }
                ofd.FileOk += (dialog, cancelEventArgs) =>
                {
                    if (!File.Exists(ofd.FileName))
                        if (MessageBox.Show(this, "The file is not existed. Updater will create or update the cache here everytime the updater finished updating the game.\nContinue?", "Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                            cancelEventArgs.Cancel = true;
                };
                if (ofd.ShowDialog(this) == DialogResult.OK)
                {
                    this.textBox_cache.Text = ofd.FileName;
                    if (!string.IsNullOrEmpty(this.textBox_cache.Text))
                        this.textBox_cache.Select(this.textBox_cache.Text.Length, 0);
                }
            }
        }
    }
}
