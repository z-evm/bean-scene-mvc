using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BeanScene.Models;

public class Restaurant
{
    public Restaurant(string name)
    {
        Name = name;
    }
    public int Id { get; set; }
    public string Name { get; set; }
    public string Location { get; set; } =default!;
    public List<Sitting> Sittings { get; set; } = new();
    public List<RestaurantArea> RestaurantAreas { get; set; } = new();
}
