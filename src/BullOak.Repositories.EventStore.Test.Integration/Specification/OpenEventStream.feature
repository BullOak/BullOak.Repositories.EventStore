Feature: OpenEventsStream
	In order to persist using an event stream
	As a developer using this new library
	I want to be able to open an event stream

Background:
    Given the grpc protocol is being used

Scenario: When trying to open a stream that does not exist, session should report IsNewState true
    Given a new stream
	When I try to open the new stream
	Then the session reports new state
	And the session state is intialized

Scenario: When trying to open a stream that was soft-deleted, session should report IsNewState true
    Given a new stream
	And the following events with the following timestamps
		| Timestamp           |
		| 2020-09-10 11:10:00 |
		| 2020-09-20 11:10:00 |
		| 2020-09-23 11:10:00 |
	And I try to save the new events in the stream through their interface
    And I soft-delete the stream
	When I try to open the new stream
	Then the session reports new state
	And the session state is intialized

