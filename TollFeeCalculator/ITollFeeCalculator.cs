using System;
using System.Collections.Generic;
using TollFeeCalculator.Models;

namespace TollFeeCalculator
{
    public interface ITollFeeCalculator
    {
        IEnumerable<TollFee> GetTollFee(Vehicle vehicle, IEnumerable<DateTimeOffset> passes);
        TollFee GetTollFee(Vehicle vehicle, DateTimeOffset pass);
    }
}
