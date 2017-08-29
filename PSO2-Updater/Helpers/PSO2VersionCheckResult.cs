using System;

namespace Leayal.PSO2.Updater
{
    public class PSO2VersionCheckResult
    {
        public string PatchURL { get; }
        public string MasterURL { get; }
        public string LatestVersion { get; }
        public string CurrentVersion { get; }
        public bool IsNewVersionFound { get; }
        public Exception Error { get; }

        internal PSO2VersionCheckResult(Exception ex) : this(string.Empty, string.Empty, string.Empty, string.Empty)
        {
            this.Error = ex;
        }

        internal PSO2VersionCheckResult(string latest, string current, string _masterurl, string _patchurl)
        {
            this.LatestVersion = latest;
            this.CurrentVersion = current;
            if (latest.ToLower() == current.ToLower())
                this.IsNewVersionFound = false;
            else
                this.IsNewVersionFound = true;
            this.Error = null;
            this.PatchURL = _patchurl;
            this.MasterURL = _masterurl;
        }
    }
}