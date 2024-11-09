namespace WMS_UI_API.Models
{
    public class MachineMasterS
    {
        public string? MachineID { get; set; }
        public string MachineSrNo { get; set; }
        public int MachineNo { get; set; }
        public string MachineName { get; set; }
        public string MachineSpec { get; set; }
        public string Make { get; set; }
        public string Model { get; set; }
        public string Location { get; set; }
        public int MachinetypeID { get; set; }
    }

    public class MachineMasterU
    {
        public string MachineSrNo { get; set; }
        public int MachineNo { get; set; }
        public string MachineName { get; set; }
        public string MachineSpec { get; set; }
        public string Make { get; set; }
        public string Model { get; set; }
        public string Location { get; set; }
        public int MachinetypeID { get; set; }
    }

    public class MachineMasterG
    {
        public string? MachineID { get; set; }
        public string MachineSrNo { get; set; }
        public int MachineNo { get; set; }
        public string MachineName { get; set; }
        public string? MachineSpec { get; set; }
        public string Make { get; set; }
        public string Model { get; set; }
        public string Location { get; set; }
        public string MachineTypeName { get; set; }
    }
}
