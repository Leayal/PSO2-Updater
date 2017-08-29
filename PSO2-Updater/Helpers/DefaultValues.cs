using System;

namespace Leayal.PSO2.Updater
{
    internal static class DefaultValues
    {
        public const string CensorFilename = "ffbff2ac5b7a7948961212cefd4d402c";
        public static class Directory
        {
            private static string cache_DocumentWorkSpace;
            public static string DocumentWorkSpace
            {
                get
                {
                    if (string.IsNullOrEmpty(cache_DocumentWorkSpace))
                        cache_DocumentWorkSpace = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "SEGA", "PHANTASYSTARONLINE2");
                    return cache_DocumentWorkSpace;
                }
            }
        }

        public static class Web
        {
            public const string UserAgent = "AQUA_HTTP";
            public const string DownloadHost = "download.pso2.jp";
            public const string PrecedeDownloadLink = "http://" + DownloadHost + "/patch_prod/patches_precede";
            public const string FakeFileExtension = ".pat";
        }

        public static class PatchInfo
        {
            // public const string file_patchold = "patchlist_old.txt";
            public const string file_patch = "patchlist.txt";
            public const string file_launcher = "launcherlist.txt";
            public const string called_masterlist = "patchlist_master.txt";
            public const string called_patchlist = file_patch;
            private static Uri _patchmanagement;
            public static Uri PatchManagement
            {
                get
                {
                    if (_patchmanagement == null)
                        _patchmanagement = new Uri("http://patch01.pso2gs.net/patch_prod/patches/management_beta.txt");
                    return _patchmanagement;
                }
            }

            public const string file_precedelist = "patchlist{0}.txt";
            private static Uri _PrecedeVersionLink;
            public static Uri PrecedeVersionLink
            {
                get
                {
                    if (_PrecedeVersionLink == null)
                        _PrecedeVersionLink = new Uri(Leayal.UriHelper.URLConcat(Web.PrecedeDownloadLink, "version.ver"));
                    return _PrecedeVersionLink;
                }
            }
            public static Uri GetPrecedeDownloadLink(string filelistName)
            {
                return new Uri(Leayal.UriHelper.URLConcat(Web.PrecedeDownloadLink, filelistName));
            }
            public class PatchList
            {
                public string BaseURL { get; }
                public Uri PatchListURL { get; }
                public PatchList(string _baseURL) : this(_baseURL, file_patch) { }
                public PatchList(string _baseURL, string filename)
                {
                    this.BaseURL = _baseURL;
                    this.PatchListURL = new Uri(Leayal.UriHelper.URLConcat(_baseURL, filename));
                }
            }
        }
    }
}
