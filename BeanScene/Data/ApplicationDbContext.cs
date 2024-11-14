using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using BeanScene.Models; 

namespace BeanScene.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options){}
        
        
        public DbSet<Person> Persons { get; set; }=default!;
        public DbSet<Restaurant> Restaurants { get; set; }=default!;

        public DbSet<ReservationStatus> ReservationStatuses{get;set;}=default!;
        public DbSet<RestaurantArea> RestaurantAreas { get; set; }=default!;
        public DbSet<RestaurantTable> RestaurantTables { get; set; }=default!;
        public DbSet<Sitting> Sittings { get; set; }=default!;
        public DbSet<Reservation> Reservations { get; set; }=default!;
      
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
             base.OnModelCreating(modelBuilder);


             modelBuilder.Entity<Person>()
                .HasOne(p => p.User) // one
                .WithOne() // No navigation from IdentityUser back to Person
                .HasForeignKey<Person>(p => p.UserId)
                .OnDelete(DeleteBehavior.SetNull); // 

          



            modelBuilder.Entity<Reservation>() //many to many
                .HasMany(r => r.Tables)
                .WithMany(t => t.Reservations);



             modelBuilder.Entity<Reservation>() // give as defeult status "Pending"
                .Property(r => r.ReservationStatusId)
                .HasDefaultValue(1); 
                    
            
            modelBuilder.Entity<Reservation>() //Calculate End time on sql server side 
                    .Property(r => r.End)
                    .HasComputedColumnSql("DATEADD(MINUTE, [Duration], [Start])");

            

            
                    
         
        }
    }
}
