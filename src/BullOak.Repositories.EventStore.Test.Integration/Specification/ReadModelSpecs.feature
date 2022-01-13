Feature: ReadModelSpecs
	In order to support read models
	As a user of this library
	I want to be able to load entities from readonly repositories

Scenario Outline: Reconstitute state from one event stored using interface
    Given the <protocol> protocol is being used
	* a new stream
	* 3 new events
	* I try to save the new events in the stream through their interface
	When I load my entity through the read-only repository
	Then the load process should succeed
	* HighOrder property should be 2
	* have a concurrency id of 2

Examples:
| protocol |
| tcp      |
| grpc     |

Scenario Outline: Load entity as at a point in time
    Given the <protocol> protocol is being used
	* a new stream
	* the following events with the following timestamps
		| Timestamp           |
		| 2020-09-10 11:10:00 |
		| 2020-09-20 11:10:00 |
		| 2020-09-23 11:10:00 |
	* I try to save the new events in the stream through their interface
	When I load my entity through the read-only repository as of '2020-09-22 11:10:00'
	Then the load process should succeed
	* HighOrder property should be 1

Examples:
| protocol |
| tcp      |
| grpc     |

Scenario Outline: Reconstitute streams with one event type based on category
    Given the <protocol> protocol is being used
	* a new stream
	* the following events with the following timestamps
		| Timestamp           |
		| 2020-09-10 11:10:00 |
		| 2020-09-20 11:10:00 |
		| 2020-09-23 11:10:00 |
	* I try to save the new events in the stream through their interface
	* after waiting for 15 seconds for categories to be processed
	When I load all my entities for the streams category
	Then the load process should succeed
    * HighOrder property should be 2

Examples:
| protocol |
| tcp      |
| grpc     |

Scenario Outline: Reconstitute streams with one event type based on category up to a given date
    Given the <protocol> protocol is being used
    * a new stream
    * another new stream
	* the following events with timestamps for stream 1
		| Timestamp           |
		| 2020-09-10 11:10:00 |
		| 2020-09-20 11:10:00 |
		| 2020-09-23 11:10:00 |
    * the following events with timestamps for stream 2
		| Timestamp           |
		| 2020-09-10 11:10:00 |
		| 2020-09-20 11:10:00 |
        | 2020-09-20 12:10:00 |
        | 2020-09-20 12:20:00 |
		| 2020-09-23 11:10:00 |
	* I try to save the new events in the stream through their interface
	* after waiting for 15 seconds for categories to be processed
	When I load all my entities as of '2020-09-22 11:10:00' for the streams category
	Then the load process should succeed
    * HighOrder property for stream 1 should be 1
    * HighOrder property for stream 2 should be 3

Examples:
| protocol |
| tcp      |
| grpc     |

Scenario Outline: Reconstitute state based on category with two event types up to a given date
    Given the <protocol> protocol is being used
	* a new stream
	* the following events with the following timestamps
		| Timestamp           |
		| 2020-09-10 11:10:00 |
	* I try to save the new events in the stream through their interface
    * I update the state of visible to be enabled as of '2020-09-22 11:10:00'
	* after waiting for 15 seconds for categories to be processed
	When I load all my entities as of '2020-09-20 11:10:00' for the streams category
	Then the load process should succeed
    * the visibilty should be disabled

Examples:
| protocol |
| tcp      |
| grpc     |
