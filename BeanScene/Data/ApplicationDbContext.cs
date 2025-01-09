using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using BeanScene.Models; 

namespace BeanScene.Data
{
    // ApplicationDbContext inherits from IdentityDbContext to include Identity features
    public class ApplicationDbContext : IdentityDbContext
    {
        // Constructor that accepts DbContextOptions and passes it to the base class
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options){}

        // DbSet properties for each entity in the application
        public DbSet<Person> Persons { get; set; } = default!;
        public DbSet<Restaurant> Restaurants { get; set; } = default!;
        public DbSet<ReservationStatus> ReservationStatus {get;set;} = default!;
        public DbSet<RestaurantArea> RestaurantAreas { get; set; } = default!;
        public DbSet<RestaurantTable> RestaurantTables { get; set; } = default!;
        public DbSet<Sitting> Sittings { get; set; } = default!;
        public DbSet<Reservation> Reservations { get; set; } = default!;

        // Method to configure the model using the ModelBuilder
        protected override void OnModelCreating(ModelBuilder builder)
        {
            // Call the base class's OnModelCreating method
            base.OnModelCreating(builder);

            // Configure the Person entity
            builder.Entity<Person>()
                .HasOne(p => p.User) // One-to-one relationship with IdentityUser
                .WithOne()
                .HasForeignKey<Person>(p => p.UserId) // Foreign key in Person entity
                .OnDelete(DeleteBehavior.SetNull); // Set UserId to null on delete

            // Configure the many-to-many relationship between Reservation and RestaurantTable
            builder.Entity<Reservation>()
                .HasMany(r => r.Tables)
                .WithMany(t => t.Reservations);

            // Set default value for ReservationStatusId in Reservation entity
            builder.Entity<Reservation>()
                .Property(r => r.ReservationStatusId)
                .HasDefaultValue(1);

            // Define a computed column for the End property in Reservation entity
            builder.Entity<Reservation>()
                    .Property(r => r.End)
                    .HasComputedColumnSql("DATEADD(MINUTE, [Duration], [Start])");
        }
    }
}
