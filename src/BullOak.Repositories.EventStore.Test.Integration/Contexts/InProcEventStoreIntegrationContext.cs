﻿namespace BullOak.Repositories.EventStore.Test.Integration.Contexts
{
    using BullOak.Repositories.Config;
    using BullOak.Repositories.EventStore.Test.Integration.Components;
    using BullOak.Repositories.Session;
    using global::EventStore.ClientAPI;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;
    using BullOak.Repositories.EventStore.Test.Integration.EventStoreServer;
    using Events;
    using TechTalk.SpecFlow;

    internal class InProcEventStoreIntegrationContext
    {
        //private static ClusterVNode node;
        private readonly EventStoreRepository<string, IHoldHigherOrder> repository;
        public readonly EventStoreReadOnlyRepository<string, IHoldHigherOrder> readOnlyRepository;
        private static IEventStoreConnection connection;
        private static Process eventStoreProcess;

        public InProcEventStoreIntegrationContext(PassThroughValidator validator)
        {
            var configuration = Configuration.Begin()
               .WithDefaultCollection()
               .WithDefaultStateFactory()
               .NeverUseThreadSafe()
               .WithNoEventPublisher()
               .WithAnyAppliersFrom(Assembly.GetExecutingAssembly())
               //.WithEventApplier(new StateApplier())
               .AndNoMoreAppliers()
               .WithNoUpconverters()
               .Build();

            repository = new EventStoreRepository<string, IHoldHigherOrder>(validator, configuration, GetConnection());
            readOnlyRepository = new EventStoreReadOnlyRepository<string, IHoldHigherOrder>(configuration, GetConnection());
        }

        private static IEventStoreConnection GetConnection()
        {
            return connection;
        }

        [BeforeTestRun]
        public static async Task SetupNode()
        {
            if (connection == null)
            {
                RunEventStoreServerProcess();

                var settings = ConnectionSettings
                    .Create()
                    .KeepReconnecting()
                    .FailOnNoServerResponse()
                    .KeepRetrying()
                    .UseConsoleLogger();

                var localhostConnectionString = "ConnectTo=tcp://localhost:1113; HeartBeatTimeout=500";

                connection = EventStoreConnection.Create(localhostConnectionString, settings);
                await connection.ConnectAsync();
            }
        }

        private static void RunEventStoreServerProcess()
        {
            eventStoreProcess = EventStoreServerStarterHelper.StartServer();
        }

        [AfterTestRun]
        public static void TeardownNode()
        {
            EventStoreServerStarterHelper.StopProcess(eventStoreProcess);
        }

        public async Task<IManageSessionOf<IHoldHigherOrder>> StartSession(Guid currentStreamId)
        {
            var session = await repository.BeginSessionFor(currentStreamId.ToString()).ConfigureAwait(false);
            return session;

        }

        public async Task AppendEventsToCurrentStream(Guid id, MyEvent[] events)
        {
            using (var session = await StartSession(id))
            {
                session.AddEvents(events);
                await session.SaveChanges();
            }
        }

        public Task SoftDeleteStream(Guid id)
            => repository.SoftDelete(id.ToString());

        public Task HardDeleteStream(Guid id)
            => GetConnection().DeleteStreamAsync(id.ToString(), -1, true);

        public Task SoftDeleteByEvent(Guid id)
            => repository.SoftDeleteByEvent(id.ToString());

        public Task SoftDeleteByEvent<TSoftDeleteEvent>(Guid id, Func<TSoftDeleteEvent> createSoftDeleteEvent)
            where TSoftDeleteEvent : EntitySoftDeleted
            => repository.SoftDeleteByEvent(id.ToString(), createSoftDeleteEvent);

        public async Task<ResolvedEvent[]> ReadEventsFromStreamRaw(Guid id)
        {
            var conn = GetConnection();
            var result = new List<ResolvedEvent>();
            StreamEventsSlice currentSlice;
            long nextSliceStart = StreamPosition.Start;
            do
            {
                currentSlice = await conn.ReadStreamEventsForwardAsync(id.ToString(), nextSliceStart, 100, false);
                nextSliceStart = currentSlice.NextEventNumber;
                result.AddRange(currentSlice.Events);
            } while (!currentSlice.IsEndOfStream);

            return result.ToArray();
        }

        internal Task WriteEventsToStreamRaw(Guid currentStreamInUse, IEnumerable<MyEvent> myEvents)
        {
            var conn = GetConnection();
            return conn.AppendToStreamAsync(currentStreamInUse.ToString(), ExpectedVersion.Any,
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
    }
}
