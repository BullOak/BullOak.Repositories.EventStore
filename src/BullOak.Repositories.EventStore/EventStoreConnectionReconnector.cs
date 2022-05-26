using EventStore.ClientAPI;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace BullOak.Repositories.EventStore
{
    internal class EventStoreConnectionReconnector : IKeepESConnectionAlive
    {
        private readonly Func<Task<IEventStoreConnection>> esConnectionFactory;
        private IEventStoreConnection esConnection;
        private bool disposedValue = false;

        public IEventStoreConnection Connection => esConnection;

        public EventStoreConnectionReconnector(Func<Task<IEventStoreConnection>> esConnectionFactory)
        {
            this.esConnectionFactory = esConnectionFactory ?? throw new ArgumentNullException(nameof(esConnectionFactory));
            EstablishOrRefreshEventStoreConnection();
        }

        private void EstablishOrRefreshEventStoreConnection()
        {
            if (esConnection != null)
                esConnection.Closed -= EventStoreConnection_Closed;


            esConnection = Task.Run(() => esConnectionFactory()).GetAwaiter().GetResult();

            try
            {
                Task.Run(() => esConnection.ConnectAsync()).Wait();
            }
            catch(AggregateException aggregateException)
            {
                if(!(aggregateException.InnerException is InvalidOperationException))
                    throw;

                //We have an InvalidOperationException.
                //
                //This means we're already connected. Nothing to worry about.
                // If this was already disposed (which would indicate something funny)
                // it would have thrown an ObjectDisposedException instead
            }

            esConnection.Closed += EventStoreConnection_Closed;
        }

        private void EventStoreConnection_Closed(object _, ClientClosedEventArgs __)
            => EstablishOrRefreshEventStoreConnection();

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    esConnection?.Dispose();
                    esConnection.Closed -= EventStoreConnection_Closed;
                }
                disposedValue = true;
            }
        }
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
