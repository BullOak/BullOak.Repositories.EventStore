using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using EventStore.Client;
using EventStore.Client.Projections;

namespace BullOak.Repositories.EventStore.ConsoleTestEventStoreClient
{
    class Program
    {
        static async Task Main(string[] args)
        {
            //esdb://localhost:2113?tls=false

            var settings = EventStoreClientSettings
                .Create("esdb://localhost:2114?tls=false");
            var client = new EventStoreClient(settings);

            //await TestReadFromtombstoned(client);

            var projectionClient = new EventStoreProjectionManagementClient(settings);
            var operationsClient = new EventStoreOperationsClient(settings);


            await client.DeleteAsync("dokimi-1", StreamState.Any);
            //await client.DeleteAsync("dokimi-2", StreamState.Any);
            //await client.DeleteAsync("dokimi-3", StreamState.Any);


            //await projectionClient.DisableAsync("$by_category");
            //var result = await operationsClient.StartScavengeAsync();
            //await Task.Delay(TimeSpan.FromSeconds(20));
            //await projectionClient.EnableAsync("$by_category");
            //await projectionClient.ResetAsync("$by_category");
            //var readDeltedStreamResult = client.ReadStreamAsync(Direction.Forwards, "test", StreamPosition.Start);

            //Console.WriteLine("Stream existed: {0}\nSteam was empty: {1}",
            //    await readDeltedStreamResult.ReadState,
            //    await readDeltedStreamResult.CountAsync());

            var data = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new Data() {Value = 5}));
            var writeResult = await client.AppendToStreamAsync("dokimi-1", StreamState.Any,
                new[] { new EventData(Uuid.NewUuid(), "TestType", data) });

            var testResult = await client.DeleteAsync("dokimi-1", StreamState.Any);
            var randomStreamDeleteResult = await client.DeleteAsync(Guid.NewGuid().ToString(), StreamState.Any);
            writeResult = await client.AppendToStreamAsync("dokimi-2", StreamRevision.FromInt64(-1),
                new[] { new EventData(Uuid.NewUuid(), "TestType", data) });
            writeResult = await client.AppendToStreamAsync("dokimi-3", StreamRevision.FromInt64(-1),
                new[] { new EventData(Uuid.NewUuid(), "TestType", data) });

            await Task.Delay(TimeSpan.FromSeconds(10));

            var readResult = client.ReadStreamAsync(Direction.Forwards, "$ce-dokimi",
                StreamPosition.Start,
                resolveLinkTos: true);
            var isSuccess = await readResult.ReadState;
            var events = await readResult.Where(x=> x.Event != null).ToListAsync();
            var eventCount = events.Count;

            var readResultStream = client.ReadStreamAsync(Direction.Forwards, "dokimi-2", StreamPosition.Start);
            var isSuccessStream = await readResultStream.ReadState;
            var eventsInStream = await readResultStream.ToListAsync();
            var eventCountStream = eventsInStream.Count;

            Console.WriteLine("Could read stream: {0}", isSuccess);
            Console.WriteLine("Found {0} events", eventCount);

            Console.WriteLine("Press any key to exit!");
            Console.ReadKey();
        }

        public class Data
        {
            public int Value { get; set; }
        }

        private static async Task TestReadFromtombstoned(EventStoreClient client)
        {
            var idForTombstoneTest = Guid.NewGuid().ToString();
            await client.AppendToStreamAsync(idForTombstoneTest, StreamRevision.FromInt64(-1),
                new[] {new EventData(Uuid.NewUuid(), "TestType", new byte[] {0x00})});
            await client.TombstoneAsync(idForTombstoneTest, StreamState.Any);

            var testResultNonExistent =
                client.ReadStreamAsync(Direction.Backwards, Guid.NewGuid().ToString(), StreamPosition.End);
            var testResult = client.ReadStreamAsync(Direction.Backwards, idForTombstoneTest, StreamPosition.End);
            var testState = await testResultNonExistent.ReadState;
            testState = await testResult.ReadState;
        }
    }
}

// New streams are written with index -1
// New streams raise exceptions when attempting to iterate events
// Softdeleted streams read as StreamNotFound
// Softdeleted streams write with expected stream revision -1
// Categories throw if tried to read when they are empty
// Categories only get created and populated with data writter AFTER they're created
// Tombstoned 
