namespace WMS_UI_API.Models
{
    public class ItemS
    {
        public string ItemCode { get; set; }
        public string ItemName { get; set; }
        public short ItmsGrpCod { get; set; }
        public string ItmsGrpNam { get; set; }
        public short ItmsSubGrpCod { get; set; }
        public string ItmsSubGrpNam { get; set; } 
        public string UomCode { get; set; }
        public string UomName { get; set; }
        public string QRMngBy { get; set; }
        public string QRMngByName { get; set; }
        public string ItemMngBy { get; set; }
        public string ItemMngByName { get; set; }
        public string IsActive { get; set; }
        public string QRCodeId { get; set; }
        public int AtcEntry { get; set; }
        public string ObjType { get; set; }
    }


    public class ItemInsert
    {
        public int BranchID { get; set; }
        public string ItemCode { get; set; }
        public string ItemName { get; set; }
        public short ItmsGrpCod { get; set; }
        public short ItmsSubGrpCod { get; set; }
        public string UomCode { get; set; }
        public string QRMngBy { get; set; }
        public string ItemMngBy { get; set; }
        public string IsActive { get; set; }
        public int AtcEntry { get; set; }

    }


    public class ItemUpdate
    {
        public int BranchID { get; set; }
        public string ItemName { get; set; }
        public short ItmsGrpCod { get; set; }
        public short ItmsSubGrpCod { get; set; }
        public string UomCode { get; set; }
        public string QRMngBy { get; set; }
        public string ItemMngBy { get; set; }
        public string IsActive { get; set; }
        public int AtcEntry { get; set; }

    }

}
