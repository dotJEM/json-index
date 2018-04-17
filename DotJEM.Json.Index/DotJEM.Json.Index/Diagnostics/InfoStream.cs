using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using DotJEM.Json.Index.Util;

namespace DotJEM.Json.Index.Diagnostics
{
    public interface IInfoStream
    {
        IInfoStreamCorrelationScope Scope(Type callerType, Guid correlationId);

        void Debug(Type callerType, string message, Guid correlationId, object[] args, [CallerMemberName]string member = null);
        void Info(Type callerType, string message, Guid correlationId, object[] args, [CallerMemberName]string member = null);
    }

    public class InfoStream : IInfoStream
    {
        public IInfoStreamCorrelationScope Scope(Type callerType, Guid correlationId)
        {
            return new InfoStreamCorrelationScope(this, callerType, correlationId);
        }

        public void Debug(Type callerType, string message, Guid correlationId, object[] args, [CallerMemberName]string member = null)
        {
        }

        public void Info(Type callerType, string message, Guid correlationId, object[] args, string member = null)
        {
        }
    }
    public interface IInfoStreamCorrelationScope : IDisposable
    {
        void Debug(string message, object[] args, [CallerMemberName]string member = null);
        void Info(string message, object[] args, [CallerMemberName]string member = null);
    }

    public class InfoStreamCorrelationScope : Disposable, IInfoStreamCorrelationScope
    {
        private readonly IInfoStream inner;
        private readonly Type callerType;
        private readonly Guid correlationId;

        public InfoStreamCorrelationScope(IInfoStream inner, Type callerType, Guid correlationId)
        {
            this.inner = inner;
            this.callerType = callerType;
            this.correlationId = correlationId;
        }

        public void Debug(string message, object[] args, [CallerMemberName]string member = null)
            => inner.Debug(callerType, message, correlationId, args);
        public void Info(string message, object[] args, [CallerMemberName]string member = null)
            => inner.Info(callerType, message, correlationId, args);
    }


}
