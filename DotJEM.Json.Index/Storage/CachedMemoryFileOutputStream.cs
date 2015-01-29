using System;
using Lucene.Net.Store;

namespace DotJEM.Json.Index.Storage
{
    public class CachedMemoryFileOutputStream : IndexOutput
    {
        private readonly CachedMemoryFile file;
        
        internal const int BUFFER_SIZE = 1024;
        private byte[] currentBuffer;
        private int currentBufferIndex;
        private bool isDisposed;
        private int bufferPosition;
        private long bufferStart;
        private int bufferLength;

        public override long Length
        {
            get
            {
                return file.Length;
            }
        }

        public override long FilePointer
        {
            get
            {
                if (currentBufferIndex >= 0)
                    return bufferStart + bufferPosition;
                else
                    return 0L;
            }
        }

        /// <summary>
        /// Construct an empty output buffer.
        /// </summary>
        public CachedMemoryFileOutputStream()
            : this(new CachedMemoryFile())
        {
        }

        internal CachedMemoryFileOutputStream(CachedMemoryFile file)
        {
            this.file = file;
            currentBufferIndex = -1;
            currentBuffer = null;
        }

        /// <summary>
        /// Copy the current contents of this buffer to the named output.
        /// </summary>
        public virtual void WriteTo(IndexOutput other)
        {
            Flush();
            long num1 = file.Length;
            long num2 = 0L;
            int num3 = 0;
            long num4;
            for (; num2 < num1; num2 = num4)
            {
                int length = 1024;
                num4 = num2 + length;
                if (num4 > num1)
                    length = (int)(num1 - num2);
                other.WriteBytes(file.GetBuffer(num3++), length);
            }
        }

        /// <summary>
        /// Resets this to an empty buffer.
        /// </summary>
        public virtual void Reset()
        {
            currentBuffer = null;
            currentBufferIndex = -1;
            bufferPosition = 0;
            bufferStart = 0L;
            bufferLength = 0;
            file.Length = 0L;
        }

        protected override void Dispose(bool disposing)
        {
            if (isDisposed)
                return;
            if (disposing)
                Flush();
            isDisposed = true;
        }

        public override void Seek(long pos)
        {
            SetFileLength();
            if (pos < bufferStart || pos >= bufferStart + bufferLength)
            {
                currentBufferIndex = (int)(pos / 1024L);
                SwitchCurrentBuffer();
            }
            bufferPosition = (int)(pos % 1024L);
        }

        public override void WriteByte(byte b)
        {
            if (bufferPosition == bufferLength)
            {
                ++currentBufferIndex;
                SwitchCurrentBuffer();
            }
            currentBuffer[bufferPosition++] = b;
        }

        public override void WriteBytes(byte[] b, int offset, int len)
        {
            while (len > 0)
            {
                if (bufferPosition == bufferLength)
                {
                    ++currentBufferIndex;
                    SwitchCurrentBuffer();
                }
                int num = currentBuffer.Length - bufferPosition;
                int length = len < num ? len : num;
                Array.Copy(b, offset, currentBuffer, bufferPosition, length);
                offset += length;
                len -= length;
                bufferPosition += length;
            }
        }

        private void SwitchCurrentBuffer()
        {
            currentBuffer = currentBufferIndex != file.NumBuffers() ? file.GetBuffer(currentBufferIndex) : file.AddBuffer(1024);
            bufferPosition = 0;
            bufferStart = 1024L * currentBufferIndex;
            bufferLength = currentBuffer.Length;
        }

        private void SetFileLength()
        {
            long num = bufferStart + bufferPosition;
            if (num <= file.Length)
                return;
            file.Length = num;
        }

        public override void Flush()
        {
            file.LastModified = DateTime.UtcNow.Ticks / 10000L;
            SetFileLength();
        }

        /// <summary>
        /// Returns byte usage of all buffers.
        /// </summary>
        public virtual long SizeInBytes()
        {
            return file.NumBuffers() * 1024;
        }
    }
}