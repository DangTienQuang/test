namespace DAL.Entities
{
    public class FleetImportError
    {
        public int FleetImportErrorId { get; set; }

        public int FleetImportBatchId { get; set; }

        public int RowNumber { get; set; }

        public string ErrorMessage { get; set; } = null!;

        public FleetImportBatch FleetImportBatch { get; set; } = null!;
    }
}