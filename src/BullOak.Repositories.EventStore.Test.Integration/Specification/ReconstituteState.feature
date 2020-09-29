Feature: ReconstituteState
	In order to apply business logic on stored entities
	As a developer using this library
	I want to be able to get correctly reconstituted states from my event stream

Scenario Outline: Load stored entity with from existing events
	Given a new stream
	And <eventsCount> new events
	When I try to save the new events in the stream
	And I load my entity
	Then HighOrder property should be <expectedState>

	Examples:
		| eventsCount | expectedState |
		| 2           | 1             |
		| 5           | 4             |
		| 10000       | 9999          |

Scenario: Reconstitute state from one event stored using interface
	Given a new stream
	And 3 new events
	And I try to save the new events in the stream through their interface
	When I load my entity
	Then the load process should succeed
	And HighOrder property should be 2

Scenario: Reconstitute state up to a given date
	Given a new stream
	And the following events with the following timestamps
		| Timestamp           |
		| 2020-09-10 11:10:00 |
		| 2020-09-20 11:10:00 |
		| 2020-09-23 11:10:00 |
	And I try to save the new events in the stream through their interface
	When I load my entity as of '2020-09-22 11:10:00'
	Then the load process should succeed
	And HighOrder property should be 1

Scenario: Reconstitute streams with one event type state based on category up to a given date
	Given a new stream
    And a second stream with the same category
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
	When I load all my entities as of '2020-09-22 11:10:00'
	Then the load process should succeed
	And two streams should be returned
    And HighOrder property for stream 1 should be 1
    And HighOrder property for stream 2 should be 3

Scenario: Reconstitute state based on category with two event types up to a given date

Scenario: Reconstitute state from empty stream should succeed and return default state
	Given a new stream
	When I load my entity
	Then the load process should succeed
	And HighOrder property should be 0

Scenario: Reconstitute state after a soft delete should succeed and return default state
	Given a new stream
	And 3 new events
	And  I soft-delete the stream
	When I load my entity
	Then the load process should succeed
	And HighOrder property should be 0

Scenario: Reconstitute state after a hard delete should succeed and return default state
	Given a new stream
	And 3 new events
	And  I hard-delete the stream
	When I load my entity
	Then the load process should succeed
	And HighOrder property should be 0

Scenario: Reconstitute state after a soft delete by event should succeed and return default state
	Given a new stream
	And 3 new events
	And I soft-delete-by-event the stream
	When I load my entity
	Then the load process should succeed
	And HighOrder property should be 0

Scenario: Reconstitute state after a soft delete by custom event should succeed and return default state
	Given a new stream
	And 3 new events
	And I soft-delete-by-custom-event the stream
	When I load my entity
	Then the load process should succeed
	And HighOrder property should be 0
