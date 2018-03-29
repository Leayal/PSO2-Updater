using System.IO;

namespace Leayal.PSO2.Updater.ChecksumCache
{
    public class PSO2FileChecksum
    {
        public static PSO2FileChecksum FromFile(string folder, string filepath)
        {
            string result = string.Empty;
            long len = 0;
            if (File.Exists(filepath))
                using (FileStream fs = File.OpenRead(filepath))
                {
                    len = fs.Length;
                    result = Leayal.Security.Cryptography.MD5Wrapper.HashFromStream(fs);
                }
            if (Path.IsPathRooted(filepath))
                return new PSO2FileChecksum(filepath.Remove(0, folder.Length), len, result);
            else
                return new PSO2FileChecksum(filepath, len, result);
        }

        internal PSO2FileChecksum(string _relativePath, long filelength, string filemd5)
        {
            _relativePath = Leayal.IO.PathHelper.PathTrim(_relativePath);
            this.RelativePath = _relativePath.TrimStart('\\');
            this.FileSize = filelength;
            this.MD5 = filemd5;
        }

        public string RelativePath { get; }
        public long FileSize { get; }
        public string MD5 { get; }

        public override string ToString() => GetString(this.RelativePath, this.FileSize, this.MD5);

        internal static string GetString(string relativePath, long fileSize, string hash)
        {
            // Microsoft.VisualBasic.ControlChars.Tab or '\t' doesn't matter
            return string.Concat(relativePath, Microsoft.VisualBasic.ControlChars.Tab, fileSize.ToString(), Microsoft.VisualBasic.ControlChars.Tab, hash);
        }

        public override bool Equals(object obj)
        {
            PSO2FileChecksum target = obj as PSO2FileChecksum;
            if (target != null)
                if (Leayal.StringHelper.IsEqual(this.RelativePath, target.RelativePath, true))
                    if (Leayal.StringHelper.IsEqual(this.MD5, target.MD5, true))
                        if (this.FileSize == target.FileSize)
                            return true;
            return false;
        }

        public override int GetHashCode()
        {
            return this.RelativePath.GetHashCode() ^ this.FileSize.GetHashCode() ^ this.MD5.GetHashCode();
        }
    }
}
