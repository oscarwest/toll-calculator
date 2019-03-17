using System;

namespace TollFeeCalculator
{
    public class TollFee
    {
        public readonly decimal Amount;
        public readonly DateTimeOffset PassTime;

        public TollFee(decimal amount, DateTimeOffset passTime)
        {
            Amount = amount;
            PassTime = passTime;
        }
    }
}
