using System;
using System.Collections.Generic;
using System.IO;
using System.ComponentModel;
using Leayal.Net;
using System.Linq;
using Leayal.IO;
using Leayal.PSO2.Updater.Events;

namespace Leayal.PSO2.Updater
{
    /// <summary>
    /// Main class to update PSO2 Client.
    /// </summary>
    public class PSO2UpdateManager : IDisposable
    {
        private static readonly char[] bangonly = { '=' };
        private System.Threading.SynchronizationContext syncContext;
        private ExtendedWebClient myWebClient;
        private BackgroundWorker bWorker;
        private MemoryFileCollection myFileList;
        private bool _isbusy;

        private AnotherSmallThreadPool anothersmallthreadpool;
        private string _LastKnownLatestVersion;
        public string LastKnownLatestVersion { get { return this._LastKnownLatestVersion; } }
        public ChecksumCache.ChecksumCache ChecksumCache { get; set; }

        public PSO2UpdateManager()
        {
            this._LastKnownLatestVersion = string.Empty;
            this._isbusy = false;
            this.syncContext = System.Threading.SynchronizationContext.Current;
            this.myWebClient = new ExtendedWebClient();
            this.myWebClient.UserAgent = DefaultValues.Web.UserAgent;
            this.myFileList = new MemoryFileCollection();
            this.bWorker = new BackgroundWorker();
            this.bWorker.WorkerReportsProgress = true;
            this.bWorker.WorkerSupportsCancellation = true;
            this.bWorker.DoWork += BWorker_DoWork;
            this.bWorker.RunWorkerCompleted += BWorker_RunWorkerCompleted;
        }

        /// <summary>
        /// Get the patch management from SEGA server.
        /// </summary>
        /// <returns>This will be used for version checking.</returns>
        public PSO2VersionCheckResult CheckForUpdates()
        {
            PSO2VersionCheckResult result;
            try
            {
                string management = this.myWebClient.DownloadString(DefaultValues.PatchInfo.PatchManagement);
                if (string.IsNullOrWhiteSpace(management))
                    throw new NullReferenceException("Latest version is null. Something bad happened.");
                else
                {
                    string currentline, master = null, patch = null;
                    string[] splitedline;
                    
                    using (StringReader sr = new StringReader(management))
                        while (sr.Peek() > -1)
                        {
                            currentline = sr.ReadLine();
                            if (!string.IsNullOrWhiteSpace(currentline))
                            {
                                splitedline = currentline.Split(bangonly, 2, StringSplitOptions.RemoveEmptyEntries);
                                if (Leayal.StringHelper.IsEqual(splitedline[0], "MasterURL", true))
                                    master = splitedline[1];
                                else if (Leayal.StringHelper.IsEqual(splitedline[0], "PatchURL", true))
                                    patch = splitedline[1];
                                if (!string.IsNullOrWhiteSpace(master) && !string.IsNullOrWhiteSpace(patch))
                                    break;
                            }
                        }

                    if (string.IsNullOrWhiteSpace(master))
                        result = new PSO2VersionCheckResult(new ArgumentNullException("MasterURL is not found"));
                    else if (string.IsNullOrWhiteSpace(patch))
                        result = new PSO2VersionCheckResult(new ArgumentNullException("PatchURL is not found"));
                    else
                    {
                        string latestver = this.myWebClient.DownloadString(Leayal.UriHelper.URLConcat(patch, "version.ver"));

                        if (string.IsNullOrWhiteSpace(latestver))
                            result = new PSO2VersionCheckResult(new ArgumentNullException("Latest version file is not found"));
                        else
                        {
                            this._LastKnownLatestVersion = latestver;
                            result = new PSO2VersionCheckResult(latestver, Settings.VersionString, master, patch);
                        }
                    }
                }
                this.myWebClient.CacheStorage = null;
            }
            catch (Exception ex)
            {
                result = new PSO2VersionCheckResult(ex);
            }
            return result;
        }

        /// <summary>
        /// Check for game update with specified PSO2 directory.
        /// </summary>
        /// <param name="_pso2path">Directory path to the "pso2_bin"</param>
        public void UpdateGame(string _pso2path)
        {
            this.CheckLocalFiles(new WorkerParams(_pso2path, this.ChecksumCache));
        }

