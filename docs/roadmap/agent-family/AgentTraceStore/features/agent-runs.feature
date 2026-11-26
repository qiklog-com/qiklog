Feature: Ingest an agent run

  As a .NET AI agent
  I want to record a complete run
  So that humans and other agents can debug and measure it later

  Scenario: Successful run is stored
    Given a valid agent run payload for agent "PaymentBot" in session "sess-123"
    And the run has outcome "success" and cost 0.0042
    When the agent posts the run to the trace store
    Then the store accepts the run
    And the run can be retrieved by its id
    And the run appears in queries for agent "PaymentBot"

  Scenario: Run with tool calls is stored completely
    Given a run that contains two tool calls
    When the agent posts the run
    Then both tool calls are persisted with their arguments and results

  Scenario: Invalid outcome is rejected
    Given a run with outcome "unknown-value"
    When the agent posts the run
    Then the store rejects the request with a validation error

Feature: Query agent runs

  Scenario: Filter by agent name
    Given several runs exist for different agents
    When a caller queries runs for agent "PaymentBot"
    Then only runs for that agent are returned

  Scenario: Filter by session and trace
    Given a run with session "sess-123" and trace "trace-abc"
    When a caller queries by that session or trace
    Then the matching run is returned

Feature: Live notification of new runs

  Scenario: Connected clients receive new runs
    Given a client is subscribed to the live runs hub
    When a new run is ingested
    Then the client receives the run summary

Feature: x402 payment for high volume

  Scenario: Free tier allows ingest
    Given the free volume has not been exceeded
    When an agent posts a run without payment
    Then the run is accepted

  Scenario: Exceeded free tier requires payment
    Given the free volume has been exceeded
    When an agent posts a run without payment
    Then the server returns 402 Payment Required
    And the response contains a PAYMENT-REQUIRED header

  Scenario: Valid payment allows ingest after 402
    Given the free volume has been exceeded
    And the agent presents a valid x402 payment signature
    When the agent posts the run
    Then the run is accepted
