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
    using Events;
    using global::EventStore.ClientAPI;
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
        private static IEventStoreConnection conn;

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
            using (var session = await StartSession(id))
            {
                session.AddEvents(events);
                await session.SaveChanges();
            }
        }

        public Task SoftDeleteStream(string id)
            => repository.SoftDelete(id);

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

        private static async Task<EventStoreClient> ConfigureEventStoreGrpc()
        {
            var settings = EventStoreClientSettings
                .Create("esdb://localhost:2114?tls=false");
            var client = new EventStoreClient(settings);
            var projectionsClient = new EventStoreProjectionManagementClient(settings);
            await projectionsClient.EnableAsync("$by_category");

            await Task.Delay(TimeSpan.FromSeconds(3));

            return client;
        }

        private static async Task<IEventStoreConnection> ConfigureEventStoreTcp()
        {
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
    }
}
