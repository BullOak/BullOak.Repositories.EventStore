namespace BullOak.Repositories.EventStore.Test.Integration.Contexts
{
    using Config;
    using Components;
    using Session;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;
    using Events;
    using ClientV5 = global::EventStore.ClientAPI;
    using ClientV20 = global::EventStore.Client;
    using Streams;

    internal class EventStoreIntegrationContext : IDisposable
    {
        private readonly PassThroughValidator validator;
        private static IHoldAllConfiguration configuration;

        private IStoreEventsToStream writer;
        private IReadEventsFromStream reader;

        private EventStoreRepository<string, IHoldHigherOrder> repository;
        private EventStoreRepository<string, IHoldHigherOrder> Repository
        {
            get
            {
                if (repository == null)
                    BuildRepositories().Wait();

                return repository;
            }
        }
        public EventStoreReadOnlyRepository<string, IHoldHigherOrder> ReadOnlyRepository
        {
            get
            {
                if (readOnlyRepository == null)
                    BuildRepositories().Wait();

                return readOnlyRepository;
            }
        }
        public async Task BuildRepositories(string protocol = "tcp")
        {
            this.protocol = protocol;

            reader = await ConfigureReader(protocol);
            writer = await ConfigureWriter(protocol);

            repository = new EventStoreRepository<string, IHoldHigherOrder>(validator, configuration, reader, writer, DateTimeProvider);
            readOnlyRepository = new EventStoreReadOnlyRepository<string, IHoldHigherOrder>(configuration, reader);
        }

        private string protocol = "tcp";
        private ClientV5.IEventStoreConnection connV5;
        private ClientV20.EventStoreClient connV20;

        private EventStoreReadOnlyRepository<string, IHoldHigherOrder> readOnlyRepository;

        public TestDateTimeProvider DateTimeProvider { get; }

        public EventStoreIntegrationContext(PassThroughValidator validator)
        {
            this.validator = validator;
            DateTimeProvider = new TestDateTimeProvider();
        }

        public static void SetupNode()
        {
            configuration = Configuration.Begin()
                .WithDefaultCollection()
                .WithDefaultStateFactory()
                .NeverUseThreadSafe()
                .WithNoEventPublisher()
                .WithAnyAppliersFrom(Assembly.GetExecutingAssembly())
                .AndNoMoreAppliers()
                .WithNoUpconverters()
                .Build();
        }

        async Task<IReadEventsFromStream> ConfigureReader(string protocol)
            => protocol switch
            {
                "tcp" => new TcpEventReader(await ConfigureEventStoreTcp(), configuration),
                "grpc" => new GrpcEventReader(await ConfigureEventStoreGrpc(), configuration),
                _ => throw new ArgumentOutOfRangeException(nameof(protocol))
            };

        async Task<IStoreEventsToStream> ConfigureWriter(string protocol)
            => protocol switch
            {
                "tcp" => new TcpEventWriter(await ConfigureEventStoreTcp()),
                "grpc" => new GrpcEventWriter(await ConfigureEventStoreGrpc()),
                _ => throw new ArgumentOutOfRangeException(nameof(protocol))
            };

        public static void TeardownNode()
        {
        }

        public async Task<IManageSessionOf<IHoldHigherOrder>> StartSession(string streamName, DateTime? appliesAt = null)
        {
            var session = await Repository.BeginSessionFor(streamName, appliesAt: appliesAt).ConfigureAwait(false);
            return session;
        }

        public async Task<IEnumerable<ReadModel<IHoldHigherOrder>>> ReadAllEntitiesFromCategory(string categoryName, DateTime? appliesAt = null)
        {
            return await ReadOnlyRepository.ReadAllEntitiesFromCategory(categoryName, e =>
            {
                if (!appliesAt.HasValue || e.Metadata?.Properties == null || !e.Metadata.Properties.TryGetValue(MetadataProperties.Timestamp,
                    out var eventTimestamp)) return true;

                if (!DateTime.TryParse(eventTimestamp, out var timestamp)) return true;

                return timestamp <= appliesAt;
            }).ConfigureAwait(false);
        }

        public async Task AppendEventsToCurrentStream(string id, IMyEvent[] events)
        {
            using (var session = await StartSession(id))
            {
                session.AddEvents(events);
                await session.SaveChanges();
            }
        }

        public Task SoftDeleteStream(string id)
            => Repository.SoftDelete(id);


        public Task HardDeleteStream(string id)
            => protocol switch
            {
                "tcp" => connV5.DeleteStreamAsync(id, -1, true),
                "grpc" => connV20.TombstoneAsync(id, ClientV20.StreamState.Any),
                _ => throw new ArgumentOutOfRangeException(nameof(protocol))
            };

        public Task<StoredEvent[]> ReadEventsFromStreamRaw(string id)
            => protocol switch
            {
                "tcp" => TcpReadEventsFromStreamRaw(id),
                "grpc" => GrpcReadEventsFromStreamRaw(id),
                _ => throw new ArgumentOutOfRangeException(nameof(protocol))
            };

        private async Task<StoredEvent[]> GrpcReadEventsFromStreamRaw(string id)
        {
            var readResults = connV20.ReadStreamAsync(ClientV20.Direction.Forwards, id, ClientV20.StreamPosition.Start);
            return await readResults
                        .Where(e => e.Event != null)
                        .Select(e => e.Event.ToStoredEvent(configuration.StateFactory))
                        .ToArrayAsync();
        }

        private async Task<StoredEvent[]> TcpReadEventsFromStreamRaw(string id)
        {
            var result = new List<ClientV5.ResolvedEvent>();
            ClientV5.StreamEventsSlice currentSlice;
            long nextSliceStart = ClientV5.StreamPosition.Start;
            do
            {
                currentSlice = await connV5.ReadStreamEventsForwardAsync(id, nextSliceStart, 100, false);
                nextSliceStart = currentSlice.NextEventNumber;
                result.AddRange(currentSlice.Events);
            } while (!currentSlice.IsEndOfStream);

            return result
                  .Where(e => e.Event != null)
                  .Select((e, _) => e.Event.ToStoredEvent(configuration.StateFactory))
                  .TakeWhile(e => e.DeserializedEvent is not EntitySoftDeleted)
                  .ToArray();
        }

        internal Task WriteEventsToStreamRaw(string currentStreamInUse, IEnumerable<MyEvent> myEvents)
            => protocol switch
            {
                "tcp" => TcpWriteEventsToStreamRaw(currentStreamInUse, myEvents),
                "grpc" => GrpcWriteEventsToStreamRaw(currentStreamInUse, myEvents),
                _ => throw new ArgumentOutOfRangeException(nameof(protocol))
            };

        private async Task GrpcWriteEventsToStreamRaw(string currentStreamInUse, IEnumerable<MyEvent> myEvents)
        {
            await connV20.AppendToStreamAsync(currentStreamInUse, ClientV20.StreamState.Any,
                                              myEvents.Select(e =>
                                              {
                                                  var serialized = JsonConvert.SerializeObject(e);
                                                  var bytes = System.Text.Encoding.UTF8.GetBytes(serialized);
                                                  return new ClientV20.EventData(ClientV20.Uuid.NewUuid(), e.GetType().AssemblyQualifiedName, bytes, null);
                                              }));
        }

        private async Task TcpWriteEventsToStreamRaw(string currentStreamInUse, IEnumerable<MyEvent> myEvents)
        {
            await connV5.AppendToStreamAsync(currentStreamInUse, ClientV5.ExpectedVersion.Any,
                                             myEvents.Select(e =>
                                             {
                                                 var serialized = JsonConvert.SerializeObject(e);
                                                 var bytes = System.Text.Encoding.UTF8.GetBytes(serialized);
                                                 return new ClientV5.EventData(Guid.NewGuid(), e.GetType().AssemblyQualifiedName, true, bytes, null);
                                             }));
        }

        private async Task<ClientV20.EventStoreClient> ConfigureEventStoreGrpc()
        {
            if (connV20 != null)
                return connV20;

            var settings = ClientV20.EventStoreClientSettings
                .Create("esdb://localhost:2114?tls=false");
            var client = new ClientV20.EventStoreClient(settings);
            var projectionsClient = new ClientV20.EventStoreProjectionManagementClient(settings);
            await projectionsClient.EnableAsync("$by_category");

            await Task.Delay(TimeSpan.FromSeconds(3));
            connV20 = client;
            return client;
        }

        private async Task<ClientV5.IEventStoreConnection> ConfigureEventStoreTcp()
        {
            if (connV5 != null)
                return connV5;

            var connectionString =
                "ConnectTo=tcp://admin:changeit@localhost:1113;HeartBeatTimeout=500;UseSslConnection=false;";
            var builder = ClientV5.ConnectionSettings.Create()
                .KeepReconnecting();

            var connection = ClientV5.EventStoreConnection.Create(connectionString, builder);

            await connection.ConnectAsync();
            connV5 = connection;
            return connection;
        }
        public void Dispose()
        {
            connV5?.Dispose();
        }
    }
}
