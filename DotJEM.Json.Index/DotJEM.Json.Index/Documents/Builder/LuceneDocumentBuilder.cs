using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using DotJEM.Json.Index.Diagnostics;
using DotJEM.Json.Index.Documents.Fields;
using DotJEM.Json.Index.Documents.Strategies;
using DotJEM.Json.Index.Serialization;
using Lucene.Net.Index;
using Newtonsoft.Json.Linq;

namespace DotJEM.Json.Index.Documents.Builder
{
    public class LuceneDocumentBuilder : AbstractLuceneDocumentBuilder
    {
        private readonly IFieldStrategyCollection strategies;

        public LuceneDocumentBuilder(IFieldStrategyCollection strategies = null, ILuceneJsonDocumentSerializer documentSerializer = null, IInfoEventStream infoStream = null) 
            : base(documentSerializer, infoStream)
        {
            this.strategies = strategies ?? new NullFieldStrategyCollection();
        }

        protected override void Visit(JArray json, IPathContext context)
        {
            IFieldStrategy strategy = strategies.Resolve(context.Path, JTokenType.Array)
                                      ?? new ArrayFieldStrategy();
            strategy.CreateFields(json, context);
            base.Visit(json, context);
        }

        protected override void VisitInteger(JValue json, IPathContext context)
        {
            IFieldStrategy strategy = strategies.Resolve(context.Path, JTokenType.Integer)
                                      ?? new Int64FieldStrategy();
            strategy.CreateFields(json, context);
            base.VisitInteger(json, context);
        }

        protected override void VisitFloat(JValue json, IPathContext context)
        {
            IFieldStrategy strategy = strategies.Resolve(context.Path, JTokenType.Float)
                                      ?? new DoubleFieldStrategy();
            strategy.CreateFields(json, context);
            base.VisitFloat(json, context);
        }

        protected override void VisitString(JValue json, IPathContext context)
        {
            IFieldStrategy strategy = strategies.Resolve(context.Path, JTokenType.String);
            //
            string str = json.ToString(CultureInfo.InvariantCulture);
            //TODO: This is problematic as PhraseQueries will fail if just a single field is indexed with StringField...
            //      So we need to figure out a better way.
            //      Ideally, if we have our own analyzer which doesn't split GUID's and other things, e.g. is far more simple.
            //      Then we can just use TextField always.
            if (str.Contains(" "))
            {
                strategy = strategy ?? new TextFieldStrategy(); 
                strategy.CreateFields(json, context);
            }
            else
            {
                strategy = strategy ?? new StringFieldStrategy(); 
                strategy.CreateFields(json, context);
            }
            base.VisitString(json, context);
        }

        protected override void VisitBoolean(JValue json, IPathContext context)
        {
            IFieldStrategy strategy = strategies.Resolve(context.Path, JTokenType.Boolean)
                                      ?? new BooleanFieldStrategy();
            strategy.CreateFields(json, context);
            base.VisitBoolean(json, context);
        }

        protected override void VisitNull(JValue json, IPathContext context)
        {
            IFieldStrategy strategy = strategies.Resolve(context.Path, JTokenType.Null)
                                      ?? new NullFieldStrategy("$$NULL$$");
            strategy.CreateFields(json, context);
            base.VisitNull(json, context);
        }

        protected override void VisitUndefined(JValue json, IPathContext context)
        {
            IFieldStrategy strategy = strategies.Resolve(context.Path, JTokenType.Undefined)
                                      ?? new NullFieldStrategy("$$UNDEFINED$$");
            strategy.CreateFields(json, context);
            base.VisitUndefined(json, context);
        }

        protected override void VisitDate(JValue json, IPathContext context)
        {
            IFieldStrategy strategy = strategies.Resolve(context.Path, JTokenType.Date)
                                      ?? new ExpandedDateTimeFieldStrategy();
            strategy.CreateFields(json, context);
            base.VisitDate(json, context);
        }

        protected override void VisitGuid(JValue json, IPathContext context)
        {
            IFieldStrategy strategy = strategies.Resolve(context.Path, JTokenType.Guid)
                                      ?? new IdentityFieldStrategy();
            strategy.CreateFields(json, context);
            base.VisitGuid(json, context);
        }

        protected override void VisitUri(JValue json, IPathContext context)
        {
            IFieldStrategy strategy = strategies.Resolve(context.Path, JTokenType.Uri)
                                      ?? new StringFieldStrategy();
            strategy.CreateFields(json, context);
            base.VisitUri(json, context);
        }

        protected override void VisitTimeSpan(JValue json, IPathContext context)
        {
            IFieldStrategy strategy = strategies.Resolve(context.Path, JTokenType.TimeSpan)
                                      ?? new ExpandedTimeSpanFieldStrategy();
            strategy.CreateFields(json, context);
            base.VisitTimeSpan(json, context);
        }
    }
}