        /// <summary>
        /// Check for game update with specified PSO2 directory and specified PSO2 version.
        /// </summary>
        /// <param name="_pso2path">Directory path to the "pso2_bin"</param>
        /// <param name="latestver">Specify the latest version</param>
        public void UpdateGame(string _pso2path, string latestver)
        {
            this.CheckLocalFiles(new WorkerParams(_pso2path, latestver, this.ChecksumCache, false, false, false));
        }

        /// <summary>
        /// Install PSO2 client to given path.
        /// </summary>
        /// <param name="path">Directory path to the "pso2_bin"</param>
        public void InstallPSO2To(string path)
        {
            this.CheckLocalFiles(new WorkerParams(path, this.ChecksumCache, true));
        }

        /// <summary>
        /// Install PSO2 client to given path and with specified PSO2 version.
        /// </summary>
        /// <param name="path">Directory path to the "pso2_bin"</param>
        /// <param name="latestver">Specify the latest version</param>
        public void InstallPSO2To(string path, string latestver)
        {
            this.CheckLocalFiles(new WorkerParams(path, latestver, this.ChecksumCache, true, true, false));
        }

        /// <summary>
        /// Check files with specified PSO2 version. This method will ignore version checking and force the file checking.
        /// </summary>
        /// <param name="_pso2path">Directory path to the "pso2_bin"</param>
        /// <param name="latestver">Specify the latest version</param>
        public void CheckLocalFiles(string _pso2path, string latestver)
        {
            this.CheckLocalFiles(new WorkerParams(_pso2path, latestver, this.ChecksumCache, false, true, true));
        }

        /// <summary>
        /// Check files. This method will ignore version checking and force the file checking.
        /// </summary>
        /// <param name="_pso2path">Directory path to the "pso2_bin"</param>
        public void CheckLocalFiles(string _pso2path)
        {
            this.CheckLocalFiles(new WorkerParams(_pso2path, string.Empty, this.ChecksumCache, false, true, true));
        }

        private void CheckLocalFiles(WorkerParams wp)
        {
            if (this.IsBusy)
                throw new InvalidOperationException("The manager is busy.");
            this._isbusy = true;
            this.bWorker.RunWorkerAsync(wp);
        }

        /// <summary>
        /// This method is to get the patchlist from SEGA server. Return True if success, otherwise False.
        /// </summary>
        /// <param name="patchinfo">Patch management which can be obtain from <see cref="CheckForUpdates"/></param>
        /// <returns>True if success, otherwise false.</returns>
        protected virtual bool GetFilesList(PSO2VersionCheckResult patchinfo)
        {
            this.myFileList.Clear();
            // this.ProgressTotal = DefaultValues.PatchInfo.PatchListFiles.Count;
            this.ProgressTotal = 3;
            // patchurl
            RecyclableMemoryStream memStream;

            this.CurrentStep = UpdateStep.PSO2UpdateManager_DownloadingPatchList;

            this.ProgressCurrent = 1;
            this.OnCurrentStepChanged(new StepEventArgs(UpdateStep.PSO2UpdateManager_DownloadingPatchList, DefaultValues.PatchInfo.called_masterlist));
            memStream = this.myWebClient.DownloadToMemory(Leayal.UriHelper.URLConcat(patchinfo.MasterURL, DefaultValues.PatchInfo.file_patch), DefaultValues.PatchInfo.called_masterlist);
            if (memStream != null && memStream.Length > 0)
                this.myFileList.Add(DefaultValues.PatchInfo.called_masterlist, memStream);

            this.ProgressCurrent = 2;
            this.OnCurrentStepChanged(new StepEventArgs(UpdateStep.PSO2UpdateManager_DownloadingPatchList, DefaultValues.PatchInfo.called_patchlist));
            memStream = this.myWebClient.DownloadToMemory(Leayal.UriHelper.URLConcat(patchinfo.PatchURL, DefaultValues.PatchInfo.file_patch), DefaultValues.PatchInfo.called_patchlist);
            if (memStream != null && memStream.Length > 0)
                this.myFileList.Add(DefaultValues.PatchInfo.called_patchlist, memStream);

            this.ProgressCurrent = 3;
            this.OnCurrentStepChanged(new StepEventArgs(UpdateStep.PSO2UpdateManager_DownloadingPatchList, DefaultValues.PatchInfo.file_launcher));
            memStream = this.myWebClient.DownloadToMemory(Leayal.UriHelper.URLConcat(patchinfo.PatchURL, DefaultValues.PatchInfo.file_launcher), DefaultValues.PatchInfo.file_launcher);
            if (memStream != null && memStream.Length > 0)
                this.myFileList.Add(DefaultValues.PatchInfo.file_launcher, memStream);

            this.myWebClient.CacheStorage = null;
            if (this.myFileList.Count == 3)
                return true;
            else
                return false;
        }

