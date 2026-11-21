Feature: Structured log properties

  Scenario: Properties bag is stored
    Given a log payload with source "Payment" level "Information" and properties containing orderId 12345
    When the log is posted to /v1/logs
    Then the log is accepted
    And the properties are persisted and can be retrieved

  Scenario: Serilog structured properties are forwarded
    Given a .NET app using the QikLog Serilog sink
    When it writes a structured log with property UserId
    Then the property appears in the stored log entry

Feature: OpenTelemetry correlation

  Scenario: TraceId from Activity is captured
    Given an active System.Diagnostics.Activity with a TraceId
    When a log is written through the sink or API
    Then the stored log contains that TraceId and SpanId

  Scenario: Traceparent header is honored
    Given a request that includes a valid traceparent header
    When a log is ingested
    Then the TraceId from the header is stored

Feature: x402 payment gate

  Scenario: Free tier allows ingest
    Given free volume has not been exceeded
    When a log is posted without payment
    Then the log is accepted

  Scenario: Exceeded free tier returns 402
    Given free volume has been exceeded
    When a log is posted without payment
    Then the response is 402 Payment Required
    And a PAYMENT-REQUIRED header is present

  Scenario: Valid payment allows ingest
    Given free volume has been exceeded
    And a valid x402 payment signature is presented
    When the log is posted
    Then the log is accepted

Feature: Local serve mode

  Scenario: qiklog serve starts without Docker
    Given the CLI is installed
    When the user runs qiklog serve
    Then the API and dashboard start on localhost using SQLite
    And a log can be posted and appears in the tail
