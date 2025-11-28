using System.Collections.Generic;

namespace TimeEntryLegacyAPI.Models
{
    public class OvertimePolicyProvider : IOvertimePolicyProvider
    {
        private static readonly IReadOnlyDictionary<string, OvertimePolicy> Policies =
            new Dictionary<string, OvertimePolicy>
            {
                { "US", new OvertimePolicy { Country = "US", StandardWorkWeekHours = 40 } },
                { "UK", new OvertimePolicy { Country = "UK", StandardWorkWeekHours = 37 } },
                { "DE", new OvertimePolicy { Country = "DE", StandardWorkWeekHours = 35 } }
                // Add more countries as needed
            };

        public OvertimePolicy? GetPolicy(string countryCode)
        {
            Policies.TryGetValue(countryCode, out var policy);
            return policy;
        }
    }
}
