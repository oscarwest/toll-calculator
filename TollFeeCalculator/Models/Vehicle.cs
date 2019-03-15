using TollFeeCalculator.Types;

namespace TollFeeCalculator.Models
{
    public class Vehicle
    {
        public readonly VehicleType VehicleType;

        public Vehicle(VehicleType vehicleType)
        {
            VehicleType = vehicleType;
        }
    }
}
