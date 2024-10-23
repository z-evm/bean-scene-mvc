using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using BeanScean.Models; 

namespace BeanScean.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }
        public DbSet<Person> People { get; set; }
        public DbSet<Restaurant> Restaurants { get; set; }
        public DbSet<RestaurantArea> RestaurantAreas { get; set; }
        public DbSet<RestaurantTable> RestaurantTables { get; set; }
        public DbSet<Sitting> Sittings { get; set; }
        public DbSet<Reservation> Reservations { get; set; }
       // public DbSet<AspNetUsers> Users { get; set; }  // ths come from  login system 
        //public DbSet<AspNetRoles> Roles { get; set; } // ths come from  login system 
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
           
         
        }
    }
}
