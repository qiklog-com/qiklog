Feature: Scrub personal information

  Scenario: Email is redacted
    Given the text "Contact me at grace@example.com for details"
    When the text is submitted for scrubbing
    Then the redacted text contains "[REDACTED]"
    And the original email is not present
    And findings include type "email"

  Scenario: Phone number is redacted
    Given the text "Call 555-123-4567 tomorrow"
    When the text is submitted for scrubbing
    Then the phone number is replaced

  Scenario: Multiple findings are reported
    Given text that contains both an email and a phone number
    When the text is scrubbed
    Then findings list both types

  Scenario: Clean text is unchanged
    Given text with no personal information
    When the text is scrubbed
    Then the text is returned unchanged
    And findings is empty

Feature: x402 payment for scrubbing

  Scenario: Free tier allows scrub
    Given free volume remains
    When text is submitted without payment
    Then a redacted result is returned

  Scenario: Payment required
    Given free volume is exhausted
    When text is submitted without payment
    Then the server returns 402 Payment Required
