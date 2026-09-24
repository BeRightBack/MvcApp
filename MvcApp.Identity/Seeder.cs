using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Reflection;
using System.Text.Json;
using MvcApp.Core;
using MvcApp.Infrastructure;

#pragma warning disable CA1873 // Avoid conditional access in logging statements

namespace MvcApp.Identity
{
    public static class Seeder
    {
        public static void SeedData(this IApplicationBuilder app) => SeedStoreAsync(app).GetAwaiter().GetResult();

        private static byte[] GetDefaultProfilePicture()
        {
            try
            {
                string filePath = "wwwroot/images/default-user.png";
                if (File.Exists(filePath))
                {
                    return File.ReadAllBytes(filePath);
                }
                return [];
            }
            catch
            {
                return [];
            }
        }

        private static async Task SeedStoreAsync(IApplicationBuilder app)
        {
            using var scope = app.ApplicationServices.CreateScope();

            var context = scope.ServiceProvider.GetRequiredService<UserDbContext>();
            var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<UserDetails>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<UserRole>>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<UserDbContext>>();

            try
            {
                // Apply pending migrations
                // NOTE: Do NOT use EnsureCreated() anywhere else in the code as it conflicts with migrations
                if (context.Database.GetPendingMigrations().Any())
                {
                    logger.LogInformation("Applying pending migrations...");
                    await context.Database.MigrateAsync();
                    logger.LogInformation("Migrations applied successfully.");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while applying migrations.");
                throw;
            }

            await SeedRolesAsync(roleManager, logger);
            await SeedAdminUserAsync(config, userManager, logger);
            await SeedTestUsersAsync(userManager, logger, context);
            await SeedLikesAndMessagesAsync(context, logger);
        }

        private static async Task SeedRolesAsync(RoleManager<UserRole> roleManager, ILogger logger)
        {
            try
            {
                string[] roleNames = ["Admin", "Moderator"];
                logger.LogInformation("Starting role seeding...");

                foreach (var roleName in roleNames)
                {
                    if (!await roleManager.RoleExistsAsync(roleName))
                    {
                        logger.LogInformation("Creating role: {RoleName}", roleName);
                        var result = await roleManager.CreateAsync(new UserRole(roleName));

                        if (!result.Succeeded)
                        {
                            logger.LogError("Failed to create role {RoleName} with {ErrorCount} error(s)", roleName, result.Errors.Count());
                            foreach (var error in result.Errors)
                            {
                                logger.LogError("Role creation error: {Code} - {Description}", error.Code, error.Description);
                            }
                        }
                        else
                        {
                            logger.LogInformation("Role {RoleName} created successfully", roleName);
                        }
                    }
                    else
                    {
                        logger.LogInformation("Role {RoleName} already exists", roleName);
                    }
                }

                logger.LogInformation("Role seeding completed.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while seeding roles.");
                throw;
            }
        }

        private static async Task SeedAdminUserAsync(IConfiguration config, UserManager<UserDetails> userManager, ILogger logger)
        {
            try
            {
                string adminRole = config["Administrator:Role"] ?? "Admin";
                string adminUsername = config["Administrator:Username"] ?? "admin";
                string adminEmail = config["Administrator:User"] ?? "admin@frenzyzone.com";
                string adminPassword = config["Administrator:Password"] ?? "Electro@2013";

                logger.LogInformation("Checking for admin user: {AdminEmail}", adminEmail);

                var adminUser = await userManager.FindByEmailAsync(adminEmail);

                if (adminUser == null)
                {
                    logger.LogInformation("Creating admin user: {AdminEmail}", adminEmail);

                    adminUser = new UserDetails
                    {
                        UserName = adminUsername,
                        Email = adminEmail,
                        EmailConfirmed = true,
                        FirstName = "Admin",
                        LastName = "Administrator",
                        PhoneNumber = "1234567890",
                        PhoneNumberConfirmed = true,
                        ProfilePicture = GetDefaultProfilePicture(),
                        ProfilePicturePath = "/images/default-user.png",
                        DateOfBirth = DateTime.Parse("1980-01-01"),
                        Age = DateTime.Now.Year - 1980,
                        AgeRange = [18, 100],
                        KnownAs = "Admin",
                        Created = DateTime.UtcNow,
                        LastActive = DateTime.UtcNow,
                        Gender = "Not Specified",
                        Sexuality = "Not Specified",
                        Introduction = "System Administrator",
                        LookingFor = "N/A",
                        Interests = ["System Administration", "Management"],
                        City = "Toronto",
                        State = "Ontario",
                        Country = "Canada",
                        IsProfileComplete = true,
                        ReportedUsers = new List<Guid>()
                    };

                    var result = await userManager.CreateAsync(adminUser, adminPassword);

                    if (result.Succeeded)
                    {
                        logger.LogInformation("Admin user created successfully.");
                        await userManager.AddToRoleAsync(adminUser, adminRole);
                        logger.LogInformation("Admin user added to {AdminRole} role.", adminRole);
                    }
                    else
                    {
                        logger.LogError("Failed to create admin user with {ErrorCount} error(s)", result.Errors.Count());
                        foreach (var error in result.Errors)
                        {
                            logger.LogError("Admin creation error: {Code} - {Description}", error.Code, error.Description);
                        }
                        return;
                    }
                }
                else
                {
                    logger.LogInformation("Admin user already exists: {AdminEmail}", adminEmail);
                    if (!await userManager.IsInRoleAsync(adminUser, adminRole))
                    {
                        await userManager.AddToRoleAsync(adminUser, adminRole);
                        logger.LogInformation("Admin role added to existing user {AdminEmail}.", adminEmail);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while seeding admin user.");
                throw;
            }
        }

        private static async Task SeedTestUsersAsync(UserManager<UserDetails> userManager, ILogger logger, UserDbContext context)
        {
            // Check if test users already exist
            var testUserExists = await userManager.Users.AnyAsync(u => u.Email != null && u.Email.Contains("@test.com"));
            if (testUserExists)
            {
                logger.LogInformation("Test users already exist. Skipping test user seeding.");
                return;
            }

            try
            {
                string? execPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                string jsonPath = Path.Combine(execPath ?? "", "Data", "UserSeedData.json");

                if (!File.Exists(jsonPath))
                {
                    logger.LogWarning("User seed data file not found at: {JsonPath}. Skipping test user seeding.", jsonPath);
                    return;
                }

                string userData = await File.ReadAllTextAsync(jsonPath);
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var usersData = JsonSerializer.Deserialize<List<JsonElement>>(userData, options);

                if (usersData == null || usersData.Count == 0)
                {
                    logger.LogWarning("No users found in seed data file.");
                    return;
                }

                logger.LogInformation("Seeding {UserCount} test users...", usersData.Count);

                foreach (var userData_element in usersData)
                {
                    var user = new UserDetails
                    {
                        UserName = userData_element.GetProperty("UserName").GetString(),
                        Email = userData_element.GetProperty("UserName").GetString() + "@test.com",
                        EmailConfirmed = true,
                        Gender = userData_element.TryGetProperty("Gender", out var gender) ? gender.GetString() ?? "Not Specified" : "Not Specified",
                        DateOfBirth = userData_element.TryGetProperty("DateOfBirth", out var dob) ? DateTime.Parse(dob.GetString() ?? "") : DateTime.UtcNow.AddYears(-25),
                        KnownAs = userData_element.TryGetProperty("KnownAs", out var knownAs) ? knownAs.GetString() ?? "" : userData_element.GetProperty("UserName").GetString() ?? "",
                        Created = userData_element.TryGetProperty("Created", out var created) ? DateTime.Parse(created.GetString() ?? "") : DateTime.UtcNow,
                        LastActive = userData_element.TryGetProperty("LastActive", out var lastActive) ? DateTime.Parse(lastActive.GetString() ?? "") : DateTime.UtcNow,
                        Introduction = userData_element.TryGetProperty("Introduction", out var intro) ? intro.GetString() ?? "" : "",
                        LookingFor = userData_element.TryGetProperty("LookingFor", out var looking) ? looking.GetString() ?? "" : "",
                        City = userData_element.TryGetProperty("City", out var city) ? city.GetString() ?? "" : "",
                        State = userData_element.TryGetProperty("State", out var state) ? state.GetString() ?? "" : "",
                        IsProfileComplete = true
                    };

                    // Handle Interests - can be string or array
                    if (userData_element.TryGetProperty("Interests", out var interests))
                    {
                        if (interests.ValueKind == JsonValueKind.String)
                        {
                            var interestsStr = interests.GetString() ?? "";
                            user.Interests = interestsStr.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                        }
                        else if (interests.ValueKind == JsonValueKind.Array)
                        {
                            user.Interests = interests.EnumerateArray().Select(i => i.GetString() ?? "").ToArray();
                        }
                    }

                    if (user.Interests.Length == 0)
                    {
                        user.Interests = ["General Interest"];
                    }

                    // Handle Photos
                    if (userData_element.TryGetProperty("Photos", out var photos) && photos.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var photoElement in photos.EnumerateArray())
                        {
                            var photo = new Photo
                            {
                                Filename = photoElement.GetProperty("Filename").GetString() ?? "",
                                IsMain = photoElement.TryGetProperty("IsMain", out var isMain) && isMain.GetBoolean(),
                                UserDetails = user,
                                UserDetailsId = user.Id
                            };
                            user.Photos.Add(photo);
                        }
                    }

                    var result = await userManager.CreateAsync(user, "Passw0rd123!!");

                    if (result.Succeeded)
                    {
                        logger.LogInformation("Created test user: {UserName}", user.UserName);
                    }
                    else
                    {
                        logger.LogError("Failed to create user {UserName}: {Errors}", user.UserName, string.Join(", ", result.Errors.Select(e => e.Description)));
                    }
                }

                await context.SaveChangesAsync();
                logger.LogInformation("Test users seeded successfully.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while seeding test users.");
            }
        }

        private static async Task SeedLikesAndMessagesAsync(UserDbContext context, ILogger logger)
        {
            if (await context.Likes!.AnyAsync())
            {
                logger.LogInformation("Likes and messages already exist. Skipping.");
                return;
            }

            try
            {
                var users = await context.Users.OfType<UserDetails>().ToListAsync();

                if (users.Count < 2)
                {
                    logger.LogWarning("Not enough users to seed likes and messages.");
                    return;
                }

                logger.LogInformation("Seeding likes and messages...");
                var random = new Random();
                var pendingPairs = new HashSet<(string, string)>();

                foreach (var user in users)
                {
                    int numLikes = random.Next(0, Math.Min(5, users.Count - 1));

                    for (int i = 0; i < numLikes; i++)
                    {
                        var potentialLikes = users
                            .Where(u => u.Id != user.Id && u.Gender != user.Gender)
                            .OrderBy(x => Guid.NewGuid())
                            .Take(1)
                            .FirstOrDefault();

                        if (potentialLikes != null && !pendingPairs.Contains((user.Id, potentialLikes.Id)))
                        {
                            var likeExists = await context.Likes!
                                .AnyAsync(l => l.SourceUserId == user.Id && l.LikedUserId == potentialLikes.Id);

                            if (!likeExists)
                            {
                                pendingPairs.Add((user.Id, potentialLikes.Id));
                                context.Likes!.Add(new UserLike
                                {
                                    SourceUserId = user.Id,
                                    LikedUserId = potentialLikes.Id
                                });

                                context.Messages!.Add(new Message
                                {
                                    SenderId = user.Id,
                                    SenderUsername = user.UserName,
                                    RecipientId = potentialLikes.Id,
                                    RecipientUsername = potentialLikes.UserName,
                                    Content = "Hi " + potentialLikes.KnownAs + "! I'd love to chat with you.",
                                    MessageSent = DateTime.UtcNow.AddDays(-random.Next(1, 30))
                                });
                            }
                        }
                    }
                }

                await context.SaveChangesAsync();
                logger.LogInformation("Likes and messages seeded successfully.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while seeding likes and messages.");
            }
        }
    }
}
