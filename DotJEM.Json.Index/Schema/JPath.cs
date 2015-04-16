using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Lucene.Net.Analysis;

namespace DotJEM.Json.Index.Schema
{
    public class JPath
    {
        private readonly string[] segments;

        public string Path { get; private set; }

        private JPath(JPath root, JPath relative)
        {
            segments = new string[root.segments.Length + relative.segments.Length];
            Array.Copy(root.segments, 0, segments, 0, root.segments.Length);
            Array.Copy(relative.segments, 0, segments, root.segments.Length, relative.segments.Length);
        }

        private JPath(string value)
        {
            Path = value;
            segments = JPathTokenizer.Tokens(value);
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

        public override string ToString()
        {
            return ToString(".");
        }
    }

    public class JPathTokenizer : IEnumerable<string>
    {
        private readonly string value;

        private JPathTokenizer(string value)
        {
            this.value = value;
        }

        public IEnumerator<string> GetEnumerator()
        {
            //TODO: Temporary implementation... Should work more like JPath in JSON Core.
            return value
                .Split(new[] {'.', '/'}, StringSplitOptions.RemoveEmptyEntries)
                .ToList()
                .GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
        
        public static string[] Tokens(string value)
        {
            return new JPathTokenizer(value).ToArray();
        }
    }
}