        /// <summary>
        /// This method is to merging the patch lists into one list.
        /// </summary>
        /// <param name="filelist">Filelist is filled after calling the method <see cref="GetFilesList"/></param>
        /// <param name="patchinfo">Patch management which can be obtain from <see cref="CheckForUpdates"/></param>
        /// <returns>The merged list</returns>
        protected virtual System.Collections.Concurrent.ConcurrentDictionary<string, PSO2File> ParseFilelist(MemoryFileCollection filelist, PSO2VersionCheckResult patchinfo)
        {
            Dictionary<string, PSO2File> result = new Dictionary<string, PSO2File>();
            if (filelist != null && filelist.Count > 0)
            {
                string linebuffer;
                PSO2File pso2filebuffer;
                this.ProgressTotal = filelist.Count;
                this.CurrentStep = UpdateStep.PSO2UpdateManager_BuildingFileList;
                int i = 0;
                foreach (var _pair in filelist.GetEnumerator())
                {
                    i++;
                    using (StreamReader sr = new StreamReader(_pair.Value))
                        while (!sr.EndOfStream)
                        {
                            linebuffer = sr.ReadLine();
                            if (!string.IsNullOrWhiteSpace(linebuffer))
                            {
                                if (_pair.Key == DefaultValues.PatchInfo.called_masterlist)
                                {
                                    if (PSO2File.TryParse(linebuffer, patchinfo.MasterURL, out pso2filebuffer))
                                    {
                                        if (!result.ContainsKey(pso2filebuffer.WindowFilename))
                                            result.Add(pso2filebuffer.WindowFilename, pso2filebuffer);
                                    }
                                }
                                else
                                {
                                    if (PSO2File.TryParse(linebuffer, patchinfo, out pso2filebuffer))
                                    {
                                        if (!result.ContainsKey(pso2filebuffer.WindowFilename))
                                            result.Add(pso2filebuffer.WindowFilename, pso2filebuffer);
                                        else
                                        {
                                            if (_pair.Key == DefaultValues.PatchInfo.file_patch)
                                                result[pso2filebuffer.WindowFilename] = pso2filebuffer;
                                        }
                                    }
                                }
                            }
                        }
                    this.ProgressCurrent = i;
                }
            }

            return new System.Collections.Concurrent.ConcurrentDictionary<string, PSO2File>(result);
        }

        private void BWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            WorkerParams wp = e.Argument as WorkerParams;
            string pso2Path = wp.PSO2Path;
            
            // Check if there is any prepatch files
            // Skip, because currently I have no idea if the pre-patch system is changed as well as Ep5's new update Implementation
            /*if (!wp.IgnorePrepatch)
            {
                string prepatchFolderData = Path.Combine(pso2Path, PrepatchManager.PrepatchManager.PrepatchFolderName, "data");
                if (!DirectoryHelper.IsFolderEmpty(prepatchFolderData))
                {
                    // Ignore prepatch files if it's older than the current client version
                    PSO2Version currentVersion = PSO2Version.Parse(MySettings.PSO2Version);
                    PSO2Version prepatchVersion = PSO2Version.Parse(MySettings.PSO2PrecedeVersion.Version);
                    if (prepatchVersion.CompareTo(currentVersion) > 0)
                    {
                        this.CurrentStep = LanguageManager.GetMessageText("PSO2Updater_FoundValidPrepatch", "Found prepatch files which are ready to be used.");
                        ValidPrepatchPromptEventArgs myEventArgs = new ValidPrepatchPromptEventArgs();
                        this.OnValidPrepatchPrompt(myEventArgs);
                        if (myEventArgs.Use)
                        {
                            string[] filenames = Directory.GetFiles(prepatchFolderData, "*", SearchOption.AllDirectories);
                            this.CurrentStep = LanguageManager.GetMessageText("PSO2Updater_MovingPrepatchFiles", "Applying prepatch files.");
                            this.ProgressTotal = filenames.Length;
                            this.OnProgressStateChanged(new ProgressBarStateChangedEventArgs(Forms.MyMainMenu.ProgressBarVisibleState.Percent));
                            string str = null, maindatafolder = Path.Combine(pso2Path, "data"), targetfile = null;
                            for (int i = 0; i < filenames.Length; i++)
                            {
                                str = filenames[i];
                                targetfile = maindatafolder + str.Remove(0, prepatchFolderData.Length);
                                File.Delete(targetfile);
                                File.Move(str, targetfile);
                                this.ProgressCurrent = i + 1;
                            }

                            // Check if it's empty again to remove it
                            if (DirectoryHelper.IsFolderEmpty(prepatchFolderData))
                            {
                                string prepatchfolder = Path.Combine(pso2Path, PrepatchManager.PrepatchManager.PrepatchFolderName);
                                try
                                {
                                    Directory.Delete(prepatchfolder, true);
                                }
                                catch { }
                            }
                        }
                    }
                    else
                    {
                        this.CurrentStep = UpdateStep.PSO2Updater_FoundInvalidPrepatch;
                        InvalidPrepatchPromptEventArgs myEventArgs = new InvalidPrepatchPromptEventArgs();
                        this.OnInvalidPrepatchPrompt(myEventArgs);
                        if (myEventArgs.Delete)
                        {
                            this.CurrentStep = UpdateStep.PSO2Updater_DeletingInvalidPrepatch;
                            string prepatchfolder = Path.Combine(pso2Path, PrepatchManager.PrepatchManager.PrepatchFolderName);
                            try
                            {
                                Directory.Delete(prepatchfolder, true);
                            }
                            catch { }
                        }
                    }
                }
            }//*/

