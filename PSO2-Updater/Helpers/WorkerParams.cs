namespace Leayal.PSO2.Updater
{
    internal class WorkerParams
    {
        public string PSO2Path { get; }
        public string NewVersionString { get; }
        public bool Installation { get; set; }
        public bool IgnorePrepatch { get; set; }
        public bool IgnoreVersionCheck { get; }
        public ChecksumCache.ChecksumCache Cache { get; }

        public WorkerParams(string _pso2path, string latestversionstring, ChecksumCache.ChecksumCache _cache, bool install, bool ignoreprepatch, bool ignoreVerCheck)
        {
            this.PSO2Path = _pso2path;
            this.NewVersionString = latestversionstring;
            this.Installation = install;
            this.IgnorePrepatch = IgnorePrepatch;
            this.Cache = _cache;
            this.IgnoreVersionCheck = ignoreVerCheck;
        }
        public WorkerParams(string _pso2path, string latestversionstring, bool install, bool ignoreprepatch, bool ignoreVerCheck) : this(_pso2path, latestversionstring, null, install, ignoreprepatch, ignoreVerCheck) { }
        public WorkerParams(string _pso2path, string latestversionstring, bool install, bool ignoreprepatch) : this(_pso2path, latestversionstring, null, install, ignoreprepatch, false) { }
        public WorkerParams(string _pso2path, string latestversionstring) : this(_pso2path, latestversionstring, false, false) { }
        public WorkerParams(string _pso2path) : this(_pso2path, string.Empty) { }
        public WorkerParams(string _pso2path, bool install) : this(_pso2path, string.Empty, install, false) { }
        public WorkerParams(string _pso2path, bool install, bool ignoreprepatch) : this(_pso2path, string.Empty, install, ignoreprepatch) { }

        public WorkerParams(string _pso2path, string latestversionstring, ChecksumCache.ChecksumCache _cache, bool ignoreprepatch) : this(_pso2path, latestversionstring, _cache, false, false, ignoreprepatch) { }
        public WorkerParams(string _pso2path, string latestversionstring, ChecksumCache.ChecksumCache _cache) : this(_pso2path, latestversionstring, _cache, false, false, false) { }
        public WorkerParams(string _pso2path, ChecksumCache.ChecksumCache _cache) : this(_pso2path, string.Empty, _cache) { }
        public WorkerParams(string _pso2path, ChecksumCache.ChecksumCache _cache, bool install) : this(_pso2path, string.Empty, _cache, install, false, false) { }
        public WorkerParams(string _pso2path, ChecksumCache.ChecksumCache _cache, bool install, bool ignoreprepatch) : this(_pso2path, string.Empty, _cache, install, ignoreprepatch, false) { }
    }
}
