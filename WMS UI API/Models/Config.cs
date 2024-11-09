namespace WMS_UI_API.Models
{
    public class SaveConfig
    {
        public string BranchID { get; set; }
        public string AtcPath { get; set; }
        public string GenMethod { get; set; }
        public string QRMngBy { get; set; }
        public string QRGenMethod { get; set; }
        public string BatchType { get; set; }
        public string Remark { get; set; }
        public string Indicator { get; set; }
        public string QCRequired { get; set; }
        public string IncomingQCWhs { get; set; }
        public string InProcessQCWhs { get; set; }
        public string ApprovedWhs { get; set; }
        public string RejectedWhs { get; set; }
        public string NonQCWhs { get; set; }
        public string DeliveryWhs { get; set; }
        public int IncomingQCBinAbsEntry { get; set; }
        public int InProcessQCBinAbsEntry { get; set; }
        public int ApproveQCBinAbsEntry { get; set; }
        public int RejectQCBinAbsEntry { get; set; }
        public int NonQCBinAbsEntry { get; set; }
        public int DeliveryBinAbsEntry { get; set; }
        public string IssueSeries { get; set; }
        public string ReceiptSeries { get; set; }
        public string DeliverySeries { get; set; }
        public string GRPOSeries { get; set; }
        public string ITSeries { get; set; }
        public string QCITSeries { get; set; }
        public string OSSeries { get; set; }
        public string IssueSeriesName { get; set; }
        public string ReceiptSeriesName { get; set; }
        public string DeliverySeriesName { get; set; }
        public string GRPOSeriesName { get; set; }
        public string ITSeriesName { get; set; }
        public string QCITSeriesName { get; set; }
        public string OSSeriesName { get; set; }
        public string IsProRework { get; set; }
        public string GRPODeliveryDate { get; set; }
        public string IsPickActive { get; set; }
        public string PickWhs { get; set; } = string.Empty;
        public int PickBinAbsEntry { get; set; }
    }

    public class GetConfig
    {
        public string BranchID { get; set; }
        public string AtcPath { get; set; }
        public string GenMethod { get; set; }
        public string QRMngBy { get; set; }
        public string QRGenMethod { get; set; }
        public string BatchType { get; set; }
        public string Indicator { get; set; }
        public string QCRequired { get; set; }
        public string Remark { get; set; }
        public string IncomingQCWhs { get; set; }
        public string InProcessQCWhs { get; set; }
        public string ApprovedWhs { get; set; }
        public string RejectedWhs { get; set; }
        public string NonQCWhs { get; set; }
        public string DeliveryWhs { get; set; }
        public int IncomingBin { get; set; }
        public int InProcessBin { get; set; }
        public int ApproveBin { get; set; }
        public int RejectBin { get; set; }
        public int NonQCBin { get; set; }
        public int DeliveryBin { get; set; }
        public string IncomingBinCode { get; set; }
        public string InProcessBinCode { get; set; }
        public string ApproveBinCode { get; set; }
        public string RejectBinCode { get; set; }
        public string NonQCBinCode { get; set; }
        public string DeliveryBinCode { get; set; }
        public string IssueSeries { get; set; }
        public string ReceiptSeries { get; set; }
        public string DeliverySeries { get; set; }
        public string GRPOSeries { get; set; }
        public string ITSeries { get; set; }
        public string QCITSeries { get; set; }
        public string OSSeries { get; set; }
        public string IssueSeriesName { get; set; }
        public string ReceiptSeriesName { get; set; }
        public string DeliverySeriesName { get; set; }
        public string GRPOSeriesName { get; set; }
        public string ITSeriesName { get; set; }
        public string QCITSeriesName { get; set; }
        public string OSSeriesName { get; set; }
        public string IsProRework { get; set; }
        public string GRPODeliveryDate { get; set; }
        public string IsPickActive { get; set; }
        public string PickWhs { get; set; }
        public int PickBin { get; set; }
        public string PickBinCode { get; set; }

    }
}
