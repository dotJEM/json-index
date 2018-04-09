using System;
using System.Collections.Generic;
using System.Text;

namespace DotJEM.Json.Index.QueryParsers.Simplified.Ast.Scanner
{
    public interface IFieldCollectorScanner : ISimplifiedQueryAstVisitor<QueryAst, object>
    {
    }

    public class FieldCollectorScanner : SimplifiedQueryAstVisitor<QueryAst, object>, IFieldCollectorScanner
    {
        public override QueryAst Visit(FieldQuery ast, object context)
        {
            if (ast.Name == "$contentType")
            {
                Value value = ast.Value;
                switch (ast.Operator)
                {
                    case FieldOperator.None:
                        throw new NotSupportedException();

                    case FieldOperator.Equals:
                        
                        //ast.Add("FieldCollectorScanner", new EqualsEvaluator(value));
                    case FieldOperator.NotEquals:

                    case FieldOperator.GreaterThan:
                        //Note: Fallback to all
                        throw new NotSupportedException();

                    case FieldOperator.GreaterThanOrEquals:
                        //Note: Fallback to all
                        throw new NotSupportedException();

                    case FieldOperator.LessThan:
                        //Note: Fallback to all
                        throw new NotSupportedException();

                    case FieldOperator.LessThanOrEquals:
                        //Note: Fallback to all
                        throw new NotSupportedException();

                    case FieldOperator.In:
                    case FieldOperator.NotIt:
                    case FieldOperator.Similar:
                        //Note: Fallback to all
                        throw new NotSupportedException();

                    case FieldOperator.NotSimilar:
                        //Note: Fallback to all
                        throw new NotSupportedException();

                }
            }

            return base.Visit(ast, context);
        }

        public override QueryAst Visit(OrQuery ast, object context)
        {
            return base.Visit(ast, context);
        }

        public override QueryAst Visit(AndQuery ast, object context)
        {
            return base.Visit(ast, context);
        }

        public override QueryAst Visit(ImplicitCompositeQuery ast, object context)
        {
            return base.Visit(ast, context);
        }

        public override QueryAst Visit(NotQuery ast, object context)
        {
            return base.Visit(ast, context);
        }
    }

    
}
