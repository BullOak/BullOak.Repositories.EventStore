Feature: OpenEventsStream
	In order to persist using an event stream
	As a developer using this new library
	I want to be able to open an event stream

Background:
    Given the grpc protocol is being used

Scenario: Open a stream that does not exist
    Given a new stream
	When I try to open the new stream
	Then the session reports new state
	And the session state is intialized
