using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace BeanScene.Models;
public class Reservation
{
    public int Id { get; set; }
    public DateTime Start { get; set; }
    
    public int Duration {get;set;} //duration of the reservation in minutes

    public int Pax { get; set; } // Number of guests for the reservation
    public string? Notes { get; set; } // Additional notes for the reservatio

    public DateTime? End  {get;set;}

    //navigation prop

    public int PersonId { get; set; }
    public Person Person { get; set; } = new Person();
    public int SittingId { get; set; }
    public Sitting? Sitting { get; set; } = default!;

   public int ReservationStatusId { get; set; }
    public ReservationStatus? ReservationStatus { get; set; }

    //automatically resolved many to many with RestaurantTable collection
    public List<RestaurantTable> Tables { get; set; } = new();

}
