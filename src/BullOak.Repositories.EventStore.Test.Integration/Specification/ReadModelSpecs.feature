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
	And 3 new events with the following timestamps
		| Timestamp           |
		| 2020-09-10 11:10:00 |
		| 2020-09-20 11:10:00 |
		| 2020-09-23 11:10:00 |
	And I try to save the new events in the stream through their interface
	When I load my entity through the read-only repository as of '2020-09-22 11:10:00'
	Then the load process should succeed
	And HighOrder property should be 1
