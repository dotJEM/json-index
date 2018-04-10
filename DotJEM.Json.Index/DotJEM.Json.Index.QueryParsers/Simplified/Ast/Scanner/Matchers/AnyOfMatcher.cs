using System.Collections.Generic;
using System.Linq;

namespace DotJEM.Json.Index.QueryParsers.Simplified.Ast.Scanner.Matchers
{
    public class AnyOfMatcher : IValueMatcher
    {
        private readonly List<IValueMatcher> matchers;

        public AnyOfMatcher(IEnumerable<IValueMatcher> matchers)
        {
            this.matchers = matchers.ToList();
        }

        public bool Matches(string value) => matchers.Any(m => m.Matches(value));
    }
}