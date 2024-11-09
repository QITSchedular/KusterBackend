namespace WMS_UI_API.Models
{
    public class ProductionOrder
    {
    }

    public class ProductionOrderList
    {
        public int DocEntry { get; set; }
        public int DocNum { get; set; }
        public int Series { get; set; }
        public string SeriesName { get; set; }
        public string Type { get; set; }
        public DateTime DueDate { get; set; }
        public string ItemCode { get; set; }
        public string ProdName { get; set; }
        public double PlannedQty { get; set; }  
        public string Comments { get; set; }
        public DateTime StartDate { get; set; }
        public int Priority { get; set; }
    }

    public class ProOrderItemcls
    {
        public List<ProOrderItem> proOrderItem { get; set; }
    }

    public class ProOrderItem
    {
        public int DocEntry { get; set; }
    }

    public class ProductionOrderItemList
    {
        public int DocNum { get; set; }
        public int LineNum { get; set; }
        public string ItemCode { get; set; }
        public string ItemName { get; set; }
        public int DocItemType { get; set; }
        public double IssuedQty { get; set; }
        public string WareHouse { get; set; }
        public int DocEntry { get; set; }
        public double PlannedQty { get; set; }
        public string Type { get; set; }
        public int ParentDocEntry { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string SeqNum { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
    }


    public class ProItemValidateCls
    {
        public int ProDocEntry { get; set; }
        public string DetailQRCodeID { get; set; }
    }

    public class ValidItemCls
    {
        public int DocEntry { get; set; }
        public int DocNum { get; set; }
        public int GateRecNo { get; set; }
        public string DetailQRCodeID { get; set; }
        public string ItemCode { get; set; }
        public string ItemName { get; set; }
        public int LineNum { get; set; }
        public string PlannedQty { get; set; }
        public string IssuedQty { get; set; }
        public double? IssQty { get; set; }
        public string UomCode { get; set; }
        public string Project { get; set; }
        public string ItemMngBy { get; set; }
        public string BatchSerialNo { get; set; }
        public string QRQty { get; set; }
        public string BatchWhsCode { get; set; }
        public string BatchAvailQty { get; set; }
        public string ProWhsCode { get; set; }
        public string DraftIssueQtyByQR { get; set; }
        public string DraftIssueQtyByQRPro { get; set; }
        public string QRProject { get; set; }
        public int FromBin { get; set; }
        public string FromBinCode { get; set; }
        public string ProOrdWiseTempQty { get; set; }
        public string ItemTempQty { get; set; }
    }

    public class ProductionDraftIssue
    {
        public int BranchID { get; set; }
        public int ProOrdDocEntry { get; set; }
        public int ProOrdDocNum { get; set; }
        public int Series { get; set; }
        public string Comment { get; set; }
        public List<PDI_Items> piItems { get; set; }
    }

    public class PDI_Items
    {
        public string ItemCode { get; set; }
        public int LineNum { get; set; }
        public string ItemMngBy { get; set; }
        public string UoMCode { get; set; }
        public string WhsCode { get; set; }
        public string Qty { get; set; }
        public List<PDIBatchSerial> piBatchSerial { get; set; }
    }

    public class PDIBatchSerial
    {
        public int DraftIssNo { get; set; }
        public string ItemCode { get; set; }
        public string DetailQRCodeID { get; set; }
        public int fromBinAbsEntry { get; set; }
        public string Project { get; set; }
        public string BatchSerialNo { get; set; }
        public string Qty { get; set; } 
    }


    public class ProductionIssue
    {
        public int BranchID { get; set; }
        public int ProOrdDocEntry { get; set; }
        public int ProOrdDocNum { get; set; } 
        public int Series { get; set; }
        public string Comment { get; set; }
        public List<PI_Items> piItems { get; set; }
    }

    public class PI_Items
    { 
        public string ItemCode { get; set; }
        public int LineNum { get; set; }
        public string ItemMngBy { get; set; }
        public string UoMCode { get; set; }
        public string WhsCode { get; set; }
        public string Qty { get; set; }
        public List<PIBatchSerial> piBatchSerial { get; set; }
    }


    public class PIBatchSerial
    {
        
        public string ItemCode { get; set; }
        public string DetailQRCodeID { get; set; }
        public int fromBinAbsEntry { get; set; }
        public string Project { get; set; }
        public string BatchSerialNo { get; set; }
        public string Qty { get; set; }
        public List<PIIssData> piIssData { get; set; }
    }

    public class PIIssData
    {
        public int DraftIssNo { get; set; }
        public string Qty { get; set; }
    }


    #region Class for Receipt
    public class ProductionOrderReceipt
    {
        public int DocEntry { get; set; }
        public int DocNum { get; set; }
        public string SeriesName { get; set; }
        public string Type { get; set; }
        public DateTime DueDate { get; set; }
        public string ItemCode { get; set; }
        public string ProdName { get; set; }
        public decimal Quantity { get; set; }
        public decimal PlannedQty { get; set; }
        public decimal CmpltQty { get; set; }
        public string Warehouse { get; set; }
        public string UomCode { get; set; }
        public string Comments { get; set; }
        public string BinActivat { get; set; }
        public string Project { get; set; }
        public decimal? ReceiptQty { get; set; }
        public string QARequired {  get; set; } 
    }

    public class ProductionReceipt
    {
        public int ProOrdDocEntry { get; set; }
        public int Series { get; set; }
        public string WhsCode { get; set; }
        public int BinAbsEntry { get; set; }  
        public string Project { get; set; }
        public string ReceiptQty { get; set; }
        public string Comment { get; set; }
        public List<ProReWork>? proReworkDet { get; set; }

    }

    public class ProReWork
    {
        public int deptId { get; set; }
        public double hours { get; set; }
        public string delay { get; set; }
    }

    public class DraftReceiptList
    {
        public int ProOrdDocEntry { get; set; }
        public int ProOrdDocNum { get; set; }
        public string ProOrdDocDate { get; set; }
        public string ItemCode { get; set; }
        public string ItemName { get; set; }
        public string PlannedQty { get; set; }
        public int Series { get; set; }
        public string Comments { get; set; }


    }

    public class DraftReceiptDetail
    {
        public int ProOrdDocEntry { get; set; }
        public int ProOrdDocNum { get; set; }
        public string ProOrdDocDate { get; set; }
        public int Series { get; set; }
        public string ItemCode { get; set; }
        public string ItemName { get; set; }
        public decimal PlannedQty { get; set; }
        public decimal ReceiptQty { get; set; }
        public string RecComment { get; set; }
        public string Comments { get; set; }
        public string ItemMngBy { get; set; }
        public string QRMngBy { get; set; }
        public int RecNo { get; set; }
        public string WhsCode { get; set; }
        public int BinAbsEntry { get; set; }
        public string Project { get; set; }
        public string UomCode { get; set; }
    }

    public class ProductionReceiptPro
    {
        public int BranchID { get; set; }
        public int ProOrdDocEntry { get; set; }
        public int ProOrdDocNum { get; set; }
        public int Series { get; set; }
        public int RecNo { get; set; }
        public string ItemCode { get; set; }
        public string ItemMngBy { get; set; }
        public string WhsCode { get; set; }
        public int BinAbsEntry { get; set; }
        public string ReceiptQty { get; set; }
        public string Project { get; set; }
        public string UomCode { get; set; }
        public string Comment { get; set; }
        public List<recBatchSerial> recDetails { get; set; }
    }

    public class recBatchSerial
    {
        public string DetailQRCodeID { get; set; }
        public string BatchSerialNo { get; set; }
        public string Qty { get; set; }
    }


    #endregion

    public class ValidDraftIssueItem
    {
        public int TransId { get; set; }
        public int IssNo { get; set; }
        public int TransSeq { get; set; }
        public int ProOrdDocEntry { get; set; }
        public int DocNum {  get; set; }
        public int Series { get; set; }
        public string QRCodeID { get; set; }
        public string ItemCode { get; set; }
        public int LineNum { get; set; }
        public string BatchSerialNo { get; set; }
        public string WhsCode { get; set; }
        public string UomCode { get; set; }
        public int FromBinAbsEntry { get; set; }
        public decimal IssQty { get; set; }
        public string Comment { get; set; }
        public DateTime EntryDate { get; set; }
        public string ItemMngBy { get; set; }
        public string QRMngBy { get; set; }
        public string Project { get; set; }
    }

    public class ProIssuedItems
    {
        public int TransSeq { get; set; }
        public int BaseDocEntry { get; set; }
        public long BaseDocNum { get; set; }
        public int DocEntry { get; set; }
        public long DocNum { get; set; }
        public string ItemCode { get; set; }
        public double IssuedQty { get; set; }
        public string QRCodeID { get; set; }
        public string BatchSerialNo { get; set; }
    }


    public class SaveTempProIssue
    {
        public int BranchID { get; set; }
        public int ProOrdDocEntry { get; set; }
        public int ProOrdDocNum { get; set; }
        public string ItemCode { get; set; }
        public string DetailQRCodeID { get; set; }
        public string Qty { get; set; }

    }

    public class DeleteTempProIssue
    {
        public int BranchID { get; set; }
        public int ProOrdDocEntry { get; set; }
        public string ItemCode { get; set; }
        public string DetailQRCodeID { get; set; } 

    }

}
