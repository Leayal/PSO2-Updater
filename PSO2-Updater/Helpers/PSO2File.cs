using System;
using System.IO;

namespace Leayal.PSO2.Updater
{
    public class PSO2File
    {
        public static readonly char[] _spaceonly = { ' ' };
        public static readonly char[] _tabonly = { Microsoft.VisualBasic.ControlChars.Tab };
        public static bool TryParse(string rawdatastring, ClientVersionCheckResult baseUrl, out PSO2File _pso2file)
        {
            string[] splitbuffer = null;
            if (rawdatastring.IndexOf(Microsoft.VisualBasic.ControlChars.Tab) > -1)
            { splitbuffer = rawdatastring.Split(_tabonly, 5, StringSplitOptions.RemoveEmptyEntries); }
            else if (rawdatastring.IndexOf(" ") > -1)
            { splitbuffer = rawdatastring.Split(_spaceonly, 5, StringSplitOptions.RemoveEmptyEntries); }
            if (splitbuffer != null)
            {
                if (splitbuffer.Length >= 4)
                {
                    if (StringHelper.IsEqual(splitbuffer[3], "m", true))
                        _pso2file = new PSO2File(splitbuffer[0], splitbuffer[2], splitbuffer[1], baseUrl.MasterURL);
                    else
                        _pso2file = new PSO2File(splitbuffer[0], splitbuffer[2], splitbuffer[1], baseUrl.PatchURL);
                    return true;
                }
                else if (splitbuffer.Length == 3)
                {
                    _pso2file = new PSO2File(splitbuffer[0], splitbuffer[1], splitbuffer[2], baseUrl.PatchURL);
                    return true;
                }
                else
                {
                    _pso2file = null;
                    return false;
                }
            }
            else
            {
                _pso2file = null;
                return false;
            }
        }
        public static bool TryParse(string rawdatastring, string baseUrl, out PSO2File _pso2file)
        {
            string[] splitbuffer = null;
            if (rawdatastring.IndexOf(Microsoft.VisualBasic.ControlChars.Tab) > -1)
            { splitbuffer = rawdatastring.Split(_tabonly, 5, StringSplitOptions.RemoveEmptyEntries); }
            else if (rawdatastring.IndexOf(" ") > -1)
            { splitbuffer = rawdatastring.Split(_spaceonly, 5, StringSplitOptions.RemoveEmptyEntries); }
            if (splitbuffer != null)
            {
                if (splitbuffer.Length >= 4)
                {
                    _pso2file = new PSO2File(splitbuffer[0], splitbuffer[2], splitbuffer[1], baseUrl);
                    return true;
                }
                else if (splitbuffer.Length == 3)
                {
                    _pso2file = new PSO2File(splitbuffer[0], splitbuffer[1], splitbuffer[2], baseUrl);
                    return true;
                }
                else
                {
                    _pso2file = null;
                    return false;
                }
            }
            else
            {
                _pso2file = null;
                return false;
            }
        }
        public string SafeFilename { get; }
        public string WindowFilename { get; }
        public string Filename { get; }
        public string OriginalFilename { get; }
        public Uri Url { get; }
        public long Length { get; }
        public string MD5Hash { get; }
        public PSO2File(string _filename, string _length, string _md5, string _baseurl)
        {
            this.Filename = TrimPat(_filename);
            this.OriginalFilename = _filename;
            this.WindowFilename = SwitchToWindowsPath(this.Filename);
            this.SafeFilename = Path.GetFileName(this.Filename);
            long filelength;
            if (!long.TryParse(_length, out filelength))
                filelength = -1;
            this.Length = filelength;
            this.MD5Hash = _md5.ToUpper();
            this.Url = new Uri(Leayal.UriHelper.URLConcat(_baseurl, _filename));
            //new PSO2FileUrl(Classes.Infos.CommonMethods.URLConcat(DefaultValues.Web.MainDownloadLink, _filename), Classes.Infos.CommonMethods.URLConcat(DefaultValues.Web.OldDownloadLink, _filename));
        }

        private static string SwitchToWindowsPath(string _path)
        {
            if (_path.IndexOf("//") > -1)
                _path = _path.Replace("//", "/");
            if (_path.IndexOf(@"\\") > -1)
                _path = _path.Replace(@"\\", @"\");
            if (_path.IndexOf("/") > -1)
                _path = _path.Replace("/", "\\");
            return _path;
        }

        private static string TrimPat(string _path)
        {
            if (_path.EndsWith(DefaultValues.Web.FakeFileExtension, StringComparison.OrdinalIgnoreCase))
                return _path.Substring(0, _path.Length - DefaultValues.Web.FakeFileExtension.Length);
            else
                return _path;
        }
    }
}
