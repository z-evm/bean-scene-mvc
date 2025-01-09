using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace BeanScene.Models;

public enum SittingType 
{
    Breakfast,
    Lunch,
    Dinner,
    SpecialEvent
}
public class Sitting 
{
        public int Id { get; set; }
        public string Name { get; set; }=default!; //"Friday Dinner" ,Saturday Brunch
        public DateTime Start { get; set; }  // Breakfast Start time  like  07 :30
        public DateTime End { get; set; }  // Breakfast Finish Time like 11:00
        public int Capacity { get; set; } // Dinner we have 40 people capacity 
        public bool Closed { get; set; }  // This  is marked as closed
        public int RestaurantId { get; set; }
        public Restaurant? Restaurant { get; set; }
        public SittingType Type { get; set; }//Breakfast .Lunch ,Dinner,Special Event
        public List<Reservation> Reservations { get; set; } = new();  //Sitting can be many reservation
        public bool IsAvailable(DateTime start, DateTime end, int guests)
        {
            var isAvailable = Reservations.All(r => r.End <= start || r.Start >= end);
            Console.WriteLine($"Sitting availability checked for {start} to {end} with {guests} guests. Available: {isAvailable}");

            return Reservations.All(r => r.End <= start || r.Start >= end);
        }
}
