using System.Collections.Generic;
using System.Diagnostics;

namespace DotJEM.Json.Index.Storage
{
    public class MemoryCachedFile
    {
        private readonly List<byte[]> buffers = new List<byte[]>();

        private readonly object padLock = new object();
        private long length;
        private long lastModified;
        private long sizeInBytes;

        public long Length
        {
            get
            {
                Debug.WriteLine("long Length => " + length);
                return length;
            }
            set
            {
                Debug.WriteLine("long Length <= " + value); 
                length = value;
            }
        }

        public long LastModified
        {
            get
            {
                Debug.WriteLine("long LastModified => " + lastModified);
                return lastModified;
            }
            set
            {
                Debug.WriteLine("long LastModified <= " + value);
                lastModified = value;
            }
        }

        public long SizeInBytes
        {
            get
            {
                Debug.WriteLine("long SizeInBytes => " + sizeInBytes);
                return sizeInBytes;
            }
            private set
            {
                Debug.WriteLine("long SizeInBytes <= " + value);
                sizeInBytes = value;
            }
        }

        public MemoryCachedFile()
        {
            LastModified = Stopwatch.GetTimestamp();
        }

        internal byte[] AddBuffer(int size)
        {
            Debug.WriteLine("byte[] AddBuffer(" + size + ")");
            byte[] numArray = new byte[size];
            lock (padLock)
            {
                buffers.Add(numArray);
                sizeInBytes += size;
            }
            return numArray;
        }

        public byte[] GetBuffer(int index)
        {
            Debug.WriteLine("byte[] GetBuffer("+index+")");
            return buffers[index];
        }

        public int NumBuffers()
        {
            Debug.WriteLine("int NumBuffers() = " + buffers.Count);
            return buffers.Count;
        }

        public void Delete()
        {

        }

        public void Touch()
        {
            LastModified = Stopwatch.GetTimestamp();
            Debug.WriteLine("void Touch() = " + LastModified);
        }
    }
}