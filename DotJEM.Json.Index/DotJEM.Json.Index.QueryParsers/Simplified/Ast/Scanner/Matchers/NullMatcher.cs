namespace DotJEM.Json.Index.QueryParsers.Simplified.Ast.Scanner.Matchers
{
    public class NullMatcher : IValueMatcher
    {
        public bool Matches(string value) => false;
    }
}