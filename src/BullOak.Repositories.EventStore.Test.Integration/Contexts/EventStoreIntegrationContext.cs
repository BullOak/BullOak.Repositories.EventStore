using EventStore.Client;

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
    using global::EventStore.ClientAPI;
    using Streams;
    using TechTalk.SpecFlow;

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
            reader = await ConfigureReader(protocol);
            writer = await ConfigureWriter(protocol);

            repository = new EventStoreRepository<string, IHoldHigherOrder>(validator, configuration, reader, writer, DateTimeProvider);
            readOnlyRepository = new EventStoreReadOnlyRepository<string, IHoldHigherOrder>(configuration, reader);
        }

        private IEventStoreConnection conn;
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
            => conn.DeleteStreamAsync(id, -1, true);

        public async Task<ResolvedEvent[]> ReadEventsFromStreamRaw(string id)
        {
            var result = new List<ResolvedEvent>();
            StreamEventsSlice currentSlice;
            long nextSliceStart = StreamPosition.Start;
            do
            {
                currentSlice = await conn.ReadStreamEventsForwardAsync(id, nextSliceStart, 100, false);
                nextSliceStart = currentSlice.NextEventNumber;
                result.AddRange(currentSlice.Events);
            } while (!currentSlice.IsEndOfStream);

            return result.ToArray();
        }

        internal async Task WriteEventsToStreamRaw(string currentStreamInUse, IEnumerable<MyEvent> myEvents)
        {
            await conn.AppendToStreamAsync(currentStreamInUse, ExpectedVersion.Any,
                myEvents.Select(e =>
                {
                    var serialized = JsonConvert.SerializeObject(e);
                    byte[] bytes = System.Text.Encoding.UTF8.GetBytes(serialized);
                    return new EventData(Guid.NewGuid(),
                        e.GetType().AssemblyQualifiedName,
                        true,
                        bytes,
                        null);
                }));
        }

        private async Task<EventStoreClient> ConfigureEventStoreGrpc()
        {
            var settings = EventStoreClientSettings
                .Create("esdb://localhost:2114?tls=false");
            var client = new EventStoreClient(settings);
            var projectionsClient = new EventStoreProjectionManagementClient(settings);
            await projectionsClient.EnableAsync("$by_category");

            await Task.Delay(TimeSpan.FromSeconds(3));

            return client;
        }

        private async Task<IEventStoreConnection> ConfigureEventStoreTcp()
        {
            if (conn != null)
                return conn;

            var connectionString =
                "ConnectTo=tcp://admin:changeit@localhost:1113;HeartBeatTimeout=500;UseSslConnection=false;";
            var builder = ConnectionSettings.Create()
                .KeepReconnecting()
                .KeepRetrying();

            var connection = EventStoreConnection.Create(connectionString, builder);

            await connection.ConnectAsync();
            conn = connection;
            return connection;
        }
        public void Dispose()
        {
            conn?.Dispose();
        }
    }
}
