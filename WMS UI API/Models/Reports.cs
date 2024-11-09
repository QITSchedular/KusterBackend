namespace WMS_UI_API.Models
{
    public class Reports
    {
    }

    public class GateInDetails
    {
        public int BranchID { get; set; }
        public string FromDate { get; set; }
        public string ToDate { get; set; }
        public int PODocEntry { get; set; }
    }

    public class GateInDetailsReport
    {
        public int DocEntry { get; set; }
        public int DocNum { get; set; }
        public int GateInNo { get; set; }
        public DateTime GateInDate { get; set; }
        public double GateInQty { get; set; }
        public double OrderQty { get; set; }
        //public double OpenQty { get; set; }
        public string ItemCode { get; set; }
        public string ItemName { get; set; }
        public string Project { get; set; }
        public string UomCode { get; set; }
        public string VehicleNo { get; set; }
        public string TransporterCode { get; set; }
        public string? LRNo { get; set; }
        public string invoice_no { get; set; }
        public DateTime? LRDate { get; set; }
        public DateTime invoice_date { get; set; }
        public string vendorCode { get; set; }
        public string vendorName { get; set; }

    }

    public class ItemWiseQRWiseStock
    {
        public int BranchID { get; set; }
        public string ItemCode { get; set; }
        public string Project { get; set; }
    }

    public class ItemWiseQRWiseStockReport
    {
        public string ItemCode { get; set; }
        public string ItemName { get; set; }
        public string QRCodeID { get; set; }
        public string Project { get; set; }
        public string WhsCode { get; set; }
        public string WhsName { get; set; }
        public string BinCode { get; set; }
        public string Stock { get; set; }
    }

    public class QRWiseStock
    {
        public int BranchID { get; set; }
        public string ItemCode { get; set; }
        public string Project { get; set; }
        public int? PODocEntry { get; set; }
        public int? PRODocEntry { get; set; }
    }

    public class ProList
    {
        public int DocEntry { get; set; }
        public int DocNum { get; set; }
        public string ItemCode { get; set; }
        public string ItemName { get; set; }

    }

    public class ProOrd
    {
        public int DocEntry { get; set; }
        public int DocNum { get; set; } 
        public string ItemCode { get; set; }
        public string ItemName { get; set; }

    }

    public class ProItems
    {
        public int DocEntry { get; set; }
        public int DocNum { get; set; }
        public int LineNum { get; set; }
        public string ItemCode { get; set; }
        public string ItemType { get; set; }
        public string ItemName { get; set; }

    }

    public class PoIList
    {
        public int DocEntry { get; set; }
        public int DocNum { get; set; }
        public string CardCode { get; set; }
        public string CardName { get; set; }

    }

    public class GRNDetailsReport
    {
        public int PO_DocEntry { get; set; }
        public int PO_DocNum { get; set; }
        public DateTime PO_DocDate { get; set; }
        public int GateInNo { get; set; }
        public DateTime GateIn_Date { get; set; }
        public double GateInQty { get; set; }
        public double PO_Qty { get; set; }
        public double? GRN_Qty { get; set; }
        public int? GRN_DocNum { get; set; }
        public DateTime? GRN_DocDate { get; set; }
        public string PO_WhsCode { get; set; }
        public string? GRN_WhsCode { get; set; }
        public string ItemCode { get; set; }
        public string ItemName { get; set; }
        public string? Vendor_Code { get; set; }
        public string? Vendor_Name { get; set; }

    }

    public class QRScanReportInput
    {
        public int BranchID { get; set; }
        public string QRCodeID { get; set; }
    }

    public class QRScanReportOutput
    {
        public string QRCodeID { get; set; }
        public string ItemCode { get; set; }
        public string ItemName { get; set; }
        public string BatchSerialNo { get; set; }
        public string WhsCode { get; set; }
        public string WhsName { get; set; }
        public string BinCode { get; set; }
        public string Project { get; set; }
        public double BatchQty { get; set; }
        public double Stock { get; set; }
    }

    public class QcDetailsReport
    {
        public int GateInNo { get; set; }
        public DateTime GateIn_Date { get; set; }
        public int GRN_DocNum { get; set; }
        public int GRN_DocEntry { get; set; }
        //public double GateInQty { get; set; }
        public DateTime GRN_DocDate { get; set; }
        public string ItemCode { get; set; }
        public string ItemName { get; set; }
        public string QARequired { get; set; }
        public DateTime? QC_Date { get; set; }
        public double RecQty { get; set; }
        public double GRN_Qty { get; set; }
        public double? QC_Qty { get; set; }
        public string GRN_WhsCode { get; set; }
        public string? QC_FromWhs { get; set; }
        public string? QC_ToWhs { get; set; }
        public int PO_DocNum { get; set; }
        public int PO_DocEntry { get; set; }
        public DateTime PO_DocDate { get; set; }
        public string? Vendor_Code { get; set; }
        public string? Vendor_Name { get; set; }
    }


    public class GRN_Qc_DetailsReport
    {
        public int PO_DocEntry { get; set; }
        public int PO_DocNum { get; set; }
        public DateTime PO_DocDate { get; set; }
        public int GateInNo { get; set; }
        public DateTime GateIn_Date { get; set; }
        public double GateInQty { get; set; }
        public double PO_Qty { get; set; }
        public double? GRN_Qty { get; set; }
        public int? GRN_DocNum { get; set; }
        public DateTime? GRN_DocDate { get; set; }
        public string PO_WhsCode { get; set; }
        public string? GRN_WhsCode { get; set; }
        public string ItemCode { get; set; }
        public string ItemName { get; set; }
        public string? Vendor_Code { get; set; }
        public string? Vendor_Name { get; set; }
        public string? QC_ToWhs { get; set; }
        public string QARequired { get; set; }
        public double? QC_Qty { get; set; }

        public class LogModules
        {
            public string Module { get; set; }
        }

        public class LogDetails
        {
            public int BranchID { get; set; }
            public string FromDate { get; set; }
            public string ToDate { get; set; }
            public string Module { get; set; }
            public string UserName { get; set; }
            public string LogLevel { get; set; }

        }
        public class LogReport
        {
            public int BranchID { get; set; }
            public string Module { get; set; }
            public string Status { get; set; }
            public string LogMessage { get; set; }
            public string userName { get; set; }
            public string LogDate { get; set; }
            public string jsonPayload { get; set; }
        }


        public class QCDetailsReport
        {
            public int GateInNo { get; set; }
            public int PODocNum { get; set; }
            public string CardCode { get; set; }
            public string CardName { get; set; }
            public int GRNDocNum { get; set; }
            public string GRNQty { get; set; }
            public string ItemCode { get; set; }
            public string ItemName { get; set; }
            public string QARequired { get; set; }
            public string ApprovedQty { get; set; }
            public string RejectedQty { get; set; }
        }

        public class SupplierGateInDetails
        {
            public int BranchID { get; set; }
            public string FromDate { get; set; }
            public string ToDate { get; set; }
            public int PODocEntry { get; set; }
            public string VendorCode { get; set; }
        }
         
        public class SupplierGateInDetailsReport

        {
            public int DocEntry { get; set; }
            public int DocNum { get; set; }
            public int GateInNo { get; set; }
            public DateTime GateInDate { get; set; }
            public double GateInQty { get; set; }
            public double OrderQty { get; set; }
            public string ItemCode { get; set; }
            public string ItemName { get; set; }
            public string Project { get; set; }
            public string UomCode { get; set; }
            public string vendorCode { get; set; }
            public string vendorName { get; set; }


        }

    }

}
