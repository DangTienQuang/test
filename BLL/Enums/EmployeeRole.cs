using System.Text.Json.Serialization;

namespace AutoWashPro.BLL.Enums
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum EmployeeRole
    {
        Staff,
        Manager
    }
}
