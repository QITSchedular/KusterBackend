namespace WMS_UI_API.Models
{
    public class ItemSubGroup
    {
        public int BranchID { get; set; }
        public short ItmsGrpCod { get; set; }
        public string ItmsGrpNam { get; set; }
        public short ItmsSubGrpCod { get; set; }
        public string ItmsSubGrpNam { get; set; }
        public string Locked { get; set; }
    }

    public class ItemSubGroupSave
    {
        public int BranchID { get; set; }
        public short ItmsGrpCod { get; set; }
        public string ItmsSubGrpNam { get; set; }
        public string Locked { get; set; }
    }

    public class ItemSubGroupUpdate
    {
        public int BranchID { get; set; }
        public short ItmsGrpCod { get; set; }  
        public string ItmsSubGrpNam { get; set; }
        public string Locked { get; set; }

    }

    public class FillItemSubGroup
    {
        public int BranchID { get; set; }
        public short ItmsSubGrpCod { get; set; }
        public string ItmsSubGrpNam { get; set; }

    }
}
