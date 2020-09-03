using System;
using System.IO;
using Lucene.Net.Store;

namespace DotJEM.Json.Index.Snapshots
{
    public class IndexInputStream : Stream
    {
        private readonly IndexInput input;

        public IndexInputStream(IndexInput input)
        {
            this.input = input;
        }

        public override void Flush()
        {
            throw new InvalidOperationException("Cannot flush a readonly stream.");
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            switch (origin)
            {
                case SeekOrigin.Begin:
                    Position = offset;
                    break;
                case SeekOrigin.Current:
                    Position += offset;
                    break;
                case SeekOrigin.End:
                    Position = Length - offset;
                    break;
            }
            return Position;
        }

        public override void SetLength(long value)
        {
            throw new InvalidOperationException("Cannot change length of a readonly stream.");
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int remaining = (int)(input.Length - input.GetFilePointer());
            int readCount = Math.Min(remaining, count);
            input.ReadBytes(buffer, offset, readCount);
            return readCount;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new InvalidCastException("Cannot write to a readonly stream.");
        }

        public override bool CanRead => true;
        public override bool CanSeek => true;
        public override bool CanWrite => false;
        public override long Length => input.Length;

        public override long Position
        {
            get => input.GetFilePointer();
            set => input.Seek(value);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                input.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}