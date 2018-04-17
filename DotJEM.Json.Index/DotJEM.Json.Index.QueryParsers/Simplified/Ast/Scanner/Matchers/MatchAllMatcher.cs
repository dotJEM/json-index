namespace DotJEM.Json.Index.QueryParsers.Simplified.Ast.Scanner.Matchers
{
    public class MatchAllMatcher : IValueMatcher
    {
        public bool Matches(string value) => true;
    }
}