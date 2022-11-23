using System;
using System.Collections.Generic;
using System.IO;
using Lucene.Net.Store;

namespace DotJEM.Json.Index.Storage.Snapshot;

public interface ISnapshot
{
    long Generation { get; }
    ILuceneFile SegmentsFile { get; }
    IEnumerable<ILuceneFile> Files { get; }

    void WriteFile(IndexInputStream stream);
    void WriteSegmentsFile(IndexInputStream stream);
    void WriteGeneration(long generation);
}

public interface ILuceneFile
{
    string Name { get; }
    Stream Open();
}
    public class IndexOutputStream : Stream
    {
        public string FileName { get; }
        public IndexOutput IndexOutput { get; }

        public IndexOutputStream(string fileName, IndexOutput indexOutput)
        {
            FileName = fileName;
            IndexOutput = indexOutput;
        }

        public override void Flush()
        {
            IndexOutput.Flush();
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
            IndexOutput.SetLength(value);
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new InvalidOperationException("Cannot read from a writeonly stream.");
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            IndexOutput.WriteBytes(buffer, offset, count);
        }

        public override bool CanRead => false;
        public override bool CanSeek => true;
        public override bool CanWrite => true;
        public override long Length => IndexOutput.Length;

        public override long Position
        {
            get => IndexOutput.FilePointer;
            set => IndexOutput.Seek(value);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                IndexOutput.Dispose();
            }
            base.Dispose(disposing);
        }
    }

    public class IndexInputStream : Stream
    {
        public string FileName { get; }
        public IndexInput IndexInput { get; }

        public IndexInputStream(string fileName, IndexInput indexInput)
        {
            FileName = fileName;
            IndexInput = indexInput;
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
            int remaining = (int)(IndexInput.Length() - IndexInput.FilePointer);
            int readCount = Math.Min(remaining, count);
            IndexInput.ReadBytes(buffer, offset, readCount);
            return readCount;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new InvalidCastException("Cannot write to a readonly stream.");
        }

        public override bool CanRead => true;
        public override bool CanSeek => true;
        public override bool CanWrite => false;
        public override long Length => IndexInput.Length();

        public override long Position
        {
            get => IndexInput.FilePointer;
            set => IndexInput.Seek(value);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                IndexInput.Dispose();
            }
            base.Dispose(disposing);
        }
    }