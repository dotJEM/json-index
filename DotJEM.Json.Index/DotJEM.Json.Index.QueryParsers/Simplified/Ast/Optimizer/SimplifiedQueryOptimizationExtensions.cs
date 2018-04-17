namespace DotJEM.Json.Index.QueryParsers.Simplified.Ast.Optimizer
{
    public static class SimplifiedQueryOptimizationExtensions
    {
        public static BaseQuery Optimize(this BaseQuery ast, DefaultOperator defaultOperator = DefaultOperator.And)
        {
            return ast.Optimize(new SimplifiedQueryOptimizationVisitor(defaultOperator));
        }

        public static BaseQuery Optimize(this BaseQuery ast, ISimplifiedQueryOptimizationVisitor optimizer)
        {
            return ast.Accept(optimizer, null);
        }
    }
}
