﻿using System;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using DotJEM.Json.Index.Util;

namespace DotJEM.Json.Index.Diagnostics
{
    // TODO: Split into exposed and "internal" parts - internal parts should still be public but not part of the interface.
    public interface IEventInfoStream : IObservable<InfoEventArgs>
    {
        ITypeBoundEventInfoStream Bind<TCaller>();
        ITypeBoundEventInfoStream Bind(Type callerType);
        IInfoStreamCorrelationScope Scope(Type callerType);
        IInfoStreamCorrelationScope Scope(Type callerType, Guid correlationId);

        void Exception(Type callerType, string message, Exception ex, Guid correlationId, object[] args, [CallerMemberName]string member = null);
        void Error(Type callerType, string message, Guid correlationId, object[] args, [CallerMemberName]string member = null);
        void Debug(Type callerType, string message, Guid correlationId, object[] args, [CallerMemberName]string member = null);
        void Info(Type callerType, string message, Guid correlationId, object[] args, [CallerMemberName]string member = null);
    }



    public class InfoEventArgs : EventArgs
    {
        public InfoType InfoType { get; }
        public Type CallerType { get; }
        public string Member { get; }
        public string Message { get; }
        public Guid CorrelationId { get; }
        public object[] Args { get; }

        public InfoEventArgs(InfoType infoType, Type callerType, string member, string message, Guid correlationId, object[] args)
        {
            InfoType = infoType;
            CallerType = callerType;
            Member = member;
            Message = message;
            CorrelationId = correlationId;
            Args = args;
        }

        public override string ToString()
        {
            return CorrelationId == Guid.Empty 
                ? $"{InfoType:F}: {CallerType.Name}.{Member} {Message} {string.Join(", ", Args)}"
                : $"{CorrelationId} {InfoType:F}: {CallerType.Name}.{Member} {Message} {string.Join(", ", Args)}";
        }
    }

    public class InfoExceptionEventArgs : InfoEventArgs
    {
        public Exception Exception { get; }

        public InfoExceptionEventArgs(InfoType infoType, Type callerType, string member, string message, Exception exception, Guid correlationId, object[] args)
            : base(infoType, callerType, member, message, correlationId, args)
        {
            Exception = exception;
        }

