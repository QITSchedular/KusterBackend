using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using SAPbobsCOM;
using System.Data.SqlClient;
using WMS_UI_API.Common;
using WMS_UI_API.Models;

namespace WMS_UI_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UoMsController : ControllerBase
    {
        private string _ApplicationApiKey = string.Empty;
        private string _connection = string.Empty;
        private string _QIT_connection = string.Empty;
        private string _QIT_DB = string.Empty;
        private string _Query = string.Empty;
        private SqlCommand cmd;

        public IConfiguration Configuration { get; }
        private readonly ILogger<UoMsController> _logger;

        public UoMsController(IConfiguration configuration, ILogger<UoMsController> logger)
        {
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
                _logger.LogError(" Error in UoMsController :: {Error}" + ex.ToString());
            }
        }


        [HttpGet("Get")]
        public async Task<ActionResult<IEnumerable<UOM>>> GetUoM(int BranchID, string Filter)
        {
            SqlConnection QITcon;
            SqlDataAdapter oAdptr;
            try
            {
                _logger.LogInformation(" Calling UoMsController : GetUoM() ");

                if (BranchID <= 0)
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Provide Branch" });

                System.Data.DataTable dtData = new System.Data.DataTable();
                QITcon = new SqlConnection(_QIT_connection);

                string _where = string.Empty;
                if (Filter.ToUpper() == "Y" || Filter.ToUpper() == "N")
                    _where = " AND A.Locked = @locked ";

                _Query = @"  
                SELECT A.UomCode, A.UomName,  
                       CASE WHEN A.Locked = 'Y' then 'Yes' else 'No' end Locked 
                FROM QIT_UoM_Master A  
                WHERE 1 = 1 AND ISNULL(A.BranchID, @bId) = @bId " + _where;

                _logger.LogInformation(" UoMsController : GetUoM() Query : {q} ", _Query.ToString());
                QITcon.Open();
                oAdptr = new SqlDataAdapter(_Query, QITcon);
                oAdptr.SelectCommand.Parameters.AddWithValue("@bId", BranchID);
                if (Filter.ToUpper() == "Y" || Filter.ToUpper() == "N")
                    oAdptr.SelectCommand.Parameters.AddWithValue("@locked", Filter);
                oAdptr.Fill(dtData);
                QITcon.Close();

                if (dtData.Rows.Count > 0)
                {
                    List<UOM> obj = new List<UOM>();
                    dynamic arData = JsonConvert.SerializeObject(dtData);
                    obj = JsonConvert.DeserializeObject<List<UOM>>(arData.ToString());
                    return obj;
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "No data found" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(" Error in UoMsController : GetUoM() :: {Error}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
            }
        }


        [HttpPost("Save")]
        public IActionResult SaveUOM([FromBody] UOM payload)
        {
            string _IsSaved = "N";
            SqlConnection QITcon;
            try
            {
                _logger.LogInformation(" Calling UoMsController : SaveUOM() ");

                if (payload != null)
                {
                    if (payload.BranchID <= 0)
                        return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Provide Branch" });

                    #region Check for Locked
                    if (payload.Locked.ToString().ToUpper() != "Y" && payload.Locked.ToString().ToUpper() != "N")
                        return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Locked Values : Y / N " });

                    #endregion

                    QITcon = new SqlConnection(_QIT_connection);
                    _Query = @"
                    INSERT INTO QIT_UoM_Master(BranchID, UomCode, UomName, Locked) 
                    VALUES (@bId, @uCode, @uName, @locked)";
                    _logger.LogInformation(" UoMsController : SaveUOM() Query : {q} ", _Query.ToString());

                    cmd = new SqlCommand(_Query, QITcon);
                    cmd.Parameters.AddWithValue("@bId", payload.BranchID);
                    cmd.Parameters.AddWithValue("@uCode", payload.UomCode);
                    cmd.Parameters.AddWithValue("@uName", payload.UomName);
                    cmd.Parameters.AddWithValue("@locked", payload.Locked.ToUpper());

                    QITcon.Open();
                    int intNum = cmd.ExecuteNonQuery();
                    QITcon.Close();

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
                _logger.LogError("Error in UoMsController : SaveUOM() :: {Error}", ex.ToString());

                if (ex.Message.ToLower().Contains("uc_uomcode"))
                {
                    return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "UoM Code already exist" });
                }
                else if (ex.Message.ToLower().Contains("uc_uomname"))
                {
                    return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "UoM Name already exist" });
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = ex.Message.ToString() });
                }
            }
        }


        [HttpPut("Update/{UoMCode}")]
        public IActionResult UpdateUOM([FromBody] UOMUpdate payload, string UoMCode)
        {
            string _IsSaved = "N";
            SqlConnection QITcon;
            try
            {
                _logger.LogInformation(" Calling UoMsController : UpdateUOM() ");

                string p_ErrorMsg = string.Empty;

                if (payload != null)
                {
                    if (payload.BranchID <= 0)
                        return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Provide Branch" });

                    #region Check for Locked

                    if (payload.Locked.ToString().ToUpper() != "Y" && payload.Locked.ToString().ToUpper() != "N")
                        return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Locked Values : Y / N " });

                    #endregion

                    QITcon = new SqlConnection(_QIT_connection);
                    _Query = @" SELECT count(*) from QIT_UoM_Master where UomCode = @uCode and ISNULL(BranchID, @bId) = @bId ";
                    _logger.LogInformation(" UoMsController : UoM Exist Query : {q} ", _Query.ToString());
                    cmd = new SqlCommand(_Query, QITcon);
                    cmd.Parameters.AddWithValue("@bId", payload.BranchID);
                    cmd.Parameters.AddWithValue("@uCode", UoMCode);
                    QITcon.Open();
                    Object Value = cmd.ExecuteScalar();
                    QITcon.Close();
                    if (Int32.Parse(Value.ToString()) == 1)
                    {
                        _Query = @" UPDATE QIT_UoM_Master SET UomName=@uName, Locked=@locked where UomCode = @uCode and ISNULL(BranchID, @bId) = @bId ";
                        _logger.LogInformation(" UoMsController : UpdateUOM() Query : {q} ", _Query.ToString());

                        cmd = new SqlCommand(_Query, QITcon);
                        cmd.Parameters.AddWithValue("@bId", payload.BranchID);
                        cmd.Parameters.AddWithValue("@uCode", UoMCode);
                        cmd.Parameters.AddWithValue("@uName", payload.UomName);
                        cmd.Parameters.AddWithValue("@locked", payload.Locked.ToUpper());

                        QITcon.Open();
                        int intNum = cmd.ExecuteNonQuery();
                        QITcon.Close();

                        if (intNum > 0)
                            _IsSaved = "Y";
                        else
                            _IsSaved = "N";

                        return Ok(new { StatusCode = "200", IsSaved = _IsSaved, StatusMsg = "Updated Successfully!!!" });
                    }
                    else
                    {
                        return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "UoM : " + UoMCode + " does not exist" });
                    }
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = " Details not found" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Error in UoMsController : UpdateUOM() :: {Error}", ex.ToString());

                if (ex.Message.ToLower().Contains("uc_uomcode"))
                {
                    return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "UoM Code already exist" });
                }
                else if (ex.Message.ToLower().Contains("uc_uomname"))
                {
                    return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "UoM Name already exist" });
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = ex.Message.ToString() });
                }
            }
        }


        [HttpDelete("Delete")]
        public IActionResult DeleteUOM(int BranchID, string ID)
        {
            SqlConnection QITcon;
            string _IsSaved = "N";
            try
            {
                _logger.LogInformation(" Calling UoMsController : DeleteUOM() ");

                QITcon  = new SqlConnection(_QIT_connection);

                string query = @" SELECT COUNT(*) FROM QIT_UoM_Master WHERE UomCode = @uCode and ISNULL(BranchID, @bId) = @bId ";
                _logger.LogInformation(" UoMsController : UoM Code Query : {q} ", query.ToString());

                QITcon.Open();
                cmd = new SqlCommand(query, QITcon);
                cmd.Parameters.AddWithValue("@bId", BranchID);
                cmd.Parameters.AddWithValue("@uCode", ID);
                Object Value = cmd.ExecuteScalar();
                QITcon.Close();
                if (Int32.Parse(Value.ToString()) > 0)
                {
                    _Query = @" DELETE FROM QIT_UoM_Master WHERE UomCode = @uCode and ISNULL(BranchID, @bId) = @bId";
                    _logger.LogInformation(" UoMsController : DeleteUOM() Query : {q} ", _Query.ToString());

                    cmd = new SqlCommand(_Query, QITcon);
                    cmd.Parameters.AddWithValue("@bId", BranchID);
                    cmd.Parameters.AddWithValue("@uCode", ID);
                    QITcon.Open();
                    int intNum = cmd.ExecuteNonQuery();
                    QITcon.Close();

                    if (intNum > 0)
                        _IsSaved = "Y";
                    else
                        _IsSaved = "N";

                    return Ok(new { StatusCode = "200", IsSaved = _IsSaved, StatusMsg = "Deleted Successfully!!!" });
                }
                else
                    return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "UoM Code does not exist : " + ID });
            }
            catch (Exception ex)
            {
                _logger.LogError("Error in UoMsController : DeleteUOM() :: {Error}", ex.ToString());
                return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = ex.ToString() });
            }
        }
    
    }
}
