using System;
using System.ComponentModel;
using System.Threading;
using System.Collections.Concurrent;
using System.IO;
using Leayal.PSO2.Updater.Events;
using Leayal.PSO2.Updater.ChecksumCache;

namespace Leayal.PSO2.Updater
{
    class AnotherSmallThreadPool : IDisposable
    {
        private int _DownloadedFileCount;
        public int DownloadedFileCount { get { return this._DownloadedFileCount; } }
        
        //private int _throttlecachespeed;
        public int ThrottleCacheSpeed { get; set; }
        public bool UseChecksumCache { get; set; }
        private int _FileCount;
        public int FileCount { get { return this._FileCount; } }
        public int FileTotal { get { return this.myPSO2filesList.Count; } }
        public string PSO2Path { get; }
        public string PSO2Ver { get; }

        ConcurrentBag<string> _failedList;
        ConcurrentQueue<string> _keys;
        ConcurrentDictionary<string, PSO2File> myPSO2filesList;

        public ChecksumCache.ChecksumCache ChecksumCache { get; }

        ExtendedBackgroundWorker bWorker;

        private bool _IsBusy;
        public bool IsBusy
        {
            get
            {
                return (this._IsBusy || this.bWorker.IsBusy);
            }
            private set
            {
                this._IsBusy = value;
            }
        }

        public AnotherSmallThreadPool(string _pso2Path, string _pso2ver, ConcurrentDictionary<string, PSO2File> PSO2filesList) : this(_pso2Path, _pso2ver, PSO2filesList, null) { }
        public AnotherSmallThreadPool(string _pso2Path, string _pso2ver, ConcurrentDictionary<string, PSO2File> PSO2filesList, ChecksumCache.ChecksumCache _cache)
        {
            this.bWorker = new ExtendedBackgroundWorker();
            this.bWorker.WorkerReportsProgress = false;
            this.bWorker.WorkerSupportsCancellation = true;
            this.bWorker.DoWork += this.Bworker_DoWork;
            this.bWorker.RunWorkerCompleted += this.Bworker_RunWorkerCompleted;
            this.IsBusy = false;
            this.PSO2Path = _pso2Path;
            this.PSO2Ver = _pso2ver;
            this.ChecksumCache = _cache;
            this.ResetWork(PSO2filesList);
        }

        private void ResetWork(ConcurrentDictionary<string, PSO2File> PSO2filesList)
        {
            if (this.IsBusy)
                this.CancelWork();
            this._DownloadedFileCount = 0;
            this._FileCount = 0;
            this._failedList = new ConcurrentBag<string>();
            this.myPSO2filesList = PSO2filesList;
            if (this.ChecksumCache != null)
                this.ChecksumCache.ReadChecksumCache();
            this._keys = new ConcurrentQueue<string>(PSO2filesList.Keys);
        }

        private bool SeekNextMove()
        {
            if (!_keys.IsEmpty)
            {
                this.bWorker.RunWorkerAsync();
                return true;
            }
            else
                return false;
        }
        
