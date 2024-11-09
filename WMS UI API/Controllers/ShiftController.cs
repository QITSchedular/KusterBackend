using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Data.SqlClient;
using System.Data;
using WMS_UI_API.Common;
using WMS_UI_API.Models;
using Newtonsoft.Json;

namespace WMS_UI_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ShiftController : ControllerBase
    {
        private string _QIT_connection = string.Empty;
        private string _QIT_DB = string.Empty;
        private string _Query = string.Empty;
        private SqlCommand cmd;
        DataSet ds = new DataSet();
        private readonly ILogger<ShiftController> _logger;
        public IConfiguration Configuration { get; }

        public ShiftController(IConfiguration configuration, ILogger<ShiftController> MachineLogger)
        {
            _logger = MachineLogger;
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


        // POST api/<ShiftController>
        [HttpPost("Save")]
        public IActionResult SaveShiftMaster([FromBody] ShiftMasterS payload)
        {
            string _IsSaved = "N";
            string _msg = "Details not found";
            int _StatusCode = 400;
            try
            {
                _logger.LogInformation(" Calling ShiftController : SaveShiftMaster() ");

                string p_ErrorMsg = string.Empty;
                if (payload.Locked.ToUpper() != "Y" || payload.Locked.ToUpper() != "N")
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Locked Values : Y / N " });
                if (payload.Name == string.Empty || payload.Name == null)
                {
                    return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Provide Shift Name." });
                }
                if (payload.Locked == string.Empty || payload.Locked == null)
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Locked Values : Y / N " });
                }
                if (payload.StartTime == null)
                {
                    return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Provide Shift start time." });
                }
                if (payload.EndTime == null)
                {
                    return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Provide Shift end time." });
                }

                if (payload != null)
                {
                    SqlConnection con = new SqlConnection(_QIT_connection);
                    _Query = @"INSERT INTO [dbo].[QIT_Shift_Master]
                                   ([Name]
                                   ,[Locked]
                                   ,[StartTime]
                                   ,[EndTime])
                             VALUES
                                   (@name
                                   ,@locked                                   
                                   ,@startTime
                                   ,@endTime
                                    )";
                    _logger.LogInformation(" ShiftController : SaveShiftMaster() Query : {q} ", _Query.ToString());

                    cmd = new SqlCommand(_Query, con);
                    cmd.Parameters.AddWithValue("@name", payload.Name);
                    cmd.Parameters.AddWithValue("@locked", payload.Locked);
                    cmd.Parameters.AddWithValue("@startTime", payload.StartTime);
                    cmd.Parameters.AddWithValue("@endTime", payload.EndTime);

                    con.Open();
                    int intNum = cmd.ExecuteNonQuery();
                    con.Close();

                    if (intNum > 0)
                    {
                        _IsSaved = "Y";
                        _StatusCode = 200;
                        _msg = "Saved Successfully!!!";
                    }
                    else
                        _IsSaved = "N";

                    return Ok(new { StatusCode = _StatusCode, IsSaved = _IsSaved, StatusMsg = _msg });
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = " Details not found" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Error in ShiftController : SaveShiftMaster() :: {Error}", ex.ToString());
                return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = ex.Message.ToString() });
            }
        }


        // PUT api/<ShiftController>/5
        [HttpPut("Update")]
        public IActionResult UpdateShiftMaster(int id, [FromBody] ShiftMasterU payload)
        {
            string _IsSaved = "N";
            string _msg = "Details not found";
            int _StatusCode = 400;
            try
            {
                _logger.LogInformation(" Calling ShiftController : UpdateShiftMaster() ");

                string p_ErrorMsg = string.Empty;

                if (payload != null)
                {
                    SqlConnection con = new SqlConnection(_QIT_connection);

                    string query = @" SELECT COUNT(*) FROM [dbo].[QIT_Shift_Master] WHERE Id=@id ";
                    _logger.LogInformation(" ShiftController : DeleteShiftMaster() : Query : {q} ", query.ToString());

                    if (con.State == ConnectionState.Closed)
                        con.Open();
                    SqlCommand cmd = new SqlCommand(query, con);
                    cmd.Parameters.AddWithValue("@id", id);

                    Object Value = cmd.ExecuteScalar();
                    con.Close();
                    if (Int32.Parse(Value.ToString()) > 0)
                    {


                        _Query = @"UPDATE [dbo].[QIT_Shift_Master]
                               SET [Name] = @name
                                  ,[Locked] = @locked
                                  ,[StartTime] = @startTime
                                  ,[EndTime] =  @endTime
                             WHERE Id=@id";
                        _logger.LogInformation(" ShiftController : UpdateShiftMaster() Query : {q} ", _Query.ToString());

                        cmd = new SqlCommand(_Query, con);
                        cmd.Parameters.AddWithValue("@name", payload.Name);
                        cmd.Parameters.AddWithValue("@locked", payload.Locked);
                        cmd.Parameters.AddWithValue("@startTime", payload.StartTime);
                        cmd.Parameters.AddWithValue("@EndTime", payload.EndTime);
                        cmd.Parameters.AddWithValue("@id", id);

                        con.Open();
                        int intNum = cmd.ExecuteNonQuery();
                        con.Close();

                        if (intNum > 0)
                        {
                            _IsSaved = "Y";
                            _StatusCode = 200;
                            _msg = "Updated Successfully!!!";
                        }
                        else
                            _IsSaved = "N";

                        return Ok(new { StatusCode = _StatusCode, IsSaved = _IsSaved, StatusMsg = _msg });
                    }
                    else
                        return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Shift details not found...!!" });

                }
                else
                {
                    return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = " Details not found" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Error in ShiftController : UpdateShiftMaster() :: {Error}", ex.ToString());
                return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = ex.Message.ToString() });
            }
        }


        // DELETE api/<ShiftController>/5
        [HttpDelete("Delete")]
        public IActionResult DeleteShiftMaster(int id)
        {
            string _IsSaved = "N";
            try
            {
                _logger.LogInformation(" Calling ShiftController : DeleteShiftMaster() ");

                string p_ErrorMsg = string.Empty;

                SqlConnection con = new SqlConnection(_QIT_connection);

                string query = @" SELECT COUNT(*) FROM [dbo].[QIT_Shift_Master] WHERE Id=@id ";
                _logger.LogInformation(" ShiftController : DeleteShiftMaster() : Query : {q} ", query.ToString());

                if (con.State == ConnectionState.Closed)
                    con.Open();
                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@id", id);

                Object Value = cmd.ExecuteScalar();
                con.Close();
                if (Int32.Parse(Value.ToString()) > 0)
                {
                    _Query = @"DELETE FROM [dbo].[QIT_Shift_Master] WHERE Id=@id";
                    _logger.LogInformation(" ShiftController : DeleteShiftMaster() Query : {q} ", _Query.ToString());

                    cmd = new SqlCommand(_Query, con);
                    cmd.Parameters.AddWithValue("@id", id);
                    con.Open();
                    int intNum = cmd.ExecuteNonQuery();
                    con.Close();

                    if (intNum > 0)
                        _IsSaved = "Y";
                    else
                        _IsSaved = "N";

                    return Ok(new { StatusCode = "200", IsSaved = _IsSaved, StatusMsg = "Deleted Successfully!!!" });
                }
                else
                    return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Shift does not exist" });
            }
            catch (Exception ex)
            {
                _logger.LogError("Error in ShiftController : DeleteShiftMaster() :: {Error}", ex.ToString());
                return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = ex.ToString() });
            }
        }


        // GET: api/<ShiftController>
        [HttpGet("Get")]
        public async Task<ActionResult<IEnumerable<ShiftMasterS>>> GetMachineMaster(String Filter)
        {
            _logger.LogInformation(" Calling ShiftController : GetMachineMaster() ");
            try
            {
                List<ShiftMasterS> obj = new List<ShiftMasterS>();
                DataTable dtPeriod = new DataTable();
                SqlConnection con = new SqlConnection(_QIT_connection);
                string _where = string.Empty;
                if (Filter.ToUpper() == "Y" || Filter.ToUpper() == "N")
                    _where = " AND Locked = @Locked ";
                _Query = @" SELECT * FROM QIT_Shift_Master WHERE 1=1 " + _where;
                _logger.LogInformation(" ShiftController :  GetMachineMaster() Query : {q} ", _Query.ToString());
                con.Open();
                SqlDataAdapter oAdptr = new SqlDataAdapter(_Query, con);
                if (Filter.ToUpper() == "Y" || Filter.ToUpper() == "N")
                    oAdptr.SelectCommand.Parameters.AddWithValue("@Locked", Filter);
                
                oAdptr.Fill(dtPeriod);
                con.Close();

                if (dtPeriod.Rows.Count > 0)
                {
                    dynamic arData = JsonConvert.SerializeObject(dtPeriod);
                    obj = JsonConvert.DeserializeObject<List<ShiftMasterS>>(arData.ToString());
                    return obj;
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "No data...!!" });
                }


            }
            catch (Exception ex)
            {
                _logger.LogError("Error in ShiftController :  GetMachineMaster() :: {Error}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.Message.ToString() });
            }
        }

    }
}
