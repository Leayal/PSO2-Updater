using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using MahApps.Metro.Controls.Dialogs;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using MahApps.Metro.Controls;
using Microsoft.Win32;
using Leayal.PSO2.Updater;
using System.Threading;
using Leayal.PSO2.Updater.ChecksumCache;
using System.Collections.Concurrent;

namespace PSO2_Updater_WPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        private SimpleINI config;
        private bool configReady;
        private SynchronizationContext synccontext;
        private ClientUpdater updater;

        public MainWindow()
        {
            this.configReady = false;
            InitializeComponent();

            Stream resStream = Leayal.AppInfo.EntryAssembly.GetManifestResourceStream("PSO2_Updater_WPF.icon.ico");
            if (resStream != null)
            {
                BitmapImage imageSource = new BitmapImage();
                imageSource.BeginInit();
                imageSource.StreamSource = resStream;
                imageSource.CacheOption = BitmapCacheOption.OnDemand;
                imageSource.CreateOptions = BitmapCreateOptions.DelayCreation;
                imageSource.DownloadCompleted += (sender, e) => { resStream.Dispose(); };
                imageSource.EndInit();
                this.Icon = imageSource;
            }

            this.synccontext = SynchronizationContext.Current;
            this.config = new SimpleINI(Path.Combine(Leayal.AppInfo.EntryAssemblyInfo.DirectoryPath, "config.ini"));
            int totalthreads = Environment.ProcessorCount;
            if (totalthreads == 1)
                this.maxDegreeOfParallelism.Items.Add(new ComboBoxItem() { Tag = 1, Content = "1 (Default)" });
            else
            {
                if (totalthreads >= 12)
                    this.AddThreadsOptionsWithRecommendation(totalthreads, 6);
                else if (totalthreads > 4)
                    this.AddThreadsOptionsWithRecommendation(totalthreads, 4);
                else
                    this.AddThreadsOptionsWithRecommendation(totalthreads, 2);
            }

            UpdaterProfile.Items.Add(new ComboBoxItem() { Tag = Leayal.PSO2.Updater.UpdaterProfile.Balanced, Content = "Balanced (Default)" });
            UpdaterProfile.Items.Add(new ComboBoxItem() { Tag = Leayal.PSO2.Updater.UpdaterProfile.PreferSpeed, Content = "Prefer speed (Fastest, low accuracy, may stress CPU)" });
            UpdaterProfile.Items.Add(new ComboBoxItem() { Tag = Leayal.PSO2.Updater.UpdaterProfile.PreferAccuracy, Content = "Prefer accuracy (Slowest, best accuracy, may demand more computer resources)" });
            UpdaterProfile.SelectedIndex = 0;

            string fetchconfig = this.config.GetValue("Cache", "Filepath", string.Empty);
            if (!string.IsNullOrWhiteSpace(fetchconfig))
                this.checksumcache_path.Text = fetchconfig;
            fetchconfig = this.config.GetValue("PSO2", "Directory", string.Empty);
            if (!string.IsNullOrWhiteSpace(fetchconfig))
                this.pso2directory_path.Text = fetchconfig;
            fetchconfig = this.config.GetValue("Updater", "Threads", string.Empty);
            if (int.TryParse(fetchconfig, out var configthread))
            {
                if (configthread == 0)
                    configthread = 1;
                this.maxDegreeOfParallelism.SelectedIndex = Math.Min(totalthreads, configthread) - 1;
            }
            fetchconfig = this.config.GetValue("Updater", "Profile", string.Empty);
            if (int.TryParse(fetchconfig, out var configprofile))
            {
                foreach (ComboBoxItem item in this.UpdaterProfile.Items)
                {
                    if (((int)((UpdaterProfile)item.Tag)) == configprofile)
                    {
                        this.UpdaterProfile.SelectedItem = item;
                        break;
                    }
                }
            }
            this.checksumcache_use.IsChecked = !string.Equals(this.config.GetValue("Cache", "Use", "0"), "0", StringComparison.OrdinalIgnoreCase);

            this.updater = new ClientUpdater();
            this.updater.ProgressChanged += new Action<int, int>(Updater_ProgressChanged);
            this.updater.StepChanged += new Action<UpdateStep, object>(Updater_StepChanged);
            this.updater.UpdateCompleted += new Action<Leayal.PSO2.Updater.Events.PSO2NotifyEventArgs>(Updater_UpdateCompleted);

            this.configReady = true;
        }

        private void Updater_UpdateCompleted(Leayal.PSO2.Updater.Events.PSO2NotifyEventArgs obj)
        {
            this.synccontext.Post(new SendOrPostCallback(async (x) =>
            {
                this.TaskbarItemInfo.ProgressState = System.Windows.Shell.TaskbarItemProgressState.None;
                this.tab_Mainmenu.IsSelected = true;
                Leayal.PSO2.Updater.Events.PSO2NotifyEventArgs e = (Leayal.PSO2.Updater.Events.PSO2NotifyEventArgs)x;
                if (e.Cancelled)
                {
                    if (e.FailedList.Count == 0)
                        await this.MsgBoxOK($"Operation cancelled.", "Information");
                    else if (e.FailedList.Count == 1)
                        await this.MsgBoxOK($"Operation cancelled with 1 file had failed to be downloaded.", "Information");
                    else
                        await this.MsgBoxOK($"Operation cancelled with {e.FailedList.Count} files had failed to be downloaded.", "Information");
                }
                else
                {
                    if (e.FailedList.Count == 0)
                        await this.MsgBoxOK($"Your PSO2 client has been verified and updated to version '{e.NewClientVersion}'.", "Information");
                    else if (e.FailedList.Count == 1)
                        await this.MsgBoxOK($"Your PSO2 client has been verified and updated to version '{e.NewClientVersion}'.\nHowever, there is 1 file had failed to be downloaded.", "Information");
                    else
                        await this.MsgBoxOK($"Your PSO2 client has been verified and updated to version '{e.NewClientVersion}'.\nHowever, there is {e.FailedList.Count} files had failed to be downloaded.", "Information");
                }
            }), obj);
        }

        string currentstep;
        ConcurrentDictionary<string, bool> downloadingfiles;

        private void Updater_StepChanged(UpdateStep arg1, object arg2)
        {
            switch (arg1)
            {
                case UpdateStep.DownloadingFileStart:
                    if (arg2 is PSO2File file)
                    {
                        this.downloadingfiles.TryAdd(file.SafeFilename, true);
                        if (this.downloadingfiles.Count == 1)
                        {
                            string filename = this.downloadingfiles.First().Key;
                            this.synccontext.Post(new SendOrPostCallback(delegate { this.downloadingStep.Text = $"Downloading:\n{filename}"; }), null);
                        }
                        else
                        {
                            StringBuilder sb = new StringBuilder();
                            sb.Append("Downloading:");
                            foreach (string filename in this.downloadingfiles.Keys)
                            {
                                sb.Append("\n");
                                sb.Append(filename);
                            }
                            this.synccontext.Post(new SendOrPostCallback(delegate { this.downloadingStep.Text = sb.ToString(); }), null);
                        }
                    }
                    break;
                case UpdateStep.DownloadingFileEnd:
                    if (arg2 is PSO2File file2)
                    {
                        this.downloadingfiles.TryRemove(file2.SafeFilename, out var somebool);
                        if (this.downloadingfiles.Count == 0)
                            this.synccontext.Post(new SendOrPostCallback(delegate { this.downloadingStep.Text = string.Empty; }), null);
                        else if (this.downloadingfiles.Count == 1)
                        {
                            string filename = this.downloadingfiles.First().Key;
                            this.synccontext.Post(new SendOrPostCallback(delegate { this.downloadingStep.Text = $"Downloading:\n{filename}"; }), null);
                        }
                        else
                        {
                            StringBuilder sb = new StringBuilder();
                            sb.Append("Downloading:");
                            foreach (string filename in this.downloadingfiles.Keys)
                            {
                                sb.Append("\n");
                                sb.Append(filename);
                            }
                            this.synccontext.Post(new SendOrPostCallback(delegate { this.downloadingStep.Text = sb.ToString(); }), null);
                        }
                    }
                    break;
                case UpdateStep.BeginFileCheckAndDownload:
                    this.currentstep = "Verifying files";
                    break;
                case UpdateStep.WriteCache:
                    this.synccontext.Post(new SendOrPostCallback((x) =>
                    {
                        ChecksumCache cache = x as ChecksumCache;
                        if (cache != null)
                        {
                            if (cache.ChecksumList.Count == 1)
                                this.progressStep.Text = "Writing 1 entry to the cache file.";
                            else
                                this.progressStep.Text = $"Writing {cache.ChecksumList.Count} entries to the cache file.";
                        }
                    }), arg2);
                    break;
            }
        }

        private void Updater_ProgressChanged(int arg1, int arg2)
        {
            this.synccontext.Post(new SendOrPostCallback(delegate
            {
                if (arg2 != 0)
                {
                    double val = (double)arg1 / arg2;
                    this.progressbar.Value = Math.Floor(val * 100);
                    this.TaskbarItemInfo.ProgressValue = val;

                    this.progressStep.Text = $"{this.currentstep} ({arg1}/{arg2})";
                }
            }), null);
        }

        private void AddThreadsOptionsWithRecommendation(int totalthreads, int recommend)
        {
            for (int threadcount = 1; threadcount <= totalthreads; threadcount++)
            {
                if (threadcount == recommend)
                    this.maxDegreeOfParallelism.Items.Add(new ComboBoxItem() { Tag = threadcount, Content = $"{threadcount} (Recommended)" });
                else
                    this.maxDegreeOfParallelism.Items.Add(new ComboBoxItem() { Tag = threadcount, Content = threadcount.ToString() });
            }
            this.maxDegreeOfParallelism.SelectedIndex = Math.Min(recommend, totalthreads) - 1;
        }

        private void ChecksumcacheBrowse_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.RestoreDirectory = true;
            ofd.Title = "Select the checksum cache file";
            ofd.Multiselect = false;
            ofd.Filter = "Checksum Cache (*.leaCheck)|*.leaCheck";
            ofd.CheckFileExists = false;
            ofd.CheckPathExists = true;
            if (ofd.ShowDialog(this) == true)
            {
                this.checksumcache_path.Text = ofd.FileName;
                this.config.SetValue("Cache", "Filepath", ofd.FileName);
            }
        }

        private void PSO2Directory_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.RestoreDirectory = true;
            ofd.Title = "Select the PSO2 executable files";
            ofd.Multiselect = false;
            ofd.Filter = "Game executable|pso2.exe|Game Launcher|pso2launcher.exe|Game executables|pso2.exe;pso2launcher.exe";
            ofd.FilterIndex = 3;
            if (ofd.ShowDialog(this) == true)
            {
                string fulldir = Microsoft.VisualBasic.FileIO.FileSystem.GetParentPath(ofd.FileName);
                this.pso2directory_path.Text = fulldir;
                this.config.SetValue("PSO2", "Directory", fulldir);
            }
        }

        private void Checksumcache_use_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (!this.configReady) return;
            if (this.checksumcache_use.IsChecked == true)
                this.config.SetValue("Cache", "Use", "1");
            else
                this.config.SetValue("Cache", "Use", "0");
        }

        private void UpdaterProfile_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!this.configReady) return;
            if (e.AddedItems.Count > 0)
                if (e.AddedItems[0] is ComboBoxItem item)
                    this.config.SetValue("Updater", "Profile", ((int)((UpdaterProfile)item.Tag)).ToString());
        }

        private void MaxDegreeOfParallelism_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!this.configReady) return;
            if (e.AddedItems.Count > 0)
                if (e.AddedItems[0] is ComboBoxItem item)
                    this.config.SetValue("Updater", "Threads", item.Tag.ToString());
        }

        private void CheckForUpdates_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                this.EnsureThings();
                UpdaterProfile profile;
                ComboBoxItem item = this.UpdaterProfile.SelectedItem as ComboBoxItem;
                if (item == null)
                    profile = Leayal.PSO2.Updater.UpdaterProfile.Balanced;
                else
                    profile = (Leayal.PSO2.Updater.UpdaterProfile)item.Tag;
                int threadcount = Math.Min(Environment.ProcessorCount, 4);
                item = this.maxDegreeOfParallelism.SelectedItem as ComboBoxItem;
                if (item != null)
                    threadcount = (int)item.Tag;
                this.progressStep.Text = "Preparing";
                this.progressbar.IsIndeterminate = true;
                this.tab_Progress.IsSelected = true;
                bool usecache = (this.checksumcache_use.IsChecked == true);
                string cachePath = this.checksumcache_path.Text,
                    pso2dir = this.pso2directory_path.Text;
                this.config.SetValue("Cache", "Filepath", cachePath);
                this.config.SetValue("PSO2", "Directory", pso2dir);

                Task.Run(async () =>
                    {
                        var version = await this.updater.GetPatchManagementAsync();
                        if (version.IsNewVersionFound)
                        {
                            this.synccontext.Send(new SendOrPostCallback(async delegate
                            {
                                await this.MsgBoxYesNo($"Found new PSO2 client version.\nLatest version: {version.LatestVersion}\nCurrent version: {version.CurrentVersion}\nDo you want to perform update?", "Question").ContinueWith(async (t) =>
                                {
                                    if (t.Result == MessageDialogResult.Affirmative)
                                    {
                                        ChecksumCache checksumCache = null;
                                        if ((usecache == true) && !string.IsNullOrWhiteSpace(cachePath))
                                        {
                                            if (File.Exists(cachePath))
                                                checksumCache = ChecksumCache.OpenFromFile(cachePath);
                                            else
                                                checksumCache = ChecksumCache.Create(cachePath);
                                        }

                                        this.synccontext.Send(new SendOrPostCallback(delegate
                                        {
                                            this.progressStep.Text = "Preparing patchlist";
                                            this.progressbar.IsIndeterminate = false;
                                            this.TaskbarItemInfo.ProgressState = System.Windows.Shell.TaskbarItemProgressState.Normal;
                                        }), null);
                                        this.downloadingfiles = new ConcurrentDictionary<string, bool>();

                                        this.currentstep = "Downloading patchlists";
                                        RemotePatchlist patchlist = await this.updater.GetPatchlistAsync(version, PatchListType.Patch | PatchListType.LauncherList);
                                        this.updater.VerifyAndDownloadAsync(pso2dir, version, patchlist, new ClientUpdateOptions()
                                        {
                                            ChecksumCache = checksumCache,
                                            Profile = profile,
                                            MaxDegreeOfParallelism = threadcount
                                        });
                                    }
                                    else
                                    {
                                        this.synccontext.Send(new SendOrPostCallback(delegate
                                        {
                                            this.tab_Mainmenu.IsSelected = true;
                                        }), null);
                                    }
                                });
                            }), null);
                        }
                        else
                        {
                            this.synccontext.Send(new SendOrPostCallback(async delegate
                            {
                                this.tab_Mainmenu.IsSelected = true;
                                await this.MsgBoxOK("You already had the latest PSO2 client.", "Information");
                            }), null);
                        }
                    });
            }
