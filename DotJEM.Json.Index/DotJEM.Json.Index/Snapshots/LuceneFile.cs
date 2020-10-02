namespace DotJEM.Json.Index.Snapshots
{
    
    public interface ILuceneFile
    {
        string Name { get; }
        byte[] Bytes { get; }
        int Length { get; }
    }

    public class LuceneFile : ILuceneFile
    {
        public byte[] Bytes { get; }
        public int Length { get; }
        public string Name { get; }

        public LuceneFile(string name, byte[] bytes)
        {
            Bytes = bytes;
            Name = name;
            Length = bytes.Length;
        }
    }
}