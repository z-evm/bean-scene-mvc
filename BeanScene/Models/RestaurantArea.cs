using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BeanScene.Models;

public class RestaurantArea
{

    public int Id { get; set; }
    public string Name { get; set; } =default!;
    public List<RestaurantTable> Tables { get; set; } = new(); //restarant table can be many table such as Main = M1,M2,M3
}
