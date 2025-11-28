using System;
using TimeEntryLegacyAPI.Models;
using TimeEntryLegacyAPI.Services;

namespace TimeEntryLegacyApi.Tests;

public class PayrollCalculatorTests
{
    private readonly PayrollCalculator _payrollCalculator;

    public PayrollCalculatorTests()
    {
        _payrollCalculator = new PayrollCalculator();
    }
    [Fact]
    public void CalculateBasePay_FTE_ReturnsCorrectBasePay()
    {
        // Arrange
        var entry = new TimeEntry
        {
            EmployeeType = "FTE",
            HourlyRate = 20m
        };
        double hours = 40.0;

        // Act
        var basePay = _payrollCalculator.CalculateBasePay(entry, hours);

        // Assert
        Assert.Equal(800m, basePay);
    }

    [Fact]
    public void CalculateBasePay_Contractor_ReturnsCorrectBasePay()
    {
        // Arrange
        var entry = new TimeEntry
        {
            EmployeeType = "Contractor",
            HourlyRate = 30m
        };
        double hours = 40.0;

        // Act
        var basePay = _payrollCalculator.CalculateBasePay(entry, hours);

        // Assert
        Assert.Equal(1440m, basePay); // 30 * 1.2 * 40
    }

    [Fact]
    public void CalculateBasePay_PartTime_ReturnsCorrectBasePay()
    {
        // Arrange
        var entry = new TimeEntry
        {
            EmployeeType = "PartTime",
            HourlyRate = 15m
        };
        double hours = 20.0;

        // Act
        var basePay = _payrollCalculator.CalculateBasePay(entry, hours);

        // Assert
        Assert.Equal(285m, basePay); // 15 * 0.95 * 20
    }

    [Fact]
    public void CalculateBasePay_UnknownEmployeeType_ReturnsZero()
    {
        // Arrange
        var entry = new TimeEntry
        {
            EmployeeType = "Intern",
            HourlyRate = 10m
        };
        double hours = 10.0;

        // Act
        var basePay = _payrollCalculator.CalculateBasePay(entry, hours);

        // Assert
        Assert.Equal(0m, basePay);
    }

}
