Feature: EventStore Metadata Support
    In order to be able to use the library with any EventStore stream
    As a developer using this library
    I want to be able to use session with regular streams and projections containing
    metadata entries not related to regular events, e.g. links to missing events

    Scenario: Open EventStore deleted stream

        As opposed to event-based soft deleted stream (by using custom EntitySoftDeleted event),
        EventStore soft-deleted stream is a stream that has been deleted via ES Admin Console
        or by using HTTP DELETE operation. ES adds a soft-delete metadata marker and scavenges (eventually)
        all the existing events from the stream.

        Given a stream with events
        And I delete the stream in EventStore
        When I try to open the stream
        Then the session reports new state

    Scenario: Append events to EventStore deleted stream

        Given a stream with events
        And I delete the stream in EventStore
        And I add some new events to the stream
        When I try to open the stream
        Then the session can rehydrate the state

    Scenario: Open EventStore truncated stream

        EventStore allows to truncate all the events in the stream before a given event version N.
        ES modifies the stream metadata and scavenges (eventually) all the events before version N.
        Resulting stream looks just like a non-truncated stream, except the first event in the stream
        has id N and not 0.

        Semantically, an empty stream after being truncated is the same as an empty projection

        Given a stream with events
        And I truncate the stream in EventStore
        When I try to open the stream
        Then the session reports new state

    Scenario: Append events to EventStore truncated stream

        Given a stream with events
        And I truncate the stream in EventStore
        And I add some new events to the stream
        When I try to open the stream
        Then the session can rehydrate the state

    Scenario: Open EventStore projection pointing to undefined events

        Session can be used pointing to a projection that does not include any events.

        When I try to open a projection that uses undefined events
        Then the session reports new state

    Scenario: Open EventStore projection containing links to deleted events

        https://developers.eventstore.com/server/v20.10/docs/streams/deleting-streams-and-events.html#deleted-events-and-projections

        Projections may contain metadata entries that do no resolve to an actual event
        because the original event was truncated.

        Given a stream with events
        And I truncate the stream in EventStore
        And I add some new events to the stream
        When I try to open a projection that uses events from the truncated stream
        Then the session can rehydrate the state
