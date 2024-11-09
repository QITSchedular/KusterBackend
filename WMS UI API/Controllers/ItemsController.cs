using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using NLog.Filters;
using System.Data;
using System.Data.SqlClient;
using WMS_UI_API.Common;
using WMS_UI_API.Models;

namespace WMS_UI_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ItemsController : ControllerBase
    {
        private string _ApplicationApiKey = string.Empty;
        private string _connection = string.Empty;
        private string _QIT_connection = string.Empty;
        private string _QIT_DB = string.Empty;
        private string _Query = string.Empty;
        private SqlCommand cmd;
        public IConfiguration Configuration { get; }
        private readonly ILogger<ItemsController> _logger;

        public ItemsController(IConfiguration configuration, ILogger<ItemsController> logger)
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
                _logger.LogError(" Error in ItemsController :: {Error}" + ex.ToString());
            }
        }


        [HttpGet("Get")]
        public async Task<ActionResult<IEnumerable<ItemS>>> GetItem(int BranchID, string Filter)
        {
            SqlConnection QITcon;
            SqlDataAdapter oAdptr;
            try
            {
                _logger.LogInformation(" Calling ItemsController : GetItem() ");

                System.Data.DataTable dtData = new System.Data.DataTable();
                QITcon = new SqlConnection(_QIT_connection);

                string _where = string.Empty;
                if (Filter.ToUpper() == "Y" || Filter.ToUpper() == "N")
                    _where = " AND A.IsActive = @isActive ";

                _Query = @"  
                SELECT A.ItemCode, A.ItemName, A.ItmsGrpCod, B.ItmsGrpNam, A.ItmsSubGrpCod, C.ItmsSubGrpNam, 
                       D.UomCode, D.UomName, A.QRMngBy, E.QRMngByName QRMngByName, A.ItemMngBy, F.QRMngByName ItemMngByName,  
                       CASE WHEN A.IsActive = 'Y' then 'Yes' else 'No' end IsActive, A.AtcEntry, A.ObjType, A.QRCodeId 
                FROM QIT_Item_Master A  
                     LEFT JOIN QIT_ItemGroup_Master B ON A.ItmsGrpCod = B.ItmsGrpCod and B.Locked = 'N'  
                     LEFT JOIN QIT_ItemSubGroup_Master C ON A.ItmsSubGrpCod = C.ItmsSubGrpCod and B.ItmsGrpCod = C.ItmsGrpCod and C.Locked = 'N'  
                     LEFT JOIN QIT_UoM_Master D ON A.UomCode = D.UomCode and D.Locked = 'N' 
                     LEFT JOIN QIT_QRMngBy_Master E on A.QRMngBy = E.QRMngById  
                     LEFT JOIN QIT_QRMngBy_Master F on A.QRMngBy = F.QRMngById  
                WHERE 1 = 1 AND ISNULL(A.BranchID, @bId) = @bId " + _where + @"
                ORDER BY A.ItemCode ";

                _logger.LogInformation(" ItemsController : GetItem() Query : {q} ", _Query.ToString());
                QITcon.Open();
                oAdptr = new SqlDataAdapter(_Query, QITcon);
                oAdptr.SelectCommand.Parameters.AddWithValue("@bId", BranchID);
                if (Filter.ToUpper() == "Y" || Filter.ToUpper() == "N")
                    oAdptr.SelectCommand.Parameters.AddWithValue("@isActive", Filter);
                oAdptr.Fill(dtData);
                QITcon.Close();

                if (dtData.Rows.Count > 0)
                {
                    List<ItemS> obj = new List<ItemS>();
                    dynamic arData = JsonConvert.SerializeObject(dtData);
                    obj = JsonConvert.DeserializeObject<List<ItemS>>(arData.ToString());
                    return obj;
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "No data found" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(" Error in ItemsController : GetItem() :: {Error}", ex.ToString());
                return BadRequest(new { StatusCode = "400", IsSaved = "N", StatusMsg = ex.Message.ToString() });
            }
        }


        [HttpPost("Save")]
        public IActionResult SaveItem([FromBody] ItemInsert payload)
        {
            string _IsSaved = "N";
            SqlConnection QITcon;
            try
            {
                _logger.LogInformation(" Calling ItemsController : SaveItem() ");

                if (payload != null)
                {
                    QITcon = new SqlConnection(_QIT_connection);

                    #region Check Branch
                    if (payload.BranchID <= 0)
                    {
                        return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Provide Branch" });
                    }
                    #endregion

                    #region Check for Item Code
                    if (payload.ItemCode.ToString().Trim() == "")
                    {
                        return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Provide Item Code" });
                    }
                    if (payload.ItemCode.ToString().ToLower() == "string")
                    {
                        return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Provide Item Code" });
                    }

                    _Query = @" SELECT count(*) from QIT_Item_Master WHERE ItemCode = @itemCode AND ISNULL(BranchID, @bId) = @bId ";
                    _logger.LogInformation(" ItemsController : ItemCode Query : {q} ", _Query.ToString());

                    cmd = new SqlCommand(_Query, QITcon);
                    cmd.Parameters.AddWithValue("@itemCode", payload.ItemCode);
                    cmd.Parameters.AddWithValue("@bId", payload.BranchID);
                    QITcon.Open();
                    Object Value = cmd.ExecuteScalar();
                    QITcon.Close();
                    if (Int32.Parse(Value.ToString()) == 1)
                    {
                        return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Item already exist" });
                    }
                    #endregion

                    #region Check for Item Name
                    if (payload.ItemName.ToString().Trim() == "")
                    {
                        return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Provide Item Name" });
                    }
                    if (payload.ItemName.ToString().ToLower() == "string")
                    {
                        return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Provide Item Name" });
                    }
                    #endregion

                    #region Check for Item Group Code
                    if (payload.ItmsGrpCod.ToString().Trim() != "-1" && payload.ItmsGrpCod.ToString().Trim() != "0")
                    {
                        _Query = @" SELECT count(*) from QIT_ItemGroup_Master WHERE ItmsGrpCod = @igCode and Locked = 'N' ";
                        _logger.LogInformation(" ItemsController : Check Item Group Query : {q} ", _Query.ToString());

                        cmd = new SqlCommand(_Query, QITcon);
                        cmd.Parameters.AddWithValue("@igCode", payload.ItmsGrpCod);
                        QITcon.Open();
                        Value = cmd.ExecuteScalar();
                        QITcon.Close();
                        if (Int32.Parse(Value.ToString()) == 0)
                        {
                            return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Item Group does not exist" });
                        }
                    }
                    #endregion

                    #region Check for Item Sub Group Code
                    if (payload.ItmsSubGrpCod.ToString().Trim() != "-1" && payload.ItmsSubGrpCod.ToString().Trim() != "0")
                    {
                        _Query = @" SELECT count(*) from QIT_ItemSubGroup_Master WHERE ItmsSubGrpCod = @isgCode and Locked = 'N' ";
                        _logger.LogInformation(" ItemsController : Check Item Sub Group Query : {q} ", _Query.ToString());

                        cmd = new SqlCommand(_Query, QITcon);
                        cmd.Parameters.AddWithValue("@isgCode", payload.ItmsSubGrpCod);
                        QITcon.Open();
                        Value = cmd.ExecuteScalar();
                        QITcon.Close();
                        if (Int32.Parse(Value.ToString()) == 0)
                        {
                            return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Item Sub Group does not exist" });
                        }
                    }
                    #endregion

                    #region Check for Uom
                    _Query = @" SELECT count(*) from QIT_UoM_Master WHERE UomCode = @uomCode and Locked = 'N' ";
                    _logger.LogInformation(" ItemsController : Check UoM Query : {q} ", _Query.ToString());

                    cmd = new SqlCommand(_Query, QITcon);
                    cmd.Parameters.AddWithValue("@uomCode", payload.UomCode);
                    QITcon.Open();
                    Value = cmd.ExecuteScalar();
                    QITcon.Close();
                    if (Int32.Parse(Value.ToString()) == 0)
                    {
                        return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "UoM does not exist" });
                    }
                    #endregion

                    #region Check for Is Active 

                    if (payload.IsActive.ToString().ToUpper() != "Y" && payload.IsActive.ToString().ToUpper() != "N")
                        return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "IsActive Values : Y / N " });

                    #endregion

                    #region Check Item Manage By

                    if (payload.ItemMngBy.ToString().ToUpper() != "N" && payload.ItemMngBy.ToString().ToUpper() != "B" && payload.ItemMngBy.ToString().ToUpper() != "S")
                        return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "ItemMngBy Values : N:None / B:Batch / S:Serial Numbers " });

                    #endregion

                    #region Check QR Manage By

                    if (payload.QRMngBy.ToString().ToUpper() != "N" && payload.QRMngBy.ToString().ToUpper() != "B" && payload.QRMngBy.ToString().ToUpper() != "S")
                        return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "QRMngBy Values : N:None / B:Batch / S:Serial Numbers " });

                    #endregion

                    #region Get QR Gen Type
                    string _QRGenType = "T";
                    _Query = "SELECT QRGenMethod FROM QIT_Config_Master";
                    QITcon.Open();
                    cmd = new SqlCommand(_Query, QITcon);
                    _QRGenType = (string)cmd.ExecuteScalar();
                    QITcon.Close();
                    #endregion

                    if (_QRGenType.ToUpper() == "M")
                    {
                        _Query = @"
                        INSERT INTO QIT_Item_Master
                        (BranchID, ItemCode, ItemName, ItmsGrpCod, ItmsSubGrpCod, UomCode, QRMngBy, ItemMngBy, IsActive, QRCodeId, AtcEntry) 
                        VALUES (@bId, @itemCode, @itemName, @igCode, @isgCode, @uomCode, @qrMngBy, @itemMngBy, @isActive, @qrCodeID, @atcEntry)";
                        _logger.LogInformation(" ItemsController : Insert Query : {q} ", _Query.ToString());

                        cmd = new SqlCommand(_Query, QITcon);
                        cmd.Parameters.AddWithValue("@bId", payload.BranchID);
                        cmd.Parameters.AddWithValue("@itemCode", payload.ItemCode);
                        cmd.Parameters.AddWithValue("@itemName", payload.ItemName);
                        cmd.Parameters.AddWithValue("@igCode", payload.ItmsGrpCod);
                        cmd.Parameters.AddWithValue("@isgCode", payload.ItmsSubGrpCod);
                        cmd.Parameters.AddWithValue("@uomCode", payload.UomCode);
                        cmd.Parameters.AddWithValue("@qrMngBy", payload.QRMngBy.ToUpper());
                        cmd.Parameters.AddWithValue("@itemMngBy", payload.ItemMngBy.ToUpper());
                        cmd.Parameters.AddWithValue("@isActive", payload.IsActive);
                        cmd.Parameters.AddWithValue("@qrCodeID", Guid.NewGuid());
                        cmd.Parameters.AddWithValue("@atcEntry", payload.AtcEntry);
                    }
                    else
                    {
                        _Query = @"
                        INSERT INTO QIT_Item_Master
                        (BranchID, ItemCode, ItemName, ItmsGrpCod, ItmsSubGrpCod, UomCode, QRMngBy, ItemMngBy, IsActive, AtcEntry) 
                        VALUES (@bId, @itemCode, @itemName, @igCode, @isgCode, @uomCode, @qrMngBy, @itemMngBy, @isActive, @atcEntry)";
                        _logger.LogInformation(" ItemsController : Insert Query : {q} ", _Query.ToString());

                        cmd = new SqlCommand(_Query, QITcon);
                        cmd.Parameters.AddWithValue("@bId", payload.BranchID);
                        cmd.Parameters.AddWithValue("@itemCode", payload.ItemCode);
                        cmd.Parameters.AddWithValue("@itemName", payload.ItemName);
                        cmd.Parameters.AddWithValue("@igCode", payload.ItmsGrpCod);
                        cmd.Parameters.AddWithValue("@isgCode", payload.ItmsSubGrpCod);
                        cmd.Parameters.AddWithValue("@uomCode", payload.UomCode);
                        cmd.Parameters.AddWithValue("@qrMngBy", payload.QRMngBy.ToUpper());
                        cmd.Parameters.AddWithValue("@itemMngBy", payload.ItemMngBy.ToUpper());
                        cmd.Parameters.AddWithValue("@isActive", payload.IsActive);
                        cmd.Parameters.AddWithValue("@atcEntry", payload.AtcEntry);
                    }
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
                _logger.LogError("Error in ItemsController : SaveItem() :: {Error}", ex.ToString());

                if (ex.Message.ToLower().Contains("uc_itemcode"))
                {
                    return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Item already exist" });
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = ex.Message.ToString() });
                }
            }
        }


        [HttpPut("Update/{ItemCode}")]
        public IActionResult UpdateItem([FromBody] ItemUpdate payload, string ItemCode)
        {
            string _IsSaved = "N";

            SqlConnection QITcon;

            try
            {
                _logger.LogInformation(" Calling ItemsController : UpdateItem() ");

                if (payload != null)
                {
                    QITcon = new SqlConnection(_QIT_connection);
                    
                    if (payload.BranchID <=0)
                        return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Provide Branch" });

                    #region Check for Item Code
                    if (ItemCode.ToString().Trim() == "" || ItemCode.ToString().ToLower() == "string")
                    {
                        return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Provide Item Code" });
                    }

                    _Query = @" SELECT count(*) from QIT_Item_Master WHERE ItemCode = @itemCode and ISNULL(BranchID, @bId) = @bId  ";
                    _logger.LogInformation(" ItemsController : UpdateItem() : Check ItemCode Query : {q} ", _Query.ToString());

                    cmd = new SqlCommand(_Query, QITcon);
                    cmd.Parameters.AddWithValue("@bId", payload.BranchID);
                    cmd.Parameters.AddWithValue("@itemCode", ItemCode);
                    QITcon.Open();
                    Object Value = cmd.ExecuteScalar();
                    QITcon.Close();
                    if (Int32.Parse(Value.ToString()) == 0)
                    {
                        return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Item does not exist" });
                    }
                    #endregion

                    #region Check for Item Name
                    if (payload.ItemName.ToString().Trim() == "")
                    {
                        return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Provide Item Name" });
                    }
                    if (payload.ItemName.ToString().ToLower() == "string")
                    {
                        return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Provide Item Name" });
                    }
                    #endregion

                    #region Check for Item Group Code
                    if (payload.ItmsGrpCod.ToString().Trim() != "0")
                    {
                        _Query = @" SELECT count(*) from QIT_ItemGroup_Master WHERE ItmsGrpCod = @igCode and Locked = 'N' ";
                        _logger.LogInformation(" ItemsController : UpdateItem() : Check Item Group Query : {q} ", _Query.ToString());

                        cmd = new SqlCommand(_Query, QITcon);
                        cmd.Parameters.AddWithValue("@igCode", payload.ItmsGrpCod);
                        QITcon.Open();
                        Value = cmd.ExecuteScalar();
                        QITcon.Close();
                        if (Int32.Parse(Value.ToString()) == 0)
                        {
                            return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Item Group does not exist" });
                        }
                    }
                    #endregion

                    #region Check for Item Sub Group Code
                    if (payload.ItmsSubGrpCod.ToString().Trim() != "0")
                    {
                        _Query = @" SELECT count(*) from QIT_ItemSubGroup_Master WHERE ItmsSubGrpCod = @isgCode and Locked = 'N' ";
                        _logger.LogInformation(" ItemsController : UpdateItem() : Check Item Sub Group Query : {q} ", _Query.ToString());

                        cmd = new SqlCommand(_Query, QITcon);
                        cmd.Parameters.AddWithValue("@isgCode", payload.ItmsSubGrpCod);
                        QITcon.Open();
                        Value = cmd.ExecuteScalar();
                        QITcon.Close();
                        if (Int32.Parse(Value.ToString()) == 0)
                        {
                            return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Item Sub Group does not exist" });
                        }
                    }
                    #endregion

                    #region Check for Uom
                    _Query = @" SELECT count(*) from QIT_UoM_Master WHERE UomCode = @uomCode and Locked = 'N' ";
                    _logger.LogInformation(" ItemsController : UpdateItem() : Check UoM Query : {q} ", _Query.ToString());

                    cmd = new SqlCommand(_Query, QITcon);
                    cmd.Parameters.AddWithValue("@uomCode", payload.UomCode);
                    QITcon.Open();
                    Value = cmd.ExecuteScalar();
                    QITcon.Close();
                    if (Int32.Parse(Value.ToString()) == 0)
                    {
                        return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "UoM does not exist" });
                    }
                    #endregion

                    #region Check for Is Active 

                    if (payload.IsActive.ToString().ToUpper() != "Y" && payload.IsActive.ToString().ToUpper() != "N")
                        return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "IsActive Values : Y / N " });

                    #endregion

                    #region Check Item Manage By

                    if (payload.ItemMngBy.ToString().ToUpper() != "N" &&
                        payload.ItemMngBy.ToString().ToUpper() != "B" &&
                        payload.ItemMngBy.ToString().ToUpper() != "S")
                        return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "ItemMngBy Values : N:None / B:Batch / S:Serial Numbers " });

                    #endregion

                    #region Check QR Manage By

                    if (payload.QRMngBy.ToString().ToUpper() != "N" &&
                        payload.QRMngBy.ToString().ToUpper() != "B" &&
                        payload.QRMngBy.ToString().ToUpper() != "S")
                        return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "QRMngBy Values : N:None / B:Batch / S:Serial Numbers " });

                    #endregion

                    _Query = @" 
                    UPDATE QIT_Item_Master 
                    SET ItemName = @iname, ItmsGrpCod = @igCode, ItmsSubGrpCod = @isgCode, UoMCode = @uomCode,
                        QRMngBy = @qrMngBy, ItemMngBy = @itemMngBy, IsActive = @isActive, AtcEntry = @atc
                    WHERE ItemCode = @itemCode AND (BranchID, @bId) = @bId";
                    _logger.LogInformation(" ItemsController : UpdateItem() Query : {q} ", _Query.ToString());

                    cmd = new SqlCommand(_Query, QITcon);
                    cmd.Parameters.AddWithValue("@bId", payload.BranchID);
                    cmd.Parameters.AddWithValue("@iname", payload.ItemName);
                    cmd.Parameters.AddWithValue("@igCode", payload.ItmsGrpCod);
                    cmd.Parameters.AddWithValue("@isgCode", payload.ItmsSubGrpCod);
                    cmd.Parameters.AddWithValue("@uomCode", payload.UomCode);
                    cmd.Parameters.AddWithValue("@qrMngBy", payload.QRMngBy.ToUpper());
                    cmd.Parameters.AddWithValue("@itemMngBy", payload.ItemMngBy.ToUpper());
                    cmd.Parameters.AddWithValue("@isActive", payload.IsActive);
                    cmd.Parameters.AddWithValue("@atc", payload.AtcEntry);
                    cmd.Parameters.AddWithValue("@itemCode", ItemCode);

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
                _logger.LogError("Error in ItemsController : UpdateItem() :: {Error}", ex.ToString());
                return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = ex.Message.ToString() });
            }
        }


        [HttpDelete("Delete")]
        public IActionResult DeleteItem(int BranchID, string ID)
        {
            SqlConnection QITcon;
            string _IsSaved = "N";
            try
            {
                _logger.LogInformation(" Calling ItemsController : DeleteItem() ");

                QITcon = new SqlConnection(_QIT_connection);
                string query = @" SELECT COUNT(*) FROM QIT_Item_Master WHERE ItemCode = @ICode and ISNULL(BranchID, @bId) = @bId ";
                _logger.LogInformation(" ItemsController : Item Code Query : {q} ", query.ToString());

                QITcon.Open();
                cmd = new SqlCommand(query, QITcon);
                cmd.Parameters.AddWithValue("@bId", BranchID);
                cmd.Parameters.AddWithValue("@ICode", ID);
                Object Value = cmd.ExecuteScalar();
                QITcon.Close();
                if (Int32.Parse(Value.ToString()) > 0)
                {
                    _Query = @" DELETE FROM QIT_Item_Master WHERE ItemCode = @code and ISNULL(BranchID, @bId) = @bId";
                    _logger.LogInformation(" ItemsController : DeleteItemGroup() Query : {q} ", _Query.ToString());
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
                    return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Item Code does not exist : " + ID });
            }
            catch (Exception ex)
            {
                _logger.LogError("Error in ItemsController : DeleteItemGroup() :: {Error}", ex.ToString());
                return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = ex.ToString() });
            }
        }


        [HttpGet("GetItemByItemCode")]
        public async Task<ActionResult<IEnumerable<ItemS>>> GetItemByItemCode(int BranchID, string ItemCode)
        {
            SqlConnection QITcon;
            SqlDataAdapter oAdptr;
            try
            {
                _logger.LogInformation(" Calling ItemsController : GetItemByItemCode() ");

                System.Data.DataTable dtData = new System.Data.DataTable();
                QITcon = new SqlConnection(_QIT_connection);

                string _where = string.Empty;
                if (ItemCode.Trim() == String.Empty || ItemCode.Trim().ToLower() == "string")
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Provide Item Code" });

                #region Check for Item Existance
                _Query = @"  SELECT count(*) FROM QIT_Item_Master A WHERE 1 = 1 AND A.ItemCode = @itemCode and ISNULL(A.BranchID, @bId) = @bId ";
                _logger.LogInformation(" ItemsController : GetItemByItemCode() Query : {q} ", _Query.ToString());
                cmd = new SqlCommand(_Query, QITcon);
                cmd.Parameters.AddWithValue("@bId", BranchID);
                cmd.Parameters.AddWithValue("@itemCode", ItemCode);
                QITcon.Open();
                Object Value = cmd.ExecuteScalar();
                QITcon.Close();
                if (Int32.Parse(Value.ToString()) == 0)
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Item Code does not exist : " + ItemCode });
                }
                #endregion

                _Query = @"  
                SELECT A.ItemCode, A.ItemName, A.ItmsGrpCod, B.ItmsGrpNam, A.ItmsSubGrpCod, C.ItmsSubGrpNam,    
                       A.UomCode, D.UomCode, D.UomName,  
                       A.QRMngBy, E.QRMngByName QRMngByName, A.ItemMngBy, F.QRMngByName ItemMngByName,  
                       case when A.IsActive = 'Y' then 'Yes' else 'No' end IsActive,  
                       A.AtcEntry, A.ObjType, A.QRCodeId  
                FROM QIT_Item_Master A 
                LEFT JOIN QIT_ItemGroup_Master B ON A.ItmsGrpCod = B.ItmsGrpCod  
                LEFT JOIN QIT_ItemSubGroup_Master C ON A.ItmsSubGrpCod = C.ItmsSubGrpCod and B.ItmsGrpCod = C.ItmsGrpCod  
                LEFT JOIN QIT_UoM_Master D ON A.UomCode = D.UomCode
                LEFT JOIN QIT_QRMngBy_Master E on A.QRMngBy = E.QRMngById 
                LEFT JOIN QIT_QRMngBy_Master F on A.QRMngBy = F.QRMngById  
                WHERE 1 = 1 AND A.ItemCode = @itemCode and ISNULL(A.BranchID, @bId) = @bId ";

                _logger.LogInformation(" ItemsController : GetItemByItemCode() Query : {q} ", _Query.ToString());
                QITcon.Open();
                oAdptr = new SqlDataAdapter(_Query, QITcon);
                oAdptr.SelectCommand.Parameters.AddWithValue("@bId", BranchID);
                oAdptr.SelectCommand.Parameters.AddWithValue("@itemCode", ItemCode);
                oAdptr.Fill(dtData);
                QITcon.Close();

                if (dtData.Rows.Count > 0)
                {
                    List<ItemS> obj = new List<ItemS>();
                    dynamic arData = JsonConvert.SerializeObject(dtData);
                    obj = JsonConvert.DeserializeObject<List<ItemS>>(arData.ToString());
                    return obj;
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "No data found" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(" Error in ItemsController : GetItemByItemCode() :: {Error}", ex.ToString());
                return BadRequest(new { StatusCode = "400", IsSaved = "N", StatusMsg = ex.Message.ToString() });
            }
        }
    }
}
