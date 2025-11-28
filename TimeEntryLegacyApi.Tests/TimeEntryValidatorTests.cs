using System;
using TimeEntryLegacyAPI.Models;
using TimeEntryLegacyAPI.Services;

namespace TimeEntryLegacyApi.Tests;

public class TimeEntryValidatorTests
{
    private readonly TimeEntryValidator _validator;

    public TimeEntryValidatorTests()
    {
        _validator = new TimeEntryValidator();
    }

    [Fact]
    public void Validate_ValidTimeEntry_ReturnsTrue()
    {
        // Arrange
        var entry = new TimeEntry
        {
            Id = 1,
            EmployeeId = 101,
            EmployeeName = "John Doe",
            EmployeeType = "FTE",
            HourlyRate = 20m,
            StartTime = new DateTime(2024, 6, 1, 9, 0, 0),
            EndTime = new DateTime(2024, 6, 1, 17, 0, 0),
            TotalPay = 160m,
            Notes = "Regular shift",
            YearsOfService = 2
        };

        // Act
        var result = _validator.Validate(entry);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_EmptyEmployeeType_ReturnsFalse()
    {
        // Arrange
        var entry = new TimeEntry
        {
            Id = 2,
            EmployeeId = 102,
            EmployeeName = "Jane Smith",
            EmployeeType = "",
            HourlyRate = 15m,
            StartTime = new DateTime(2024, 6, 1, 9, 0, 0),
            EndTime = new DateTime(2024, 6, 1, 17, 0, 0),
            TotalPay = 120m,
            Notes = "Summer intern",
            YearsOfService = 0
        };

        // Act
        var result = _validator.Validate(entry);

        // Assert
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_EndTimeBeforeStartTime_ReturnsFalse()
    {
        // Arrange
        var entry = new TimeEntry
        {
            Id = 3,
            EmployeeId = 103,
            EmployeeName = "Alice Johnson",
            EmployeeType = "Contractor",
            HourlyRate = 25m,
            StartTime = new DateTime(2024, 6, 1, 18, 0, 0),
            EndTime = new DateTime(2024, 6, 1, 9, 0, 0),
            TotalPay = 0m,
            Notes = "Incorrect times",
            YearsOfService = 1
        };

        // Act
        var result = _validator.Validate(entry);

        // Assert
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_NegativeHourlyRate_ReturnsFalse()
    {
        // Arrange
        var entry = new TimeEntry
        {
            Id = 4,
            EmployeeId = 104,
            EmployeeName = "Bob Brown",
            EmployeeType = "PartTime",
            HourlyRate = -10m,
            StartTime = new DateTime(2024, 6, 1, 9, 0, 0),
            EndTime = new DateTime(2024, 6, 1, 17, 0, 0),
            TotalPay = -80m,
            Notes = "Negative pay",
            YearsOfService = 3
        };

        // Act
        var result = _validator.Validate(entry);

        // Assert
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_EmptyEmployeeName_ReturnsFalse()
    {
        // Arrange
        var entry = new TimeEntry
        {
            Id = 5,
            EmployeeId = 105,
            EmployeeName = "",
            EmployeeType = "FTE",
            HourlyRate = 20m,
            StartTime = new DateTime(2024, 6, 1, 9, 0, 0),
            EndTime = new DateTime(2024, 6, 1, 17, 0, 0),
            TotalPay = 160m,
            Notes = "Missing name",
            YearsOfService = 5
        };

        // Act
        var result = _validator.Validate(entry);

        // Assert
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_NegativeYearsOfService_ReturnsFalse()
    {
        // Arrange
        var entry = new TimeEntry
        {
            Id = 6,
            EmployeeId = 106,
            EmployeeName = "Charlie Davis",
            EmployeeType = "FTE",
            HourlyRate = 30m,
            StartTime = new DateTime(2024, 6, 1, 9, 0, 0),
            EndTime = new DateTime(2024, 6, 1, 17, 0, 0),
            TotalPay = 240m,
            Notes = "Negative years of service",
            YearsOfService = -2
        };

        // Act
        var result = _validator.Validate(entry);

        // Assert
        Assert.False(result.IsValid);
    }
}