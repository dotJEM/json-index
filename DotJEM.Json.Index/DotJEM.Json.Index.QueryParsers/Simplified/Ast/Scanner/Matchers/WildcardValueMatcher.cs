using System.Text.RegularExpressions;

namespace DotJEM.Json.Index.QueryParsers.Simplified.Ast.Scanner.Matchers
{
    public class WildcardValueMatcher : IValueMatcher
    {
        private readonly Regex expression;

        public WildcardValueMatcher(string value)
        {
            expression = new Regex(WildcardToRegex(value), RegexOptions.Compiled | RegexOptions.IgnoreCase);
            string WildcardToRegex(string pattern)
            {
                return "^" + Regex.Escape(pattern).
                           Replace("\\*", ".*").
                           Replace("\\?", ".") + "$";
            }
        }


        public bool Matches(string value) => expression.IsMatch(value);
    }
}