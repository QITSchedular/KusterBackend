namespace WMS_UI_API.Models
{
    public class Process
    {
        public int? ID { get; set; }
        public string Name { get; set; }
        public string Locked { get; set; }
    }

    public class ProcessU
    {
        public string Name { get; set; }
        public string Locked { get; set; }
    }
}
