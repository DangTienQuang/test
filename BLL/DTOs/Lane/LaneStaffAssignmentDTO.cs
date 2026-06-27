using System.Collections.Generic;

namespace AutoWashPro.BLL.DTOs
{
    public class LaneStaffAssignmentDTO
    {
        public int LaneId { get; set; }
        public string LaneName { get; set; } = null!;
        public bool IsBusinessLane { get; set; }
        public List<ManagerStaffDTO> AssignedStaff { get; set; } = new();
    }
}
