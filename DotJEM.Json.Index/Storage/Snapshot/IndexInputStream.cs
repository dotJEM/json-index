using System;
using System.IO;
using Lucene.Net.Store;

namespace DotJEM.Json.Index.Storage.Snapshot;

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
        long remaining = IndexInput.Length - IndexInput.Position;
        int readCount = (int)Math.Min(remaining, count);
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
    public override long Length => IndexInput.Length;

    public override long Position
    {
        get => IndexInput.Position;
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