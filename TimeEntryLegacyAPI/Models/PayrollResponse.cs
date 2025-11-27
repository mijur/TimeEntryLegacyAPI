using System;

namespace TimeEntryLegacyAPI.Models
{
    public class PayrollResponse
    {
        public int Id { get; set; }
        public string EmployeeName { get; set; }
        public decimal BasePay { get; set; }
        public decimal BonusPay { get; set; }
        public decimal TotalPay { get; set; }
        public string Breakdown { get; set; }
    }
}
