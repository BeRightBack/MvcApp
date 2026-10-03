# Payment gate — configuration reference

MvcApp ships payments as a platform capability. Checkout code resolves an `IPaymentGateway` by
**rail** and never knows which rail it got, so a template or a deployment changes its payment options
by configuration, not by editing module code.

Nothing below is required for the app to boot: with no configuration the gate offers whatever rails
are registered (PayPal) and refuses anything else loudly.

## Where the code lives

| What | Where |
|---|---|
| Rail contracts (`IPaymentGateway`, `PaymentVerification`, `PaymentProviderKind`) | `MvcApp.Common/Payments/PaymentGateway.cs` |
| PayPal rail | `MvcApp.Common/Payments/PayPalGateway.cs` |
| Offline rail (bank transfer / Interac / PayPal.Me / manual) | `MvcApp.Common/Payments/ManualPaymentGateway.cs` |
| Registration + resolver | `MvcApp.Common/Payments/PaymentGatewayServiceCollectionExtensions.cs` |
| Sealed checkout binding (binds a payment to what was quoted) | `MvcApp.Common/Payments/PaymentIntentProtector.cs` |
| Who activates an unverifiable payment | `MvcApp.Common/Payments/ActivationPolicy.cs` |

## Rails available today

| Rail (`PaymentProviderKind`) | Kind of rail | Needs at setup |
|---|---|---|
| `PayPal` | Hosted approval + capture | `PayPal:ClientId`, `PayPal:ClientSecret` — a PayPal business account |
| `Interac` | Offline (payer sends, a human confirms) | A receiving e-transfer address |
| `PayPalMe` | Offline (payer sends via link, a human confirms) | A PayPal.Me link |
| `BankTransfer` | Offline | Bank details, or free text |
| `Manual` | Offline, generic | Anything you want printed in the instructions |
| `Card` | **Not implemented** | See "Adding a card rail" |

There is deliberately **no** card rail yet. It needs a processor account *and* that processor's API
integration; registering one without it would offer a checkout option that cannot work. The gate
therefore does not advertise `Card`.

## Configuration

```jsonc
{
  "Payments": {
    // Who activates a payment that no machine can verify (bank transfer, Interac, PayPal.Me).
    "Activation": {
      "Mode": "AdminConfirmed",        // AdminConfirmed (default) | AutomaticOnClaim | AutomaticBelowThreshold
      "AutoActivateBelowAmount": 0     // only read by AutomaticBelowThreshold; 0 disables the fast path
    },

    // One entry per offline rail you want to offer. The key must be a PaymentProviderKind name.
    "Manual": {
      "Rails": {
        "Interac": {
          "Recipient": "payments@example.com",
          "ReferencePrefix": "IPTV-",
          "InstructionsTemplate": "Send {amount} {currency} to {recipient} and include the reference {reference} in the message."
        },
        "PayPalMe": {
          "Recipient": "https://paypal.me/yourname",
          "ReferencePrefix": "IPTV-"
        }
      }
    }
  },

  // Used by the PayPal rail. Also read by the IPTV module's currency conversion.
  "PayPal": {
    "ClientId": "",
    "ClientSecret": ""
  }
}
```

### Activation modes

| Mode | Behaviour | Trade-off |
|---|---|---|
| `AdminConfirmed` *(default)* | A claim only emails `AdminEmail`; an administrator confirms the deposit and activates. | Slowest, nothing is granted on a claim alone. |
| `AutomaticOnClaim` | The customer's own claim activates and issues credentials. | Fastest, verifies nothing. |
| `AutomaticBelowThreshold` | Claims below `AutoActivateBelowAmount` auto-activate; larger ones wait for a human. | A middle path for small amounts. A threshold of `0` activates **nothing** — misconfiguration fails closed. |

A newly created template gets `AdminConfirmed`, so it never inherits the self-activation behaviour.

### Instruction placeholders

`{amount}`, `{currency}`, `{reference}`, `{recipient}`. Keep the **reference** visible — it is what
lets an administrator match a deposit to an order. The reference is
`{ReferencePrefix}{the app's own order/subscription id}`.

## Adding a card rail (when you have the account)

One class, registered alongside the existing rails. Nothing else changes — Store, VIP and IPTV
already resolve through `IPaymentGateway`.

1. Implement `IPaymentGateway` with `Kind => PaymentProviderKind.Card`. Return a `RedirectUrl` (or
   `Instructions`) from `InitiateAsync`, and from `CaptureAsync` return a `PaymentVerification`
   carrying the **captured amount and currency**.
2. Register it in `PaymentGatewayServiceCollectionExtensions.AddPaymentGateways`.
3. Add the provider's keys to configuration.

Rules that apply to every rail, learned the hard way here:

- **Never report `Completed` on your own authority.** Completion comes from the provider (a verified
  webhook or an authenticated server-side call) or, for offline rails, from an explicit human step.
- **Always return the captured amount and currency** so callers can compare them. A status alone
  cannot tell a $5 payment from a $500 one.
- Callers must gate grants on `PaymentVerification.Matches(expectedAmount, expectedCurrency)` and
  never on `Status` alone.

### Webhooks

There is currently **no webhook or signature-verification layer anywhere in the app** — every
confirmation rides a browser redirect. That is acceptable for hosted-approval rails where the return
is confirmed server-side by a capture call (PayPal, as implemented), but a card processor expects
webhooks as the source of truth for subscription state. The persistent idempotency ledger a webhook
layer needs is **not built yet**; it requires a small schema addition.

## What the platform guarantees

- Resolving a rail that is not enabled **throws**, naming the rails that are and where to configure
  one — it never quietly falls back to a different rail.
- An unrecognised name under `Payments:Manual:Rails` is a **configuration error** that throws at
  startup rather than being skipped, because a silently dropped rail surfaces as a customer who
  cannot pay.
- `Matches()` refuses a non-positive expected amount or a blank expected currency, so an empty
  capture can never satisfy a forgotten amount.
