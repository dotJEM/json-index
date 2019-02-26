using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using DotJEM.Json.Index.QueryParsers;
using DotJEM.Json.Index.QueryParsers.Simplified;
using DotJEM.Json.Index.QueryParsers.Simplified.Ast;
using Microsoft.Msagl.Drawing;
using Microsoft.Msagl.GraphViewerGdi;

namespace DotJEM.Json.Index.QueryParser.Visualizer
{
    public partial class LuceneQueryVisualizer : Form
    {
        private readonly GViewer viewer = new GViewer();
        public LuceneQueryVisualizer()
        {
            InitializeComponent();

            ctrlGraph.Controls.Add(viewer);
        }

        private void LuceneQueryVisualizer_Load(object sender, EventArgs e)
        {


         
        }

        private void QueryChanged(object sender, KeyEventArgs e)
        {
            try
            {
                SimplifiedQueryAstParser parser = new SimplifiedQueryAstParser();
                BaseQuery query = parser.Parse(ctrlQuery.Text);
                Graph graph = query.Accept(new GraphBuilderVisitor(), new GraphBuilderVisitor.GraphBuilderContext(new Graph(), "[root]"));

                //Graph graph = new Graph("graph");

                //graph.AddEdge("A", "", "B");

                //graph.FindNode("A").Label.Text = "AAA";

                //graph.AddEdge("A", "", "C");
                //graph.AddEdge("C", "", "D");
                //graph.AddEdge("C", "", "E");

                SuspendLayout();
                viewer.Graph = graph;
                viewer.Dock = DockStyle.Fill;

                ResumeLayout();
            }
            catch (Exception ex)
            {
                ctrlError.Text = ex.ToString();
            }
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

            public void AddEdge(string id, string label)
            {
                Graph.AddEdge(Parent, id).TargetNode.Label.Text = label;
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
            return context.Graph;
        }

        public Graph Visit(OrderedQuery ast, GraphBuilderContext context)
        {
            string id = Guid.NewGuid().ToString("N");
            ast.Query.Accept(this, new GraphBuilderContext(context.Graph, id));
            context.AddEdge(id, "QUERY");

            id = Guid.NewGuid().ToString("N");
            context.AddEdge(id, "ORDER");
            ast.Ordering?.Accept(this, new GraphBuilderContext(context.Graph, id));
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
            return context.Graph;
        }

        public Graph Visit(OrderField ast, GraphBuilderContext context)
        {
            string id = Guid.NewGuid().ToString("N");
            context.AddEdge(id, $"{ast.Name} {ast.SpecifiedOrder}");
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
            return context.Graph;
        }
    }
}
