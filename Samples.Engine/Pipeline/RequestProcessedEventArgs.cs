using System;
using System.Net;
using Lextm.SharpSnmpLib.Messaging;

namespace Samples.Pipeline
{
    /// <summary>
    /// Describes a completed SNMP request/response exchange processed by <see cref="SnmpEngine"/>.
    /// </summary>
    public sealed class RequestProcessedEventArgs : EventArgs
    {
        public RequestProcessedEventArgs(
            ISnmpMessage request,
            ISnmpMessage? response,
            IPEndPoint sender,
            IListenerBinding binding,
            TimeSpan duration,
            Exception? exception,
            string? processingNote)
        {
            Request = request ?? throw new ArgumentNullException(nameof(request));
            Sender = sender ?? throw new ArgumentNullException(nameof(sender));
            Binding = binding ?? throw new ArgumentNullException(nameof(binding));
            Duration = duration;
            Response = response;
            Exception = exception;
            ProcessingNote = processingNote;
        }

        public ISnmpMessage Request { get; }

        public ISnmpMessage? Response { get; }

        public IPEndPoint Sender { get; }

        public IListenerBinding Binding { get; }

        public TimeSpan Duration { get; }

        public Exception? Exception { get; }

        public string? ProcessingNote { get; }
    }
}
