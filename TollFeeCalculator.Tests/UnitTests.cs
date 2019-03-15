using System;
using System.Collections.Generic;
using TollFeeCalculator.Models;
using TollFeeCalculator.Types;
using Xunit;

namespace TollFeeCalculator.Tests
{
    public class UnitTests
    {
        public static IEnumerable<object[]> TestData => 
            new object[][] {
                new object[] { new Vehicle(VehicleType.Motorbike), new DateTime(2017,3,1), 10 }
            };

        [Theory]
        [MemberData(nameof(TestData))]
        public void GetTollFee_DuringStandardHours_ReturnsCorrectFee(Vehicle vehicle, DateTime passTime, int expectedFee)
        {
            var calc = new Calculator();

            var fee = calc.GetTollFee(vehicle, passTime);

            Assert.Equal(expectedFee, fee);
        }
    }
}
