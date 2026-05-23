## 2024-10-25 - [Rate Limiting for Authentication]
**Vulnerability:** Missing brute force protection on the Login endpoint (`API/Controllers/AuthController.cs`).
**Learning:** By default, ASP.NET Core endpoints are vulnerable to credential stuffing. An `AuthPolicy` using `RateLimitPartition.GetFixedWindowLimiter` should be created explicitly.
**Prevention:** Always define an `AuthPolicy` in `Program.cs` and use the `[EnableRateLimiting("AuthPolicy")]` attribute specifically on high-value authentication endpoints, not necessarily on the whole class to avoid blocking non-auth methods unintentionally.
