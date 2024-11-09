namespace WMS_UI_API.Models
{
    public class MachineIn
    {
        public string MachineId { get; set; }
        public string UserName { get; set; }
        public int ProcessID { get; set; }
        public int ShiftID { get; set; }
        public string WhsCode { get; set; }
    }

    public class MachineOut
    {
        public String UserName { get; set; }

    }

    public class UserMachineStat
    {
        //public int Id { get; set; }
        public string MachineId { get; set; }
        public string MachineName { get; set; }
        public int UserID { get; set; }
        public string ProcessName { get; set; }
        public string ShiftName { get; set; }
        public string WhsCode { get; set; }
        public DateTime InTime { get; set; }
        public DateTime? OutTime { get; set; }
    }

    public class MachineDetails
    {
        public string FromDate { get; set; }
        public string ToDate { get; set; }
        public string? MachineId { get; set; }
        public string? Status { get; set; }
        public string? WhsCode { get; set; }
        public int? UserId { get; set; }
    }

    public class MachineReport
    {
        public string MachineId { get; set; }
        public string MachineSrNo { get; set; }
        public string MachineName { get; set; }
        public string User_Name { get; set; }
        public string ProcessName { get; set; }
        public string ShiftName { get; set; }
        public string WhsCode { get; set; }
        public DateTime LoggedInTime { get; set; }
        public DateTime? LoggedOutTime { get; set; }
        public string TotalTime { get; set; }
    }
}