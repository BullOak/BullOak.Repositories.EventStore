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