        private void Bworker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (this.ChecksumCache != null && !this.ChecksumCache.ChecksumList.IsEmpty)
            {
                // Use background thread (in a thread pool) so that "WriteChecksumCache" method won't affect the responsive of the UI
                ThreadPool.QueueUserWorkItem(new WaitCallback(delegate 
                {
                    if (e.Cancelled)
                    {
                        if (this.cancelling)
                        {
                            string asfw;
                            while (_keys.TryDequeue(out asfw))
                                this._failedList.Add(asfw);
                            this.ChecksumCache.WriteChecksumCache();
                            this.OnKaboomFinished(new KaboomFinishedEventArgs(UpdateResult.Cancelled, this._failedList, null, this.token));
                            this.cancelling = false;
                            if (_disposed)
                                (sender as ExtendedBackgroundWorker).Dispose();
                        }
                    }
                    else if (!this.SeekNextMove())
                    {
                        if (e.Error != null)
                        {
                            this.ChecksumCache.WriteChecksumCache();
                            this.OnKaboomFinished(new KaboomFinishedEventArgs(UpdateResult.Failed, null, e.Error, this.token));
                        }
                        else if (e.Cancelled)
                        { }
                        else
                        {
                            if (myPSO2filesList.Count == this.DownloadedFileCount)
                            {
                                this.ChecksumCache.WriteChecksumCache(this.token.NewVersionString);
                                this.OnKaboomFinished(new KaboomFinishedEventArgs(UpdateResult.Success, null, null, this.token));
                            }
                            else if (this.DownloadedFileCount > myPSO2filesList.Count)
                            {
                                this.ChecksumCache.WriteChecksumCache(this.token.NewVersionString);
                                this.OnKaboomFinished(new KaboomFinishedEventArgs(UpdateResult.Success, null, null, this.token));
                            }
                            else
                            {
                                if ((myPSO2filesList.Count - this.DownloadedFileCount) < 3)
                                {
                                    this.ChecksumCache.WriteChecksumCache(this.token.NewVersionString);
                                    this.OnKaboomFinished(new KaboomFinishedEventArgs(UpdateResult.MissingSomeFiles, this._failedList, null, this.token));
                                }
                                else
                                {
                                    this.ChecksumCache.WriteChecksumCache();
                                    this.OnKaboomFinished(new KaboomFinishedEventArgs(UpdateResult.Failed, this._failedList, null, this.token));
                                }
                            }
                        }
                    }
                    this.ChecksumCache.Release();
                }));
            }
            else
            {
                if (e.Cancelled)
                {
                    if (this.cancelling)
                    {
                        string asfw;
                        while (_keys.TryDequeue(out asfw))
                            this._failedList.Add(asfw);
                        this.OnKaboomFinished(new KaboomFinishedEventArgs(UpdateResult.Cancelled, this._failedList, null, this.token));
                        this.cancelling = false;
                        if (_disposed)
                            (sender as ExtendedBackgroundWorker).Dispose();
                    }
                }
                else if (!this.SeekNextMove())
                {
                    if (e.Error != null)
                        this.OnKaboomFinished(new KaboomFinishedEventArgs(UpdateResult.Failed, null, e.Error, this.token));
                    else if (e.Cancelled)
                    { }
                    else
                    {
                        if (myPSO2filesList.Count == this.DownloadedFileCount)
                            this.OnKaboomFinished(new KaboomFinishedEventArgs(UpdateResult.Success, null, null, this.token));
                        else if (this.DownloadedFileCount > myPSO2filesList.Count)
                            this.OnKaboomFinished(new KaboomFinishedEventArgs(UpdateResult.Success, null, null, this.token));
                        else
                        {
                            //WebClientPool.SynchronizationContext.Send(new SendOrPostCallback(delegate { System.Windows.Forms.MessageBox.Show("IT'S A FAIL", "Update"); }), null);
                            if ((myPSO2filesList.Count - this.DownloadedFileCount) < 3)
                                this.OnKaboomFinished(new KaboomFinishedEventArgs(UpdateResult.MissingSomeFiles, this._failedList, null, this.token));
                            else
                                this.OnKaboomFinished(new KaboomFinishedEventArgs(UpdateResult.Failed, this._failedList, null, this.token));
                        }
                    }
                }
            }
        }
        private void Bworker_DoWork(object sender, DoWorkEventArgs e)
        {
            ExtendedBackgroundWorker bworker = sender as ExtendedBackgroundWorker;
            string currentfilepath, filemd5, _key;
            PSO2File _value;
            PSO2FileChecksum checksumobj;
            if (_keys.TryDequeue(out _key))
                if (myPSO2filesList.TryGetValue(_key, out _value))
                {
                    currentfilepath = null;
                    filemd5 = null;

                    //This hard-coded looks ugly, doesn't it???
                    if (Leayal.StringHelper.IsEqual(_value.SafeFilename, DefaultValues.CensorFilename, true))
                    {
                        if (File.Exists(Leayal.IO.PathHelper.Combine(this.PSO2Path, _key)))
                        {
                            if (this.ChecksumCache != null)
                            {
                                if (this.ChecksumCache.ChecksumList.TryGetValue(_key, out checksumobj))
                                {
                                    currentfilepath = Leayal.IO.PathHelper.Combine(this.PSO2Path, checksumobj.RelativePath);
                                    FileInfo asd = new FileInfo(currentfilepath);
                                    if (asd.Exists && asd.Length == checksumobj.FileSize)
                                    {
                                        filemd5 = checksumobj.MD5;
                                        //Let's slow down a little
                                        if (this.ThrottleCacheSpeed > 0)
                                            Thread.Sleep(this.ThrottleCacheSpeed);
                                    }
                                    else
                                    {
                                        currentfilepath = Leayal.IO.PathHelper.Combine(this.PSO2Path, _key);
                                        checksumobj = PSO2FileChecksum.FromFile(this.PSO2Path, currentfilepath);
                                        filemd5 = checksumobj.MD5;
                                        if (!this.ChecksumCache.ChecksumList.TryAdd(checksumobj.RelativePath, checksumobj))
                                            this.ChecksumCache.ChecksumList[checksumobj.RelativePath] = checksumobj;
                                    }
                                }
                                else
                                {
                                    currentfilepath = Leayal.IO.PathHelper.Combine(this.PSO2Path, _key);
                                    checksumobj = PSO2FileChecksum.FromFile(this.PSO2Path, currentfilepath);
                                    filemd5 = checksumobj.MD5;
                                    if (!this.ChecksumCache.ChecksumList.TryAdd(checksumobj.RelativePath, checksumobj))
                                        this.ChecksumCache.ChecksumList[checksumobj.RelativePath] = checksumobj;
                                }
                            }
                            else
                            {
                                currentfilepath = Leayal.IO.PathHelper.Combine(this.PSO2Path, _key);
                                checksumobj = PSO2FileChecksum.FromFile(this.PSO2Path, currentfilepath);
                                filemd5 = checksumobj.MD5;
                            }
                            if (!string.IsNullOrEmpty(filemd5))
                            {
                                if (_value.MD5Hash == filemd5)
                                    Interlocked.Increment(ref this._DownloadedFileCount);
                                else
                                {
                                    this.OnStepChanged(new StepEventArgs(UpdateStep.PSO2UpdateManager_DownloadingFileStart, _value.SafeFilename));
                                    try
                                    {
                                        if (bworker.WebClient.DownloadFile(_value.Url, currentfilepath))
                                        {
                                            if (this.ChecksumCache != null)
                                                this.ChecksumCache.ChecksumList.TryUpdate(checksumobj.RelativePath, PSO2FileChecksum.FromFile(this.PSO2Path, currentfilepath), checksumobj);
                                            Interlocked.Increment(ref this._DownloadedFileCount);
                                        }
                                        else
                                            _failedList.Add(_key);
                                    }
                                    catch (System.Net.WebException)
                                    {
                                        _failedList.Add(_key);
                                    }
                                    this.OnStepChanged(new StepEventArgs(UpdateStep.PSO2UpdateManager_DownloadingFileEnd));
                                }
                            }
                            else
                            {
                                this.OnStepChanged(new StepEventArgs(UpdateStep.PSO2UpdateManager_DownloadingFileStart, _value.SafeFilename));
                                try
                                {
                                    if (bworker.WebClient.DownloadFile(_value.Url, currentfilepath))
                                    {
                                        if (this.ChecksumCache != null)
                                            this.ChecksumCache.ChecksumList.TryUpdate(checksumobj.RelativePath, PSO2FileChecksum.FromFile(this.PSO2Path, currentfilepath), checksumobj);
                                        Interlocked.Increment(ref this._DownloadedFileCount);
                                    }
                                    else
                                        _failedList.Add(_key);
                                }
                                catch (System.Net.WebException)
                                {
                                    _failedList.Add(_key);
                                }
                                this.OnStepChanged(new StepEventArgs(UpdateStep.PSO2UpdateManager_DownloadingFileEnd));
                            }
                        }
                        else if (File.Exists(Leayal.IO.PathHelper.Combine(this.PSO2Path, _key + ".backup")))
                        {
                            if (this.ChecksumCache != null)
                            {
                                if (this.ChecksumCache.ChecksumList.TryGetValue(_key, out checksumobj))
                                {
                                    currentfilepath = Leayal.IO.PathHelper.Combine(this.PSO2Path, checksumobj.RelativePath + ".backup");
                                    FileInfo asd = new FileInfo(currentfilepath);
                                    if (asd.Exists && asd.Length == checksumobj.FileSize)
                                    {
                                        filemd5 = checksumobj.MD5;
                                        //Let's slow down a little
                                        if (this.ThrottleCacheSpeed > 0)
                                            Thread.Sleep(this.ThrottleCacheSpeed);
                                    }
                                    else
                                    {
                                        currentfilepath = Leayal.IO.PathHelper.Combine(this.PSO2Path, _key + ".backup");
                                        checksumobj = PSO2FileChecksum.FromFile(this.PSO2Path, currentfilepath);
                                        filemd5 = checksumobj.MD5;
                                        if (!this.ChecksumCache.ChecksumList.TryAdd(checksumobj.RelativePath, checksumobj))
                                            this.ChecksumCache.ChecksumList[checksumobj.RelativePath] = checksumobj;
                                    }
                                }
                                else
                                {
                                    currentfilepath = Leayal.IO.PathHelper.Combine(this.PSO2Path, _key + ".backup");
                                    checksumobj = PSO2FileChecksum.FromFile(this.PSO2Path, currentfilepath);
                                    filemd5 = checksumobj.MD5;
                                    if (!this.ChecksumCache.ChecksumList.TryAdd(checksumobj.RelativePath, checksumobj))
                                        this.ChecksumCache.ChecksumList[checksumobj.RelativePath] = checksumobj;
                                }
                            }
                            else
                            {
                                currentfilepath = Leayal.IO.PathHelper.Combine(this.PSO2Path, _key + ".backup");
                                checksumobj = PSO2FileChecksum.FromFile(this.PSO2Path, currentfilepath);
                                filemd5 = checksumobj.MD5;
                            }
                            if (!string.IsNullOrEmpty(filemd5))
                            {
                                if (_value.MD5Hash == filemd5)
                                    Interlocked.Increment(ref this._DownloadedFileCount);
                                else
                                {
                                    this.OnStepChanged(new StepEventArgs(UpdateStep.PSO2UpdateManager_DownloadingFileStart, _value.SafeFilename));
                                    try
                                    {
                                        if (bworker.WebClient.DownloadFile(_value.Url, currentfilepath))
                                        {
                                            currentfilepath = Leayal.IO.PathHelper.Combine(this.PSO2Path, _key);
                                            if (this.ChecksumCache != null)
                                                using (var myfs = new FileStream(currentfilepath + ".backup", FileMode.Open, FileAccess.Read))
                                                    this.ChecksumCache.ChecksumList.TryUpdate(checksumobj.RelativePath, new PSO2FileChecksum(currentfilepath, myfs.Length, Leayal.Security.Cryptography.MD5Wrapper.FromStream(myfs)), checksumobj);
                                            Interlocked.Increment(ref this._DownloadedFileCount);
                                        }
                                        else
                                            _failedList.Add(_key);
                                    }
                                    catch (System.Net.WebException)
                                    {
                                        _failedList.Add(_key);
                                    }
                                    this.OnStepChanged(new StepEventArgs(UpdateStep.PSO2UpdateManager_DownloadingFileEnd));
                                }
                            }
                            else
                            {
                                this.OnStepChanged(new StepEventArgs(UpdateStep.PSO2UpdateManager_DownloadingFileStart, _value.SafeFilename));
                                try
                                {
                                    if (bworker.WebClient.DownloadFile(_value.Url, currentfilepath))
                                    {
                                        currentfilepath = Leayal.IO.PathHelper.Combine(this.PSO2Path, _key);
                                        if (this.ChecksumCache != null)
                                            using (var myfs = new FileStream(currentfilepath + ".backup", FileMode.Open, FileAccess.Read))
                                                this.ChecksumCache.ChecksumList.TryUpdate(checksumobj.RelativePath, new PSO2FileChecksum(currentfilepath, myfs.Length, Leayal.Security.Cryptography.MD5Wrapper.FromStream(myfs)), checksumobj);
                                        Interlocked.Increment(ref this._DownloadedFileCount);
                                    }
                                    else
                                        _failedList.Add(_key);
                                }
                                catch (System.Net.WebException)
                                {
                                    _failedList.Add(_key);
                                }
                                this.OnStepChanged(new StepEventArgs(UpdateStep.PSO2UpdateManager_DownloadingFileEnd));
                            }
                        }
                    }
                    else
                    {
                        if (this.ChecksumCache != null)
                        {
                            if (this.ChecksumCache.ChecksumList.TryGetValue(_key, out checksumobj))
                            {
                                currentfilepath = Leayal.IO.PathHelper.Combine(this.PSO2Path, checksumobj.RelativePath);
                                FileInfo asd = new FileInfo(currentfilepath);
                                if (asd.Exists && asd.Length == checksumobj.FileSize)
                                {
                                    filemd5 = checksumobj.MD5;
                                    //Let's slow down a little
                                    if (this.ThrottleCacheSpeed > 0)
                                        Thread.Sleep(this.ThrottleCacheSpeed);
                                }
                                else
                                {
                                    currentfilepath = Leayal.IO.PathHelper.Combine(this.PSO2Path, _key);
                                    checksumobj = PSO2FileChecksum.FromFile(this.PSO2Path, currentfilepath);
                                    filemd5 = checksumobj.MD5;
                                    if (!this.ChecksumCache.ChecksumList.TryAdd(checksumobj.RelativePath, checksumobj))
                                        this.ChecksumCache.ChecksumList[checksumobj.RelativePath] = checksumobj;
                                }
                            }
                            else
                            {
                                currentfilepath = Leayal.IO.PathHelper.Combine(this.PSO2Path, _key);
                                checksumobj = PSO2FileChecksum.FromFile(this.PSO2Path, currentfilepath);
                                filemd5 = checksumobj.MD5;
                                if (!this.ChecksumCache.ChecksumList.TryAdd(checksumobj.RelativePath, checksumobj))
                                    this.ChecksumCache.ChecksumList[checksumobj.RelativePath] = checksumobj;
                            }
                        }
                        else
                        {
                            currentfilepath = Leayal.IO.PathHelper.Combine(this.PSO2Path, _key);
                            checksumobj = PSO2FileChecksum.FromFile(this.PSO2Path, currentfilepath);
                            filemd5 = checksumobj.MD5;
                        }
                        
                        if (!string.IsNullOrEmpty(filemd5))
                        {
                            if (_value.MD5Hash == filemd5)
                                Interlocked.Increment(ref this._DownloadedFileCount);
                            else
                            {
                                this.OnStepChanged(new StepEventArgs(UpdateStep.PSO2UpdateManager_DownloadingFileStart, _value.SafeFilename));
                                try
                                {
                                    if (bworker.WebClient.DownloadFile(_value.Url, currentfilepath))
                                    {
                                        if (this.ChecksumCache != null)
                                            this.ChecksumCache.ChecksumList.TryUpdate(checksumobj.RelativePath, PSO2FileChecksum.FromFile(this.PSO2Path, currentfilepath), checksumobj);
                                        Interlocked.Increment(ref this._DownloadedFileCount);
                                    }
                                    else
                                        _failedList.Add(_key);
                                }
                                catch (System.Net.WebException)
                                {
                                    _failedList.Add(_key);
                                }
                                this.OnStepChanged(new StepEventArgs(UpdateStep.PSO2UpdateManager_DownloadingFileEnd));
                            }
                        }
                        else
                        {
                            this.OnStepChanged(new StepEventArgs(UpdateStep.PSO2UpdateManager_DownloadingFileStart, _value.SafeFilename));
                            try
                            {
                                if (bworker.WebClient.DownloadFile(_value.Url, currentfilepath))
                                {
                                    if (this.ChecksumCache != null)
                                        this.ChecksumCache.ChecksumList.TryUpdate(checksumobj.RelativePath, PSO2FileChecksum.FromFile(this.PSO2Path, currentfilepath), checksumobj);
                                    Interlocked.Increment(ref this._DownloadedFileCount);
                                }
                                else
                                    _failedList.Add(_key);
                            }
                            catch (System.Net.WebException)
                            {
                                _failedList.Add(_key);
                            }
                            this.OnStepChanged(new StepEventArgs(UpdateStep.PSO2UpdateManager_DownloadingFileEnd));
                        }
                    }
                }
            Interlocked.Increment(ref this._FileCount);
            this.OnProgressChanged(new DetailedProgressChangedEventArgs(this.FileCount, this.FileTotal));
            if (bworker.CancellationPending)
                e.Cancel = true;
        }

