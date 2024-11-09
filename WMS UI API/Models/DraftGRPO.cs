namespace WMS_UI_API.Models
{
    public class DraftGRPO
    {
        public int BranchID { get; set; }
        public int Series { get; set; }
        public string CardCode { get; set; }
        public string Comments { get; set; }
        public string QAWhsCode { get; set; }
        public int QABinAbsEntry { get; set; }
        public string NonQAWhsCode { get; set; }
        public int NonQABinAbsEntry { get; set; }
        public int GateInNo { get; set; }
        public List<PODetails> poDet { get; set; }
        public List<FreightDetails> FreightDet { get; set; }
    }

    public class PODetails
    {
        public int DocEntry { get; set; }
        public int DocNum { get; set; }
        public List<DraftGRPODet> grpoDet { get; set; }
    }

    public class DraftGRPODet
    {
        public string ItemCode { get; set; }
        public string LineNum { get; set; }
        public string QARequired { get; set; }
        public string UoMCode { get; set; } 
        public string FromWhs { get; set; }
        public string ItemMngBy { get; set; }
        public string TaxCode { get; set; }
        public string Qty { get; set; }
        public string Price { get; set; }
        public string TolApplied { get; set; }     
        public string PurchaseUnit { get; set; }
        public List<DraftGRPOBatchSerial> grpoBatchSerial { get; set; }
    }

    public class DraftGRPOBatchSerial
    {

        public string ItemCode { get; set; }
        public string DetailQRCodeID { get; set; }
        public string BatchSerialNo { get; set; }
        public string Project { get; set; }
        public string Qty { get; set; }

    }

    public class FreightDetails
    {
        public int ExpnsCode { get; set; } 
        public double Amount { get; set; }
    }

    public class GetPOList
    {
        public int DocEntry { get; set; }
        public int DocNum { get; set; }
        public int Series { get; set; }
        public string SeriesName { get; set; }
        public string DocDate { get; set; } 
        public string CardCode { get; set; }
        public string CardName { get; set; }
        public string NumAtCard { get; set; }
        public string QRCodeID { get; set; }
        public double RecQty { get; set; }
        public string FreightApplied { get; set; }
        public string GRPOVendorRefNo { get; set; }
        public string GRPODocDate { get; set; }
        public string VehicleNo { get; set; }
        public string Transporter { get; set; }
        public string LRNo { get; set; }
        public string LRDate { get; set; }
    }

    public class ValidateItemQR
    {
        public int BranchID { get; set; }
        public int GateInNo { get; set; }
        public int PODocEntry { get; set; } 
        public string DetailQRCodeID { get; set; }

    }

    public class ValidQRData
    {
        public int DocEntry { get; set; }
        public int LineNum { get; set; }
        public int DocNum { get; set; }
        public string DocDate { get; set; }
        public string CardCode { get; set; }
        public string CardName { get; set; }
        public string HeaderQRCodeID { get; set; }
        public int GateInNo { get; set; }
        public string GateInDate { get; set; }
        public string ItemCode { get; set; }
        public string ItemName { get; set; }
        public string ItemMngBy { get; set; }
        public string QRMngBy { get; set; }
        public string DetailQRCodeID { get; set; }
        public string BatchSerialNo { get; set; }
        public string Qty { get; set; }
        public string Project { get; set; }
        public string TaxCode { get; set; }
        public string Remark { get; set; }
        public string OrderQty { get; set; }
        public string RecQty { get; set; }
        public string WhsCode { get; set; }
        public string UoMCode { get; set; }
        public string QARequired { get; set; }
        public string LRNo { get; set; }
        public string LRDate { get; set; }
        public string VehicleNo { get; set; }
        public string TransporterCode { get; set; }
        public string GRPOVendorRefNo { get; set; }
        public string GRPODocDate { get; set; }
        public string Price { get; set; }
        public string TolApplied { get; set; }
        public string PurchaseUnit { get; set; }
    }

    public class ApprovalPendingGRPO
    {
        public int PODocEntry { get; set; }
        public int PODocNum { get; set; }
        public string PODocDate { get; set; }
        public string POTaxDate { get; set; }
        public string POComment { get; set; }
        public List<ApprovalPendingGRPODet> pendingGrpo { get; set; }

    }

    public class ApprovalPendingGRPODet
    {
        public int DDocEntry { get; set; }
        public int DDocNum { get; set; }
        public int DSeries { get; set; }
        public string DComment { get; set; }
        public string DDocDate { get; set; }
        public string DCardCode { get; set; }
        public string DCardName { get; set; }
        public string DNumAtCard { get; set; }
        public string DProject { get; set; }
        public List<ApprovalPendingGRPODetDet> pendingGrpoDet { get; set; }
    }

    public class ApprovalPendingGRPODetDet
    {
        public int DDocEntry { get; set; }
        public int DLineNum { get; set; }
        public string DItemCode { get; set; }
        public string DItemName { get; set; }
        public string DQuantity { get; set; }

    }

    public class GateInNoList
    {
        public int GateInNo { get; set; }
    }


    public class FreightCategory
    {
        public int ExpnsCode { get; set; }
        public string ExpnsName { get; set; }
        public string Amount { get; set; }
    }
}
