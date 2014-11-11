using System;
using System.Linq;

namespace DotJEM.Json.Index.Schema
{
    public class JPath
    {
        private readonly string[] segments;

        private JPath(JPath root, JPath relative)
        {
            segments = new string[root.segments.Length + relative.segments.Length];
            Array.Copy(root.segments, 0, segments, 0, root.segments.Length);
            Array.Copy(relative.segments, 0, segments, root.segments.Length, relative.segments.Length);
        }

        private JPath(string value)
        {
            segments = value.Split(new[] { '.', '/' }, StringSplitOptions.RemoveEmptyEntries);
        }

        public static implicit operator JPath(string str)
        {
            return new JPath(str);
        }

        public static JPath operator +(JPath b, JPath c)
        {
            return new JPath(b, c);
        }

        public string ToString(string separator)
        {
            return string.Join(separator, segments.Where(s => !string.IsNullOrWhiteSpace(s)));
        }
    }
}