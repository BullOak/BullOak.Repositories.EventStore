﻿using EventStore.Client;

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
    using TechTalk.SpecFlow;
    using Polly;
    using FluentAssertions;

    internal class EventStoreIntegrationContext
    {
        private readonly EventStoreRepository<string, IHoldHigherOrder> repository;
        public readonly EventStoreReadOnlyRepository<string, IHoldHigherOrder> readOnlyRepository;
        private static EventStoreClient client;

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

        private static EventStoreClient GetConnection()
        {
            return client;
        }

        [BeforeTestRun]
        public static async Task SetupNode()
        {
            client ??= await SetupConnection();
        }

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

        public async Task TruncateStream(string id)
        {
            var connection = GetConnection();
            var metadata = await connection.GetStreamMetadataAsync(id);
            if (metadata.MetastreamRevision.HasValue)
            {
                await connection.SetStreamMetadataAsync(
                    id,
                    new StreamRevision(metadata.MetastreamRevision.Value),
                    new StreamMetadata(truncateBefore: metadata.MetastreamRevision.Value));
            }
        }

        public async Task AssertStreamHasNoResolvedEvents(string id)
        {
            var retry = Policy
                .HandleResult(true)
                .WaitAndRetryAsync(3, count => TimeSpan.FromMilliseconds(500));

            var anyResolvedEvents = await retry.ExecuteAsync(async () =>
            {
                var connection = GetConnection();
                var result = connection.ReadStreamAsync(Direction.Forwards, id, StreamPosition.Start, 1);
                var eventFound = false;

                await foreach (var x in result)
                {
                    if (x.IsResolved)
                    {
                        eventFound = true;
                        break;
                    }
                }
                return eventFound;
            });


            anyResolvedEvents.Should().BeFalse();
        }

        public Task SoftDeleteStream(string id)
            => GetConnection().SoftDeleteAsync(id, StreamState.Any);

        public Task HardDeleteStream(string id)
            => GetConnection().TombstoneAsync(id, StreamState.Any);

        public Task SoftDeleteStreamFromRepository(string id)
            => repository.SoftDelete(id);
        public Task SoftDeleteFromRepositoryBySoftDeleteEvent(string id)
            => repository.SoftDeleteByEvent(id);

        public Task SoftDeleteFromRepositoryBySoftDeleteEvent<TSoftDeleteEvent>(string id, Func<TSoftDeleteEvent> createSoftDeleteEvent)
            where TSoftDeleteEvent : EntitySoftDeleted
            => repository.SoftDeleteByEvent(id, createSoftDeleteEvent);

        public async Task<ResolvedEvent[]> ReadEventsFromStreamRaw(string id)
        {
            var client = GetConnection();
            var result = new List<ResolvedEvent>();
            var readResults = client.ReadStreamAsync(Direction.Forwards, id, StreamPosition.Start);

            return await readResults.ToArrayAsync();
        }

        internal async Task WriteEventsToStreamRaw(string currentStreamInUse, IEnumerable<MyEvent> myEvents)
        {
            var conn = await SetupConnection();

            await conn.AppendToStreamAsync(currentStreamInUse, StreamState.Any,
                myEvents.Select(e =>
                {
                    var serialized = JsonConvert.SerializeObject(e);
                    byte[] bytes = System.Text.Encoding.UTF8.GetBytes(serialized);
                    return new EventData(Uuid.NewUuid(),
                        e.GetType().AssemblyQualifiedName,
                        bytes,
                        null);
                }));
        }

        private static async Task<EventStoreClient> SetupConnection()
        {
            var settings = EventStoreClientSettings
                .Create("esdb://localhost:2113?tls=false");
            var client = new EventStoreClient(settings);
            var projectionsClient = new EventStoreProjectionManagementClient(settings);
            await projectionsClient.EnableAsync("$by_category");

            await Task.Delay(TimeSpan.FromSeconds(3));

            return client;
        }
    }
}
