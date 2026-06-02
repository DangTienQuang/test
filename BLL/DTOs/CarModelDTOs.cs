namespace AutoWashPro.BLL.DTOs
{
    public class CarModelDTO
    {
        public int Id { get; set; }
        public string Brand { get; set; }
        public string Name { get; set; }
    }

    public class CreateCarModelDTO
    {
        public string Brand { get; set; }
        public string Name { get; set; }
    }

    public class UpdateCarModelDTO
    {
        public string Brand { get; set; }
        public string Name { get; set; }
        public bool IsActive { get; set; }
    }
}
