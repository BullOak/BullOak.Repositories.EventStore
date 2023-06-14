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

### Comparing versions 3.0.0-alpha24, 3.0.0-alpha23, 3.0.0-alpha21

Benchmark results directly comparing 3 latest versions:

* [Full report](./Benchmark-VersionsComparison-Full.html)
* [Short report](./Benchmark-VersionsComparison-Short.html) with most
  permutations excluded, it is easier to read but still provides a good overview

### `BullOak.EventStore` 3.0.0-alpha24

```ini
BenchmarkDotNet=v0.13.5, OS=pop 22.04
AMD Ryzen 9 7950X, 1 CPU, 32 logical and 16 physical cores
.NET SDK=7.0.302
  [Host]     : .NET 6.0.16 (6.0.1623.17311), X64 RyuJIT AVX2
  Job-FSGJAI : .NET 6.0.16 (6.0.1623.17311), X64 RyuJIT AVX2

InvocationCount=1  MaxIterationCount=50  MinIterationCount=20
UnrollFactor=1  WarmupCount=1
```

|     Method | EventsCount | EventSize |       Mean |      Error |     StdDev |       Gen0 |      Gen1 |     Allocated |
|----------- |------------ |---------- |-----------:|-----------:|-----------:|-----------:|----------:|--------------:|
| LoadStream |          10 |        10 |   1.552 ms |  0.0940 ms |  0.1877 ms |          - |         - |     188.55 KB |
| LoadStream |          10 |        20 |   1.532 ms |  0.0748 ms |  0.1459 ms |          - |         - |     216.82 KB |
| LoadStream |          10 |        50 |   1.492 ms |  0.0818 ms |  0.1575 ms |          - |         - |      299.4 KB |
| LoadStream |         100 |        10 |   2.801 ms |  0.1850 ms |  0.3736 ms |          - |         - |    1545.11 KB |
| LoadStream |         100 |        20 |   3.048 ms |  0.1800 ms |  0.3594 ms |          - |         - |    1763.38 KB |
| LoadStream |         100 |        50 |   3.624 ms |  0.2956 ms |  0.5904 ms |          - |         - |    2571.34 KB |
| LoadStream |         200 |        10 |   4.320 ms |  0.2714 ms |  0.5357 ms |          - |         - |    2925.52 KB |
| LoadStream |         200 |        20 |   4.304 ms |  0.2418 ms |  0.4659 ms |          - |         - |    3482.82 KB |
| LoadStream |         200 |        50 |   5.552 ms |  0.3599 ms |  0.7104 ms |          - |         - |    5096.26 KB |
| LoadStream |         500 |        10 |   8.584 ms |  0.8805 ms |  1.7380 ms |          - |         - |    7249.25 KB |
| LoadStream |         500 |        20 |   8.900 ms |  0.7306 ms |  1.4592 ms |          - |         - |    8641.35 KB |
| LoadStream |         500 |        50 |  12.822 ms |  1.0406 ms |  2.1021 ms |          - |         - |   12970.44 KB |
| LoadStream |        1000 |        10 |  17.849 ms |  1.7595 ms |  3.5138 ms |          - |         - |   15063.75 KB |
| LoadStream |        1000 |        20 |  17.543 ms |  1.1593 ms |  2.2610 ms |          - |         - |   17703.13 KB |
| LoadStream |        1000 |        50 |  25.648 ms |  1.9724 ms |  3.9391 ms |          - |         - |   25294.23 KB |
| LoadStream |        2000 |        10 |  30.015 ms |  2.3754 ms |  4.6331 ms |          - |         - |   30084.53 KB |
| LoadStream |        2000 |        20 |  32.220 ms |  2.6758 ms |  5.1554 ms |          - |         - |   35414.64 KB |
| LoadStream |        2000 |        50 |  45.455 ms |  3.6696 ms |  7.4127 ms |          - |         - |   51143.98 KB |
| LoadStream |        5000 |        10 |  70.871 ms |  4.7333 ms |  9.3432 ms |          - |         - |   75141.59 KB |
| LoadStream |        5000 |        20 |  82.555 ms |  4.4087 ms |  8.8046 ms |  1000.0000 |         - |   86387.01 KB |
| LoadStream |        5000 |        50 |  97.967 ms |  4.4871 ms |  8.9612 ms |  1000.0000 |         - |  128826.98 KB |
| LoadStream |       10000 |        10 | 124.212 ms |  5.3951 ms | 10.8984 ms |  1000.0000 |         - |  150033.38 KB |
| LoadStream |       10000 |        20 | 146.099 ms |  5.9870 ms | 11.9567 ms |  2000.0000 |         - |  178077.86 KB |
| LoadStream |       10000 |        50 | 183.048 ms |  7.0636 ms | 14.2689 ms |  3000.0000 |         - |  255811.01 KB |
| LoadStream |       20000 |        10 | 229.650 ms |  7.2827 ms | 14.7115 ms |  3000.0000 |         - |  298309.46 KB |
| LoadStream |       20000 |        20 | 257.823 ms |  7.4720 ms | 15.0939 ms |  4000.0000 |         - |  356043.27 KB |
| LoadStream |       20000 |        50 | 333.234 ms | 10.3116 ms | 20.8299 ms |  6000.0000 |         - |  516585.06 KB |
| LoadStream |       50000 |        10 | 532.740 ms | 12.7867 ms | 25.8297 ms |  9000.0000 |         - |  751012.45 KB |
| LoadStream |       50000 |        20 | 580.832 ms | 12.3614 ms | 24.6870 ms | 10000.0000 |         - |  889961.38 KB |
| LoadStream |       50000 |        50 | 804.877 ms | 16.0397 ms | 26.7987 ms | 15000.0000 | 1000.0000 | 1290127.38 KB |

