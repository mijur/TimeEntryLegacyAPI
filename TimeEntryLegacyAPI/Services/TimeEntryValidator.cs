using System;
using TimeEntryLegacyAPI.Models;

namespace TimeEntryLegacyAPI.Services
{
    public class TimeEntryValidator
    {
        public ValidationResult Validate(TimeEntry entry)
        {
            if (entry == null)
            {
                return new ValidationResult { IsValid = false, ErrorMessage = "Entry is null." };
            }

            if (string.IsNullOrWhiteSpace(entry.EmployeeName))
            {
                return new ValidationResult { IsValid = false, ErrorMessage = "Employee name is required." };
            }

            if (string.IsNullOrWhiteSpace(entry.EmployeeType))
            {
                return new ValidationResult { IsValid = false, ErrorMessage = "Employee type is required." };
            }

            if (entry.HourlyRate < 0)
            {
                return new ValidationResult { IsValid = false, ErrorMessage = "Hourly rate cannot be negative." };
            }

            if (entry.YearsOfService < 0)
            {
                return new ValidationResult { IsValid = false, ErrorMessage = "Years of service cannot be negative." };
            }

            if (entry.EndTime < entry.StartTime)
            {
                return new ValidationResult { IsValid = false, ErrorMessage = "End time cannot be before start time." };
            }

            return new ValidationResult { IsValid = true };
        }
    }

    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public string ErrorMessage { get; set; }
    }
}
