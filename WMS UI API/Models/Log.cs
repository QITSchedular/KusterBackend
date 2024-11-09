namespace WMS_UI_API.Models
{
    public class SaveLog
    {
        public int BranchID { get; set; } 
        public string Module { get; set; }
        public string ControllerName { get; set; }
        public string MethodName { get; set; }
        public string LogLevel { get; set; }
        public string LogMessage { get; set; }
        public string jsonPayload { get; set; }
        public string LoginUser {  get; set; }


    }
}
