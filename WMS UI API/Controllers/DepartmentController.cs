using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Data;
using System.Data.SqlClient;
using WMS_UI_API.Common;
using WMS_UI_API.Models;

namespace WMS_UI_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DepartmentController : ControllerBase
    {
        private string _ApplicationApiKey = string.Empty;
        private string _connection = string.Empty;
        private string _QIT_connection = string.Empty;
        private string _QIT_DB = string.Empty;
        private string _Query = string.Empty;
        private SqlCommand cmd;
        public IConfiguration Configuration { get; }
        private readonly ILogger<DepartmentController> _logger;


        public DepartmentController(IConfiguration configuration, ILogger<DepartmentController> logger)
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
                _logger.LogError(" Error in DepartmentController :: {Error}" + ex.ToString());
            }
        }


        [HttpGet("Get")]
        public async Task<ActionResult<IEnumerable<DepartmentG>>> GetDepartment(string Filter)
        {
            try
            {
                _logger.LogInformation(" Calling DepartmentController : GetItem() ");
                List<DepartmentG> obj = new List<DepartmentG>();
                System.Data.DataTable dtData = new System.Data.DataTable();
                SqlConnection con = new SqlConnection(_QIT_connection);

                string _where = string.Empty;
                if (Filter.ToUpper() == "Y" || Filter.ToUpper() == "N")
                    _where = " AND A.Locked = @Locked ";

                _Query = @" SELECT A.DeptId, A.DeptName, A.Locked 
                            FROM QIT_Dept_Master A
                            WHERE 1=1 " + _where +
                          " ORDER BY A.DeptId ";
                _logger.LogInformation(" DepartmentController : GetDepartment() Query : {q} ", _Query.ToString());
                con.Open();
                SqlDataAdapter oAdptr = new SqlDataAdapter(_Query, con);
                if (Filter.ToUpper() == "Y" || Filter.ToUpper() == "N")
                    oAdptr.SelectCommand.Parameters.AddWithValue("@Locked", Filter);
                oAdptr.Fill(dtData);
                con.Close();

                if (dtData.Rows.Count > 0)
                {
                    dynamic arData = JsonConvert.SerializeObject(dtData);
                    obj = JsonConvert.DeserializeObject<List<DepartmentG>>(arData.ToString());
                    return obj;
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "No data found" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(" Error in DepartmentController : GetDepartment() :: {Error}", ex.ToString());
                return BadRequest(new { StatusCode = "400", IsSaved = "N", StatusMsg = ex.Message.ToString() });
            }
        }


        [HttpPost("Save")]
        public IActionResult SaveDepartment([FromBody] DepartmentS payload)
        {
            string _IsSaved = "N";
            try
            {
                _logger.LogInformation(" Calling DepartmentController : SaveDepartment() ");

                string p_ErrorMsg = string.Empty;

                if (payload != null)
                {
                    #region Check for Is Active 
                    int _active = payload.Locked.ToString().Length;
                    if (_active > 1)
                    {
                        return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Locked Values : Y / N " });
                    }
                    else
                    {
                        if (payload.Locked.ToString().ToUpper() != "Y" && payload.Locked.ToString().ToUpper() != "N")
                            return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Locked Values : Y / N " });
                    }
                    #endregion

                    SqlConnection con = new SqlConnection(_QIT_connection);
                    _Query = @"INSERT INTO QIT_Dept_Master(DeptName, Locked) VALUES (@deptName, @Locked)";
                    _logger.LogInformation(" DepartmentController : SaveDepartment() Query : {q} ", _Query.ToString());

                    cmd = new SqlCommand(_Query, con);
                    cmd.Parameters.AddWithValue("@deptName", payload.DeptName);
                    cmd.Parameters.AddWithValue("@Locked", payload.Locked);

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
                _logger.LogError("Error in DepartmentController : SaveDepartment() :: {Error}", ex.ToString());

                if (ex.Message.ToLower().Contains("uc_deptname"))
                {
                    return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Department already exist" });
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = ex.Message.ToString() });
                }
            }
        }


        [HttpPut("Update/{DeptId}")]
        public IActionResult UpdateDepartment(int DeptId, string Locked)
        {
            string _IsSaved = "N";
            try
            {
                _logger.LogInformation(" Calling DepartmentController : UpdateDepartment() ");


                SqlConnection con = new SqlConnection(_QIT_connection);

                _Query = @" select count(*) from QIT_Dept_Master where DeptId = @deptId ";
                cmd = new SqlCommand(_Query, con);
                cmd.Parameters.AddWithValue("@deptId", DeptId);
                con.Open();
                Object Value = cmd.ExecuteScalar();
                con.Close();
                if (Int32.Parse(Value.ToString()) == 0)
                {
                    return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "No such Department exist" });
                }


                #region Check for Is Active 
                int _active = Locked.ToString().Length;
                if (_active > 1)
                {
                    return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Locked Values : Y / N " });
                }
                else
                {
                    if (Locked.ToString().ToUpper() != "Y" && Locked.ToString().ToUpper() != "N")
                        return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Locked Values : Y / N " });
                }
                #endregion

                _Query = @" UPDATE QIT_Dept_Master 
                            SET Locked = @Locked 
                            WHERE DeptId = @deptId";
                _logger.LogInformation(" DepartmentController : UpdateDepartment() Query : {q} ", _Query.ToString());

                cmd = new SqlCommand(_Query, con);
                cmd.Parameters.AddWithValue("@Locked", Locked);
                cmd.Parameters.AddWithValue("@deptId", DeptId);

                con.Open();
                int intNum = cmd.ExecuteNonQuery();
                con.Close();

                if (intNum > 0)
                    _IsSaved = "Y";
                else
                    _IsSaved = "N";

                return Ok(new { StatusCode = "200", IsSaved = _IsSaved, StatusMsg = "Updated Successfully!!!" });
            }
            catch (Exception ex)
            {
                _logger.LogError("Error in DepartmentController : UpdateDepartment() :: {Error}", ex.ToString());
                return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = ex.Message.ToString() });
            }
        }


        [HttpDelete("Delete")]
        public IActionResult DeleteDepartment(int DeptId)
        {
            string _IsSaved = "N";
            try
            {
                _logger.LogInformation(" Calling DepartmentController : DeleteDepartment() ");

                string p_ErrorMsg = string.Empty;

                SqlConnection con = new SqlConnection(_QIT_connection);

                string query = @" SELECT COUNT(*) FROM QIT_Dept_Master WHERE DeptId = @deptId ";
                _logger.LogInformation(" DepartmentController : Query : {q} ", query.ToString());

                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@deptId", DeptId);
                con.Open();
                Object Value = cmd.ExecuteScalar();
                con.Close();
                if (Int32.Parse(Value.ToString()) > 0)
                {
                    _Query = @" DELETE FROM QIT_Dept_Master WHERE DeptId = @deptId ";
                    _logger.LogInformation(" DepartmentController : DeleteDepartmentGroup() Query : {q} ", _Query.ToString());

                    cmd = new SqlCommand(_Query, con);
                    cmd.Parameters.AddWithValue("@deptId", DeptId);
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
                    return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "No such Department exist" });
            }
            catch (Exception ex)
            {
                _logger.LogError("Error in DepartmentController : DeleteDepartmentGroup() :: {Error}", ex.ToString());
                return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = ex.ToString() });
            }
        }
    }
}
