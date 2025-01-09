
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
    public List<Reservation> Reservations { get; set; } = new();

    public bool IsAvailable(DateTime start)
    {
        DateTime end = start.AddHours(2);

        return Reservations.All(r => r.End <= start || r.Start >= end);
    }
}