#if !DEBUG
            catch (WrappedWarningException warn)
            {
                this.tab_Mainmenu.IsSelected = true;
                this.synccontext.Send(new SendOrPostCallback(async delegate
                {
                    await this.MsgBoxOK(warn.Message, "Warning");
                }), null);
            }
#endif
            catch (Exception ex)
            {
                this.tab_Mainmenu.IsSelected = true;
                this.synccontext.Send(new SendOrPostCallback(async delegate
                {
                    await this.MsgBoxOK(ex.ToString(), "Error");
                }), null);
            }
        }

        private void VerifyFiles_Click(object sender, RoutedEventArgs e)
        {
            //*/
            try
            {
                this.EnsureThings();
                UpdaterProfile profile;
                ComboBoxItem item = this.UpdaterProfile.SelectedItem as ComboBoxItem;
                if (item == null)
                    profile = Leayal.PSO2.Updater.UpdaterProfile.Balanced;
                else
                    profile = (Leayal.PSO2.Updater.UpdaterProfile)item.Tag;
                int threadcount = Math.Min(Environment.ProcessorCount, 4);
                item = this.maxDegreeOfParallelism.SelectedItem as ComboBoxItem;
                if (item != null)
                    threadcount = (int)item.Tag;
                this.progressStep.Text = "Preparing";
                this.progressbar.IsIndeterminate = true;
                this.tab_Progress.IsSelected = true;
                bool usecache = (this.checksumcache_use.IsChecked == true);
                string cachePath = this.checksumcache_path.Text,
                    pso2dir = this.pso2directory_path.Text;
                this.config.SetValue("Cache", "Filepath", cachePath);
                this.config.SetValue("PSO2", "Directory", pso2dir);

                this.synccontext.Send(new SendOrPostCallback(async delegate 
                {
                    if (await this.MsgBoxYesNo($"Are you sure you want to verify the whole game client?", "Question") == MessageDialogResult.Affirmative)
                    {
                        await Task.Run(async () =>
                        {
                            var version = await this.updater.GetPatchManagementAsync();
                            ChecksumCache checksumCache = null;
                            if ((usecache == true) && !string.IsNullOrWhiteSpace(cachePath))
                            {
                                if (File.Exists(cachePath))
                                    try { checksumCache = ChecksumCache.OpenFromFile(cachePath); }
                                    catch (InvalidCacheException) { checksumCache = ChecksumCache.Create(cachePath); }
                                else
                                    checksumCache = ChecksumCache.Create(cachePath);
                            }

                            this.synccontext.Send(new SendOrPostCallback(delegate
                            {
                                this.progressStep.Text = "Preparing patchlist";
                                this.progressbar.IsIndeterminate = false;
                                this.TaskbarItemInfo.ProgressState = System.Windows.Shell.TaskbarItemProgressState.Normal;
                            }), null);
                            this.downloadingfiles = new ConcurrentDictionary<string, bool>();

                            this.currentstep = "Downloading patchlists";
                            RemotePatchlist patchlist = await this.updater.GetPatchlistAsync(version);
                            this.updater.VerifyAndDownloadAsync(pso2dir, version, patchlist, new ClientUpdateOptions()
                            {
                                ChecksumCache = checksumCache,
                                Profile = profile,
                                MaxDegreeOfParallelism = threadcount
                            });
                        });
                    }
                    else
                    {
                        this.tab_Mainmenu.IsSelected = true;
                    }
                }), null);
            }
#if !DEBUG
            catch (WrappedWarningException warn)
            {
                this.tab_Mainmenu.IsSelected = true;
                this.synccontext.Send(new SendOrPostCallback(async delegate
                {
                    await this.MsgBoxOK(warn.Message, "Warning");
                }), null);
            }
#endif
            catch (Exception ex)
            {
                this.tab_Mainmenu.IsSelected = true;
                this.synccontext.Send(new SendOrPostCallback(async delegate
                {
                    await this.MsgBoxOK(ex.ToString(), "Error");
                }), null);
            }
        }

        private void EnsureThings()
        {
            if (string.IsNullOrWhiteSpace(this.pso2directory_path.Text) || !Directory.Exists(this.pso2directory_path.Text))
                throw new WrappedWarningException($"'{this.pso2directory_path.Text}' is not pso2 directory.");
        }

        class WrappedWarningException : Exception
        {
            public WrappedWarningException(string message) : base(message) { }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.updater.CancelDownloadOperations();
        }

        private Task<MessageDialogResult> MsgBoxYesNo(string text, string title)
        {
            return DialogManager.ShowMessageAsync(this, title, text, MessageDialogStyle.AffirmativeAndNegative, new MetroDialogSettings() { AffirmativeButtonText = "Yes", AnimateHide = true, AnimateShow = true, NegativeButtonText = "No" });
        }

        private Task<MessageDialogResult> MsgBoxOK(string text, string title)
        {
            return DialogManager.ShowMessageAsync(this, title, text, MessageDialogStyle.Affirmative, new MetroDialogSettings() { AnimateHide = true, AnimateShow = true });
        }
    }
}
