using System;
using Antlr4.Runtime;
using DotJEM.Index.QueryParsers.Simplified;
using DotJEM.Json.Index.Documents.Fields;
using DotJEM.Json.Index.Documents.Info;
using DotJEM.Json.Index.QueryParsers.Simplified;
using DotJEM.Json.Index.QueryParsers.Simplified.Ast;
using DotJEM.Json.Index.QueryParsers.Simplified.Ast.Optimizer;
using Lucene.Net.Analysis;
using Lucene.Net.Search;
using LuceneQuery = Lucene.Net.Search.Query;

namespace DotJEM.Json.Index.QueryParsers
{
    public class LuceneQueryInfo
    {
        public LuceneQuery Query { get; }
        public Sort Sort { get;  }

        public LuceneQueryInfo(LuceneQuery query, Sort sort = null)
        {
            Query = query;
            Sort = sort;
        }

        public static implicit operator LuceneQueryInfo(LuceneQuery query) => new LuceneQueryInfo(query);
    }

    public interface ILuceneQueryParser
    {
        LuceneQueryInfo Parse(string query);
    }

    public class SimplifiedLuceneQueryParser : ILuceneQueryParser
    {
        private readonly IAstParser<QueryAst> astParser;
        private readonly ISimplifiedQueryAstVisitor<LuceneQueryInfo, FieldContext> visitor;

        public SimplifiedLuceneQueryParser(IFieldInformationManager fields, IFieldResolver resolver, Analyzer analyzer, IAstParser<QueryAst> astParser = null)
            : this(new SimplifiedLuceneQueryAstVisitor(fields, resolver, analyzer), astParser) { }

        public SimplifiedLuceneQueryParser(ISimplifiedQueryAstVisitor<LuceneQueryInfo, FieldContext> visitor, IAstParser<QueryAst> astParser = null)
        {
            this.visitor = visitor ?? throw new ArgumentNullException(nameof(visitor));
            this.astParser = astParser ?? new SimplifiedQueryAstParser();
        }

        public LuceneQueryInfo Parse(string query)
        {
            //Note: All fields!
            return astParser
                .Parse(query)
                .Optimize()
                .Accept(visitor, new FieldContext(FieldOperator.None));
        }
    }

    public interface IAstParser<out TAst>
    {
        TAst Parse(string query);
    }


    public class SimplifiedQueryAstParser : IAstParser<QueryAst>
    {
        public QueryAst Parse(string query)
        {
            if (string.IsNullOrWhiteSpace(query)) throw new ArgumentException("message", nameof(query));

            ICharStream inputStream = new AntlrInputStream(query);
            SimplifiedLexer lexer = new SimplifiedLexer(inputStream);
            ITokenStream tokenStream = new CommonTokenStream(lexer);
            SimplifiedParser parser = new SimplifiedParser(tokenStream);

            lexer.RemoveErrorListeners();
            parser.RemoveErrorListeners();
            CommonErrorListener errors = new CommonErrorListener();
            parser.AddErrorListener(errors);

            SimplifiedParserVisitor visitor = new SimplifiedParserVisitor();
            SimplifiedParser.QueryContext queryContext = parser.query();

            QueryAst result = visitor.Visit(queryContext);
            if (!errors.IsValid)
                throw new Exception($"Error at col {errors.ErrorLocation}: {errors.ErrorMessage}");

            return result;
        }
    }
}
