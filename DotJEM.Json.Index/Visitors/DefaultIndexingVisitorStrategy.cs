using System;
using System.Collections.Generic;
using System.Globalization;
using DotJEM.Json.Index.Configuration;
using Lucene.Net.Documents;
using Lucene.Net.Search;
using Newtonsoft.Json.Linq;

namespace DotJEM.Json.Index.Visitors
{
    public interface IIndexingVisitorStrategy
    {
        bool Visit(Action<IFieldable> add, JValue token, string value, IDocumentBuilderContext context);
        bool Visit(Action<IFieldable> add, JValue token, double value, IDocumentBuilderContext context);
        bool Visit(Action<IFieldable> add, JValue token, long value, IDocumentBuilderContext context);
        bool Visit(Action<IFieldable> add, JValue token, bool value, IDocumentBuilderContext context);
        bool Visit(Action<IFieldable> add, JValue token, DateTime value, IDocumentBuilderContext context);
        bool Visit(Action<IFieldable> add, JValue token, Guid value, IDocumentBuilderContext context);
        bool Visit(Action<IFieldable> add, JValue token, Uri value, IDocumentBuilderContext context);
        bool Visit(Action<IFieldable> add, JValue token, TimeSpan value, IDocumentBuilderContext context);

        bool VisitNull(Action<IFieldable> add, JValue token, IDocumentBuilderContext context);
        bool VisitUndefined(Action<IFieldable> add, JValue token, IDocumentBuilderContext context);
    }

    public class DefaultIndexingVisitorStrategy : IIndexingVisitorStrategy
    {
        public virtual bool Visit(Action<IFieldable> add, JValue token, string value, IDocumentBuilderContext context) => false;
        public virtual bool Visit(Action<IFieldable> add, JValue token, double value, IDocumentBuilderContext context) => false;
        public virtual bool Visit(Action<IFieldable> add, JValue token, long value, IDocumentBuilderContext context) => false;
        public virtual bool Visit(Action<IFieldable> add, JValue token, bool value, IDocumentBuilderContext context) => false;
        public virtual bool Visit(Action<IFieldable> add, JValue token, DateTime value, IDocumentBuilderContext context) => false;
        public virtual bool Visit(Action<IFieldable> add, JValue token, TimeSpan value, IDocumentBuilderContext context) => false;
        public virtual bool Visit(Action<IFieldable> add, JValue token, Guid value, IDocumentBuilderContext context) => false;
        public virtual bool Visit(Action<IFieldable> add, JValue token, Uri value, IDocumentBuilderContext context) => false;

        public virtual bool VisitNull(Action<IFieldable> add, JValue token, IDocumentBuilderContext context) => false;
        public virtual bool VisitUndefined(Action<IFieldable> add, JValue token, IDocumentBuilderContext context) => false;
    }

    public class ImplementationSelectorIndexingVisitorStrategy : IIndexingVisitorStrategy
    {
        private readonly Dictionary<JTokenType, Func<Action<IFieldable>, JValue, IDocumentBuilderContext, bool>> overrides 
            = new Dictionary<JTokenType, Func<Action<IFieldable>, JValue, IDocumentBuilderContext, bool>>();

        protected Func<Action<IFieldable>, JValue, IDocumentBuilderContext, bool> DefaultImplementation { get; set; } = (action, token, ctx) => false;
        
        public virtual bool Visit(Action<IFieldable> add, JValue token, string value, IDocumentBuilderContext context)=> Select(token.Type, context)(add, token, context);
        public virtual bool Visit(Action<IFieldable> add, JValue token, double value, IDocumentBuilderContext context) => Select(token.Type, context)(add, token, context);
        public virtual bool Visit(Action<IFieldable> add, JValue token, long value, IDocumentBuilderContext context) => Select(token.Type, context)(add, token, context);
        public virtual bool Visit(Action<IFieldable> add, JValue token, bool value, IDocumentBuilderContext context) => Select(token.Type, context)(add, token, context);
        public virtual bool Visit(Action<IFieldable> add, JValue token, DateTime value, IDocumentBuilderContext context) => Select(token.Type, context)(add, token, context);
        public virtual bool Visit(Action<IFieldable> add, JValue token, TimeSpan value, IDocumentBuilderContext context) => Select(token.Type, context)(add, token, context);
        public virtual bool Visit(Action<IFieldable> add, JValue token, Guid value, IDocumentBuilderContext context) => Select(token.Type, context)(add, token, context);
        public virtual bool Visit(Action<IFieldable> add, JValue token, Uri value, IDocumentBuilderContext context) => Select(token.Type, context)(add, token, context);
        public virtual bool VisitNull(Action<IFieldable> add, JValue token, IDocumentBuilderContext context) => Select(token.Type, context)(add, token, context);
        public virtual bool VisitUndefined(Action<IFieldable> add, JValue token, IDocumentBuilderContext context) => Select(token.Type, context)(add, token, context);

        protected virtual Func<Action<IFieldable>, JValue, IDocumentBuilderContext, bool> Select(JTokenType type, IDocumentBuilderContext context)
        {
            Func<Action<IFieldable>, JValue, IDocumentBuilderContext, bool> impl;
            return overrides.TryGetValue(type, out impl) ? impl : DefaultImplementation;
        }

        public ImplementationSelectorIndexingVisitorStrategy Override(JTokenType key, Func<Action<IFieldable>, JValue, IDocumentBuilderContext, bool> value)
        {
            overrides[key] = value;
            return this;
        }

        public Func<Action<IFieldable>, JValue, IDocumentBuilderContext, bool> this[JTokenType key]
        {
            get { return overrides[key]; }
            set { overrides[key] = value; }
        }
    }

    public class NullFieldIndexingVisitorStrategy : ImplementationSelectorIndexingVisitorStrategy
    {
        protected override Func<Action<IFieldable>, JValue, IDocumentBuilderContext, bool> Select(JTokenType type, IDocumentBuilderContext context)
        {
            return Visit;
        }

        private bool Visit(Action<IFieldable> add, JValue value, IDocumentBuilderContext context) => true;
    }

    public class TermFieldIndexingVisitorStrategy : ImplementationSelectorIndexingVisitorStrategy
    {
        protected override Func<Action<IFieldable>, JValue, IDocumentBuilderContext, bool> Select(JTokenType type, IDocumentBuilderContext context)
        {
            return Visit;
        }

        private bool Visit(Action<IFieldable> add, JValue value, IDocumentBuilderContext context)
        {
            string str = value.ToString(CultureInfo.InvariantCulture);
            add(new Field(context.Path, str, Field.Store.NO, Field.Index.ANALYZED));
            add(new Field(context.Path, str, Field.Store.NO, Field.Index.NOT_ANALYZED));
            return true;
        }
    }
    
}