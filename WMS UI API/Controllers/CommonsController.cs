using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using NLog.Filters;
using SAPbobsCOM;
using System.Data;
using System.Data.SqlClient;
using System.Xml.Linq;
using WMS_UI_API.Common;
using WMS_UI_API.Models;
using BinLocation = WMS_UI_API.Models.BinLocation;
using Branch = WMS_UI_API.Models.Branch;
using Project = WMS_UI_API.Models.Project;

namespace WMS_UI_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CommonsController : ControllerBase
    {
        private string _ApplicationApiKey = string.Empty;
        private string _connection = string.Empty;
        private string _QIT_connection = string.Empty;
        private string _QIT_DB = string.Empty;
        private string _Query = string.Empty;

        private SqlCommand cmd;
        SqlConnection QITcon;
        SqlConnection SAPcon;
        SqlDataAdapter oAdptr;


        bool _isExist = false;
        public IConfiguration Configuration { get; }
        private readonly ILogger<CommonsController> _logger;


        public CommonsController(IConfiguration configuration, ILogger<CommonsController> logger)
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
                _logger.LogError(" Error in CommonsController :: {Error}" + ex.ToString());
            }
        }


        [HttpGet("Branch")]
        public async Task<ActionResult<IEnumerable<Branch>>> GetBranch()
        {
            try
            {
                _logger.LogInformation(" Calling CommonsController : GetBranch() ");

                DataTable dtBranch = new DataTable();
                SAPcon = new SqlConnection(_connection);

                _Query = @" select BPLId, BPLName from OBPL where Disabled = 'N' ";
                _logger.LogInformation(" CommonsController : GetBranch() Query : {q} ", _Query.ToString());
                SAPcon.Open();
                oAdptr = new SqlDataAdapter(_Query, SAPcon);
                oAdptr.Fill(dtBranch);
                SAPcon.Close();

                List<Branch> obj = new List<Branch>();
                dynamic arData = JsonConvert.SerializeObject(dtBranch);
                obj = JsonConvert.DeserializeObject<List<Branch>>(arData.ToString());
                return obj;
            }
            catch (Exception ex)
            {
                _logger.LogError(" Error in CommonsController : GetBranch() :: {Error}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
            }
        }


        [HttpGet("Period Indicator")]
        public async Task<ActionResult<IEnumerable<PeriodIndicator>>> GetPeriodIndicator()
        {
            try
            {
                _logger.LogInformation(" Calling CommonsController : GetPeriodIndicator() ");
                DataTable dtPeriod = new DataTable();
                SAPcon = new SqlConnection(_connection);

                _Query = @" select Indicator from OPID ";
                _logger.LogInformation(" CommonsController : GetPeriodIndicator() Query : {q} ", _Query.ToString());
                SAPcon.Open();
                oAdptr = new SqlDataAdapter(_Query, SAPcon);
                oAdptr.Fill(dtPeriod);
                SAPcon.Close();

                if (dtPeriod.Rows.Count > 0)
                {
                    List<PeriodIndicator> obj = new List<PeriodIndicator>();
                    dynamic arData = JsonConvert.SerializeObject(dtPeriod);
                    obj = JsonConvert.DeserializeObject<List<PeriodIndicator>>(arData.ToString());
                    return obj;
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Define period indicator in SAP" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(" Error in CommonsController : GetPeriodIndicator() :: {Error}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
            }
        }


        [HttpGet("Series")]
        public async Task<ActionResult<IEnumerable<SeriesCls>>> GetSeries(string? Indicator, string ObjType, int? BranchID)
        {
            try
            {
                _logger.LogInformation(" Calling CommonsController : GetSeries() ");

                DataTable dtSeries = new DataTable();
                SAPcon = new SqlConnection(_connection);

                if (Indicator == null)
                    _Query = @" select Series, SeriesName from NNM1 WHERE Locked = 'N' and ObjectCode = @objType and ISNULL(BPLId, @bplID) = @bplID";
                else
                    _Query = @" select Series, SeriesName from NNM1 WHERE Indicator = ISNULL(@indi,Indicator) and Locked = 'N' and ObjectCode = @objType and ISNULL(BPLId, @bplID) = @bplID ";

                _logger.LogInformation(" CommonsController : GetSeries() Query : {q} ", _Query.ToString());
                SAPcon.Open();
                oAdptr = new SqlDataAdapter(_Query, SAPcon);
                if (Indicator != null)
                    oAdptr.SelectCommand.Parameters.AddWithValue("@indi", Indicator);
                oAdptr.SelectCommand.Parameters.AddWithValue("@objType", ObjType);
                oAdptr.SelectCommand.Parameters.AddWithValue("@bplID", BranchID);
                oAdptr.Fill(dtSeries);
                SAPcon.Close();

                if (dtSeries.Rows.Count > 0)
                {
                    List<SeriesCls> obj = new List<SeriesCls>();
                    dynamic arData = JsonConvert.SerializeObject(dtSeries);
                    obj = JsonConvert.DeserializeObject<List<SeriesCls>>(arData.ToString());
                    return obj;
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Define Series in SAP" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(" Error in CommonsController : GetSeries() :: {Error}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
            }
        }


        [HttpGet("QRManagedBy")]
        public async Task<ActionResult<IEnumerable<QRMngBy>>> GetQRMngBy()
        {
            try
            {
                _logger.LogInformation(" Calling CommonsController : GetQRMngBy() ");
                List<QRMngBy> obj = new List<QRMngBy>();
                DataTable dtData = new DataTable();
                QITcon = new SqlConnection(_QIT_connection);

                _Query = @" select QRMngById, QRMngByName from QIT_QRMngBy_Master order by SrNo ";
                _logger.LogInformation(" CommonsController : GetQRMngBy() Query : {q} ", _Query.ToString());
                QITcon.Open();
                oAdptr = new SqlDataAdapter(_Query, QITcon);
                oAdptr.Fill(dtData);
                QITcon.Close();

                dynamic arData = JsonConvert.SerializeObject(dtData);
                obj = JsonConvert.DeserializeObject<List<QRMngBy>>(arData.ToString());
                return obj;
            }
            catch (Exception ex)
            {
                _logger.LogError(" Error in CommonsController : GetQRMngBy() :: {Error}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
            }
        }


        [HttpGet("Project")]
        public async Task<ActionResult<IEnumerable<Project>>> GetProject()
        {
            try
            {
                _logger.LogInformation(" Calling CommonsController : GetProject() ");
                DataTable dtProject = new DataTable();
                SAPcon = new SqlConnection(_connection);

                _Query = @" select PrjCode, PrjName from OPRJ where Locked = 'N' and Active = 'Y' ";
                _logger.LogInformation(" CommonsController : GetProject() Query : {q} ", _Query.ToString());
                SAPcon.Open();
                oAdptr = new SqlDataAdapter(_Query, SAPcon);
                oAdptr.Fill(dtProject);
                SAPcon.Close();

                if (dtProject.Rows.Count > 0)
                {
                    List<Project> obj = new List<Project>();
                    dynamic arData = JsonConvert.SerializeObject(dtProject);
                    obj = JsonConvert.DeserializeObject<List<Project>>(arData.ToString());
                    return obj;
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Define Project in SAP" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(" Error in CommonsController : GetProject() :: {Error}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
            }
        }


        [HttpGet("Warehouse")]
        public async Task<ActionResult<IEnumerable<QITWarehouse>>> GetWarehouse()
        {
            try
            {
                _logger.LogInformation(" Calling CommonsController : GetWarehouse() ");

                DataTable dtWarehouse = new DataTable();
                QITcon = new SqlConnection(_QIT_connection);

                _Query = @" select WhsCode, WhsName from [dbo].[QIT_Warehouse_Master]  where Locked = 'N'  ";
                _logger.LogInformation(" CommonsController : GetWarehouse() Query : {q} ", _Query.ToString());
                QITcon.Open();
                oAdptr = new SqlDataAdapter(_Query, QITcon);
                oAdptr.Fill(dtWarehouse);
                QITcon.Close();

                if (dtWarehouse.Rows.Count > 0)
                {
                    List<QITWarehouse> obj = new List<QITWarehouse>();
                    dynamic arData = JsonConvert.SerializeObject(dtWarehouse);
                    obj = JsonConvert.DeserializeObject<List<QITWarehouse>>(arData.ToString());
                    return obj;
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Define Warehouse in SAP" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(" Error in CommonsController : GetWarehouse() :: {Error}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
            }
        }


        [HttpGet("IsItemGroupExist")]
        public ActionResult<bool> ItemGroupExist(string _ItemGroupName)
        {
            _isExist = false;
            try
            {
                _logger.LogInformation(" Calling CommonsController : ItemGroupExist() ");

                QITcon = new SqlConnection(_QIT_connection);

                _Query = @" SELECT count(*) from QIT_ItemGroup_Master WHERE ItmsGrpNam = @name  ";
                _logger.LogInformation(" CommonsController : ItemGroupExist() Query : {q} ", _Query.ToString());

                cmd = new SqlCommand(_Query, QITcon);
                cmd.Parameters.AddWithValue("@name", _ItemGroupName);
                QITcon.Open();
                Object Value = cmd.ExecuteScalar();
                if (Int32.Parse(Value.ToString()) == 1)
                {
                    _isExist = true;
                }
                else
                {
                    _isExist = false;
                }
                QITcon.Close();
                return _isExist;
            }
            catch (Exception ex)
            {
                _logger.LogError(" Error in CommonsController : ItemGroupExist() :: {Error}", ex.ToString());
                return _isExist;
            }
        }


        [HttpPut("UpdateBatchQty")]
        public IActionResult UpdateBatchQty([FromBody] UpdateBatchQty payload)
        {
            string _IsSaved = "N";
            try
            {
                _logger.LogInformation(" Calling CommonsController : UpdateBatchQty() ");

                if (payload != null)
                {
                    QITcon = new SqlConnection(_QIT_connection);
                    _Query = @" UPDATE A
                                SET A.Qty = @qty	
                                FROM QIT_QR_Detail A INNER JOIN QIT_QR_Header B ON A.HeaderSrNo = B.HeaderSrNo
                                WHERE A.ItemCode = @itemCode AND A.GateInNo = @gateInNo AND A.BranchID = @bId AND 
                                      B.DocEntry = @docEntry AND B.DocNum = @docNum AND B.ObjType = @objType AND 
                                      A.QRCodeID = @detQRCode ";
                    _logger.LogInformation(" CommonsController : UpdateBatchQty() Query : {q} ", _Query.ToString());

                    cmd = new SqlCommand(_Query, QITcon);
                    cmd.Parameters.AddWithValue("@qty", payload.Qty);
                    cmd.Parameters.AddWithValue("@itemCode", payload.ItemCode);
                    cmd.Parameters.AddWithValue("@gateInNo", payload.GateInNo);
                    cmd.Parameters.AddWithValue("@bId", payload.BranchID);
                    cmd.Parameters.AddWithValue("@docEntry", payload.DocEntry);
                    cmd.Parameters.AddWithValue("@docNum", payload.DocNum);
                    cmd.Parameters.AddWithValue("@objType", payload.ObjType);
                    cmd.Parameters.AddWithValue("@detQRCode", payload.DetailQRCodeID);

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
                _logger.LogError("Error in CommonsController : UpdateBatchQty() :: {Error}", ex.ToString());
                return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = ex.Message.ToString() });
            }
        }


        [HttpPost("GetGateINNo")]
        public ActionResult<string> GetGateINNo(string BranchID)
        {
            _isExist = false;
            try
            {
                _logger.LogInformation(" Calling CommonsController : GetGateINNo() ");
                QITcon = new SqlConnection(_QIT_connection);
                DataTable dtData = new DataTable();

                _Query = @" select ISNULL(max(GateInNo),0) + 1 from QIT_GateIN where ISNULL(BranchID,1) = @bID  ";
                _logger.LogInformation(" CommonsController : GetGateINNo() Query : {q} ", _Query.ToString());

                cmd = new SqlCommand(_Query, QITcon);
                cmd.Parameters.AddWithValue("@bID", BranchID);
                QITcon.Open();
                Object Value = cmd.ExecuteScalar();
                QITcon.Close();
                return Value.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(" Error in CommonsController : GetGateINNo() :: {Error}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
            }
        }


        [HttpGet("FillGateInNo")]
        public async Task<ActionResult<IEnumerable<FillGateInNo>>> FillGateInNo(int Series, int DocNum)
        {
            try
            {
                _logger.LogInformation(" Calling CommonsController : FillGateInNo() ");

                DataTable dtGateInNo = new DataTable();
                QITcon = new SqlConnection(_QIT_connection);

                _Query = @" SELECT DISTINCT GateInNo FROM QIT_GateIN A
                            WHERE Series = @series and DocNum = @docNum and Canceled = 'N' ";
                _logger.LogInformation(" CommonsController : FillGateInNo() Query : {q} ", _Query.ToString());
                QITcon.Open();
                oAdptr = new SqlDataAdapter(_Query, QITcon);
                oAdptr.SelectCommand.Parameters.AddWithValue("@series", Series);
                oAdptr.SelectCommand.Parameters.AddWithValue("@docNum", DocNum);
                oAdptr.Fill(dtGateInNo);
                QITcon.Close();

                if (dtGateInNo.Rows.Count > 0)
                {
                    List<FillGateInNo> obj = new List<FillGateInNo>();
                    dynamic arData = JsonConvert.SerializeObject(dtGateInNo);
                    obj = JsonConvert.DeserializeObject<List<FillGateInNo>>(arData.ToString());
                    return obj;
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "No Data Found" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(" Error in CommonsController : FillGateInNo() :: {Error}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
            }
        }


        [HttpGet("ItemStock")]
        public async Task<ActionResult<IEnumerable<ItemStock>>> ItemStock(string? ItemCode)
        {
            try
            {
                _logger.LogInformation(" Calling CommonsController : ItemStock() ");

                DataTable dtStock = new DataTable();
                QITcon = new SqlConnection(_QIT_connection);

                if (ItemCode == null)
                    ItemCode = string.Empty;
                _Query = @" 
                SELECT A.WhsCode, A.WhsName, 
	                   Case when  A.POtoGRPO > 0 then 
                            (POtoGRPO + InQty - OutQty - IssueQty - DeliverQty + ReceiptQty) 
                       else 
                            (InQty - OutQty - IssueQty - DeliverQty + ReceiptQty) 
                       end Stock
                FROM 
                (
                    SELECT A.WhsCode, A.WhsName,
                    (
                        ISNULL((	
		                    SELECT sum(Z.Qty) POtoGRPOQty FROM " + Global.QIT_DB + @".dbo.[QIT_QRStock_InvTrans] Z
		                    WHERE Z.ItemCode = @itemCode and Z.FromObjType = '22' and Z.ToObjType = '67' and Z.ToWhs = A.WhsCode
	                    ),0)
                    ) POtoGRPO,
                    (ISNULL((
		                    SELECT sum(Z.Qty) InQty FROM " + Global.QIT_DB + @".dbo.[QIT_QRStock_InvTrans] Z
		                    WHERE Z.ItemCode = @itemCode and (Z.FromObjType <> '22') AND 
                                  Z.ToWhs collate SQL_Latin1_General_CP850_CI_AS = A.WhsCode
	                    ),0) 
                    ) InQty,
                    (
                        ISNULL((
		                    SELECT sum(Z.Qty) OutQty FROM " + Global.QIT_DB + @".dbo.[QIT_QRStock_InvTrans] Z
		                    WHERE Z.ItemCode = @itemCode and (Z.FromObjType <> '22') and 
                                  Z.FromWhs collate SQL_Latin1_General_CP850_CI_AS = A.WhsCode
	                    ),0)
                    ) OutQty,
                    (
                        ISNULL((
		                    SELECT sum(Z.Qty) IssueQty FROM " + Global.QIT_DB + @".dbo.QIT_QRStock_ProToIssue Z
		                    WHERE Z.ItemCode = @itemCode and  
                                  Z.FromWhs collate SQL_Latin1_General_CP850_CI_AS = A.WhsCode
	                    ),0)
                    ) IssueQty,
                    (
                        ISNULL((
		                    SELECT sum(Z.Qty) DeliverQty FROM " + Global.QIT_DB + @".dbo.QIT_QRStock_SOToDelivery Z
		                    WHERE Z.ItemCode = @itemCode and  
                                  Z.FromWhs collate SQL_Latin1_General_CP850_CI_AS = A.WhsCode
	                    ),0)
                    ) DeliverQty,
                    (
                        ISNULL((
		                    SELECT sum(Z.Qty) ReceiptQty FROM " + Global.QIT_DB + @".dbo.QIT_QRStock_ProToReceipt Z
		                    WHERE Z.ItemCode = @itemCode and  
                                  Z.ToWhs collate SQL_Latin1_General_CP850_CI_AS = A.WhsCode
	                    ),0)
                    ) ReceiptQty
                FROM " + Global.QIT_DB + @".dbo.QIT_Warehouse_Master AS A 
                where Locked = 'N' 
                ) as A";

                _logger.LogInformation(" CommonsController : ItemStock() Query : {q} ", _Query.ToString());
                QITcon.Open();
                oAdptr = new SqlDataAdapter(_Query, QITcon);
                oAdptr.SelectCommand.Parameters.AddWithValue("@itemCode", ItemCode);
                oAdptr.Fill(dtStock);
                QITcon.Close();

                if (dtStock.Rows.Count > 0)
                {
                    List<ItemStock> obj = new List<ItemStock>();
                    dynamic arData = JsonConvert.SerializeObject(dtStock);
                    obj = JsonConvert.DeserializeObject<List<ItemStock>>(arData.ToString());
                    return obj;
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "No data found" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(" Error in CommonsController : ItemStock() :: {Error}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
            }
        }


        [HttpGet("ItemBinStock")]
        public async Task<ActionResult<IEnumerable<ItemBinStock>>> ItemBinStock(int BranchID, string ItemCode, string WhsCode)
        {
            try
            {
                _logger.LogInformation(" Calling CommonsController : ItemBinStock() ");

                DataTable dtStock = new DataTable();
                QITcon = new SqlConnection(_QIT_connection);

                if (ItemCode == null)
                    ItemCode = string.Empty;
                _Query = @" 
                SELECT C.WhsCode, C.WhsName, B.BinCode, SUM(A.Stock) Stock 
                FROM 
                (   
                    SELECT Z.ToBinAbsEntry Bin, ISNULL(sum(Z.Qty),0) * 1 Stock FROM " + Global.QIT_DB + @".dbo.[QIT_QRStock_InvTrans] Z
                    WHERE  Z.ItemCode = @itemCode and Z.FromObjType = '22' and Z.ToObjType = '67' and Z.ToWhs = @whsCode  and ISNULL(Z.BranchID, @bID) = @bID
                    GROUP BY Z.ToBinAbsEntry	
      
                    UNION 
	     
                    SELECT Z.ToBinAbsEntry Bin, ISNULL(sum(Z.Qty),0) * 1 Stock FROM " + Global.QIT_DB + @".dbo.[QIT_QRStock_InvTrans] Z
                    WHERE  Z.ItemCode = @itemCode and (Z.FromObjType <> '22') AND 
                           Z.ToWhs collate SQL_Latin1_General_CP850_CI_AS = @whsCode  and ISNULL(Z.BranchID, @bID) = @bID
                    GROUP BY Z.ToBinAbsEntry

                    UNION 

                    SELECT Z.FromBinAbsEntry Bin, ISNULL(sum(Z.Qty),0) * -1 Stock FROM " + Global.QIT_DB + @".dbo.[QIT_QRStock_InvTrans] Z
                    WHERE Z.ItemCode = @itemCode and (Z.FromObjType <> '22') and 
                            Z.FromWhs collate SQL_Latin1_General_CP850_CI_AS = @whsCode and ISNULL(Z.BranchID, @bID) = @bID
                    GROUP BY Z.FromBinAbsEntry

                    UNION 

                    SELECT Z.FromBinAbsEntry Bin, ISNULL(sum(Z.Qty),0) * -1 Stock FROM " + Global.QIT_DB + @".dbo.QIT_QRStock_ProToIssue Z
                    WHERE Z.ItemCode = @itemCode and  
                        Z.FromWhs collate SQL_Latin1_General_CP850_CI_AS = @whsCode and ISNULL(Z.BranchID, @bID) = @bID
                    GROUP BY Z.FromBinAbsEntry

                    UNION

                    SELECT Z.FromBinAbsEntry Bin, ISNULL(sum(Z.Qty),0) * -1 Stock FROM " + Global.QIT_DB + @".dbo.QIT_QRStock_SOToDelivery Z
                    WHERE Z.ItemCode = @itemCode and  
                          Z.FromWhs collate SQL_Latin1_General_CP850_CI_AS = @whsCode and ISNULL(Z.BranchID, @bID) = @bID
                    GROUP BY Z.FromBinAbsEntry

                    UNION

                    SELECT Z.ToBinAbsEntry Bin, ISNULL(sum(Z.Qty),0) * 1 Stock FROM " + Global.QIT_DB + @".dbo.QIT_QRStock_ProToReceipt Z
                         WHERE Z.ItemCode = @itemCode and  
                               Z.ToWhs collate SQL_Latin1_General_CP850_CI_AS = @whsCode and ISNULL(Z.BranchID, @bID) = @bID
                    GROUP BY Z.ToBinAbsEntry
                 ) as A
                 INNER JOIN " + Global.SAP_DB + @".dbo.OBIN B ON A.Bin = B.AbsEntry
                 INNER JOIN " + Global.SAP_DB + @".dbo.OWHS C ON C.WhsCode = B.WhsCode
                 GROUP BY C.WhsCode, C.WhsName, B.BinCode                   
                ";

                _logger.LogInformation(" CommonsController : ItemBinStock() Query : {q} ", _Query.ToString());
                QITcon.Open();
                oAdptr = new SqlDataAdapter(_Query, QITcon);
                oAdptr.SelectCommand.Parameters.AddWithValue("@bID", BranchID);
                oAdptr.SelectCommand.Parameters.AddWithValue("@itemCode", ItemCode);
                oAdptr.SelectCommand.Parameters.AddWithValue("@whsCode", WhsCode);
                oAdptr.Fill(dtStock);
                QITcon.Close();

                if (dtStock.Rows.Count > 0)
                {
                    List<ItemBinStock> obj = new List<ItemBinStock>();
                    dynamic arData = JsonConvert.SerializeObject(dtStock);
                    obj = JsonConvert.DeserializeObject<List<ItemBinStock>>(arData.ToString());
                    return obj;
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "No data found" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(" Error in CommonsController : ItemBinStock() :: {Error}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
            }
        }


        [HttpGet("BinLocation")]
        public async Task<ActionResult<IEnumerable<BinLocation>>> BinLocation(string WhsCode)
        {
            try
            {
                _logger.LogInformation(" Calling CommonsController : BinLocation() ");

                DataTable dtBin = new DataTable();
                SAPcon = new SqlConnection(_connection);

                _Query = @" select AbsEntry, BinCode from OBIN where Deleted = 'N' and WhsCode = @whsCode ";

                _logger.LogInformation(" CommonsController : BinLocation() Query : {q} ", _Query.ToString());
                SAPcon.Open();
                oAdptr = new SqlDataAdapter(_Query, SAPcon);
                oAdptr.SelectCommand.Parameters.AddWithValue("@whsCode", WhsCode);
                oAdptr.Fill(dtBin);
                SAPcon.Close();

                if (dtBin.Rows.Count > 0)
                {
                    List<BinLocation> obj = new List<BinLocation>();
                    dynamic arData = JsonConvert.SerializeObject(dtBin);
                    obj = JsonConvert.DeserializeObject<List<BinLocation>>(arData.ToString());
                    return obj;
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "No data found" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(" Error in CommonsController : BinLocation() :: {Error}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
            }
        }


        [HttpGet("QAStatus")]
        public async Task<ActionResult<IEnumerable<QAStatus>>> QAStatus()
        {
            try
            {
                _logger.LogInformation(" Calling CommonsController : QAStatus() ");

                DataTable dtBin = new DataTable();
                SAPcon = new SqlConnection(_connection);

                _Query =
                @" select ID + ' - ' + Status val, ID, Status 
                FROM
                (
                    select 1 SrNo, 'D' ID, 'Done' Status UNION
                    select 2 SrNo, 'A' ID, 'Pending' Status UNION
                    select 3 SrNo, 'PR' ID, 'Partial' Status UNION
                    select 4 SrNo, 'NA' ID, 'Not Applicable' 
                ) as A Order By A.SrNo ";

                _logger.LogInformation(" CommonsController : QAStatus() Query : {q} ", _Query.ToString());
                SAPcon.Open();
                oAdptr = new SqlDataAdapter(_Query, SAPcon);
                oAdptr.Fill(dtBin);
                SAPcon.Close();

                if (dtBin.Rows.Count > 0)
                {
                    List<QAStatus> obj = new List<QAStatus>();
                    dynamic arData = JsonConvert.SerializeObject(dtBin);
                    obj = JsonConvert.DeserializeObject<List<QAStatus>>(arData.ToString());
                    return obj;
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "No data found" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(" Error in CommonsController : QAStatus() :: {Error}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
            }
        }


        [HttpGet("IsExchangeRateExist")]
        public ActionResult<bool> IsExchangeRateExist(string TransDate)
        {
            try
            {
                System.Data.DataTable dtData = new System.Data.DataTable();
                _logger.LogInformation(" Calling CommonsController : IsExchangeRateExist() ");

                SAPcon = new SqlConnection(_connection);

                _Query = @" select * from ORTT where RateDate = @date ";
                _logger.LogInformation(" CommonsController : IsExchangeRateExist() Query : {q} ", _Query.ToString());
                oAdptr = new SqlDataAdapter(_Query, SAPcon);
                oAdptr.SelectCommand.Parameters.AddWithValue("@date", TransDate);
                oAdptr.Fill(dtData);
                SAPcon.Close();

                if (dtData.Rows.Count > 0)
                {
                    return Ok(new { StatusCode = "200", IsExist = "Y", StatusMsg = "Exchange Rate exist" });
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", IsExist = "N", StatusMsg = "Exchange Rate does not exist" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(" Error in CommonsController : IsExchangeRateExist() :: {Error}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
            }
        }


        [HttpGet("BusinessPartner")]
        public async Task<ActionResult<IEnumerable<BusinessPartner>>> GetBusinessPartner(string CardType)
        {
            try
            {
                _logger.LogInformation(" Calling CommonsController : GetBusinessPartner() ");
                DataTable dtBP = new DataTable();
                SAPcon = new SqlConnection(_connection);

                _Query = @" 
                SELECT CardCode, CardName 
                FROM OCRD 
                WHERE CardType = @cardType
                ORDER BY CardName
                FOR BROWSE  
                ";

                _logger.LogInformation(" CommonsController : GetBusinessPartner() Query : {q} ", _Query.ToString());
                SAPcon.Open();
                oAdptr = new SqlDataAdapter(_Query, SAPcon);
                oAdptr.SelectCommand.Parameters.AddWithValue("cardType", CardType);
                oAdptr.Fill(dtBP);
                SAPcon.Close();

                if (dtBP.Rows.Count > 0)
                {
                    List<BusinessPartner> obj = new List<BusinessPartner>();
                    dynamic arData = JsonConvert.SerializeObject(dtBP);
                    obj = JsonConvert.DeserializeObject<List<BusinessPartner>>(arData.ToString());
                    return obj;
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "No data found" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(" Error in CommonsController : GetBusinessPartner() :: {Error}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
            }
        }


        #region Purchase Order : QR Generation

        [HttpPost("IsHeaderQRExist")]
        public ActionResult<bool> IsHeaderQRExist(CheckHeaderPO payload)
        {
            try
            {
                System.Data.DataTable dtData = new System.Data.DataTable();
                _logger.LogInformation(" Calling CommonsController : IsHeaderQRExist() ");

                QITcon = new SqlConnection(_QIT_connection);

                _Query = @" SELECT * from QIT_QR_Header 
                            WHERE BranchID = @bId AND 
                                  DocEntry = @docEntry AND
                                  DocNum = @docNum AND
                                  Series = @series AND
                                  ObjType = @objType
                         ";
                _logger.LogInformation(" CommonsController : IsHeaderQRExist() Query : {q} ", _Query.ToString());
                oAdptr = new SqlDataAdapter(_Query, QITcon);

                oAdptr.SelectCommand.Parameters.AddWithValue("@bId", payload.BranchID);
                oAdptr.SelectCommand.Parameters.AddWithValue("@docEntry", payload.DocEntry);
                oAdptr.SelectCommand.Parameters.AddWithValue("@docNum", payload.DocNum);
                oAdptr.SelectCommand.Parameters.AddWithValue("@series", payload.Series);
                oAdptr.SelectCommand.Parameters.AddWithValue("@objType", payload.ObjType);
                oAdptr.Fill(dtData);
                QITcon.Close();

                if (dtData.Rows.Count > 0)
                {
                    return Ok(new { StatusCode = "200", IsExist = "Y", QRCode = dtData.Rows[0]["QRCodeID"].ToString().Replace("~", " ") });
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", IsExist = "N", StatusMsg = "QR Code ID does not exist" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(" Error in CommonsController : IsHeaderQRExist() :: {Error}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
            }
        }


        [HttpPost("GetHeaderQR")]
        public ActionResult<string> GetHeaderQR(CheckHeaderPO payload)
        {
            _isExist = false;
            try
            {
                _logger.LogInformation(" Calling CommonsController : GetHeaderQR() ");

                QITcon = new SqlConnection(_QIT_connection);

                string _strDay = DateTime.Now.Day.ToString("D2");
                string _strMonth = DateTime.Now.Month.ToString("D2");
                string _strYear = DateTime.Now.Year.ToString();

                string _qr = ConvertInQRString(_strYear) + ConvertInQRString(_strMonth) + ConvertInQRString(_strDay) + "~" +
                             ConvertInQRString(_strMonth) + "~";

                _Query = @" SELECT RIGHT('00000' + CONVERT(VARCHAR,ISNULL(MAX(INC_NO),0) + 1), 6) 
                            FROM QIT_QR_Header 
                            WHERE YEAR(EntryDate) = @year AND 
							      FORMAT(MONTH(EntryDate), '00') = @month -- AND FORMAT(Day(EntryDate), '00') = @day 
                          ";
                _logger.LogInformation(" CommonsController : GetHeaderQR() Query : {q} ", _Query.ToString());

                cmd = new SqlCommand(_Query, QITcon);
                cmd.Parameters.AddWithValue("@year", _strYear);
                cmd.Parameters.AddWithValue("@month", _strMonth);
                //cmd.Parameters.AddWithValue("@day", _strDay);
                QITcon.Open();
                Object Value = cmd.ExecuteScalar();
                QITcon.Close();

                _qr = _qr + Value.ToString() + "~" + "PO";
                return Ok(new { StatusCode = "200", QRCode = _qr.Replace("~", " "), IncNo = Value.ToString() });
                //return _qr;
            }
            catch (Exception ex)
            {
                _logger.LogError(" Error in CommonsController : GetHeaderQR() :: {Error}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
            }
        }


        [HttpPost("SaveHeaderQR")]
        public IActionResult SaveHeaderQR(SaveHeaderQR payload)
        {
            string _IsSaved = "N";
            try
            {
                _logger.LogInformation(" Calling CommonsController : SaveHeaderQR() ");

                if (payload != null)
                {
                    if (payload.QRCodeID.Replace(" ", "~").Split('~')[2].ToString() != payload.IncNo)
                    {
                        return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Inc No must be " + payload.QRCodeID.Replace(" ", "~").Split('~')[2].ToString() });
                    }

                    QITcon = new SqlConnection(_QIT_connection);
                    _Query = @" INSERT INTO QIT_QR_Header(HeaderSrNo, BranchID, QRCodeID, DocEntry, DocNum, Series, DocDate, ObjType, Inc_No, EntryDate)
                                VALUES( (select ISNULL(max(HeaderSrNo),0) + 1 from QIT_QR_Header), @bId, @qr, @docEntry, @docNum, @series, @docDate, @objType, @incNo, @entryDate)
                              ";
                    _logger.LogInformation(" CommonsController : SaveHeaderQR() Query : {q} ", _Query.ToString());

                    cmd = new SqlCommand(_Query, QITcon);
                    cmd.Parameters.AddWithValue("@bID", payload.BranchID);
                    cmd.Parameters.AddWithValue("@qr", payload.QRCodeID.Replace(" ", "~"));
                    cmd.Parameters.AddWithValue("@docEntry", payload.DocEntry);
                    cmd.Parameters.AddWithValue("@docNum", payload.DocNum);
                    cmd.Parameters.AddWithValue("@series", payload.Series);
                    cmd.Parameters.AddWithValue("@docDate", DateTime.Parse(payload.DocDate));
                    cmd.Parameters.AddWithValue("@objType", payload.ObjType);
                    cmd.Parameters.AddWithValue("@incNo", payload.IncNo);
                    cmd.Parameters.AddWithValue("@entryDate", DateTime.Now);

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
                _logger.LogError(" Error in CommonsController : SaveHeaderQR() :: {Error}", ex.ToString());
                if (ex.Message.ToLower().Contains("uc_qrcodeid"))
                {
                    return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "QR Code ID is already exists" });
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
                }
            }
        }


        [HttpPost("GetDetailQR")]
        public ActionResult<string> GetDetailQR(string HeaderQR)
        {
            _isExist = false;
            try
            {
                _logger.LogInformation(" Calling CommonsController : GetDetailQR() ");
                QITcon = new SqlConnection(_QIT_connection);
                DataTable dtData = new DataTable();

                _Query = @" SELECT * FROM QIT_QR_Header WHERE QRCodeID = @headerQR ";
                _logger.LogInformation(" CommonsController : GetDetailQR() Query : {q} ", _Query.ToString());

                oAdptr = new SqlDataAdapter(_Query, QITcon);
                oAdptr.SelectCommand.Parameters.AddWithValue("@headerQR", HeaderQR.Replace(" ", "~"));
                QITcon.Open();
                oAdptr.Fill(dtData);
                QITcon.Close();

                if (dtData.Rows.Count > 0)
                {
                    _Query = @" SELECT RIGHT('00000' + CONVERT(VARCHAR,ISNULL(MAX(A.Inc_No),0) + 1), 6) 
                                FROM QIT_QR_Detail A inner join QIT_QR_Header B on A.HeaderSrNo = B.HeaderSrNo
                                WHERE B.QRCodeID = @headerQR
                              ";
                    _logger.LogInformation(" CommonsController : GetDetailQR() Query : {q} ", _Query.ToString());

                    cmd = new SqlCommand(_Query, QITcon);
                    cmd.Parameters.AddWithValue("@headerQR", HeaderQR.Replace(" ", "~"));
                    QITcon.Open();
                    Object Value = cmd.ExecuteScalar();
                    QITcon.Close();

                    string _qr = HeaderQR + "~" + Value.ToString();
                    return Ok(new { StatusCode = "200", QRCode = _qr.Replace("~", " "), IncNo = Value.ToString() });
                    // return Value.ToString();
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", IsExist = "N", StatusMsg = "Header QR does not exist" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(" Error in CommonsController : GetDetailQR() :: {Error}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
            }
        }


        [HttpPost("IsDetailQRExist")]
        public ActionResult<bool> IsDetailQRExist(CheckDetailPO payload)
        {
            try
            {
                System.Data.DataTable dtData = new System.Data.DataTable();
                _logger.LogInformation(" Calling CommonsController : IsDetailQRExist() ");

                QITcon = new SqlConnection(_QIT_connection);

                _Query = @" SELECT A.* from QIT_QR_Detail A inner join QIT_QR_Header B on A.HeaderSrNo = B.HeaderSrNo
                            WHERE B.BranchID = @bId AND 
                                  B.DocEntry = @docEntry AND
                                  B.DocNum = @docNum AND
                                  B.Series = @series AND
                                  B.ObjType = @objType AND A.ItemCode = @iCode AND A.GateInNo = @gateInNo AND A.LineNum = @line
                         ";
                _logger.LogInformation(" CommonsController : IsDetailQRExist() Query : {q} ", _Query.ToString());
                oAdptr = new SqlDataAdapter(_Query, QITcon);

                oAdptr.SelectCommand.Parameters.AddWithValue("@bId", payload.BranchID);
                oAdptr.SelectCommand.Parameters.AddWithValue("@docEntry", payload.DocEntry);
                oAdptr.SelectCommand.Parameters.AddWithValue("@docNum", payload.DocNum);
                oAdptr.SelectCommand.Parameters.AddWithValue("@series", payload.Series);
                oAdptr.SelectCommand.Parameters.AddWithValue("@objType", payload.ObjType);
                oAdptr.SelectCommand.Parameters.AddWithValue("@iCode", payload.ItemCode);
                oAdptr.SelectCommand.Parameters.AddWithValue("@line", payload.LineNum);
                oAdptr.SelectCommand.Parameters.AddWithValue("@gateInNo", payload.GateInNo);
                oAdptr.Fill(dtData);
                QITcon.Close();

                if (dtData.Rows.Count > 0)
                {
                    return Ok(new { StatusCode = "400", IsExist = "Y", QRCode = dtData.Rows[0]["QRCodeID"].ToString().Replace("~", " ") });
                    //return true;
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", IsExist = "N", StatusMsg = "QR Code ID does not exist" });
                    //return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(" Error in CommonsController : IsDetailQRExist() :: {Error}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
            }
        }


        [HttpPost("SaveDetailQR")]
        public IActionResult SaveDetailQR(SaveDetailQR payload)
        {
            string _IsSaved = "N";
            try
            {
                _logger.LogInformation(" Calling CommonsController : SaveDetailQR() ");

                if (payload != null)
                {
                    QITcon = new SqlConnection(_QIT_connection);
                    DataTable dtData = new DataTable();

                    _Query = @" SELECT * FROM QIT_QR_Header WHERE QRCodeID = @headerQR ";
                    _logger.LogInformation(" CommonsController : DetailIncNo() Query : {q} ", _Query.ToString());

                    oAdptr = new SqlDataAdapter(_Query, QITcon);
                    oAdptr.SelectCommand.Parameters.AddWithValue("@headerQR", payload.HeaderQRCodeID.Replace(" ", "~"));
                    QITcon.Open();
                    oAdptr.Fill(dtData);
                    QITcon.Close();

                    if (dtData.Rows.Count > 0)
                    {
                        _Query = " INSERT INTO QIT_QR_Detail " +
                                 " (   DetailSrNo, HeaderSrNo, GateInNo, BranchID, QRCodeID, Inc_No, ItemCode, LineNum, QRMngBy, " +
                                 "     BatchSerialNo, Qty, Remark,  EntryDate" +
                                 " ) " +
                                 " VALUES " +
                                 " ( " +
                                 "      (select ISNULL(max(DetailSrNo),0) + 1 from QIT_QR_Detail), " +
                                 "      @hSrNo, @gateInNo,  @bId, @qr, @incNo, @iCode, @line, @qrMngBy, " +
                                 "      case when '" + payload.QRMngBy + "' = 'N' then '-' else (" +
                                 "      case when (select count(*) from QIT_QR_Detail where YEAR(EntryDate) = YEAR(GETDATE()) and MONTH(EntryDate) = MONTH(GETDATE())) > 0 then (  " +
                                 "          select top 1 RIGHT(YEAR(GetDate()),2) + RIGHT('00' + CONVERT(VARCHAR,MONTH(GETDATE()),2), 2) + " +
                                 "                 case when BatchSerialNo is null then RIGHT('000000' + CONVERT(VARCHAR, 1), 6) " +
                                 "                 else RIGHT('000000' + CONVERT(VARCHAR,  (right(left(BatchSerialNo,10), 6) + 1)), 6)  end + " +
                                 "      '" + payload.QRMngBy + "' + " + "'PO'" +
                                 "          from QIT_QR_Detail " +
                                 "          where YEAR(EntryDate) = YEAR(GETDATE()) and MONTH(EntryDate) = MONTH(GETDATE())" +
                                 "          order by DetailSrNo desc )" +
                                 "          else" +
                                 "          RIGHT(YEAR(GetDate()),2) + RIGHT('00' + CONVERT(VARCHAR,MONTH(GETDATE()),2), 2) +  " +

                                 "          RIGHT('000000' + CONVERT(VARCHAR, 1), 6) + " +
                                 "      '" + payload.QRMngBy + "' + " + "'PO'" +
                                 "end  " +
                                 "      ) end , " +
                                 "      @qty, @remark, @entryDate " +
                                 " )";

                        _logger.LogInformation(" CommonsController : SaveDetailQR() Query : {q} ", _Query.ToString());

                        cmd = new SqlCommand(_Query, QITcon);
                        cmd.Parameters.AddWithValue("@hSrNo", dtData.Rows[0]["HeaderSrNo"]);
                        cmd.Parameters.AddWithValue("@gateInNo", payload.GateInNo);
                        cmd.Parameters.AddWithValue("@bId", payload.BranchID);
                        cmd.Parameters.AddWithValue("@qr", payload.DetailQRCodeID.Replace(" ", "~"));
                        cmd.Parameters.AddWithValue("@incNo", payload.IncNo);
                        cmd.Parameters.AddWithValue("@iCode", payload.ItemCode);
                        cmd.Parameters.AddWithValue("@line", payload.LineNum);
                        cmd.Parameters.AddWithValue("@qrMngBy", payload.QRMngBy);
                        cmd.Parameters.AddWithValue("@qty", payload.Qty);
                        cmd.Parameters.AddWithValue("@remark", payload.Remark);
                        cmd.Parameters.AddWithValue("@entryDate", DateTime.Now);

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
                        return BadRequest(new { StatusCode = "400", IsExist = "N", StatusMsg = "PO QR does not exist" });
                    }
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = " Details not found" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(" Error in CommonsController : SaveDetailQR() :: {Error}", ex.ToString());
                if (ex.Message.ToLower().Contains("uc_qrcodeid_det"))
                {
                    return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Detail QR Code ID is already exists" });
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
                }
            }
        }


        [HttpPost("GetDetailDataQR")]
        public async Task<ActionResult<IEnumerable<SaveDetailQR>>> DetailDataQR(CheckDetailPO payload)
        {
            try
            {
                _logger.LogInformation(" Calling CommonsController : DetailDataQR() ");
               
                DataTable dtData = new DataTable();
                QITcon = new SqlConnection(_QIT_connection);

              

                _Query = @" 
                SELECT  A.BranchID BranchID, B.DocNum, B.QRCodeID HeaderQRCodeID, A.QRCodeID DetailQRCodeID, A.GateInNo,
                        A.Inc_No IncNo, A.ItemCode ItemCode, E.ItemName, A.LineNum, A.QRMngBy QRMngBy, A.Qty Qty, 
                        A.Remark, F.Project, A.BatchSerialNo, D.BinCode DefaultBin
                FROM QIT_QR_Detail A 
                INNER JOIN QIT_QR_Header B on A.HeaderSrNo = B.HeaderSrNo
                INNER JOIN QIT_GateIN F ON F.GateInNo = A.GateInNo and F.ItemCode = A.ItemCode and 
                           F.LineNum = A.LineNum and F.DocEntry = B.DocEntry
                LEFT JOIN " + Global.SAP_DB + @".dbo.OITW C ON A.ItemCode collate SQL_Latin1_General_CP850_CI_AS = C.ItemCode AND 
                          C.DftBinAbs is not null
	            LEFT JOIN " + Global.SAP_DB + @".dbo.OBIN D ON D.AbsEntry = C.DftBinAbs
                LEFT JOIN " + Global.SAP_DB + @".dbo.OITM E ON E.ItemCode collate SQL_Latin1_General_CP1_CI_AS = A.ItemCode
                WHERE ISNULL(A.BranchID, @bid) = @bid AND B.DocEntry = @docEntry and B.DocNum = @docNum and 
                      B.Series = @series and B.ObjType = @objType and A.ItemCode = @iCode and A.LineNum = @line and A.GateInNo = @gateInNo ";
                _logger.LogInformation(" CommonsController : DetailDataQR() Query : {q} ", _Query.ToString());
                QITcon.Open();
                oAdptr = new SqlDataAdapter(_Query, QITcon);
                oAdptr.SelectCommand.Parameters.AddWithValue("@bid", payload.BranchID);
                oAdptr.SelectCommand.Parameters.AddWithValue("@docEntry", payload.DocEntry);
                oAdptr.SelectCommand.Parameters.AddWithValue("@docNum", payload.DocNum);
                oAdptr.SelectCommand.Parameters.AddWithValue("@series", payload.Series);
                oAdptr.SelectCommand.Parameters.AddWithValue("@objType", payload.ObjType);
                oAdptr.SelectCommand.Parameters.AddWithValue("@iCode", payload.ItemCode);
                oAdptr.SelectCommand.Parameters.AddWithValue("@line", payload.LineNum);
                oAdptr.SelectCommand.Parameters.AddWithValue("@gateInNo", payload.GateInNo);

                oAdptr.Fill(dtData);
                QITcon.Close();

                if (dtData.Rows.Count > 0)
                {
                    List<SaveDetailQR> obj = new List<SaveDetailQR>();
                    dynamic arData = JsonConvert.SerializeObject(dtData);
                    obj = JsonConvert.DeserializeObject<List<SaveDetailQR>>(arData.ToString().Replace("~", " "));
                    return obj;
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "No data found" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(" Error in CommonsController : DetailDataQR() :: {Error}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
            }
        }


        [HttpPost("GetAllDetailDataQR")]
        public async Task<ActionResult<IEnumerable<SaveDetailQR>>> GetAllDetailDataQR(CheckAllDetailPO payload)
        {
            try
            {
                _logger.LogInformation(" Calling CommonsController : GetAllDetailDataQR() ");

                DataTable dtData = new DataTable();
                QITcon = new SqlConnection(_QIT_connection);

                _Query = @" 
                SELECT  A.BranchID BranchID, B.DocNum, B.QRCodeID HeaderQRCodeID, A.QRCodeID DetailQRCodeID, A.GateInNo, 
                        A.Inc_No IncNo, A.ItemCode ItemCode, E.ItemName, A.LineNum, A.QRMngBy QRMngBy, 
                        A.Qty Qty, A.Remark, F.Project, A.BatchSerialNo, D.BinCode DefaultBin
                FROM QIT_QR_Detail A 
                INNER JOIN QIT_QR_Header B on A.HeaderSrNo = B.HeaderSrNo
                INNER JOIN QIT_GateIN F ON F.GateInNo = A.GateInNo and F.ItemCode = A.ItemCode and 
                           F.LineNum = A.LineNum and F.DocEntry = B.DocEntry
                LEFT JOIN " + Global.SAP_DB + @".dbo.OITW C ON A.ItemCode collate SQL_Latin1_General_CP850_CI_AS = C.ItemCode AND 
                          C.DftBinAbs is not null
	            LEFT JOIN " + Global.SAP_DB + @".dbo.OBIN D ON D.AbsEntry = C.DftBinAbs
                LEFT JOIN " + Global.SAP_DB + @".dbo.OITM E ON E.ItemCode collate SQL_Latin1_General_CP1_CI_AS = A.ItemCode
                WHERE ISNULL(A.BranchID, @bid) = @bid and A.GateInNo = @gateInNo ";
                _logger.LogInformation(" CommonsController : GetAllDetailDataQR() Query : {q} ", _Query.ToString());
                QITcon.Open();
                oAdptr = new SqlDataAdapter(_Query, QITcon);
                oAdptr.SelectCommand.Parameters.AddWithValue("@bid", payload.BranchID);  
                oAdptr.SelectCommand.Parameters.AddWithValue("@gateInNo", payload.GateInNo);

                oAdptr.Fill(dtData);
                QITcon.Close();

                if (dtData.Rows.Count > 0)
                {
                    List<SaveDetailQR> obj = new List<SaveDetailQR>();
                    dynamic arData = JsonConvert.SerializeObject(dtData);
                    obj = JsonConvert.DeserializeObject<List<SaveDetailQR>>(arData.ToString().Replace("~", " "));
                    return obj;
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "No data found" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(" Error in CommonsController : GetAllDetailDataQR() :: {Error}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
            }
        }


        #endregion

        #region Production Order : QR Generation

        [HttpPost("IsHeaderProductionQRExist")]
        public ActionResult<bool> IsHeaderProductionQRExist(CheckHeaderPO payload)
        {
            try
            {
                System.Data.DataTable dtData = new System.Data.DataTable();
                _logger.LogInformation(" Calling CommonsController : IsHeaderProductionQRExist() ");

                QITcon = new SqlConnection(_QIT_connection);

                _Query = @" SELECT * from QIT_Production_QR_Header 
                            WHERE BranchID = @bId AND 
                                  DocEntry = @docEntry AND
                                  DocNum = @docNum AND
                                  Series = @series AND
                                  ObjType = @objType
                         ";
                _logger.LogInformation(" CommonsController : IsHeaderProductionQRExist() Query : {q} ", _Query.ToString());
                oAdptr = new SqlDataAdapter(_Query, QITcon);

                oAdptr.SelectCommand.Parameters.AddWithValue("@bId", payload.BranchID);
                oAdptr.SelectCommand.Parameters.AddWithValue("@docEntry", payload.DocEntry);
                oAdptr.SelectCommand.Parameters.AddWithValue("@docNum", payload.DocNum);
                oAdptr.SelectCommand.Parameters.AddWithValue("@series", payload.Series);
                oAdptr.SelectCommand.Parameters.AddWithValue("@objType", payload.ObjType);
                oAdptr.Fill(dtData);
                QITcon.Close();

                if (dtData.Rows.Count > 0)
                {
                    return Ok(new { StatusCode = "200", IsExist = "Y", QRCode = dtData.Rows[0]["QRCodeID"].ToString().Replace("~", " ") });
                    //return true;
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", IsExist = "N", StatusMsg = "QR Code ID does not exist" });
                    //return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(" Error in CommonsController : IsHeaderProductionQRExist() :: {Error}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
            }
        }


        [HttpPost("GetProductionHeaderQR")]
        public ActionResult<string> GetProductionHeaderQR(CheckHeaderPO payload)
        {
            _isExist = false;
            try
            {
                _logger.LogInformation(" Calling CommonsController : GetProductionHeaderQR() ");

                QITcon = new SqlConnection(_QIT_connection);

                string _strDay = DateTime.Now.Day.ToString("D2");
                string _strMonth = DateTime.Now.Month.ToString("D2");
                string _strYear = DateTime.Now.Year.ToString();

                string _qr = ConvertInQRString(_strYear) + ConvertInQRString(_strMonth) + ConvertInQRString(_strDay) + "~" +
                             ConvertInQRString(_strMonth) + "~";

                _Query = @" SELECT RIGHT('00000' + CONVERT(VARCHAR,ISNULL(MAX(INC_NO),0) + 1), 6) 
                            FROM QIT_Production_QR_Header 
                            WHERE YEAR(EntryDate) = @year AND 
							      FORMAT(MONTH(EntryDate), '00') = @month AND 
							      FORMAT(Day(EntryDate), '00') = @day ";
                _logger.LogInformation(" CommonsController : GetProductionHeaderQR() Query : {q} ", _Query.ToString());

                cmd = new SqlCommand(_Query, QITcon);
                cmd.Parameters.AddWithValue("@year", _strYear);
                cmd.Parameters.AddWithValue("@month", _strMonth);
                cmd.Parameters.AddWithValue("@day", _strDay);
                QITcon.Open();
                Object Value = cmd.ExecuteScalar();
                QITcon.Close();

                _qr = _qr + Value.ToString() + "~" + "PRO";
                return Ok(new { StatusCode = "200", QRCode = _qr.Replace("~", " "), IncNo = Value.ToString() });
            }
            catch (Exception ex)
            {
                _logger.LogError(" Error in CommonsController : GetProductionHeaderQR() :: {Error}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
            }
        }


        [HttpPost("SaveHeaderProductionQR")]
        public IActionResult SaveHeaderProductionQR(SaveHeaderQR payload)
        {
            string _IsSaved = "N";
            try
            {
                _logger.LogInformation(" Calling CommonsController : SaveHeaderProductionQR() ");

                if (payload != null)
                {
                    if (payload.QRCodeID.Replace(" ", "~").Split('~')[2].ToString() != payload.IncNo)
                    {
                        return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Inc No must be " + payload.QRCodeID.Replace(" ", "~").Split('~')[2].ToString() });
                    }

                    QITcon = new SqlConnection(_QIT_connection);
                    _Query = @" INSERT INTO QIT_Production_QR_Header(HeaderSrNo, BranchID, QRCodeID, DocEntry, DocNum, Series, DocDate, ObjType, Inc_No, EntryDate)
                                VALUES( (select ISNULL(max(HeaderSrNo),0) + 1 from QIT_Production_QR_Header), @bId, @qr, @docEntry, @docNum, @series, @docDate, @objType, @incNo, @entryDate)
                              ";
                    _logger.LogInformation(" CommonsController : SaveHeaderProductionQR() Query : {q} ", _Query.ToString());

                    cmd = new SqlCommand(_Query, QITcon);
                    cmd.Parameters.AddWithValue("@bID", payload.BranchID);
                    cmd.Parameters.AddWithValue("@qr", payload.QRCodeID.Replace(" ", "~"));
                    cmd.Parameters.AddWithValue("@docEntry", payload.DocEntry);
                    cmd.Parameters.AddWithValue("@docNum", payload.DocNum);
                    cmd.Parameters.AddWithValue("@series", payload.Series);
                    cmd.Parameters.AddWithValue("@docDate", DateTime.Parse(payload.DocDate));
                    cmd.Parameters.AddWithValue("@objType", payload.ObjType);
                    cmd.Parameters.AddWithValue("@incNo", payload.IncNo);
                    cmd.Parameters.AddWithValue("@entryDate", DateTime.Now);

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
                _logger.LogError(" Error in CommonsController : SaveHeaderProductionQR() :: {Error}", ex.ToString());
                if (ex.Message.ToLower().Contains("uc_productionqrcodeid"))
                {
                    return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "QR Code ID is already exists" });
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
                }
            }
        }


        [HttpPost("IsDetailProductionQRExist")]
        public ActionResult<bool> IsDetailProductionQRExist(CheckDetailPRO payload)
        {
            try
            {
                System.Data.DataTable dtData = new System.Data.DataTable();
                _logger.LogInformation(" Calling CommonsController : IsDetailProductionQRExist() ");

                QITcon = new SqlConnection(_QIT_connection);

                _Query = @" SELECT A.* from QIT_Production_QR_Detail A inner join QIT_Production_QR_Header B on A.HeaderSrNo = B.HeaderSrNo
                            WHERE B.BranchID = @bId AND 
                                  B.DocEntry = @docEntry AND
                                  B.DocNum = @docNum AND
                                  B.Series = @series AND
                                  B.ObjType = @objType AND A.ItemCode = @iCode AND A.RecNo = @recNo
                         ";
                _logger.LogInformation(" CommonsController : IsDetailProductionQRExist() Query : {q} ", _Query.ToString());
                 oAdptr = new SqlDataAdapter(_Query, QITcon);

                oAdptr.SelectCommand.Parameters.AddWithValue("@bId", payload.BranchID);
                oAdptr.SelectCommand.Parameters.AddWithValue("@docEntry", payload.DocEntry);
                oAdptr.SelectCommand.Parameters.AddWithValue("@docNum", payload.DocNum);
                oAdptr.SelectCommand.Parameters.AddWithValue("@series", payload.Series);
                oAdptr.SelectCommand.Parameters.AddWithValue("@objType", payload.ObjType);
                oAdptr.SelectCommand.Parameters.AddWithValue("@iCode", payload.ItemCode);
                oAdptr.SelectCommand.Parameters.AddWithValue("@recNo", payload.RecNo);
                oAdptr.Fill(dtData);
                QITcon.Close();

                if (dtData.Rows.Count > 0)
                {
                    return Ok(new { StatusCode = "200", IsExist = "Y", QRCode = dtData.Rows[0]["QRCodeID"].ToString().Replace("~", " ") });
                    //return true;
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", IsExist = "N", StatusMsg = "QR Code ID does not exist" });
                    //return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(" Error in CommonsController : IsDetailProductionQRExist() :: {Error}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
            }
        }


        [HttpPost("GetProductionDetailQR")]
        public ActionResult<string> GetProductionDetailQR(string HeaderQR)
        {
            _isExist = false;
            try
            {
                _logger.LogInformation(" Calling CommonsController : GetProductionDetailQR() ");
                QITcon = new SqlConnection(_QIT_connection);
                DataTable dtData = new DataTable();

                _Query = @" SELECT * FROM QIT_Production_QR_Header WHERE QRCodeID = @headerQR ";
                _logger.LogInformation(" CommonsController : GetProductionDetailQR() Query : {q} ", _Query.ToString());

                 oAdptr = new SqlDataAdapter(_Query, QITcon);
                oAdptr.SelectCommand.Parameters.AddWithValue("@headerQR", HeaderQR.Replace(" ", "~"));
                QITcon.Open();
                oAdptr.Fill(dtData);
                QITcon.Close();

                if (dtData.Rows.Count > 0)
                {
                    _Query = @" SELECT RIGHT('00000' + CONVERT(VARCHAR,ISNULL(MAX(A.Inc_No),0) + 1), 6) 
                                FROM QIT_Production_QR_Detail A inner join QIT_Production_QR_Header B on A.HeaderSrNo = B.HeaderSrNo
                                WHERE B.QRCodeID = @headerQR
                              ";
                    _logger.LogInformation(" CommonsController : GetProductionDetailQR() Query : {q} ", _Query.ToString());

                     cmd = new SqlCommand(_Query, QITcon);
                    cmd.Parameters.AddWithValue("@headerQR", HeaderQR.Replace(" ", "~"));
                    QITcon.Open();
                    Object Value = cmd.ExecuteScalar();
                    QITcon.Close();

                    string _qr = HeaderQR + "~" + Value.ToString();
                    return Ok(new { StatusCode = "200", QRCode = _qr.Replace("~", " "), IncNo = Value.ToString() });
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", IsExist = "N", StatusMsg = "Header QR does not exist" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(" Error in CommonsController : GetProductionDetailQR() :: {Error}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
            }
        }


        [HttpPost("GetDetailDataProductionQR")]
        public async Task<ActionResult<IEnumerable<SaveDetailQR>>> GetDetailDataProductionQR(CheckDetailPRO payload)
        {
            try
            {
                _logger.LogInformation(" Calling CommonsController : GetDetailDataProductionQR() ");
               
                DataTable dtData = new DataTable();
                QITcon = new SqlConnection(_QIT_connection);

                _Query = @" select A.BranchID BranchID, B.QRCodeID HeaderQRCodeID, A.QRCodeID DetailQRCodeID, 
                                   A.Inc_No IncNo, A.ItemCode ItemCode, A.QRMngBy QRMngBy, A.Qty Qty, A.Remark, A.BatchSerialNo
                            from QIT_Production_QR_Detail A inner join QIT_Production_QR_Header B on A.HeaderSrNo = B.HeaderSrNo
                            where ISNULL(A.BranchID, @bid) = @bid AND B.DocEntry = @docEntry and B.DocNum = @docNum and 
                                  B.Series = @series and B.ObjType = @objType and ItemCode = @iCode and A.RecNo = @recNo ";
                _logger.LogInformation(" CommonsController : GetDetailDataProductionQR() Query : {q} ", _Query.ToString());
                QITcon.Open();
                SqlDataAdapter oAdptr = new SqlDataAdapter(_Query, QITcon);
                oAdptr.SelectCommand.Parameters.AddWithValue("@bid", payload.BranchID);
                oAdptr.SelectCommand.Parameters.AddWithValue("@docEntry", payload.DocEntry);
                oAdptr.SelectCommand.Parameters.AddWithValue("@docNum", payload.DocNum);
                oAdptr.SelectCommand.Parameters.AddWithValue("@series", payload.Series);
                oAdptr.SelectCommand.Parameters.AddWithValue("@objType", payload.ObjType);
                oAdptr.SelectCommand.Parameters.AddWithValue("@iCode", payload.ItemCode);
                oAdptr.SelectCommand.Parameters.AddWithValue("@recNo", payload.RecNo);

                oAdptr.Fill(dtData);
                QITcon.Close();

                if (dtData.Rows.Count > 0)
                {
                    List<SaveDetailQR> obj = new List<SaveDetailQR>();
                    dynamic arData = JsonConvert.SerializeObject(dtData);
                    obj = JsonConvert.DeserializeObject<List<SaveDetailQR>>(arData.ToString().Replace("~", " "));
                    return obj;
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "No data found" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(" Error in CommonsController : GetDetailDataProductionQR() :: {Error}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
            }
        }


        [HttpPost("SaveDetailProductionQR")]
        public IActionResult SaveDetailProductionQR(SaveDetailProductionQR payload)
        {
            string _IsSaved = "N";
            try
            {
                _logger.LogInformation(" Calling CommonsController : SaveDetailProductionQR() ");

                if (payload != null)
                {
                    QITcon = new SqlConnection(_QIT_connection);
                    DataTable dtData = new DataTable();

                    _Query = @" SELECT * FROM QIT_Production_QR_Header WHERE QRCodeID = @headerQR ";
                    _logger.LogInformation(" CommonsController : DetailProductionIncNo() Query : {q} ", _Query.ToString());

                    oAdptr = new SqlDataAdapter(_Query, QITcon);
                    oAdptr.SelectCommand.Parameters.AddWithValue("@headerQR", payload.HeaderQRCodeID.Replace(" ", "~"));
                    QITcon.Open();
                    oAdptr.Fill(dtData);
                    QITcon.Close();

                    if (dtData.Rows.Count > 0)
                    {
                        _Query = " INSERT INTO QIT_Production_QR_Detail " +
                                 " (   DetailSrNo, HeaderSrNo, RecNo, BranchID, QRCodeID, Inc_No, ItemCode, QRMngBy, " +
                                 "     BatchSerialNo, Project, Qty, Remark,  EntryDate" +
                                 " ) " +
                                 " VALUES " +
                                 " ( " +
                                 "      (select ISNULL(max(DetailSrNo),0) + 1 from QIT_Production_QR_Detail), " +
                                 "      @hSrNo, @recNo,  @bId, @qr, @incNo, @iCode, @qrMngBy, " +
                                 "      case when '" + payload.QRMngBy + "' = 'N' then '-' else (" +
                                 "      case when (select count(*) from QIT_Production_QR_Detail where  YEAR(EntryDate) = YEAR(GETDATE()) and MONTH(EntryDate) = MONTH(GETDATE())) > 0  then (  " +
                                 "          select top 1 RIGHT(YEAR(GetDate()),2) + RIGHT('00' + CONVERT(VARCHAR,MONTH(GETDATE()),2), 2) + " +
                                 "                 case when BatchSerialNo is null then RIGHT('000000' + CONVERT(VARCHAR, 1), 6) " +
                                 "                 else RIGHT('000000' + CONVERT(VARCHAR,  (right(left(BatchSerialNo,10), 6) + 1)), 6)  end + " +
                                 "      '" + payload.QRMngBy + "' + " + "'PRO'" +
                                 "          from QIT_Production_QR_Detail " +
                                 "          where YEAR(EntryDate) = YEAR(GETDATE()) and MONTH(EntryDate) = MONTH(GETDATE())" +
                                 "          order by DetailSrNo desc )" +
                                 "          else" +
                                 "          RIGHT(YEAR(GetDate()),2) + RIGHT('00' + CONVERT(VARCHAR,MONTH(GETDATE()),2), 2) +  " +
                                 "          RIGHT('000000' + CONVERT(VARCHAR, 1), 6) + " +
                                 "      '" + payload.QRMngBy + "' + " + "'PRO'" +
                                 "end  " +
                                 "      ) end , " +
                                 "      @proj, @qty, @remark, @entryDate " +
                                 " )";

                        _logger.LogInformation(" CommonsController : SaveDetailProductionQR() Query : {q} ", _Query.ToString());

                         cmd = new SqlCommand(_Query, QITcon);
                        cmd.Parameters.AddWithValue("@hSrNo", dtData.Rows[0]["HeaderSrNo"]);
                        cmd.Parameters.AddWithValue("@recNo", payload.RecNo);
                        cmd.Parameters.AddWithValue("@bId", payload.BranchID);
                        cmd.Parameters.AddWithValue("@qr", payload.DetailQRCodeID.Replace(" ", "~"));
                        cmd.Parameters.AddWithValue("@incNo", payload.IncNo);
                        cmd.Parameters.AddWithValue("@iCode", payload.ItemCode);
                        cmd.Parameters.AddWithValue("@qrMngBy", payload.QRMngBy);
                        cmd.Parameters.AddWithValue("@proj", payload.Project);
                        cmd.Parameters.AddWithValue("@qty", payload.Qty);
                        cmd.Parameters.AddWithValue("@remark", payload.Remark);
                        cmd.Parameters.AddWithValue("@entryDate", DateTime.Now);

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
                        return BadRequest(new { StatusCode = "400", IsExist = "N", StatusMsg = "PO QR does not exist" });
                    }
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = " Details not found" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(" Error in CommonsController : SaveDetailProductionQR() :: {Error}", ex.ToString());
                if (ex.Message.ToLower().Contains("uc_productionqrcodeid_det"))
                {
                    return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Detail QR Code ID is already exists" });
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
                }
            }
        }

        #endregion

        #region Method

        private string ConvertInQRString(string p_str)
        {
            try
            {
                _logger.LogInformation(" Calling ProductionController : ConvertInQRString() ");
                string _retVal = p_str.Replace('0', 'A')
                                        .Replace('1', 'B')
                                        .Replace('2', 'C')
                                        .Replace('3', 'D')
                                        .Replace('4', 'E')
                                        .Replace('5', 'F')
                                        .Replace('6', 'G')
                                        .Replace('7', 'H')
                                        .Replace('8', 'I')
                                        .Replace('9', 'J');
                return _retVal;
            }
            catch (Exception ex)
            {
                _logger.LogError("Error in ProductionController : ConvertInQRString() :: {Error}", ex.ToString());
                return String.Empty;
            }
        }

        #endregion

    }
}
