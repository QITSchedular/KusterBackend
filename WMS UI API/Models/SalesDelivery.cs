namespace WMS_UI_API.Models
{
    public class SalesDelivery
    {
        public int BranchID { get; set; }
        public int SODocEntry { get; set; }
        public int SODocNum { get; set; }
        public int Series { get; set; }
        public string WhsCode { get; set; }
        public int BinAbsEntry { get; set; }
        public string Comment { get; set; }
        public List<SD_Items> sdItems { get; set; }
    }

    public class SD_Items
    {
        public string ItemCode { get; set; }
        public int LineNum { get; set; }
        public string UoMCode { get; set; }
        public string ItemMngBy { get; set; }
        public string TotalQty { get; set; }        
        public List<SDBatchSerial> sdBatchSerial { get; set; }
    }
        
    public class SDBatchSerial
    {
        public int poProDocEntry { get; set; }
        public int poProDocNum { get; set; }
        public string poProObjType { get; set; }
        public int GateRecNo { get; set; }
        public string ItemCode { get; set; }
        public string DetailQRCodeID { get; set; }
        public string BatchSerialNo { get; set; }
        public string Qty { get; set; }
    }

    public class SalesOrderList
    {
        public int DocEntry { get; set; }
        public int DocNum { get; set; }
        public int Series { get; set; }
        public string DocDate { get; set; }
        public string CardCode { get; set; }
        public string CardName { get; set; }
        public string? NumAtCard { get; set; }
        public string? Comments { get; set; }
    }

    public class SOItemValidateCls
    {
        public int BranchID { get; set; }
        public int SODocEntry { get; set; }
        public string DetailQRCodeID { get; set; }
    }

    public class SOItemData
    {
        //public int PickDocEntry { get; set; }
        //public int PickDocNum { get; set; } 
        //public string QRCodeId { get; set; }
        //public string BatchSerialNo { get; set; }
        public int SODocEntry { get; set; }
        public string SODocNum { get; set; }
        public int SOObjType { get; set; }
        public DateTime SODocDate { get; set; }
        public string CardCode { get; set; }
        public string CardName { get; set; }
        public int LineNum { get; set; }
        public string ItemCode { get; set; }
        public string ItemName { get; set; }
        public string UomCode {  get; set; }    
        public string QRQty { get; set; }
        public string OrderedQty { get; set; }
        public string QtyInWhs { get; set; }
        public string SOQRItemPrevDeliver { get; set; }
        public string SOItemPrevDeliver { get; set; }
        public string BaseObjType { get; set; }
        public int BaseDocEntry { get; set; }
        public int BaseDocNum { get; set; }
        public int GateRecNo { get; set; }
        public string QRCodeID { get; set; }
        public string BatchSerialNo { get; set; }        
        public string WhsCode { get; set; }
        public string ItemMngBy { get; set; }
        public string QRMngBy { get; set; }

    }


    public class PickedItemsList
    {
        public int BranchID { get; set; }
        public int SODocEntry { get; set; }
        public int SODocNum { get;set; }
        public int PickNo { get; set;}
        public string ItemCode { get; set; }
        public string Qty { get; set; } 
        public string UoMCode { get; set; }
        public string Remark { get; set; }
    }
}
