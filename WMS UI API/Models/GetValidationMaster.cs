namespace WMS_UI_API.Models
{
    public class GetValidationMaster
    {
        public int Validation_Master_ID { get; set; }
        public string Validation_Name { get; set; }
        public string Modules { get; set; }
        public string Filter_Type { get; set; }
        public string Condition { get; set; }
        public string Comparision_Value { get; set; }
        public int N_Rule_ID { get; set; }
        public string Message { get; set; }
    }

    public class ValidationRule
    {
        public int ValidationRule_Master_Id { get; set; }
        public int Validation_Master_ID { get; set; }
        public List<int> User_Details { get; set; }
        public ValidationRule()
        {
            User_Details = new List<int>();
        }
    }

    public class GetValidationRule
    {
        public int Validation_Master_ID { get; set; }
        public string Modules { get; set; }
    }

    public class GetValidationRuleUsers
    {
        public int User_ID { get; set; }
        public string User_Name { get; set; }
        public Boolean IsBind { get; set; } = false;
    }
}
