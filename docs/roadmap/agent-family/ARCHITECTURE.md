# Architecture Sketch — QikLog Family

Mono-repo for now. Shared contracts and x402 live once. Services stay independent projects.

## Layout

```
qiklog/                          # existing repo or new root
├── src/
│   ├── QikLog.Core/             # shared domain types, contracts
│   ├── QikLog.Api/              # core logs
│   ├── QikLog.Web/
│   ├── QikLog.Cli/
│   ├── QikLog.Serilog/
│   ├── AgentTraceStore/
│   ├── CostMeter/
│   ├── PiiScrub/
│   ├── QueryProxy/
│   ├── AlertWebhook/
│   └── QikLog.AgentToolkit/     # NuGet
├── tests/                       # Reqnroll + unit/integration
├── shared/
│   └── X402/                    # middleware + facilitator interface
├── docs/
└── docker-compose.yml           # optional multi-service local
```

## Rules

- Services depend on Core or shared/, not on each other.
- Each service is its own .csproj and can run alone.
- Features live next to the service they describe.
- One solution file is fine at this size.
- Extract a service to its own repo later only if it needs independent life or different release cadence.

## Suggested build order

1. Core upgrades (structured properties, TraceId, x402, local serve)
2. Agent Trace Store
3. Cost Meter
4. Agent Toolkit (distribution)
5. Remaining services as demand appears

## x402

One middleware implementation in `shared/X402`. Every paid endpoint uses it. Free tier configurable per service. Facilitator interface so local stub or real facilitator can be swapped.
