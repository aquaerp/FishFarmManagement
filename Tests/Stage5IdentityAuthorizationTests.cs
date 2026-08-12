using FishFarmManager.Data;
using FishFarmManager.Models;
using FishFarmManager.Services;
using Microsoft.EntityFrameworkCore;

namespace FishFarmManager.Tests;

public sealed class Stage5IdentityAuthorizationTests
{
    [Fact]
    public void UserAdministration_IsEnforcedInsideServiceAndAudited()
    {
        var path = Path.Combine(Path.GetTempPath(), $"aquafarm-g5-rbac-{Guid.NewGuid():N}.db");
        var options = new DbContextOptionsBuilder<FishFarmContext>()
            .UseSqlite($"Data Source={path};Pooling=False").Options;
        try
        {
            using var context = new FishFarmContext(options);
            context.Database.Migrate();
            AddUser(context, "viewer", UserRole.Viewer);
            AddUser(context, "admin", UserRole.Admin);
            context.SaveChanges();
            var authentication = new AuthenticationService(context);

            Assert.True(authentication.Login("viewer", "Valid!Password123"));
            Assert.False(authentication.CreateUser("blocked", "Valid!Password123", "Blocked", "", UserRole.Manager));
            Assert.DoesNotContain(context.Users, value => value.Username == "blocked");
            Assert.Contains(context.SecurityAuditEvents, value => value.Action == "CreateUser" && value.Outcome == "Denied");

            authentication.Logout();
            Assert.True(authentication.Login("admin", "Valid!Password123"));
            Assert.True(authentication.CreateUser("created", "Valid!Password123", "Created", "", UserRole.Manager));
            var created = context.Users.Single(value => value.Username == "created");
            Assert.True(authentication.UpdateUserProfile(created.UserId, "Updated", "updated@example.com", UserRole.Accountant, true));
            Assert.True(authentication.DeleteUser(created.UserId));
            Assert.Contains(context.SecurityAuditEvents, value => value.Action == "CreateUser" && value.Outcome == "Succeeded");
            Assert.Contains(context.SecurityAuditEvents, value => value.Action == "UpdateUser" && value.Outcome == "Succeeded");
            Assert.Contains(context.SecurityAuditEvents, value => value.Action == "DeleteUser" && value.Outcome == "Succeeded");
        }
        finally
        {
            if (AuthenticationService.CurrentUser != null)
            {
                using var cleanup = new FishFarmContext(options);
                new AuthenticationService(cleanup).Logout();
            }
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            if (File.Exists(path)) File.Delete(path);
        }
    }

    private static void AddUser(FishFarmContext context, string username, UserRole role) => context.Users.Add(new User
    {
        Username = username, FullName = username, Role = role, IsActive = true,
        PasswordHash = AuthenticationService.HashPassword("Valid!Password123"),
        PasswordChangedAt = DateTime.UtcNow, CreatedAt = DateTime.UtcNow, CreatedBy = "test"
    });
}

public sealed class Stage5AuditPrivacyTests
{
    [Theory]
    [InlineData("password=SuperSecret123", "password=[REDACTED]")]
    [InlineData("token: abc.def.ghi", "token=[REDACTED]")]
    [InlineData("Authorization=Basic dXNlcjpwYXNz", "Authorization=[REDACTED]")]
    public void SecurityAudit_RedactsSecretValues(string details, string expected)
    {
        Assert.Equal(expected, SecurityAuditService.RedactSecrets(details));
    }
}
