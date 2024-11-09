namespace WMS_UI_API.Models
{
    public class NotificationRule
    {
        public int N_Rule_ID { get; set; }
        public int User_ID { get; set; }
        public List<getNotificationModuleClass> N_Rule_Details { get; set; }
    }
    public class getNotificationModuleClass
    {
        public string text { get; set; }
        public string path { get; set; }
        public string icon { get; set; }
        public List<getNotificationSubModuleClass> items { get; set; }
    }

    public class getNotificationSubModuleClass
    {
        public string text { get; set; }
        public string path { get; set; }
        public string icon { get; set; }
        public Notification_access n_Rule { get; set; }
    }

    public class Notification_access
    {
        public Boolean application { get; set; }
        public Boolean sms { get; set; }
        public Boolean email { get; set; }
    }
}
