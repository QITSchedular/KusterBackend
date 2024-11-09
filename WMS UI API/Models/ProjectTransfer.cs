namespace WMS_UI_API.Models
{
    public class ProjectTransfer
    {
    }

    public class getQRDataInput
    {
        public string BranchID { get; set; }
        public string QRCodeID { get; set; }
    }

    public class getQRDataOutput
    {
        public int PODocEntry { get; set; }
        public int PODocNum { get; set; }
        public int GateInNo { get; set; }
        public string ItemCode { get; set; }
        public string ItemName { get; set; }
        public int LineNum { get; set; }
        public string QRCodeID { get; set; }
        public string BatchSerialNo { get; set; }
        public string Qty { get; set; }
        public string Project { get; set; }
    }

    public class updateProjectInput
    {
        public string BranchID { get; set; }
        public string QRCodeID { get; set; }
        public string Project { get; set; }
    }
}
