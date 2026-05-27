## 2024-05-18 - [Avoid full entity fetching and O(N*M) aggregation loops in APIs]
**Learning:** In endpoints that aggregate counts (e.g. checking available slots against booked appointments), fetching full entities to count them in a nested loop creates O(n*m) complexity and severe memory bloat. This is particularly noticeable in EF Core without `AsNoTracking()`.
**Action:** Always project only the required fields (e.g. `Select(x => x.Time)`) and pre-aggregate counts into an O(1) dictionary mapping in memory (or database-side) before the checking loop.
