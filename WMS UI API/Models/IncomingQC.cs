using SAPbobsCOM;

namespace WMS_UI_API.Models
{
    public class IncomingQC
    {
    }

    public class GRPOListFilter
    {
        public int BranchID { get; set; }
        public string FromDate { get; set; }
        public string ToDate { get; set; }
        public string HeaderQRCodeID { get; set; }
        public string getAll { get; set; } = "N";
    }

    public class GRPOList
    {
        public string HeaderQRCodeID { get; set; }
        public int DocEntry { get; set; }
        public int DocNum { get; set; }
        public string CardCode { get; set; }
        public string CardName { get; set; }
        public string NumAtCard { get; set; }
        public int Series { get; set; }
        public string SeriesName { get; set; }
        public string PostDate { get; set; }
        public string DocDate { get; set; }
        public string Project { get; set; }
        public int PODocEntry { get; set; }
        public int PODocNum { get; set; }
        public string Comments { get; set; }
        public string GRPOQtyCount { get; set;}
        public string QCQty { get; set; }
        public string Status { get; set; }

    }

    public class GRPOItemFilter
    {
        public int BranchID { get; set; }
        public string HeaderQRCodeID { get; set; }
        public string DetailQRCodeID { get; set; }
        public int GRPODocEntry { get; set; }

    }

    public class GRPOItem
    {
        public int PODocEntry { get; set; }
        public string PODocNum { get; set; }
        public string HeaderQRCodeID { get; set; }
        public string DetailQRCodeID { get; set; }
        public int GateInNo { get; set; }
        public int GRPODocEntry { get; set; }
        public string GRPODocNum { get; set; }
        public string CardCode { get; set; }
        public string CardName { get; set; }
        public string DocDate { get; set; }
        public string ItemCode { get; set; }
        public string ItemName { get; set; }
        public double RecQty { get; set; }
        public double QRQty { get; set; }
        public double QCQty { get; set; }
        public string Project { get; set; }
        public string UomCode { get; set; }
        public string WhsCode { get; set; }
        public string FromWhs { get; set; }
        public int FromBin { get; set; }
        public string FromBinCode { get; set; }

    }

    public class QCPayload
    {
        public int BranchID { get; set; }
        public int GRPODocEntry { get; set; }
        public string DetailQRCodeID { get; set; }
        public string FromWhs { get; set; }
        public int FromBin { get; set; }
        public string Action { get; set; }
        public double qty { get; set; }
        public string Comment { get; set; }
    }

    public class QCPayloadNewFlow
    {
        public int BranchID { get; set; }
        public int GRPODocEntry { get; set; }
        public string DetailQRCodeID { get; set; }
        public string FromWhs { get; set; }
        public int FromBin { get; set; }
        public double ApprovedQty { get; set; }
        public double RejectedQty { get; set; }
        public string Comment { get; set; }
    }

    public class POList
    {
        public string QRCodeID { get; set; }
        public int PODocNum { get; set; }
        public int PODocEntry { get; set; }
        public string CardCode { get; set; }
        public string CardName { get; set; }
        public string NumAtCard { get; set; }
        public int Series { get; set; }
        public string SeriesName { get; set; }
    }

    public class ValidateItemQRInput
    {
        public int BranchID { get; set; }        
        public string DetailQRCodeID { get; set; }       

    }
}
