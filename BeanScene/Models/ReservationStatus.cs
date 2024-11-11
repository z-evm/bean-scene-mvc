namespace BeanScene.Models
{
    public class ReservationStatus
    {
        public int Id { get; set; }

        public string Name { get; set; }="Pending"; // pending , approve , seated, finish 

        public List<Reservation> Reservations { get; set; } = new();  // one status can be many  reservation like ,  in one time can be many pending reservation or approved 
    }
}
