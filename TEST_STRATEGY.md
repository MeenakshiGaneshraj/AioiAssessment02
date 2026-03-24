# Test Strategy

## Quality Focus
The test suite targets the highest-risk behaviors for this time-boxed task:
- Login correctness and protection of the address checker
- Address validation request handling
- Important dependency failure behavior

## Automated Coverage

### Primary layer: NUnit (`tests/WebApp.Tests`)
- Protected route redirects unauthenticated users to login
- Login invalid credentials messaging
- Login valid credentials redirection
- Address API empty input validation
- Address API partial match response mapping
- Address API failure path returns service unavailable

### Supporting layer: Playwright smoke (`tests/WebApp.E2E`)
- User can log in and reach address checker
- User sees error for invalid login attempt

## Failure Scenario Covered
- Upstream NZ Post failure simulated via test double in NUnit tests to verify 503 behavior.

## Out of Scope (Time-box trade-offs)
- Full cross-browser matrix
- Deep UI visual/accessibility checks
- End-to-end tests against live NZ Post service (kept deterministic and fast)
- Performance/load testing

## Why this approach
Depth on core risk paths gives better release confidence than broad shallow coverage in a 4-5 hour exercise.
