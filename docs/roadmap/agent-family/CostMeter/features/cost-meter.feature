Feature: Record a model cost event

  Scenario: Valid cost event is recorded
    Given an agent named "PaymentBot"
    When the agent records 1500 input tokens, 300 output tokens, and cost 0.0021
    Then the event is accepted
    And the running total for the agent is 0.0021

  Scenario: Multiple events accumulate
    Given an agent has already recorded 0.0021
    When the agent records another event with cost 0.0015
    Then the running total is 0.0036

  Scenario: Negative cost is rejected
    When an agent attempts to record a cost of -0.001
    Then the request is rejected with a validation error

Feature: Query running total

  Scenario: Get total for an agent
    Given several cost events exist for "PaymentBot"
    When a caller requests the total for "PaymentBot"
    Then the correct sum is returned

Feature: x402 on high volume

  Scenario: Free tier accepts event
    Given free volume remains
    When an agent records a cost event without payment
    Then the event is accepted

  Scenario: Payment required after free tier
    Given free volume is exhausted
    When an agent records an event without payment
    Then the server returns 402
    And PAYMENT-REQUIRED header is present
