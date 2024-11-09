using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace WMS_UI_API.Models
{
    [JsonObject(ItemRequired = Required.Always)]
    public class WarehouseRule
    {
        public int WarehouseRule_ID { get; set; }
        public string Warehouse_Code { get; set; }
        public string Warehouse_Name { get; set; }
        public string Warehouse_Location { get; set; }
        public string Warehouse_binActivat { get; set; }
        [Newtonsoft.Json.JsonIgnore]
        [JsonProperty(Required = Required.Default)]
        public List<int> User_Details { get; set; }
    }
    public class GetWarehouseRule
    {
        public string Warehouse_Code { get; set; }
    }
    
    public class UserBindWithWarehouse
    {
        public int User_ID { get; set; }
        public string User_Name { get; set; }
        public string User_Email { get; set; } = null;
        public string User_Password { get; set; }
        public long Mobile_No { get; set; } = 0;
        public string Department { get; set; } = null;
        public string IsActive { get; set; }
        public Boolean IsBind { get; set; } = false;
    }
    public class GetWarehousebyUser
    {
        public int User_ID { get; set; }
    }

    public class GetWarehouseForUser
    {
        public string Warehouse_Code { get; set; }
        public string Warehouse_Name { get; set; }
        //public string Warehouse_Location { get; set; }
        //public string Warehouse_binActivat { get; set; }
    }

}
