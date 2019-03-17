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
            // TODO: Setup with some kind of options
            _feeSchedule = feeSchedule;
            _maxFee = maxFee;
            _logger = logger;
        }

        /**
        * Calculate the total toll fees given a list of passes
        *
        * @param vehicle - the vehicle
        * @param dates   - date and time of all passes
        * @return - a list of toll fees
        */
        public IEnumerable<TollFee> GetTollFee(Vehicle vehicle, IEnumerable<DateTimeOffset> passes)
        {
            if (vehicle == null) throw new ArgumentNullException(nameof(vehicle));
            if (!passes.Any()) throw new ArgumentNullException(nameof(passes));

            if (IsTollFreeVehicle(vehicle))
                return Enumerable.Empty<TollFee>();


            var nextDebitableTime = DateTimeOffset.MinValue;
            var currentDate = DateTimeOffset.MinValue;
            var tollFees = new List<TollFee>();
            var dailyTotalFee = 0m;

            foreach (var pass in passes.OrderBy(p => p.DateTime))
            {
                // Check if pass is in non-debitable window
                if (pass < nextDebitableTime)
                {
                    tollFees.Add(new TollFee(0m, pass));
                    continue;
                }

                nextDebitableTime = pass.AddHours(1);

                // If we're on a different day, reset
                if (pass.Date != currentDate && pass.Date != DateTimeOffset.MinValue)
                {
                    currentDate = pass.Date;
                    dailyTotalFee = 0m;
                }

                var fee = CalculateTollFee(pass);

                // If we're over max, free pass
                if (dailyTotalFee >= _maxFee)
                {
                    tollFees.Add(new TollFee(0m, pass));
                }
                // If we're going over max, fee up to the daily max
                else if (dailyTotalFee + fee > _maxFee)
                {
                    var maxPassFee = _maxFee - dailyTotalFee;
                    tollFees.Add(new TollFee(maxPassFee, pass));
                }
                else
                {
                    tollFees.Add(new TollFee(fee, pass));
                }

                dailyTotalFee += fee;
            }

            return tollFees;
        }

        /**
        * Calculate the total toll fee for a pass
        *
        * @param vehicle - the vehicle
        * @param date   - date and time of the pass
        * @return - the toll fee for the pass
        */
        public TollFee GetTollFee(Vehicle vehicle, DateTimeOffset pass)
        {
            if (vehicle == null) throw new ArgumentNullException(nameof(vehicle));

            if (IsTollFreeDate(pass) || IsTollFreeVehicle(vehicle))
                return new TollFee(0m, pass);

            var amount = CalculateTollFee(pass);

            return new TollFee(amount, pass);
        }

        private decimal CalculateTollFee(DateTimeOffset pass)
        {
            return _feeSchedule
                .LastOrDefault(x => x.Key <= pass.TimeOfDay)
                .Value;
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
