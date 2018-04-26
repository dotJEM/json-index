using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using DotJEM.Json.Index.Util;

namespace DotJEM.Json.Index.Diagnostics
{
    // TODO: Split into exposed and "internal" parts - internal parts should still be public but not part of the interface.
    public interface IInfoEventStream : IObservable<InfoEventArgs>
    {
        IInfoStreamCorrelationScope Scope(Type callerType, Guid correlationId);

        void Exception(Type callerType, string message, Exception ex, Guid correlationId, object[] args, [CallerMemberName]string member = null);
        void Error(Type callerType, string message, Guid correlationId, object[] args, [CallerMemberName]string member = null);
        void Debug(Type callerType, string message, Guid correlationId, object[] args, [CallerMemberName]string member = null);
        void Info(Type callerType, string message, Guid correlationId, object[] args, [CallerMemberName]string member = null);
    }

    public class InfoEventArgs : EventArgs
    {
        public InfoType TypeType { get; }
        public Type CallerType { get; }
        public string Member { get; }
        public string Message { get; }
        public Guid CorrelationId { get; }
        public object[] Args { get; }

        public InfoEventArgs(InfoType typeType, Type callerType, string member, string message, Guid correlationId, object[] args)
        {
            TypeType = typeType;
            CallerType = callerType;
            Member = member;
            Message = message;
            CorrelationId = correlationId;
            Args = args;
        }
    }

    public class InfoExceptionEventArgs : InfoEventArgs
    {
        public Exception Exception { get; }

        public InfoExceptionEventArgs(InfoType typeType, Type callerType, string member, string message, Exception exception, Guid correlationId, object[] args) 
            : base(typeType, callerType, member, message, correlationId, args)
        {
            Exception = exception;
        }
    }

    [Flags]
    public enum InfoType
    {
        None = 0,
        Debug = 1,
        Info = 2, 
        Error = 4,
        Exception = 8,
        All = Debug | Info | Error | Exception
    }

    public interface IInfoEventStreamSubscribtion : IDisposable
    {
        Guid Key { get; }
    }

    public class InfoEventStreamSubscribtion : IInfoEventStreamSubscribtion
    {
        private readonly Action<Guid> unsubscribe;
        public Guid Key { get; }

        public InfoEventStreamSubscribtion(Guid key, Action<Guid> unsubscribe)
        {
            this.unsubscribe = unsubscribe;
            Key = key;
        }

        public void Dispose() => unsubscribe(Key);
    }

    public sealed class InfoEventStream : IInfoEventStream
    {
        private readonly ConcurrentDictionary<Guid, IObserver<InfoEventArgs>> observers 
            = new ConcurrentDictionary<Guid, IObserver<InfoEventArgs>>();

        public IDisposable Subscribe(IObserver<InfoEventArgs> observer)
        {
            Guid key = Guid.NewGuid();
            observers.TryAdd(key, observer);
            return new InfoEventStreamSubscribtion(key, Unsubscribe);
        }


        private void Unsubscribe(Guid key) => observers.TryRemove(key, out var _);

        public IInfoStreamCorrelationScope Scope(Type callerType, Guid correlationId)
        {
            return new InfoStreamCorrelationScope(this, callerType, correlationId);
        }

        public void Debug(Type callerType, string message, Guid correlationId, object[] args, [CallerMemberName]string member = null) 
            => Publish(new InfoEventArgs(InfoType.Debug, callerType, member, message, correlationId, args));

        public void Info(Type callerType, string message, Guid correlationId, object[] args, string member = null) 
            => Publish(new InfoEventArgs(InfoType.Info, callerType, member, message, correlationId, args));

        public void Error(Type callerType, string message, Guid correlationId, object[] args, string member = null) 
            => Publish(new InfoEventArgs(InfoType.Error, callerType, member, message, correlationId, args));

        public void Exception(Type callerType, string message, Exception ex, Guid correlationId, object[] args, string member = null) 
            => Publish(new InfoExceptionEventArgs(InfoType.Exception, callerType, member, message, ex, correlationId, args));

        private void Publish(InfoEventArgs message)
        {
            foreach (IObserver<InfoEventArgs> observer in observers.Values)
            {
                try
                {
                    observer.OnNext(message);
                }
                catch (Exception ex)
                {
                    observer.OnError(ex);
                }
            }
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
        private readonly IInfoEventStream inner;
        private readonly Type callerType;
        private readonly Guid correlationId;

        public InfoStreamCorrelationScope(IInfoEventStream inner, Type callerType, Guid correlationId)
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
