You are building the QikLog .NET Agent Toolkit — a NuGet package that makes it trivial for .NET agents to log runs and pay via x402.

Goal: A small library + sample that a developer can add and immediately have structured logging + optional paid calls without writing payment or logging boilerplate.

## Requirements

1. Library surface:
   - Extension methods or middleware for IChatClient / Microsoft.Extensions.AI
   - Automatic capture of prompt, response, tokens, cost (when available)
   - POST to configured Trace Store / Cost Meter endpoint
   - Optional: wrap any HttpClient so 402 responses are handled (sign payment, retry)

2. Configuration:
   - Endpoint URLs
   - Free-tier vs paid mode
   - Wallet / signing abstraction (interface so local stub or real wallet can be plugged in)

3. Sample:
   - Console app that uses Microsoft.Extensions.AI, logs a run, and demonstrates a 402 retry

4. Testing:
   - Reqnroll features provided
   - Tests use a test server or mock handler
   - Assert that a run is posted and that a 402 is retried with payment header

5. Packaging:
   - Ready for `dotnet pack`
   - Clear README with 5-line getting-started

## Deliverables

- Library project
- Sample
- Features green
- Documented public API

Start with the logging wrapper, then the x402 retry handler, then the sample and feature steps.
