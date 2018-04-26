using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using DotJEM.Json.Index.Util;

namespace DotJEM.Json.Index.Diagnostics
{
    public interface IInfoStream
    {
        event EventHandler<InfoStreamEventArgs> OnDebugMessage;
        event EventHandler<InfoStreamEventArgs> OnInfoMessage;
        event EventHandler<InfoStreamEventArgs> OnErrorMessage;
        event EventHandler<InfoStreamEventArgs> OnExceptionMessage;

        IInfoStreamCorrelationScope Scope(Type callerType, Guid correlationId);

        void Exception(Type callerType, string message, Exception ex, Guid correlationId, object[] args, [CallerMemberName]string member = null);
        void Error(Type callerType, string message, Guid correlationId, object[] args, [CallerMemberName]string member = null);
        void Debug(Type callerType, string message, Guid correlationId, object[] args, [CallerMemberName]string member = null);
        void Info(Type callerType, string message, Guid correlationId, object[] args, [CallerMemberName]string member = null);
    }

    public class InfoStreamEventArgs : EventArgs
    {
    }

    public class InfoStreamExceptionEventArgs : InfoStreamEventArgs
    {

    }


    public class InfoStream : IInfoStream
    {
        public event EventHandler<InfoStreamEventArgs> OnDebugMessage;
        public event EventHandler<InfoStreamEventArgs> OnInfoMessage;
        public event EventHandler<InfoStreamEventArgs> OnErrorMessage;
        public event EventHandler<InfoStreamEventArgs> OnExceptionMessage;

        public virtual IInfoStreamCorrelationScope Scope(Type callerType, Guid correlationId)
        {
            return new InfoStreamCorrelationScope(this, callerType, correlationId);
        }

        public virtual void Debug(Type callerType, string message, Guid correlationId, object[] args, [CallerMemberName]string member = null)
        {
        }

        public virtual void Info(Type callerType, string message, Guid correlationId, object[] args, string member = null)
        {
        }
        public virtual void Exception(Type callerType, string message, Exception ex, Guid correlationId, object[] args, string member = null)
        {
        }

        public virtual void Error(Type callerType, string message, Guid correlationId, object[] args, string member = null)
        {
        }

        protected virtual void OnOnDebugMessage(InfoStreamEventArgs e)
        {
            OnDebugMessage?.Invoke(this, e);
        }

        protected virtual void OnOnInfoMessage(InfoStreamEventArgs e)
        {
            OnInfoMessage?.Invoke(this, e);
        }

        protected virtual void OnOnErrorMessage(InfoStreamEventArgs e)
        {
            OnErrorMessage?.Invoke(this, e);
        }

        protected virtual void OnOnExceptionMessage(InfoStreamEventArgs e)
        {
            OnExceptionMessage?.Invoke(this, e);
        }
    }
    public interface IInfoStreamCorrelationScope : IDisposable
    {
        void Debug(string message, object[] args, [CallerMemberName]string member = null);
        void Info(string message, object[] args, [CallerMemberName]string member = null);
        void Error(string message, object[] args, [CallerMemberName]string member = null);
        void Exception(string message, Exception ex, object[] args, [CallerMemberName]string member = null);
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
        public void Error(string message, object[] args, [CallerMemberName]string member = null)
            => inner.Error(callerType, message, correlationId, args);
        public void Exception(string message, Exception ex, object[] args, [CallerMemberName]string member = null)
            => inner.Exception(callerType, message, ex, correlationId, args);
    }
}
