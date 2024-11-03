using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace BeanScene.Models;

public class Sitting
{
    public Sitting(DateTime startTime, DateTime endTime)
    {
        StartTime = startTime;
        EndTime = endTime;
    }
    public int Id { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public int RestaurantTableId { get; set; }
    public RestaurantTable RestaurantTable { get; set; } = default!;
}