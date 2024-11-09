using System.ComponentModel.DataAnnotations;

namespace WMS_UI_API.Models
{
    public class NotificationMasterClass
    {
        public string Module { get; set; }
        public string Sender_User_Name { get; set; }
        public string Notification_Text { get; set; }
        public DateTime N_Date_Time { get; set; } = DateTime.Now;
        public Boolean Chk_Status { get; set; } = false;
    }

    public class testclass
    {
        public List<Notification_Get_Class> data { get; set; }
        public int dataCount { get; set; }
    }
    public class Notification_Get_Class
    {
        public int N_Id { get; set; }
        public string Notification_Text { get; set; }
        public string timeLimit { get; set; }
        public string Chk_Status { get; set; }
    }

    public class Notification_Update_Status
    {
        public int N_Id { get; set; }
    }

    public partial class Notification
    {
        [Key]
        public int Id { get; set; }
        public string Username { get; set; } = null!;
        public string Message { get; set; } = null!;
        public DateTime NotificationDateTime { get; set; }
    }

    public class Notification_readAll_Status
    {
        public string Username { get; set; }
    }
}
