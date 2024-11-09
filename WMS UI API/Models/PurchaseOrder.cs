namespace WMS_UI_API.Models
{
    public class POHeader
    {
        public int DocEntry { get; set; }
        public int DocNum { get; set; }
        public string ObjType { get; set; }
        public string DocDate { get; set; }
        public string CardCode { get; set; }
        public string CardName { get; set; }
        public string U_LRNo { get; set; }
        public string FreightApplied { get; set; }
        public List<PODetail> poDet { get; set; }

    }

    public class PODetail
    {
        public int DocEntry { get; set; }
        public int DocNum { get; set; }
        public int LineNum { get; set; }
        public string ItemCode { get; set; }
        public string ItemName { get; set; }
        public double Qty { get; set; }
        public double ReceivedQty { get; set; }
        public double OpenQty { get; set; }
        public double Price { get; set; }
        public string ItemMngBy { get; set; }
        public string QRMngBy { get; set; }
        public string RecDate { get; set; }
        public int GateInNo { get; set; }
        public string Project { get; set; }
        public string UomCode { get; set; }
        public string WhsCode { get; set; }
    }

    public class PurchaseOrderFilter
    {
        public int BranchID { get; set; }
        public int Series { get; set; }
        public int DocNum { get; set; }
        public int GateInNo { get; set; }
        public string Canceled { get; set; }
    }

    public class GateINPO
    {
        public string ObjType { get; set; }
        public int BranchID { get; set; }
        public int GateInNo { get; set; }
        public int DocEntry { get; set; }
        public int LineNum { get; set; }
        public string ItemCode { get; set; }
        public string RecQty { get; set; }
        public string VehicleNo { get; set; }
        public string Transporter { get; set; }
        public string LRNo { get; set; }
        public string LRDate { get; set; }
        public string GRPOVendorRefNo { get; set; }
        public string GRPODocDate { get; set; }
        public string TolApplied { get; set; }
    }

    public class ValidPO
    {
        public string ObjType { get; set; }
        public int BranchID { get; set; }
        public int DocNum { get; set; }
        public int Series { get; set; }

    }

    public class GateINList
    {
        public int SrNo { get; set; }
        public int GateInNo { get; set; }
        public int DocEntry { get; set; }
        public int DocNum { get; set; }
        public int Series { get; set; }
        public int BranchID { get; set; }
        public string RecDate { get; set; }
        public string VehicleNo { get; set; }
        public string TransporterCode { get; set; }
        public string Canceled { get; set; }
    }


    public class GateINView
    {
        public int BranchId { get; set; }
        public int SrNo { get; set; }
        public int GateInNo { get; set; }
        public int DocEntry { get; set; }
        public long DocNum { get; set; }
        public int Series { get; set; }
        public string ItemCode { get; set; }
        public int LineNum { get; set; }
        public double RecQty { get; set; }
        public string RecDate { get; set; }
        public string Project { get; set; }
        public string WhsCode { get; set; }
        public string UoMCode { get; set; }
        public string VehicleNo { get; set; }
        public string TransporterCode { get; set; }
    }

    public class MultiplePOList
    {
        public int DocEntry { get; set; }
        public int DocNum { get; set; }
        public string DocDate { get; set; }
        public string CardCode { get; set; }
        public string CardName { get; set; }
        public string FreightApplied { get; set; }
        public string SAPEntryCount { get; set; }

    }

    public class DocList
    {
        public int BranchID { get; set; }
        public int Series { get; set; }
        public List<DocEntryList> DocEntryList { get; set; }
    }

    public class DocEntryList
    {
        public int DocEntry { get; set; }
    }

    public class POItems
    {
        public int Series { get; set; }
        public int DocEntry { get; set; }
        public int DocNum { get; set; }
        public string ObjType { get; set; }
        public string DocDate { get; set; }
        public string CardCode { get; set; }
        public string CardName { get; set; }
        public string ItemCode { get; set; }
        public string ItemName { get; set; }
        public int LineNum { get; set; }
        public double Qty { get; set; }
        public double ReceivedQty { get; set; }
        public double Price { get; set; }
        public double OpenQty { get; set; }
        public string ItemMngBy { get; set; }
        public string QRMngBy { get; set; }
        public string Project { get; set; }
        public string UomCode { get; set; }
        public string WhsCode { get; set; }
        public string FreightApplied { get; set; }
    }

    public class SaveGateINHeader
    {
        public int BranchID { get; set; }
        public string ObjType { get; set; }
        public string CardCode { get; set; }
        public int Series { get; set; }
        public int GateInNo { get; set; }
        public string VehicleNo { get; set; }
        public string Transporter { get; set; }
        public string LRNo { get; set; }
        public string LRDate { get; set; }
        public string GRPOVendorRefNo { get; set; }
        public string GRPODocDate { get; set; }
        public List<SaveGateINDetail> gateInDetails { get; set; }
    }

    public class SaveGateINDetail
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

    public class GPPayload
    {
        public int BranchID { get; set; }
        public int GateInNo { get; set; }

    }

    public class GP_GateInDetails
    {
        public int BranchID { get; set; }
        public int GateInNo { get; set; }
        public string GateInDate { get; set; }
        public List<GP_POHeader> gpPOHeader { get; set; }
    }


    public class GP_POHeader
    {
        public int DocEntry { get; set; }
        public int DocNum { get; set; }
        public int Series { get; set; }
        public string ObjType { get; set; }
        public string DocDate { get; set; }
        public string CardCode { get; set; }
        public string CardName { get; set; }
        public List<GP_PODetail> gpPODetail { get; set; }
    }

    public class GP_PODetail
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

    

    public class VehicleNoList
    {
        public string VehicleNo { get; set; }
    }

    public class TransporterList
    {
        public string Transporter { get; set; }
    }
}
