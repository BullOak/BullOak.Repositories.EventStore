namespace BullOak.Repositories.EventStore
{
    using System;
    using Exceptions;
    using global::EventStore.ClientAPI;

    public static class StreamAppendHelpers
    {
        public static void CheckConditionalWriteResultStatus(ConditionalWriteResult writeResult, string id)
        {
            switch (writeResult.Status)
            {
                case ConditionalWriteStatus.Succeeded:
                    break;
                case ConditionalWriteStatus.VersionMismatch:
                    throw new ConcurrencyException(id, null);
                case ConditionalWriteStatus.StreamDeleted:
                    throw new InvalidOperationException($"Stream was deleted. StreamId: {id}");
                default:
                    throw new InvalidOperationException($"Unexpected write result: {writeResult.Status}");
            }
        }
    }
}
