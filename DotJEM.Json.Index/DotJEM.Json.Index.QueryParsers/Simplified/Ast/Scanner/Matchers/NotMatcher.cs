namespace DotJEM.Json.Index.QueryParsers.Simplified.Ast.Scanner.Matchers
{
    public class NotMatcher : IValueMatcher
    {
        private readonly IValueMatcher matcher;

        public NotMatcher(IValueMatcher matcher)
        {
            this.matcher = matcher;
        }

        public bool Matches(string value) => !matcher.Matches(value);
    }
}