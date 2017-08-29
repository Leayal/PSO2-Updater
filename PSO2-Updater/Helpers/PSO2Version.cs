using System;
using Leayal;

namespace Leayal.PSO2.Updater
{
    public class PSO2Version : IComparable
    {
        public static readonly char[] underline = { '_' };

        public static bool TryParse(string rawstring, out PSO2Version result)
        {
            string[] spplitted = null;
            if (rawstring.IndexOf(underline[0]) > -1)
                spplitted = rawstring.Split(underline, StringSplitOptions.RemoveEmptyEntries);
            if (spplitted != null && spplitted.Length == 3)
            {
                if (spplitted.Length == 3)
                {
                    result = new PSO2Version(rawstring, spplitted[0], spplitted[2]);
                    return true;
                }
                else if (spplitted.Length == 4)
                {
                    result = new PSO2Version(rawstring, spplitted[0], spplitted[2], spplitted[3]);
                    return true;
                }
                else
                {
                    result = null;
                    return false;
                }
            }
            else
            {
                result = null;
                return false;
            }
        }

        public static PSO2Version Parse(string rawstring)
        {
            string[] spplitted = null;
            if (rawstring.IndexOf(underline[0]) > -1)
                spplitted = rawstring.Split(underline, StringSplitOptions.RemoveEmptyEntries);
            if (spplitted != null)
            {
                if (spplitted.Length == 3)
                    return new PSO2Version(rawstring, spplitted[0], spplitted[2]);
                else if (spplitted.Length == 4)
                    return new PSO2Version(rawstring, spplitted[0], spplitted[2], spplitted[3]);
                else
                    return new PSO2Version(rawstring, rawstring, "-1");
            }
            else
                return new PSO2Version(rawstring, rawstring, "-1");
        }

        //v40500_rc_131
        private string innerRaw;
        public string MajorVersionString { get; }
        public string ReleaseCandidateVersionString { get; }
        public int MajorVersion { get; }
        public int ReleaseCandidateVersion { get; }
        public int BuildVersion { get; }
        public string BuildVersionString { get; }

        private PSO2Version(string rawstring, string majorVersion, string rcVersion) : this(rawstring, majorVersion, rcVersion, "0") { }

        private PSO2Version(string rawstring, string majorVersion, string rcVersion, string buildver)
        {
            this.innerRaw = rawstring;
            if (majorVersion.StartsWith("v", StringComparison.OrdinalIgnoreCase))
                this.MajorVersionString = majorVersion.Remove(0, 1);
            else
                this.MajorVersionString = majorVersion;
            this.ReleaseCandidateVersionString = rcVersion;
            this.MajorVersion = this.MajorVersionString.ToInt();
            this.ReleaseCandidateVersion = this.ReleaseCandidateVersionString.ToInt();
            if (!string.IsNullOrEmpty(buildver))
            {
                this.BuildVersionString = buildver;
                this.BuildVersion = this.BuildVersionString.ToInt();
            }
            else
            {
                this.BuildVersionString = "0";
                this.BuildVersion = 0;
            }
        }

        public bool IsEqual(string version)
        {
            return (this.innerRaw.IsEqual(version, true));
        }

        public bool IsEqual(PSO2Version version)
        {
            if (this.MajorVersion == version.MajorVersion && this.ReleaseCandidateVersion == version.ReleaseCandidateVersion)
                return true;
            else
                return false;
        }

        /// <summary>
        /// Compare two version. 0 if equal, 1 if this version is higher than compared version, -1 if this version is lower than compared version.
        /// </summary>
        /// <param name="pso2ver">PSO2Version. The version to be compared.</param>
        /// <returns>int. 0 if equal, 1 if this version is higher than compared version, -1 if this version is lower than compared version.</returns>
        public int CompareTo(PSO2Version pso2ver)
        {
            if (this.MajorVersion < pso2ver.MajorVersion)
                return -1;
            else if (this.MajorVersion > pso2ver.MajorVersion)
                return 1;
            else
            {
                if (this.ReleaseCandidateVersion < pso2ver.ReleaseCandidateVersion)
                    return -1;
                else if (this.ReleaseCandidateVersion > pso2ver.ReleaseCandidateVersion)
                    return 1;
                else
                {
                    if (this.BuildVersion < pso2ver.BuildVersion)
                        return -1;
                    else if (this.BuildVersion > pso2ver.BuildVersion)
                        return 1;
                    else
                        return 0;
                }
            }
        }

        public override string ToString()
        {
            return this.innerRaw;
        }

        /// <summary>
        /// Compare two version. 0 if equal, 1 if this version is higher than compared version, -1 if this version is lower than compared version.
        /// </summary>
        /// <param name="pso2ver">PSO2Version. The version to be compared.</param>
        /// <returns>int. 0 if equal, 1 if this version is higher than compared version, -1 if this version is lower than compared version, -2 if it can't compare.</returns>
        public int CompareTo(object obj)
        {
            if (obj is PSO2Version)
                return this.CompareTo((PSO2Version)obj);
            else
                return -2;
        }
    }
}