            var patchinfo = this.CheckForUpdates();

            string verstring = wp.NewVersionString;
            if (string.IsNullOrWhiteSpace(verstring))
                verstring = this.myWebClient.DownloadString(Leayal.UriHelper.URLConcat(patchinfo.PatchURL, "version.ver"));
            if (!string.IsNullOrWhiteSpace(verstring))
                verstring = verstring.Trim();

            if (!wp.IgnoreVersionCheck)
            {
                if (Settings.VersionString.IsEqual(verstring, true))
                {
                    e.Result = new PSO2NotifyEventArgs(verstring, wp.PSO2Path);
                    return;
                }
            }

            if (this.GetFilesList(patchinfo))
            {
                System.Collections.Concurrent.ConcurrentDictionary<string, PSO2File> myPSO2filesList = ParseFilelist(this.myFileList, patchinfo);
                if (!myPSO2filesList.IsEmpty)
                {
                    this.ProgressTotal = myPSO2filesList.Count;
                    
                    if (wp.Cache != null)
                        wp.Cache.Lock();
                    try
                    {
                        anothersmallthreadpool = new AnotherSmallThreadPool(pso2Path, verstring, myPSO2filesList, wp.Cache);
                        anothersmallthreadpool.StepChanged += Anothersmallthreadpool_StepChanged;
                        anothersmallthreadpool.ProgressChanged += Anothersmallthreadpool_ProgressChanged;
                        anothersmallthreadpool.KaboomFinished += Anothersmallthreadpool_KaboomFinished;

                        this.CurrentStep = UpdateStep.PSO2Updater_BeginFileCheckAndDownload;

                        anothersmallthreadpool.StartWork(new WorkerParams(pso2Path, verstring, wp.Installation, wp.IgnorePrepatch));
                    }
                    catch (Exception ex)
                    {
                        if (wp.Cache != null)
                            wp.Cache.Release();
                        throw ex;
                    }
                }
                else
                    e.Result = new PSO2NotifyEventWrapper(wp.Cache, new PSO2NotifyEventArgs(wp.NewVersionString, wp.Installation, new System.Collections.ObjectModel.ReadOnlyCollection<string>(myPSO2filesList.Keys.ToArray())));
            }
            else
            {
                e.Result = new PSO2UpdateResult(UpdateResult.Unknown);
                throw new PSO2UpdateException("Failed to get PSO2's file list.");
            }
        }

