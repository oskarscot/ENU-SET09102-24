namespace SET09102.Administrator.Models
{
    public class Role
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public List<string> Permissions { get; set; } = new List<string>();
    }
}