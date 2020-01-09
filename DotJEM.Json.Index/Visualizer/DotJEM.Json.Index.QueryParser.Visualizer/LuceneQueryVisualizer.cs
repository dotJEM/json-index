using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using DotJEM.Json.Index.Diagnostics;
using DotJEM.Json.Index.Documents.Fields;
using DotJEM.Json.Index.Documents.Info;
using DotJEM.Json.Index.QueryParsers;
using DotJEM.Json.Index.QueryParsers.Simplified;
using DotJEM.Json.Index.QueryParsers.Simplified.Ast;
using DotJEM.Json.Index.QueryParsers.Simplified.Ast.Optimizer;
using DotJEM.Json.Index.QueryParsers.Simplified.Ast.Scanner;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Search;
using Lucene.Net.Util;
using Microsoft.Msagl.Drawing;
using Microsoft.Msagl.GraphViewerGdi;

namespace DotJEM.Json.Index.QueryParser.Visualizer
{
    public partial class LuceneQueryVisualizer : Form
    {
        private readonly SimplifiedLuceneQueryParser parser = new SimplifiedLuceneQueryParser(
            new DefaultFieldInformationManager(new FieldResolver()), new StandardAnalyzer(LuceneVersion.LUCENE_48) );


        private readonly GViewer viewer = new GViewer();
        public LuceneQueryVisualizer()
        {
            InitializeComponent();

            ctrlGraph.Controls.Add(viewer);
        }

        private void LuceneQueryVisualizer_Load(object sender, EventArgs e)
        {

            ctrlQuery.Text = "(contentType: diary AND name: token) OR (contentType: animal AND color IN (Brown, Yellow, Red)) ORDER BY $created DESC";
            QueryChanged(null, null);
        }

        private void QueryChanged(object sender, KeyEventArgs e)
        {
            BuildGraph();
        }

        private void OptimizeChanged(object sender, EventArgs e)
        {
            BuildGraph();
        }

        private void BuildGraph()
        {
            try
            {
                SimplifiedQueryAstParser parser = new SimplifiedQueryAstParser();
                BaseQuery query = parser.Parse(ctrlQuery.Text);
                if (ctrlOptimize.Text == "Optimized")
                {
                    query = query.Optimize();
                }

                query = query.DecorateWithContentTypes(new DummyManager());
                Graph graph = query.Accept(new GraphBuilderVisitor(), new GraphBuilderVisitor.GraphBuilderContext(new Graph(), "[root]"));

                SuspendLayout();
                viewer.Graph = graph;
                viewer.Dock = DockStyle.Fill;

                Query luceneQuery = this.parser.Parse(ctrlQuery.Text).Query;
                ctrlTranslation.Text = luceneQuery.ToString();

                ResumeLayout();
            }
            catch (Exception ex)
            {
                ctrlError.Text = ex.ToString();
            }
        }
    }

    internal class DummyManager : IFieldInformationManager
    {
        public IInfoEventStream InfoStream { get; }
        public IFieldResolver Resolver { get; } = new FieldResolver(contentTypeField:"contentType");
        public IEnumerable<string> ContentTypes => "animal;diary".Split(';');
        public IEnumerable<string> AllFields => "contentType;name;color;$created;type".Split(';');
        public void Merge(string contentType, IFieldInfoCollection info)
        {
            throw new NotImplementedException();
        }

        public IJsonFieldInfo Lookup(string fieldName)
        {
            throw new NotImplementedException();
        }

        public IJsonFieldInfo Lookup(string contentType, string fieldName)
        {
            throw new NotImplementedException();
        }
    }

    public class GraphBuilderVisitor : ISimplifiedQueryAstVisitor<Graph, GraphBuilderVisitor.GraphBuilderContext>
    {
        public class GraphBuilderContext
        {
            public Graph Graph { get; }
            public string Parent { get; }

            public GraphBuilderContext(Graph graph, string parent)
            {
                Graph = graph;
                Parent = parent;
            }

            public Edge AddEdge(string id, string label)
            {
                Edge edge = Graph.AddEdge(Parent, id);
                edge.TargetNode.Label.Text = label;
                return edge;
            }

            public Edge AddEdge(string id, string target, string label)
            {
                Edge edge = Graph.AddEdge(id, target);
                edge.TargetNode.Label.Text = label;
                return edge;
            }
        }

        public Graph Visit(BaseQuery ast, GraphBuilderContext context)
        {
            throw new NotImplementedException();
        }

        public Graph Visit(NotQuery ast, GraphBuilderContext context)
        {
            string id = Guid.NewGuid().ToString("N");
            context.AddEdge(id, "Not");
            ast.Not.Accept(this, new GraphBuilderContext(context.Graph, id));
            AddContentTypes(ast, context, id);
            return context.Graph;
        }

