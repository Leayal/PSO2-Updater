using System.IO;

namespace PSO2_Updater_WinForm.Helpers
{
    static class CommonMethods
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
    }
}
