using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using BeanScene.Data;
using BeanScene.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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

            // Ensure the database is properly migrated
            Console.WriteLine("Applying Migrations...");
            await context.Database.MigrateAsync();

            // Seed Roles
            await SeedRolesAsync(roleManager);

            // Seed Users
            await SeedUsersAsync(userManager);

            // Seed Restaurants, Sittings, Reservation Statuses, and Reservations
            SeedRestaurantData(context);
        }

       private static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager)
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
                        ConcurrencyStamp = roleData.ConcurrencyStamp // Set ConcurrencyStamp explicitly
                    };

                    await roleManager.CreateAsync(role);
                }
            }
        }


        private static async Task SeedUsersAsync(UserManager<IdentityUser> userManager)
        {
            Console.WriteLine("Seeding Users...");
            var users = new[]
            {
                new { Email = "member@BeanScene", Password = "111111", Role = "Member" },
                new { Email = "admin@BeanScene", Password = "111111", Role = "Admin" },
                new { Email = "staff@BeanScene", Password = "111111", Role = "Staff" },
                new { Email = "manager@BeanScene", Password = "111111", Role = "Manager" }
            };

            foreach (var userData in users)
            {
                if (await userManager.FindByEmailAsync(userData.Email) == null)
                {
                    Console.WriteLine($"Creating User: {userData.Email}");
                    var user = new IdentityUser { UserName = userData.Email, Email = userData.Email };
                    var result = await userManager.CreateAsync(user, userData.Password);

                    if (result.Succeeded)
                    {
                        await userManager.AddToRoleAsync(user, userData.Role);
                    }
                }
            }
            Console.WriteLine("Users Seeded.");
        }

        private static void SeedRestaurantData(ApplicationDbContext context)
        {
            Console.WriteLine("Seeding Restaurants...");
            if (!context.Restaurants.Any())
            {
                var restaurant = new Restaurant("Bean Scene Cafe")
                {
                    Location = "123 Main St, Sydney",
                    RestaurantAreas = new List<RestaurantArea>
                    {
                        new RestaurantArea { Name = "Main" },
                        new RestaurantArea { Name = "Balcony" },
                        new RestaurantArea { Name = "Outside" }
                    }
                };

                context.Restaurants.Add(restaurant);
                context.SaveChanges();
                Console.WriteLine("Restaurant Seeded.");

                // Seed Restaurant Tables
                var mainArea = restaurant.RestaurantAreas.First(a => a.Name == "Main");
                mainArea.Tables.AddRange(Enumerable.Range(1, 10).Select(i => new RestaurantTable($"M{i}")));

                var balconyArea = restaurant.RestaurantAreas.First(a => a.Name == "Balcony");
                balconyArea.Tables.AddRange(Enumerable.Range(1, 10).Select(i => new RestaurantTable($"B{i}")));

                var outsideArea = restaurant.RestaurantAreas.First(a => a.Name == "Outside");
                outsideArea.Tables.AddRange(Enumerable.Range(1, 10).Select(i => new RestaurantTable($"O{i}")));

                context.SaveChanges();
                Console.WriteLine("Tables Seeded.");
            }

            Console.WriteLine("Seeding Sittings...");
            if (!context.Sittings.Any())
            {
                var restaurant = context.Restaurants.First();
                context.Sittings.AddRange(new List<Sitting>
                {
                    new Sitting
                    {
                        Name = "BREAKFAST",
                        Start = DateTime.Today.AddHours(7),
                        End = DateTime.Today.AddHours(11),
                        Capacity = 40,
                        Closed = false,
                        Type = SittingType.Breakfast,
                        RestaurantId = restaurant.Id
                    },
                    new Sitting
                    {
                        Name = "LUNCH",
                        Start = DateTime.Today.AddHours(12),
                        End = DateTime.Today.AddHours(16),
                        Capacity = 40,
                        Closed = false,
                        Type = SittingType.Lunch,
                        RestaurantId = restaurant.Id
                    },
                    new Sitting
                    {
                        Name = "DINNER",
                        Start = DateTime.Today.AddHours(17),
                        End = DateTime.Today.AddHours(22),
                        Capacity = 40,
                        Closed = false,
                        Type = SittingType.Dinner,
                        RestaurantId = restaurant.Id
                    }
                });

                context.SaveChanges();
                Console.WriteLine("Sittings Seeded.");
            }

            Console.WriteLine("Seeding Reservation Statuses...");
            if (!context.ReservationStatuses.Any())
            {
                context.ReservationStatuses.AddRange(new List<ReservationStatus>
                {
                    new ReservationStatus {Name = "Pending" },
                    new ReservationStatus {Name = "Approved" },
                    new ReservationStatus {Name = "Seated" },
                    new ReservationStatus {Name = "Finished" }
                });

                context.SaveChanges();
                Console.WriteLine("Reservation Statuses Seeded.");
            }



            if (!context.Reservations.Any())
            {
                Console.WriteLine("Seeding Reservations with Persons...");

                // Retrieve Sitting and other required entities
                var sitting = context.Sittings.FirstOrDefault();
                var reservationStatus = context.ReservationStatuses.FirstOrDefault();
                var mainTable = context.RestaurantTables.FirstOrDefault();

                // Ensure required data exists
                if (sitting == null || reservationStatus == null || mainTable == null)
                {
                    Console.WriteLine("Required data for reservations (Sittings, ReservationStatuses, or Tables) is missing. Skipping reservation seeding.");
                    return;
                }

                // Create or Retrieve the Person
                var ramazan = context.Persons.FirstOrDefault(p => p.Email == "ramazan@BeanScene");
                if (ramazan == null)
                {
                    ramazan = new Person
                    {
                        Name = "Ramazan",
                        Phone = "123456789",
                        Email = "ramazan@BeanScene"
                    };

                    context.Persons.Add(ramazan);
                    context.SaveChanges(); // Save to generate Person ID
                    Console.WriteLine("Person added: Ramazan.");
                }

                // Add the Reservation
                var reservationRamazan = new Reservation
                {
                    Start = DateTime.Today.AddHours(8), // Today at 8:00 AM
                    Duration = 120, // 2 hours
                    Pax = 4, // Number of guests
                    Notes = "Window seat requested",
                    SittingId = sitting.Id,
                    PersonId = ramazan.Id, // Associate with the created/retrieved person
                    ReservationStatusId = reservationStatus.Id,
                    Tables = new List<RestaurantTable> { mainTable }
                };






                // Create or Retrieve the Person
                var alex = context.Persons.FirstOrDefault(p => p.Email == "alex@BeanScene");
                if (alex == null)
                {
                    alex = new Person
                    {
                        Name = "alex",
                        Phone = "123456789",
                        Email = "alex@BeanScene"
                    };

                    context.Persons.Add(alex);
                    context.SaveChanges(); // Save to generate Person ID
                    Console.WriteLine("Person added: Alex");
                }

                // Add the Reservation
                var reservationAlex = new Reservation
                {
                    Start = DateTime.Today.AddHours(13), // Today at 8:00 AM
                    Duration = 120, // 2 hours
                    Pax = 4, // Number of guests
                    Notes = "Window seat requested",
                    SittingId = sitting.Id,
                    PersonId = alex.Id, // Associate with the created/retrieved person
                    ReservationStatusId = reservationStatus.Id,
                    Tables = new List<RestaurantTable> { mainTable }
                };





                // Create or Retrieve the Person
                var zack = context.Persons.FirstOrDefault(p => p.Email == "zack@BeanScene");
                if (zack == null)
                {
                    zack = new Person
                    {
                        Name = "zack",
                        Phone = "123456789",
                        Email = "zack@BeanScene"
                    };

                    context.Persons.Add(zack);
                    context.SaveChanges(); // Save to generate Person ID
                    Console.WriteLine("Person added: zack");
                }

                // Add the Reservation
                var reservationZack = new Reservation
                {
                    Start = DateTime.Today.AddHours(17), // Today at 8:00 AM
                    Duration = 120, // 2 hours
                    Pax = 6, // Number of guests
                    Notes = "Big Table",
                    SittingId = sitting.Id,
                    PersonId = zack.Id, // Associate with the created/retrieved person
                    ReservationStatusId = reservationStatus.Id,
                    Tables = new List<RestaurantTable> { mainTable }
                };


                context.Reservations.Add(reservationRamazan);
                context.Reservations.Add(reservationAlex);
                context.Reservations.Add(reservationZack);
                context.SaveChanges();
                Console.WriteLine("Reservatio");

                    }
                }
    }
}
