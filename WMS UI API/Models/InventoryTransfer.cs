namespace WMS_UI_API.Models
{
    public class InventoryTransfer
    {
    }

    public class ValidateItemInIT
    {
        public int BranchID { get; set; }
        public string FromWhs { get; set; }
        public int? FromBinAbsEntry { get; set; }
        public string DetailQRCodeID { get; set; }
    }

    public class ValidItemData
    {
        public int BranchID { get; set; }
        public string DetailQRCodeID { get; set; }
        public string ItemCode { get; set; }
        public string ItemName { get; set; }
        public string BatchSerialNo { get; set; }
        public string Whs { get; set; }
        public string Qty { get; set; }
        public string TransQty { get; set; }
        public string CardCode { get; set; }
        public string CardName { get; set; }
        public int GateInNo { get; set; }
        public string Project { get; set; }
        public string UoMCode { get; set; }
        public string ItemMngBy { get; set; }
        public string ItemWhsStock { get; set; }
        public string ItemStock { get; set; }

    }

    public class IT
    {
        public int BranchID { get; set; }
        public int Series { get; set; }
        public string CardCode { get; set; }
        //public string FromWhsCode { get; set; }
        public string ToWhsCode { get; set; }
        public int ToBinAbsEntry { get; set; }
        public string? FromObjType { get; set; }
        public string? ToObjType { get; set; }
        //public int FromBinAbsEntry { get; set; }
        public string FromIT { get; set; } = "Y";
        public string Comments { get; set; }
        public List<ITDetails> itDetails { get; set; }
    }

    public class ITDetails
    {
        public string ItemCode { get; set; }
        public double TotalItemQty { get; set; }
        public string Project { get; set; }
        public string Reason { get; set; }
        public string ItemMngBy { get; set; }
        public List<ITQRDetails> itQRDetails { get; set; }
    }

    public class ITQRDetails
    {
        public int GateInNo { get; set; }
        public string DetailQRCodeID { get; set; }
        public string BatchSerialNo { get; set; }
        public string FromWhsCode { get; set; }
        public int FromBinAbsEntry { get; set; }
        public double Qty { get; set; }
    }

    public class ScanQR
    {
        public int BranchId { get; set; }
        public string QRCodeID { get; set; }
    }

    public class AvailableQRWhs
    {
        public string WhsCode { get; set; }
        public string WhsName { get; set; }
        public string BinActivat { get; set; }
        public int? BinAbsEntry { get; set; }
        public string BinCode { get; set; }
        public string Stock { get; set; }
    }

    public class ITNewFlow
    {
        public int BranchID { get; set; }
        public int Series { get; set; }
        public string CardCode { get; set; }
        public string? FromObjType { get; set; }
        public string? ToObjType { get; set; } 
        public string FromIT { get; set; } = "Y";
        public string Comments { get; set; }
        public List<ITDetailsNewFlow> itDetails { get; set; }
    }

    public class ITDetailsNewFlow
    {
        public string ItemCode { get; set; }
        public double TotalItemQty { get; set; }
        public string Project { get; set; }
        public string Reason { get; set; }
        public string ItemMngBy { get; set; }
        public List<ITQRDetailsNewFlow> itQRDetails { get; set; }
    }

    public class ITQRDetailsNewFlow
    {
        public int GateRecNo { get; set; }
        public string DetailQRCodeID { get; set; }
        public string BatchSerialNo { get; set; }
        public string FromWhsCode { get; set; }
        public int FromBinAbsEntry { get; set; }
        public string ToWhsCode { get; set; }
        public int ToBinAbsEntry { get; set; }
        public double Qty { get; set; }
    }
}
