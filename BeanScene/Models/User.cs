namespace BeanScene.Models
{
    public class User
    {
        public int Id { get; set; }
        public int PersonId { get; set; }
        public Person Person { get; set; } = default!;
    }
}
