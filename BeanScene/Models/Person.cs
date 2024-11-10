using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BeanScene.Models
{
    public class Person
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; } // Primary key for Person

        public string? UserId { get; set; } // Nullable FK to IdentityUser

        [ForeignKey("UserId")]
        public IdentityUser? User { get; set; } // Navigation property to IdentityUser

        //  properties specific to Person
        public required string Name { get; set; }
        public required string Phone { get; set; }
        public required string Email { get; set; }
    }
}
