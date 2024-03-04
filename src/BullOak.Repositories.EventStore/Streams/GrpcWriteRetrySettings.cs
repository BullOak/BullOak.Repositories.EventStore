using System;
using System.Collections.Generic;
using Grpc.Core;

namespace BullOak.Repositories.EventStore.Streams;

public class GrpcWriteRetrySettings
{
    public int Limit { get; init; }
    public TimeSpan IntervalDelta { get; init; }
    public bool FastFirst { get; init; }

    public HashSet<StatusCode> RetryableStatusCodes { get; init; }

    public Action<Exception, TimeSpan, Polly.Context> OnRetry { get; init; }

    public static GrpcWriteRetrySettings DefaultRetrySettings = new ()
    {
        Limit = 5,
        FastFirst = true,
        IntervalDelta = TimeSpan.FromMilliseconds(200),
        RetryableStatusCodes = {  StatusCode.Unavailable, StatusCode.DeadlineExceeded, StatusCode.NotFound },
        OnRetry = NoOp
    };

    private static Action<Exception, TimeSpan, Polly.Context> NoOp => (_, _, _) => { };
}
