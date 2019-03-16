using System;
using System.Collections.Generic;
using System.Linq;
using TollFeeCalculator.Models;
using TollFeeCalculator.Types;
using Microsoft.Extensions.Logging;
using Nager.Date;

namespace TollFeeCalculator
{

    public class Calculator : ITollFeeCalculator
    {
        private readonly IDictionary<TimeSpan, decimal> _feeSchedule;
        private readonly decimal _maxFee;
        private readonly ILogger<Calculator> _logger;

        public Calculator(IDictionary<TimeSpan, decimal> feeSchedule, decimal maxFee, ILogger<Calculator> logger)
        {
            _feeSchedule = feeSchedule;
            _maxFee = maxFee;
            _logger = logger;
        }

        /**
        * Calculate the total toll fee for one day given a list of DateTimeOffset's
        *
        * @param vehicle - the vehicle
        * @param dates   - date and time of all passes on one day
        * @return - the total toll fee for that day
        */
        public decimal GetTollFeesForOneDay(Vehicle vehicle, IEnumerable<DateTimeOffset> passes)
        {
            if (vehicle == null) throw new ArgumentNullException(nameof(vehicle));
            if (!passes.Any()) throw new ArgumentNullException(nameof(passes));
            if (passes.GroupBy(x => x.Date).Count() > 1) throw new ArgumentOutOfRangeException("All passes need to be on the same day");

            if (IsTollFreeVehicle(vehicle))
                return 0m;

            var totalFee = passes
                .Sum(date => {
                    return GetTollFee(vehicle, date);
                });

            if (totalFee > 60m)
            {
                _logger.LogInformation($"Vehicle {vehicle.RegistrationNumber} hit toll fee limit on {passes.First()}");
                return 60m;
            }

            return totalFee;
        }

        /**
        * Calculate the total toll fee for a given DateTimeOffset
        *
        * @param vehicle - the vehicle
        * @param date   - date and time of the pass
        * @return - the toll fee for the pass
        */
        public decimal GetTollFee(Vehicle vehicle, DateTimeOffset pass)
        {
            if (vehicle == null) throw new ArgumentNullException(nameof(vehicle));

            if (IsTollFreeDate(pass) || IsTollFreeVehicle(vehicle))
                return 0m;

            var result = _feeSchedule
                .LastOrDefault(x => x.Key <= pass.TimeOfDay)
                .Value;

            return result;
        }

        private bool IsTollFreeVehicle(Vehicle vehicle)
        {
            switch (vehicle.VehicleType)
            {
                case VehicleType.Car:
                case VehicleType.Motorbike:
                case VehicleType.Tractor:
                    return false;
                case VehicleType.Emergency:
                case VehicleType.Diplomat:
                case VehicleType.Foreign:
                case VehicleType.Military:
                    return true;
                default:
                    _logger.LogError($"Unhandled VehicleType {nameof(vehicle.VehicleType)} in IsTollFreeVehicle Check");
                    break;
            }

            return true;
        }

        private bool IsTollFreeDate(DateTimeOffset date)
        {
            var dateTime = date.UtcDateTime;

            return DateSystem.IsPublicHoliday(dateTime, CountryCode.SE) || DateSystem.IsWeekend(dateTime, CountryCode.SE);
        }
    }
}
