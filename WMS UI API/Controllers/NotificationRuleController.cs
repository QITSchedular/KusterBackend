using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Data.SqlClient;
using System.Data;
using WMS_UI_API.Models;
using Newtonsoft.Json;
using WMS_UI_API.Common;

namespace WMS_UI_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NotificationRuleController : ControllerBase
    {
        private string _QIT_connection = string.Empty;
        private string _QIT_DB = string.Empty;
        private string _Query = string.Empty;
        private SqlCommand cmd;
        DataSet ds = new DataSet();
        private readonly ILogger<AuthUserController> _logger;
        public IConfiguration Configuration { get; }
        private static List<NotificationRule> notificationRules = new List<NotificationRule>();
      
        
        public NotificationRuleController(IConfiguration configuration, ILogger<AuthUserController> AuthLogger)
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

        [HttpGet]
        //[Authorize]
        public async Task<ActionResult<IEnumerable<getNotificationModuleClass>>> Get(int? id)
        {
            try
            {
                if (id == null)
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "User ID is empty..!!" });
                }
                List<getNotificationModuleClass> obj;
                DataTable dtPeriod = new DataTable();
                _Query = @" select * from QIT_Notification_Rule where User_ID=" + id;
                SqlConnection con = new SqlConnection(_QIT_connection);
                con.Open();
                SqlDataAdapter oAdptr = new SqlDataAdapter(_Query, con);
                oAdptr.Fill(dtPeriod);
                con.Close();


                if (dtPeriod.Rows.Count > 0)
                {
                    obj = JsonConvert.DeserializeObject<List<getNotificationModuleClass>>(dtPeriod.Rows[0]["N_Rule_Details"].ToString()); 
                    return obj;
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", errorMessage = "No data found" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Error in NotificationRuleController : Get() :: {Error}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.Message.ToString() });
            }
        }

        [HttpPost]
        //[Authorize]
        public async Task<ActionResult<IEnumerable<NotificationRule>>> Post(NotificationRule nRule)
        {
            string _IsSaved = "N";
            try
            {
                if (nRule == null)
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Payload is empty..!!" });
                }
                SqlConnection _QITcon = new SqlConnection(_QIT_connection);
                _QITcon.Open();
                dynamic nRuleData = JsonConvert.SerializeObject(nRule.N_Rule_Details);
                string query = @"MERGE INTO QIT_Notification_Rule AS Target
                USING (SELECT @User_ID AS User_ID, @N_Rule_Details AS N_Rule_Details) AS Source
                ON Target.User_ID = Source.User_ID
                WHEN MATCHED THEN
                    UPDATE SET N_Rule_Details = Source.N_Rule_Details
                WHEN NOT MATCHED THEN
                    INSERT (User_ID, N_Rule_Details)
                    VALUES (Source.User_ID, Source.N_Rule_Details);";

                using (SqlCommand cmd = new SqlCommand(query, _QITcon))
                {
                    cmd.Parameters.AddWithValue("@User_ID", nRule.User_ID);
                    cmd.Parameters.AddWithValue("@N_Rule_Details", nRuleData);
                    int insertCount = cmd.ExecuteNonQuery();
                    if (insertCount > 0)
                        _IsSaved = "Y";
                }
                _QITcon.Close();
                return Ok(new { StatusCode = "200", IsSaved = _IsSaved, StatusMsg = "Saved Successfully!!!" });
                 
            }
            catch (Exception ex)
            {
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.Message.ToString() });
            }
        }

    }
}
