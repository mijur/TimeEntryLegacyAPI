using System.Collections.Generic;
using TimeEntryLegacyAPI.Models;

namespace TimeEntryLegacyAPI.Services
{
    public interface IOvertimeCalculator
    {
        // Calculates overtime hours based on country code
        double CalculateOvertime(string countryCode, double totalHoursWorked);
        // Optionally, add more methods as needed
    }
}
