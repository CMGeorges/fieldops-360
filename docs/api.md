# API walkthrough

The demo API uses synthetic identities supplied through headers. Production authentication is deliberately outside this milestone.

## Demo identities

- organization: `11111111-1111-1111-1111-111111111111`
- dispatcher: `22222222-2222-2222-2222-222222222222`
- contractor: `33333333-3333-3333-3333-333333333333`

Each write accepts an `Idempotency-Key` header. Contractor operations also require the demo contractor identifier in `X-Actor-Id`.

## Critical path

```text
POST /api/work-orders/
POST /api/work-orders/{id}/triage
POST /api/work-orders/{id}/offer
POST /api/work-orders/{id}/accept
POST /api/work-orders/{id}/schedule
POST /api/work-orders/{id}/start
POST /api/work-orders/{id}/review
POST /api/work-orders/{id}/close
```

Exception routes include `no-response` and `no-show`. Invalid state transitions return an RFC 9457-style problem response with a stable domain error code.

