namespace WMS_UI_API.Models
{
    public class PickList
    {
        public int BranchID { get; set; }
        public int SODocEntry { get; set; }
        public int SODocNum { get; set; }
        public string Comment { get; set; }
        public List<PickList_Items> plItems { get; set; }
    }

    public class PickList_Items
    {
        public string ItemCode { get; set; }
        public int LineNum { get; set; }
        public string WhsCode { get; set; }
        public string ItemMngBy { get; set; }
        public string UoMCode { get; set; }
        public string TotalQty { get; set; }
        public List<PLBatchSerial> plBatchSerial { get; set; }
    }

    public class PLBatchSerial
    {
        public string DetailQRCodeID { get; set; }
        public string BatchSerialNo { get; set; }
        public string Qty { get; set; }
        public int BinAbsEntry { get; set; }
        public int GateRecNo { get; set; }
     
    }


    public class PickListValidItem
    {
        public int DocEntry { get; set; }
        public int DocNum { get; set; }
        public int LineNum { get; set; }
        public string CardCode { get; set; }
        public string CardName { get; set; }
        public string ItemCode { get; set; }
        public string ItemName { get; set; }
        public string ItemMngBy { get; set; }
        public string WhsCode { get; set; }
        public string QRCodeID { get; set; }
        public string BatchSerialNo { get; set; }  
        public int  GateRecNo { get; set; } 
        public string Project { get; set; }
        public string UomCode { get; set; }       
        public double OrderedQty { get; set; }
        public double QRQty { get; set; }
        public double AvailQtyInWhs { get; set; }
        public double CanReleaseQty { get; set; }
        
    }
}
