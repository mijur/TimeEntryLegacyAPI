using System;

namespace TimeEntryLegacyAPI.Models
{
    public class TimeEntry
    {
        public int Id { get; set; }
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; }
        public string EmployeeType { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public decimal HourlyRate { get; set; }
        public decimal TotalPay { get; set; }
        public string Notes { get; set; }
        public int YearsOfService { get; set; }
        // Optional ISO country code used for overtime policy lookup
        public string? CountryCode { get; set; }
    }
}
