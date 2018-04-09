using System;
using System.Collections.Generic;
using System.Linq;

namespace DotJEM.Json.Index.QueryParsers.Simplified.Ast.Optimizer
{
    public enum DefaultOperator
    {
        And, Or
    }

    public static class SimplifiedQueryOptimizationExtensions
    {
        public static QueryAst Optimize(this QueryAst ast, DefaultOperator defaultOperator = DefaultOperator.And)
        {
            return ast.Optimize(new SimplifiedQueryOptimizationVisitor(defaultOperator));
        }

        public static QueryAst Optimize(this QueryAst ast, ISimplifiedQueryOptimizationVisitor optimizer)
        {
            return ast.Accept(optimizer, null);
        }
    }

    public interface ISimplifiedQueryOptimizationVisitor : ISimplifiedQueryAstVisitor<QueryAst, object>
    {
    }

    public class SimplifiedQueryOptimizationVisitor : SimplifiedQueryAstVisitor<QueryAst, object>, ISimplifiedQueryOptimizationVisitor
    {
        private readonly DefaultOperator defaultOperator;

        public SimplifiedQueryOptimizationVisitor(DefaultOperator defaultOperator)
        {
            this.defaultOperator = defaultOperator;
        }

        public override QueryAst Visit(QueryAst ast, object context) => ast;

        public override QueryAst Visit(NotQuery ast, object context)
        {
            return ast.Not is NotQuery nq ? nq.Not : ast;
        }

        public override QueryAst Visit(OrQuery ast, object context)
        {
            /* Note: collect similar fields.
             *
             * Examples would be:
             *  - field: x OR field: y OR field: z => field IN (x, y, z);
             *  - field != x OR field != y OR field != z => field NOT IN (x, y, z);
             */
            string GroupName(QueryAst q) => q is FieldQuery fq ? fq.Name : null;
            QueryAst[] optimized =
                (from query in ast.Queries.Select(q => q.Optimize(this))
                group query by GroupName(query) into fieldGroup
                from query in OptimizeFieldGroup(fieldGroup.Key, fieldGroup)
                select query).ToArray();
            //TODO: Currently we do a double optimize here, this is in order to catch a Eq => In along side of a regular In.
            //      Instead of grouping on FieldOperator, we should group on a targeted FieldOperator.
            return optimized.Length > 1 ? new OrQuery(optimized) : optimized[0];

            FieldOperator TargetOperator(FieldOperator source)
            {
                switch (source)
                {
                    case FieldOperator.Equals:
                    case FieldOperator.In: return FieldOperator.In;
                    case FieldOperator.NotEquals: 
                    case FieldOperator.NotIt: return FieldOperator.NotIt;
                    default:
                        return source;
                }
            }

            IEnumerable<QueryAst> OptimizeFieldGroup(string field, IEnumerable<QueryAst> queries)
            {
                return field == null 
                    ? queries.SelectMany(q => q is OrQuery or ? or.Queries : new[] { q })
                    : queries.Cast<FieldQuery>()
                        .GroupBy(f => TargetOperator(f.Operator))
                        .SelectMany(g => OptimizeOperatorGroup(field, g.Key, g));
            }

            IEnumerable<FieldQuery> OptimizeOperatorGroup(string field, FieldOperator key, IEnumerable<FieldQuery> fields)
            {
                List<FieldQuery> fieldsArr = fields.ToList();
                if (fieldsArr.Count < 2)
                    return fieldsArr;

                switch (key)
                {
                    case FieldOperator.In:
                        //Note: Captures "Equals" as well as In is a suitable target for multiple Or Equals
                        IEnumerable<Value> inValues =
                            from value in fieldsArr.Select(f => f.Value).SelectMany(val => val is ListValue list ? list.Values : new[] { val })
                            select value;
                        return new[] { new FieldQuery(field, FieldOperator.In, new ListValue(inValues.ToArray())) };

                    case FieldOperator.NotIt:
                        //Note: Captures "NotEquals" as well as NotIn is a suitable target for multiple Or NotEquals
                        IEnumerable<Value> notInValues =
                            from value in fieldsArr.Select(f => f.Value).SelectMany(val => val is ListValue list ? list.Values : new[] { val })
                            select value;
                        return new[] { new FieldQuery(field, FieldOperator.NotIt, new ListValue(notInValues.ToArray())) };

                    default:
                        return fieldsArr;
                }
            }
        }