### `BullOak.EventStore` 3.0.0-alpha23

```ini
BenchmarkDotNet=v0.13.5, OS=pop 22.04
AMD Ryzen 9 7950X, 1 CPU, 32 logical and 16 physical cores
.NET SDK=7.0.302
  [Host]     : .NET 6.0.16 (6.0.1623.17311), X64 RyuJIT AVX2
  Job-UIEOTV : .NET 6.0.16 (6.0.1623.17311), X64 RyuJIT AVX2

InvocationCount=1  MaxIterationCount=50  MinIterationCount=20
UnrollFactor=1  WarmupCount=1
```

|     Method | EventsCount | EventSize |       Mean |      Error |     StdDev |       Gen0 |      Gen1 |     Allocated |
|----------- |------------ |---------- |-----------:|-----------:|-----------:|-----------:|----------:|--------------:|
| LoadStream |          10 |        10 |   2.520 ms |  0.1222 ms |  0.2412 ms |          - |         - |     235.59 KB |
| LoadStream |          10 |        20 |   2.431 ms |  0.1052 ms |  0.2027 ms |          - |         - |     282.73 KB |
| LoadStream |          10 |        50 |   2.597 ms |  0.1122 ms |  0.2214 ms |          - |         - |     360.18 KB |
| LoadStream |         100 |        10 |   3.719 ms |  0.1342 ms |  0.2649 ms |          - |         - |    1531.16 KB |
| LoadStream |         100 |        20 |   3.880 ms |  0.1967 ms |  0.3883 ms |          - |         - |    1812.23 KB |
| LoadStream |         100 |        50 |   4.526 ms |  0.2178 ms |  0.4248 ms |          - |         - |    2627.75 KB |
| LoadStream |         200 |        10 |   5.336 ms |  0.2836 ms |  0.5532 ms |          - |         - |    2972.41 KB |
| LoadStream |         200 |        20 |   5.208 ms |  0.2673 ms |  0.5277 ms |          - |         - |    3646.58 KB |
| LoadStream |         200 |        50 |   6.499 ms |  0.4444 ms |  0.8772 ms |          - |         - |    5152.13 KB |
| LoadStream |         500 |        10 |  10.308 ms |  0.8686 ms |  1.7146 ms |          - |         - |    7507.81 KB |
| LoadStream |         500 |        20 |   9.709 ms |  0.7598 ms |  1.4638 ms |          - |         - |    8802.37 KB |
| LoadStream |         500 |        50 |  12.328 ms |  1.0560 ms |  2.0597 ms |          - |         - |   12730.02 KB |
| LoadStream |        1000 |        10 |  18.865 ms |  1.8090 ms |  3.5709 ms |          - |         - |   15101.38 KB |
| LoadStream |        1000 |        20 |  21.133 ms |  1.8879 ms |  3.8136 ms |          - |         - |   17287.59 KB |
| LoadStream |        1000 |        50 |  25.401 ms |  2.2444 ms |  4.5337 ms |          - |         - |   25350.02 KB |
| LoadStream |        2000 |        10 |  32.086 ms |  2.4070 ms |  4.8622 ms |          - |         - |   28914.65 KB |
| LoadStream |        2000 |        20 |  39.813 ms |  3.3498 ms |  6.7668 ms |          - |         - |   34482.17 KB |
| LoadStream |        2000 |        50 |  48.342 ms |  4.4869 ms |  8.9609 ms |          - |         - |   50598.52 KB |
| LoadStream |        5000 |        10 |  73.721 ms |  5.1575 ms | 10.4184 ms |          - |         - |   73715.01 KB |
| LoadStream |        5000 |        20 |  82.353 ms |  5.3934 ms | 10.6461 ms |  1000.0000 |         - |   87456.27 KB |
| LoadStream |        5000 |        50 | 104.116 ms |  4.5825 ms |  9.2570 ms |  1000.0000 |         - |   128087.9 KB |
| LoadStream |       10000 |        10 | 128.425 ms |  4.7900 ms |  9.2287 ms |  1000.0000 |         - |   149828.7 KB |
| LoadStream |       10000 |        20 | 145.147 ms |  6.2878 ms | 12.7017 ms |  2000.0000 |         - |  177777.85 KB |
| LoadStream |       10000 |        50 | 178.810 ms |  7.2537 ms | 14.6528 ms |  3000.0000 |         - |  256627.16 KB |
| LoadStream |       20000 |        10 | 233.480 ms |  7.5443 ms | 15.2399 ms |  3000.0000 |         - |  299680.45 KB |
| LoadStream |       20000 |        20 | 260.254 ms |  7.3737 ms | 14.8952 ms |  4000.0000 |         - |  356050.26 KB |
| LoadStream |       20000 |        50 | 338.950 ms |  8.7203 ms | 17.4154 ms |  6000.0000 |         - |  517114.99 KB |
| LoadStream |       50000 |        10 | 552.210 ms | 10.9243 ms | 20.5185 ms |  9000.0000 |         - |  751169.00 KB |
| LoadStream |       50000 |        20 | 584.105 ms | 11.3986 ms | 19.0444 ms | 10000.0000 |         - |  890236.01 KB |
| LoadStream |       50000 |        50 | 779.933 ms | 15.2969 ms | 29.4720 ms | 15000.0000 | 1000.0000 | 1292078.75 KB |

