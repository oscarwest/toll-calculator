using System;
using System.Collections.Generic;
using System.Linq;
using FakeItEasy;
using Microsoft.Extensions.Logging;
using TimeZoneConverter;
using TollFeeCalculator.Models;
using TollFeeCalculator.Types;
using Xunit;

namespace TollFeeCalculator.Tests
{
    public class CalculatorTests
    {
        private static TimeSpan TimeZoneOffset = TZConvert.GetTimeZoneInfo("W. Europe Standard Time").BaseUtcOffset;
        private const decimal MAX_FEE = 60m;
        private IDictionary<TimeSpan, decimal> _feeSchedule => new Dictionary<TimeSpan, decimal>
            {
                { new TimeSpan(06, 00, 0), 8m },
                { new TimeSpan(06, 30, 0), 13m },
                { new TimeSpan(07, 00, 0), 18m }, // "Rush Hour"
                { new TimeSpan(08, 00, 0), 13m },
                { new TimeSpan(08, 30, 0), 8m },
                { new TimeSpan(15, 00, 0), 13m },
                { new TimeSpan(15, 30, 0), 18m }, // "Rush Hour"
                { new TimeSpan(17, 00, 0), 13m },
                { new TimeSpan(18, 00, 0), 8m },
                { new TimeSpan(18, 30, 0), 0m }
            };

        /**
        *** GetTollFee single passes
        **/

        // Needs to be public because of xUnit
        public static IEnumerable<object[]> SinglePassRegularDayNonFreeVehicleData => 
            new object[][] {
                new object[] { new Vehicle(VehicleType.Motorbike, "ABC123"), new DateTimeOffset(new DateTime(2019, 3, 13, 5, 59, 0), TimeZoneOffset), 0m },
                new object[] { new Vehicle(VehicleType.Motorbike, "ABC123"), new DateTimeOffset(new DateTime(2019, 3, 13, 6, 0, 0), TimeZoneOffset), 8m },
                new object[] { new Vehicle(VehicleType.Motorbike, "ABC123"), new DateTimeOffset(new DateTime(2019, 3, 13, 6, 5, 0), TimeZoneOffset), 8m },
                new object[] { new Vehicle(VehicleType.Motorbike, "ABC123"), new DateTimeOffset(new DateTime(2019, 3, 13, 6, 30, 0), TimeZoneOffset), 13m },
                new object[] { new Vehicle(VehicleType.Motorbike, "ABC123"), new DateTimeOffset(new DateTime(2019, 3, 13, 7, 0, 0), TimeZoneOffset), 18m },
                new object[] { new Vehicle(VehicleType.Motorbike, "ABC123"), new DateTimeOffset(new DateTime(2019, 3, 13, 8, 0, 0), TimeZoneOffset), 13m },
                new object[] { new Vehicle(VehicleType.Motorbike, "ABC123"), new DateTimeOffset(new DateTime(2019, 3, 13, 8, 30, 0), TimeZoneOffset), 8m },
                new object[] { new Vehicle(VehicleType.Motorbike, "ABC123"), new DateTimeOffset(new DateTime(2019, 3, 13, 15, 0, 0), TimeZoneOffset), 13m },
                new object[] { new Vehicle(VehicleType.Motorbike, "ABC123"), new DateTimeOffset(new DateTime(2019, 3, 13, 15, 30, 0), TimeZoneOffset), 18m },
                new object[] { new Vehicle(VehicleType.Motorbike, "ABC123"), new DateTimeOffset(new DateTime(2019, 3, 13, 17, 0, 0), TimeZoneOffset), 13m },
                new object[] { new Vehicle(VehicleType.Motorbike, "ABC123"), new DateTimeOffset(new DateTime(2019, 3, 13, 18, 0, 0), TimeZoneOffset), 8m },
                new object[] { new Vehicle(VehicleType.Motorbike, "ABC123"), new DateTimeOffset(new DateTime(2019, 3, 13, 18, 30, 0), TimeZoneOffset), 0m },
                new object[] { new Vehicle(VehicleType.Motorbike, "ABC123"), new DateTimeOffset(new DateTime(2019, 3, 13, 0, 0, 0), TimeZoneOffset), 0m }
            };
        
        [Theory]
        [MemberData(nameof(SinglePassRegularDayNonFreeVehicleData))]
        public void GetTollFeeForSinglePass_RegularDay_ReturnsCorrectFee(Vehicle vehicle,  DateTimeOffset passTime, decimal expectedFee)
        {
            // Arrange
            var calc = new Calculator(_feeSchedule, MAX_FEE, A.Fake<ILogger<Calculator>>());

            // Act
            var fee = calc.GetTollFee(vehicle, passTime);

            // Assert
            Assert.Equal(expectedFee, fee.Amount);
        }
        
