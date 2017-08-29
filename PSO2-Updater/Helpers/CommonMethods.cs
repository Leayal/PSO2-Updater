using System;
using System.IO;

namespace Leayal.PSO2.Updater
{
    internal static class CommonMethods
    {
        public static bool IsPSO2RootFolder(string path)
        {
            return IsPSO2Folder(Path.Combine(path, "pso2_bin"));
        }

        public static bool IsPSO2Folder(string path)
        {
            if (File.Exists(Path.Combine(path, "pso2launcher.exe")))
                if (File.Exists(Path.Combine(path, "pso2.exe")))
                    if (Directory.Exists(Path.Combine(path, "data", "win32")))
                        return true;
            return false;
        }

        private static string cache_DocumentWorkSpace, cache_VersionFilepath;
        public static string DocumentWorkSpace
        {
            get
            {
                if (string.IsNullOrEmpty(cache_DocumentWorkSpace))
                    cache_DocumentWorkSpace = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "SEGA", "PHANTASYSTARONLINE2");
                return cache_DocumentWorkSpace;
            }
        }

        public static string VersionFilepath
        {
            get
            {
                if (string.IsNullOrEmpty(cache_VersionFilepath))
                    cache_VersionFilepath = Path.Combine(DocumentWorkSpace, "version.ver");
                return cache_VersionFilepath;
            }
        }

        public static bool IsVersionFileExist => File.Exists(VersionFilepath);
    }
}
