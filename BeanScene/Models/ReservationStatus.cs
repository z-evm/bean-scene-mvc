namespace BeanScene.Models
{
    public class ReservationStatus
    {
        public int Id { get; set; }
        public string Name { get; set; }="Pending";
        public List<Reservation> Reservations { get; set; } = new();
    }
}