        [Fact]
        public void GetTollFeeForSinglePass_NullVehicle_ThrowsException()
        {
            var calc = new Calculator(_feeSchedule, MAX_FEE, A.Fake<ILogger<Calculator>>());

            Action act = () => calc.GetTollFee(null, new DateTimeOffset(new DateTime(2019, 3, 13, 0, 0, 0), TimeZoneOffset));

            Assert.Throws<ArgumentNullException>(act);
        }

        [Theory]
        [InlineData(VehicleType.Car, 8)]
        [InlineData(VehicleType.Tractor, 8)]
        [InlineData(VehicleType.Motorbike, 8)]
        [InlineData(VehicleType.Diplomat, 0)]
        [InlineData(VehicleType.Emergency, 0)]
        [InlineData(VehicleType.Foreign, 0)]
        [InlineData(VehicleType.Military, 0)]
        public void GetTollFeeForSinglePass_FreeVehicleType_ReturnsCorrectFee(VehicleType vehicleType, decimal expectedFee)
        {
            var calc = new Calculator(_feeSchedule, MAX_FEE, A.Fake<ILogger<Calculator>>());
            var vehicle = new Vehicle(vehicleType, "ABC123");

            var fee = calc.GetTollFee(vehicle, new DateTimeOffset(new DateTime(2019, 3, 13, 6, 0, 0), TimeZoneOffset));

            Assert.Equal(expectedFee, fee.Amount);
        }

        /**
        *** GetTollFee multiple passes same day
        **/

        // Needs to be public because of xUnit
        public static IEnumerable<object[]> MultiplePassesRegularDayNonFreeVehicleData => 
            new object[][] {
                // One pass
                new object[] { 
                    new Vehicle(VehicleType.Car, "ABC123"),
                    new [] {
                        new DateTimeOffset(new DateTime(2019, 3, 13, 6, 0, 0), TimeZoneOffset)
                    },
                    8m
                },
                // Two passes
                new object[] {
                    new Vehicle(VehicleType.Car, "ABC123"),
                    new [] {
                        new DateTimeOffset(new DateTime(2019, 3, 13, 6, 0, 0), TimeZoneOffset),
                        new DateTimeOffset(new DateTime(2019, 3, 13, 15, 0, 0), TimeZoneOffset)
                    },
                    21m
                },
                // Two passes within 1 hour, expect only first charge
                new object[] {
                    new Vehicle(VehicleType.Car, "ABC123"),
                    new [] {
                        new DateTimeOffset(new DateTime(2019, 3, 13, 6, 0, 0), TimeZoneOffset),
                        new DateTimeOffset(new DateTime(2019, 3, 13, 6, 59, 0), TimeZoneOffset)
                    },
                    8m
                },
                // Five passes, none within 1 hour of eachother, totaling 70, expect the max 60m
                new object[] {
                    new Vehicle(VehicleType.Car, "ABC123"),
                    new [] {
                        new DateTimeOffset(new DateTime(2019, 3, 13, 6, 0, 0), TimeZoneOffset),     // 8
                        new DateTimeOffset(new DateTime(2019, 3, 13, 7, 1, 0), TimeZoneOffset),     // 18
                        new DateTimeOffset(new DateTime(2019, 3, 13, 8, 2, 0), TimeZoneOffset),     // 13
                        new DateTimeOffset(new DateTime(2019, 3, 13, 15, 0, 0), TimeZoneOffset),    // 13
                        new DateTimeOffset(new DateTime(2019, 3, 13, 16, 1, 0), TimeZoneOffset)     // 18
                    },
                    60m
                },
            };

        [Theory]
        [MemberData(nameof(MultiplePassesRegularDayNonFreeVehicleData))]
        public void GetTollFeesForMultiplePasses_RegularDay_ReturnsCorrectFee(Vehicle vehicle,  IEnumerable<DateTimeOffset> passTimes, decimal expectedFee)
        {
            var calc = new Calculator(_feeSchedule, MAX_FEE, A.Fake<ILogger<Calculator>>());

            var fees = calc.GetTollFee(vehicle, passTimes);

            Assert.Equal(expectedFee, fees.Sum(p => p.Amount));
        }

        [Fact]
        public void GetTollFeesForMultiplePasses_NullVehicle_ThrowsException()
        {
            var calc = new Calculator(_feeSchedule, MAX_FEE, A.Fake<ILogger<Calculator>>());

            Action act = () => calc.GetTollFee(null, new[] { new DateTimeOffset(new DateTime(2019, 3, 13, 0, 0, 0), TimeZoneOffset) });

            Assert.Throws<ArgumentNullException>(act);
        }

