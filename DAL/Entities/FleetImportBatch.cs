namespace DAL.Entities
{
    public class FleetImportBatch
    {
        public int FleetImportBatchId { get; set; }

        public int BusinessProfileId { get; set; }

        public string FileUrl { get; set; } = null!;

        public string Status { get; set; } = "Processing";

        public int TotalRows { get; set; }

        public int SuccessRows { get; set; }

        public int FailedRows { get; set; }

        public DateTime CreatedAt { get; set; }

        public BusinessProfile BusinessProfile { get; set; } = null!;
    }
}