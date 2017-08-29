using System.IO;

namespace Leayal.PSO2.Updater
{
    internal static class Settings
    {
        private static string cache_pso2verpath, cache_pso2precedeverpath;

        public static string VersionString
        {
            get
            {
                if (string.IsNullOrEmpty(cache_pso2verpath))
                    cache_pso2verpath = Path.Combine(DefaultValues.Directory.DocumentWorkSpace, "version.ver");
                if (File.Exists(cache_pso2verpath))
                {
                    string result = File.ReadAllText(cache_pso2verpath);
                    if (string.IsNullOrWhiteSpace(result))
                        return string.Empty;
                    else
                        return result.Trim();
                }
                else
                    return string.Empty;
            }
            set
            {
                if (string.IsNullOrEmpty(cache_pso2verpath))
                    cache_pso2verpath = Path.Combine(DefaultValues.Directory.DocumentWorkSpace, "version.ver");
                Microsoft.VisualBasic.FileIO.FileSystem.CreateDirectory(DefaultValues.Directory.DocumentWorkSpace);
                File.WriteAllText(cache_pso2verpath, value);
            }
        }

        public static string PrecedeVersionString
        {
            get
            {
                if (string.IsNullOrEmpty(cache_pso2precedeverpath))
                    cache_pso2precedeverpath = Path.Combine(DefaultValues.Directory.DocumentWorkSpace, "precede.txt");
                if (File.Exists(cache_pso2precedeverpath))
                {
                    string result = File.ReadAllText(cache_pso2precedeverpath);
                    if (string.IsNullOrWhiteSpace(result))
                        return string.Empty;
                    else
                        return result.Trim();
                }
                else
                    return string.Empty;
            }
            set
            {
                if (string.IsNullOrEmpty(cache_pso2precedeverpath))
                    cache_pso2precedeverpath = Path.Combine(DefaultValues.Directory.DocumentWorkSpace, "precede.txt");
                Microsoft.VisualBasic.FileIO.FileSystem.CreateDirectory(DefaultValues.Directory.DocumentWorkSpace);
                File.WriteAllText(cache_pso2precedeverpath, value);
            }
        }

        
    }
}