        public event EventHandler<StepEventArgs> StepChanged;
        protected virtual void OnStepChanged(StepEventArgs e)
        {
            this.StepChanged?.Invoke(this, e);
        }

        public event EventHandler<KaboomFinishedEventArgs> KaboomFinished;
        protected virtual void OnKaboomFinished(KaboomFinishedEventArgs e)
        {
            this.KaboomFinished?.Invoke(this, e);
        }

        public event EventHandler<DetailedProgressChangedEventArgs> ProgressChanged;
        protected virtual void OnProgressChanged(DetailedProgressChangedEventArgs e)
        {
            this.ProgressChanged?.Invoke(this, e);
        }
        private bool cancelling;
        public void CancelWork()
        {
            if (this.IsBusy)
            {
                this.cancelling = true;
                this.bWorker.CancelAsync();
            }
        }

        private WorkerParams token;

        public void StartWork(WorkerParams argument)
        {
            if (!this.IsBusy)
            {
                this.token = argument;
                if (!myPSO2filesList.IsEmpty)
                {
                    this.bWorker.RunWorkerAsync();
                    /*ExtendedBackgroundWorker asdasd;
                    asdasd = this._bwList.GetRestingWorker();
                    if (asdasd != null && !asdasd.IsBusy)
                        asdasd.RunWorkerAsync();
                    while (this._bwList.GetNumberOfRunning() < this._bwList.MaxCount)
                    {
                        asdasd = this._bwList.GetRestingWorker();
                        if (asdasd != null && !asdasd.IsBusy)
                            asdasd.RunWorkerAsync();
                    }//*/
                }
            }
        }

        private bool _disposed;
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            this.CancelWork();
        }
    }
}
