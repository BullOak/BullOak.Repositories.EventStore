Feature: SaveEventsStream
	In order to persist using an event stream
	As a developer usint this new library
	I want to be able to save events in a stream

Scenario Outline: Save events in a new stream using TCP
    Given the tcp protocol is being used
	* a new stream
	* <eventsCount> new events
	When I try to save the new events in the stream
	Then the save process should succeed
	* there should be <eventsCount> events in the stream
Examples:
	| eventsCount |
	| 1           |
	| 30          |
	| 10000       |

Scenario Outline: Save events in a new stream using GRPC
    Given the grpc protocol is being used
	* a new stream
	* <eventsCount> new events
	When I try to save the new events in the stream
	Then the save process should succeed
	* there should be <eventsCount> events in the stream
Examples:
	| eventsCount |
	| 1           |
	| 30          |
	| 10000       |

Scenario Outline: Save one event using interface
    Given the <protocol> protocol is being used
	* a new stream
	And 1 new event
	When I try to save the new events in the stream through their interface
	Then the save process should succeed
Examples:
| protocol |
| tcp      |
| grpc     |

Scenario Outline: Save additional events in an existing stream
    Given the <protocol> protocol is being used
	* an existing stream with 10 events
	* 10 new events
	When I try to save the new events in the stream
	Then the save process should succeed
	* there should be 20 events in the stream
Examples:
| protocol |
| tcp      |
| grpc     |

Scenario Outline: Concurrent write should fail for outdated session
    Given the <protocol> protocol is being used
	* an existing stream with 10 events
	* session 'Session1' is open
	* session 'Session2' is open
	* 10 new events are added by 'Session1'
	* 10 new events are added by 'Session2'
	When I try to save 'Session1'
	* I try to save 'Session2'
	Then the save process should succeed for 'Session1'
	* the save process should fail for 'Session2' with ConcurrencyException
	* there should be 20 events in the stream
Examples:
| protocol |
| tcp      |
| grpc     |

Scenario Outline: Saving already saved session should throw meaningful usage advice exception
    Given the <protocol> protocol is being used
	* an existing stream with 10 events
	* session 'Session1' is open
	* 10 new events are added by 'Session1'
	When I try to save 'Session1'
	* I try to save 'Session1'
	Then the save process should fail for 'Session1'
	* there should be 20 events in the stream
Examples:
| protocol |
| tcp      |
| grpc     |

Scenario Outline: Write after a hard deleted stream should fail
    Given the <protocol> protocol is being used
	* a new stream
	* 3 new events
	*  I hard-delete the stream
	* 10 new events
	When I try to save the new events in the stream
	Then the save process should fail
Examples:
| protocol |
| tcp      |
| grpc     |

Scenario Outline: Write after a soft deleted stream should succeed
    Given the <protocol> protocol is being used
	* a new stream
	* 3 new events
	*  I soft-delete the stream
	* 10 new events
	When I try to save the new events in the stream
	* I load my entity
	Then the load process should succeed
	* HighOrder property should be 9
Examples:
| protocol |
| tcp      |
| grpc     |

Scenario Outline: When invariants fail no events are saved
    Given the <protocol> protocol is being used
	* an always-fail invariant checker
	* an existing stream with 3 events
	* 5 new events
	When I try to save the new events in the stream
	* I load my entity ignoring any errors
	Then the save process should fail
	* HighOrder property should be 2
Examples:
| protocol |
| tcp      |
| grpc     |

Scenario Outline: When invariants pass events are saved normally using TCP
    Given the tcp protocol is being used
	* an invariant checker that allows a maximum higher order of <maxHighOrder>
	* an existing stream with <existingCount> events
	* <newEventsCount> new events
	When I try to save the new events in the stream
    * I load my entity ignoring any errors
	Then the save process should <outcome>
	* HighOrder property should be <expectedHighOrder>

Examples:
	| existingCount | newEventsCount | maxHighOrder | outcome | expectedHighOrder |
	| 3             | 10             | 5            | fail    | 2                 |
	| 10            | 20             | 20           | succeed | 19                |
	| 1             | 5              | 20           | succeed | 4                 |

Scenario Outline: When invariants pass events are saved normally using GRPC
    Given the grpc protocol is being used
	*  an invariant checker that allows a maximum higher order of <maxHighOrder>
	* an existing stream with <existingCount> events
	* <newEventsCount> new events
	When I try to save the new events in the stream
	* I load my entity ignoring any errors
	Then the save process should <outcome>
	* HighOrder property should be <expectedHighOrder>

Examples:
	| existingCount | newEventsCount | maxHighOrder | outcome | expectedHighOrder |
	| 3             | 10             | 5            | fail    | 2                 |
	| 10            | 20             | 20           | succeed | 19                |
	| 1             | 5              | 20           | succeed | 4                 |
