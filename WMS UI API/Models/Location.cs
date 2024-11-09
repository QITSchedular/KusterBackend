namespace WMS_UI_API.Models
{
    public class LocationIU
    {
        public int BranchID { get; set; }
        public string Location { get; set; }
        public string GSTIN { get; set; }
    }

    public class LocationS
    {
        public int BranchID { get; set; }
        public int Code { get; set; }
        public string Location { get; set; }
        public string GSTIN { get; set; }
    }
}
