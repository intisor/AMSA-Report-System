# AMSA Reporting System Unit Test Specification

## Scope
This specification defines the unit test coverage for deterministic logic in the AMSA Reporting System Blazor solution.

## Covered Modules

### Services
- `ReportAccessService`
- `AmsaTokenCache`
- `AmsaDirectoryLookupCache`
- `DepartmentReportFormMapper`
- `StateReportFormMapper`
- `AuthContext`
- `ReportDepartmentCatalog`

### Entities and DTOs
- `StateReport.UnitPresidentAttendanceRate`
- `ReportWithStateContext`
- `StateReportContext`
- `StateReportProgramView`

## Test Goals
- Verify role and access rules across unit, state, and national scopes.
- Verify cache expiration and fallback behavior.
- Verify form serialization and deserialization rules.
- Verify computed properties and default values.
- Verify report catalog ordering and membership.

## Test Categories

### Access Control Tests
- National leadership can access all report scopes.
- State leadership can access same-state reports.
- Unit leadership can access same-unit reports.
- Department officers can only act where their department and scope match.

### Token Cache Tests
- Fresh tokens are returned.
- Expired tokens are rejected.
- MKAN ID storage is preserved.
- Effective expiration is normalized and skewed safely.

### Lookup Cache Tests
- State and unit names are resolved from the API client.
- Cached values are reused.
- Fallback labels are returned when the API lookup fails.

### Form Mapper Tests
- Each department JSON payload deserializes into the correct form type.
- Invalid or empty JSON returns a default form instance.
- Tabligh campus detection updates `HasOnCampusActivity` correctly.
- State report mapping preserves metrics and program ordering.

### Entity / DTO Tests
- `AuthContext.GetDisplayName()` returns first and last name.
- `AuthContext.GetDashboard()` selects the highest-priority dashboard.
- `AuthContext` leadership flags reflect parsed roles.
- `StateReport.UnitPresidentAttendanceRate` computes the correct percentage.
- `ReportDepartmentCatalog.ReportableDepartments` contains all reporting departments in the expected order.

## Test Framework
- xUnit
- Moq for isolated external dependency behavior

## Naming Convention
Use behavior-driven names:
- `CanEditDepartment_WhenNationalLeadership_ReturnsTrue`
- `Deserialize_WhenInvalidJson_ReturnsDefaultForm`
- `GetValidToken_WhenTokenIsExpired_ReturnsNull`

## Execution Guidance
- Keep tests isolated and deterministic.
- Prefer unit tests over integration tests for pure logic.
- Avoid network, database, JS interop, and file system dependencies in unit tests.
