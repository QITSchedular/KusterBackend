namespace WMS_UI_API.Models
{
    public class StockOpening
    {
    }

    public class OS_Warehouse
    {
        public string WhsCode { get; set; }
        public string WhsName { get; set; }
        public string BinActivate { get; set; }
    }

    public class OS_BinLocation
    {
        public int AbsEntry { get; set; }
        public string BinCode { get; set; }
    }

    public class OS_Project
    {
        public string ProjectCode { get; set; }
        public string ProjectName { get; set; }
    }


    public class OS_ItemData
    {
        public string WhsCode { get; set; }
        public string Bin { get; set; }
        public string ItemCode { get; set; }
        public string ItemName { get; set; }
        public double SAPStock { get; set; }
        public double QITStock { get; set; }
        public double EligibleStock { get; set; } // only for validation 
        public object OpeningStock { get; set; } // input field
        public string DistNumber { get; set; }

    }


    public class OS_Save
    {
        public int BranchId { get; set; }
        public int Series { get; set; }
        public string WhsCode { get; set; }
        public int BinAbsEntry { get; set; }
        public string Project { get; set; }
        public List<OS_SaveDetail> OpeningDetails { get; set; }
    }

    public class OS_SaveDetail
    {
        public string ItemCode { get; set; }
        public string DistNumber { get; set; }
        public string OpeningQty { get; set; }
    }

    public class OpeningNoList
    {
        public int OpeningNo { get; set; }
    }


    public class OS_OpeningStockData
    {
        public int OpeningNo { get; set; }
        public string ObjType { get; set; }
        public int Series { get; set; }
        public string SeriesName { get; set; }
        public string ItemCode { get; set; }
        public string ItemName { get; set; }
        public int DocLineNum { get; set; }
        public double Qty { get; set; }
        public string OpeningDate { get; set; }
        public string WhsCode { get; set; }
        public string Project { get; set; }
        public int BinAbsEntry { get; set; }
        public string BinCode { get; set; }
        public string Canceled { get; set; }
        public string BatchSerialNo { get; set; }
        public string ItemMngBy { get; set; }
        public string QRMngBy { get; set; }

    }

    public class CheckHeaderOpening
    {
        public string BranchID { get; set; }
        public int OpeningNo { get; set; }  
        public int Series { get; set; }
        public string ObjType { get; set; }
    }


    public class SaveHeaderOpeningQR
    {
        public string BranchID { get; set; }
        public string QRCodeID { get; set; }
        public int OpeningNo { get; set; }
        public int Series { get; set; }
        public string ObjType { get; set; }
        public string IncNo { get; set; }
    }

    public class CheckDetailOpening
    {
        public string BranchID { get; set; }
        public int OpeningNo { get; set; }
        public int Series { get; set; }
        public string ObjType { get; set; }
        public string ItemCode { get; set; }
        public int DocLineNum { get; set; }
        
    }


    public class SaveDetailOpeningQR
    {
        public string BranchID { get; set; }
        public string HeaderQRCodeID { get; set; }
        public string DetailQRCodeID { get; set; }
        public string IncNo { get; set; }
        public string ItemCode { get; set; }
        public int DocLineNum { get; set; }
        public string QRMngBy { get; set; }
        public string Qty { get; set; }
        public int OpeningNo { get; set; }
        public string Remark { get; set; }
        public string BatchSerialNo { get; set; }
    }


    public class GetDetailDataQR
    {
        public string BranchID { get; set; }
        public string HeaderQRCodeID { get; set; }
        public string DetailQRCodeID { get; set; }
        public string IncNo { get; set; }
        public string ItemCode { get; set; }
        public int DocLineNum { get; set; }
        public string QRMngBy { get; set; }
        public string Qty { get; set; }
        public int OpeningNo { get; set; }
        public string Remark { get; set; }
        public string Project { get; set; }
        public string BatchSerialNo { get; set; }
    }

}
