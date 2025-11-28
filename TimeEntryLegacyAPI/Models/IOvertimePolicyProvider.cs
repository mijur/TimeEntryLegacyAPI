using System.Collections.Generic;

namespace TimeEntryLegacyAPI.Models
{
    public interface IOvertimePolicyProvider
    {
        OvertimePolicy? GetPolicy(string countryCode);
    }
}
