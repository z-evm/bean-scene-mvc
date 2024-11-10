using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using BeanScene.Models; 

namespace BeanScene.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options){}
        
        
        public DbSet<Person> Persons { get; set; }
        public DbSet<Restaurant> Restaurants { get; set; }

        public DbSet<ReservationStatus> ReservationStatuses{get;set;}
        public DbSet<RestaurantArea> RestaurantAreas { get; set; }
        public DbSet<RestaurantTable> RestaurantTables { get; set; }
        public DbSet<Sitting> Sittings { get; set; }
        public DbSet<Reservation> Reservations { get; set; }
      
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
             base.OnModelCreating(modelBuilder);


             modelBuilder.Entity<Person>()
                .HasOne(p => p.User)
                .WithOne() // No navigation from IdentityUser back to Person
                .HasForeignKey<Person>(p => p.UserId)
                .OnDelete(DeleteBehavior.SetNull); // 
         
        }
    }
}
