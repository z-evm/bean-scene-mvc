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
    public int Duration { get; set; } = 120;
    public int Pax { get; set; }
    public string? Notes { get; set; }
    public DateTime? End { get; set; }
    public int PersonId { get; set; }
    public Person Person { get; set; } = default!;
    public int SittingId { get; set; }
    public Sitting? Sitting { get; set; } = default!;
    public int ReservationStatusId { get; set; }
    public ReservationStatus? ReservationStatus { get; set; }
    public List<RestaurantTable> Tables { get; set; } = new();
}
