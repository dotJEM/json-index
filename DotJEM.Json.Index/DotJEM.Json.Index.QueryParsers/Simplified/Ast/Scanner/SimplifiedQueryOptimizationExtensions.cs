using System.Text;
using DotJEM.Json.Index.Documents.Info;
using DotJEM.Json.Index.QueryParsers.Ast;

namespace DotJEM.Json.Index.QueryParsers.Simplified.Ast.Scanner
{
    public static class SimplifiedQueryOptimizationExtensions
    {
        public static BaseQuery DecorateWithContentTypes(this BaseQuery ast, IFieldInformationManager fields)
            => ast.DecorateWithContentTypes(new ContentTypesDecorator(fields));

        public static BaseQuery DecorateWithContentTypes(this BaseQuery ast, IContentTypesDecorator decorator)
        {
            ast.Accept(decorator, null);
            return ast;
        }
    }
}