        private void Anothersmallthreadpool_KaboomFinished(object sender, KaboomFinishedEventArgs e)
        {
            this._isbusy = false;
            if (e.Error != null)
                this.OnHandledException(e.Error);
            else
            {
                switch (e.Result)
                {
                    case UpdateResult.Cancelled:
                        if (e.UserToken != null && e.UserToken is WorkerParams)
                        {
                            WorkerParams wp = e.UserToken as WorkerParams;
                            if (wp.Installation)
                                this.OnPSO2Installed(new PSO2NotifyEventArgs(true, wp.PSO2Path, e.FailedList));
                            else
                                this.OnPSO2Installed(new PSO2NotifyEventArgs(true, false, e.FailedList));
                        }
                        break;
                    case UpdateResult.Failed:
                        if (e.UserToken != null && e.UserToken is WorkerParams)
                        {
                            WorkerParams wp = e.UserToken as WorkerParams;
                            if (wp.Installation)
                                this.OnPSO2Installed(new PSO2NotifyEventArgs(wp.NewVersionString, wp.PSO2Path, e.FailedList));
                            else
                                this.OnPSO2Installed(new PSO2NotifyEventArgs(wp.NewVersionString, false, e.FailedList));
                        }
                        break;
                    default:
                        if (e.UserToken != null && e.UserToken is WorkerParams)
                        {
                            WorkerParams wp = e.UserToken as WorkerParams;
                            if (!string.IsNullOrWhiteSpace(wp.NewVersionString))
                                Settings.VersionString = wp.NewVersionString;
                            if (wp.Installation)
                                this.OnPSO2Installed(new PSO2NotifyEventArgs(wp.NewVersionString, wp.PSO2Path));
                            else
                                this.OnPSO2Installed(new PSO2NotifyEventArgs(wp.NewVersionString, false));
                        }
                        break;
                }
            }
            anothersmallthreadpool.Dispose();
        }

        private void Anothersmallthreadpool_ProgressChanged(object sender, DetailedProgressChangedEventArgs e)
        {
            this.ProgressCurrent = e.Current;
        }

        private void Anothersmallthreadpool_StepChanged(object sender, StepEventArgs e)
        {
            this.OnCurrentStepChanged(e);
            // this.CurrentStep = e.Value;
        }

