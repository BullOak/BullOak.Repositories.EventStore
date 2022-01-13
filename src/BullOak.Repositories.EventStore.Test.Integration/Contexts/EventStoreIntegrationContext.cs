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
    using ClientV5 =  global::EventStore.ClientAPI;
    using ClientV20 =  global::EventStore.Client;
    using Streams;
    using TechTalk.SpecFlow;

    internal class EventStoreIntegrationContext
    {
        private readonly EventStoreRepository<string, IHoldHigherOrder> repository;
        public readonly EventStoreReadOnlyRepository<string, IHoldHigherOrder> readOnlyRepository;
        private static IReadEventsFromStream reader;
        private static IStoreEventsToStream writer;
        private static IHoldAllConfiguration configuration;

        private static string protocol = "tcp";
        private static ClientV5.IEventStoreConnection connV5;
        private static ClientV20.EventStoreClient connV20;

        public TestDateTimeProvider DateTimeProvider { get; }

        public EventStoreIntegrationContext(PassThroughValidator validator)
        {
            DateTimeProvider = new TestDateTimeProvider();

            repository = new EventStoreRepository<string, IHoldHigherOrder>(validator, configuration, reader, writer, DateTimeProvider);
            readOnlyRepository = new EventStoreReadOnlyRepository<string, IHoldHigherOrder>(configuration, reader);
        }

        [BeforeTestRun]
        public static async Task SetupNode()
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
            reader = await ConfigureReader();
            writer = await ConfigureWriter();
        }

        static async Task<IReadEventsFromStream> ConfigureReader()
            => protocol switch
            {
                "tcp" => new TcpEventReader(await ConfigureEventStoreTcp(), configuration),
                "grpc" => new GrpcEventReader(await ConfigureEventStoreGrpc(), configuration),
                _ => throw new ArgumentOutOfRangeException(nameof(protocol))
            };

        static async Task<IStoreEventsToStream> ConfigureWriter()
            => protocol switch
            {
                "tcp" => new TcpEventWriter(await ConfigureEventStoreTcp()),
                "grpc" => new GrpcEventWriter(await ConfigureEventStoreGrpc()),
                _ => throw new ArgumentOutOfRangeException(nameof(protocol))
            };

        [AfterTestRun]
        public static void TeardownNode()
        {
        }

        public async Task<IManageSessionOf<IHoldHigherOrder>> StartSession(string streamName, DateTime? appliesAt = null)
        {
            var session = await repository.BeginSessionFor(streamName, appliesAt: appliesAt).ConfigureAwait(false);
            return session;
        }

        public async Task<IEnumerable<ReadModel<IHoldHigherOrder>>> ReadAllEntitiesFromCategory(string categoryName, DateTime? appliesAt = null)
        {
            return await readOnlyRepository.ReadAllEntitiesFromCategory(categoryName, e =>
            {
                if (!appliesAt.HasValue || e.Metadata?.Properties == null || !e.Metadata.Properties.TryGetValue(MetadataProperties.Timestamp,
                    out var eventTimestamp)) return true;

                if (!DateTime.TryParse(eventTimestamp, out var timestamp)) return true;

                return timestamp <= appliesAt;
            }).ConfigureAwait(false);
        }

        public async Task AppendEventsToCurrentStream(string id, IMyEvent[] events)
        {
            using var session = await StartSession(id);
            session.AddEvents(events);
            await session.SaveChanges();
        }

        public Task SoftDeleteStream(string id)
            => repository.SoftDelete(id);

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

        private static async Task<ClientV20.EventStoreClient> ConfigureEventStoreGrpc()
        {
            var settings = ClientV20.EventStoreClientSettings
                .Create("esdb://localhost:2114?tls=false");
            var client = new ClientV20.EventStoreClient(settings);
            var projectionsClient = new ClientV20.EventStoreProjectionManagementClient(settings);
            await projectionsClient.EnableAsync("$by_category");

            await Task.Delay(TimeSpan.FromSeconds(3));
            connV20 = client;
            return client;
        }

        private static async Task<ClientV5.IEventStoreConnection> ConfigureEventStoreTcp()
        {
            var connectionString =
                "ConnectTo=tcp://admin:changeit@localhost:1113;HeartBeatTimeout=500;UseSslConnection=false;";
            var builder = ClientV5.ConnectionSettings.Create()
                .KeepReconnecting()
                .KeepRetrying();

            var connection = ClientV5.EventStoreConnection.Create(connectionString, builder);

            await connection.ConnectAsync();
            connV5 = connection;
            return connection;
        }
    }
}