        public override string ToString()
        {
            return CorrelationId == Guid.Empty 
                ? $"{InfoType:F}: {CallerType.Name}.{Member} {Message} {string.Join(", ", Args)}\n\r --- Exception:\n\r{Exception}" 
                : $"{CorrelationId} {InfoType:F}: {CallerType.Name}.{Member} {Message} {string.Join(", ", Args)}\n\r --- Exception:\n\r{Exception}";
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

    public sealed class EventInfoStream : IEventInfoStream
    {
        public static IEventInfoStream Default { get; } = new EventInfoStream();

        private readonly ConcurrentDictionary<Guid, IObserver<InfoEventArgs>> observers
            = new ConcurrentDictionary<Guid, IObserver<InfoEventArgs>>();

        public IDisposable Subscribe(IObserver<InfoEventArgs> observer)
        {
            Guid key = Guid.NewGuid();
            observers.TryAdd(key, observer);
            return new InfoEventStreamSubscribtion(key, Unsubscribe);
        }

        private void Unsubscribe(Guid key) => observers.TryRemove(key, out var _);
        public ITypeBoundEventInfoStream Bind<TCaller>()
            => Bind(typeof(TCaller));
        public ITypeBoundEventInfoStream Bind(Type callerType)
            => new TypeBoundEventInfoStream(this, callerType);

        public IInfoStreamCorrelationScope Scope(Type callerType)
            => new InfoStreamCorrelationScope(this, callerType, Guid.NewGuid());
        public IInfoStreamCorrelationScope Scope(Type callerType, Guid correlationId)
            => new InfoStreamCorrelationScope(this, callerType, correlationId);

        public void Debug(Type callerType, string message, Guid correlationId, object[] args, [CallerMemberName]string member = null)
            => Publish(new InfoEventArgs(InfoType.Debug, callerType, member, message, correlationId, args));

        public void Info(Type callerType, string message, Guid correlationId, object[] args, [CallerMemberName]string member = null)
            => Publish(new InfoEventArgs(InfoType.Info, callerType, member, message, correlationId, args));

        public void Error(Type callerType, string message, Guid correlationId, object[] args, [CallerMemberName]string member = null)
            => Publish(new InfoEventArgs(InfoType.Error, callerType, member, message, correlationId, args));

        public void Exception(Type callerType, string message, Exception ex, Guid correlationId, object[] args, [CallerMemberName]string member = null)
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

    public interface ITypeBoundEventInfoStream : IEventInfoStream
    {
        IInfoStreamCorrelationScope Scope();
        IInfoStreamCorrelationScope Scope(Guid correlationId);

        void Debug(string message, Guid correlationId, object[] args, [CallerMemberName]string member = null);
        void Info(string message, Guid correlationId, object[] args, [CallerMemberName]string member = null);
        void Error(string message, Guid correlationId, object[] args, [CallerMemberName]string member = null);
        void Exception(string message, Guid correlationId, Exception ex, object[] args, [CallerMemberName]string member = null);


        void Debug(string message, object[] args, [CallerMemberName]string member = null);
        void Info(string message, object[] args, [CallerMemberName]string member = null);
        void Error(string message, object[] args, [CallerMemberName]string member = null);
        void Exception(string message, Exception ex, object[] args, [CallerMemberName]string member = null);
    }

    public class TypeBoundEventInfoStream : ITypeBoundEventInfoStream
    {
        private readonly IEventInfoStream inner;
        private readonly Type callerType;

        public TypeBoundEventInfoStream(IEventInfoStream inner, Type callerType)
        {
            this.inner = inner;
            this.callerType = callerType;
        }

        public IInfoStreamCorrelationScope Scope()
            => new InfoStreamCorrelationScope(inner, callerType, Guid.NewGuid());

        public IInfoStreamCorrelationScope Scope(Guid correlationId)
            => new InfoStreamCorrelationScope(inner, callerType, correlationId);

        public void Debug(string message, Guid correlationId, object[] args, [CallerMemberName]string member = null)
            => inner.Debug(callerType, message, correlationId, args, member);
        public void Info(string message, Guid correlationId, object[] args, [CallerMemberName]string member = null)
            => inner.Info(callerType, message, correlationId, args, member);
        public void Error(string message, Guid correlationId, object[] args, [CallerMemberName]string member = null)
            => inner.Error(callerType, message, correlationId, args, member);
        public void Exception(string message, Guid correlationId, Exception ex, object[] args, [CallerMemberName]string member = null)
            => inner.Exception(callerType, message, ex, correlationId, args, member);

        public void Debug(string message, object[] args, [CallerMemberName]string member = null)
            => inner.Debug(callerType, message, Guid.Empty, args, member);
        public void Info(string message, object[] args, [CallerMemberName]string member = null)
            => inner.Info(callerType, message, Guid.Empty, args, member);
        public void Error(string message, object[] args, [CallerMemberName]string member = null)
            => inner.Error(callerType, message, Guid.Empty, args, member);
        public void Exception(string message, Exception ex, object[] args, [CallerMemberName]string member = null)
            => inner.Exception(callerType, message, ex, Guid.Empty, args, member);

        public IDisposable Subscribe(IObserver<InfoEventArgs> observer) 
            => inner.Subscribe(observer);

        public ITypeBoundEventInfoStream Bind<TCaller>()
            => inner.Bind<TCaller>();
        public ITypeBoundEventInfoStream Bind(Type callerType)
            => inner.Bind(callerType);
        public IInfoStreamCorrelationScope Scope(Type callerType) 
            => inner.Scope(callerType);
        public IInfoStreamCorrelationScope Scope(Type callerType, Guid correlationId)
            => inner.Scope(callerType, correlationId);

        public void Exception(Type callerType, string message, Exception ex, Guid correlationId, object[] args, [CallerMemberName]string member = null)
        {
            inner.Exception(callerType, message, ex, correlationId, args, member);
        }

        public void Error(Type callerType, string message, Guid correlationId, object[] args, [CallerMemberName]string member = null)
        {
            inner.Error(callerType, message, correlationId, args, member);
        }

        public void Debug(Type callerType, string message, Guid correlationId, object[] args, [CallerMemberName]string member = null)
        {
            inner.Debug(callerType, message, correlationId, args, member);
        }

        public void Info(Type callerType, string message, Guid correlationId, object[] args, [CallerMemberName]string member = null)
        {
            inner.Info(callerType, message, correlationId, args, member);
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
        private readonly IEventInfoStream inner;
        private readonly Type callerType;
        private readonly Guid correlationId;

        public InfoStreamCorrelationScope(IEventInfoStream inner, Type callerType, Guid correlationId)
        {
            this.inner = inner;
            this.callerType = callerType;
            this.correlationId = correlationId;
        }

        public void Debug(string message, object[] args, [CallerMemberName]string member = null)
            => inner.Debug(callerType, message, correlationId, args, member);
        public void Info(string message, object[] args, [CallerMemberName]string member = null)
            => inner.Info(callerType, message, correlationId, args, member);
        public void Error(string message, object[] args, [CallerMemberName]string member = null)
            => inner.Error(callerType, message, correlationId, args, member);
        public void Exception(string message, Exception ex, object[] args, [CallerMemberName]string member = null)
            => inner.Exception(callerType, message, ex, correlationId, args, member);
    }
}
