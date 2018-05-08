using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using DotJEM.Json.Index.Diagnostics;
using DotJEM.Json.Index.Documents.Strategies;
using DotJEM.Json.Index.Serialization;
using Lucene.Net.Index;
using Newtonsoft.Json.Linq;

namespace DotJEM.Json.Index.Documents.Builder
{
    public class LuceneDocumentBuilder : AbstractLuceneDocumentBuilder
    {
        public LuceneDocumentBuilder(IJsonSerializer serializer = null, IInfoEventStream infoStream = null) 
            : base(serializer, infoStream)
        {
        }

        protected override void Visit(JArray json, IJsonPathContext context)
        {
            context.Apply<ArrayFieldStrategy>();
            base.Visit(json, context);
        }

        protected override void VisitInteger(JValue json, IJsonPathContext context)
        {
            context.Apply<Int64FieldStrategy>();
            base.VisitInteger(json, context);
        }

        protected override void VisitFloat(JValue json, IJsonPathContext context)
        {
            context.Apply<DoubleFieldStrategy>();
            base.VisitFloat(json, context);
        }

        protected override void VisitString(JValue json, IJsonPathContext context)
        {
            string str = json.ToString(CultureInfo.InvariantCulture);
            //TODO: This is problematic as PhraseQueries will fail if just a single field is indexed with StringField...
            //      So we need to figure out a better way.
            //      Ideally, if we have our own analyzer which doesn't split GUID's and other things, e.g. is far more simple.
            //      Then we can just use TextField always.
            if (str.Contains(" "))
            {
                context.Apply<TextFieldStrategy>();
            }
            else
            {
                context.Apply<StringFieldStrategy>();
            }
            base.VisitString(json, context);
        }

        protected override void VisitBoolean(JValue json, IJsonPathContext context)
        {
            context.Apply<BooleanFieldStrategy>();
            base.VisitBoolean(json, context);
        }

        protected override void VisitNull(JValue json, IJsonPathContext context)
        {
            context.Apply(new NullFieldStrategy("$$NULL$$"));
            base.VisitNull(json, context);
        }

        protected override void VisitUndefined(JValue json, IJsonPathContext context)
        {
            context.Apply(new NullFieldStrategy("$$UNDEFINED$$"));
            base.VisitUndefined(json, context);
        }

        protected override void VisitDate(JValue json, IJsonPathContext context)
        {
            context.Apply<ExpandedDateTimeFieldStrategy>();
            base.VisitDate(json, context);
        }

        protected override void VisitGuid(JValue json, IJsonPathContext context)
        {
            context.Apply<IdentityFieldStrategy>();
            base.VisitGuid(json, context);
        }

        protected override void VisitUri(JValue json, IJsonPathContext context)
        {
            context.Apply<StringFieldStrategy>();
            base.VisitUri(json, context);
        }

        protected override void VisitTimeSpan(JValue json, IJsonPathContext context)
        {
            context.Apply<ExpandedTimeSpanFieldStrategy>();
            base.VisitTimeSpan(json, context);
        }
    }
}
