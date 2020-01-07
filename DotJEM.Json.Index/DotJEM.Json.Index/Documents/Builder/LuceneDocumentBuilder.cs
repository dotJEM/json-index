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

        public LuceneDocumentBuilder(IFieldStrategyCollection strategies = null, IFieldResolver fields = null, ILuceneJsonDocumentSerializer documentSerializer = null, IInfoEventStream infoStream = null) 
            : base(fields, documentSerializer, infoStream)
        {
            this.strategies = strategies ?? new NullFieldStrategyCollection();
        }

        protected override void Visit(JArray json, IJsonPathContext context)
        {
            IFieldStrategy strategy = strategies.Resolve(context.Path, JTokenType.Array)
                                      ?? new ArrayFieldStrategy();
            context.Apply(strategy);
            base.Visit(json, context);
        }

        protected override void VisitInteger(JValue json, IJsonPathContext context)
        {
            IFieldStrategy strategy = strategies.Resolve(context.Path, JTokenType.Integer)
                                      ?? new Int64FieldStrategy();
            context.Apply(strategy);
            base.VisitInteger(json, context);
        }

        protected override void VisitFloat(JValue json, IJsonPathContext context)
        {
            IFieldStrategy strategy = strategies.Resolve(context.Path, JTokenType.Float)
                                      ?? new DoubleFieldStrategy();
            context.Apply(strategy);
            base.VisitFloat(json, context);
        }

        protected override void VisitString(JValue json, IJsonPathContext context)
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
                context.Apply(strategy ?? new TextFieldStrategy());
            }
            else
            {
                context.Apply(strategy ?? new StringFieldStrategy());
            }
            base.VisitString(json, context);
        }

        protected override void VisitBoolean(JValue json, IJsonPathContext context)
        {
            IFieldStrategy strategy = strategies.Resolve(context.Path, JTokenType.Boolean)
                                      ?? new BooleanFieldStrategy();
            context.Apply(strategy);
            base.VisitBoolean(json, context);
        }

        protected override void VisitNull(JValue json, IJsonPathContext context)
        {
            IFieldStrategy strategy = strategies.Resolve(context.Path, JTokenType.Null)
                                      ?? new NullFieldStrategy("$$NULL$$");
            context.Apply(strategy);
            base.VisitNull(json, context);
        }

        protected override void VisitUndefined(JValue json, IJsonPathContext context)
        {
            IFieldStrategy strategy = strategies.Resolve(context.Path, JTokenType.Undefined)
                                      ?? new NullFieldStrategy("$$UNDEFINED$$");
            context.Apply(strategy);
            base.VisitUndefined(json, context);
        }

        protected override void VisitDate(JValue json, IJsonPathContext context)
        {
            IFieldStrategy strategy = strategies.Resolve(context.Path, JTokenType.Date)
                                      ?? new ExpandedDateTimeFieldStrategy();
            context.Apply(strategy);
            base.VisitDate(json, context);
        }

        protected override void VisitGuid(JValue json, IJsonPathContext context)
        {
            IFieldStrategy strategy = strategies.Resolve(context.Path, JTokenType.Guid)
                                      ?? new IdentityFieldStrategy();
            context.Apply(strategy);
            base.VisitGuid(json, context);
        }

        protected override void VisitUri(JValue json, IJsonPathContext context)
        {
            IFieldStrategy strategy = strategies.Resolve(context.Path, JTokenType.Uri)
                                      ?? new StringFieldStrategy();
            context.Apply(strategy);
            base.VisitUri(json, context);
        }

        protected override void VisitTimeSpan(JValue json, IJsonPathContext context)
        {
            IFieldStrategy strategy = strategies.Resolve(context.Path, JTokenType.TimeSpan)
                                      ?? new ExpandedTimeSpanFieldStrategy();
            context.Apply(strategy);
            base.VisitTimeSpan(json, context);
        }
    }
}
