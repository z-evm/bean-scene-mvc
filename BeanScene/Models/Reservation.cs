using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace BeanScene.Models;
public class Reservation
{
    public Reservation(int pax, string status)
    {
        Pax = pax;
        Status = status;
    }
    public int Id { get; set; }
      public DateTime ReservationTime { get; set; }
    public int Pax { get; set; }
    public string Status { get; set; }
    public int PersonId { get; set; }
    public Person Person { get; set; } = default!;

    public int SittingId { get; set; }
    public Sitting Sitting { get; set; } = default!;

}
