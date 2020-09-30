using System.Globalization;
using System.IO;

namespace DotJEM.Json.Index.Snapshots.Zip
{
    public class ZipFileSnapshot : ISnapshot
    {
        public string FilePath { get; }
        public long Generation { get; }

        public ZipFileSnapshot(string path)
        {
            FilePath = path;
            Generation = long.Parse(Path.GetFileNameWithoutExtension(path), NumberStyles.AllowHexSpecifier);
        }
    }
}