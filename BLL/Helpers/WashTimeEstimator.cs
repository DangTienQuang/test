using AutoWashPro.DAL.Entities;
using DAL.Entities;

namespace BLL.Helpers
{
    /// <summary>
    /// Derives estimated wash duration purely from existing DB fields.
    /// No new columns needed — uses VehicleType.BaseWeight and ServicePrice.CapacityWeight.
    /// </summary>
    public static class WashTimeEstimator
    {
        private const int InterVehicleBufferMinutes = 2;

        /// <summary>
        /// Estimates wash time in minutes for one vehicle + its selected services.
        /// </summary>
            public static int EstimateMinutes(IEnumerable<ServicePrice> servicePrices)
        {
            // Use EstimatedDurationMinutes directly from ServicePrice
            // Take MAX across selected services — dominant service drives the time
            return servicePrices
                .Select(sp => sp.EstimatedDurationMinutes)
                .DefaultIfEmpty(10)
                .Max();
        }

        public static int GetInterVehicleBuffer() => InterVehicleBufferMinutes;
    }
}