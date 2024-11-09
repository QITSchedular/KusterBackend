using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Data;
using System.Data.SqlClient;
using System.Reflection.Emit;
using WMS_UI_API.Common;
using WMS_UI_API.Models;

namespace WMS_UI_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LocationsController : ControllerBase
    {
        private string _ApplicationApiKey = string.Empty;
        private string _connection = string.Empty;
        private string _QIT_connection = string.Empty;
        private string _QIT_DB = string.Empty;
        private string _Query = string.Empty;
        private SqlCommand cmd;

        public IConfiguration Configuration { get; }
        private readonly ILogger<LocationsController> _logger;

        public LocationsController(IConfiguration configuration, ILogger<LocationsController> logger)
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
                _logger.LogError(" Error in LocationsController :: {Error}" + ex.ToString());
            }
        }


        [HttpGet("Get")]
        public async Task<ActionResult<IEnumerable<LocationS>>> GetLocation(int BranchID)
        {
            SqlConnection QITcon;
            SqlDataAdapter oAdptr;
            try
            {
                _logger.LogInformation(" Calling LocationsController : GetLocation() ");

                if (BranchID <= 0)
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Provide Branch" });

                System.Data.DataTable dtData = new System.Data.DataTable();
                QITcon = new SqlConnection(_QIT_connection);

                _Query = @"  
                SELECT A.Code, A.Location, A.GSTIN  
                FROM QIT_Location_Master A 
                WHERE ISNULL(BranchID, @bId) = @bId
                ORDER BY Code ";
                _logger.LogInformation(" LocationsController : GetLocation() Query : {q} ", _Query.ToString());
                QITcon.Open();
                oAdptr = new SqlDataAdapter(_Query, QITcon);
                oAdptr.SelectCommand.Parameters.AddWithValue("@bId", BranchID);
                oAdptr.Fill(dtData);
                QITcon.Close();

                if (dtData.Rows.Count > 0)
                {
                    List<LocationS> obj = new List<LocationS>();
                    dynamic arData = JsonConvert.SerializeObject(dtData);
                    obj = JsonConvert.DeserializeObject<List<LocationS>>(arData.ToString());
                    return obj;
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "No data found" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(" Error in LocationsController : GetLocation() :: {Error}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
            }
        }


        [HttpPost("Save")]
        public IActionResult SaveLocation([FromBody] LocationIU payload)
        {
            SqlConnection QITcon;
            string _IsSaved = "N";
            try
            {
                _logger.LogInformation(" Calling LocationsController : SaveLocation() ");

                if (payload != null)
                {
                    if (payload.BranchID <= 0)
                        return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Provide Branch" });

                    QITcon = new SqlConnection(_QIT_connection);
                    _Query = @"
                    INSERT INTO QIT_Location_Master(BranchID, Code, Location, GstIN) 
                    VALUES (@bId, (select ISNULL(max(code),0) + 1 from QIT_Location_Master), @location, @GstIn)";
                    _logger.LogInformation(" LocationsController : SaveLocation() Query : {q} ", _Query.ToString());

                    cmd = new SqlCommand(_Query, QITcon);
                    cmd.Parameters.AddWithValue("@bId", payload.BranchID);
                    cmd.Parameters.AddWithValue("@location", payload.Location);
                    cmd.Parameters.AddWithValue("@GstIn", payload.GSTIN);

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
                _logger.LogError("Error in LocationsController : SaveLocation() :: {Error}", ex.ToString());

                if (ex.Message.ToLower().Contains("uc_location"))
                {
                    return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Location already exist" });
                }
                else if (ex.Message.ToLower().Contains("uc_gstin"))
                {
                    return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "GSTIN already exist" });
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = ex.Message.ToString() });
                }
            }
        }


        [HttpPut("Update/{Code}")]
        public IActionResult UpdateLocation([FromBody] LocationIU payload, string Code)
        {
            string _IsSaved = "N";
            SqlConnection QITcon;
            try
            {
                _logger.LogInformation(" Calling LocationsController : UpdateLocation() ");

                string p_ErrorMsg = string.Empty;

                if (payload != null)
                {
                    if (payload.BranchID <= 0)
                        return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Provide Branch" });

                    QITcon = new SqlConnection(_QIT_connection);
                    _Query = @" UPDATE QIT_Location_Master SET Location = @loc, GstIN = @gst where Code = @code and ISNULL(BranchID, @bId) = @bId";
                    _logger.LogInformation(" LocationsController : UpdateLocation() Query : {q} ", _Query.ToString());

                    cmd = new SqlCommand(_Query, QITcon);
                    cmd.Parameters.AddWithValue("@bId", payload.BranchID);
                    cmd.Parameters.AddWithValue("@code", Code);
                    cmd.Parameters.AddWithValue("@loc", payload.Location);
                    cmd.Parameters.AddWithValue("@gst", payload.GSTIN);

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
                    return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = " Details not found" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Error in LocationsController : UpdateLocation() :: {Error}", ex.ToString());

                if (ex.Message.ToLower().Contains("uc_location"))
                {
                    return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Location already exist" });
                }
                else if (ex.Message.ToLower().Contains("uc_gstin"))
                {
                    return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "GSTIN already exist" });
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = ex.Message.ToString() });
                }
            }
        }


        [HttpDelete("Delete")]
        public IActionResult DeleteLocation(int BranchID, string ID)
        {
            string _IsSaved = "N";
            SqlConnection QITcon;
            try
            {
                _logger.LogInformation(" Calling LocationsController : DeleteLocation() ");

                if (BranchID <= 0)
                    return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Provide Branch" });

                QITcon = new SqlConnection(_QIT_connection);
                _Query = @" SELECT COUNT(*) FROM QIT_Location_Master WHERE Code = @code and ISNULL(BranchID, @bId) = @bId ";
                _logger.LogInformation(" LocationsController : Location Code Query : {q} ", _Query.ToString());
                QITcon.Open();
                cmd = new SqlCommand(_Query, QITcon);
                cmd.Parameters.AddWithValue("@bId", BranchID);
                cmd.Parameters.AddWithValue("@code", ID);
                Object Value = cmd.ExecuteScalar();
                QITcon.Close();
                if (Int32.Parse(Value.ToString()) > 0)
                {
                    _Query = @" SELECT COUNT(*) FROM QIT_Warehouse_Master WHERE Location = @LocCode and ISNULL(BranchID, @bId) = @bId ";
                    _logger.LogInformation(" LocationsController : Query : {q} ", _Query.ToString());

                    QITcon.Open();
                    
                    cmd = new SqlCommand(_Query, QITcon);
                    cmd.Parameters.AddWithValue("@bId", BranchID);
                    cmd.Parameters.AddWithValue("@LocCode", ID);
                    Value = cmd.ExecuteScalar();
                    QITcon.Close();
                    if (Int32.Parse(Value.ToString()) == 0)
                    {
                        _Query = @" DELETE FROM QIT_Location_Master WHERE Code = @code and ISNULL(BranchID, @bId) = @bId ";
                        _logger.LogInformation(" LocationsController : DeleteLocation() Query : {q} ", _Query.ToString());

                       
                        cmd = new SqlCommand(_Query, QITcon);
                        cmd.Parameters.AddWithValue("@bId", BranchID);
                        cmd.Parameters.AddWithValue("@code", ID);
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
                        return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Location is in use in Warehouse Master" });
                }
                else
                    return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Location Code does not exist : " + ID });

            }
            catch (Exception ex)
            {
                _logger.LogError("Error in LocationsController : DeleteLocation() :: {Error}", ex.ToString());
                return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = ex.ToString() });
            }
        }
               
    }
}
