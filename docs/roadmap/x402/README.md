# x402 (parked, separate product)

> **Status: PARKED.** Planned as its own repo and NuGet package, not code in this repo.

Original note from the packs:

> Place the reusable x402 middleware, payment requirement builder, and facilitator interface here.
> Services reference this project instead of copying the 402 logic.
> Local/dev can use a stub facilitator. Production uses a real one (Base or Solana USDC).

## Split between the library and QikLog

| Generic (x402 repo) | QikLog-specific (this repo) |
| --- | --- |
| 402 middleware, `PAYMENT-REQUIRED` header builder | Free-tier policy: "has this tenant exceeded N events?" |
| `PAYMENT-SIGNATURE` parsing | Usage counters in Postgres |
| `IFacilitator` interface + stub + real facilitator client | Mapping a paid request to a tenant/source |
| Client `DelegatingHandler` (402 → sign → retry) | Prices per endpoint (config) |
| Signer/wallet interface | Wallet address + facilitator keys in Key Vault |

The library must never reference QikLog. QikLog plugs its policy into a hook the library provides, something like `IPaymentPolicy`.

## Before mainnet

- Wallet custody, accounting/tax treatment, refunds. See the "QikPay" note in `docs/MARKETING_NOTES.md`.
- Testnet only until then.
