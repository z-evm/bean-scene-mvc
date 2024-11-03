using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;


namespace BeanScene.Models;

public class Person
{
    public Person(string name)
    {
        Name = name;
    }
    public int Id { get; set; }
    public string Name { get; set; }
    public string? UserId { get; set; }
    public IdentityUser? User { get; set; }
}
