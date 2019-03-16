using System;
using System.Collections.Generic;
using TollFeeCalculator.Models;

namespace TollFeeCalculator
{
    public interface ITollFeeCalculator
    {
        decimal GetTollFeesForOneDay(Vehicle vehicle, IEnumerable<DateTimeOffset> passes);
        decimal GetTollFee(Vehicle vehicle, DateTimeOffset pass);
    }
}
