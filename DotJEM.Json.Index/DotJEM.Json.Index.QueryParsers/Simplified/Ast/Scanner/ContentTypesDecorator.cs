using System;
using System.Linq;
using DotJEM.Json.Index.Documents.Info;
using DotJEM.Json.Index.QueryParsers.Simplified.Ast.Scanner.Matchers;

namespace DotJEM.Json.Index.QueryParsers.Simplified.Ast.Scanner
{
    public interface IContentTypesDecorator : ISimplifiedQueryAstVisitor<IValueMatcher, object>
    {
    }

    public class ContentTypesDecorator : SimplifiedQueryAstVisitor<IValueMatcher, object>, IContentTypesDecorator
    {
        private readonly IFieldInformationManager fieldsInfo;

        public ContentTypesDecorator(IFieldInformationManager fieldsInfo)
        {
            this.fieldsInfo = fieldsInfo;
        }

        public override IValueMatcher Visit(FieldQuery ast, object context)
        {
            if (ast.Name != fieldsInfo.Resolver.ContentTypeField)
                return new MatchAllMatcher();

            switch (ast.Operator)
            {
                case FieldOperator.None:
                    throw new NotSupportedException();

                case FieldOperator.Equals:
                case FieldOperator.In:
                    return CreateMatcher(ast.Value);

                case FieldOperator.NotEquals:
                case FieldOperator.NotIt:
                    return new NotMatcher(CreateMatcher(ast.Value));

                case FieldOperator.GreaterThanOrEquals:
                case FieldOperator.GreaterThan:
                case FieldOperator.LessThan:
                case FieldOperator.LessThanOrEquals:
                case FieldOperator.Similar:
                case FieldOperator.NotSimilar:
                    return new NullMatcher();
            }
            return new MatchAllMatcher();

            IValueMatcher CreateMatcher(Value value)
            {
                switch (value)
                {
                    case ListValue listValue:
                        return new AnyOfMatcher(listValue.Values.Select(CreateMatcher).Where(m => m != null));
                    case MatchAllValue _:
                        return new MatchAllMatcher();
                    case WildcardValue wildcardValue:
                        return new WildcardValueMatcher(wildcardValue.Value);
                    case StringValue stringValue:
                        return new ExactValueMatcher(stringValue.Value);
                    case NumberValue _:
                    case OffsetDateTime _:
                    case PhraseValue _:
                    case DateTimeValue _:
                    case IntegerValue _:
                        return new NullMatcher();
                }
                return new NullMatcher();
            }
        }


        public override IValueMatcher Visit(OrQuery ast, object context) 
            => Decorate(ast, new AnyOfMatcher(ast.Queries.Select(q => q.Accept(this, context)).Where(m => m != null)));

        public override IValueMatcher Visit(AndQuery ast, object context) 
            => Decorate(ast, new AllOfMatcher(ast.Queries.Select(q => q.Accept(this, context)).Where(m => m != null)));

        public override IValueMatcher Visit(ImplicitCompositeQuery ast, object context)
            => throw new NotSupportedException();

        public override IValueMatcher Visit(NotQuery ast, object context)
        {
            IValueMatcher matcher = ast.Not.Accept(this, context);
            return Decorate(ast, matcher != null ? (IValueMatcher)new NotMatcher(matcher) : new MatchAllMatcher());
        }

        //Note: Ordering does not matter.
        public override IValueMatcher Visit(OrderedQuery ast, object context) 
            => Decorate(ast, ast.Query.Accept(this, context) ?? new MatchAllMatcher());

        private IValueMatcher Decorate(BaseQuery ast, IValueMatcher matcher)
        {
            ast.Add("contentTypes", fieldsInfo.ContentTypes.Where(matcher.Matches).ToArray());
            return matcher;
        }
    }
}