namespace BullOak.Repositories.EventStore.Test.Integration.Contexts
{
    using Config;
    using Components;
    using EventStoreIsolation;
    using Session;
    using global::EventStore.ClientAPI;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;
    using Events;
    using global::EventStore.ClientAPI.SystemData;
    using TechTalk.SpecFlow;

    internal class EventStoreIntegrationContext
    {
        private static readonly IntegrationTestsSettings testsSettings = new IntegrationTestsSettings();
        private readonly EventStoreRepository<string, IHoldHigherOrder> repository;
        public readonly EventStoreReadOnlyRepository<string, IHoldHigherOrder> readOnlyRepository;
        private static IEventStoreConnection connection;
        private static IDisposable eventStoreIsolation;

        public TestDateTimeProvider DateTimeProvider { get; }

        public EventStoreIntegrationContext(PassThroughValidator validator)
        {
            var configuration = Configuration.Begin()
               .WithDefaultCollection()
               .WithDefaultStateFactory()
               .NeverUseThreadSafe()
               .WithNoEventPublisher()
               .WithAnyAppliersFrom(Assembly.GetExecutingAssembly())
               .AndNoMoreAppliers()
               .WithNoUpconverters()
               .Build();

            DateTimeProvider = new TestDateTimeProvider();

            repository = new EventStoreRepository<string, IHoldHigherOrder>(validator, configuration, GetConnection(), DateTimeProvider);
            readOnlyRepository = new EventStoreReadOnlyRepository<string, IHoldHigherOrder>(configuration, GetConnection());
        }

        private static IEventStoreConnection GetConnection()
        {
            return connection;
        }

        [BeforeTestRun]
        public static async Task SetupNode()
        {
            eventStoreIsolation = IsolationFactory.StartIsolation(
                testsSettings.EventStoreIsolationMode,
                testsSettings.EventStoreIsolationCommand,
                testsSettings.EventStoreIsolationArguments);

            if (connection == null)
                connection = await SetupConnection();
        }

        [AfterTestRun]
        public static void TeardownNode()
        {
            eventStoreIsolation?.Dispose();
        }

        public async Task<IManageSessionOf<IHoldHigherOrder>> StartSession(string streamName, DateTime? appliesAt = null)
        {
            var session = await repository.BeginSessionFor(streamName, appliesAt: appliesAt).ConfigureAwait(false);
            return session;
        }

        public async Task<IEnumerable<ReadModel<IHoldHigherOrder>>> ReadAllEntitiesFromCategory(string categoryName, DateTime? appliesAt = null)
        {
            return await readOnlyRepository.ReadAllEntitiesFromCategory(categoryName, appliesAt: appliesAt).ConfigureAwait(false);
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
            => GetConnection().DeleteStreamAsync(id, -1, true);

        public Task SoftDeleteByEvent(string id)
            => repository.SoftDeleteByEvent(id);

        public Task SoftDeleteByEvent<TSoftDeleteEvent>(string id, Func<TSoftDeleteEvent> createSoftDeleteEvent)
            where TSoftDeleteEvent : EntitySoftDeleted
            => repository.SoftDeleteByEvent(id, createSoftDeleteEvent);

        public async Task<ResolvedEvent[]> ReadEventsFromStreamRaw(string id)
        {
            var conn = GetConnection();
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
            var conn = await SetupConnection(true);

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

        private static async Task<IEventStoreConnection> SetupConnection(bool withDefaultUser = false)
        {
            var settings = ConnectionSettings
                .Create()
                .KeepReconnecting()
                .FailOnNoServerResponse()
                .KeepRetrying()
                .UseConsoleLogger();

            if (withDefaultUser)
                settings.SetDefaultUserCredentials(new UserCredentials("admin", "changeit"));

            const string localhostConnectionString = "ConnectTo=tcp://localhost:1113; HeartBeatTimeout=500";

            connection = EventStoreConnection.Create(localhostConnectionString, settings);
            await connection.ConnectAsync();

            return connection;
        }
    }
}
