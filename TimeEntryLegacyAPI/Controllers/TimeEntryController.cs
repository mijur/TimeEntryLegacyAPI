using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using TimeEntryLegacyAPI.Models;
using TimeEntryLegacyAPI.Services;

namespace TimeEntryLegacyAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TimeEntryController : ControllerBase
    {
        private readonly PayrollCalculator _payrollCalculator;
        private readonly TimeEntryValidator _validator;

        public TimeEntryController(PayrollCalculator payrollCalculator, TimeEntryValidator validator)
        {
            _payrollCalculator = payrollCalculator;
            _validator = validator;
        }
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
            var validation = _validator.Validate(entry);
            if (!validation.IsValid)
            {
                return BadRequest(validation.ErrorMessage);
            }

            entry.Id = _nextId++;
            var hours = (entry.EndTime - entry.StartTime).TotalHours;
            var basePay = _payrollCalculator.CalculateBasePay(entry, hours);
            var bonus = _payrollCalculator.CalculateBonuses(entry, hours, basePay);

            entry.TotalPay = basePay + bonus;
            _timeEntries.Add(entry);

            return CreatedAtAction(nameof(GetById), new { id = entry.Id }, entry);
        }

        [HttpPost("calculate")]
        public IActionResult CalculatePay([FromBody] TimeEntry entry)
        {
            var validation = _validator.Validate(entry);
            if (!validation.IsValid)
            {
                return BadRequest(validation.ErrorMessage);
            }

            var hours = (entry.EndTime - entry.StartTime).TotalHours;
            var basePay = _payrollCalculator.CalculateBasePay(entry, hours);
            var (bonus, breakdown) = _payrollCalculator.CalculateBonusesWithBreakdown(entry, hours, basePay);

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

            var validation = _validator.Validate(entry);
            if (!validation.IsValid)
            {
                return BadRequest(validation.ErrorMessage);
            }

            existing.EmployeeName = entry.EmployeeName;
            existing.EmployeeType = entry.EmployeeType;
            existing.StartTime = entry.StartTime;
            existing.EndTime = entry.EndTime;
            existing.HourlyRate = entry.HourlyRate;
            existing.Notes = entry.Notes;
            existing.YearsOfService = entry.YearsOfService;

            var hours = (existing.EndTime - existing.StartTime).TotalHours;
            var basePay = _payrollCalculator.CalculateBasePay(existing, hours);
            var bonus = _payrollCalculator.CalculateBonuses(existing, hours, basePay);

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
