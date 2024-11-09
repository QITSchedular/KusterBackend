namespace WMS_UI_API.Models
{
    public class InprocessQC
    {
    }

    public class RECPRDListFilter
    {
        public int BranchID { get; set; }

        public string? FromDate { get; set; }

        public string? ToDate { get; set; }

        public string HeaderQRCodeID { get; set; }

        public string? getAll { get; set; } = "N";
    }

    public class ProReceiptList
    {
        public string HeaderQRCodeID { get; set; }
        public int DocEntry { get; set; }
        public int DocNum { get; set; }
        public string? CardCode { get; set; }
        public string? CardName { get; set; }
        public string? NumAtCard { get; set; }
        public int Series { get; set; }
        public string SeriesName { get; set; }
        public string PostDate { get; set; }
        public string DocDate { get; set; }
        public string? Project { get; set; }
        public int ProOrdDocEntry { get; set; }
        public int ProOrdDocNum { get; set; }
        public string Comments { get; set; }

    }

    public class RECPRDItemFilter
    {

        public int BranchID { get; set; }

        public string HeaderQRCodeID { get; set; }

        public string DetailQRCodeID { get; set; }

        public int RECDocEntry { get; set; }
    }

    public class RECItem
    {

        public int ProOrdDocEntry { get; set; }

        public int ProOrdDocNum { get; set; }

        public string HeaderQRCodeID { get; set; }

        public string DetailQRCodeID { get; set; }
        public int RecNo { get; set; }

        public int RecDocEntry { get; set; }

        public int RecDocNum { get; set; }

        public string? CardCode { get; set; }

        public string? CardName { get; set; }

        public string? DocDate { get; set; }

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

    public class INPROQCPayload
    {

        public int BranchID { get; set; }
        public int RECDocEntry { get; set; }
        public string DetailQRCodeID { get; set; }
        public string FromWhs { get; set; }
        public int FromBin { get; set; }
        public string Action { get; set; }
        public double qty { get; set; }
        public string RejectComment { get; set; }
    }

    public class PRDList
    {

        public string QRCodeID { get; set; }

        public int ProOrdDocNum { get; set; }

        public int ProOrdDocEntry { get; set; }

        public string? CardCode { get; set; }

        public string? CardName { get; set; }

        public int Series { get; set; }

        public string? SeriesName { get; set; }
    }

    public class ValidateItemQRInputQC
    {
        public int BranchID { get; set; }
        public string DetailQRCodeID { get; set; }

    }

    public class InProcessQCPayloadNewFlow
    {
        public int BranchID { get; set; }
        public int ReceiptDocEntry { get; set; }
        public string DetailQRCodeID { get; set; }
        public string FromWhs { get; set; }
        public int FromBin { get; set; }
        public double ApprovedQty { get; set; }
        public double RejectedQty { get; set; }
        public string Comment { get; set; }
    }
}
