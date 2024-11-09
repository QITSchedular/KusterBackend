namespace WMS_UI_API.Models
{
    public class SupplierGateIN
    {
    }

    public class POHelp
    {
        public int DocEntry { get; set; }
        public int DocNum { get; set; }
        public string DocDate { get; set; }
        public int Series { get; set; }
        public string SeriesName { get; set; }
    }

    public class SDocList
    {
        public int BranchID { get; set; }
        public int Series { get; set; }
        public string CardCode { get; set; }
        public List<SDocEntryList> DocEntryList { get; set; }
    }

    public class SDocEntryList
    {
        public int DocEntry { get; set; }
    }


    public class SaveVendorGateIN
    {
        public int BranchID { get; set; }
        public string ObjType { get; set; }
        public int Series { get; set; }
        public string EntryUser { get; set; }
        public List<SaveVendorGateINDetail> gateInDetails { get; set; }
    }

    public class SaveVendorGateINDetail
    {
        public int DocEntry { get; set; }
        public int DocNum { get; set; }
        public int LineNum { get; set; }
        public string ItemCode { get; set; }
        public string Project { get; set; }
        public string UomCode { get; set; }
        public string WhsCode { get; set; }
        public string RecQty { get; set; }
        public string TolApplied { get; set; }
    }

    public class SPayload
    {
        public int BranchID { get; set; }
        public int GateInNo { get; set; }
        public string CardCode { get; set; }

    }


    public class S_GateInDetails
    {
        public int BranchID { get; set; }
        public int VendorGateInNo { get; set; }
        public string GateInDate { get; set; }
        public List<S_POHeader> sPOHeader { get; set; }
    }


    public class S_POHeader
    {
        public int DocEntry { get; set; }
        public int DocNum { get; set; }
        public int Series { get; set; }
        public string ObjType { get; set; }
        public string DocDate { get; set; }
        public string CardCode { get; set; }
        public string CardName { get; set; }
        public List<S_PODetail> sPODetail { get; set; }
    }

    public class S_PODetail
    {
        public int DocEntry { get; set; }
        public int LineNum { get; set; }
        public string ItemCode { get; set; }
        public string ItemName { get; set; }
        public decimal Qty { get; set; }
        public decimal OpenQty { get; set; }
        public decimal Price { get; set; }
        public string ItemMngBy { get; set; }
        public string QRMngBy { get; set; }
        public string RecDate { get; set; }
        public string Project { get; set; }
        public string UomCode { get; set; }
        public string WhsCode { get; set; }

    }

    public class SCheckDetailPO
    {
        public string BranchID { get; set; }
        public string CardCode { get; set; }
        public int DocEntry { get; set; }
        public int DocNum { get; set; }
        public int Series { get; set; }
        public string ObjType { get; set; }
        public string ItemCode { get; set; }
        public int LineNum { get; set; }
        public int VendorGateInNo { get; set; }
    }

    public class SSaveDetailQR
    {
        public string BranchID { get; set; }
        public string CardCode { get; set; }
        public int DocNum { get; set; } = 0;
        public string HeaderQRCodeID { get; set; }
        public string DetailQRCodeID { get; set; }
        public string IncNo { get; set; }
        public string ItemCode { get; set; }
        public string ItemName { get; set; } = string.Empty;
        public int LineNum { get; set; }
        public string QRMngBy { get; set; }
        public string Qty { get; set; }
        public int VendorGateInNo { get; set; }
        public string Remark { get; set; }
        public string Project { get; set; } = string.Empty;
        public string BatchSerialNo { get; set; }
        public string DefaultBin { get; set; } = string.Empty;
    }


    public class SCheckAllDetailPO
    {
        public string BranchID { get; set; }

        public int VendorGateInNo { get; set; }
    }

    public class SCheckHeaderPO
    {
        public string BranchID { get; set; }
        public string CardCode { get; set; }
        public int DocEntry { get; set; }
        public int DocNum { get; set; }
        public int Series { get; set; }
        public string ObjType { get; set; }

    }


    public class SSaveHeaderQR
    {
        public string BranchID { get; set; }
        public string QRCodeID { get; set; }
        public string CardCode { get; set; }
        public int DocEntry { get; set; }
        public int DocNum { get; set; }
        public int Series { get; set; }
        public string DocDate { get; set; }
        public string ObjType { get; set; }
        public string IncNo { get; set; }
    }


    public class HeaderQRData
    {
        public int DocEntry { get; set; }
        public int DocNum { get; set; }
        public int Series { get; set; }
        public string SeriesName { get; set; }
        public string CardCode { get; set; }
        public string CardName { get; set; }
        public string QRCodeID { get; set; }
        public int GateInNo { get; set; }
        public int ItemCount { get; set; }
        public string TotalRecQty { get; set; }
    }

    public class ValidatePOItemQR
    {
        public int BranchId { get; set; }
        public int GateInNo { get; set; }
        public int PODocEntry { get; set; }
        public string HeaderQRCodeID { get; set; }
        public string DetailQRCodeID { get; set; }
    }


    public class ValidPOItemQRData
    {
        public int BranchID { get; set; }
        public int DocEntry { get; set; }
        public int DocNum { get; set; }
        public string HeaderQRCodeId { get; set; }
        public string DetailQRCodeId { get; set; }
        public int GateInNo { get; set; }
        public int Series { get; set; }
        public string DocDate { get; set; }
        public string CardCode { get; set; }
        public string CardName { get; set; }
        public string ItemCode { get; set; }
        public string ItemName { get; set; }
        public int LineNum { get; set; }
        public string OrderedQty { get; set; }
        public string RecQty { get; set; }
        public string Price { get; set; }
        public string ItemMngBy { get; set; }
        public string QRMngBy { get; set; }
        public string Project { get; set; }
        public string UomCode { get; set; }
        public string WhsCode { get; set; }
        public string FreightApplied { get; set; }
    }


    public class UpdateVendorGateIN
    {
        public int BranchID { get; set; }
        public int GateInNo { get; set; }
        public string VehicleNo { get; set; }
        public string Transporter { get; set; }
        public string LRNo { get; set; }
        public string LRDate { get; set; }
        public string GRPOVendorRefNo { get; set; }
        public string GRPODocDate { get; set; }
    }

}