        private static void AddContentTypes(BaseQuery ast, GraphBuilderContext context, string id)
        {
            if (ast.ContainsKey("contentTypes"))
            {
                Edge edge = context.AddEdge(id, id + "contentTypes", string.Join(";", ast.GetAs<string[]>("contentTypes")));
                edge.TargetNode.Attr.Shape = Shape.Ellipse;
                edge.LabelText = "ContentTypes";
            }
        }

        public Graph Visit(OrderedQuery ast, GraphBuilderContext context)
        {
            string id = Guid.NewGuid().ToString("N");
            ast.Query.Accept(this, new GraphBuilderContext(context.Graph, id));
            context.AddEdge(id, "QUERY");
            AddContentTypes(ast, context, id);

            if (ast.Ordering == null)
                return context.Graph;

            ast.Ordering.Accept(this, context);
            return context.Graph;
        }

        public Graph Visit(OrderBy ast, GraphBuilderContext context)
        {
            string id = Guid.NewGuid().ToString("N");
            context.AddEdge(id, "ORDER BY");
            foreach (OrderField field in ast.OrderFields)
            {
                field.Accept(this, new GraphBuilderContext(context.Graph, id));
            }
            AddContentTypes(ast, context, id);
            return context.Graph;
        }

        public Graph Visit(OrderField ast, GraphBuilderContext context)
        {
            string id = Guid.NewGuid().ToString("N");
            context.AddEdge(id, $"{ast.Name} {ast.SpecifiedOrder}");
            AddContentTypes(ast, context, id);
            return context.Graph;
        }

        public Graph Visit(CompositeQuery ast, GraphBuilderContext context)
        {
            string id = Guid.NewGuid().ToString("N");
            context.AddEdge(id, "COMPOSITE");
            foreach (BaseQuery query in ast.Queries)
            {
                query.Accept(this, new GraphBuilderContext(context.Graph, id));
            }
            AddContentTypes(ast, context, id);
            return context.Graph;
        }

        public Graph Visit(OrQuery ast, GraphBuilderContext context)
        {
            string id = Guid.NewGuid().ToString("N");
            context.AddEdge(id, "OR");
            foreach (BaseQuery query in ast.Queries)
            {
                query.Accept(this, new GraphBuilderContext(context.Graph, id));
            }
            AddContentTypes(ast, context, id);
            return context.Graph;
        }

        public Graph Visit(AndQuery ast, GraphBuilderContext context)
        {
            string id = Guid.NewGuid().ToString("N");
            context.AddEdge(id, "AND");
            foreach (BaseQuery query in ast.Queries)
            {
                query.Accept(this, new GraphBuilderContext(context.Graph, id));
            }
            AddContentTypes(ast, context, id);
            return context.Graph;
        }

        public Graph Visit(ImplicitCompositeQuery ast, GraphBuilderContext context)
        {
            string id = Guid.NewGuid().ToString("N");
            context.AddEdge(id, "IMPLICIT");
            foreach (BaseQuery query in ast.Queries)
            {
                query.Accept(this, new GraphBuilderContext(context.Graph, id));
            }
            AddContentTypes(ast, context, id);
            return context.Graph;
        }

        public Graph Visit(FieldQuery ast, GraphBuilderContext context)
        {
            string id = Guid.NewGuid().ToString("N");
            switch (ast.Operator)
            {
                case FieldOperator.None:
                    context.AddEdge(id, $"{ast.Name} = {ast.Value}");
                    break;
                case FieldOperator.Equals:
                    context.AddEdge(id, $"{ast.Name} = {ast.Value}");
                    break;
                case FieldOperator.NotEquals:
                    context.AddEdge(id, $"{ast.Name} != {ast.Value}");
                    break;
                case FieldOperator.GreaterThan:
                    context.AddEdge(id, $"{ast.Name} > {ast.Value}");
                    break;
                case FieldOperator.GreaterThanOrEquals:
                    context.AddEdge(id, $"{ast.Name} >= {ast.Value}");
                    break;
                case FieldOperator.LessThan:
                    context.AddEdge(id, $"{ast.Name} < {ast.Value}");
                    break;
                case FieldOperator.LessThanOrEquals:
                    context.AddEdge(id, $"{ast.Name} <= {ast.Value}");
                    break;
                case FieldOperator.In:
                    context.AddEdge(id, $"{ast.Name} IN {ast.Value}");
                    break;
                case FieldOperator.NotIt:
                    context.AddEdge(id, $"{ast.Name} NOT IN {ast.Value}");
                    break;
                case FieldOperator.Similar:
                    context.AddEdge(id, $"{ast.Name} ~ {ast.Value}");
                    break;
                case FieldOperator.NotSimilar:
                    context.AddEdge(id, $"{ast.Name} !~ {ast.Value}");
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            AddContentTypes(ast, context, id);
            return context.Graph;
        }
    }
}
