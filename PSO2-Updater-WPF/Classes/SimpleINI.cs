using System.Collections.Concurrent;
using System.Runtime.InteropServices;

namespace PSO2_Updater_WPF
{
    class SimpleINI
    {
        private ConcurrentBag<char[]> bufferbag;
        public string Path { get; }
        private int BufferSize { get; }

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        static extern long WritePrivateProfileString(string Section, string Key, string Value, string FilePath);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        static extern int GetPrivateProfileString(string Section, string Key, string Default, [In, Out] char[] lpReturnedString, int Size, string FilePath);

        public SimpleINI(string IniPath) : this(IniPath, 4096) { }

        public SimpleINI(string IniPath, int buffersize)
        {
            this.Path = IniPath;
            this.BufferSize = buffersize;
            this.bufferbag = new ConcurrentBag<char[]>();
        }

        public string GetValue(string Section, string Key, string defaultValue)
        {
            char[] RetVal;
            if (!this.bufferbag.TryTake(out RetVal))
                RetVal = new char[this.BufferSize];
            int length = GetPrivateProfileString(Section, Key, defaultValue, RetVal, RetVal.Length, this.Path);
            string result;
            if (length > 0)
                result = new string(RetVal, 0, length);
            else
                result = defaultValue;
            this.bufferbag.Add(RetVal);
            return result;
        }

        public void SetValue(string Section, string Key, string Value)
        {
            WritePrivateProfileString(Section, Key, Value, this.Path);
        }

        public void DeleteKey(string Section, string Key)
        {
            this.SetValue(Section, Key, null);
        }

        public void DeleteSection(string Section)
        {
            this.SetValue(Section, null, null);
        }

        public bool KeyExists(string Section, string Key)
        {
            char[] RetVal;
            if (!this.bufferbag.TryTake(out RetVal))
                RetVal = new char[this.BufferSize];
            bool result = (GetPrivateProfileString(Section, Key, string.Empty, RetVal, RetVal.Length, this.Path) > 0);
            this.bufferbag.Add(RetVal);
            return result;
        }


    }
}
