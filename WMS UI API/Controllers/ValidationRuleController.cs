using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Data.SqlClient;
using System.Data;
using Newtonsoft.Json;
using WMS_UI_API.Models;
using Microsoft.OpenApi.Validations;
using ValidationRule = WMS_UI_API.Models.ValidationRule;
using WMS_UI_API.Common;

namespace WMS_UI_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ValidationRuleController : ControllerBase
    {
        private string _QIT_connection = string.Empty;
        private string _QIT_DB = string.Empty;
        private string _Query = string.Empty;
        private SqlCommand cmd;
        DataSet ds = new DataSet();
        private readonly ILogger<ValidationRuleController> _logger;
        public IConfiguration Configuration { get; }


        public ValidationRuleController(IConfiguration configuration, ILogger<ValidationRuleController> AuthLogger)
        {
            _logger = AuthLogger;
            try
            {
                Configuration = configuration;
                _QIT_connection = Configuration["connectApp:QITConnString"];
                _QIT_DB = Configuration["QITDB"];
                Global.QIT_DB = _QIT_DB;
                Global.SAP_DB = Configuration["CompanyDB"];

            }
            catch (Exception e)
            {
                _logger.LogError("Error : " + e.ToString());
            }
        }

        // GET: api/<ValidationRuleController>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<GetValidationMaster>>> Get()
        {
            try
            {
                List<GetValidationMaster> obj = new List<GetValidationMaster>();
                System.Data.DataTable dtData = new System.Data.DataTable();
                SqlConnection con = new SqlConnection(_QIT_connection);
                _Query = @"select * from QIT_Validation_Master";
                con.Open();
                SqlDataAdapter oAdptrr = new SqlDataAdapter(_Query, con);
                oAdptrr.Fill(dtData);
                con.Close();
                if (dtData.Rows.Count > 0)
                {

                    dynamic arObj = JsonConvert.SerializeObject(dtData);
                    obj = JsonConvert.DeserializeObject<List<GetValidationMaster>>(arObj);
                    return Ok(obj);
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", errorMessage = "No data found" });
                }
            }
            catch (Exception ex)
            {
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.Message.ToString() });
            }
        }

        // POST api/<ValidationRuleController>
        [HttpPost("SaveValidationMaster")]
        public async Task<ActionResult> Post([FromBody] GetValidationMaster payload)
        {

            string _IsSaved = "N";
            try
            {
                _logger.LogInformation(" Calling ValidationRuleController : SaveValidationMaster() ");

                string p_ErrorMsg = string.Empty;
                if (payload != null)
                {
                    SqlConnection con = new SqlConnection(_QIT_connection);
                    _Query = @"insert into QIT_Validation_Master(Validation_Name, Modules, Filter_Type, Condition,Comparision_Value,N_Rule_ID,Message) 
                           VALUES (@Validation_Name, @Modules, @Filter_Type, @Condition,@Comparision_Value, @N_Rule_ID,@Message)";
                    _logger.LogInformation(" ValidationRuleController : SaveValidationMaster() Query : {q} ", _Query.ToString());

                    cmd = new SqlCommand(_Query, con);
                    cmd.Parameters.AddWithValue("@Validation_Name", payload.Validation_Name);
                    cmd.Parameters.AddWithValue("@Modules", payload.Modules);
                    cmd.Parameters.AddWithValue("@Filter_Type", payload.Filter_Type);
                    cmd.Parameters.AddWithValue("@Condition", payload.Condition);
                    cmd.Parameters.AddWithValue("@Comparision_Value", payload.Comparision_Value);
                    cmd.Parameters.AddWithValue("@N_Rule_ID", payload.N_Rule_ID);
                    cmd.Parameters.AddWithValue("@Message", payload.Message);

                    con.Open();
                    int intNum = cmd.ExecuteNonQuery();
                    con.Close();

                    if (intNum > 0)
                        _IsSaved = "Y";
                    else
                        _IsSaved = "N";

                    return Ok(new { StatusCode = "200", IsSaved = _IsSaved, StatusMsg = "Saved Successfully!!!" });
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Data can not be empty", isSaved = _IsSaved });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Error in AuthUserController : SaveUser() :: {Error}", ex.ToString());
                if (ex.ToString().Contains("UQ_Validation_Name"))
                {
                    return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Validation Rule Name already exist" });
                }
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.Message.ToString(), isSaved = _IsSaved });
            }
        }


        [HttpPost("SaveValidationRuleMaster")]
        public async Task<ActionResult> SaveValidationRuleMaster([FromBody] ValidationRule payload)
        {

            string _IsSaved = "N";
            try
            {
                _logger.LogInformation(" Calling ValidationRuleController : SaveValidationRuleMaster() ");

                if (payload == null)
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Data can not be empty", isSaved = _IsSaved });
                }

                SqlConnection _QITcon = new SqlConnection(_QIT_connection);
                _QITcon.Open();
                string mergeQuery = @"MERGE INTO QIT_ValidationRule_Master AS Target
USING (SELECT @Validation_Master_Id AS Validation_Master_Id, @User_details AS User_details) AS Source
ON Target.Validation_Master_Id = Source.Validation_Master_Id
WHEN MATCHED THEN
    UPDATE SET User_details = Source.User_details
WHEN NOT MATCHED THEN
    INSERT (Validation_Master_ID, User_details)
    VALUES (Source.Validation_Master_ID, Source.User_details);";
                using (SqlCommand cmd = new SqlCommand(mergeQuery, _QITcon))
                {
                    cmd.Parameters.AddWithValue("@Validation_Master_ID", payload.Validation_Master_ID);
                    cmd.Parameters.AddWithValue("@User_details", JsonConvert.SerializeObject(payload.User_Details));
                    _logger.LogInformation(" ValidationRuleController : SaveValidationRuleMaster() Query : {q} ", mergeQuery.ToString());
                    int insertCount = cmd.ExecuteNonQuery();
                    if (insertCount > 0)
                        _IsSaved = "Y";
                }

                _QITcon.Close();
                return Ok(new { StatusCode = "200", IsSaved = _IsSaved, StatusMsg = "Saved Successfully!!!" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = ex.Message.ToString() });
            }
        }

        [HttpPost("GetValidationRule")]
        public async Task<ActionResult<IEnumerable<ValidationRule>>> GetValidationRule([FromBody] GetValidationRule payload)
        {
            try
            {
                if (payload.Validation_Master_ID == null || payload.Validation_Master_ID == 0)
                {
                    return BadRequest(new { StatusCode = 400, StatusMsg = "Validation rule id is required" });
                }
                List<GetValidationRuleUsers> obj = new List<GetValidationRuleUsers>();
                DataTable dtV = new DataTable();
                SqlConnection con = new SqlConnection(_QIT_connection);
                _Query = @"select Validation_Master_ID,User_Details from QIT_ValidationRule_Master where Validation_Master_ID=@Validation_Master_ID";
                con.Open();
                using (SqlCommand cmd = new SqlCommand(_Query, con))
                {
                    cmd.Parameters.AddWithValue("@Validation_Master_ID", payload.Validation_Master_ID);

                    SqlDataAdapter oAdptrW = new SqlDataAdapter(cmd);
                    oAdptrW.Fill(dtV);
                    con.Close();

                }
                ValidationRule rule = new ValidationRule();
                if (dtV.Rows.Count > 0)
                {
                    rule.User_Details = JsonConvert.DeserializeObject<List<int>>(dtV.Rows[0]["User_Details"].ToString()); 
                }



                _Query = @"select User_ID,Authentication_Rule_Details from QIT_Authentication_Rule";
                con.Open();
                DataTable dtA = new DataTable();
                using (SqlCommand cmd = new SqlCommand(_Query, con))
                {
                    SqlDataAdapter oAdptrW = new SqlDataAdapter(cmd);
                    oAdptrW.Fill(dtA);
                    con.Close();
                }

                List<int> userIds = dtA.AsEnumerable()
                 .Where(row => row["Authentication_rule_details"] != DBNull.Value)
                 .Select(row =>
                 {
                     getDataClass module = new getDataClass
                     {
                         User_ID = Convert.ToInt32(row["User_ID"]),
                         moduleCLasses = JsonConvert.DeserializeObject<List<getModuleClass>>(row["Authentication_rule_details"].ToString())
                     };
                     return module;
                 })
                 .Where(module => module.moduleCLasses != null &&
                                  module.moduleCLasses.Any(subModule =>
                                     subModule.items != null &&
                                     subModule.items.Any(subItem =>
                                         subItem.text == payload.Modules &&
                                         subItem.rightsAccess != null &&
                                         subItem.rightsAccess.Contains("T"))))
                 .Select(module => module.User_ID)
                 .ToList();

                if (userIds.Count > 0)
                { 
                    string query = @"SELECT User_ID, User_Name FROM QIT_User_Master WHERE User_ID IN (" + string.Join(',', userIds) + ")";
                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        DataTable dtU = new DataTable();
                        using (SqlDataAdapter oAdptrW = new SqlDataAdapter(cmd))
                        {
                            oAdptrW.Fill(dtU);
                        }


                        if (dtU.Rows.Count > 0)
                        {
                            List<GetValidationRuleUsers> objOfUsers = new List<GetValidationRuleUsers>();
                            dynamic arData = JsonConvert.SerializeObject(dtU);
                            objOfUsers = JsonConvert.DeserializeObject<List<GetValidationRuleUsers>>(arData.ToString());

                            if (rule.User_Details.Count > 0)
                            {
                                foreach (var user in rule.User_Details)
                                {
                                    objOfUsers.Where(item => item.User_ID == user).ToList().ForEach(o => o.IsBind = true);
                                }
                                
                                return Ok(objOfUsers);
                            }
                            else
                            {
                                return Ok(objOfUsers);
                            }
                        }
                        else
                        {
                            return BadRequest(new { StatusCode = "400", StatusMsg = "No user data found " });
                        } 
                    }
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "No user have access of " + payload.Modules + " " });
                }

                return Ok(userIds);

            }
            catch (Exception ex)
            {
                _logger.LogError("Error in ValidationRuleController : GetValidationRule() :: {Error}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.Message.ToString() });
            }


        }
    }
}
