## 2024-05-26 - [Performance Optimization]
**Learning:** `AsNoTracking()` is not used for any Entity Framework Core read-only queries in this application. This causes unnecessary tracking of entities, adding overhead for read-only endpoints and projecting DTOs.
**Action:** Append `.AsNoTracking()` to read-only queries to improve performance, especially when directly mapping to DTOs in service layer methods.
