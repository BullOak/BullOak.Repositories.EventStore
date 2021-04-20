Feature: ReadEventsSpecs
	In order to support stream migration
	As a user of this library
	I want to be able to read all events from a particular stream

Scenario: Read all events from a stream
   Given a new stream
	 And 3 new events
     And I try to save the new events in the stream through their interface
    When I read all the events back from the stream
    Then the load process should succeed
     And I should see all the events

Scenario: Read events after a hard delete should return empty events
   Given a new stream
	 And 3 new events
     And I try to save the new events in the stream through their interface
    When I hard-delete the stream
     And I read all the events back from the stream
    Then the stream should be empty

Scenario: Read events after a soft delete should return empty events
   Given a new stream
	 And 3 new events
     And I try to save the new events in the stream through their interface
    When I soft-delete the stream
     And I read all the events back from the stream
    Then the stream should be empty

Scenario: Read events after a soft delete by event should return empty events
   Given a new stream
	 And 3 new events
     And I try to save the new events in the stream through their interface
    When I soft-delete-by-event the stream
     And I read all the events back from the stream
    Then the stream should be empty

Scenario: Read events after a soft delete by event with subsequant events appended should return only appended events
   Given a new stream
	 And 3 new events
     And I try to save the new events in the stream through their interface
     And I soft-delete-by-event the stream
	 And 5 new events
     And I try to save the new events in the stream through their interface
    When I read all the events back from the stream
    Then the load process should succeed
     And I should see all the appended events only

Scenario: Read events after a soft delete by custom event should return empty events
   Given a new stream
	 And 3 new events
     And I try to save the new events in the stream through their interface
    When I soft-delete-by-custom-event the stream
     And I read all the events back from the stream
    Then the stream should be empty

Scenario: Read events after a soft delete by custom event with subsequant events appended should return only appended events
   Given a new stream
	 And 3 new events
     And I try to save the new events in the stream through their interface
     And I soft-delete-by-custom-event the stream
	 And 5 new events
     And I try to save the new events in the stream through their interface
    When I read all the events back from the stream
    Then the load process should succeed
     And I should see all the appended events only
