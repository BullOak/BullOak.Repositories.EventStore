using System;
using System.Collections.Generic;
using Grpc.Core;

namespace BullOak.Repositories.EventStore.Streams;

public class GrpcWriteRetrySettings
{
    public TimeSpan IntervalDelta { get; }
    public int Limit { get; }
    public bool FastFirst { get; }

    public HashSet<StatusCode> RetryableStatusCodes { get; }

    public Action<Exception, TimeSpan, Polly.Context> OnRetry { get; init; }

    private GrpcWriteRetrySettings(
        TimeSpan intervalDelta,
        int limit,
        bool fastFirst = true,
        HashSet<StatusCode> retryableStatusCodes = null,
        Action<Exception, TimeSpan, Polly.Context> onRetry = null)
    {
        IntervalDelta = intervalDelta;
        Limit = limit;
        FastFirst = fastFirst;
        RetryableStatusCodes = retryableStatusCodes ?? new HashSet<StatusCode>();
        OnRetry = onRetry ?? NoOp;
    }

    // With default settings we will only retry on NotLeaderException
    public static GrpcWriteRetrySettings DefaultRetrySettings = new(TimeSpan.FromMilliseconds(200), 5);

    private static Action<Exception, TimeSpan, Polly.Context> NoOp => (_, _, _) => { };
}
