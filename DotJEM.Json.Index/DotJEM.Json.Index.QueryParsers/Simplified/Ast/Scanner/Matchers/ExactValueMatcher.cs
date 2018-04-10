using System;
using System.Collections.Generic;
using System.Linq;

namespace DotJEM.Json.Index.QueryParsers.Simplified.Ast.Scanner.Matchers
{
    public class ExactValueMatcher : IValueMatcher
    {
        private readonly HashSet<string> values;

        public ExactValueMatcher(params string[] values) 
            : this(values.AsEnumerable())
        {
        }

        public ExactValueMatcher(IEnumerable<string> values)
        {
            this.values = new HashSet<string>(values, StringComparer.OrdinalIgnoreCase);
        }

        public bool Matches(string value)
        {
            return values.Contains(value);
        }
    }
}