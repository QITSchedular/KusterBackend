using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Data.SqlClient;
using System.Data;
using WMS_UI_API.Common;
using WMS_UI_API.Models;

namespace WMS_UI_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MachineTypeController : ControllerBase
    {
        private string _QIT_connection = string.Empty;
        private string _QIT_DB = string.Empty;
        private string _Query = string.Empty;
        private SqlCommand cmd;
        DataSet ds = new DataSet();
        private readonly ILogger<MachineTypeController> _logger;
        public IConfiguration Configuration { get; }

        public MachineTypeController(IConfiguration configuration, ILogger<MachineTypeController> AuthLogger)
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

        [HttpPost("Save")] 
        public IActionResult SaveMachineType([FromBody] MachineType payload)
        {
            string _IsSaved = "N";
            try
            {
                _logger.LogInformation("Calling MachineTypeController: SaveMachineType()");

                if (payload != null)
                {
                    if (payload.Locked.ToUpper() == "Y" || payload.Locked.ToUpper() == "N")
                    {
                        string connectionString = _QIT_connection;
                        using (SqlConnection con = new SqlConnection(connectionString))
                        {
                            con.Open();

                            string insertQuery = "INSERT INTO QIT_MachineType_Master (Name,Locked) VALUES (@Machine_type_Name,@Locked)";
                            SqlCommand insertCmd = new SqlCommand(insertQuery, con);
                            // Set parameters for the insert
                            insertCmd.Parameters.AddWithValue("@Machine_type_Name", payload.Name);
                            insertCmd.Parameters.AddWithValue("@Locked", payload.Locked.ToUpper());
                            int insertCount = insertCmd.ExecuteNonQuery();
                            if (insertCount > 0)
                                _IsSaved = "Y";
                            con.Close();

                            return Ok(new { StatusCode = "200", IsSaved = _IsSaved, StatusMsg = "Saved Successfully!!!" });
                        }
                    }
                    else
                    {
                        return BadRequest(new { StatusCode = "400", StatusMsg = "Locked value should Y/N" });
                    }
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Details not found" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Error in MachineTypeController : SaveMachineType() :: {Error}", ex.ToString());
                return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = ex.Message.ToString() });
            }
        }

        // GET: api/<MachineTypeController>
        [HttpGet("Get")]
        public async Task<ActionResult<IEnumerable<MachineType>>> GetMachineType(string Filter)
        {
            try
            {
                _logger.LogInformation(" Calling MachineTypeController : GetMachineType() ");
                string _where = string.Empty;
                if (Filter.ToUpper() == "Y" || Filter.ToUpper() == "N")
                    _where = " AND Locked = @Locked ";

                SqlConnection _QITConn = new SqlConnection(_QIT_connection);
                System.Data.DataTable dtData = new System.Data.DataTable();
                _Query = @"SELECT ID,Name,Locked  FROM QIT_MachineType_Master where 1=1 " + _where;


                _logger.LogInformation(" MachineTypeController :  GetMachineType Query : {q} ", _Query.ToString());
                SqlDataAdapter oAdptr = new SqlDataAdapter(_Query, _QITConn);

                _QITConn.Open();
                if (Filter.ToUpper() == "Y" || Filter.ToUpper() == "N")
                    oAdptr.SelectCommand.Parameters.AddWithValue("@Locked", Filter);
                oAdptr.Fill(dtData);
                _QITConn.Close();


                if (dtData.Rows.Count > 0)
                {
                    List<MachineType> obj = new List<MachineType>();
                    dynamic arData = JsonConvert.SerializeObject(dtData);
                    obj = JsonConvert.DeserializeObject<List<MachineType>>(arData);
                    return obj;
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "No data found" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(" Error in MachineTypeController : GetMachineType() :: {Error}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
            }
        }


        [HttpDelete("Delete")]
        public IActionResult DeleteMachineType(int id)
        {
            string _IsSaved = "N";
             
            try
            {
                _logger.LogInformation(" Calling MachineTypeController : DeleteMachineType() ");

                SqlConnection _QITConn = new SqlConnection(_QIT_connection);

                _Query = @" DELETE FROM QIT_MachineType_Master WHERE ID = @id";
                _logger.LogInformation(" MachineTypeController : DeleteMachineType() Query : {q} ", _Query.ToString());

                cmd = new SqlCommand(_Query, _QITConn);
                cmd.Parameters.AddWithValue("@id", id);
                _QITConn.Open();
                int intNum = cmd.ExecuteNonQuery();
                _QITConn.Close();

                if (intNum > 0)
                {
                    _IsSaved = "Y"; 
                }
                else
                {
                    _IsSaved = "N";
                }

                return Ok(new { StatusCode = "200", IsSaved = _IsSaved, StatusMsg = "Deleted Successfully!!!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(" Error in MachineTypeController : DeleteMachineType() :: {Error}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
            }
        }
    }
}
