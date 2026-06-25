// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using System.Security.Claims;
using System.Text.Json;
using Duende.IdentityModel;
using Duende.IdentityServer;
using Duende.IdentityServer.Test;

namespace IdentityServer;

public interface IFakeUserService
{
    List<TestUser> GetUsers();
    List<FakeUserSummary> GetUserSummaries();
    FakeUserSummary? GetUserSummary(string subjectId);
    bool ValidateCredentials(string username, string password);
    TestUser? FindByUsername(string username);
}

public record FakeUserSummary(
    string Id,
    string Username,
    string FirstName,
    string LastName,
    string Email,
    string Role,
    bool Status
);

public record FakeIdentityUser(
    Guid Id,
    string Username,
    string Password,
    string FirstName,
    string LastName,
    string Email,
    string Role,
    bool IsActive)
{
    public string Name => $"{FirstName} {LastName}";
}

public class FakeUserService : IFakeUserService
{
    private static readonly IReadOnlyList<FakeIdentityUser> IdentityUsers =
    [
        new(Guid.Parse("11111111-1111-1111-1111-111111111111"), "alice", "alice", "Alice", "Smith", "AliceSmith@email.com", "admin", true),
        new(Guid.Parse("22222222-2222-2222-2222-222222222222"), "michael.johnson", "michael", "Michael", "Johnson", "michael.johnson@northwind-warehouse.com", "warehouse_manager", true),
        new(Guid.Parse("33333333-3333-3333-3333-333333333333"), "sarah.williams", "sarah", "Sarah", "Williams", "sarah.williams@northwind-warehouse.com", "inventory_manager", true),
        new(Guid.Parse("44444444-4444-4444-4444-444444444444"), "david.brown", "david", "David", "Brown", "david.brown@northwind-warehouse.com", "shift_supervisor", true),
        new(Guid.Parse("55555555-5555-5555-5555-555555555555"), "emily.davis", "emily", "Emily", "Davis", "emily.davis@northwind-warehouse.com", "shift_supervisor", true),
        new(Guid.Parse("66666666-6666-6666-6666-666666666666"), "james.miller", "james", "James", "Miller", "james.miller@northwind-warehouse.com", "receiving_clerk", true),
        new(Guid.Parse("77777777-7777-7777-7777-777777777777"), "linda.wilson", "linda", "Linda", "Wilson", "linda.wilson@northwind-warehouse.com", "receiving_clerk", true),
        new(Guid.Parse("88888888-8888-8888-8888-888888888888"), "robert.moore", "robert", "Robert", "Moore", "robert.moore@northwind-warehouse.com", "picker", true),
        new(Guid.Parse("99999999-9999-9999-9999-999999999999"), "patricia.taylor", "patricia", "Patricia", "Taylor", "patricia.taylor@northwind-warehouse.com", "picker", true),
        new(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), "william.anderson", "william", "William", "Anderson", "william.anderson@northwind-warehouse.com", "picker", true),
        new(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"), "barbara.thomas", "barbara", "Barbara", "Thomas", "barbara.thomas@northwind-warehouse.com", "packer", true),
        new(Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"), "richard.jackson", "richard", "Richard", "Jackson", "richard.jackson@northwind-warehouse.com", "packer", true),
        new(Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd"), "elizabeth.white", "elizabeth", "Elizabeth", "White", "elizabeth.white@northwind-warehouse.com", "forklift_operator", true),
        new(Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee"), "thomas.harris", "thomas", "Thomas", "Harris", "thomas.harris@northwind-warehouse.com", "forklift_operator", true),
        new(Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff"), "jennifer.martin", "jennifer", "Jennifer", "Martin", "jennifer.martin@northwind-warehouse.com", "quality_controller", true),
        new(Guid.Parse("12121212-1212-1212-1212-121212121212"), "charles.thompson", "charles", "Charles", "Thompson", "charles.thompson@northwind-warehouse.com", "dispatch_clerk", true),
        new(Guid.Parse("13131313-1313-1313-1313-131313131313"), "mary.garcia", "mary", "Mary", "Garcia", "mary.garcia@northwind-warehouse.com", "dispatch_clerk", true),
        new(Guid.Parse("14141414-1414-1414-1414-141414141414"), "christopher.martinez", "christopher", "Christopher", "Martinez", "christopher.martinez@northwind-warehouse.com", "stock_controller", true),
        new(Guid.Parse("15151515-1515-1515-1515-151515151515"), "nancy.robinson", "nancy", "Nancy", "Robinson", "nancy.robinson@northwind-warehouse.com", "returns_coordinator", true),
        new(Guid.Parse("16161616-1616-1616-1616-161616161616"), "daniel.clark", "daniel", "Daniel", "Clark", "daniel.clark@northwind-warehouse.com", "maintenance", true),
        new(Guid.Parse("17171717-1717-1717-1717-171717171717"), "karen.rodriguez", "karen", "Karen", "Rodriguez", "karen.rodriguez@northwind-warehouse.com", "planner", true),
        new(Guid.Parse("18181818-1818-1818-1818-181818181818"), "mark.lewis", "mark", "Mark", "Lewis", "mark.lewis@northwind-warehouse.com", "auditor", true),
        new(Guid.Parse("19191919-1919-1919-1919-191919191919"), "susan.lee", "susan", "Susan", "Lee", "susan.lee@northwind-warehouse.com", "customer_service", true),
        new(Guid.Parse("20202020-2020-2020-2020-202020202020"), "kevin.walker", "kevin", "Kevin", "Walker", "kevin.walker@northwind-warehouse.com", "warehouse_operator", false)
    ];

    public static List<TestUser> Users
    {
        get
        {
            var address = new
            {
                street_address = "One Hacker Way",
                locality = "Heidelberg",
                postal_code = 69118,
                country = "Germany"
            };

            return IdentityUsers
                .Select(user => new TestUser
                {
                    SubjectId = user.Id.ToString(),
                    Username = user.Username,
                    Password = user.Password,
                    Claims =
                    {
                        new Claim(JwtClaimTypes.Name, user.Name),
                        new Claim(JwtClaimTypes.GivenName, user.FirstName),
                        new Claim(JwtClaimTypes.FamilyName, user.LastName),
                        new Claim(JwtClaimTypes.Email, user.Email),
                        new Claim(JwtClaimTypes.EmailVerified, user.IsActive.ToString().ToLowerInvariant(), ClaimValueTypes.Boolean),
                        new Claim(JwtClaimTypes.WebSite, $"https://profiles.northwind-warehouse.example/{user.Username}"),
                        new Claim(JwtClaimTypes.Role, user.Role),
                        new Claim(JwtClaimTypes.Address, JsonSerializer.Serialize(address), IdentityServerConstants.ClaimValueTypes.Json)
                    }
                })
                .ToList();
        }
    }


    public List<TestUser> GetUsers()
    {
        return Users;
    }

    public List<FakeUserSummary> GetUserSummaries()
    {
        return Users.Select(MapToSummary).ToList();
    }

    public FakeUserSummary? GetUserSummary(string subjectId)
    {
        var user = Users.FirstOrDefault(x => x.SubjectId.Equals(subjectId, StringComparison.OrdinalIgnoreCase));
        return user is null ? null : MapToSummary(user);
    }

    public bool ValidateCredentials(string username, string password)
    {
        var user = FindByUsername(username);

        return user != null
            ? string.IsNullOrWhiteSpace(user.Password) && string.IsNullOrWhiteSpace(password) ? true : Equals(user.Password, password)
            : false;
    }
    public TestUser? FindByUsername(string username) => Users.FirstOrDefault(x => x.Username.Equals(username, StringComparison.OrdinalIgnoreCase));

    private static FakeUserSummary MapToSummary(TestUser user)
    {
        var firstName = user.Claims.FirstOrDefault(c => c.Type == JwtClaimTypes.GivenName)?.Value ?? string.Empty;
        var lastName = user.Claims.FirstOrDefault(c => c.Type == JwtClaimTypes.FamilyName)?.Value ?? string.Empty;
        var email = user.Claims.FirstOrDefault(c => c.Type == JwtClaimTypes.Email)?.Value ?? string.Empty;
        var role = user.Claims.FirstOrDefault(c => c.Type == JwtClaimTypes.Role)?.Value ?? "user";
        var statusClaim = user.Claims.FirstOrDefault(c => c.Type == JwtClaimTypes.EmailVerified)?.Value;
        var status = bool.TryParse(statusClaim, out var parsedStatus) && parsedStatus;

        return new FakeUserSummary(
            Id: user.SubjectId,
            Username: user.Username,
            FirstName: firstName,
            LastName: lastName,
            Email: email,
            Role: role,
            Status: status
        );
    }
}
