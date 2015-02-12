using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Lucene.Net.Store;

namespace DotJEM.Json.Index.Storage
{
    public interface ILock : IDisposable
    {

    }

    public class RangeLock : ILock
    {
        private readonly object padlock;

        public RangeLock(object padlock)
        {
            Monitor.Enter(this.padlock = padlock);
        }

        public void Dispose()
        {
            Monitor.Exit(padlock);
        }
    }

    public class MemoryCachedFile : ILuceneFile
    {
        //4096 = 4KB, 16384 = 16KB, 32768 = 32KB, 65536 = 64KB
        private const int BLOCK_SIZE = 16384; //85000 Bytes and above goes to the Large Object Heap... We wan't to avoid that.

        private readonly List<byte[]> blocks = new List<byte[]>();

        public long Length { get; private set; }
        public long Capacity { get; private set; }
        public long LastModified { get; private set; }

        private readonly string cacheFile;
        private readonly object padlock = new object();

        public MemoryCachedFile(string cacheFile)
        {
            this.cacheFile = cacheFile;
        }

        public MemoryCachedFile(string cacheFile, byte[] buffer)
            : this(cacheFile)
        {
            WriteBytes(0, buffer, 0, buffer.Length);
        }

        public void Flush()
        {
            using (FileStream stream = File.Open(cacheFile, FileMode.Create, FileAccess.ReadWrite, FileShare.Read))
            {
                int count = BLOCK_SIZE;
                foreach (byte[] block in blocks)
                {
                    count = (int) Math.Min(count, Length - stream.Position);
                    
                    stream.Write(block, 0, count);
                    stream.Flush();
                }
            }
            Touch();
        }

        public long Touch()
        {
            return LastModified = Stopwatch.GetTimestamp();
        }

        public void Delete()
        {
            blocks.Clear();
            File.Delete(cacheFile);
        }

        public void WriteBytes(long pos, byte[] buffer, int offset, int count)
        {
            if (count == 0)
                return;

            lock (padlock)
            {
                Length = Math.Max(Length, EnsureCapacity(pos + count));
                do
                {
                    int blockOffset = (int)(pos % BLOCK_SIZE);
                    int copysize = Math.Min(count, BLOCK_SIZE - blockOffset);

                    byte[] block = blocks[(int)(pos / BLOCK_SIZE)];
                    Buffer.BlockCopy(buffer, offset, block, blockOffset, copysize);

                    count -= copysize;
                    offset += copysize;
                    pos += copysize;
                } while (count > 0);
                Touch();
            }
        }

        public int ReadBytes(long pos, byte[] buffer, int offset, int count)
        {
            if (Length == 0)
                return 0;

            lock(padlock)
            {
                count = (int)Math.Min(Length - pos, count);

                int read = 0;
                do
                {
                    int blockOffset = (int)(pos % BLOCK_SIZE);
                    int copysize = Math.Min(count, BLOCK_SIZE - blockOffset);

                    byte[] block = blocks[(int)(pos / BLOCK_SIZE)];
                    Buffer.BlockCopy(block, blockOffset, buffer, offset, copysize);

                    read += copysize;
                    count -= copysize;

                    offset += copysize;
                    pos += copysize;
                } while (count > 0);
                return read;
            }
        }

        private long EnsureCapacity(long length)
        {
            if (Capacity > length)
                return length;

            int growth = (int)((length - Capacity) / BLOCK_SIZE)+1;
            blocks.AddRange(Enumerable.Range(0, growth).Select(index => new byte[BLOCK_SIZE]).ToArray());
            Capacity = BLOCK_SIZE * blocks.Count;
            return length;
        }
    }

    public class MemoryOutputStream : IndexOutput
    {
        private readonly MemoryCachedFile file;

        private bool isDisposed;
        private long position;

        public override long FilePointer
        {
            get { return position; }
        }

        public override long Length { get { return file.Length; } }

        public MemoryOutputStream(MemoryCachedFile file)
        {
            this.file = file;
        }

        public override void WriteByte(byte b)
        {
            WriteBytes(new[] { b }, 0, 1);
        }

        public override void WriteBytes(byte[] buffer, int offset, int length)
        {
            file.WriteBytes(position, buffer, offset, length);
            position += length;
        }

        public override void Flush()
        {
            file.Flush();
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
            position = pos;
        }
    }

    public class MemoryInputStream : IndexInput
    {
        private readonly MemoryCachedFile file;

        private long position;

        public override long FilePointer
        {
            get { return position; }
        }

        public MemoryInputStream(MemoryCachedFile file)
        {
            this.file = file;
        }

        public override byte ReadByte()
        {
            byte[] output = new byte[1];
            ReadBytes(output, 0, 1);
            return output[0];
        }

        public override void ReadBytes(byte[] buffer, int offset, int len)
        {
            position += file.ReadBytes(position, buffer, offset, len);
        }

        protected override void Dispose(bool disposing)
        {
        }

        public override void Seek(long pos)
        {
            position = pos;
        }

        public override long Length()
        {
            return file.Length;
        }
    }
}