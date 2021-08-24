Feature: ReadModelSpecs
	In order to support read models
	As a user of this library
	I want to be able to load entities from readonly repositories

Scenario: Reconstitute state from one event stored using interface
	Given a new stream
	And 3 new events
	And I try to save the new events in the stream through their interface
	When I load my entity through the read-only repository
	Then the load process should succeed
	And HighOrder property should be 2
	And have a concurrency id of 2

Scenario: Load entity as at a point in time
	Given a new stream
	And the following events with the following timestamps
		| Timestamp           |
		| 2020-09-10 11:10:00 |
		| 2020-09-20 11:10:00 |
		| 2020-09-23 11:10:00 |
	And I try to save the new events in the stream through their interface
	When I load my entity through the read-only repository as of '2020-09-22 11:10:00'
	Then the load process should succeed
	And HighOrder property should be 1

Scenario: Reconstitute streams with one event type based on category
	Given a new stream
	And the following events with the following timestamps
		| Timestamp           |
		| 2020-09-10 11:10:00 |
		| 2020-09-20 11:10:00 |
		| 2020-09-23 11:10:00 |
	And I try to save the new events in the stream through their interface
	And after waiting for 15 seconds for categories to be processed
	When I load all my entities for the streams category
	Then the load process should succeed
    And HighOrder property should be 2

Scenario: Reconstitute streams with one event type based on category up to a given date
	Given a new stream
    And another new stream
	And the following events with timestamps for stream 1
		| Timestamp           |
		| 2020-09-10 11:10:00 |
		| 2020-09-20 11:10:00 |
		| 2020-09-23 11:10:00 |
    And the following events with timestamps for stream 2
		| Timestamp           |
		| 2020-09-10 11:10:00 |
		| 2020-09-20 11:10:00 |
        | 2020-09-20 12:10:00 |
        | 2020-09-20 12:20:00 |
		| 2020-09-23 11:10:00 |
	And I try to save the new events in the stream through their interface
	And after waiting for 15 seconds for categories to be processed
	When I load all my entities as of '2020-09-22 11:10:00' for the streams category
	Then the load process should succeed
    And HighOrder property for stream 1 should be 1
    And HighOrder property for stream 2 should be 3

Scenario: Reconstitute state based on category with two event types up to a given date
	Given a new stream
	And the following events with the following timestamps
		| Timestamp           |
		| 2020-09-10 11:10:00 |
	And I try to save the new events in the stream through their interface
    And I update the state of visible to be enabled as of '2020-09-22 11:10:00'
	And after waiting for 15 seconds for categories to be processed
	When I load all my entities as of '2020-09-20 11:10:00' for the streams category
	Then the load process should succeed
    And the visibilty should be disabled
