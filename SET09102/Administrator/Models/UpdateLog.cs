namespace SET09102.Models
{
    public class UpdateLog
    {
        public int Id { get; set; }
        public int SensorId { get; set; }
        public string UpdateType { get; set; }
        public DateTime Timestamp { get; set; }
        public bool Success { get; set; }
    }
}