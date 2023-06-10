# BullOak EventStore Benchmark

## Benchmarks

Implemented using [BenchmarkDotNet](https://github.com/dotnet/BenchmarkDotNet).

At the moment one [benchmark test](./ReadEventStreamBenchmark.cs) is
implemented - reading events from event stream.
(Technically there is [another one](./WriteEventStreamBenchmark.cs), but
it exists only to prepare the data in the EventStore for the reading test).

The main objective of this test is to see how BullOak behaves when dealing
with large number of events in an event stream when loading events and
rehydrating the state.

Benchmark has two parameters (passed into test by BenchmarkDotNet, set of values
specified via attributes in the [test code](./BenchmarkParameters.cs)):

* `EventsCount` to control the number of events in an event stream
* `EventSize` to control event size

## Running benchmark

> Benchmark test makes frequent requests to the EventStoreDB, and this may
> exhaust OS TCP connection limits. Symptom is that you will see GRPC exception
>
> ```txt
> Grpc.Core.RpcException: Status(
>   StatusCode="ResourceExhausted",
>   Detail="Error starting gRPC call.
>     HttpRequestException: An error occurred while sending the request.
>     IOException: The request was aborted.
>     Http2StreamException: The HTTP/2 server reset the stream. HTTP/2 error code 'ENHANCE_YOUR_CALM' (0xb).",
>   DebugException="System.Net.Http.HttpRequestException: An error occurred while sending the request.")
> ```
>
> As a workaround, we may use one of the two following options.
>
> First option is to increase OS TCP connections limits. In Linux that would be
> something similar to this:
>
> ```bash
> sudo sysctl net.core.netdev_max_backlog=10000
> sudo sysctl net.ipv4.tcp_max_syn_backlog=10000
> ```
>
> (see <https://stackoverflow.com/a/3923785>)
>
> Second option would be to use a small delay at the end of the test code, and
> subtract the delay value from the `Mean` when interpreting results.
>
> Second option (explicit delay) seems to be more reliable.

Start EventStore V22 instance with default authentication settings.

You may use Docker Compose and configuration file included in this project:

```bash
docker compose -p esdb -f esdb_v5_v22.yml up -d
```

(this Docker Compose file is also suitable for using with integration tests,
this is why it starts both ESDB V5 and ESDB V22)

Prepare the data for read benchmark test:

> ***IMPORTANT:***
>
> * This step needs to be run after set of benchmark parameters is modified
>   or when starting EventStoreDB from a clean state.
> * This step takes *long time* to complete.

```bash
dotnet run -c Release -- --filter '*WriteEventStreamBenchmark*'
```

Run benchmark:

> This step can be repeated as many times as needed given that
> benchmark parameters stay the same

```bash
dotnet run -c Release -- --filter '*ReadEventStreamBenchmark*'
```

Stop and clean up ESDB:

```bash
docker compose -p esdb -f esdb_v5_v22.yml down
docker volume rm esdb_eventstore_v5-data
docker volume rm esdb_eventstore_v22-data
docker volume prune -f
```

## Results

> 1 second delay was added after each operation as a workaround against
> `ResourceExhausted` error from ESDB/GRPC. Subtract `1.0` from the
> `Mean` values below when interpreting results.

### `BullOak.EventStore` 3.0.0-alpha23

```ini
BenchmarkDotNet=v0.13.5, OS=pop 22.04
AMD Ryzen 9 7950X, 1 CPU, 32 logical and 16 physical cores
.NET SDK=7.0.302
  [Host]     : .NET 6.0.16 (6.0.1623.17311), X64 RyuJIT AVX2
  Job-BRXPQI : .NET 6.0.16 (6.0.1623.17311), X64 RyuJIT AVX2

MaxIterationCount=20  MinIterationCount=10  WarmupCount=1
```

|     Method | EventsCount | EventSize |    Mean |    Error |   StdDev |       Gen0 |      Gen1 |      Gen2 |     Allocated |
|----------- |------------ |---------- |--------:|---------:|---------:|-----------:|----------:|----------:|--------------:|
| LoadStream |          10 |        10 | 1.003 s | 0.0003 s | 0.0002 s |          - |         - |         - |     248.38 KB |
| LoadStream |          10 |        20 | 1.003 s | 0.0014 s | 0.0008 s |          - |         - |         - |     320.44 KB |
| LoadStream |          10 |        50 | 1.003 s | 0.0019 s | 0.0013 s |          - |         - |         - |     423.38 KB |
| LoadStream |         100 |        10 | 1.005 s | 0.0006 s | 0.0004 s |          - |         - |         - |    1692.23 KB |
| LoadStream |         100 |        20 | 1.005 s | 0.0012 s | 0.0008 s |          - |         - |         - |    2133.46 KB |
| LoadStream |         100 |        50 | 1.008 s | 0.0006 s | 0.0004 s |          - |         - |         - |    3178.84 KB |
| LoadStream |         200 |        10 | 1.006 s | 0.0021 s | 0.0014 s |          - |         - |         - |    3291.84 KB |
| LoadStream |         200 |        20 | 1.008 s | 0.0012 s | 0.0007 s |          - |         - |         - |       4143 KB |
| LoadStream |         200 |        50 | 1.012 s | 0.0034 s | 0.0020 s |          - |         - |         - |    6221.27 KB |
| LoadStream |         500 |        10 | 1.012 s | 0.0037 s | 0.0025 s |          - |         - |         - |    7913.44 KB |
| LoadStream |         500 |        20 | 1.017 s | 0.0025 s | 0.0015 s |          - |         - |         - |    9839.73 KB |
| LoadStream |         500 |        50 | 1.026 s | 0.0014 s | 0.0008 s |          - |         - |         - |   15218.51 KB |
| LoadStream |        1000 |        10 | 1.023 s | 0.0025 s | 0.0015 s |          - |         - |         - |   15670.19 KB |
| LoadStream |        1000 |        20 | 1.030 s | 0.0022 s | 0.0013 s |          - |         - |         - |    19392.7 KB |
| LoadStream |        1000 |        50 | 1.053 s | 0.0082 s | 0.0054 s |          - |         - |         - |   30288.97 KB |
| LoadStream |        2000 |        10 | 1.038 s | 0.0037 s | 0.0022 s |          - |         - |         - |   32186.16 KB |
| LoadStream |        2000 |        20 | 1.054 s | 0.0084 s | 0.0056 s |          - |         - |         - |   38967.61 KB |
| LoadStream |        2000 |        50 | 1.084 s | 0.0127 s | 0.0084 s |          - |         - |         - |   61511.05 KB |
| LoadStream |        5000 |        10 | 1.092 s | 0.0125 s | 0.0083 s |          - |         - |         - |    80757.4 KB |
| LoadStream |        5000 |        20 | 1.106 s | 0.0186 s | 0.0097 s |  1000.0000 |         - |         - |   97029.32 KB |
| LoadStream |        5000 |        50 | 1.179 s | 0.0196 s | 0.0130 s |  1000.0000 |         - |         - |  153617.35 KB |
| LoadStream |       10000 |        10 | 1.174 s | 0.0222 s | 0.0161 s |  1000.0000 |         - |         - |  158585.27 KB |
| LoadStream |       10000 |        20 | 1.237 s | 0.0244 s | 0.0162 s |  2000.0000 | 1000.0000 |         - |  197793.82 KB |
| LoadStream |       10000 |        50 | 1.383 s | 0.0252 s | 0.0182 s |  4000.0000 | 2000.0000 | 1000.0000 |  307212.94 KB |
| LoadStream |       20000 |        10 | 1.333 s | 0.0171 s | 0.0090 s |  4000.0000 | 2000.0000 | 1000.0000 |  319541.38 KB |
| LoadStream |       20000 |        20 | 1.411 s | 0.0259 s | 0.0171 s |  5000.0000 | 3000.0000 | 1000.0000 |  395874.24 KB |
| LoadStream |       20000 |        50 | 1.706 s | 0.0337 s | 0.0263 s |  8000.0000 | 6000.0000 | 1000.0000 |   614288.3 KB |
| LoadStream |       50000 |        10 | 1.744 s | 0.0309 s | 0.0205 s | 10000.0000 | 8000.0000 | 1000.0000 |  805549.17 KB |
| LoadStream |       50000 |        20 | 2.035 s | 0.0346 s | 0.0229 s | 13000.0000 | 3000.0000 | 1000.0000 |  992398.91 KB |
| LoadStream |       50000 |        50 | 2.749 s | 0.0513 s | 0.0339 s | 19000.0000 | 4000.0000 | 1000.0000 | 1535713.16 KB |

### `BullOak.EventStore` 3.0.0-alpha21

> This is the version before refactorings made in `3.0.0-alpha23`.

```ini
BenchmarkDotNet=v0.13.5, OS=pop 22.04
AMD Ryzen 9 7950X, 1 CPU, 32 logical and 16 physical cores
.NET SDK=7.0.302
  [Host]     : .NET 6.0.16 (6.0.1623.17311), X64 RyuJIT AVX2
  Job-VQRORY : .NET 6.0.16 (6.0.1623.17311), X64 RyuJIT AVX2

MaxIterationCount=20  MinIterationCount=10  WarmupCount=1
```

|     Method | EventsCount | EventSize |    Mean |    Error |   StdDev |       Gen0 |       Gen1 |      Gen2 |     Allocated |
|----------- |------------ |---------- |--------:|---------:|---------:|-----------:|-----------:|----------:|--------------:|
| LoadStream |          10 |        10 | 1.003 s | 0.0006 s | 0.0004 s |          - |          - |         - |     268.71 KB |
| LoadStream |          10 |        20 | 1.003 s | 0.0005 s | 0.0004 s |          - |          - |         - |     343.88 KB |
| LoadStream |          10 |        50 | 1.003 s | 0.0003 s | 0.0002 s |          - |          - |         - |     453.84 KB |
| LoadStream |         100 |        10 | 1.005 s | 0.0004 s | 0.0003 s |          - |          - |         - |    1717.99 KB |
| LoadStream |         100 |        20 | 1.006 s | 0.0008 s | 0.0005 s |          - |          - |         - |    2094.98 KB |
| LoadStream |         100 |        50 | 1.008 s | 0.0006 s | 0.0004 s |          - |          - |         - |       3152 KB |
| LoadStream |         200 |        10 | 1.007 s | 0.0025 s | 0.0016 s |          - |          - |         - |    3308.71 KB |
| LoadStream |         200 |        20 | 1.008 s | 0.0012 s | 0.0006 s |          - |          - |         - |    4078.55 KB |
| LoadStream |         200 |        50 | 1.013 s | 0.0010 s | 0.0006 s |          - |          - |         - |    6217.61 KB |
| LoadStream |         500 |        10 | 1.011 s | 0.0012 s | 0.0007 s |          - |          - |         - |    7921.59 KB |
| LoadStream |         500 |        20 | 1.016 s | 0.0023 s | 0.0015 s |          - |          - |         - |    9984.25 KB |
| LoadStream |         500 |        50 | 1.026 s | 0.0020 s | 0.0012 s |          - |          - |         - |   15500.52 KB |
| LoadStream |        1000 |        10 | 1.026 s | 0.0042 s | 0.0028 s |          - |          - |         - |   16210.26 KB |
| LoadStream |        1000 |        20 | 1.029 s | 0.0028 s | 0.0014 s |          - |          - |         - |   19446.05 KB |
| LoadStream |        1000 |        50 | 1.054 s | 0.0080 s | 0.0053 s |          - |          - |         - |   30362.68 KB |
| LoadStream |        2000 |        10 | 1.046 s | 0.0095 s | 0.0063 s |          - |          - |         - |   31346.41 KB |
| LoadStream |        2000 |        20 | 1.057 s | 0.0088 s | 0.0052 s |          - |          - |         - |   39188.41 KB |
| LoadStream |        2000 |        50 | 1.080 s | 0.0097 s | 0.0064 s |          - |          - |         - |   61176.91 KB |
| LoadStream |        5000 |        10 | 1.104 s | 0.0118 s | 0.0078 s |          - |          - |         - |   81172.85 KB |
| LoadStream |        5000 |        20 | 1.127 s | 0.0133 s | 0.0088 s |  1000.0000 |          - |         - |   99251.73 KB |
| LoadStream |        5000 |        50 | 1.193 s | 0.0218 s | 0.0144 s |  1000.0000 |          - |         - |  152116.36 KB |
| LoadStream |       10000 |        10 | 1.177 s | 0.0211 s | 0.0140 s |  1000.0000 |          - |         - |  161332.19 KB |
| LoadStream |       10000 |        20 | 1.259 s | 0.0176 s | 0.0116 s |  2000.0000 |  1000.0000 |         - |  197245.36 KB |
| LoadStream |       10000 |        50 | 1.443 s | 0.0129 s | 0.0077 s |  4000.0000 |  2000.0000 | 1000.0000 |  307932.65 KB |
| LoadStream |       20000 |        10 | 1.401 s | 0.0144 s | 0.0095 s |  4000.0000 |  2000.0000 | 1000.0000 |  324198.27 KB |
| LoadStream |       20000 |        20 | 1.519 s | 0.0259 s | 0.0171 s |  5000.0000 |  3000.0000 | 1000.0000 |  397231.91 KB |
| LoadStream |       20000 |        50 | 1.941 s | 0.0184 s | 0.0122 s | 10000.0000 |  6000.0000 | 3000.0000 |  614465.45 KB |
| LoadStream |       50000 |        10 | 1.998 s | 0.0369 s | 0.0288 s | 12000.0000 |  8000.0000 | 3000.0000 |  809893.01 KB |
| LoadStream |       50000 |        20 | 2.384 s | 0.0471 s | 0.0368 s | 14000.0000 |  8000.0000 | 3000.0000 |  993713.66 KB |
| LoadStream |       50000 |        50 | 3.199 s | 0.0442 s | 0.0293 s | 22000.0000 | 17000.0000 | 5000.0000 | 1539122.24 KB |
