
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BeanScene.Models;

public class RestaurantTable
{
    public RestaurantTable(string number)
    {
       Number = number;
    }
    public int Id { get; set; }
    public string Number { get; set; }
     public List<Reservation> Reservations { get; set; } = new(); // restauranta table can be many reservation suchas , M1 can be many reservation in one day ,breakfast,lunch , dinner
}
