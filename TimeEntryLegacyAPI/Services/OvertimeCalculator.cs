using System;

namespace TimeEntryLegacyAPI.Services
{
    public class OvertimeCalculator : IOvertimeCalculator
    {
        private readonly TimeEntryLegacyAPI.Models.IOvertimePolicyProvider _provider;

        public OvertimeCalculator(TimeEntryLegacyAPI.Models.IOvertimePolicyProvider provider)
        {
            _provider = provider;
        }

        public double CalculateOvertime(string countryCode, double totalHoursWorked)
        {
            int standardWorkWeekHours = 0;
            // Special case for France (not in OvertimePolicyProvider)
            if (countryCode == "FR")
            {
                standardWorkWeekHours = 35;
            }
            else
            {
                var policy = _provider.GetPolicy(countryCode);
                if (policy != null)
                {
                    standardWorkWeekHours = policy.StandardWorkWeekHours;
                }
                else
                {
                    // Unknown country, return 0 overtime
                    return 0;
                }
            }
            double overtime = totalHoursWorked - standardWorkWeekHours;
            return overtime > 0 ? overtime : 0;
        }
    }
}
