# Time Entry Legacy API

## ⚠️ WARNING: This is LEGACY CODE for demonstration purposes ⚠️

This codebase contains **intentionally bad architectural decisions** to serve as a "before" example for refactoring presentations.

## 🚨 Bad Practices Included (DO NOT COPY!)

### Architecture Anti-Patterns
- ✗ **No separation of concerns** - All business logic in controllers
- ✗ **No dependency injection** - Static state everywhere
- ✗ **No interfaces or abstractions** - Tightly coupled code
- ✗ **No layered architecture** - No services, repositories, or domain layer
- ✗ **Thread-unsafe static collections** - Race conditions guaranteed
- ✗ **No validation** - Accept any input
- ✗ **Copy-pasted code** - Same calculation logic repeated 3+ times

### Code Smells
- ✗ **Magic numbers** - `0.5m`, `40`, `22`, `6` everywhere
- ✗ **Magic strings** - `"FTE"`, `"Contractor"`, `"PartTime"`
- ✗ **Long methods** - 100+ line methods
- ✗ **God objects** - Controller doing everything
- ✗ **Hardcoded dates** - Holidays hardcoded in multiple places
- ✗ **No error handling** - Crashes on bad input
- ✗ **No logging** - Silent failures

### Design Issues
- ✗ **Mutable models** - Public setters everywhere
- ✗ **No value objects** - Primitive obsession
- ✗ **DateTime instead of NodaTime** - Time zone bugs waiting to happen
- ✗ **No tests** - Zero test coverage
- ✗ **No documentation** - What does this code do?

## Running the API

```powershell
cd TimeEntryLegacyAPI
dotnet run --urls "http://localhost:5000"
```

Access Swagger UI at: http://localhost:5000/swagger

## API Endpoints

### Health Check
```http
GET http://localhost:5000/api/health
```

### Get All Time Entries
```http
GET http://localhost:5000/api/timeentry
```

### Create Time Entry
```http
POST http://localhost:5000/api/timeentry
Content-Type: application/json

{
  "employeeId": 1,
  "employeeName": "John Doe",
  "employeeType": "FTE",
  "startTime": "2024-11-27T09:00:00",
  "endTime": "2024-11-27T17:00:00",
  "hourlyRate": 50.00,
  "notes": "Regular shift",
  "yearsOfService": 3
}
```

### Calculate Pay (Without Saving)
```http
POST http://localhost:5000/api/timeentry/calculate
Content-Type: application/json

{
  "employeeId": 1,
  "employeeName": "Jane Smith",
  "employeeType": "Contractor",
  "startTime": "2024-11-27T22:00:00",
  "endTime": "2024-11-28T06:00:00",
  "hourlyRate": 75.00,
  "notes": "Night shift",
  "yearsOfService": 7
}
```

### Update Time Entry
```http
PUT http://localhost:5000/api/timeentry/1
Content-Type: application/json

{
  "employeeId": 1,
  "employeeName": "John Doe",
  "employeeType": "FTE",
  "startTime": "2024-11-27T09:00:00",
  "endTime": "2024-11-27T18:00:00",
  "hourlyRate": 50.00,
  "notes": "Updated end time",
  "yearsOfService": 3
}
```

### Delete Time Entry
```http
DELETE http://localhost:5000/api/timeentry/1
```

### Weekly Report
```http
GET http://localhost:5000/api/timeentry/report/weekly/1?startDate=2024-11-25
```

## Business Rules (Buried in Code)

### Employee Types
- **FTE**: Base rate × hours
- **Contractor**: Base rate × 1.2 × hours
- **PartTime**: Base rate × 0.95 × hours

### Bonuses
- **Night Shift**: +50% for hours between 22:00-06:00
- **Weekend**: +50% of base pay if Saturday/Sunday
- **Holiday**: +100% of base pay for hardcoded holidays (Jan 1, July 4, Dec 25)
- **Loyalty**: +10% after 5 years, +15% after 10 years
- **Overtime**: +50% per hour over 40 hours

## What's Wrong?

1. **No tests** - How do you know calculations are correct?
2. **Static state** - Multiple requests will corrupt data
3. **No database** - Data lost on restart
4. **No validation** - Can submit negative hours, future dates, etc.
5. **No error handling** - Division by zero? Null refs? Good luck!
6. **Duplicated logic** - Change a rate calculation? Update in 3 places!
7. **Hard to extend** - Adding a new employee type? Good luck!
8. **Hard to test** - No dependency injection
9. **Hard to maintain** - 200+ line controller methods
10. **No security** - No auth, no rate limiting, no input sanitization

## Refactoring Goals

This code should be refactored to:
- ✓ Implement Strategy Pattern for employee types
- ✓ Implement Chain of Responsibility for bonuses
- ✓ Use Value Objects (Rate, TimeEntry)
- ✓ Add service layer with dependency injection
- ✓ Add proper validation
- ✓ Add comprehensive tests (80%+ coverage)
- ✓ Use NodaTime for date/time handling
- ✓ Extract business rules to separate classes
- ✓ Add proper error handling and logging
- ✓ Follow SOLID principles

## See Also

- `AGENTS.md` - Guidelines for the GOOD version
- `ARCHITECTURE.md` - Proper design patterns to use
- `PRESENTATION_SCENARIO.md` - How to present this refactoring
