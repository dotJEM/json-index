using System;

namespace DotJEM.Json.Index
{

    public class IndexWriterInfoEventArgs : EventArgs
    {
        public string Message { get; }

        public IndexWriterInfoEventArgs(string message)
        {
            Message = message;
        }
    }

    public class IndexWriterExceptionEventArgs : IndexWriterInfoEventArgs
    {
        public Exception Exception { get; }

        public IndexWriterExceptionEventArgs(Exception exception)
            : this(exception.Message, exception)
        {
            Exception = exception;
        }

        public IndexWriterExceptionEventArgs(string message, Exception exception) 
            : base(message)
        {
            Exception = exception;
        }
    }
}