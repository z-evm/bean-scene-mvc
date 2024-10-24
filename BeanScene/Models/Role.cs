namespace BeanScene.Models
{
    public class Role
    {
        public Role(string rolename)
        {
            RoleName = rolename;
        }
        public int Id { get; set; }
        public string RoleName { get; set; }
    }
}
