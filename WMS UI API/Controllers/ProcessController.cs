using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Data.SqlClient;
using System.Data;
using WMS_UI_API.Common;
using Newtonsoft.Json;
using WMS_UI_API.Models;

namespace WMS_UI_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProcessController : ControllerBase
    {
        private string _QIT_connection = string.Empty;
        private string _QIT_DB = string.Empty;
        private string _Query = string.Empty;
        private SqlCommand cmd;
        DataSet ds = new DataSet();
        private readonly ILogger<AuthUserController> _logger;
        public IConfiguration Configuration { get; }

        public ProcessController(IConfiguration configuration, ILogger<AuthUserController> AuthLogger)
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
                _logger.LogError(" ProcessController Error : " + e.ToString());
            }
        }


        [HttpPost("Save")]
        public IActionResult SaveProcess([FromBody] Process payload)
        {
            string _IsSaved = "N";
            try
            {
                _logger.LogInformation("Calling ProcessController: SaveProcess()");

                if (payload != null)
                {
                    if (payload.Locked.ToString().ToUpper() != "Y" && payload.Locked.ToString().ToUpper() != "N")
                        return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Locked Values : Y / N " });
                                         
                    using (SqlConnection con = new SqlConnection(_QIT_connection))
                    {
                        con.Open();

                        _Query = "INSERT INTO QIT_Process_Master (Name,Locked) VALUES (@Process_Name,@Locked)";
                        cmd = new SqlCommand(_Query, con);

                        cmd.Parameters.AddWithValue("@Process_Name", payload.Name);
                        cmd.Parameters.AddWithValue("@Locked", payload.Locked.ToUpper());
                        int insertCount = cmd.ExecuteNonQuery();
                        if (insertCount > 0)
                            _IsSaved = "Y";
                        con.Close();

                        return Ok(new { StatusCode = "200", IsSaved = _IsSaved, StatusMsg = "Saved Successfully!!!" });
                    } 
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Details not found" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Error in ProcessController : SaveProcessMaster() :: {Error}", ex.ToString());
                return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = ex.Message.ToString() });
            }
        }
        

        // PUT api/<ProcessController>/5
        [HttpPut("Update")]
        public IActionResult UpdateProcess(int id, [FromBody] ProcessU payload)
        {
            string _IsSaved = "N";
            string _msg = "Details not found";
            int _StatusCode = 400;
            try
            {
                _logger.LogInformation(" Calling ProcessController : UpdateProcess() ");

                string p_ErrorMsg = string.Empty;

                if (payload != null)
                {
                    SqlConnection con = new SqlConnection(_QIT_connection);

                    string query = @" SELECT COUNT(*) FROM QIT_Process_Master WHERE ID = @id ";
                    _logger.LogInformation(" ProcessController : Query : {q} ", query.ToString());
                    if (con.State == ConnectionState.Closed)
                        con.Open();
                    SqlCommand cmd = new SqlCommand(query, con);
                    cmd.Parameters.AddWithValue("@id", id);
                    Object Value = cmd.ExecuteScalar();
                    _logger.LogInformation(" ProcessController Object : Query : {q} {R} ", query.ToString(), Value.ToString());
                    con.Close();
                    if (Int32.Parse(Value.ToString()) > 0)
                    {
                        _Query = @"UPDATE [dbo].[QIT_Process_Master]
                               SET [Name] = @name
                                  ,[Locked] = @locked
                             WHERE Id=@id";
                        _logger.LogInformation(" ProcessController : UpdateProcess() Query : {q} ", _Query.ToString());

                        cmd = new SqlCommand(_Query, con);
                        cmd.Parameters.AddWithValue("@name", payload.Name);
                        cmd.Parameters.AddWithValue("@locked", payload.Locked);
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
                        return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Process does not exist" });


                }
                else
                {
                    return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Details not found" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Error in ProcessController : UpdateProcess() :: {Error}", ex.ToString());
                return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = ex.Message.ToString() });
            }
        }


        [HttpGet("Get")]
        public async Task<ActionResult<IEnumerable<Process>>> GetProcessMaster(string Filter)
        {
            try
            {
                _logger.LogInformation(" Calling ProcessController : GetProcessMaster() ");
                string _where = string.Empty;
                if (Filter.ToUpper() == "Y" || Filter.ToUpper() == "N")
                    _where = " AND Locked = @Locked ";

                SqlConnection _QITConn = new SqlConnection(_QIT_connection);
                System.Data.DataTable dtData = new System.Data.DataTable();
                _Query = @"SELECT ID,Name,Locked Locked  FROM QIT_Process_Master where 1=1 " + _where;


                _logger.LogInformation(" ProcessController :  GetProcess Query : {q} ", _Query.ToString());
                SqlDataAdapter oAdptr = new SqlDataAdapter(_Query, _QITConn);

                _QITConn.Open();
                if (Filter.ToUpper() == "Y" || Filter.ToUpper() == "N")
                    oAdptr.SelectCommand.Parameters.AddWithValue("@Locked", Filter);
                
                oAdptr.Fill(dtData);
                _QITConn.Close();


                if (dtData.Rows.Count > 0)
                {
                    List<Process> obj = new List<Process>();
                    dynamic arData = JsonConvert.SerializeObject(dtData);
                    obj = JsonConvert.DeserializeObject<List<Process>>(arData);
                    return obj;
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "No data found" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(" Error in ProcessController : GetProcess() :: {Error}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
            }
        }


        [HttpDelete("Delete")]
        public IActionResult DeleteProcessMaster(int id)
        {
            string _IsSaved = "N";
            try
            {
                _logger.LogInformation(" Calling ProcessController : DeleteProcess() ");

                string p_ErrorMsg = string.Empty;

                SqlConnection con = new SqlConnection(_QIT_connection);

                string query = @" SELECT COUNT(*) FROM QIT_Process_Master WHERE ID = @id ";
                _logger.LogInformation(" ProcessController : Query : {q} ", query.ToString());
                if (con.State == ConnectionState.Closed)
                    con.Open();
                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@id", id);
                Object Value = cmd.ExecuteScalar();
                _logger.LogInformation(" ProcessController Object : Query : {q} {R} ", query.ToString(), Value.ToString());
                con.Close();
                if (Int32.Parse(Value.ToString()) > 0)
                {
                    _Query = @" DELETE FROM QIT_Process_Master WHERE ID = @id";
                    _logger.LogInformation(" ProcessController : DeleteProcessMaster() Query : {q} ", _Query.ToString());

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
                    return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Process does not exist" });
            }
            catch (Exception ex)
            {
                _logger.LogError(" Error in ProcessController : DeleteProcess() :: {Error}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
            }
        }

    }
}
