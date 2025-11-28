using System;
using TimeEntryLegacyAPI.Services;
using Xunit;

namespace TimeEntryLegacyApi.Tests
{
    public class OvertimeCalculatorTests
    {

        [Fact]
        public void CalculateOvertime_France_ReturnsCorrectOvertimeHours()
        {
            // Arrange
            var provider = new TimeEntryLegacyAPI.Models.OvertimePolicyProvider();
            var calculator = new OvertimeCalculator(provider);
            string countryCode = "FR";
            double totalHoursWorked = 50; // Example input

            // Act
            double overtimeHours = calculator.CalculateOvertime(countryCode, totalHoursWorked);

            // Assert
            Assert.Equal(15, overtimeHours); // Assuming standard work week is 35 hours in France
        }
        [Fact]
        public void CalculateOvertime_Germany_ReturnsCorrectOvertimeHours()
        {
            // Arrange
            var provider = new TimeEntryLegacyAPI.Models.OvertimePolicyProvider();
            var calculator = new OvertimeCalculator(provider);
            string countryCode = "DE";
            double totalHoursWorked = 45; // Example input

            // Act
            double overtimeHours = calculator.CalculateOvertime(countryCode, totalHoursWorked);

            // Assert
            Assert.Equal(10, overtimeHours); // Assuming standard work week is 35 hours in Germany
        }
        [Fact]
        public void CalculateOvertime_UnknownCountry_ReturnsZeroOvertimeHours()
        {
            // Arrange
            var provider = new TimeEntryLegacyAPI.Models.OvertimePolicyProvider();
            var calculator = new OvertimeCalculator(provider);
            string countryCode = "XX"; // Unknown country code
            double totalHoursWorked = 40; // Example input

            // Act
            double overtimeHours = calculator.CalculateOvertime(countryCode, totalHoursWorked);

            // Assert
            Assert.Equal(0, overtimeHours); // Assuming unknown country returns zero overtime
        }
        [Fact]
        public void CalculateOvertime_US_ReturnsCorrectOvertimeHours()
        {
            // Arrange
            var provider = new TimeEntryLegacyAPI.Models.OvertimePolicyProvider();
            var calculator = new OvertimeCalculator(provider);
            string countryCode = "US";
            double totalHoursWorked = 50; // Example input

            // Act
            double overtimeHours = calculator.CalculateOvertime(countryCode, totalHoursWorked);

            // Assert
            Assert.Equal(10, overtimeHours); // Assuming standard work week is 40 hours in US
        }
    }
}