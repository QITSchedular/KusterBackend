using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using SAPbobsCOM;
using System.Data;
using System.Data.SqlClient;
using System.Reflection.Metadata;
using WMS_UI_API.Common;
using WMS_UI_API.Models;

namespace WMS_UI_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ItemGroupsController : ControllerBase
    {
        private string _ApplicationApiKey = string.Empty;
        private string _connection = string.Empty;
        private string _QIT_connection = string.Empty;
        private string _QIT_DB = string.Empty;
        private string _Query = string.Empty;
        private SqlCommand cmd;
        public IConfiguration Configuration { get; }
        private readonly ILogger<ItemGroupsController> _logger;


        public ItemGroupsController(IConfiguration configuration, ILogger<ItemGroupsController> logger)
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
                _logger.LogError(" Error in ItemGroupsController :: {Error}" + ex.ToString());
            }
        }


        [HttpGet("Get")]
        public async Task<ActionResult<IEnumerable<ItemGroupS>>> GetItemGroup(int BranchID, string Filter)
        {
            SqlConnection QITcon;
            SqlDataAdapter oAdptr;
            try
            {
                _logger.LogInformation(" Calling ItemGroupsController : GetItemGroup() ");

                System.Data.DataTable dtData = new System.Data.DataTable();
                QITcon = new SqlConnection(_QIT_connection);

                string _where = string.Empty;
                if (Filter.ToUpper() == "Y" || Filter.ToUpper() == "N")
                    _where = " AND A.Locked = @locked ";

                _Query = @"  SELECT A.ItmsGrpCod, A.ItmsGrpNam, B.QRMngByName QRMngBy,  
                                    CASE WHEN A.Locked = 'Y' then 'Yes' else 'No' end Locked, A.ObjType  
                             FROM QIT_ItemGroup_Master A  
                                  INNER JOIN QIT_QRMngBy_Master B on A.QRMngBy = B.QRMngById 
                             WHERE 1 = 1 AND ISNULL(A.BranchID, @bId) = @bId " + _where + @"
                             ORDER BY ItmsGrpCod ";
                _logger.LogInformation(" ItemGroupsController : GetItemGroup() Query : {q} ", _Query.ToString());
                QITcon.Open();
                oAdptr = new SqlDataAdapter(_Query, QITcon);
                oAdptr.SelectCommand.Parameters.AddWithValue("@bId", BranchID);
                if (Filter.ToUpper() == "Y" || Filter.ToUpper() == "N")
                    oAdptr.SelectCommand.Parameters.AddWithValue("@locked", Filter);
                oAdptr.Fill(dtData);
                QITcon.Close();

                if (dtData.Rows.Count > 0)
                {
                    List<ItemGroupS> obj = new List<ItemGroupS>();
                    dynamic arData = JsonConvert.SerializeObject(dtData);
                    obj = JsonConvert.DeserializeObject<List<ItemGroupS>>(arData.ToString());
                    return obj;
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "No data found" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(" Error in ItemGroupsController : GetItemGroup() :: {Error}", ex.ToString());
                return BadRequest(new { StatusCode = "400", IsSaved = "N", StatusMsg = ex.Message.ToString() });
            }
        }


        [HttpPost("Save")]
        public IActionResult SaveItemGroup([FromBody] ItemGroupIU payload)
        {
            SqlConnection QITcon;
            string _IsSaved = "N";
            try
            {
                _logger.LogInformation(" Calling ItemGroupsController : SaveItemGroup() ");

                if (payload != null)
                {
                    if (payload.BranchID <= 0)
                        return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Provide Branch" });

                    QITcon = new SqlConnection(_QIT_connection);
                    _Query = @"
                    INSERT INTO QIT_ItemGroup_Master(BranchID, ItmsGrpCod, ItmsGrpNam, QRMngBy, Locked) 
                    VALUES (@bId, (select ISNULL(max(ItmsGrpCod),0) + 1 FROM QIT_ItemGroup_Master), @name, @MngBy, @locked)";
                    _logger.LogInformation(" ItemGroupsController : SaveItemGroup() Query : {q} ", _Query.ToString());

                    cmd = new SqlCommand(_Query, QITcon);
                    cmd.Parameters.AddWithValue("@bId", payload.BranchID);
                    cmd.Parameters.AddWithValue("@name", payload.ItmsGrpNam);
                    cmd.Parameters.AddWithValue("@MngBy", payload.QRMngBy);
                    cmd.Parameters.AddWithValue("@locked", payload.Locked);

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
                    return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Details not found" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Error in ItemGroupsController : SaveItemGroup() :: {Error}", ex.ToString());

                if (ex.Message.ToLower().Contains("uc_itemgroupname"))
                {
                    return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Item Group already exist" });
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = ex.Message.ToString() });
                }
            }
        }


        [HttpPut("Update/{Code}")]
        public IActionResult UpdateItemGroup([FromBody] ItemGroupIU payload, string Code)
        {
            SqlConnection QITcon;
            string _IsSaved = "N";
            try
            {
                _logger.LogInformation(" Calling ItemGroupsController : UpdateItemGroup() ");

                if (payload != null)
                {
                    if (payload.BranchID <= 0)
                        return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Provide Branch" });

                    QITcon = new SqlConnection(_QIT_connection);
                    _Query = @" 
                    UPDATE QIT_ItemGroup_Master 
                    SET ItmsGrpNam = @name, QRMngBy = @qr, Locked = @locked 
                    WHERE ItmsGrpCod = @code and ISNULL(BranchID, @bId) = @bId ";
                    _logger.LogInformation(" ItemGroupsController : UpdateItemGroup() Query : {q} ", _Query.ToString());

                    cmd = new SqlCommand(_Query, QITcon);
                    cmd.Parameters.AddWithValue("@bId", payload.BranchID);
                    cmd.Parameters.AddWithValue("@code", Code);
                    cmd.Parameters.AddWithValue("@name", payload.ItmsGrpNam);
                    cmd.Parameters.AddWithValue("@qr", payload.QRMngBy);
                    cmd.Parameters.AddWithValue("@locked", payload.Locked);

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
                _logger.LogError("Error in ItemGroupsController : UpdateItemGroup() :: {Error}", ex.ToString());

                if (ex.Message.ToLower().Contains("uc_itemgroupname"))
                {
                    return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Item Group already exist" });
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = ex.Message.ToString() });
                }
            }
        }


        [HttpDelete("Delete")]
        public IActionResult DeleteItemGroup(int BranchID, string ID)
        {
            SqlConnection QITcon;
            string _IsSaved = "N";
            try
            {
                _logger.LogInformation(" Calling ItemGroupsController : DeleteItemGroup() ");

                string p_ErrorMsg = string.Empty;

                QITcon = new SqlConnection(_QIT_connection);

                #region Check for Item Group Existance
                _Query = @"  SELECT count(*) FROM QIT_ItemGroup_Master A WHERE 1 = 1 AND A.ItmsGrpCod = @groupCode and ISNULL(A.BranchID, @bId) = @bId ";
                _logger.LogInformation(" ItemGroupsController : DeleteItemGroup() : Check for Item Group Query : {q} ", _Query.ToString());
                cmd = new SqlCommand(_Query, QITcon);
                cmd.Parameters.AddWithValue("@bId", BranchID);
                cmd.Parameters.AddWithValue("@groupCode", ID);
                QITcon.Open();
                Object Value = cmd.ExecuteScalar();
                QITcon.Close();
                if (Int32.Parse(Value.ToString()) == 0)
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Item Group does not exist : " + ID });
                }
                #endregion

                string query = @" SELECT COUNT(*) FROM QIT_ItemSubGroup_Master WHERE ItmsGrpCod = @ICode and ISNULL(BranchID, @bId) = @bId ";
                _logger.LogInformation(" ItemGroupsController : Query : {q} ", query.ToString());

                QITcon.Open();
                cmd = new SqlCommand(query, QITcon);
                cmd.Parameters.AddWithValue("@bId", BranchID);
                cmd.Parameters.AddWithValue("@ICode", ID);
                Value = cmd.ExecuteScalar();
                QITcon.Close();
                if (Int32.Parse(Value.ToString()) == 0)
                {
                    _Query = @" DELETE FROM QIT_ItemGroup_Master WHERE ItmsGrpCod = @code and ISNULL(BranchID, @bId) = @bId ";
                    _logger.LogInformation(" ItemGroupsController : DeleteItemGroup() Query : {q} ", _Query.ToString());

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
                    return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Item Group is in use in Item Sub Group Master" });
            }
            catch (Exception ex)
            {
                _logger.LogError("Error in ItemGroupsController : DeleteItemGroup() :: {Error}", ex.ToString());
                return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = ex.ToString() });
            }
        }

    }
}
