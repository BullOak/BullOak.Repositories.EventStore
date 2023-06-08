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

Start EventStore V22 instance with default authentication settings.
You may use Docker Compose and configuration file included in this project:

```bash
docker compose -p esdb -f esdb_v5_v22.yml up -d
```

(this Docker Compose file is also suitable for using with integration tests,
this is why it starts both ESDB V5 and ESDB V22)

Prepare the data for read benchmark test:

> ***IMPORTANT:***
> * This step needs to be run after set of benchmark parameters is modified
>   or when starting EventStoreDB from a clean state.
> * This step takes _long time_ to complete.

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

> 3 second delay was added after each operation as a workaround against
> `ResourceExhausted` error from ESDB/GRPC. Subtract `3.0` from the
> `Mean` values below when interpreting results.

### `BullOak.EventStore` 3.0.0-alpha23

``` ini
BenchmarkDotNet=v0.13.5, OS=pop 22.04
AMD Ryzen 9 7950X, 1 CPU, 32 logical and 16 physical cores
.NET SDK=7.0.302
  [Host]     : .NET 6.0.16 (6.0.1623.17311), X64 RyuJIT AVX2
  Job-PWWHVJ : .NET 6.0.16 (6.0.1623.17311), X64 RyuJIT AVX2

MaxIterationCount=10  MinIterationCount=5  WarmupCount=1
```

|     Method | EventsCount | EventSize |    Mean |    Error |   StdDev |       Gen0 |      Gen1 |      Gen2 |     Allocated |
|----------- |------------ |---------- |--------:|---------:|---------:|-----------:|----------:|----------:|--------------:|
| **LoadStream** |          **10** |        **10** | **3.003 s** | **0.0005 s** | **0.0001 s** |          **-** |         **-** |         **-** |     **397.78 KB** |
| **LoadStream** |          **10** |        **20** | **3.002 s** | **0.0039 s** | **0.0010 s** |          **-** |         **-** |         **-** |     **415.98 KB** |
| **LoadStream** |          **10** |        **50** | **3.004 s** | **0.0060 s** | **0.0015 s** |          **-** |         **-** |         **-** |     **555.05 KB** |
| **LoadStream** |         **100** |        **10** | **3.004 s** | **0.0077 s** | **0.0020 s** |          **-** |         **-** |         **-** |    **1798.96 KB** |
| **LoadStream** |         **100** |        **20** | **3.006 s** | **0.0006 s** | **0.0001 s** |          **-** |         **-** |         **-** |    **2173.54 KB** |
| **LoadStream** |         **100** |        **50** | **3.007 s** | **0.0081 s** | **0.0012 s** |          **-** |         **-** |         **-** |    **3301.76 KB** |
| **LoadStream** |         **200** |        **10** | **3.006 s** | **0.0040 s** | **0.0006 s** |          **-** |         **-** |         **-** |    **3361.64 KB** |
| **LoadStream** |         **200** |        **20** | **3.007 s** | **0.0027 s** | **0.0007 s** |          **-** |         **-** |         **-** |    **4144.62 KB** |
| **LoadStream** |         **200** |        **50** | **3.012 s** | **0.0051 s** | **0.0013 s** |          **-** |         **-** |         **-** |    **6290.73 KB** |
| **LoadStream** |         **500** |        **10** | **3.012 s** | **0.0070 s** | **0.0018 s** |          **-** |         **-** |         **-** |    **8321.01 KB** |
| **LoadStream** |         **500** |        **20** | **3.017 s** | **0.0059 s** | **0.0015 s** |          **-** |         **-** |         **-** |    **9846.96 KB** |
| **LoadStream** |         **500** |        **50** | **3.023 s** | **0.0215 s** | **0.0033 s** |          **-** |         **-** |         **-** |   **15286.16 KB** |
| **LoadStream** |        **1000** |        **10** | **3.022 s** | **0.0121 s** | **0.0031 s** |          **-** |         **-** |         **-** |   **16264.49 KB** |
| **LoadStream** |        **1000** |        **20** | **3.034 s** | **0.0182 s** | **0.0047 s** |          **-** |         **-** |         **-** |   **19529.27 KB** |
| **LoadStream** |        **1000** |        **50** | **3.053 s** | **0.0162 s** | **0.0042 s** |          **-** |         **-** |         **-** |   **30370.91 KB** |
| **LoadStream** |        **2000** |        **10** | **3.036 s** | **0.0166 s** | **0.0026 s** |          **-** |         **-** |         **-** |    **32010.7 KB** |
| **LoadStream** |        **2000** |        **20** | **3.060 s** | **0.0313 s** | **0.0081 s** |          **-** |         **-** |         **-** |   **38747.32 KB** |
| **LoadStream** |        **2000** |        **50** | **3.079 s** | **0.0273 s** | **0.0042 s** |          **-** |         **-** |         **-** |   **61426.39 KB** |
| **LoadStream** |        **5000** |        **10** | **3.090 s** | **0.0601 s** | **0.0093 s** |          **-** |         **-** |         **-** |    **80301.3 KB** |
| **LoadStream** |        **5000** |        **20** | **3.117 s** | **0.0239 s** | **0.0062 s** |  **1000.0000** |         **-** |         **-** |   **99457.47 KB** |
| **LoadStream** |        **5000** |        **50** | **3.182 s** | **0.0562 s** | **0.0200 s** |  **1000.0000** |         **-** |         **-** |  **152812.96 KB** |
| **LoadStream** |       **10000** |        **10** | **3.167 s** | **0.0360 s** | **0.0093 s** |  **1000.0000** |         **-** |         **-** |  **161482.42 KB** |
| **LoadStream** |       **10000** |        **20** | **3.240 s** | **0.0506 s** | **0.0131 s** |  **2000.0000** | **1000.0000** |         **-** |  **198632.21 KB** |
| **LoadStream** |       **10000** |        **50** | **3.364 s** | **0.0461 s** | **0.0071 s** |  **4000.0000** | **2000.0000** | **1000.0000** |  **307373.34 KB** |
| **LoadStream** |       **20000** |        **10** | **3.337 s** | **0.0290 s** | **0.0075 s** |  **4000.0000** | **2000.0000** | **1000.0000** |  **319964.18 KB** |
| **LoadStream** |       **20000** |        **20** | **3.413 s** | **0.0598 s** | **0.0155 s** |  **5000.0000** | **3000.0000** | **1000.0000** |  **395873.04 KB** |
| **LoadStream** |       **20000** |        **50** | **3.687 s** | **0.0526 s** | **0.0137 s** |  **8000.0000** | **6000.0000** | **1000.0000** |  **613118.03 KB** |
| **LoadStream** |       **50000** |        **10** | **3.733 s** | **0.0651 s** | **0.0289 s** | **10000.0000** | **8000.0000** | **1000.0000** |  **805389.76 KB** |
| **LoadStream** |       **50000** |        **20** | **3.994 s** | **0.0727 s** | **0.0433 s** | **13000.0000** | **3000.0000** | **1000.0000** |   **990498.4 KB** |
| **LoadStream** |       **50000** |        **50** | **4.707 s** | **0.0928 s** | **0.0144 s** | **19000.0000** | **4000.0000** | **1000.0000** | **1535756.89 KB** |

### `BullOak.EventStore` 3.0.0-alpha21

> This is the version before major performance improvements made in
> `3.0.0-alpha23`.
