using DotJEM.Json.Index.QueryParsers.Ast;

namespace DotJEM.Json.Index.QueryParsers.Simplified.Ast.Optimizer
{
    public interface ISimplifiedQueryOptimizationVisitor : ISimplifiedQueryAstVisitor<BaseQuery, object>
    {
    }
}