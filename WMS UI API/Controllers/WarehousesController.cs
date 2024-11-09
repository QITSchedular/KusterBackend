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
    public class WarehousesController : ControllerBase
    {
        private string _ApplicationApiKey = string.Empty;
        private string _connection = string.Empty;
        private string _QIT_connection = string.Empty;
        private string _QIT_DB = string.Empty;
        private string _Query = string.Empty;
        private SqlCommand cmd;

        public IConfiguration Configuration { get; }
        private readonly ILogger<WarehousesController> _logger;

        public WarehousesController(IConfiguration configuration, ILogger<WarehousesController> logger)
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
                _logger.LogError(" Error in WarehousesController :: {Error}" + ex.ToString());
            }
        }


        [HttpGet("Get")]
        public async Task<ActionResult<IEnumerable<Warehouse>>> GetWarehouse(int BranchID, string Filter)
        {
            SqlConnection QITcon;
            SqlDataAdapter oAdptr;
            try
            {
                _logger.LogInformation(" Calling WarehousesController : GetWarehouse() ");

                if (BranchID <= 0)
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Provide Branch" });

                System.Data.DataTable dtData = new System.Data.DataTable();
                QITcon = new SqlConnection(_QIT_connection);

                string _where = string.Empty;
                if (Filter.ToUpper() == "Y" || Filter.ToUpper() == "N")
                    _where = " AND A.Locked = @locked ";

                _Query = @"  
                SELECT A.WhsCode, A.WhsName, A.Location LocCode, B.Location,  
                       CASE WHEN A.Locked = 'Y' then 'Yes' else 'No' end Locked,  
                       CASE WHEN A.BinActivat = 'Y' then 'Yes' else 'No' end BinActivat, A.ObjType   
                FROM QIT_Warehouse_Master A  
                INNER JOIN QIT_Location_Master B ON A.Location = B.Code  
                WHERE 1 = 1 AND ISNULL(A.BranchID, @bId) = @bId " + _where + @"
                ORDER BY A.WhsCode ";

                _logger.LogInformation(" WarehousesController : GetWarehouse() Query : {q} ", _Query.ToString());
                QITcon.Open();
                oAdptr = new SqlDataAdapter(_Query, QITcon);
                oAdptr.SelectCommand.Parameters.AddWithValue("@bId", BranchID);
                if (Filter.ToUpper() == "Y" || Filter.ToUpper() == "N")
                    oAdptr.SelectCommand.Parameters.AddWithValue("@locked", Filter);
                oAdptr.Fill(dtData);
                QITcon.Close();

                if (dtData.Rows.Count > 0)
                {
                    List<Warehouse> obj = new List<Warehouse>();
                    dynamic arData = JsonConvert.SerializeObject(dtData);
                    obj = JsonConvert.DeserializeObject<List<Warehouse>>(arData.ToString());
                    return obj;
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "No data found" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(" Error in WarehousesController : GetWarehouse() :: {Error}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
            }
        }


        [HttpPost("Save")]
        public IActionResult SaveWarehouse([FromBody] WarehouseSave payload)
        {
            string _IsSaved = "N";
            SqlConnection QITcon;
            try
            {
                _logger.LogInformation(" Calling WarehousesController : SaveWarehouse() ");


                if (payload != null)
                {
                    if (payload.BranchID <= 0)
                        return BadRequest(new { StatusCode = "400", StatusMsg = "Provide Branch" });

                    QITcon = new SqlConnection(_QIT_connection);
                    _Query = @"insert into QIT_Warehouse_Master(BranchID, WhsCode, WhsName, Location, Locked, BinActivat) 
                           VALUES (@bId, @wCode, @name, @locCode, @locked, @bin)";
                    _logger.LogInformation(" WarehousesController : SaveWarehouse() Query : {q} ", _Query.ToString());

                    cmd = new SqlCommand(_Query, QITcon);
                    cmd.Parameters.AddWithValue("@bId", payload.BranchID);
                    cmd.Parameters.AddWithValue("@wCode", payload.WhsCode);
                    cmd.Parameters.AddWithValue("@name", payload.WhsName);
                    cmd.Parameters.AddWithValue("@locCode", payload.LocCode);
                    cmd.Parameters.AddWithValue("@locked", payload.Locked);
                    cmd.Parameters.AddWithValue("@bin", payload.BinActivat);
                    
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
                _logger.LogError("Error in WarehousesController : SaveWarehouse() :: {Error}", ex.ToString());

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


        [HttpPut("Update/{Code}")]
        public IActionResult UpdateWarehouse([FromBody] WarehouseUpdate payload, string Code)
        {
            string _IsSaved = "N";
            SqlConnection QITcon;
            try
            {
                _logger.LogInformation(" Calling WarehousesController : UpdateWarehouse() ");

                if (payload != null)
                {
                    if (payload.BranchID <= 0)
                        return BadRequest(new { StatusCode = "400", StatusMsg = "Provide Branch" });

                    QITcon = new SqlConnection(_QIT_connection);
                    _Query = @" 
                    UPDATE QIT_Warehouse_Master 
                    SET WhsName=@wName, Location=@locCode, Locked=@locked, BinActivat=@bin 
                    where WhsCode = @code and ISNULL(BranchID, @bId) = @bId";
                    _logger.LogInformation(" WarehousesController : UpdateWarehouse() Query : {q} ", _Query.ToString());

                    cmd = new SqlCommand(_Query, QITcon);
                    cmd.Parameters.AddWithValue("@bId", payload.BranchID);
                    cmd.Parameters.AddWithValue("@code", Code);
                    cmd.Parameters.AddWithValue("@wName", payload.WhsName);
                    cmd.Parameters.AddWithValue("@locCode", payload.LocCode);
                    cmd.Parameters.AddWithValue("@locked", payload.Locked);
                    cmd.Parameters.AddWithValue("@bin", payload.BinActivat);

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
                _logger.LogError("Error in WarehousesController : UpdateWarehouse() :: {Error}", ex.ToString());

                if (ex.Message.ToLower().Contains("uc_whsname"))
                {
                    return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Warehouse Name already exist" });
                }
                else if (ex.Message.ToLower().Contains("uc_whscode"))
                {
                    return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Warehouse Code already exist" });
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = ex.Message.ToString() });
                }
            }
        }


        [HttpDelete("Delete")]
        public IActionResult DeleteWarehouse(int BranchID, string ID)
        {
            SqlConnection QITcon;
            string _IsSaved = "N";
            try
            {
                _logger.LogInformation(" Calling WarehousesController : DeleteWarehouse() ");

                QITcon = new SqlConnection(_QIT_connection);
                _Query = @" DELETE FROM QIT_Warehouse_Master WHERE WhsCode = @code and ISNULL(BranchID, @bId) = @bId";
                _logger.LogInformation(" WarehousesController : DeleteWarehouse() Query : {q} ", _Query.ToString());

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
            catch (Exception ex)
            {
                _logger.LogError("Error in WarehousesController : DeleteWarehouse() :: {Error}", ex.ToString());
                return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = ex.ToString() });
            }
        }
    }
}
