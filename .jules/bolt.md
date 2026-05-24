## 2024-05-24 - Entity Framework Read-Only Overhead
**Learning:** Returning domain entities directly from EF Core to map to DTOs in a separate step triggers the EF change tracker by default. For `GetAll` or read-only summary queries, this causes unnecessary memory allocation and CPU overhead to snapshot entities that will never be updated.
**Action:** Always append `.AsNoTracking()` to EF Core queries that fetch data purely for DTO projection or read-only display purposes.
