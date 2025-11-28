using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using TimeEntryLegacyAPI;

namespace TimeEntryLegacyApi.Tests;

public class TimeEntryControllerIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public TimeEntryControllerIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder => { });
    }

    [Fact]
    public async Task Create_Get_Update_Delete_WorkflowAsync()
    {
        var client = _factory.CreateClient();

        // Create an entry
        var createReq = new
        {
            EmployeeId = 42,
            EmployeeName = "Alice",
            EmployeeType = "FTE",
            StartTime = "2024-11-01T09:00:00Z",
            EndTime = "2024-11-01T17:00:00Z",
            HourlyRate = 20.0m,
            Notes = "Integration test shift",
            YearsOfService = 6
        };

        var createResp = await client.PostAsJsonAsync("/api/timeentry", createReq);
        Assert.Equal(HttpStatusCode.Created, createResp.StatusCode);

        var created = await createResp.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(created.TryGetProperty("id", out var idElem));
        var createdId = idElem.GetInt32();

        // GetById
        var getResp = await client.GetAsync($"/api/timeentry/{createdId}");
        Assert.Equal(HttpStatusCode.OK, getResp.StatusCode);

        // Update the entry
        var updateReq = new
        {
            EmployeeId = 42,
            EmployeeName = "Alice Updated",
            EmployeeType = "FTE",
            StartTime = "2024-11-01T08:00:00Z",
            EndTime = "2024-11-01T18:00:00Z",
            HourlyRate = 20.0m,
            Notes = "Updated shift for overtime",
            YearsOfService = 6
        };

        var updateResp = await client.PutAsJsonAsync($"/api/timeentry/{createdId}", updateReq);
        Assert.Equal(HttpStatusCode.OK, updateResp.StatusCode);

        var updated = await updateResp.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(updated.TryGetProperty("employeeName", out var nameElem));
        Assert.Equal("Alice Updated", nameElem.GetString());

        // Delete
        var delResp = await client.DeleteAsync($"/api/timeentry/{createdId}");
        Assert.Equal(HttpStatusCode.NoContent, delResp.StatusCode);

        // Confirm deletion
        var getAfterDelete = await client.GetAsync($"/api/timeentry/{createdId}");
        Assert.Equal(HttpStatusCode.NotFound, getAfterDelete.StatusCode);
    }

    [Fact]
    public async Task CalculatePay_ReturnsExpectedValuesAsync()
    {
        var client = _factory.CreateClient();

        var calcReq = new
        {
            EmployeeId = 7,
            EmployeeName = "Bob",
            EmployeeType = "Contractor",
            StartTime = "2024-07-04T10:00:00Z", // holiday in controller
            EndTime = "2024-07-04T18:00:00Z",
            HourlyRate = 50.0m,
            Notes = "Holiday shift",
            YearsOfService = 12
        };

        var calcResp = await client.PostAsJsonAsync("/api/timeentry/calculate", calcReq);
        Assert.Equal(HttpStatusCode.OK, calcResp.StatusCode);

        var payroll = await calcResp.Content.ReadFromJsonAsync<JsonElement>();

        // Assert keys exist, and totals are reasonable
        Assert.True(payroll.TryGetProperty("basePay", out var baseElem));
        Assert.True(payroll.TryGetProperty("bonusPay", out var bonusElem));
        Assert.True(payroll.TryGetProperty("totalPay", out var totalElem));

        var basePay = baseElem.GetDecimal();
        var bonusPay = bonusElem.GetDecimal();
        var totalPay = totalElem.GetDecimal();

        Assert.True(basePay > 0);
        Assert.True(bonusPay >= 0);
        Assert.Equal(basePay + bonusPay, totalPay);
    }

    [Fact]
    public async Task WeeklyReport_AggregatesEntriesAsync()
    {
        var client = _factory.CreateClient();

        // Make two entries for the same employee in the same week
        var e1 = new
        {
            EmployeeId = 55,
            EmployeeName = "Sam",
            EmployeeType = "PartTime",
            StartTime = "2024-11-03T09:00:00Z",
            EndTime = "2024-11-03T17:00:00Z",
            HourlyRate = 30.0m,
            Notes = "Day 1",
            YearsOfService = 2
        };

        var e2 = new
        {
            EmployeeId = 55,
            EmployeeName = "Sam",
            EmployeeType = "PartTime",
            StartTime = "2024-11-05T09:00:00Z",
            EndTime = "2024-11-05T17:00:00Z",
            HourlyRate = 30.0m,
            Notes = "Day 2",
            YearsOfService = 2
        };

        var resp1 = await client.PostAsJsonAsync("/api/timeentry", e1);
        Assert.Equal(HttpStatusCode.Created, resp1.StatusCode);
        var resp2 = await client.PostAsJsonAsync("/api/timeentry", e2);
        Assert.Equal(HttpStatusCode.Created, resp2.StatusCode);

        var startDate = "2024-11-03T00:00:00Z";
        var reportResp = await client.GetAsync($"/api/timeentry/report/weekly/55?startDate={Uri.EscapeDataString(startDate)}");
        Assert.Equal(HttpStatusCode.OK, reportResp.StatusCode);

        var report = await reportResp.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(report.TryGetProperty("totalHours", out var hoursElem));
        Assert.True(report.TryGetProperty("totalPay", out var payElem));

        var totalHours = hoursElem.GetDouble();
        var totalPay = payElem.GetDecimal();

        // Two 8-hour shifts
        Assert.Equal(16.0, totalHours, 1);
        Assert.True(totalPay > 0);
    }

    [Fact]
    public async Task Calculate_PartialNightHour_DoesNotCountPartialHourAsync()
    {
        var client = _factory.CreateClient();

        var req = new
        {
            EmployeeId = 101,
            EmployeeName = "EdgePartial",
            EmployeeType = "FTE",
            StartTime = "2024-11-01T21:30:00Z",
            EndTime = "2024-11-01T22:30:00Z",
            HourlyRate = 20.0m,
            Notes = "Partial night hour",
            YearsOfService = 1
        };

        var resp = await client.PostAsJsonAsync("/api/timeentry/calculate", req);
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

        var payroll = await resp.Content.ReadFromJsonAsync<JsonElement>();
        var basePay = payroll.GetProperty("basePay").GetDecimal();
        var bonus = payroll.GetProperty("bonusPay").GetDecimal();
        var total = payroll.GetProperty("totalPay").GetDecimal();

        // Because controller only checks hour boundaries, the 21:30-22:30 shift does not count as a night hour
        Assert.Equal(1.0m * 20.0m, basePay);
        Assert.Equal(0.0m, bonus);
        Assert.Equal(basePay + bonus, total);
    }

    [Fact]
    public async Task Calculate_MidnightCrossing_23To07_CalculatesNightHoursCorrectlyAsync()
    {
        var client = _factory.CreateClient();

        var req = new
        {
            EmployeeId = 150,
            EmployeeName = "MidnightEdge",
            EmployeeType = "FTE",
            StartTime = "2024-11-01T23:00:00Z",
            EndTime = "2024-11-02T07:00:00Z",
            HourlyRate = 25.0m,
            Notes = "Midnight crossing",
            YearsOfService = 0
        };

        var resp = await client.PostAsJsonAsync("/api/timeentry/calculate", req);
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

        var payroll = await resp.Content.ReadFromJsonAsync<JsonElement>();
        var basePay = payroll.GetProperty("basePay").GetDecimal();
        var bonus = payroll.GetProperty("bonusPay").GetDecimal();
        var total = payroll.GetProperty("totalPay").GetDecimal();
        var breakdown = payroll.GetProperty("breakdown").GetString() ?? string.Empty;

        // 8 hours * $25 = $200 base, night hours counted by controller = 7 (23,0,1,2,3,4,5)
        Assert.Equal(200.0m, basePay);
        Assert.Contains("Night shift", breakdown);
        Assert.Equal(87.50m, bonus); // 7 * 25 * 0.5
        Assert.Equal(basePay + bonus, total);
    }

    [Fact]
    public async Task Calculate_CombinedBonuses_Weekend_Night_LoyaltyAsync()
    {
        var client = _factory.CreateClient();

        // Start is Saturday 2024-11-02 23:00 -> weekend + night + loyalty (YearsOfService >=10)
        var req = new
        {
            EmployeeId = 160,
            EmployeeName = "ComboBonuses",
            EmployeeType = "FTE",
            StartTime = "2024-11-02T23:00:00Z",
            EndTime = "2024-11-03T03:00:00Z",
            HourlyRate = 20.0m,
            Notes = "Weekend night with long tenure",
            YearsOfService = 12
        };

        var resp = await client.PostAsJsonAsync("/api/timeentry/calculate", req);
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

        var payroll = await resp.Content.ReadFromJsonAsync<JsonElement>();
        var basePay = payroll.GetProperty("basePay").GetDecimal();
        var bonus = payroll.GetProperty("bonusPay").GetDecimal();
        var total = payroll.GetProperty("totalPay").GetDecimal();
        var breakdown = payroll.GetProperty("breakdown").GetString() ?? string.Empty;

        // 4 hours * $20 = $80 base
        Assert.Equal(80.0m, basePay);

        // Night hours (4) => night bonus = 4 * 20 * 0.5 = 40
        // Weekend bonus = base * 0.5 = 40
        // Loyalty (>=5 -> 10%) + (>=10 -> 5%) = 15% -> 0.15 * 80 = 12
        var expectedBonus = 40.0m + 40.0m + 12.0m;
        Assert.Equal(expectedBonus, bonus);
        Assert.Equal(basePay + bonus, total);

        // breakdown should list the three bonuses
        Assert.Contains("Night shift", breakdown);
        Assert.Contains("Weekend bonus", breakdown);
        Assert.Contains("Loyalty bonus (5+ years)", breakdown);
        Assert.Contains("Loyalty bonus (10+ years)", breakdown);
    }

    [Fact]
    public async Task Calculate_Overnight_FullNightBonusAsync()
    {
        var client = _factory.CreateClient();

        var req = new
        {
            EmployeeId = 102,
            EmployeeName = "EdgeOvernight",
            EmployeeType = "FTE",
            StartTime = "2024-11-01T23:00:00Z",
            EndTime = "2024-11-02T03:00:00Z",
            HourlyRate = 30.0m,
            Notes = "Overnight",
            YearsOfService = 3
        };

        var resp = await client.PostAsJsonAsync("/api/timeentry/calculate", req);
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

        var payroll = await resp.Content.ReadFromJsonAsync<JsonElement>();
        var basePay = payroll.GetProperty("basePay").GetDecimal();
        var bonus = payroll.GetProperty("bonusPay").GetDecimal();
        var total = payroll.GetProperty("totalPay").GetDecimal();
        var breakdown = payroll.GetProperty("breakdown").GetString() ?? string.Empty;

        // 4 hours * $30 = $120 base, night bonus 4 * 30 * 0.5 = $60
        Assert.Equal(120.0m, basePay);
        Assert.Contains("Night shift", breakdown);
        Assert.Equal(60.0m, bonus);
        Assert.Equal(basePay + bonus, total);
    }

    [Fact]
    public async Task Calculate_Overtime_BoundariesAsync()
    {
        var client = _factory.CreateClient();

        // exactly 40 hours -> no overtime but could include night bonuses; compute expected bonus instead of assuming 0
        var start = DateTime.Parse("2024-11-01T00:00:00Z");
        var endExact40 = start.AddHours(40);
        var req40 = new
        {
            EmployeeId = 200,
            EmployeeName = "OvertimeExact",
            EmployeeType = "FTE",
            StartTime = start.ToString("o"),
            EndTime = endExact40.ToString("o"),
            HourlyRate = 10.0m,
            Notes = "40h exact",
            YearsOfService = 0
        };

        var r40 = await client.PostAsJsonAsync("/api/timeentry/calculate", req40);
        Assert.Equal(HttpStatusCode.OK, r40.StatusCode);
        var p40 = await r40.Content.ReadFromJsonAsync<JsonElement>();
        var base40 = p40.GetProperty("basePay").GetDecimal();
        var bonus40 = p40.GetProperty("bonusPay").GetDecimal();
        Assert.Equal(400.0m, base40);

        // compute expected night hours from controller's algorithm (hour-by-hour checking)
        var startDt = DateTime.Parse(start.ToString());
        var endDt = DateTime.Parse(endExact40.ToString());
        var dt = startDt;
        var nightCount = 0;
        while (dt < endDt)
        {
            if (dt.Hour >= 22 || dt.Hour < 6)
                nightCount++;
            dt = dt.AddHours(1);
        }

        var expectedNightBonus40 = nightCount * 10.0m * 0.5m;
        Assert.Equal(expectedNightBonus40, bonus40);

        // 40.5 hours -> 0.5 overtime => overtime bonus = 0.5 * rate * 0.5
        var end4050 = start.AddHours(40.5);
        var req4050 = new
        {
            EmployeeId = 201,
            EmployeeName = "OvertimeHalf",
            EmployeeType = "FTE",
            StartTime = start.ToString("o"),
            EndTime = end4050.ToString("o"),
            HourlyRate = 10.0m,
            Notes = "40.5h",
            YearsOfService = 0
        };

        var r4050 = await client.PostAsJsonAsync("/api/timeentry/calculate", req4050);
        Assert.Equal(HttpStatusCode.OK, r4050.StatusCode);
        var p4050 = await r4050.Content.ReadFromJsonAsync<JsonElement>();
        var base4050 = p4050.GetProperty("basePay").GetDecimal();
        var bonus4050 = p4050.GetProperty("bonusPay").GetDecimal();

        Assert.Equal(405.0m, base4050); // 40.5 * 10
        // compute expected bonuses: night + overtime
        startDt = DateTime.Parse(start.ToString());
        endDt = DateTime.Parse(end4050.ToString());
        dt = startDt;
        nightCount = 0;
        while (dt < endDt)
        {
            if (dt.Hour >= 22 || dt.Hour < 6)
                nightCount++;
            dt = dt.AddHours(1);
        }

        var expectedNightBonus4050 = nightCount * 10.0m * 0.5m;
        var overtimeHours = (decimal)40.5 - 40.0m;
        var expectedOvertimeBonus = overtimeHours * 10.0m * 0.5m;
        var expectedTotalBonus4050 = expectedNightBonus4050 + expectedOvertimeBonus;

        Assert.Equal(expectedTotalBonus4050, bonus4050);
    }

    [Fact]
    public async Task Calculate_ZeroAndNegativeDurationAsync()
    {
        var client = _factory.CreateClient();

        // zero duration
        var zeroReq = new
        {
            EmployeeId = 300,
            EmployeeName = "Zero",
            EmployeeType = "FTE",
            StartTime = "2024-11-01T10:00:00Z",
            EndTime = "2024-11-01T10:00:00Z",
            HourlyRate = 100.0m,
            Notes = "zero duration",
            YearsOfService = 0
        };

        var rZero = await client.PostAsJsonAsync("/api/timeentry/calculate", zeroReq);
        Assert.Equal(HttpStatusCode.OK, rZero.StatusCode);
        var pZero = await rZero.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(0.0m, pZero.GetProperty("basePay").GetDecimal());
        Assert.Equal(0.0m, pZero.GetProperty("bonusPay").GetDecimal());

        // negative duration (end before start) -> controller currently computes negative basePay
        var negReq = new
        {
            EmployeeId = 301,
            EmployeeName = "Negative",
            EmployeeType = "FTE",
            StartTime = "2024-11-02T12:00:00Z",
            EndTime = "2024-11-02T08:00:00Z",
            HourlyRate = 50.0m,
            Notes = "negative duration",
            YearsOfService = 0
        };

        var rNeg = await client.PostAsJsonAsync("/api/timeentry/calculate", negReq);
        Assert.Equal(HttpStatusCode.OK, rNeg.StatusCode);
        var pNeg = await rNeg.Content.ReadFromJsonAsync<JsonElement>();
        var baseNeg = pNeg.GetProperty("basePay").GetDecimal();
        var totalNeg = pNeg.GetProperty("totalPay").GetDecimal();

        Assert.True(baseNeg < 0);
        Assert.Equal(baseNeg + pNeg.GetProperty("bonusPay").GetDecimal(), totalNeg);
    }
}