        public override QueryAst Visit(AndQuery ast, object context)
        {
            /* Note: collect similar fields.
             *
             * Examples would be:
             *  - field > 0 AND field < 100 => field: [0 TO 100]; (Range Query, even though the Parser does not support it.
             * 
             */
            string GroupName(QueryAst q) => q is FieldQuery fq ? fq.Name : null;
            QueryAst[] optimized =
                (from query in ast.Queries.Select(q => q.Optimize(this))
                 group query by GroupName(query) into fieldGroup
                 from query in OptimizeFieldGroup(fieldGroup.Key, fieldGroup)
                 select query).ToArray();
            return optimized.Length > 1 ? new AndQuery(optimized) : optimized[0];

            IEnumerable<QueryAst> OptimizeFieldGroup(string field, IEnumerable<QueryAst> queries)
            {
                return field == null
                    ? queries.SelectMany(q => q is AndQuery or ? or.Queries : new[] { q })
                    : queries.Cast<FieldQuery>()
                        .GroupBy(f => f.Operator)
                        .SelectMany(g => OptimizeOperatorGroup(field, g.Key, g));
            }

            IEnumerable<FieldQuery> OptimizeOperatorGroup(string field, FieldOperator key, IEnumerable<FieldQuery> fields)
            {
                List<FieldQuery> fieldsArr = fields.ToList();
                if (fieldsArr.Count < 2)
                    return fieldsArr;

                /* Note: We can't actually optimize > and < ...
                 *
                 * This is because of situations where multiple tokens are respected, e.g. lets say we have a document with:
                 *
                 * document 1:
                 *  - field: age = 50
                 *  - feild: age = 100
                 *
                 * if we search for: age > 60 and age < 90, the above is actually a hit... this may be counter intuitive, but
                 * age = 50 satisfies age < 90 while age = 100 satisfies age > 60...
                 * So we NEED an actual range query!?!?..
                 *
                 * Extra Note: It actually looks like Lucene it self fails here, have to do further verifications, but so far it looks like two range queries
                 * that fits the above won't yield the results we would expect.
                 */

                switch (key)
                {
                    case FieldOperator.None:
                    case FieldOperator.Equals:
                    case FieldOperator.NotEquals:
                    case FieldOperator.GreaterThan:
                    case FieldOperator.GreaterThanOrEquals:
                    case FieldOperator.LessThan:
                    case FieldOperator.LessThanOrEquals:
                    case FieldOperator.In:
                    case FieldOperator.NotIt:
                    case FieldOperator.Similar:
                    case FieldOperator.NotSimilar:
                        return fieldsArr;
                    default:
                        return fieldsArr;
                }
            }

        }

        public override QueryAst Visit(ImplicitCompositeQuery ast, object context)
        {
            switch (defaultOperator)
            {
                case DefaultOperator.And:
                    return new AndQuery(ast.Queries.ToArray()).Optimize(this);
                case DefaultOperator.Or:
                    return new OrQuery(ast.Queries.ToArray()).Optimize(this);
                default:
                    throw new ArgumentOutOfRangeException();
            }

        }
    }

    public static class EnumerableExtentions
    {
        public static (IEnumerable<TOut1>, IEnumerable<TOut2>)  Split<TIn, TOut1, TOut2>(this IEnumerable<TIn> source, Func<TIn, (TOut1, TOut2)> splitter)
        {
            TwinConsumerCollection<TIn, TOut1, TOut2> twin = new TwinConsumerCollection<TIn, TOut1, TOut2>(source, splitter);
            return (twin.Out1, twin.Out2);
        }
    }

    public class TwinConsumerCollection<TIn, TOut1, TOut2>
    {
        //private IEnumerable<TIn> source;
        //private readonly Func<TIn, (TOut1, TOut2)> splitter;

        public IEnumerable<TOut1> Out1 { get; }
        public IEnumerable<TOut2> Out2 { get; }

        public TwinConsumerCollection(IEnumerable<TIn> source, Func<TIn, (TOut1, TOut2)> splitter)
        {
            //this.source = source;
            //this.splitter = splitter;
            //TODO: Candidate for some sort of coordinated Producer/Consumer implementation where the Source is the consumer and it produces for the two outputs using the splitter.

            List<TOut1> out1 = new List<TOut1>();
            List<TOut2> out2 = new List<TOut2>();
            foreach (TIn @in in source)
            {
                (TOut1 o1, TOut2 o2) = splitter(@in);
                out1.Add(o1);
                out2.Add(o2);
            }

            Out1 = out1;
            Out2 = out2;
        }



    }

}
