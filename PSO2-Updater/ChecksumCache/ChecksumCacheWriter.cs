using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.ComponentModel;

#pragma warning disable 0809
namespace Leayal.PSO2.Updater.ChecksumCache
{
    public class ChecksumCacheWriter : Stream
    {
        public Stream BaseStream { get; }
        private Leayal.IO.RecyclableMemoryStream bufferWrapper;
        private byte[] buffer;
        public ChecksumCacheWriter(Stream sourceStream)
        {
            if (!sourceStream.CanWrite)
                throw new ArgumentException("The stream must be writable");

            // Make use of Byte[] pool
            this.bufferWrapper = new IO.RecyclableMemoryStream(string.Empty, 1024);
            this.buffer = this.bufferWrapper.GetBuffer();

            this.BaseStream = sourceStream;
        }

        public Encoding Encoding => Encoding.Unicode;

        [EditorBrowsable(EditorBrowsableState.Never)]
        public string NewLine => Microsoft.VisualBasic.ControlChars.CrLf;

        [Obsolete("Please don't use this method.", true), EditorBrowsable(EditorBrowsableState.Never)]
        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            throw new NotImplementedException();
        }
        [Obsolete("Please don't use this method.", true), EditorBrowsable(EditorBrowsableState.Never)]
        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            throw new NotImplementedException();
        }

        public override bool CanRead => this.BaseStream.CanRead;
        public override bool CanSeek => this.BaseStream.CanSeek;
        public override bool CanTimeout => this.BaseStream.CanSeek;
        public override bool CanWrite => true;
        [Obsolete("Please don't use this method.", true), EditorBrowsable(EditorBrowsableState.Never)]
        public override int EndRead(IAsyncResult asyncResult)
        {
            throw new NotImplementedException();
        }
        [Obsolete("Please don't use this method.", true), EditorBrowsable(EditorBrowsableState.Never)]
        public override void EndWrite(IAsyncResult asyncResult)
        {
            throw new NotImplementedException();
        }
        public override long Length => this.BaseStream.Length;
        public override long Position { get => this.BaseStream.Position; set => this.BaseStream.Position = value; }
        public override void Flush()
        {
            this.BaseStream.Flush();
        }
        [Obsolete("Please don't use this method.", true), EditorBrowsable(EditorBrowsableState.Never)]
        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }
        public override int ReadTimeout { get => this.BaseStream.ReadTimeout; set => this.BaseStream.ReadTimeout = value; }
        [Obsolete("Please don't use this method.", true), EditorBrowsable(EditorBrowsableState.Never)]
        public override int ReadByte()
        {
            throw new NotImplementedException();
        }
        public override long Seek(long offset, SeekOrigin origin)
        {
            return this.BaseStream.Seek(offset, origin);
        }
        public override void SetLength(long value)
        {
            this.BaseStream.SetLength(value);
        }
        [Obsolete("Please don't use this method.", true), EditorBrowsable(EditorBrowsableState.Never)]
        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }
        [Obsolete("Please don't use this method.", true), EditorBrowsable(EditorBrowsableState.Never)]
        public override void WriteByte(byte value)
        {
            throw new NotImplementedException();
        }
        public override int WriteTimeout { get => this.BaseStream.WriteTimeout; set => this.BaseStream.WriteTimeout = value; }

        public void WriteEntry(string filename, long filesize, string hash)
        {
            string str = string.Concat(PSO2FileChecksum.GetString(filename, filesize, hash), this.NewLine);
            int length = this.Encoding.GetBytes(str, 0, str.Length, this.buffer, 0);
            this.BaseStream.Write(this.buffer, 0, length);
        }

        public void WriteEntry(PSO2FileChecksum data)
        {
            string str = string.Concat(data.ToString(), this.NewLine);
            int length = this.Encoding.GetBytes(str, 0, str.Length, this.buffer, 0);
            this.BaseStream.Write(this.buffer, 0, length);
        }

        public void WriteEntries(IEnumerable<PSO2FileChecksum> entries)
        {
            string str = null;
            int length;
            foreach (PSO2FileChecksum entry in entries)
            {
                str = string.Concat(entry.ToString(), this.NewLine);
                length = this.Encoding.GetBytes(str, 0, str.Length, this.buffer, 0);
                this.BaseStream.Write(this.buffer, 0, length);
            }
        }

        public override void Close()
        {
            this.buffer = null;
            // Return the byte[] to the pool
            this.bufferWrapper.Dispose();
            base.Close();
        }
    }
}
#pragma warning restore 0809
