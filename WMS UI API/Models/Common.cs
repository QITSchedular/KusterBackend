using SAPbouiCOM;

namespace WMS_UI_API.Models
{

    public class Common
    {
    }

    public class ApiResponse
    {
        public string ResCode { get; set; }
        public string ResMsg { get; set; }
    }

    public class ApiResponses_Inv
    {
        public string StatusCode { get; set; }
        public string IsSaved { get; set; }
        public string StatusMsg { get; set; }
    }

    public class QRMngBy
    { 
        public string QRMngById { get; set; }
        public string QRMngByName { get; set; }
    }

    public class Branch
    {
        public int BPLId { get; set; }
        public string BPLName { get; set; }
    }

    public class PeriodIndicator
    {
        public string Indicator { get; set; }
    }

    public class SeriesCls
    {
        public int Series { get; set; }
        public string SeriesName { get; set; }
    }

    public class Project
    {
        public string PrjCode { get; set; }
        public string PrjName { get; set; }
    }

    public class QITWarehouse
    {
        public string WhsCode { get; set; }
        public string WhsName { get; set; }
    }


    public class CheckHeaderPO
    {
        public string BranchID { get; set; }
        public int DocEntry { get; set; }
        public int DocNum { get; set; }
        public int Series { get; set; }
        public string ObjType { get; set; }
        public int GateInNo { get; set; }
    }

    public class CheckDetailPO
    {
        public string BranchID { get; set; }
        public int DocEntry { get; set; }
        public int DocNum { get; set; }
        public int Series { get; set; }
        public string ObjType { get; set; }
        public string ItemCode { get; set; }
        public int LineNum { get; set; }
        public int GateInNo { get; set; }
    }

    public class CheckHeaderPOQR
    {
        public string BranchID { get; set; }
        public int DocEntry { get; set; }         
        public int Series { get; set; }
        public string ObjType { get; set; }        
        public int GateInNo { get; set; }
    }

    public class CheckDetailPRO
    {
        public string BranchID { get; set; }
        public int DocEntry { get; set; }
        public int DocNum { get; set; }
        public int Series { get; set; }
        public string ObjType { get; set; }
        public string ItemCode { get; set; }
        
        public int RecNo { get; set; }
    }

    public class SaveHeaderQR
    {
        public string BranchID { get; set; }
        public string QRCodeID { get; set; }
        public int DocEntry { get; set; }
        public int DocNum { get; set; }
        public int Series { get; set; }
        public string DocDate { get; set; }
        public string ObjType { get; set; }
        public string IncNo { get; set; }
    }

    public class SaveDetailQR
    {
        public string BranchID { get; set; }
        public int DocNum { get; set; } = 0;
        public string HeaderQRCodeID { get; set; }
        public string DetailQRCodeID { get; set; }
        public string IncNo { get; set; }
        public string ItemCode { get; set; }
        public string ItemName { get; set; } = string.Empty;
        public int LineNum { get; set; }    
        public string QRMngBy { get; set; }
        public string Qty { get; set; }
        public int GateInNo { get; set; }
        public string Remark { get; set; }
        public string Project { get; set; } = string.Empty;
        public string BatchSerialNo { get; set; }
        public string DefaultBin { get; set; } = string.Empty;
    }

    public class UpdateBatchQty
    {
        public int DocEntry { get; set; }
        public int DocNum { get; set; }
        public string ObjType { get; set; }
        public string ItemCode { get; set; }
        public int GateInNo { get; set; }
        public string BranchID { get; set; }
        public string DetailQRCodeID { get; set; }
        public string Qty { get; set; }
    }
     

    public class FillGateInNo
    {
        public int GateInNo { get; set; } 
    }

    public class ItemStock
    {
        public string WhsCode { get; set; }
        public string WhsName { get; set; }
        public string Stock { get; set; }
    }

    public class ItemBinStock
    {
        public string WhsCode { get; set; }
        public string WhsName { get; set; }
        public string BinCode { get; set; }
        public string Stock { get; set; }
    }

    public class SaveDetailProductionQR
    {
        public string BranchID { get; set; }
        public string HeaderQRCodeID { get; set; }
        public string DetailQRCodeID { get; set; }
        public string IncNo { get; set; }
        public string ItemCode { get; set; }
        public string QRMngBy { get; set; }
        public string Qty { get; set; }
        public int RecNo { get; set; }
        public string Remark { get; set; }
        public string BatchSerialNo { get; set; }
        public string Project { get; set; }
    }

    public class BinLocation
    {
        public int AbsEntry { get;set; }
        public string BinCode { get; set; }
    }

    public class QAStatus
    {
        public string ID { get; set;}
        public string Status { get; set; }
    }

    public class CheckAllDetailPO
    {
        public string BranchID { get; set; }
        
        public int GateInNo { get; set; }
    }

    public class BusinessPartner
    {
        public string CardCode { get; set;}
        public string CardName { get; set; }
    }

}
