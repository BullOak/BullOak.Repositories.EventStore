using Grpc.Core;
using Polly;
using Polly.Contrib.WaitAndRetry;
using Polly.Retry;

namespace BullOak.Repositories.EventStore.Streams;

public static class GrpcWriteRetryPolicy
{
    public static AsyncRetryPolicy GetRetryPolicy(GrpcWriteRetrySettings retrySettings) =>
        Policy
            .Handle<global::EventStore.Client.NotLeaderException>()
            .Or<RpcException>(e => retrySettings.RetryableStatusCodes.Contains(e.StatusCode))
            .WaitAndRetryAsync(
                Backoff.DecorrelatedJitterBackoffV2(retrySettings.IntervalDelta, retrySettings.Limit, fastFirst: retrySettings.FastFirst),
                (ex, timespan, context) => retrySettings.OnRetry(ex, timespan, context));
}
