using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Newtonsoft.Json.Linq;

namespace DotJEM.Json.Index.Documents.Builder
{
    public class LuceneDocumentBuilder : AbstractLuceneDocumentBuilder
    {
        protected override void Visit(JArray json, IJsonPathContext context)
        {
            context.FieldBuilder<JArray>()
                .AddInt32Field("@count", arr => arr.Count);
            base.Visit(json, context);
        }

        protected override void VisitInteger(JValue json, IJsonPathContext context)
        {
            context.FieldBuilder<long>()
                .AddInt64Field();
            base.VisitInteger(json, context);
        }

        protected override void VisitFloat(JValue json, IJsonPathContext context)
        {
            context.FieldBuilder<double>()
                .AddDoubleField();
            base.VisitFloat(json, context);
        }

        protected override void VisitString(JValue json, IJsonPathContext context)
        {
            string str = json.ToString(CultureInfo.InvariantCulture);
            //TODO: This is problematic as PhraseQueries will fail if just a single field is indexed with StringField...
            //      So we need to figure out a better way.
            if (str.Contains(" "))
            {
                context.FieldBuilder<string>()
                    .AddTextField();
            }
            else
            {
                context.FieldBuilder<string>()
                    .AddStringField();
            }
            base.VisitString(json, context);
        }

        protected override void VisitBoolean(JValue json, IJsonPathContext context)
        {
            context.FieldBuilder<bool>()
                .AddStringField();
            base.VisitBoolean(json, context);
        }

        protected override void VisitNull(JValue json, IJsonPathContext context)
        {
            context.FieldBuilder<object>()
                .AddStringField(s => "$$NULL$$");
            base.VisitNull(json, context);
        }

        protected override void VisitUndefined(JValue json, IJsonPathContext context)
        {
            context.FieldBuilder<object>()
                .AddStringField(s => "$$UNDEFINED$$");
            base.VisitUndefined(json, context);
        }

        protected override void VisitDate(JValue json, IJsonPathContext context)
        {
            //TODO: Can we do better here? Lucene it self seems to use a lexical format for dateTimes
            //
            //   Examples: 
            //      2014-09-10T11:00 => 0hzwfs800
            //      2014-09-10T13:00 => 0hzxzie7z
            //      
            //      The fields below may however provide other search capabilities such as all things creating during the morning etc.
            context.FieldBuilder<DateTime>()
                .AddStringField(v => v.ToString("s"))
                .AddInt64Field("@ticks", v => v.Ticks)
                .AddInt32Field("@year", v => v.Year)
                .AddInt32Field("@month", v => v.Month)
                .AddInt32Field("@day", v => v.Day)
                .AddInt32Field("@hour", v => v.Hour)
                .AddInt32Field("@minute", v => v.Minute);
            base.VisitDate(json, context);
        }

        protected override void VisitGuid(JValue json, IJsonPathContext context)
        {
            context.FieldBuilder<string>()
                .AddStringField();

            base.VisitGuid(json, context);
        }

        protected override void VisitUri(JValue json, IJsonPathContext context)
        {
            context.FieldBuilder<string>()
                .AddStringField();

            base.VisitUri(json, context);
        }

        protected override void VisitTimeSpan(JValue json, IJsonPathContext context)
        {
            context.FieldBuilder<TimeSpan>()
                .AddStringField(v => v.Ticks.ToString())
                .AddInt64Field("@ticks", v => v.Ticks)
                .AddInt32Field("@days", v => v.Days)
                .AddInt32Field("@hours", v => v.Hours)
                .AddInt32Field("@minutes", v => v.Minutes);

            base.VisitTimeSpan(json, context);
        }
    }
}
