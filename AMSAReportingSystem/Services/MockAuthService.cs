//using System.Collections.Concurrent;
//using System.Text.Json;

//namespace AMSAReportingSystem.Services;

///// <summary>
///// Mock authentication service - simulates AMSA API
///// DEPRECATED: Use AmSaAuthService for real AMSA API integration
///// Kept for backward compatibility and testing without AMSA API access
///// </summary>
//public class MockAuthService
//{
//    private static readonly ConcurrentDictionary<string, AuthContext> MockUsers = new();

//    public MockAuthService()
//    {
//        SeedMockUsers();
//    }

//    private void SeedMockUsers()
//    {
//        // Officer at Taleem department
//        MockUsers.TryAdd("10001", new AuthContext
//        {
//            MemberId = 1,
//            MkanId = "10001",
//            FirstName = "Ahmed",
//            LastName = "Ibrahim",
//            Email = "ahmed@amsa.org",
//            UnitId = 5,
//            UnitName = "Lagos Central Unit",
//            StateId = 1,
//            StateName = "Lagos",
//            Token = GenerateMockJwt("10001"),
//            TokenExpiry = DateTime.UtcNow.AddHours(24),
//            Roles = new List<string> { "Taleem:DepartmentOfficer", "Tabligh:Member" }
//        });

//        // Unit President
//        MockUsers.TryAdd("10002", new AuthContext
//        {
//            MemberId = 2,
//            MkanId = "10002",
//            FirstName = "Fatima",
//            LastName = "Okonkwo",
//            Email = "fatima@amsa.org",
//            UnitId = 5,
//            UnitName = "Lagos Central Unit",
//            StateId = 1,
//            StateName = "Lagos",
//            Token = GenerateMockJwt("10002"),
//            TokenExpiry = DateTime.UtcNow.AddHours(24),
//            Roles = new List<string> { "President:UnitPresident" }
//        });

//        // State GS
//        MockUsers.TryAdd("20001", new AuthContext
//        {
//            MemberId = 3,
//            MkanId = "20001",
//            FirstName = "Emeka",
//            LastName = "Eze",
//            Email = "emeka@amsa.org",
//            UnitId = 100,
//            UnitName = "Lagos State Secretariat",
//            StateId = 1,
//            StateName = "Lagos",
//            Token = GenerateMockJwt("20001"),
//            TokenExpiry = DateTime.UtcNow.AddHours(24),
//            Roles = new List<string> { "GeneralSecretary:StateGS", "President:StatePresident" }
//        });

//        // National leadership
//        MockUsers.TryAdd("30001", new AuthContext
//        {
//            MemberId = 4,
//            MkanId = "30001",
//            FirstName = "Ibrahim",
//            LastName = "Musa",
//            Email = "ibrahim@amsa.org",
//            UnitId = 1000,
//            UnitName = "National Secretariat",
//            StateId = 37,
//            StateName = "National",
//            Token = GenerateMockJwt("30001"),
//            TokenExpiry = DateTime.UtcNow.AddHours(24),
//            Roles = new List<string> { "GeneralSecretary:NationalGS" }
//        });

//        // National President
//        MockUsers.TryAdd("30002", new AuthContext
//        {
//            MemberId = 5,
//            MkanId = "30002",
//            FirstName = "Aisha",
//            LastName = "Abdullahi",
//            Email = "aisha@amsa.org",
//            UnitId = 1000,
//            UnitName = "National Secretariat",
//            StateId = 37,
//            StateName = "National",
//            Token = GenerateMockJwt("30002"),
//            TokenExpiry = DateTime.UtcNow.AddHours(24),
//            Roles = new List<string> { "President:NationalPresident" }
//        });
//    }

//    /// <summary>
//    /// Mock login - for testing without AMSA API
//    /// </summary>
//    public async Task<(bool Success, AuthContext? User, string? Message)> LoginAsync(string mkanId, string password)
//    {
//        await Task.Delay(500); // Simulate API latency

//        if (string.IsNullOrWhiteSpace(mkanId) || string.IsNullOrWhiteSpace(password))
//            return (false, null, "MKAN ID and password are required");

//        if (!MockUsers.TryGetValue(mkanId, out var user))
//            return (false, null, "Invalid MKAN ID or password");

//        return (true, user, "Login successful");
//    }

//    /// <summary>
//    /// Get user by JWT token
//    /// </summary>
//    public async Task<AuthContext?> GetUserByTokenAsync(string token)
//    {
//        await Task.CompletedTask;
//        return MockUsers.Values.FirstOrDefault(u => u.Token == token);
//    }

//    /// <summary>
//    /// Generate mock JWT token
//    /// </summary>
//    private static string GenerateMockJwt(string mkanId)
//    {
//        // Simple mock token (not cryptographically valid)
//        var payload = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(
//            JsonSerializer.Serialize(new { mkanId, iat = DateTimeOffset.UtcNow.ToUnixTimeSeconds() })));
//        return $"eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.{payload}.mock_signature";
//    }
//}
