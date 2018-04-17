using System.Collections.Generic;
using System.Linq;

namespace DotJEM.Json.Index.QueryParsers.Simplified.Ast.Scanner.Matchers
{
    public class AllOfMatcher : IValueMatcher
    {
        private readonly List<IValueMatcher> matchers;

        public AllOfMatcher(IEnumerable<IValueMatcher> matchers)
        {
            this.matchers = matchers.ToList();
        }

        public bool Matches(string value) => matchers.All(m => m.Matches(value));
    }
}