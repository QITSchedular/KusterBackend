namespace WMS_UI_API.Models
{
    public class Warehouse
    {
        public int BranchID { get; set; }
        public string WhsCode { get; set; }
        public string WhsName { get; set; }
        public int LocCode { get; set; }
        public string Location { get; set; }
        public string Locked { get; set; }
        public string BinActivat { get; set; }
        public string ObjType { get; set; }
    }

    public class WarehouseSave
    {
        public int BranchID { get; set; }   
        public string WhsCode { get; set; }
        public string WhsName { get; set; }
        public int LocCode { get; set; } 
        public string Locked { get; set; }
        public string BinActivat { get; set; } 
    }

    public class WarehouseUpdate
    {
        public int BranchID { get; set; }
        public string WhsName { get; set; }
        public int LocCode { get; set; }
        public string Locked { get; set; }
        public string BinActivat { get; set; }
    }
}
