namespace WMS_UI_API.Models
{
    public class ShiftMaster
    {
    }

    public class ShiftMasterS
    {
        public int? Id { get; set; }
        public string Name { get; set; }
        public string Locked { get; set; }
        public string StartTime { get; set; }
        public string EndTime { get; set; }
    }

    public class ShiftMasterU
    {
        public string Name { get; set; }
        public string Locked { get; set; }
        public string StartTime { get; set; }
        public string EndTime { get; set; }
    }

}
