using System;
using System.IO;
using Lucene.Net.Store;

namespace DotJEM.Json.Index.Storage.Snapshot;

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