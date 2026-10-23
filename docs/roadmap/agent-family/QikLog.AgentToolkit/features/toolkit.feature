Feature: Automatic run logging

  Scenario: Agent call is logged to the trace store
    Given the toolkit is configured with a trace store endpoint
    When an agent completes a chat turn
    Then a run is posted containing the prompt and outcome

  Scenario: Tokens and cost are included when available
    Given the model response includes token counts
    When the turn completes
    Then the posted run contains the token counts and calculated cost

Feature: Transparent x402 payment

  Scenario: 402 is detected and retried
    Given the remote endpoint returns 402 with payment requirements
    And a payment signer is configured
    When the toolkit makes the call
    Then it signs a payment
    And it retries the request with the PAYMENT-SIGNATURE header
    And the final call succeeds

  Scenario: No payment signer skips paid endpoints gracefully
    Given no payment signer is configured
    When a 402 is received
    Then the toolkit surfaces a clear error instead of looping
