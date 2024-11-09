using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Data.SqlClient;
using WMS_UI_API.Common;
using WMS_UI_API.Models;

namespace WMS_UI_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ItemSubGroupsController : ControllerBase
    {
        private string _ApplicationApiKey = string.Empty;
        private string _connection = string.Empty;
        private string _QIT_connection = string.Empty;
        private string _QIT_DB = string.Empty;
        private string _Query = string.Empty;
        private SqlCommand cmd;

        public IConfiguration Configuration { get; }
        private readonly ILogger<ItemSubGroupsController> _logger;


        public ItemSubGroupsController(IConfiguration configuration, ILogger<ItemSubGroupsController> logger)
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
                _logger.LogError(" Error in ItemSubGroupsController :: {Error}" + ex.ToString());
            }
        }


        [HttpGet("Get")]
        public async Task<ActionResult<IEnumerable<ItemSubGroup>>> GetItemSubGroup(int BranchID, string Filter)
        {
            SqlConnection QITcon;
            SqlDataAdapter oAdptr;
            try
            {
                _logger.LogInformation(" Calling ItemSubGroupsController : GetItemSubGroup() ");

                System.Data.DataTable dtData = new System.Data.DataTable();
                QITcon = new SqlConnection(_QIT_connection);

                string _where = string.Empty;
                if (Filter.ToUpper() == "Y" || Filter.ToUpper() == "N")
                    _where = " AND A.Locked = @locked ";

                _Query = @"  
                SELECT  B.ItmsGrpCod, B.ItmsGrpNam, A.ItmsSubGrpCod, A.ItmsSubGrpNam,  
                        CASE WHEN A.Locked = 'Y' then 'Yes' else 'No' end Locked  
                FROM QIT_ItemSubGroup_Master A  
                     INNER JOIN [dbo].[QIT_ItemGroup_Master] B on A.[ItmsGrpCod] = B.ItmsGrpCod  
                WHERE 1 = 1 AND ISNULL(A.BranchID, @bId) = @bId " + _where + @"
                ORDER BY A.ItmsSubGrpCod ";

                _logger.LogInformation(" ItemSubGroupsController : GetItemSubGroup() Query : {q} ", _Query.ToString());
                QITcon.Open();
                oAdptr = new SqlDataAdapter(_Query, QITcon);
                oAdptr.SelectCommand.Parameters.AddWithValue("@bId", BranchID);
                if (Filter.ToUpper() == "Y" || Filter.ToUpper() == "N")
                    oAdptr.SelectCommand.Parameters.AddWithValue("@locked", Filter);
                oAdptr.Fill(dtData);
                QITcon.Close();

                if (dtData.Rows.Count > 0)
                {
                    List<ItemSubGroup> obj = new List<ItemSubGroup>();
                    dynamic arData = JsonConvert.SerializeObject(dtData);
                    obj = JsonConvert.DeserializeObject<List<ItemSubGroup>>(arData.ToString());
                    return obj;
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "No data found" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(" Error in ItemSubGroupsController : GetItemSubGroup() :: {Error}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
            }
        }


        [HttpPost("Save")]
        public IActionResult SaveItemSubGroup([FromBody] ItemSubGroupSave payload)
        {
            string _IsSaved = "N";
            SqlConnection QITcon;
            try
            {
                _logger.LogInformation(" Calling ItemSubGroupsController : SaveItemSubGroup() ");

                if (payload != null)
                {
                    if (payload.BranchID <= 0)
                        return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Provide Branch" });

                    QITcon = new SqlConnection(_QIT_connection);
                    _Query = @"
                    INSERT INTO QIT_ItemSubGroup_Master(BranchID, ItmsGrpCod, ItmsSubGrpCod, ItmsSubGrpNam, Locked) 
                    VALUES (@bId, @iCode, (select ISNULL(max(ItmsSubGrpCod),0) + 1 from QIT_ItemSubGroup_Master), @name, @locked)";
                    _logger.LogInformation(" ItemSubGroupsController : SaveItemSubGroup() Query : {q} ", _Query.ToString());

                    cmd = new SqlCommand(_Query, QITcon);
                    cmd.Parameters.AddWithValue("@bId", payload.BranchID);
                    cmd.Parameters.AddWithValue("@iCode", payload.ItmsGrpCod);
                    cmd.Parameters.AddWithValue("@name", payload.ItmsSubGrpNam);
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
                    return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = " Details not found" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Error in ItemSubGroupsController : SaveItemSubGroup() :: {Error}", ex.ToString());

                if (ex.Message.ToLower().Contains("uc_itemsubgrpname"))
                {
                    return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Item Sub Group already exist" });
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = ex.Message.ToString() });
                }
            }
        }


        [HttpPut("Update/{Code}")]
        public IActionResult UpdateItemSubGroup([FromBody] ItemSubGroupUpdate payload, int Code)
        {
            string _IsSaved = "N";
            SqlConnection QITcon;
            try
            {
                _logger.LogInformation(" Calling ItemSubGroupsController : UpdateItemSubGroup() ");

                string p_ErrorMsg = string.Empty;

                if (payload != null)
                {
                    if (payload.BranchID <= 0)
                        return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Provide Branch" });

                    QITcon = new SqlConnection(_QIT_connection);
                    _Query = @" 
                    UPDATE QIT_ItemSubGroup_Master 
                    SET ItmsGrpCod=@igCode, ItmsSubGrpNam=@name, Locked=@locked 
                    WHERE ItmsSubGrpCod = @code and ISNULL(BranchID, @bId) = @bId ";
                    _logger.LogInformation(" ItemSubGroupsController : UpdateItemSubGroup() Query : {q} ", _Query.ToString());

                    cmd = new SqlCommand(_Query, QITcon);
                    cmd.Parameters.AddWithValue("@bId", payload.BranchID);
                    cmd.Parameters.AddWithValue("@igCode", payload.ItmsGrpCod);
                    cmd.Parameters.AddWithValue("@name", payload.ItmsSubGrpNam);
                    cmd.Parameters.AddWithValue("@locked", payload.Locked);
                    cmd.Parameters.AddWithValue("@code", Code);

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
                _logger.LogError("Error in ItemSubGroupsController : UpdateItemSubGroup() :: {Error}", ex.ToString());

                if (ex.Message.ToLower().Contains("uc_itemsubgrpname"))
                {
                    return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Item Sub Group Name already exist" });
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = ex.Message.ToString() });
                }
            }
        }


        [HttpDelete("Delete")]
        public IActionResult DeleteItemSubGroup(int BranchID, string ID)
        {
            SqlConnection QITcon;
            string _IsSaved = "N";
            try
            {
                _logger.LogInformation(" Calling ItemSubGroupsController : DeleteItemSubGroup() ");

                if (BranchID <= 0)
                    return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Provide Branch" });


                QITcon = new SqlConnection(_QIT_connection);

                #region Check for Item Group Existance
                _Query = @"  SELECT count(*) FROM QIT_ItemSubGroup_Master A WHERE 1 = 1 AND A.ItmsSubGrpCod = @groupCode and ISNULL(A.BranchID, @bId) = @bId ";
                _logger.LogInformation(" ItemSubGroupsController : DeleteItemSubGroup() : Check for Item Sub Group Query : {q} ", _Query.ToString());
                cmd = new SqlCommand(_Query, QITcon);
                cmd.Parameters.AddWithValue("@bId", BranchID);
                cmd.Parameters.AddWithValue("@groupCode", ID);
                QITcon.Open();
                Object Value = cmd.ExecuteScalar();
                QITcon.Close();
                if (Int32.Parse(Value.ToString()) == 0)
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Item Sub Group does not exist : " + ID });
                }
                #endregion

                _Query = @" DELETE FROM QIT_ItemSubGroup_Master WHERE ItmsSubGrpCod = @code and ISNULL(BranchID, @bId) = @bId";
                _logger.LogInformation(" ItemSubGroupsController : DeleteItemSubGroup() Query : {q} ", _Query.ToString());

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
                _logger.LogError("Error in ItemSubGroupsController : DeleteItemSubGroup() :: {Error}", ex.ToString());
                return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = ex.ToString() });
            }
        }


        [HttpGet("GetItemSubGroupByItemGroup")]
        public async Task<ActionResult<IEnumerable<FillItemSubGroup>>> GetItemSubGroupByItemGroup(int BranchID, int ItemGroupCode)
        {
            SqlConnection QITcon;
            SqlDataAdapter oAdptr;
            try
            {
                _logger.LogInformation(" Calling ItemSubGroupsController : GetItemSubGroupByItemGroup() ");
                System.Data.DataTable dtData = new System.Data.DataTable();
                QITcon = new SqlConnection(_QIT_connection);

                _Query = @"  
                SELECT  A.ItmsSubGrpCod, A.ItmsSubGrpNam  
                FROM QIT_ItemSubGroup_Master A  
                     INNER JOIN [dbo].[QIT_ItemGroup_Master] B on A.[ItmsGrpCod] = B.ItmsGrpCod  
                WHERE 1 = 1 AND B.ItmsGrpCod = @itemgroupCode and A.Locked = 'N' and ISNULL(A.BranchID, @bId) = @bId 
                ORDER BY A.ItmsSubGrpCod ";

                _logger.LogInformation(" ItemSubGroupsController : GetItemSubGroupByItemGroup() Query : {q} ", _Query.ToString());
                QITcon.Open();
                oAdptr = new SqlDataAdapter(_Query, QITcon);
                oAdptr.SelectCommand.Parameters.AddWithValue("@bId", BranchID);
                oAdptr.SelectCommand.Parameters.AddWithValue("@itemgroupCode", ItemGroupCode);
                oAdptr.Fill(dtData);
                QITcon.Close();

                if (dtData.Rows.Count > 0)
                {
                    List<FillItemSubGroup> obj = new List<FillItemSubGroup>();
                    dynamic arData = JsonConvert.SerializeObject(dtData);
                    obj = JsonConvert.DeserializeObject<List<FillItemSubGroup>>(arData.ToString());
                    return obj;
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "No data found" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(" Error in ItemSubGroupsController : GetItemSubGroupByItemGroup() :: {Error}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
            }
        }
    
    }
}
