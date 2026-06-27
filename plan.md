1. **GET Manager Lanes by Date**: In `API/Controllers/ManagerController.cs`, modify `GetLanesInBranch` (currently takes no params) to `GetLanesInBranch([FromQuery] DateTime? date)`. Also in `BLL/Services/ManagerService.cs` update `GetLanesInBranchAsync` to accept `date`, and change its return type to `List<LaneStaffAssignmentDTO>`.
Wait, we need to create `LaneStaffAssignmentDTO`.
```csharp
public class LaneStaffAssignmentDTO
{
    public int LaneId { get; set; }
    public string Name { get; set; } = null!;
    public int BranchId { get; set; }
    public bool IsActive { get; set; }
    public bool IsBusinessLane { get; set; }
    public List<ManagerStaffDTO> AssignedStaff { get; set; } = new();
}
```

2. **Missing IsBusinessLane & Create Business Lane**:
   - In `BLL/DTOs/Booking/ManagerBookingListDTO.cs` and `BLL/DTOs/Business/BusinessBookingResponseDTOs.cs` (`BusinessBookingDetailDTO`), add `public bool IsBusinessLane { get; set; }`.
   - In `BLL/Services/ManagerService.cs` map `IsBusinessLane = b.ProcessingLane != null && b.ProcessingLane.IsBusinessLane` (Wait, the memory says `IsBusinessLane = l.IsBusinessLane` around 248-256... Wait, no, the requirement says "Add `IsBusinessLane = l.IsBusinessLane` mapping in `ManagerService.cs` (around lines 248-256)"). The booking doesn't have `IsBusinessLane`? No, maybe it is returning lanes. Ah! In `ManagerService.cs`, `GetLanesInBranchAsync`, add `IsBusinessLane = l.IsBusinessLane`.
   - In `BLL/DTOs/Lane/CreateLaneDTO.cs`, add `public bool IsBusinessLane { get; set; }`. Update `CreateLaneAsync` to set `IsBusinessLane = request.IsBusinessLane`. And in `LaneDTO.cs` ensure `IsBusinessLane` is returned.

3. **Staff Lane Assignment (Future Dates)**:
   - In `API/Controllers/OperationStaffController.cs`, `GetTodayLaneAssignment` -> Add `[FromQuery] DateTime? date`.
   - Update `IOperationStaffService` and `OperationStaffService.cs` `GetTodayLaneAssignmentAsync(int staffUserId, DateTime? date = null)` to filter by this date instead of `DateTime.UtcNow`.

4. **Admin Authentication for Fleet APIs**:
   - Create `AdminFleetController.cs` under `API/Controllers`.
   - Route `[Route("api/v1/fleet")]`, Authorize `[Authorize(Roles = "Admin")]`.
   - Endpoints:
     - `[HttpGet("staff/pending/all")]` -> `_fleetService.GetAllPendingVehiclesAsync()`
     - `[HttpPost("staff/approve/{id}")]` -> `_fleetService.ApproveFleetVehicleAsync(id)`
     - `[HttpPost("staff/reject/{id}")]` -> `_fleetService.RejectFleetVehicleAsync(id, reason)` (need to accept reason in body perhaps)
   - *Wait*, are these currently located somewhere else and causing conflict? Let me check `AdminVehicleController` or somewhere. Let's do a fast check just in case.

5. **Missing API: Staff Booking Appointments**:
   - In `API/Controllers/OperationStaffController.cs`, update `GetAssignedTasks([FromQuery] DateTime? date)`. Update `GetAssignedBookingsAsync(int staffUserId, DateTime? date = null)` in `OperationStaffService.cs`. Use `date ?? DateTime.UtcNow.Date` instead of `DateTime.UtcNow.Date`.

6. **Shift Swap by Phone Number**:
   - Add `SwapLaneByPhoneDTO` inside `BLL/DTOs/Lane/SwapLaneByPhoneDTO.cs`.
   ```csharp
   public class SwapLaneByPhoneDTO
   {
       public string TargetPhoneNumber { get; set; } = null!;
       public DateTime? Date { get; set; }
   }
   ```
   - In `OperationStaffController`, add `[HttpPost("swap-shift")]` endpoint.
   - Implement logic to find target user by phone number, check active status, and perform swap.
