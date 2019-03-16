using TollFeeCalculator.Types;

namespace TollFeeCalculator.Models
{
    public class Vehicle
    {
        public readonly VehicleType VehicleType;
        public readonly string RegistrationNumber;

        public Vehicle(VehicleType vehicleType, string registrationNumber)
        {
            VehicleType = vehicleType;
            RegistrationNumber = registrationNumber;
        }
    }
}
