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

    public List<Sitting> Sittings { get; set; } = new(); // Restaurant has  many sitting such as breakfast , lunch , dinner 
    public List<RestaurantArea> RestaurantAreas { get; set; } = new(); //Restaurant can be many area such as outside,balcony,main 
}
