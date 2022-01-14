Feature: ProtocolSpecs
Specs should be functional by both tcp and grpc protocols

    Scenario Outline: Reconstitute state from one event stored using interface
        Given the <protocol> protocol is being used
        * a new stream
        * 3 new events
        * I try to save the new events in the stream through IStoreEventsToStream interface
        When I load my entity through the IReadEventsFromStream
        Then the load process should succeed
        * events returned should be 3
        * stream position should be 2
        Examples:
          | protocol |
          | tcp      |
          | grpc     |
