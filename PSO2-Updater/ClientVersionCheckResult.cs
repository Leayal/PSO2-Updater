using System;

namespace Leayal.PSO2.Updater
{
    public class ClientVersionCheckResult
    {
        public string PatchURL { get; }
        public string MasterURL { get; }
        public string LatestVersion { get; }
        public string CurrentVersion { get; }
        public bool IsNewVersionFound { get; }

        internal ClientVersionCheckResult(string latest, string current, string _masterurl, string _patchurl)
        {
            this.LatestVersion = latest;
            this.CurrentVersion = current;
            this.IsNewVersionFound = !string.Equals(current, latest, StringComparison.OrdinalIgnoreCase);
            this.PatchURL = _patchurl;
            this.MasterURL = _masterurl;
        }
    }
}
