using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BeanScene.Models;

public class Restaurant
{
    public Restaurant(string name, string location)
    {
        Name = name;
        Location = location;
    }
    public int Id { get; set; }
    public string Name { get; set; }
    public string Location { get; set; }
}
