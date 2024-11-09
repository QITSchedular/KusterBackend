using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Data.SqlClient;
using WMS_UI_API.Common;
using WMS_UI_API.Models;

namespace WMS_UI_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LogController : ControllerBase
    {
        private string _ApplicationApiKey = string.Empty;
        private string _connection = string.Empty;
        private string _QIT_connection = string.Empty;
        private string _QIT_DB = string.Empty;
        private string _Query = string.Empty;
        private SqlCommand cmd;
        public Global objGlobal;

        public IConfiguration Configuration { get; }
        private readonly ILogger<LogController> _logger;

        public LogController(IConfiguration configuration, ILogger<LogController> logger)
        {
            if (objGlobal == null)
                objGlobal = new Global();
            _logger = logger;
            try
            {
                Configuration = configuration;
                _ApplicationApiKey = Configuration["connectApp:ServiceApiKey"];
                _connection = Configuration["connectApp:ConnString"];
                _QIT_connection = Configuration["connectApp:QITConnString"];
                 
                _QIT_DB = Configuration["QITDB"];
                Global.QIT_DB = _QIT_DB;
                Global.SAP_DB = Configuration["CompanyDB"];

            }
            catch (Exception ex)
            {
                _logger.LogError(" Error in LogController :: {Error}" + ex.ToString());
            }
        }

        [HttpPost("Save")]
        public IActionResult SaveLog([FromBody] SaveLog payload)
        {
            string _IsSaved = "N";
            try
            {
                _logger.LogInformation(" Calling LogController : SaveLog() ");

                string p_ErrorMsg = string.Empty;

                if (payload != null)
                {
                    #region Check Branch
                    if (payload.BranchID == 0)
                    {
                        return BadRequest(new
                        {
                            StatusCode = "400",
                            StatusMsg = "Provide Branch"
                        });
                    }
                    #endregion

                    #region Check for Module
                    if (payload.Module.ToString().Trim() == "")
                    {
                        return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Provide Module" });
                    }
                    if (payload.Module.ToString().ToLower() == "string")
                    {
                        return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Provide Module" });
                    }
                    #endregion

                    #region Check for Controller Name
                    if (payload.ControllerName.ToString().Trim() == "")
                    {
                        return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Provide Controller" });
                    }
                    if (payload.ControllerName.ToString().ToLower() == "string")
                    {
                        return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Provide Controller" });
                    }
                    #endregion

                    #region Check for Method
                    if (payload.MethodName.ToString().Trim() == "")
                    {
                        return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Provide Method" });
                    }
                    if (payload.MethodName.ToString().ToLower() == "string")
                    {
                        return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Provide Method" });
                    }
                    #endregion

                    #region Check for LogLevel 
                    int _logLevel = payload.LogLevel.ToString().Length;
                    if (_logLevel > 1)
                    {
                        return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "LogLevel Values : I:Information/S:Success/E:Error" });
                    }
                    else
                    {
                        if (payload.LogLevel.ToString().ToUpper() != "I" && payload.LogLevel.ToString().ToUpper() != "E" && payload.LogLevel.ToString().ToUpper() != "S")
                            return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "LogLevel Values : I:Information/S:Success/E:Error" });
                    }
                    #endregion

                    #region Check for Log Message
                    if (payload.LogMessage.ToString().Trim() == "")
                    {
                        return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Provide Log Message" });
                    }
                    if (payload.LogMessage.ToString().ToLower() == "string")
                    {
                        return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Provide Log Message" });
                    }
                    #endregion

                    #region Check for loginUser
                    if (payload.LoginUser.ToString().Trim() == "")
                    {
                        return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Provide Login User" });
                    }
                    if (payload.LoginUser.ToString().ToLower() == "string")
                    {
                        return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Provide Login User" });
                    }
                    #endregion

                    SqlConnection con = new SqlConnection(_QIT_connection);
                    _Query = @"insert into QIT_API_Log(BranchID, Module, ContollerName, MethodName, LogLevel, LogMessage, jsonPayload, LoginUser) 
                           VALUES ( @bID, @module, @cName, @mName, @logLevel, @logMsg, @json, @user)";
                    _logger.LogInformation(" LogController : SaveLog() Query : {q} ", _Query.ToString());

                    cmd = new SqlCommand(_Query, con);
                    cmd.Parameters.AddWithValue("@bID", payload.BranchID);
                    cmd.Parameters.AddWithValue("@module", payload.Module);
                    cmd.Parameters.AddWithValue("@cName", payload.ControllerName);
                    cmd.Parameters.AddWithValue("@mName", payload.MethodName);
                    cmd.Parameters.AddWithValue("@logLevel", payload.LogLevel);
                    cmd.Parameters.AddWithValue("@logMsg", payload.LogMessage);
                    cmd.Parameters.AddWithValue("@json", payload.jsonPayload);
                    cmd.Parameters.AddWithValue("@user", payload.LoginUser);

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
                    return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = " Details not found" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Error in LogController : SaveLog() :: {Error}", ex.ToString());

                if (ex.Message.ToLower().Contains("uc_whsname"))
                {
                    return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Warehouse Name already exist" });
                }
                else if (ex.Message.ToLower().Contains("uc_whscode"))
                {
                    return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Warehouse Code already exist" });
                }
                else if (ex.Message.ToLower().Contains("pk_qit_warehouse_master"))
                {
                    return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Warehouse Code already exist" });
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = ex.Message.ToString() });
                }
            }
        }



    }
}