        [Fact]
        public void GetTollFeesForMultiplePasses_NoPasses_ThrowsException()
        {
            var calc = new Calculator(_feeSchedule, MAX_FEE, A.Fake<ILogger<Calculator>>());
            var passes = Enumerable.Empty<DateTimeOffset>();

            Action act = () => calc.GetTollFee(new Vehicle(VehicleType.Car, "ABC123"), passes);

            Assert.Throws<ArgumentNullException>(act);
        }

        /**
        *** GetTollFee multiple passes same day
        **/    

        // Needs to be public because of xUnit
        public static IEnumerable<object[]> MultiplePassesMultipleRegularDaysNonFreeVehicleData => 
            new object[][] {
                // Multiple passes, none within 1 hour of eachother
                new object[] {
                    new Vehicle(VehicleType.Car, "ABC123"),
                    new [] {
                        new DateTimeOffset(new DateTime(2019, 3, 13, 6, 0, 0), TimeZoneOffset),     // 8
                        new DateTimeOffset(new DateTime(2019, 3, 13, 7, 1, 0), TimeZoneOffset),     // 18
                        new DateTimeOffset(new DateTime(2019, 3, 13, 8, 2, 0), TimeZoneOffset),     // 13
                        new DateTimeOffset(new DateTime(2019, 3, 13, 15, 0, 0), TimeZoneOffset),    // 13
                        new DateTimeOffset(new DateTime(2019, 3, 13, 16, 1, 0), TimeZoneOffset),     // 18
                        new DateTimeOffset(new DateTime(2019, 3, 14, 6, 0, 0), TimeZoneOffset)     // 18
                    },
                    68m
                },
                // Multiple passes, some within 1 hour of eachother
                new object[] {
                    new Vehicle(VehicleType.Car, "ABC123"),
                    new [] {
                        new DateTimeOffset(new DateTime(2019, 3, 13, 6, 0, 0), TimeZoneOffset),     // 8
                        new DateTimeOffset(new DateTime(2019, 3, 13, 6, 30, 0), TimeZoneOffset),     // 18
                        new DateTimeOffset(new DateTime(2019, 3, 14, 6, 0, 0), TimeZoneOffset),     // 18
                        new DateTimeOffset(new DateTime(2019, 3, 14, 6, 30, 0), TimeZoneOffset),     // 18
                    },
                    16m
                }
            };

        [Theory]
        [MemberData(nameof(MultiplePassesMultipleRegularDaysNonFreeVehicleData))]
        public void GetTollFeesForMultiplePasses_MultipleDays_ReturnsCorrectFee(Vehicle vehicle,  IEnumerable<DateTimeOffset> passTimes, decimal expectedFee)
        {
            var calc = new Calculator(_feeSchedule, MAX_FEE, A.Fake<ILogger<Calculator>>());

            var fees = calc.GetTollFee(vehicle, passTimes);

            Assert.Equal(expectedFee, fees.Sum(p => p.Amount));
        }

        /**
        *** Free dates tests
        **/        

        // Needs to be public because of xUnit
        public static IEnumerable<object[]> SinglePassWeekendsAndHolidaysNonFreeVehicleData => 
            new object[][] {
                new object[] { new Vehicle(VehicleType.Motorbike, "ABC123"), new DateTimeOffset(new DateTime(2019, 3, 18, 6, 0, 0), TimeZoneOffset), 8m }, // Mon
                new object[] { new Vehicle(VehicleType.Motorbike, "ABC123"), new DateTimeOffset(new DateTime(2019, 3, 16, 5, 59, 0), TimeZoneOffset), 0m }, // Sat
                new object[] { new Vehicle(VehicleType.Motorbike, "ABC123"), new DateTimeOffset(new DateTime(2019, 3, 17, 6, 0, 0), TimeZoneOffset), 0m }, // Sun
                new object[] { new Vehicle(VehicleType.Motorbike, "ABC123"), new DateTimeOffset(new DateTime(2019, 12, 24, 6, 0, 0), TimeZoneOffset), 0m }, // Xmas
            };

        [Theory]
        [MemberData(nameof(SinglePassWeekendsAndHolidaysNonFreeVehicleData))]
        public void GetTollFeeForSinglePass_WeekendOrHoliday_ReturnsCorrectFee(Vehicle vehicle,  DateTimeOffset passTime, decimal expectedFee)
        {
            var calc = new Calculator(_feeSchedule, MAX_FEE, A.Fake<ILogger<Calculator>>());

            var fee = calc.GetTollFee(vehicle, passTime);

            Assert.Equal(expectedFee, fee.Amount);
        }

    }
}
