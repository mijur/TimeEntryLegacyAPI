using System;
using System.Collections.Generic;
using System.Linq;
using TimeEntryLegacyAPI.Models;

namespace TimeEntryLegacyAPI.Services
{
    public class PayrollCalculator
    {
        private readonly IOvertimeCalculator _overtimeCalculator; // may be null when constructed without DI
        // Employee types
        private const string EmployeeTypeFte = "FTE";
        private const string EmployeeTypeContractor = "Contractor";
        private const string EmployeeTypePartTime = "PartTime";

        // Pay multipliers
        private const decimal ContractorMultiplier = 1.2m;
        private const decimal PartTimeMultiplier = 0.95m;
        private const decimal NightShiftBonusMultiplier = 0.5m;
        private const decimal WeekendBonusMultiplier = 0.5m;
        private const decimal HolidayBonusMultiplier = 1.0m;
        private const decimal LoyaltyBonusMultiplier5 = 0.1m;
        private const decimal LoyaltyBonusMultiplier10 = 0.05m;
        private const decimal OvertimeBonusMultiplier = 0.5m;

        // Hours / thresholds
        private const int NightShiftStartHour = 22;
        private const int NightShiftEndHour = 6;
        private const double OvertimeThresholdHours = 40.0; // fallback when no country policy or calculator
                public PayrollCalculator() { }

                public PayrollCalculator(IOvertimeCalculator overtimeCalculator)
                {
                    _overtimeCalculator = overtimeCalculator;
                }
        private const int LoyaltyYearsThreshold1 = 5;
        private const int LoyaltyYearsThreshold2 = 10;

        private static readonly DayOfWeek[] WeekendDays = { DayOfWeek.Saturday, DayOfWeek.Sunday };
        private static readonly List<DateTime> Holidays = new List<DateTime>
        {
            new DateTime(2024, 1, 1),
            new DateTime(2024, 7, 4),
            new DateTime(2024, 12, 25)
        };

        public decimal CalculateBasePay(TimeEntry entry, double hours)
        {
            if (entry.EmployeeType == EmployeeTypeFte)
                return (decimal)hours * entry.HourlyRate;

            if (entry.EmployeeType == EmployeeTypeContractor)
                return (decimal)hours * entry.HourlyRate * ContractorMultiplier;

            if (entry.EmployeeType == EmployeeTypePartTime)
                return (decimal)hours * entry.HourlyRate * PartTimeMultiplier;

            return 0m;
        }

        public (decimal bonus, string breakdown) CalculateBonusesWithBreakdown(TimeEntry entry, double hours, decimal basePay)
        {
            var bonus = 0m;
            var breakdown = string.Empty;

            var nightHours = CountNightHours(entry.StartTime, entry.EndTime);
            if (nightHours > 0)
            {
                bonus += (decimal)nightHours * entry.HourlyRate * NightShiftBonusMultiplier;
                breakdown += $"Night shift: {nightHours}h @ 50% = ${nightHours * entry.HourlyRate * 0.5m:F2}\n";
            }

            if (WeekendDays.Contains(entry.StartTime.DayOfWeek))
            {
                var weekendBonus = basePay * WeekendBonusMultiplier;
                bonus += weekendBonus;
                breakdown += $"Weekend bonus: ${weekendBonus:F2}\n";
            }

            if (Holidays.Any(h => h.Date == entry.StartTime.Date))
            {
                var holidayBonus = basePay * HolidayBonusMultiplier;
                bonus += holidayBonus;
                breakdown += $"Holiday bonus: ${holidayBonus:F2}\n";
            }

            if (entry.YearsOfService >= LoyaltyYearsThreshold1)
            {
                var loyaltyBonus = basePay * LoyaltyBonusMultiplier5;
                bonus += loyaltyBonus;
                breakdown += $"Loyalty bonus (5+ years): ${loyaltyBonus:F2}\n";
            }
            if (entry.YearsOfService >= LoyaltyYearsThreshold2)
            {
                var loyaltyBonus = basePay * LoyaltyBonusMultiplier10;
                bonus += loyaltyBonus;
                breakdown += $"Loyalty bonus (10+ years): ${loyaltyBonus:F2}\n";
            }

            // Overtime calculation: prefer injected calculator with country policy; fallback to static threshold
            double overtimeHoursCalculated = 0;
            if (_overtimeCalculator != null && !string.IsNullOrWhiteSpace(entry.CountryCode))
            {
                overtimeHoursCalculated = _overtimeCalculator.CalculateOvertime(entry.CountryCode, hours);
            }
            else if (hours > OvertimeThresholdHours)
            {
                overtimeHoursCalculated = hours - OvertimeThresholdHours;
            }

            if (overtimeHoursCalculated > 0)
            {
                var overtimeBonus = (decimal)overtimeHoursCalculated * entry.HourlyRate * OvertimeBonusMultiplier;
                bonus += overtimeBonus;
                breakdown += $"Overtime: {overtimeHoursCalculated}h @ 50% = ${overtimeBonus:F2}\n";
            }

            return (bonus, breakdown);
        }

        public decimal CalculateBonuses(TimeEntry entry, double hours, decimal basePay)
        {
            var (bonus, _) = CalculateBonusesWithBreakdown(entry, hours, basePay);
            return bonus;
        }

        private int CountNightHours(DateTime start, DateTime end)
        {
            var nightHours = 0;
            for (var dt = start; dt < end; dt = dt.AddHours(1))
            {
                if (dt.Hour >= NightShiftStartHour || dt.Hour < NightShiftEndHour)
                    nightHours++;
            }
            return nightHours;
        }
    }
}
