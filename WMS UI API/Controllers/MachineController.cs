using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Data;
using System.Data.SqlClient;
using WMS_UI_API.Common;
using WMS_UI_API.Models;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace WMS_UI_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MachineMasterController : ControllerBase
    {
        private string _QIT_connection = string.Empty;
        private string _QIT_DB = string.Empty;
        private string _Query = string.Empty;
        private SqlCommand cmd;
        DataSet ds = new DataSet();
        private readonly ILogger<MachineMasterController> _logger;
        public IConfiguration Configuration { get; }

        public MachineMasterController(IConfiguration configuration, ILogger<MachineMasterController> MachineLogger)
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

        private string GetLatestMachineId(SqlConnection con)
        {
            string query = "SELECT TOP 1 MachineId FROM QIT_Machine_master ORDER BY MachineId DESC";

            using (SqlCommand cmd = new SqlCommand(query, con))
            using (SqlDataReader reader = cmd.ExecuteReader())
            {
                if (reader.Read())
                {
                    //return reader["MachineId"].ToString();
                    // Step 2: Increment the MachineId
                    string newNumericPart = (int.Parse(reader["MachineId"].ToString().Substring(1)) + 1).ToString("D5");
                    string newMachineId = "M" + newNumericPart;

                    // Step 3: Use the new MachineId for the new machine
                    return newMachineId;
                }
            }
            return string.Empty;
        }



        // GET: api/<MachineMasterController>
        [HttpGet("Get")]
        public async Task<ActionResult<IEnumerable<MachineMasterG>>> GetMachineMaster()
        {
            _logger.LogInformation(" Calling MachineMasterController : GRN_QC_Compression() ");
            try
            {
                List<MachineMasterG> obj = new List<MachineMasterG>();
                DataTable dtPeriod = new DataTable();
                SqlConnection con = new SqlConnection(_QIT_connection);
                _Query = @" SELECT A.MachineID,A.MachineSrNo,A.MachineNo,A.MachineName,A.MachineSpec,A.Make,A.Model,A.Location,B.Name MachineTypeName FROM QIT_Machine_Master A,QIT_MachineType_Master B WHERE A.MachineTypeID=B.ID";
                _logger.LogInformation(" MachineMasterController :  Get Query : {q} ", _Query.ToString());
                con.Open();
                SqlDataAdapter oAdptr = new SqlDataAdapter(_Query, con);
                oAdptr.Fill(dtPeriod);
                con.Close();

                if (dtPeriod.Rows.Count > 0)
                {
                    dynamic arData = JsonConvert.SerializeObject(dtPeriod);
                    obj = JsonConvert.DeserializeObject<List<MachineMasterG>>(arData.ToString());
                    return obj;
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "No data...!!" });
                }


            }
            catch (Exception ex)
            {
                _logger.LogError("Error in MachineMasterController : Get() :: {Error}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.Message.ToString() });
            }
        }


        // POST api/<MachineMasterController>
        [HttpPost("Save")]
        public IActionResult SaveMachineMaster([FromBody] MachineMasterS payload)
        {
            string _IsSaved = "N";
            string _msg = "Details not found";
            int _StatusCode = 400;
            try
            {
                _logger.LogInformation(" Calling MachineMasterController : Post() ");
                DataTable dtPeriod = new DataTable();

                string p_ErrorMsg = string.Empty;
                if (payload.MachineNo <= 0 || payload.MachineSrNo == null)
                {
                    return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Provide Machine No." });
                }
                if (payload.MachineName == string.Empty || payload.MachineName == null)
                {
                    return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Provide Machine Name." });
                }
                if (payload.Location == string.Empty || payload.Location == null)
                {
                    return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Provide Location." });
                }
                if (payload.MachinetypeID <= 0 || payload.MachinetypeID == null)
                {
                    return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Provide Machine type." });
                }

                if (payload != null)
                {
                    SqlConnection con = new SqlConnection(_QIT_connection);
                    con.Open();
                    string latestMachineId = GetLatestMachineId(con);
                    if (latestMachineId != null)
                    {
                        _Query = @"INSERT INTO [dbo].[QIT_Machine_Master]
                                   ([MachineId]
                                   ,[MachineSrNo]
                                   ,[MachineNo]
                                   ,[MachineName]
                                   ,[MachineSpec]
                                   ,[Make]
                                   ,[Model]
                                   ,[Location]
                                   ,[MachineTypeID])
                             VALUES
                                   (@MachineID
                                   ,@Machine_SrNo
                                   ,@Machine_No
                                   ,@Machine_Name
                                   ,@Machine_Spec
                                   ,@Make
                                   ,@Model
                                   ,@Location
                                   ,@Machine_type_ID)";
                        _logger.LogInformation(" MachineMasterController : Post() Query : {q} ", _Query.ToString());

                        cmd = new SqlCommand(_Query, con);
                        cmd.Parameters.AddWithValue("@MachineID", latestMachineId);
                        cmd.Parameters.AddWithValue("@Machine_SrNo", payload.MachineSrNo);
                        cmd.Parameters.AddWithValue("@Machine_No", payload.MachineNo);
                        cmd.Parameters.AddWithValue("@Machine_Name", payload.MachineName);
                        cmd.Parameters.AddWithValue("@Machine_Spec", payload.MachineSpec);
                        cmd.Parameters.AddWithValue("@Make", payload.Make);
                        cmd.Parameters.AddWithValue("@Model", payload.Model);
                        cmd.Parameters.AddWithValue("@Location", payload.Location);
                        cmd.Parameters.AddWithValue("@Machine_type_ID", payload.MachinetypeID);

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
                        return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Error while generating QR code..!!" });
                    }


                }
                else
                {
                    return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = " Details not found" });
                }
            }
            catch (Exception ex)
            {
                if (ex.Message.ToString().Contains("FOREIGN KEY"))
                {
                    return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Machine Type does not found...!!" });
                }
                _logger.LogError("Error in MachineMasterController : Post() :: {Error}", ex.ToString());
                return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = ex.Message.ToString() });
            }
        }

        // PUT api/<MachineMasterController>/5
        [HttpPut("Update")]
        public IActionResult UpdateMachineMaster(int id, [FromBody] MachineMasterU payload)
        {
            string _IsSaved = "N";
            string _msg = "Details not found";
            int _StatusCode = 400;
            try
            {
                _logger.LogInformation(" Calling MachineMasterController : Post() ");

                string p_ErrorMsg = string.Empty;
                if (payload.MachineNo == 0)
                {
                    return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Provide Machine No." });
                }
                if (payload.MachinetypeID == 0)
                {
                    return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Provide Machine type." });
                }

                if (payload != null)
                {
                    SqlConnection con = new SqlConnection(_QIT_connection);
                    _Query = @"UPDATE [dbo].[QIT_Machine_Master]
                               SET [MachineSrNo] = @Machine_SrNo
                                  ,[MachineNo] = @Machine_No
                                  ,[MachineName] = @Machine_Name
                                  ,[MachineSpec] = @Machine_Spec
                                  ,[Make] = @Make
                                  ,[Model] = @Model
                                  ,[Location] = @Location
                                  ,[MachineTypeID] = @Machine_type_ID
                             WHERE MachineID=@Machine_ID";
                    _logger.LogInformation(" MachineMasterController : Post() Query : {q} ", _Query.ToString());

                    cmd = new SqlCommand(_Query, con);
                    cmd.Parameters.AddWithValue("@Machine_ID", id);
                    cmd.Parameters.AddWithValue("@Machine_SrNo", payload.MachineSrNo);
                    cmd.Parameters.AddWithValue("@Machine_No", payload.MachineNo);
                    cmd.Parameters.AddWithValue("@Machine_Name", payload.MachineName);
                    cmd.Parameters.AddWithValue("@Machine_Spec", payload.MachineSpec);
                    cmd.Parameters.AddWithValue("@Make", payload.Make);
                    cmd.Parameters.AddWithValue("@Model", payload.Model);
                    cmd.Parameters.AddWithValue("@Location", payload.Location);
                    cmd.Parameters.AddWithValue("@Machine_type_ID", payload.MachinetypeID);

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
                {
                    return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = " Details not found" });
                }
            }
            catch (Exception ex)
            {
                if (ex.Message.ToString().Contains("FOREIGN KEY"))
                {
                    return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Machine Type does not found...!!" });
                }
                _logger.LogError("Error in MachineMasterController : Post() :: {Error}", ex.ToString());
                return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = ex.Message.ToString() });
            }
        }

        // DELETE api/<MachineMasterController>/5
        [HttpDelete("Delete")]
        public IActionResult DeleteMachineMaster(int id)
        {
            string _IsSaved = "N";
            try
            {
                _logger.LogInformation(" Calling MachineMasterController : Delete() ");

                string p_ErrorMsg = string.Empty;

                SqlConnection con = new SqlConnection(_QIT_connection);

                string query = @" SELECT COUNT(*) FROM [dbo].[QIT_Machine_Master] WHERE MachineID=@MachineID ";
                _logger.LogInformation(" MachineMasterController : Query : {q} ", query.ToString());

                if (con.State == ConnectionState.Closed)
                    con.Open();
                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@MachineID", id);

                Object Value = cmd.ExecuteScalar();
                con.Close();
                if (Int32.Parse(Value.ToString()) > 0)
                {
                    _Query = @"DELETE FROM [dbo].[QIT_Machine_Master] WHERE MachineID=@MachineID";
                    _logger.LogInformation(" MachineMasterController : Delete() Query : {q} ", _Query.ToString());

                    cmd = new SqlCommand(_Query, con);
                    cmd.Parameters.AddWithValue("@MachineID", id);
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
                    return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Machine details not found...!!" });
            }
            catch (Exception ex)
            {
                _logger.LogError("Error in MachineMasterController : Delete() :: {Error}", ex.ToString());
                return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = ex.ToString() });
            }
        }


    }
}
