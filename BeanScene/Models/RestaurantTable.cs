
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BeanScene.Models;

public class RestaurantTable
{
    public RestaurantTable(int number)
    {
       Number = number;
    }
    public int Id { get; set; }
    public int Number { get; set; }
    public int RestaurantAreaId { get; set; }
    public RestaurantArea RestaurantArea { get; set; } = default!;
}
