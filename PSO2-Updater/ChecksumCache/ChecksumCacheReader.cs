using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text;

#pragma warning disable 0809
namespace Leayal.PSO2.Updater.ChecksumCache
{
    public class ChecksumCacheReader : StreamReader
    {
        private string currentline;
        private string[] tmpsplit;
        internal List<PSO2FileChecksum> result;

        public ChecksumCacheReader(Stream sourceStream) : base(sourceStream, Encoding.UTF8)
        {
            
        }

        [Obsolete("Please don't use this method. Use ReadLine instead.", true), EditorBrowsable(EditorBrowsableState.Never)]
        public override int Read(char[] buffer, int index, int count)
        {
            throw new NotImplementedException();
        }

        [Obsolete("Please don't use this method. Use ReadLine instead.", true), EditorBrowsable(EditorBrowsableState.Never)]
        public override int ReadBlock(char[] buffer, int index, int count)
        {
            throw new NotImplementedException();
        }

        [Obsolete("Please don't use this method. Use ReadLine instead.", true), EditorBrowsable(EditorBrowsableState.Never)]
        public override int Read()
        {
            throw new NotImplementedException();
        }

        public new PSO2FileChecksum ReadLine()
        {
            currentline = base.ReadLine();
            if (!string.IsNullOrWhiteSpace(currentline))
            {
                tmpsplit = currentline.Split(Microsoft.VisualBasic.ControlChars.Tab);
                if (tmpsplit.Length == 3)
                {
                    return new PSO2FileChecksum(tmpsplit[0], long.Parse(tmpsplit[1]), tmpsplit[2]);
                }
                else
                    return null;
            }
            else
                return null;
        }

        public new IEnumerable<PSO2FileChecksum> ReadToEnd()
        {
            if (result != null)
                return result;

            result = new List<PSO2FileChecksum>();
            PSO2FileChecksum read = null;
            while (!this.EndOfStream)
            {
                read = this.ReadLine();
                if (read != null)
                    result.Add(read);
            }
            return result;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
            {
                this.currentline = null;
                this.tmpsplit = null;
                this.result = null; ;
            }
        }
    }
}
#pragma warning restore 0809