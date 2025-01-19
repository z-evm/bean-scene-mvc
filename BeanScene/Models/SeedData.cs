using Microsoft.AspNetCore.Identity;
using BeanScene.Data;
using BeanScene.Models;
using Microsoft.EntityFrameworkCore;

namespace BeanScene
{
    public static class SeedData
    {
        public static async Task SeedAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("SeedData");

            try
            {
                logger.LogInformation("Applying Migrations...");
                await context.Database.MigrateAsync();
                await SeedRolesAsync(roleManager, logger);
                await SeedUsersAsync(userManager, logger);
                await SeedRestaurantDataAsync(context, logger);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while seeding the database.");
            }
        }

        private static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager, ILogger logger)
        {
            var roles = new[]
            {
                new { Name = "Member", ConcurrencyStamp = Guid.NewGuid().ToString() },
                new { Name = "Admin", ConcurrencyStamp = Guid.NewGuid().ToString() },
                new { Name = "Staff", ConcurrencyStamp = Guid.NewGuid().ToString() },
                new { Name = "Manager", ConcurrencyStamp = Guid.NewGuid().ToString() }
            };

            foreach (var roleData in roles)
            {
                if (!await roleManager.RoleExistsAsync(roleData.Name))
                {
                    var role = new IdentityRole
                    {
                        Name = roleData.Name,
                        NormalizedName = roleData.Name.ToUpper(),
                        ConcurrencyStamp = roleData.ConcurrencyStamp
                    };

                    var result = await roleManager.CreateAsync(role);
                    if (result.Succeeded)
                    {
                        logger.LogInformation($"Role '{roleData.Name}' created successfully.");
                    }
                    else
                    {
                        logger.LogWarning($"Failed to create role '{roleData.Name}': {string.Join(", ", result.Errors.Select(e => e.Description))}");
                    }
                }
            }
        }

        private static async Task SeedUsersAsync(UserManager<IdentityUser> userManager, ILogger logger)
        {
            logger.LogInformation("Seeding Users...");
            var users = new[]
            {
                new { Email = "member@beanscene.com", Password = "password", Role = "Member" },
                new { Email = "admin@beanscene.com", Password = "password", Role = "Admin" },
                new { Email = "staff@beanscene.com", Password = "password", Role = "Staff" },
                new { Email = "manager@beanscene.com", Password = "password", Role = "Manager" }
            };

            foreach (var userData in users)
            {
                if (await userManager.FindByEmailAsync(userData.Email) == null)
                {
                    logger.LogInformation($"Creating User: {userData.Email}");
                    var user = new IdentityUser { UserName = userData.Email, Email = userData.Email };
                    var result = await userManager.CreateAsync(user, userData.Password);

                    if (result.Succeeded)
                    {
                        await userManager.AddToRoleAsync(user, userData.Role);
                        logger.LogInformation($"User '{userData.Email}' created and assigned to role '{userData.Role}'.");
                    }
                    else
                    {
                        logger.LogWarning($"Failed to create user '{userData.Email}': {string.Join(", ", result.Errors.Select(e => e.Description))}");
                    }
                }
            }
            logger.LogInformation("Users Seeded.");
        }

        private static async Task SeedRestaurantDataAsync(ApplicationDbContext context, ILogger logger)
        {
            logger.LogInformation("Seeding Restaurant...");
            if (!await context.Restaurants.AnyAsync())
            {
                var restaurant = new Restaurant("Bean Scene Cafe")
                {
                    Location = "123 Coffee St, Sydney 2000",
                    RestaurantAreas = new List<RestaurantArea>
                    {
                        new RestaurantArea { Name = "Main" },
                        new RestaurantArea { Name = "Balcony" },
                        new RestaurantArea { Name = "Outside" }
                    }
                };

                context.Restaurants.Add(restaurant);
                await context.SaveChangesAsync();
                logger.LogInformation("Restaurant Seeded.");

                // Seed Restaurant Tables
                var mainArea = restaurant.RestaurantAreas.First(a => a.Name == "Main");
                mainArea.Tables.AddRange(Enumerable.Range(1, 10).Select(i => new RestaurantTable($"M{i}")));

                var balconyArea = restaurant.RestaurantAreas.First(a => a.Name == "Balcony");
                balconyArea.Tables.AddRange(Enumerable.Range(1, 10).Select(i => new RestaurantTable($"B{i}")));

                var outsideArea = restaurant.RestaurantAreas.First(a => a.Name == "Outside");
                outsideArea.Tables.AddRange(Enumerable.Range(1, 10).Select(i => new RestaurantTable($"O{i}")));

                await context.SaveChangesAsync();
                logger.LogInformation("Tables Seeded.");
            }

            logger.LogInformation("Seeding Sittings...");
            if (!await context.Sittings.AnyAsync())
            {
                var restaurant = await context.Restaurants.FirstAsync();
                var today = DateTime.Today;
                context.Sittings.AddRange(new List<Sitting>
                {
                    new Sitting
                    {
                        Name = "BREAKFAST",
                        Start = today.AddHours(7),
                        End = today.AddHours(11),
                        Capacity = 40,
                        Closed = false,
                        Type = SittingType.Breakfast,
                        RestaurantId = restaurant.Id
                    },
                    new Sitting
                    {
                        Name = "LUNCH",
                        Start = today.AddHours(12),
                        End = today.AddHours(16),
                        Capacity = 40,
                        Closed = false,
                        Type = SittingType.Lunch,
                        RestaurantId = restaurant.Id
                    },
                    new Sitting
                    {
                        Name = "DINNER",
                        Start = today.AddHours(17),
                        End = today.AddHours(22),
                        Capacity = 40,
                        Closed = false,
                        Type = SittingType.Dinner,
                        RestaurantId = restaurant.Id
                    }
                });

                await context.SaveChangesAsync();
                logger.LogInformation("Sittings Seeded.");
            }

            logger.LogInformation("Seeding Reservation Statuses...");
            if (!await context.ReservationStatus.AnyAsync())
            {
                context.ReservationStatus.AddRange(new List<ReservationStatus>
                {
                    new ReservationStatus { Name = "Pending" },
                    new ReservationStatus { Name = "Approved" },
                    new ReservationStatus { Name = "Seated" },
                    new ReservationStatus { Name = "Finished" }
                });

                await context.SaveChangesAsync();
                logger.LogInformation("Reservation Statuses Seeded.");
            }

            if (!await context.Reservations.AnyAsync())
            {
                logger.LogInformation("Seeding Reservations with Persons...");

                // Retrieve Sitting and other required entities
                var sitting = await context.Sittings.FirstOrDefaultAsync();
                var reservationStatus = await context.ReservationStatus.FirstOrDefaultAsync();
                var mainTable = await context.RestaurantTables.FirstOrDefaultAsync();

                // Ensure required data exists
                if (sitting == null || reservationStatus == null || mainTable == null)
                {
                    logger.LogWarning("Required data for reservations (Sittings, ReservationStatuses, or Tables) is missing. Skipping reservation seeding.");
                    return;
                }

                // Create or Retrieve the Person
                var john = await context.Persons.FirstOrDefaultAsync(p => p.Email == "john@beanscene.com");
                if (john == null)
                {
                    john = new Person
                    {
                        Name = "John",
                        Phone = "95552266",
                        Email = "john@beanscene.com"
                    };

                    context.Persons.Add(john);
                    await context.SaveChangesAsync(); // Save to generate Person ID
                    logger.LogInformation("Person added: John.");
                }

                // Add the Reservation
                var reservationJohn = new Reservation
                {
                    Start = DateTime.Today.AddHours(8), // Today at 8:00 AM
                    Duration = 120, // 2 hours
                    Pax = 4, // Number of guests
                    Notes = "Window seat requested",
                    SittingId = sitting.Id,
                    PersonId = john.Id, // Associate with the created/retrieved person
                    ReservationStatusId = reservationStatus.Id,
                    Tables = new List<RestaurantTable> { mainTable }
                };

                // Create or Retrieve the Person
                var bob = await context.Persons.FirstOrDefaultAsync(p => p.Email == "bob@beanscene.com");
                if (bob == null)
                {
                    bob = new Person
                    {
                        Name = "Bob",
                        Phone = "94459962",
                        Email = "bob@beanscene.com"
                    };

                    context.Persons.Add(bob);
                    await context.SaveChangesAsync(); // Save to generate Person ID
                    logger.LogInformation("Person added: Bob.");
                }

                // Add the Reservation
                var reservationBob = new Reservation
                {
                    Start = DateTime.Today.AddHours(13), // Today at 1:00 PM
                    Duration = 120, // 2 hours
                    Pax = 4, // Number of guests
                    Notes = "Window seat requested",
                    SittingId = sitting.Id,
                    PersonId = bob.Id, // Associate with the created/retrieved person
                    ReservationStatusId = reservationStatus.Id,
                    Tables = new List<RestaurantTable> { mainTable }
                };

                // Create or Retrieve the Person
                var alice = await context.Persons.FirstOrDefaultAsync(p => p.Email == "alice@beanscene.com");
                if (alice == null)
                {
                    alice = new Person
                    {
                        Name = "Alice",
                        Phone = "88443626",
                        Email = "alice@beanscene.com"
                    };

                    context.Persons.Add(alice);
                    await context.SaveChangesAsync(); // Save to generate Person ID
                    logger.LogInformation("Person added: Alice.");
                }

                // Add the Reservation
                var reservationAlice = new Reservation
                {
                    Start = DateTime.Today.AddHours(17), // Today at 5:00 PM
                    Duration = 120, // 2 hours
                    Pax = 6, // Number of guests
                    Notes = "Big Table",
                    SittingId = sitting.Id,
                    PersonId = alice.Id, // Associate with the created/retrieved person
                    ReservationStatusId = reservationStatus.Id,
                    Tables = new List<RestaurantTable> { mainTable }
                };

                context.Reservations.AddRange(reservationJohn, reservationBob, reservationAlice);
                await context.SaveChangesAsync();
                logger.LogInformation("Reservations Seeded.");
            }
        }
    }
}
