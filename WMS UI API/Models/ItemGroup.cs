namespace WMS_UI_API.Models
{
    public class ItemGroupIU
    {
        public int BranchID { get; set; }
        public string ItmsGrpNam { get; set; }
        public string QRMngBy { get; set; }
        public string Locked { get; set; }
        
    }

    public class ItemGroupS
    {
        public int BranchID { get; set; }
        public int ItmsGrpCod { get; set; }
        public string ItmsGrpNam { get; set; }
        public string QRMngBy { get; set; }
        public string Locked { get; set; }

    }
}
