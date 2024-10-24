using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BeanScene.Models;

public class RestaurantArea
{
    public RestaurantArea(string name)
    {
        Name = name;
    }
    public int Id { get; set; }
    public string Name { get; set; }
    public int RestaurantId { get; set; }
    public Restaurant Restaurant { get; set; } = default!;
}
