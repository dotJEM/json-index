using System;
using Newtonsoft.Json.Linq;

namespace DotJEM.Json.Index.Visitors
{
    //NOTE: This is actually not a true visitor implementation.
    //      But it's the closest descriptive name I could find for now.
    public interface IJTokenVisitor<in TContext>
    {
        void Visit(JToken json, TContext context);
    }
    
    public abstract class AbstractJTokenVisitor<TContext> : IJTokenVisitor<TContext> 
    {
        protected virtual void VisitArray(JArray json, TContext context)
        {
            foreach (JToken token in json)
                Visit(token, context);
        }

        protected virtual void VisitObject(JObject json, TContext context)
        {
            foreach (JProperty property in json.Properties())
                Visit(property, context);
        }

        protected virtual void VisitProperty(JProperty json, TContext context)
        {
            Visit(json.Value, context);
        }

        protected virtual void VisitNone(JToken json, TContext context) { }
        protected virtual void VisitConstructor(JConstructor json, TContext context) { }
        protected virtual void VisitComment(JToken json, TContext context) { }
        protected virtual void VisitInteger(JValue json, TContext context) { }
        protected virtual void VisitFloat(JValue json, TContext context) { }
        protected virtual void VisitString(JValue json, TContext context) { }
        protected virtual void VisitBoolean(JValue json, TContext context) { }
        protected virtual void VisitNull(JValue json, TContext context) { }
        protected virtual void VisitUndefined(JValue json, TContext context) { }
        protected virtual void VisitDate(JValue json, TContext context) { }
        protected virtual void VisitRaw(JRaw json, TContext context) { }
        protected virtual void VisitBytes(JValue json, TContext context) { }
        protected virtual void VisitGuid(JValue json, TContext context) { }
        protected virtual void VisitUri(JValue json, TContext context) { }
        protected virtual void VisitTimeSpan(JValue json, TContext context) { }

        public void Visit(JToken json, TContext context)
        {
            switch (json.Type)
            {
                case JTokenType.None:
                    VisitNone(json, context);
                    break;
                case JTokenType.Object:
                    VisitObject((JObject)json, context);
                    break;
                case JTokenType.Array:
                    VisitArray((JArray)json, context);
                    break;
                case JTokenType.Constructor:
                    VisitConstructor((JConstructor)json, context);
                    break;
                case JTokenType.Property:
                    VisitProperty((JProperty)json, context);
                    break;
                case JTokenType.Comment:
                    VisitComment(json, context);
                    break;
                case JTokenType.Integer:
                    VisitInteger((JValue)json, context);
                    break;
                case JTokenType.Float:
                    VisitFloat((JValue)json, context);
                    break;
                case JTokenType.String:
                    VisitString((JValue)json, context);
                    break;
                case JTokenType.Boolean:
                    VisitBoolean((JValue)json, context);
                    break;
                case JTokenType.Null:
                    VisitNull((JValue)json, context);
                    break;
                case JTokenType.Undefined:
                    VisitUndefined((JValue)json, context);
                    break;
                case JTokenType.Date:
                    VisitDate((JValue)json, context);
                    break;
                case JTokenType.Raw:
                    VisitRaw((JRaw)json, context);
                    break;
                case JTokenType.Bytes:
                    VisitBytes((JValue)json, context);
                    break;
                case JTokenType.Guid:
                    VisitGuid((JValue)json, context);
                    break;
                case JTokenType.Uri:
                    VisitUri((JValue)json, context);
                    break;
                case JTokenType.TimeSpan:
                    VisitTimeSpan((JValue)json, context);
                    break;
            }
        }
    }
}