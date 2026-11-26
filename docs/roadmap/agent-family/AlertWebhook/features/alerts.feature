Feature: Register an alert rule

  Scenario: Valid rule is stored
    Given a rule that watches for failures in service "Payment"
    And a webhook URL
    When the rule is registered
    Then the rule is saved and can be retrieved

  Scenario: Invalid webhook URL is rejected
    When a rule is registered with a malformed webhook URL
    Then the request is rejected

Feature: Fire webhook on matching event

  Scenario: Matching failure triggers delivery
    Given an active rule for failures in "Payment"
    And a test webhook receiver is listening
    When a failure log for "Payment" is ingested
    Then the webhook receives a POST containing the event summary

  Scenario: Non-matching event does not fire
    Given an active rule for failures in "Payment"
    When a success log for "Payment" arrives
    Then no webhook is called

Feature: Paid alert rules

  Scenario: Free tier allows rule creation
    Given free volume remains
    When a rule is registered without payment
    Then the rule is accepted

  Scenario: Payment required
    Given free volume is exhausted
    When a rule is registered without payment
    Then the server returns 402 Payment Required
