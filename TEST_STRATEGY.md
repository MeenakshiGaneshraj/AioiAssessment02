# Test Strategy

## Quality Focus
The test suite targets the highest-risk behaviors for this time-boxed task:
- Login correctness and protection of the address checker
- Address validation request handling (valid, partial, invalid, empty)
- Important dependency failure behavior (unavailable, timeout)
- Address suggestion selection UX flow

## Automated Coverage

### Primary layer: NUnit (`tests/WebApp.Tests`)
- Protected route redirects unauthenticated users to login
- Login invalid credentials messaging
- Login valid credentials redirection
- Login with empty username shows validation message
- Login with empty password shows validation message
- Login with both fields empty shows validation message
- Address API returns unauthorized without login
- Address API empty input validation
- Address API missing query parameter validation
- Address API valid/exact match response
- Address API partial match response mapping
- Address API no match for invalid address
- Address API failure path returns 503 (service unavailable)
- Address API timeout path returns 504 (gateway timeout)

### Supporting layer: Playwright smoke (`tests/WebApp.E2E`)
- User can log in and reach address checker
- User sees error for invalid login attempt
- Login fails with empty fields
- Address checker redirects to login when not authenticated
- Address suggestions appear when typing a partial address
- Selecting a suggestion populates the input and triggers validation
- Status message appears when address is entered

## Failure Scenarios Covered
- Upstream NZ Post failure simulated via test double in NUnit tests to verify 503 behavior.
- Upstream NZ Post timeout simulated via test double in NUnit tests to verify 504 behavior.

## Out of Scope (Time-box trade-offs)
- Full cross-browser matrix
- Deep UI visual/accessibility checks
- End-to-end tests against live NZ Post service (kept deterministic and fast)
- Performance/load testing

## Why this approach
Depth on core risk paths gives better release confidence than broad shallow coverage in a 4-5 hour exercise.
