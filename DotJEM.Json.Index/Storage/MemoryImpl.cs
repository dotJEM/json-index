using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lucene.Net.Store;

namespace DotJEM.Json.Index.Storage
{
    public class MemoryFile
    {
        //4096 = 4KB, 16384 = 16KB, 32768 = 32KB, 65536 = 64KB
        private const int BLOCK_SIZE = 32768; //85000 Bytes and above goes to the Large Object Heap... We wan't to avoid that.

        private List<byte[]> blocks = new List<byte[]>();

        public long Length { get; private set; }
        public long Capacity { get; private set; }
        public long LastModified { get; private set; }

        public void Flush()
        {
            //TODO: Write to disk here?...
            Touch();
        }

        public void Touch()
        {
            LastModified = Stopwatch.GetTimestamp();
        }

        public void Delete()
        {
            //TODO: Delete cache file or mark it "deleted"...
            blocks = null;
        }

        public void WriteBytes(long pos, byte[] buffer, int offset, int count)
        {
            EnsureCapacity(pos + count);
            do
            {
                int blockOffset = (int)(pos % BLOCK_SIZE);
                int copysize = Math.Min(count, BLOCK_SIZE - blockOffset);

                byte[] block = blocks[(int)(pos / BLOCK_SIZE)];
                Buffer.BlockCopy(buffer, offset, block, blockOffset, copysize);

                count -= copysize;
            } while (count > 0);

            Touch();
        }

        public int ReadBytes(long pos, byte[] buffer, int offset, int count)
        {
            count = (int) Math.Min(Length - pos, count);

            int read = 0;
            do
            {
                int blockOffset = (int)(pos % BLOCK_SIZE);
                int copysize = Math.Min(count, BLOCK_SIZE - blockOffset);

                byte[] block = blocks[(int)(pos / BLOCK_SIZE)];
                Buffer.BlockCopy(block, blockOffset, buffer, offset, copysize);

                read += copysize;
                count -= copysize;
            } while (count > 0);
            return read;
        }



        private void EnsureCapacity(long length)
        {
            if (Capacity > length)
                return;

            //Note: Number of Blocks to add to the file.
            int growth = (int)(((length - Capacity) / BLOCK_SIZE) + 1);
            blocks.AddRange(Enumerable.Range(0, growth).Select(index => new byte[BLOCK_SIZE]).ToArray());
            Capacity = BLOCK_SIZE * blocks.Count;
        }
    }

    public class MemoryOutputStream : IndexOutput
    {
        private readonly MemoryFile file;

        private bool isDisposed;
        private long position = 0;

        public override long FilePointer
        {
            get { return position; }
        }

        public override long Length { get { return file.Length; } }

        public MemoryOutputStream()
            : this(new MemoryFile())
        {
        }

        public MemoryOutputStream(MemoryFile file)
        {
            this.file = file;
        }

        public override void WriteByte(byte b)
        {
            WriteBytes(new[] { b }, 0, 1);
        }

        public override void WriteBytes(byte[] b, int offset, int length)
        {
            file.WriteBytes(position, b, offset, length);
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

        }
    }
}