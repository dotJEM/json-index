using System.Globalization;
using DotJEM.Json.Index.Diagnostics;
using DotJEM.Json.Index.Documents.Fields;
using DotJEM.Json.Index.Documents.Strategies;
using DotJEM.Json.Index.Serialization;
using Newtonsoft.Json.Linq;

namespace DotJEM.Json.Index.Documents.Builder
{
    public class LuceneDocumentBuilder : AbstractLuceneDocumentBuilder
    {
        private readonly IFieldStrategyCollection strategies;

        public LuceneDocumentBuilder(
            IFieldResolver resolver = null, 
            IFieldStrategyCollection strategies = null,
            ILuceneJsonDocumentSerializer documentSerializer = null,
            IEventInfoStream eventInfoStream = null) 
            : base(resolver, documentSerializer, eventInfoStream)
        {
            this.strategies = strategies ?? new NullFieldStrategyCollection();
        }


        protected override void Visit(JArray json, IPathContext context)
        {
            IFieldStrategy strategy = strategies.Resolve(context.Path, JTokenType.Array)
                                      ?? new ArrayFieldStrategy();
            Add(strategy.CreateFields(json, context));
            base.Visit(json, context);
        }

        protected override void VisitInteger(JValue json, IPathContext context)
        {
            IFieldStrategy strategy = strategies.Resolve(context.Path, JTokenType.Integer)
                                      ?? new Int64FieldStrategy();
            Add(strategy.CreateFields(json, context));
            base.VisitInteger(json, context);
        }

        protected override void VisitFloat(JValue json, IPathContext context)
        {
            IFieldStrategy strategy = strategies.Resolve(context.Path, JTokenType.Float)
                                      ?? new DoubleFieldStrategy();
            Add(strategy.CreateFields(json, context));
            base.VisitFloat(json, context);
        }

        protected override void VisitString(JValue json, IPathContext context)
        {
            //TODO: Certain fields should probably work as Identity. So there is cases where this is not good enough.
            IFieldStrategy strategy = strategies.Resolve(context.Path, JTokenType.String)
                                      ?? new TextFieldStrategy();
            Add(strategy.CreateFields(json, context));
            base.VisitString(json, context);
        }

        protected override void VisitBoolean(JValue json, IPathContext context)
        {
            IFieldStrategy strategy = strategies.Resolve(context.Path, JTokenType.Boolean)
                                      ?? new BooleanFieldStrategy();
            Add(strategy.CreateFields(json, context));
            base.VisitBoolean(json, context);
        }

        protected override void VisitNull(JValue json, IPathContext context)
        {
            IFieldStrategy strategy = strategies.Resolve(context.Path, JTokenType.Null)
                                      ?? new NullFieldStrategy("$$NULL$$");
            Add(strategy.CreateFields(json, context));
            base.VisitNull(json, context);
        }

        protected override void VisitUndefined(JValue json, IPathContext context)
        {
            IFieldStrategy strategy = strategies.Resolve(context.Path, JTokenType.Undefined)
                                      ?? new NullFieldStrategy("$$UNDEFINED$$");
            Add(strategy.CreateFields(json, context));
            base.VisitUndefined(json, context);
        }

        protected override void VisitDate(JValue json, IPathContext context)
        {
            IFieldStrategy strategy = strategies.Resolve(context.Path, JTokenType.Date)
                                      ?? new ExpandedDateTimeFieldStrategy();
            Add(strategy.CreateFields(json, context));
            base.VisitDate(json, context);
        }

        protected override void VisitGuid(JValue json, IPathContext context)
        {
            IFieldStrategy strategy = strategies.Resolve(context.Path, JTokenType.Guid)
                                      ?? new IdentityFieldStrategy();
            Add(strategy.CreateFields(json, context));
            base.VisitGuid(json, context);
        }

        protected override void VisitUri(JValue json, IPathContext context)
        {
            IFieldStrategy strategy = strategies.Resolve(context.Path, JTokenType.Uri)
                                      ?? new StringFieldStrategy();
            Add(strategy.CreateFields(json, context));
            base.VisitUri(json, context);
        }

        protected override void VisitTimeSpan(JValue json, IPathContext context)
        {
            IFieldStrategy strategy = strategies.Resolve(context.Path, JTokenType.TimeSpan)
                                      ?? new ExpandedTimeSpanFieldStrategy();
            Add(strategy.CreateFields(json, context));
            base.VisitTimeSpan(json, context);
        }
    }
}