        private void BWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                this._isbusy = false;
                this.OnHandledException(e.Error);
            }
            else if (e.Cancelled)
            { this._isbusy = false; }
            else
            {
                if (e.Result != null)
                {
                    PSO2NotifyEventWrapper result = e.Result as PSO2NotifyEventWrapper;
                    if (result != null)
                    {
                        if (result.Cache != null)
                            result.Cache.Release();
                        this._isbusy = false;
                        this.OnPSO2InstallCancelled(result.EventArgs);
                    }
                    else
                    {
                        PSO2NotifyEventArgs eventar = e.Result as PSO2NotifyEventArgs;
                        if (eventar != null)
                        {
                            this._isbusy = false;
                            this.OnPSO2Installed(eventar);
                        }
                    }
                }
            }
        }

        #region "Properties"

        /// <summary>
        /// Gets a value indicating whether the <see cref="PSO2UpdateManager"/> is running an asynchronous operation.
        /// </summary>
        public bool IsBusy => (this._isbusy || this.myWebClient.IsBusy);

        private int _totalprogress;
        /// <summary>
        /// Total progress value. (Not sure why i put this here????)
        /// </summary>
        public int ProgressTotal
        {
            get => this._totalprogress;
            internal set
            {
                this._totalprogress = value;
                this.OnCurrentTotalProgressChanged(new ProgressEventArgs(value));
            }
        }

        private int _currentprogress;
        /// <summary>
        /// Current progress value. (Not sure why i put this here????)
        /// </summary>
        public int ProgressCurrent
        {
            get => this._currentprogress;
            internal set
            {
                this._currentprogress = value;
                this.OnCurrentProgressChanged(new ProgressEventArgs(value));
            }
        }

        private UpdateStep _currentstep;
        /// <summary>
        /// Current step value. (Not sure why i put this here????)
        /// </summary>
        public UpdateStep CurrentStep
        {
            get => this._currentstep;
            internal set
            {
                if (this._currentstep != value)
                {
                    this._currentstep = value;
                    this.OnCurrentStepChanged(new StepEventArgs(value));
                }
            }
        }
        #endregion

        #region "Events"
        /// <summary>
        /// This event will be raised when <see cref="PSO2UpdateManager"/> found valid pre-patch files to confirm before continue updating.
        /// </summary>
        public event EventHandler<ValidPrepatchPromptEventArgs> ValidPrepatchPrompt;
        protected void OnValidPrepatchPrompt(ValidPrepatchPromptEventArgs e)
        {
            this.ValidPrepatchPrompt?.Invoke(this, e);
        }

        /// <summary>
        /// This event will be raised when <see cref="PSO2UpdateManager"/> found out-dated pre-patch files before continue updating.
        /// </summary>
        public event EventHandler<InvalidPrepatchPromptEventArgs> InvalidPrepatchPrompt;
        protected void OnInvalidPrepatchPrompt(InvalidPrepatchPromptEventArgs e)
        {
            this.InvalidPrepatchPrompt?.Invoke(this, e);
        }

        /// <summary>
        /// This event will be raised when <see cref="PSO2UpdateManager"/> found out-dated pre-patch files before continue updating.
        /// </summary>
        public event EventHandler<HandledExceptionEventArgs> HandledException;
        protected void OnHandledException(System.Exception ex)
        {
            this.HandledException?.Invoke(this, new HandledExceptionEventArgs(ex));
        }
        /// <summary>
        /// This event will be raised when <see cref="PSO2UpdateManager"/> after the game updating has been finished.
        /// </summary>
        public event EventHandler<PSO2NotifyEventArgs> PSO2Installed;
        protected void OnPSO2Installed(PSO2NotifyEventArgs e)
        {
            this.syncContext?.Post(new System.Threading.SendOrPostCallback(delegate { this.PSO2Installed?.Invoke(this, e); }), null);
        }
        /// <summary>
        /// This event will be raised when <see cref="PSO2UpdateManager"/> after the game updating has been cancelled by ERROR.
        /// </summary>
        public event EventHandler<PSO2NotifyEventArgs> PSO2InstallCancelled;
        protected void OnPSO2InstallCancelled(PSO2NotifyEventArgs e)
        {
            this.syncContext?.Post(new System.Threading.SendOrPostCallback(delegate { this.PSO2InstallCancelled?.Invoke(this, e); }), null);
        }
        /// <summary>
        /// This event will be raised when <see cref="PSO2UpdateManager"/> each time the operation stage is changed.
        /// </summary>
        public event EventHandler<StepEventArgs> CurrentStepChanged;
        protected void OnCurrentStepChanged(StepEventArgs e)
        {
            this.CurrentStepChanged?.Invoke(this, e);
        }
        /// <summary>
        /// This event will be raised when <see cref="PSO2UpdateManager"/> each time the progress has advanced.
        /// </summary>
        public event EventHandler<ProgressEventArgs> CurrentProgressChanged;
        protected void OnCurrentProgressChanged(ProgressEventArgs e)
        {
            this.syncContext?.Post(new System.Threading.SendOrPostCallback(delegate { this.CurrentProgressChanged?.Invoke(this, e); }), null);
        }
        /// <summary>
        /// This event will be raised when <see cref="PSO2UpdateManager"/> each time the pre-calculated total progress has changed.
        /// </summary>
        public event EventHandler<ProgressEventArgs> CurrentTotalProgressChanged;
        protected void OnCurrentTotalProgressChanged(ProgressEventArgs e)
        {
            this.syncContext?.Post(new System.Threading.SendOrPostCallback(delegate { this.CurrentTotalProgressChanged?.Invoke(this, e); }), null);
        }
        #endregion

        #region "Internal Classes"
        internal class PSO2UpdateResult
        {
            public UpdateResult StatusCode { get; }
            public object UserToken { get; }

            public PSO2UpdateResult(UpdateResult code, int missingfilecount) : this(code, missingfilecount, null) { }
            public PSO2UpdateResult(UpdateResult code) : this(code, -1, null) { }

            public PSO2UpdateResult(UpdateResult code, int missingfilecount, object _userToken)
            {
                this.StatusCode = code;
                this.UserToken = _userToken;
            }
        }

        private class PSO2NotifyEventWrapper
        {
            public ChecksumCache.ChecksumCache Cache { get; }
            public PSO2NotifyEventArgs EventArgs { get; }
            internal PSO2NotifyEventWrapper(ChecksumCache.ChecksumCache _cache, PSO2NotifyEventArgs _eventArgs)
            {
                this.Cache = _cache;
                this.EventArgs = _eventArgs;
            }
        }
        #endregion

        #region "Cancel Operation"
        public void CancelAsync()
        {
            myWebClient.CancelAsync();
            if (anothersmallthreadpool != null)
                anothersmallthreadpool.CancelWork();
        }

        private bool _disposed;
        public void Dispose()
        {
            if (_disposed) return;
            this._disposed = true;
            if (myWebClient != null)
                myWebClient.Dispose();
            if (bWorker != null)
                bWorker.Dispose();
            if (anothersmallthreadpool != null)
                anothersmallthreadpool.Dispose();
            if (myFileList != null)
                myFileList.Dispose();
        }
        #endregion
    }
}
