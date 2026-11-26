Feature: Structured query over logs and runs

  Scenario: Filter failures by service in a time window
    Given logs exist for service "Payment" with some failures in the last hour
    When a caller queries for failures of "Payment" in the last hour
    Then only matching failure events are returned

  Scenario: Filter by agent and outcome
    Given agent runs exist for "PaymentBot" with mixed outcomes
    When a caller queries runs for agent "PaymentBot" with outcome "failure"
    Then only failing runs are returned

  Scenario: Text contains filter
    Given a log entry whose message contains "timeout"
    When a caller queries for text "timeout"
    Then that entry is included

  Scenario: Empty result is valid
    Given no matching events
    When a query is executed
    Then an empty list is returned
    And the response is still successful

Feature: Paid queries

  Scenario: Free tier allows a query
    Given free query volume remains
    When a query is submitted without payment
    Then results are returned

  Scenario: Payment required for additional queries
    Given free volume is exhausted
    When a query is submitted without payment
    Then the server returns 402 Payment Required
