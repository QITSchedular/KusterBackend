namespace WMS_UI_API.Models
{
    public class UOM
    { 
        public int BranchID {  get; set; }  
        public string UomCode { get; set; }
        public string UomName { get; set; }
        public string Locked { get; set; }
    }

    public class UOMUpdate
    {
        public int BranchID { get; set; }
        public string UomName { get; set; }
        public string Locked { get; set; }
    }
}