### `BullOak.EventStore` 3.0.0-alpha21

> This is the version before refactorings made in `3.0.0-alpha23`.

```ini
BenchmarkDotNet=v0.13.5, OS=pop 22.04
AMD Ryzen 9 7950X, 1 CPU, 32 logical and 16 physical cores
.NET SDK=7.0.302
  [Host]     : .NET 6.0.16 (6.0.1623.17311), X64 RyuJIT AVX2
  Job-OBSIRM : .NET 6.0.16 (6.0.1623.17311), X64 RyuJIT AVX2

InvocationCount=1  MaxIterationCount=50  MinIterationCount=20
UnrollFactor=1  WarmupCount=1
```

|     Method | EventsCount | EventSize |         Mean |      Error |     StdDev |       Gen0 |       Gen1 |      Gen2 |     Allocated |
|----------- |------------ |---------- |-------------:|-----------:|-----------:|-----------:|-----------:|----------:|--------------:|
| LoadStream |          10 |        10 |     2.460 ms |  0.1479 ms |  0.2987 ms |          - |          - |         - |     238.48 KB |
| LoadStream |          10 |        20 |     2.440 ms |  0.1308 ms |  0.2612 ms |          - |          - |         - |     272.12 KB |
| LoadStream |          10 |        50 |     2.434 ms |  0.0872 ms |  0.1638 ms |          - |          - |         - |     369.75 KB |
| LoadStream |         100 |        10 |     3.566 ms |  0.1563 ms |  0.3012 ms |          - |          - |         - |    1540.57 KB |
| LoadStream |         100 |        20 |     3.889 ms |  0.2093 ms |  0.4180 ms |          - |          - |         - |    1824.18 KB |
| LoadStream |         100 |        50 |     4.436 ms |  0.1966 ms |  0.3788 ms |          - |          - |         - |    2679.36 KB |
| LoadStream |         200 |        10 |     5.399 ms |  0.2707 ms |  0.5344 ms |          - |          - |         - |    3081.78 KB |
| LoadStream |         200 |        20 |     5.245 ms |  0.2475 ms |  0.4769 ms |          - |          - |         - |    3550.45 KB |
| LoadStream |         200 |        50 |     6.555 ms |  0.4672 ms |  0.9001 ms |          - |          - |         - |    5178.72 KB |
| LoadStream |         500 |        10 |     9.161 ms |  0.8123 ms |  1.5843 ms |          - |          - |         - |    7329.73 KB |
| LoadStream |         500 |        20 |    10.341 ms |  0.6709 ms |  1.2765 ms |          - |          - |         - |    8730.98 KB |
| LoadStream |         500 |        50 |    13.231 ms |  0.8363 ms |  1.5912 ms |          - |          - |         - |   12771.33 KB |
| LoadStream |        1000 |        10 |    18.272 ms |  1.6466 ms |  3.2885 ms |          - |          - |         - |   15148.57 KB |
| LoadStream |        1000 |        20 |    20.029 ms |  1.6956 ms |  3.2669 ms |          - |          - |         - |   17797.24 KB |
| LoadStream |        1000 |        50 |    26.230 ms |  2.1055 ms |  4.2049 ms |          - |          - |         - |   25462.55 KB |
| LoadStream |        2000 |        10 |    31.219 ms |  3.4170 ms |  6.8240 ms |          - |          - |         - |   29395.99 KB |
| LoadStream |        2000 |        20 |    38.805 ms |  3.5219 ms |  7.1143 ms |          - |          - |         - |   34612.04 KB |
| LoadStream |        2000 |        50 |    48.107 ms |  4.6436 ms |  9.3803 ms |          - |          - |         - |   50737.45 KB |
| LoadStream |        5000 |        10 |    72.475 ms |  5.9735 ms | 12.0668 ms |          - |          - |         - |   75548.30 KB |
| LoadStream |        5000 |        20 |    97.077 ms |  6.0606 ms | 12.1036 ms |  1000.0000 |          - |         - |   89477.13 KB |
| LoadStream |        5000 |        50 |   119.984 ms |  5.4336 ms | 10.9762 ms |  1000.0000 |          - |         - |  128590.63 KB |
| LoadStream |       10000 |        10 |   149.322 ms |  6.4896 ms | 13.1094 ms |  1000.0000 |          - |         - |  149511.53 KB |
| LoadStream |       10000 |        20 |   188.259 ms |  5.3063 ms | 10.5972 ms |  2000.0000 |  1000.0000 |         - |  178699.77 KB |
| LoadStream |       10000 |        50 |   262.563 ms |  7.6288 ms | 15.4106 ms |  4000.0000 |  2000.0000 | 1000.0000 |  257000.42 KB |
| LoadStream |       20000 |        10 |   305.042 ms |  7.1400 ms | 14.4231 ms |  4000.0000 |  2000.0000 | 1000.0000 |  301759.55 KB |
| LoadStream |       20000 |        20 |   370.422 ms |  8.2918 ms | 16.5597 ms |  5000.0000 |  3000.0000 | 1000.0000 |  357488.48 KB |
| LoadStream |       20000 |        50 |   532.825 ms | 10.4340 ms | 12.8138 ms |  8000.0000 |  5000.0000 | 2000.0000 |  516827.02 KB |
| LoadStream |       50000 |        10 |   793.433 ms | 15.5532 ms | 17.9111 ms | 12000.0000 |  8000.0000 | 3000.0000 |  754559.59 KB |
| LoadStream |       50000 |        20 |   936.494 ms | 18.5947 ms | 32.0749 ms | 12000.0000 |  7000.0000 | 2000.0000 |  893463.86 KB |
| LoadStream |       50000 |        50 | 1,376.919 ms | 26.5389 ms | 41.3179 ms | 19000.0000 | 14000.0000 | 4000.0000 | 1294974.04 KB |
