using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using TimeEntryLegacyAPI.Models;

namespace TimeEntryLegacyAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TimeEntryController : ControllerBase
    {
        private static List<TimeEntry> _timeEntries = new List<TimeEntry>();
        private static int _nextId = 1;

        [HttpGet]
        public IActionResult GetAll()
        {
            return Ok(_timeEntries);
        }

        [HttpGet("{id}")]
        public IActionResult GetById(int id)
        {
            var entry = _timeEntries.FirstOrDefault(e => e.Id == id);
            if (entry == null)
                return NotFound();
            return Ok(entry);
        }

        [HttpPost]
        public IActionResult Create([FromBody] TimeEntry entry)
        {
            entry.Id = _nextId++;
            
            var hours = (entry.EndTime - entry.StartTime).TotalHours;
            var basePay = 0m;
            
            if (entry.EmployeeType == "FTE")
            {
                basePay = (decimal)hours * entry.HourlyRate;
            }
            else if (entry.EmployeeType == "Contractor")
            {
                basePay = (decimal)hours * entry.HourlyRate * 1.2m;
            }
            else if (entry.EmployeeType == "PartTime")
            {
                basePay = (decimal)hours * entry.HourlyRate * 0.95m;
            }

            var bonus = 0m;
            
            // Night shift bonus (10pm - 6am)
            // Use half-open interval [StartTime, EndTime) and include the 22:00 hour (>= 22)
            for (var dt = entry.StartTime; dt < entry.EndTime; dt = dt.AddHours(1))
            {
                if (dt.Hour >= 22 || dt.Hour < 6)
                {
                    bonus += entry.HourlyRate * 0.5m;
                }
            }

            // Weekend bonus
            if (entry.StartTime.DayOfWeek == DayOfWeek.Saturday || 
                entry.StartTime.DayOfWeek == DayOfWeek.Sunday)
            {
                bonus += basePay * 0.5m;
            }

            var holidays = new List<DateTime> 
            { 
                new DateTime(2024, 1, 1),
                new DateTime(2024, 7, 4),
                new DateTime(2024, 12, 25)
            };
            if (holidays.Any(h => h.Date == entry.StartTime.Date))
            {
                bonus += basePay * 1.0m; // Double pay!
            }

            // Loyalty bonus
            if (entry.YearsOfService >= 5)
            {
                bonus += basePay * 0.1m;
            }
            if (entry.YearsOfService >= 10)
            {
                bonus += basePay * 0.05m; // Additional 5%
            }

            if (hours > 40)
            {
                var overtimeHours = hours - 40;
                bonus += (decimal)overtimeHours * entry.HourlyRate * 0.5m;
            }

            entry.TotalPay = basePay + bonus;
            _timeEntries.Add(entry);

            return CreatedAtAction(nameof(GetById), new { id = entry.Id }, entry);
        }

        [HttpPost("calculate")]
        public IActionResult CalculatePay([FromBody] TimeEntry entry)
        {
            var hours = (entry.EndTime - entry.StartTime).TotalHours;
            var basePay = 0m;
            
            if (entry.EmployeeType == "FTE")
            {
                basePay = (decimal)hours * entry.HourlyRate;
            }
            else if (entry.EmployeeType == "Contractor")
            {
                basePay = (decimal)hours * entry.HourlyRate * 1.2m;
            }
            else if (entry.EmployeeType == "PartTime")
            {
                basePay = (decimal)hours * entry.HourlyRate * 0.95m;
            }

            var bonus = 0m;
            var breakdown = "";

            // Night shift
            var nightHours = 0;
            for (var dt = entry.StartTime; dt < entry.EndTime; dt = dt.AddHours(1))
            {
                if (dt.Hour >= 22 || dt.Hour < 6)
                {
                    nightHours++;
                    bonus += entry.HourlyRate * 0.5m;
                }
            }
            if (nightHours > 0)
                breakdown += $"Night shift: {nightHours}h @ 50% = ${nightHours * entry.HourlyRate * 0.5m:F2}\n";

            // Weekend
            if (entry.StartTime.DayOfWeek == DayOfWeek.Saturday || 
                entry.StartTime.DayOfWeek == DayOfWeek.Sunday)
            {
                var weekendBonus = basePay * 0.5m;
                bonus += weekendBonus;
                breakdown += $"Weekend bonus: ${weekendBonus:F2}\n";
            }

            // Holiday
            var holidays = new List<DateTime> 
            { 
                new DateTime(2024, 1, 1),
                new DateTime(2024, 7, 4),
                new DateTime(2024, 12, 25)
            };
            if (holidays.Any(h => h.Date == entry.StartTime.Date))
            {
                var holidayBonus = basePay * 1.0m;
                bonus += holidayBonus;
                breakdown += $"Holiday bonus: ${holidayBonus:F2}\n";
            }

            // Loyalty
            if (entry.YearsOfService >= 5)
            {
                var loyaltyBonus = basePay * 0.1m;
                bonus += loyaltyBonus;
                breakdown += $"Loyalty bonus (5+ years): ${loyaltyBonus:F2}\n";
            }
            if (entry.YearsOfService >= 10)
            {
                var loyaltyBonus = basePay * 0.05m;
                bonus += loyaltyBonus;
                breakdown += $"Loyalty bonus (10+ years): ${loyaltyBonus:F2}\n";
            }

            // Overtime
            if (hours > 40)
            {
                var overtimeHours = hours - 40;
                var overtimeBonus = (decimal)overtimeHours * entry.HourlyRate * 0.5m;
                bonus += overtimeBonus;
                breakdown += $"Overtime: {overtimeHours}h @ 50% = ${overtimeBonus:F2}\n";
            }

            var response = new PayrollResponse
            {
                EmployeeName = entry.EmployeeName,
                BasePay = basePay,
                BonusPay = bonus,
                TotalPay = basePay + bonus,
                Breakdown = breakdown
            };

            return Ok(response);
        }

        [HttpPut("{id}")]
        public IActionResult Update(int id, [FromBody] TimeEntry entry)
        {
            var existing = _timeEntries.FirstOrDefault(e => e.Id == id);
            if (existing == null)
                return NotFound();

            existing.EmployeeName = entry.EmployeeName;
            existing.EmployeeType = entry.EmployeeType;
            existing.StartTime = entry.StartTime;
            existing.EndTime = entry.EndTime;
            existing.HourlyRate = entry.HourlyRate;
            existing.Notes = entry.Notes;
            existing.YearsOfService = entry.YearsOfService;

            var hours = (existing.EndTime - existing.StartTime).TotalHours;
            var basePay = 0m;
            
            if (existing.EmployeeType == "FTE")
            {
                basePay = (decimal)hours * existing.HourlyRate;
            }
            else if (existing.EmployeeType == "Contractor")
            {
                basePay = (decimal)hours * existing.HourlyRate * 1.2m;
            }
            else if (existing.EmployeeType == "PartTime")
            {
                basePay = (decimal)hours * existing.HourlyRate * 0.95m;
            }

            var bonus = 0m;
            for (var dt = existing.StartTime; dt < existing.EndTime; dt = dt.AddHours(1))
            {
                if (dt.Hour >= 22 || dt.Hour < 6)
                {
                    bonus += existing.HourlyRate * 0.5m;
                }
            }

            if (existing.StartTime.DayOfWeek == DayOfWeek.Saturday || 
                existing.StartTime.DayOfWeek == DayOfWeek.Sunday)
            {
                bonus += basePay * 0.5m;
            }

            var holidays = new List<DateTime> 
            { 
                new DateTime(2024, 1, 1),
                new DateTime(2024, 7, 4),
                new DateTime(2024, 12, 25)
            };
            if (holidays.Any(h => h.Date == existing.StartTime.Date))
            {
                bonus += basePay * 1.0m;
            }

            if (existing.YearsOfService >= 5)
            {
                bonus += basePay * 0.1m;
            }
            if (existing.YearsOfService >= 10)
            {
                bonus += basePay * 0.05m;
            }

            if (hours > 40)
            {
                var overtimeHours = hours - 40;
                bonus += (decimal)overtimeHours * existing.HourlyRate * 0.5m;
            }

            existing.TotalPay = basePay + bonus;

            return Ok(existing);
        }

        [HttpDelete("{id}")]
        public IActionResult Delete(int id)
        {
            var entry = _timeEntries.FirstOrDefault(e => e.Id == id);
            if (entry == null)
                return NotFound();

            _timeEntries.Remove(entry);
            return NoContent();
        }

        [HttpGet("report/weekly/{employeeId}")]
        public IActionResult WeeklyReport(int employeeId, [FromQuery] DateTime startDate)
        {
            var endDate = startDate.AddDays(7);
            var entries = _timeEntries
                .Where(e => e.EmployeeId == employeeId && 
                           e.StartTime >= startDate && 
                           e.StartTime < endDate)
                .ToList();

            var totalHours = entries.Sum(e => (e.EndTime - e.StartTime).TotalHours);
            var totalPay = entries.Sum(e => e.TotalPay);

            return Ok(new 
            { 
                EmployeeId = employeeId,
                StartDate = startDate,
                EndDate = endDate,
                TotalHours = totalHours,
                TotalPay = totalPay,
                Entries = entries
            });
        }     
    }
}
