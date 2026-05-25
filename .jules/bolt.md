## 2026-05-25 - [Optimize Memory and DB Load with `.AsNoTracking()` for Read-Only Projections]
**Learning:** In Entity Framework Core, queries that map entities directly to DTOs without the intention to update them later will still track those entities in the `DbContext`'s state manager by default. This causes unnecessary memory allocation and performance overhead when fetching lists.
**Action:** Always append `.AsNoTracking()` to read-only queries, especially those projecting into DTOs using `.Select()`, to bypass the change tracker and reduce both memory usage and processing time.
