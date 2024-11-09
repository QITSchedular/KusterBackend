using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SAPbobsCOM;
using SAPbouiCOM;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Xml.Linq;
using WMS_UI_API.Common;
using WMS_UI_API.Models;
using WMS_UI_API.Services;
using DataTable = System.Data.DataTable;

namespace WMS_UI_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DraftGRPOController : ControllerBase
    {
        private string _ApplicationApiKey = string.Empty;
        private string _connection = string.Empty;
        private string _QIT_connection = string.Empty;
        private string _QIT_DB = string.Empty;
        private string _Query = string.Empty;

        SqlConnection SAPcon;
        SqlConnection QITcon;
        SqlDataAdapter oAdptr;
        private SqlCommand cmd;
        public Global objGlobal;

        public IConfiguration Configuration { get; }
        private readonly ILogger<DraftGRPOController> _logger;
        private readonly ISAPConnectionService _sapConnectionService;

        public DraftGRPOController(IConfiguration configuration, ILogger<DraftGRPOController> logger, ISAPConnectionService sapConnectionService)
        {
            if (objGlobal == null)
                objGlobal = new Global();
            _logger = logger;
            try
            {
                Configuration = configuration;
                _sapConnectionService = sapConnectionService;
                _ApplicationApiKey = Configuration["connectApp:ServiceApiKey"];
                _connection = Configuration["connectApp:ConnString"];
                _QIT_connection = Configuration["connectApp:QITConnString"];

                _QIT_DB = Configuration["QITDB"];
                Global.QIT_DB = _QIT_DB;
                Global.SAP_DB = Configuration["CompanyDB"];

                //objGlobal.gServer = Configuration["Server"];
                //objGlobal.gSqlVersion = Configuration["SQLVersion"];
                //objGlobal.gCompanyDB = Configuration["CompanyDB"];
                //objGlobal.gLicenseServer = Configuration["LicenseServer"];
                //objGlobal.gSAPUserName = Configuration["SAPUserName"];
                //objGlobal.gSAPPassword = Configuration["SAPPassword"];
                //objGlobal.gDBUserName = Configuration["DBUserName"];
                //objGlobal.gDBPassword = Configuration["DbPassword"];


            }
            catch (Exception ex)
            {
                _logger.LogError(" Error in DraftGRPOController :: {Error}" + ex.ToString());
            }
        }



        [HttpGet("GetGateInNos")]
        public async Task<ActionResult<IEnumerable<GateInNoList>>> GetGateInNos(int BranchID)
        {
            System.Data.DataTable dtData = new DataTable();
            try
            {
                _logger.LogInformation(" Calling DraftGRPOController : GetGateInNos() ");

                if (BranchID <= 0)
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Provide BranchID" });
                }

                List<GateInNoList> obj = new List<GateInNoList>();

                if (QITcon == null)
                    QITcon = new SqlConnection(_QIT_connection);

                _Query = @" 
                SELECT DISTINCT A.GateINNo FROM QIT_GateIN A
                WHERE A.GateInNo NOT IN 
                      ( SELECT A.GateInNo FROM QIT_QRStock_POToGRPO A 
                        INNER JOIN QIT_Trans_POToGRPO B ON A.TransSeq = B.TransSeq and B.DocEntry <> 0
                      ) AND ISNULL(A.BranchId, @bId) =  @bId ";

                _logger.LogInformation(" DraftGRPOController : GetGateInNos() Query : {q} ", _Query.ToString());
                QITcon.Open();
                oAdptr = new SqlDataAdapter(_Query, QITcon);
                oAdptr.SelectCommand.Parameters.AddWithValue("@bId", BranchID);
                oAdptr.Fill(dtData);
                QITcon.Close();

                if (dtData.Rows.Count > 0)
                {
                    dynamic arData = JsonConvert.SerializeObject(dtData);
                    obj = JsonConvert.DeserializeObject<List<GateInNoList>>(arData.ToString().Replace("~", " "));
                    return obj;
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "No data found" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(" Error in DraftGRPOController : GetGateInNos() :: {Error}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
            }
            finally { dtData = null; }
        }


        [HttpGet("GetPOList")]
        public async Task<ActionResult<IEnumerable<GetPOList>>> GetPOList(int BranchID, int GateInNo)
        {
            try
            {
                _logger.LogInformation(" Calling DraftGRPOController : GetPOList() ");

                #region Validation

                if (BranchID <= 0)
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Provide BranchID" });
                }

                if (GateInNo <= 0)
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Provide GateIn No" });
                }

                #region Check Gate IN exist or not 
                QITcon = new SqlConnection(_QIT_connection);
                DataTable dtGateIN = new DataTable();
                _Query = @" SELECT * FROM QIT_GateIN A WHERE A.GateInNo = @gateInNo and ISNULL(A.BranchID, @bId) = @bId and A.Canceled = 'N'  ";

                _logger.LogInformation(" DraftGRPOController : Check GateIN Query : {q} ", _Query.ToString());
                QITcon.Open();
                oAdptr = new SqlDataAdapter(_Query, QITcon);
                oAdptr.SelectCommand.Parameters.AddWithValue("@bId", BranchID);
                oAdptr.SelectCommand.Parameters.AddWithValue("@gateInNo", GateInNo);
                oAdptr.Fill(dtGateIN);
                QITcon.Close();

                if (dtGateIN.Rows.Count <= 0)
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "No such GateIn exist" });
                }
                #endregion

                #region Check Gate IN No

                dtGateIN = new System.Data.DataTable();
                QITcon = new SqlConnection(_QIT_connection);
                _Query = @" SELECT * FROM QIT_GateIN A where GateInNo = @gateInNo and ISNULL(A.BranchId, @bId) = @bId ";
                _logger.LogInformation(" DraftGRPOController : GateIN No Query : {q} ", _Query.ToString());
                QITcon.Open();
                oAdptr = new SqlDataAdapter(_Query, QITcon);
                oAdptr.SelectCommand.Parameters.AddWithValue("@bId", BranchID);
                oAdptr.SelectCommand.Parameters.AddWithValue("@gateInNo", GateInNo);
                oAdptr.Fill(dtGateIN);
                QITcon.Close();

                if (dtGateIN.Rows.Count <= 0)
                    return BadRequest(new
                    {
                        StatusCode = "400",
                        StatusMsg = "No such Gate IN exist"
                    });

                if (dtGateIN.Rows[0]["GateIn"].ToString().ToUpper() == "N" && dtGateIN.Rows[0]["VendorGateIn"].ToString().ToUpper() == "Y")
                    return BadRequest(new
                    {
                        StatusCode = "400",
                        StatusMsg = "This vendor GateIn is pending to be confirmed"
                    });

                #endregion

                #endregion


                #region check for Partial entry - SAP and QIT
                DataTable dtSAPEntryCount = new System.Data.DataTable();
                QITcon = new SqlConnection(_QIT_connection);
             
                _Query = @" 
                SELECT A.DocEntry, A.DocNum,
	                   ( SELECT count(Z1.DocEntry) from " + Global.SAP_DB + @".dbo.OPDN Z1 
                                INNER JOIN " + Global.SAP_DB + @".dbo.PDN1 Z2 ON Z1.DocEntry = Z2.DocEntry
		                 WHERE Z2.BaseRef = B.DocNum and Z2.BaseType = B.ObjType and Z2.BaseEntry = B.DocEntry and Z1.U_QIT_FromWeb = 'N'
	                   ) SAPEntryCount
                FROM
                (
                    SELECT DISTINCT A.DocEntry, A.DocNum FROM QIT_GateIN A
                    WHERE A.GateInNo = @gateInNo
                ) as A INNER JOIN " + Global.SAP_DB + @".dbo.OPOR B ON A.DocEntry = B.DocEntry
                ";
                
                _logger.LogInformation(" DraftGRPOController : GateIN No Query : {q} ", _Query.ToString());
                QITcon.Open();
                oAdptr = new SqlDataAdapter(_Query, QITcon); 
                oAdptr.SelectCommand.Parameters.AddWithValue("@gateInNo", GateInNo);
                oAdptr.Fill(dtSAPEntryCount);
                QITcon.Close();

                object objSAPEntryCount = dtSAPEntryCount.Compute("SUM(SAPEntryCount)", "");
                if (double.Parse(objSAPEntryCount.ToString()) > 0)
                {
                    for (int i = 0; i < dtSAPEntryCount.Rows.Count; i++)
                    {
                        if ( int.Parse(dtSAPEntryCount.Rows[i]["SAPEntryCount"].ToString()) > 0)
                            return BadRequest(new
                            {
                                StatusCode = "400",
                                StatusMsg = "Please do complete GRPO from SAP for the PO : " + dtSAPEntryCount.Rows[i]["DocNum"].ToString()
                            });
                    }                    
                }
                #endregion


                #region Remove unwanted data from Trans GRPO if exist

                if (QITcon == null)
                    QITcon = new SqlConnection(_QIT_connection);

                DataTable dtTransGRPO = new DataTable();

                _Query = @" 
                SELECT * FROM QIT_Trans_POToGRPO 
                WHERE DocEntry = 0 and DocNum = 0 and GateInNo = @gateInNo and ISNULL(BranchID, @bId) = @bId and 
                      GateInNo NOT IN (select GateInNo from QIT_QRStock_InvTrans B where B.FromObjType = '67' and B.ToObjType = '67')";

                _logger.LogInformation(" DraftGRPOController : Check empty GRPO entry : {q} ", _Query.ToString());
                QITcon.Open();
                oAdptr = new SqlDataAdapter(_Query, QITcon);
                oAdptr.SelectCommand.Parameters.AddWithValue("@bId", BranchID);
                oAdptr.SelectCommand.Parameters.AddWithValue("@gateInNo", GateInNo);
                oAdptr.Fill(dtTransGRPO);
                QITcon.Close();

                if (dtTransGRPO.Rows.Count > 0)
                {
                    if (QITcon == null)
                        QITcon = new SqlConnection(_QIT_connection);

                    _Query = @" 
                    DELETE FROM QIT_QRStock_InvTrans WHERE GateInNo = @gateInNo and ISNULL(BranchID, @bId) = @bId
                    DELETE FROM QIT_QRStock_POToGRPO WHERE GateInNo = @gateInNo and ISNULL(BranchID, @bId) = @bId
                    DELETE FROM QIT_Trans_POToGRPO WHERE GateInNo = @gateInNo and ISNULL(BranchID, @bId) = @bId
                    ";

                    _logger.LogInformation(" DraftGRPOController : Delete empty GRPO Query : {q} ", _Query.ToString());

                    cmd = new SqlCommand(_Query, QITcon);
                    cmd.Parameters.AddWithValue("@bId", BranchID);
                    cmd.Parameters.AddWithValue("@gateInNo", GateInNo);
                    QITcon.Open();
                    int intNum = cmd.ExecuteNonQuery();
                    QITcon.Close();

                }

                #endregion


                #region Apply validation of Delivery Date > 60

                DataTable dtDelDate60 = new System.Data.DataTable();
                QITcon = new SqlConnection(_QIT_connection);

                _Query = @" 
                select A.DocEntry, A.DocNum, A.ItemCode, A.LineNum, B.ShipDate, getDate() todayDate, 
                       ABS(DATEDIFF(d, getDate(), B.ShipDate)) diffDays,
	                   ISNULL((
	                      select 'Y' WHERE ABS(DATEDIFF(d, getDate(), B.ShipDate)) > 60 -- AND $[$3.0.string]='I'
	                   ), 'N') InValid
                FROM QIT_GateIN A
                     INNER JOIN " + Global.SAP_DB + @".dbo.POR1 B ON A.DocEntry = B.DocEntry and 
                           A.ItemCode collate SQL_Latin1_General_CP850_CI_AS = B.ItemCode and A.LineNum = B.LineNum
                WHERE A.GateInNo = @gateInNo
                ";

                _logger.LogInformation(" DraftGRPOController : Delivery Date > 60 Query : {q} ", _Query.ToString());
                QITcon.Open();
                oAdptr = new SqlDataAdapter(_Query, QITcon);
                oAdptr.SelectCommand.Parameters.AddWithValue("@gateInNo", GateInNo);
                oAdptr.Fill(dtDelDate60);
                QITcon.Close();

                DataRow[] filteredRows = dtDelDate60.Select("InValid = 'Y'");
                if (filteredRows.Count() > 0)
                {
                    foreach (var row in filteredRows)
                    {
                        return BadRequest(new
                        {
                            StatusCode = "400",
                            StatusMsg = " GRPO Del. Date greater than 60 days than Posting Date : " + Environment.NewLine + 
                                        " PO : " + row["DocNum"] + Environment.NewLine +
                                        " ItemCode : " + row["ItemCode"]
                        }); 
                    }
                } 

                #endregion


                #region Check GRPO already done for GateInNo

                if (QITcon == null)
                    QITcon = new SqlConnection(_QIT_connection);
                DataTable dtGRPO = new DataTable();

                _Query = @" 
                SELECT * FROM QIT_Trans_POToGRPO A 
                WHERE A.GateInNo = @gateInNo and ISNULL(A.BranchID, @bId) = @bId ";

                _logger.LogInformation(" DraftGRPOController : Check GRPO already done Query : {q} ", _Query.ToString());
                QITcon.Open();
                oAdptr = new SqlDataAdapter(_Query, QITcon);
                oAdptr.SelectCommand.Parameters.AddWithValue("@bId", BranchID);
                oAdptr.SelectCommand.Parameters.AddWithValue("@gateInNo", GateInNo);
                oAdptr.Fill(dtGRPO);
                QITcon.Close();

                if (dtGRPO.Rows.Count > 0)
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "GRPO is already done for this GateIn No : " + GateInNo });
                }
                dtGRPO = null;
                #endregion

                List<GetPOList> obj = new List<GetPOList>();
                System.Data.DataTable dtData = new DataTable();

                if (QITcon == null)
                    QITcon = new SqlConnection(_QIT_connection);

                _Query = @" 
                SELECT A.DocEntry, A.DocNum, B.Series, C.SeriesName, B.DocDate, B.CardCode, B.CardName, B.NumAtCard, -- D.QRCodeID,
	                   ( select CAST(SUM(RecQty) as numeric(19,3)) FROM QIT_GateIN Z 
                         where Z.GateInNo = @gateInNo and Z.DocEntry = A.DocEntry) RecQty,
	                   case when (select count(1) FROM " + Global.SAP_DB + @".dbo.POR3 WHERE DocEntry = A.DocEntry) > 0 then 'Y' else 'N' end FreightApplied,
                       ( select distinct Z.GRPOVendorRefNo from QIT_GateIN Z where Z.GateInNo = @gateInNo) GRPOVendorRefNo,
                       ( select distinct Z.GRPODocDate from QIT_GateIN Z where Z.GateInNo = @gateInNo) GRPODocDate,
	                   ( select distinct Z.VehicleNo from QIT_GateIN Z where Z.GateInNo = @gateInNo) VehicleNo,
	                   ( select distinct Z.TransporterCode from QIT_GateIN Z where Z.GateInNo = @gateInNo) Transporter,
	                   ( select distinct Z.LRNo from QIT_GateIN Z where Z.GateInNo = @gateInNo) LRNo,
	                   ( select distinct Z.LRDate from QIT_GateIN Z where Z.GateInNo = @gateInNo) LRDate
                FROM
                (
	                SELECT DISTINCT DocEntry, DocNum FROM QIT_GateIN A
	                WHERE A.GateInNo = @gateInNo and ISNULL(A.BranchID, @bId) = @bId
                ) as A
                INNER JOIN " + Global.SAP_DB + @".dbo.OPOR B ON A.DocEntry = B.DocEntry and ----B.DocStatus = 'O' and 
                          B.CANCELED = 'N' and ISNULL(B.BPLId, @bId) = @bId
                INNER JOIN " + Global.SAP_DB + @".dbo.NNM1 C ON C.Series = B.Series 
                /*INNER JOIN QIT_QR_Header D ON D.DocEntry = A.DocEntry and D.ObjType collate SQL_Latin1_General_CP850_CI_AS = B.ObjType and D.Series = B.Series
                and D.BranchID = B.BPLId*/";

                _logger.LogInformation(" DraftGRPOController : GetPOList() Query : {q} ", _Query.ToString());
                QITcon.Open();
                oAdptr = new SqlDataAdapter(_Query, QITcon);
                oAdptr.SelectCommand.Parameters.AddWithValue("@bId", BranchID);
                oAdptr.SelectCommand.Parameters.AddWithValue("@gateInNo", GateInNo);
                oAdptr.Fill(dtData);
                QITcon.Close();

                if (dtData.Rows.Count > 0)
                {
                    dynamic arData = JsonConvert.SerializeObject(dtData);
                    dtData = null;
                    obj = JsonConvert.DeserializeObject<List<GetPOList>>(arData.ToString().Replace("~", " "));
                    return obj;
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "No data found" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(" Error in DraftGRPOController : GetPOList() :: {Error}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
            }
        }


        [HttpPost("ValidateItemQR")]
        public async Task<ActionResult<IEnumerable<ValidQRData>>> ValidateItemQR(ValidateItemQR payload)
        {
            try
            {
                _logger.LogInformation(" Calling DraftGRPOController : ValidateItemQR() ");

                #region Validation

                if (payload.BranchID <= 0 || payload.BranchID == null)
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Provide Branch" });
                }

                if (payload.GateInNo <= 0 || payload.GateInNo == null)
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Provide GateIn No" });
                }

                #region Check Gate IN exist or not 
                QITcon = new SqlConnection(_QIT_connection);
                DataTable dtGateIN = new DataTable();
                _Query = @" 
                SELECT * FROM QIT_GateIN A 
                WHERE A.GateInNo = @gateInNo and ISNULL(A.BranchID, @bId) = @bId and A.Canceled = 'N' ";

                _logger.LogInformation(" DraftGRPOController : ValidateItemQR : Check GateIN Query : {q} ", _Query.ToString());
                QITcon.Open();
                oAdptr = new SqlDataAdapter(_Query, QITcon);
                oAdptr.SelectCommand.Parameters.AddWithValue("@bId", payload.BranchID);
                oAdptr.SelectCommand.Parameters.AddWithValue("@gateInNo", payload.GateInNo);
                oAdptr.Fill(dtGateIN);
                QITcon.Close();

                if (dtGateIN.Rows.Count <= 0)
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "No such GateIn exist" });
                }
                #endregion

                #region Check Detail QR
                QITcon = new SqlConnection(_QIT_connection);
                DataTable dtQR = new DataTable();
                _Query = " SELECT * FROM QIT_QR_Detail A WHERE A.QRCodeID = @detQRCodeID and A.GateInNo = @gateInNo and A.Canceled = 'N' ";
                _logger.LogInformation(" DraftGRPOController : Check QR Query : {q} ", _Query.ToString());
                oAdptr = new SqlDataAdapter(_Query, QITcon);
                oAdptr.SelectCommand.Parameters.AddWithValue("@detQRCodeID", payload.DetailQRCodeID.Replace(" ", "~"));
                oAdptr.SelectCommand.Parameters.AddWithValue("@gateInNo", payload.GateInNo);
                QITcon.Open();
                oAdptr.Fill(dtQR);
                QITcon.Close();

                if (dtQR.Rows.Count <= 0)
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = " No such QR exist for this GateIn !!!" });
                }

                #endregion

                #region Check GRPO done or not
                QITcon = new SqlConnection(_QIT_connection);
                DataTable dtGRPO = new DataTable();
                _Query = @" 
                SELECT * FROM QIT_QRStock_POToGRPO 
                WHERE QRCodeID = @detQRCodeID and ISNULL(BranchId, @bId) = @bId ";
                _logger.LogInformation(" DraftGRPOController :  Query : {q} ", _Query.ToString());
                oAdptr = new SqlDataAdapter(_Query, QITcon);
                oAdptr.SelectCommand.Parameters.AddWithValue("@bId", payload.BranchID);
                oAdptr.SelectCommand.Parameters.AddWithValue("@detQRCodeID", payload.DetailQRCodeID.Replace(" ", "~"));
                QITcon.Open();
                oAdptr.Fill(dtGRPO);
                QITcon.Close();

                if (dtGRPO.Rows.Count > 0)
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "GRPO is already done for : " + payload.DetailQRCodeID.Replace("~", " ") });
                }

                #endregion

                if (payload.PODocEntry <= 0 || payload.PODocEntry == null)
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Provide Purchase Order" });
                }

                #region Check PO exist or not 
                QITcon = new SqlConnection(_QIT_connection);
                DataTable dtPO = new DataTable();
                _Query = @" SELECT * FROM QIT_GateIN A WHERE A.DocEntry = @docEntry and ISNULL(A.BranchID, @bId) = @bId and A.Canceled = 'N' ";

                _logger.LogInformation(" DraftGRPOController : ValidateItemQR : Check PO Query : {q} ", _Query.ToString());
                QITcon.Open();
                oAdptr = new SqlDataAdapter(_Query, QITcon);
                oAdptr.SelectCommand.Parameters.AddWithValue("@bId", payload.BranchID);
                oAdptr.SelectCommand.Parameters.AddWithValue("@docEntry", payload.PODocEntry);
                oAdptr.Fill(dtPO);
                QITcon.Close();

                if (dtPO.Rows.Count <= 0)
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "No such Purchase Order exist" });
                }
                #endregion


                #endregion

                List<ValidQRData> obj = new List<ValidQRData>();
                DataTable dtData = new DataTable();

                if (QITcon == null)
                    QITcon = new SqlConnection(_QIT_connection);

                _Query = @" 
                SELECT A.DocEntry, D.LineNum, A.DocNum, A.DocDate,  F.CardCode, F.CardName, A.QRCodeID HeaderQRCodeID, B.GateInNo, 
                       D.RecDate GateInDate,
                       B.ItemCode, C.ItemName, C.ItemMngBy, B.QRMngBy, B.QRCodeID DetailQRCodeID, B.BatchSerialNo, 
                       CAST(B.Qty as numeric(19,3)) Qty,
                       D.Project, B.Remark, CAST(E.Quantity as numeric(19,3)) OrderQty, CAST(D.RecQty as numeric(19,3)) RecQty , D.WhsCode, D.UoMCode, -- , E.U_QA
                       case when G.U_QA in (1,3) then 'Y' else 'N' end QARequired,
                       D.LRNo, D.LRDate, D.VehicleNo, D.TransporterCode, D.GRPOVendorRefNo, D.GRPODocDate, 
                       E.TaxCode, E.Price, D.ToleranceApplied TolApplied, G.NumInBuy PurchaseUnit
                FROM   QIT_QR_Header A 
                       INNER JOIN QIT_QR_Detail B on A.HeaderSrNo = B.HeaderSrNo
                       INNER JOIN QIT_Item_Master C on B.ItemCode = C.ItemCode
                       INNER JOIN QIT_GateIN D on D.GateInNo = B.GateInNo  and D.ItemCode = B.ItemCode AND B.LineNum = D.LineNum
                       INNER JOIN " + Global.SAP_DB + @".dbo.POR1 E on E.DocEntry = A.DocEntry AND 
                                  E.ObjType collate SQL_Latin1_General_CP1_CI_AS = A.ObjType AND
                                  E.ItemCode collate SQL_Latin1_General_CP1_CI_AS= B.ItemCode AND E.LineNum = D.LineNum
                       INNER JOIN " + Global.SAP_DB + @".dbo.OPOR F ON E.DocEntry = F.DocEntry
                       INNER JOIN " + Global.SAP_DB + @".dbo.OITM G ON G.ItemCode collate SQL_Latin1_General_CP1_CI_AS = C.ItemCode
                WHERE B.QRCodeID = @dQR and D.Canceled = 'N' and B.Canceled = 'N' and
                      D.DocEntry = @docEntry and ISNULL(A.BranchID, @bId) = @bId ";

                _logger.LogInformation(" DraftGRPOController : ValidateItemQR() Query : {q} ", _Query.ToString());
                QITcon.Open();
                oAdptr = new SqlDataAdapter(_Query, QITcon);
                oAdptr.SelectCommand.Parameters.AddWithValue("@bId", payload.BranchID);
                oAdptr.SelectCommand.Parameters.AddWithValue("@docEntry", payload.PODocEntry);
                oAdptr.SelectCommand.Parameters.AddWithValue("@dQR", payload.DetailQRCodeID.Replace(" ", "~"));
                oAdptr.Fill(dtData);
                QITcon.Close();

                if (dtData.Rows.Count > 0)
                {
                    dynamic arData = JsonConvert.SerializeObject(dtData);
                    dtData = null;
                    obj = JsonConvert.DeserializeObject<List<ValidQRData>>(arData.ToString().Replace("~", " "));
                    return obj;
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "No data found" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(" Error in DraftGRPOController : ValidateItemQR() :: {Error}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
            }
        }


        [HttpPost("CreateDraftGRPO")]
        public async Task<IActionResult> CreateDraftGRPO([FromBody] DraftGRPO payload)
        {
            if (objGlobal == null)
                objGlobal = new Global();

            string p_ErrorMsg = string.Empty;
            string _IsSaved = "N";
            Object _TransSeq = 0;
            Object _QRTransSeq = 0;
            Object _QRTransSeqInvTrans = 0;

            try
            {
                _logger.LogInformation(" Calling DraftGRPOController : CreateDraftGRPO() for GateInNo : " + payload.GateInNo);

                if (payload != null)
                {
                    if (((WMS_UI_API.Services.SAPConnectionService)_sapConnectionService).oComp.Connected)
                    {

                        #region Validate QR Items - must do GRPO of all QRs of GateINNo
                        DataTable dtQRData = new DataTable();

                        if (QITcon == null)
                            QITcon = new SqlConnection(_QIT_connection);

                        _Query = @" SELECT * FROM QIT_QR_Detail WHERE GateInNo = @gateInNo and ISNULL(BranchID, @bID) = @bID ";
                        _logger.LogInformation(" DraftGRPOController for GateInNo : " + payload.GateInNo + "  Get QR data Query : {q} ", _Query.ToString());
                        QITcon.Open();
                        oAdptr = new SqlDataAdapter(_Query, QITcon);
                        oAdptr.SelectCommand.Parameters.AddWithValue("@gateInNo", payload.GateInNo);
                        oAdptr.SelectCommand.Parameters.AddWithValue("@bID", payload.BranchID);
                        oAdptr.Fill(dtQRData);
                        QITcon.Close();

                        if (dtQRData.Rows.Count > 0)
                        {
                            DataTable batchSerialTable = new DataTable();
                            batchSerialTable = ConvertListToDataTable(payload.poDet.SelectMany(item => item.grpoDet).SelectMany(det => det.grpoBatchSerial).ToList());

                            if (batchSerialTable.Rows.Count > 0)
                            {
                                var missingOrMismatchedQRCodes =
                                from rowA in dtQRData.AsEnumerable()
                                join rowB in batchSerialTable.AsEnumerable() on rowA.Field<string>("QRCodeID") equals rowB.Field<string>("DetailQRCodeID")
                                .Replace(" ", "~") into joined
                                from j in joined.DefaultIfEmpty()

                                let qtyA = Math.Round(Convert.ToDouble(rowA.Field<decimal>("Qty")), 3)
                                let qtyB = (j == null) ? 0 : Math.Round(Convert.ToDouble((j.Field<string>("QTY") != null) ? j.Field<string>("QTY") : 0), 3)

                                group new { qtyA, qtyB } by new
                                {
                                    QRCodeID = rowA.Field<string>("QRCodeID"),
                                    QtyMismatch = (j != null && qtyA != qtyB) || j == null
                                } into grouped
                                select new
                                {
                                    grouped.Key.QRCodeID,
                                    QtyMismatch = grouped.Key.QtyMismatch
                                };

                                var sumQty = batchSerialTable.AsEnumerable()
                                .Select(row => double.TryParse(row.Field<string>("Qty"), out double parsedValue) ? parsedValue : 0)
                                .Sum();

                                foreach (var code in missingOrMismatchedQRCodes)
                                {
                                    if (code.QtyMismatch)
                                    {
                                        return BadRequest(new
                                        {
                                            StatusCode = "400",
                                            IsSaved = _IsSaved,
                                            StatusMsg = "Partial GRPO is not allowed." + Environment.NewLine +
                                                        "Gate IN Qty = " + dtQRData.Compute("sum(Qty)", "").ToString() + Environment.NewLine +
                                                        "You are providing = " + sumQty.ToString()
                                        });
                                    }
                                }
                            }
                            else
                            {
                                return BadRequest(new
                                {
                                    StatusCode = "400",
                                    IsSaved = _IsSaved,
                                    StatusMsg = "Provide required payload for GRPO"
                                });
                            }
                        }
                        else
                        {
                            return BadRequest(new
                            {
                                StatusCode = "400",
                                IsSaved = _IsSaved,
                                StatusMsg = "No such Gate IN exist."
                            });
                        }

                        dtQRData = null;
                        #endregion

                        #region Get Config
                        System.Data.DataTable dtConfig = new System.Data.DataTable();
                        if (QITcon == null)
                            QITcon = new SqlConnection(_QIT_connection);
                        _Query = @" SELECT * FROM QIT_Config_Master WHERE ISNULL(BranchID, @bId) = @bId ";
                        QITcon.Open();
                        oAdptr = new SqlDataAdapter(_Query, QITcon);
                        oAdptr.SelectCommand.Parameters.AddWithValue("@bId", payload.BranchID);
                        oAdptr.Fill(dtConfig);
                        QITcon.Close();
                        #endregion

                        if (1 == 1)
                        {
                            _logger.LogInformation(" DraftGRPOController for GateInNo : " + payload.GateInNo + "  SAP connected");

                            int _TotalItemCount = payload.poDet.SelectMany(item => item.grpoDet).Count();
                            int _SuccessCount = 0, _DetSuccessCount = 0;
                            int _FailCount = 0, _DetFailCount = 0;

                            #region Get TransSeq No - PO to GRPO
                            if (QITcon == null)
                                QITcon = new SqlConnection(_QIT_connection);
                            _Query = @" SELECT ISNULL(max(TransSeq),0) + 1 FROM QIT_Trans_POToGRPO A  ";
                            _logger.LogInformation(" DraftGRPOController : for GateInNo : " + payload.GateInNo + "  GetTransSeqNo Query : {q} ", _Query.ToString());

                            cmd = new SqlCommand(_Query, QITcon);
                            QITcon.Open();
                            _TransSeq = cmd.ExecuteScalar();
                            QITcon.Close();
                            #endregion

                            #region Get QR TransSeq No - PO to GRPO
                            if (QITcon == null)
                                QITcon = new SqlConnection(_QIT_connection);
                            _Query = @" SELECT ISNULL(max(QRTransSeq),0) + 1 FROM QIT_QRStock_POToGRPO A  ";
                            _logger.LogInformation(" DraftGRPOController : for GateInNo : " + payload.GateInNo + "  GetQRTransSeqNo(PO to GRPO) Query : {q} ", _Query.ToString());

                            cmd = new SqlCommand(_Query, QITcon);
                            QITcon.Open();
                            _QRTransSeq = cmd.ExecuteScalar();
                            QITcon.Close();
                            #endregion

                            #region Get QR TransSeq No - Inventory Transfer
                            if (QITcon == null)
                                QITcon = new SqlConnection(_QIT_connection);
                            _Query = @" SELECT ISNULL(max(QRTransSeq),0) + 1 FROM QIT_QRStock_InvTrans A  ";
                            _logger.LogInformation(" DraftGRPOController : for GateInNo : " + payload.GateInNo + "  GetQRTransSeqNo(Inv Trans) Query : {q} ", _Query.ToString());

                            cmd = new SqlCommand(_Query, QITcon);
                            QITcon.Open();
                            _QRTransSeqInvTrans = cmd.ExecuteScalar();
                            QITcon.Close();
                            #endregion

                            foreach (var po in payload.poDet)
                            {
                                foreach (var item in po.grpoDet)
                                {
                                    #region Insert in Transaction Table - PO to GRPO
                                    if (QITcon == null)
                                        QITcon = new SqlConnection(_QIT_connection);

                                    _Query = @"
                                 INSERT INTO QIT_Trans_POToGRPO
                                 (BranchID, TransId, TransSeq, FromObjType, ToObjType, GateInNo, BaseDocEntry, BaseDocNum, DocEntry, DocNum,  
                                  ItemCode,  Qty, UoMCode, FromWhs, ToWhs, Remark
                                 ) 
                                 VALUES 
                                 ( @bID, (SELECT ISNULL(max(TransId),0) + 1 FROM QIT_Trans_POToGRPO), @transSeq, '22', '20', @gateInNo, 
                                   @baseDocEntry, @baseDocNum, @docEntry, @docNum, @itemCode, @qty, @uom, @fromWhs, @toWhs, @remark 
                                 )
                                ";

                                    _logger.LogInformation(" DraftGRPOController : for GateInNo : " + payload.GateInNo + " PO to GRPO Transfer Table Query : {q} ", _Query.ToString());

                                    cmd = new SqlCommand(_Query, QITcon);
                                    cmd.Parameters.AddWithValue("@bID", payload.BranchID);
                                    cmd.Parameters.AddWithValue("@transSeq", _TransSeq);
                                    cmd.Parameters.AddWithValue("@gateInNo", payload.GateInNo);
                                    cmd.Parameters.AddWithValue("@baseDocEntry", po.DocEntry);
                                    cmd.Parameters.AddWithValue("@baseDocNum", po.DocNum);
                                    cmd.Parameters.AddWithValue("@docEntry", 0);
                                    cmd.Parameters.AddWithValue("@docNum", 0);
                                    cmd.Parameters.AddWithValue("@itemCode", item.ItemCode);
                                    cmd.Parameters.AddWithValue("@qty", Convert.ToDouble(item.Qty));
                                    cmd.Parameters.AddWithValue("@uom", item.UoMCode);
                                    cmd.Parameters.AddWithValue("@fromWhs", item.FromWhs);
                                    cmd.Parameters.AddWithValue("@toWhs", item.QARequired.ToUpper() == "Y" ? payload.QAWhsCode : payload.NonQAWhsCode);
                                    cmd.Parameters.AddWithValue("@remark", payload.Comments);
                                    int intNum = 0;

                                    QITcon.Open();
                                    intNum = cmd.ExecuteNonQuery();
                                    QITcon.Close();

                                    if (intNum > 0)
                                        _SuccessCount = _SuccessCount + 1;
                                    else
                                        _FailCount = _FailCount + 1;

                                    #endregion

                                    #region Insert in QR Stock Table - PO to GRPO
                                    foreach (var itemDet in item.grpoBatchSerial)
                                    {
                                        if (QITcon == null)
                                            QITcon = new SqlConnection(_QIT_connection);

                                        _Query = @"
                                    INSERT INTO QIT_QRStock_POToGRPO 
                                    (       BranchID, TransId, TransSeq, QRTransSeq, QRCodeID, GateInNo, ItemCode, BatchSerialNo, 
                                            FromObjType, ToObjType, FromWhs, ToWhs, FromBinAbsEntry, ToBinAbsEntry, Qty
                                    ) 
                                    VALUES 
                                    (   @bID, (SELECT ISNULL(max(TransId),0) + 1 FROM QIT_QRStock_POToGRPO), 
                                        @transSeq, @qrtransSeq, @qrCodeID, @gateInNo, @itemCode, @bsNo, '22', '20', @fromWhs, @toWhs, @fromBin, @toBin, @qty   
                                    )

                                    INSERT INTO QIT_QRStock_InvTrans
                                    (   BranchID, TransId,  QRTransSeq, TransSeq, QRCodeID, GateInNo, ItemCode,  BatchSerialNo,
                                        FromObjType, ToObjType, FromWhs, ToWhs, FromBinAbsEntry, ToBinAbsEntry, Qty) 
                                    VALUES 
                                    (   @bID, (SELECT ISNULL(max(TransId),0) + 1 FROM QIT_QRStock_InvTrans), 
                                        @qrtransSeqInvTrans, @transSeq, @qrCodeID, @gateInNo, @itemCode,  @bsNo, '22', '67', 
                                        @fromWhs, @toWhs, @fromBin, @toBin, @qty   
                                    )
                                    ";

                                        _logger.LogInformation("DraftGRPOController : For GateInNo : " + payload.GateInNo + " QR : " + itemDet.DetailQRCodeID.Replace("~", " ") + " Transfer Table Query : {q} ", _Query.ToString());

                                        cmd = new SqlCommand(_Query, QITcon);
                                        cmd.Parameters.AddWithValue("@bID", payload.BranchID);
                                        cmd.Parameters.AddWithValue("@qrtransSeq", _QRTransSeq);
                                        cmd.Parameters.AddWithValue("@transSeq", _TransSeq);
                                        cmd.Parameters.AddWithValue("@qrtransSeqInvTrans", _QRTransSeqInvTrans);
                                        cmd.Parameters.AddWithValue("@qrCodeID", itemDet.DetailQRCodeID.Replace(" ", "~"));
                                        cmd.Parameters.AddWithValue("@gateInNo", payload.GateInNo);
                                        cmd.Parameters.AddWithValue("@itemCode", item.ItemCode);
                                        cmd.Parameters.AddWithValue("@bsNo", itemDet.BatchSerialNo);
                                        cmd.Parameters.AddWithValue("@fromWhs", item.FromWhs);
                                        cmd.Parameters.AddWithValue("@toWhs", item.QARequired.ToUpper() == "Y" ? payload.QAWhsCode : payload.NonQAWhsCode);
                                        cmd.Parameters.AddWithValue("@fromBin", 0);
                                        cmd.Parameters.AddWithValue("@toBin", item.QARequired.ToUpper() == "Y" ? payload.QABinAbsEntry : payload.NonQABinAbsEntry);
                                        cmd.Parameters.AddWithValue("@qty", itemDet.Qty);

                                        intNum = 0;
                                        try
                                        {
                                            QITcon.Open();
                                            intNum = cmd.ExecuteNonQuery();
                                            QITcon.Close();
                                        }
                                        catch (Exception ex1)
                                        {
                                            this.DeleteTransactionPOtoGRPO(_TransSeq.ToString());
                                            this.DeleteQRStockDetPOtoGRPO(_QRTransSeq.ToString());
                                            this.DeleteQRStockDetInvTrans(_QRTransSeqInvTrans.ToString());
                                            if (ex1.ToString().ToLower().Contains("ub_detailqrcodeid"))
                                            {
                                                _logger.LogError("DraftGRPOController : For GateInNo : " + payload.GateInNo + " GRPO is already done for :  " + itemDet.DetailQRCodeID.Replace("~", " "));
                                                return BadRequest(new
                                                {
                                                    StatusCode = "400",
                                                    IsSaved = "N",
                                                    TransSeq = _TransSeq,
                                                    StatusMsg = "GRPO is already done for : " + itemDet.DetailQRCodeID.Replace("~", " ")
                                                });
                                            }
                                            else
                                            {
                                                return BadRequest(new
                                                {
                                                    StatusCode = "400",
                                                    IsSaved = "N",
                                                    TransSeq = _TransSeq,
                                                    StatusMsg = ex1.Message.ToString()
                                                });
                                            }
                                        }


                                        if (intNum > 0)
                                            _DetSuccessCount = _DetSuccessCount + 1;
                                        else
                                            _DetFailCount = _DetFailCount + 1;
                                    }

                                    #endregion
                                }
                            }


                            if (_TotalItemCount == _SuccessCount && _SuccessCount > 0)
                            {
                                _logger.LogInformation(" Data stored in QIT DB : CreateDraftGRPO() for GateInNo : " + payload.GateInNo);

                                #region Gate IN Details

                                System.Data.DataTable dtGateIN = new System.Data.DataTable();

                                if (QITcon == null)
                                    QITcon = new SqlConnection(_QIT_connection);
                                _Query = @" SELECT * FROM QIT_GateIN WHERE GateInNo = @gNo ";
                                QITcon.Open();
                                oAdptr = new SqlDataAdapter(_Query, QITcon);
                                oAdptr.SelectCommand.Parameters.AddWithValue("@gNo", payload.GateInNo);
                                oAdptr.Fill(dtGateIN);
                                QITcon.Close();

                                #endregion

                                int _Line = 0;

                                #region set GRPO Header level data
                                
                                Documents grpo = (Documents)((WMS_UI_API.Services.SAPConnectionService)_sapConnectionService).oComp.GetBusinessObject(BoObjectTypes.oPurchaseDeliveryNotes);

                                grpo.DocObjectCode = BoObjectTypes.oPurchaseDeliveryNotes;

                                grpo.Series = payload.Series;
                                grpo.CardCode = payload.CardCode;
                                grpo.NumAtCard = dtGateIN.Rows[0]["GRPOVendorRefNo"].ToString();
                                grpo.TaxDate = (DateTime)dtGateIN.Rows[0]["GRPODocDate"]; // GRPODocDate from Gate IN table
                                grpo.DocDate = DateTime.Now;

                                grpo.Comments = payload.Comments;
                                grpo.BPL_IDAssignedToInvoice = payload.BranchID;

                                grpo.UserFields.Fields.Item("U_QIT_FromWeb").Value = "Y";
                                grpo.UserFields.Fields.Item("U_GE").Value = payload.GateInNo.ToString();
                                grpo.UserFields.Fields.Item("U_GEDate").Value = dtGateIN.Rows[0]["RecDate"];
                                grpo.UserFields.Fields.Item("U_Veh_Number").Value = dtGateIN.Rows[0]["VehicleNo"];
                                grpo.UserFields.Fields.Item("U_Trans").Value = dtGateIN.Rows[0]["TransporterCode"];
                                grpo.UserFields.Fields.Item("U_LRNo").Value = dtGateIN.Rows[0]["LRNo"];
                                grpo.UserFields.Fields.Item("U_LRDate").Value = dtGateIN.Rows[0]["LRDate"].ToString();

                                #endregion

                                foreach (var po in payload.poDet)
                                { 
                                    SAPbobsCOM.Documents oPurchaseOrder = (SAPbobsCOM.Documents)((WMS_UI_API.Services.SAPConnectionService)_sapConnectionService).oComp.GetBusinessObject(SAPbobsCOM.BoObjectTypes.oPurchaseOrders);

                                    if (oPurchaseOrder.GetByKey(po.DocEntry))
                                    {

                                        DateTime dtDeliveryDate;
                                        if (dtConfig.Rows[0]["GRPODeliveryDate"].ToString().ToUpper() == "P")
                                            dtDeliveryDate = oPurchaseOrder.DocDueDate;
                                        else if (dtConfig.Rows[0]["GRPODeliveryDate"].ToString().ToUpper() == "G")
                                            dtDeliveryDate = (DateTime)dtGateIN.Rows[0]["RecDate"];
                                        else
                                            dtDeliveryDate = DateTime.Now;

                                        grpo.DocDueDate = dtDeliveryDate; // DateTime.Now;
                                                                          //grpo.UserFields.Fields.Item("U_SubconPO").Value = oPurchaseOrder.UserFields.Fields.Item("U_SubconPO").Value;

                                        foreach (var item in po.grpoDet)
                                        {
                                            //grpo.Lines.ItemCode = item.ItemCode;
                                            grpo.Lines.Quantity = Convert.ToDouble(item.Qty);
                                            grpo.Lines.Price = double.Parse(item.Price);
                                            grpo.Lines.TaxCode = item.TaxCode;
                                            grpo.Lines.BaseType = 22;
                                            grpo.Lines.BaseEntry = po.DocEntry;
                                            grpo.Lines.BaseLine = int.Parse(item.LineNum);
                                            grpo.Lines.WarehouseCode = item.QARequired.ToUpper() == "Y" ? payload.QAWhsCode : payload.NonQAWhsCode;

                                            grpo.Lines.MeasureUnit = item.UoMCode;

                                            if (item.TolApplied.ToUpper() == "Y")
                                                grpo.Lines.UserFields.Fields.Item("U_QtyTolPer").Value = 5;
                                            //if (oPurchaseOrder.UserFields.Fields.Item("U_SubconPO").Value.ToString() == "Y")
                                            //    grpo.Lines.UserFields.Fields.Item("U_AObj").Value = "SubCon";

                                            // Check if the item is a Serial item
                                            if (item.ItemMngBy.ToLower() == "s")
                                            {
                                                int i = 0;
                                                foreach (var serial in item.grpoBatchSerial)
                                                {
                                                    if (!string.IsNullOrEmpty(serial.BatchSerialNo))
                                                    {
                                                        grpo.Lines.SerialNumbers.SetCurrentLine(i);
                                                        grpo.Lines.BatchNumbers.BaseLineNumber = _Line;
                                                        grpo.Lines.SerialNumbers.InternalSerialNumber = serial.BatchSerialNo;
                                                        grpo.Lines.SerialNumbers.ManufacturerSerialNumber = serial.BatchSerialNo;
                                                        grpo.Lines.SerialNumbers.Quantity = Convert.ToDouble(serial.Qty) * Convert.ToDouble(item.PurchaseUnit);

                                                        grpo.Lines.SerialNumbers.Add();

                                                        if ((item.QARequired.ToLower() == "y" && payload.QABinAbsEntry > 0) ||
                                                            (item.QARequired.ToLower() == "n" && payload.NonQABinAbsEntry > 0))
                                                        {
                                                            int _absEntry = item.QARequired.ToLower() == "y" ? payload.QABinAbsEntry : payload.NonQABinAbsEntry;
                                                            grpo.Lines.BinAllocations.BinAbsEntry = _absEntry;
                                                            grpo.Lines.BinAllocations.Quantity = Convert.ToDouble(serial.Qty) * Convert.ToDouble(item.PurchaseUnit);
                                                            grpo.Lines.BinAllocations.BaseLineNumber = _Line;
                                                            grpo.Lines.BinAllocations.SerialAndBatchNumbersBaseLine = i;
                                                            grpo.Lines.BinAllocations.Add();
                                                        }
                                                        i = i + 1;
                                                    }
                                                }
                                            }
                                            // Check if the item is a Batch item
                                            else if (item.ItemMngBy.ToLower() == "b")
                                            {
                                                int _batchLine = 0;
                                                foreach (var batch in item.grpoBatchSerial)
                                                {
                                                    if (!string.IsNullOrEmpty(batch.BatchSerialNo))
                                                    {
                                                        grpo.Lines.BatchNumbers.BaseLineNumber = _Line;
                                                        grpo.Lines.BatchNumbers.BatchNumber = batch.BatchSerialNo;
                                                        grpo.Lines.BatchNumbers.Quantity = Convert.ToDouble(batch.Qty) * Convert.ToDouble(item.PurchaseUnit);
                                                        grpo.Lines.BatchNumbers.Add();

                                                        if ((item.QARequired.ToLower() == "y" && payload.QABinAbsEntry > 0) ||
                                                            (item.QARequired.ToLower() == "n" && payload.NonQABinAbsEntry > 0))
                                                        {
                                                            int _absEntry = item.QARequired.ToLower() == "y" ? payload.QABinAbsEntry : payload.NonQABinAbsEntry;
                                                            grpo.Lines.BinAllocations.BinAbsEntry = _absEntry;
                                                            grpo.Lines.BinAllocations.Quantity = Convert.ToDouble(batch.Qty) * Convert.ToDouble(item.PurchaseUnit);
                                                            grpo.Lines.BinAllocations.BaseLineNumber = _Line;
                                                            grpo.Lines.BinAllocations.SerialAndBatchNumbersBaseLine = _batchLine;
                                                            grpo.Lines.BinAllocations.Add();
                                                        }
                                                        _batchLine = _batchLine + 1;
                                                    }
                                                }
                                            }
                                            grpo.Lines.Add();
                                            _Line = _Line + 1;


                                        }
                                    }
                                    else
                                    {
                                        this.DeleteTransactionPOtoGRPO(_TransSeq.ToString());
                                        this.DeleteQRStockDetPOtoGRPO(_QRTransSeq.ToString());
                                        this.DeleteQRStockDetInvTrans(_QRTransSeqInvTrans.ToString());
                                        return BadRequest(new
                                        {
                                            StatusCode = "400",
                                            IsSaved = "N",
                                            TransSeq = _TransSeq,
                                            StatusMsg = "No such PO exist"
                                        });
                                    }
                                }

                                foreach (var fr in payload.FreightDet)
                                {
                                    int expenseCode = fr.ExpnsCode;
                                    double expenseAmount = fr.Amount;

                                    grpo.Expenses.ExpenseCode = expenseCode;
                                    grpo.Expenses.LineTotal = expenseAmount;
                                    grpo.Expenses.Add();

                                }

                                //for (int i = 0; i < dtF.Rows.Count; i++)
                                //{
                                //    int expenseCode = (int)dtF.Rows[i]["ExpenseCode"];
                                //    double expenseAmount = (double)dtF.Rows[i]["ExpenseAmount"];

                                //    grpo.Expenses.ExpenseCode = expenseCode;
                                //    grpo.Expenses.LineTotal = expenseAmount;
                                //    grpo.Expenses.Add();

                                //}

                                int addResult = grpo.Add();

                                // Check if the addition was successful
                                if (addResult != 0)
                                {
                                    this.DeleteTransactionPOtoGRPO(_TransSeq.ToString());
                                    this.DeleteQRStockDetPOtoGRPO(_QRTransSeq.ToString());
                                    this.DeleteQRStockDetInvTrans(_QRTransSeqInvTrans.ToString());
                                    string msg;
 

                                    msg = "Error code: " + ((WMS_UI_API.Services.SAPConnectionService)_sapConnectionService).oComp.GetLastErrorCode() + Environment.NewLine +
                                          "Error message: " + ((WMS_UI_API.Services.SAPConnectionService)_sapConnectionService).oComp.GetLastErrorDescription();
                                    _logger.LogInformation(" Calling DraftGRPOController : Error " + msg);
                                    return BadRequest(new
                                    {
                                        StatusCode = "400",
                                        IsSaved = _IsSaved,
                                        TransSeq = _TransSeq,
                                        StatusMsg = "Error code: " + addResult + Environment.NewLine +
                                                    "Error message: " + ((WMS_UI_API.Services.SAPConnectionService)_sapConnectionService).oComp.GetLastErrorDescription()
                                    });
                                }
                                else
                                {
                                    int docEntry = int.Parse(((WMS_UI_API.Services.SAPConnectionService)_sapConnectionService).oComp.GetNewObjectKey());

                                    #region Update Transaction Table

                                    if (SAPcon == null)
                                        SAPcon = new SqlConnection(_connection);
                                    _Query = @" UPDATE " + Global.QIT_DB + @".dbo.QIT_Trans_POToGRPO SET DocEntry = @docEntry, DocNum = (select docnum from OPDN where DocEntry = @docEntry) where TransSeq = @code";
                                    _logger.LogInformation(" DraftGRPOController : Update Transaction Table Query : {q} ", _Query.ToString());

                                    cmd = new SqlCommand(_Query, SAPcon);
                                    cmd.Parameters.AddWithValue("@docEntry", docEntry);
                                    cmd.Parameters.AddWithValue("@code", _TransSeq);

                                    SAPcon.Open();
                                    int intNum = cmd.ExecuteNonQuery();
                                    SAPcon.Close();

                                    if (intNum > 0)
                                    {
                                        #region Get GRPO Data
                                        System.Data.DataTable dtGRN = new System.Data.DataTable();
                                        _Query = @"  SELECT * from OPDN where DocEntry = @docEntry  ";
                                        _logger.LogInformation(" DraftGRPOController : Get GRN Data : Query : {q} ", _Query.ToString());
                                        SAPcon.Open();
                                        oAdptr = new SqlDataAdapter(_Query, SAPcon);
                                        oAdptr.SelectCommand.Parameters.AddWithValue("@docEntry", docEntry);
                                        oAdptr.Fill(dtGRN);
                                        SAPcon.Close();
                                        #endregion

                                        _IsSaved = "Y";
                                        return Ok(new
                                        {
                                            StatusCode = "200",
                                            IsSaved = "Y",
                                            TransSeq = _TransSeq,
                                            DocEntry = docEntry,
                                            DocNum = dtGRN.Rows[0]["DocNum"],
                                            StatusMsg = "GRPO added successfully !!!"
                                        });
                                    }
                                    else
                                    {
                                        _IsSaved = "N";
                                        return Ok(new
                                        {
                                            StatusCode = "200",
                                            IsSaved = "N",
                                            TransSeq = _TransSeq,
                                            DocEntry = docEntry,
                                            StatusMsg = "Problem in updating Transaction Table"
                                        });
                                    }
                                    #endregion
                                }
                            }
                            else
                            {
                                this.DeleteTransactionPOtoGRPO(_TransSeq.ToString());
                                this.DeleteQRStockDetPOtoGRPO(_QRTransSeq.ToString());
                                this.DeleteQRStockDetInvTrans(_QRTransSeqInvTrans.ToString());
                                return BadRequest(new
                                {
                                    StatusCode = "400",
                                    IsSaved = "N",
                                    TransSeq = _TransSeq,
                                    StatusMsg = "Problem in saving data in Transaction table"
                                });
                            }
                        }
                        else
                        {
                            string msg;
                            msg = "Error code: " + ((WMS_UI_API.Services.SAPConnectionService)_sapConnectionService).oComp.GetLastErrorCode() + Environment.NewLine +
                                  "Error message: " + ((WMS_UI_API.Services.SAPConnectionService)_sapConnectionService).oComp.GetLastErrorDescription();
                            return BadRequest(new
                            {
                                StatusCode = "400",
                                IsSaved = _IsSaved,
                                TransSeq = _TransSeq,
                                StatusMsg = "Error code: " + ((WMS_UI_API.Services.SAPConnectionService)_sapConnectionService).oComp.GetLastErrorCode() + Environment.NewLine +
                                            "Error message: " + ((WMS_UI_API.Services.SAPConnectionService)_sapConnectionService).oComp.GetLastErrorDescription()
                            });
                        }
                    }
                    else
                    {
                        return BadRequest(new
                        {
                            StatusCode = "400",
                            IsSaved = _IsSaved,
                            TransSeq = _TransSeq,
                            StatusMsg = "Error code: " + ((WMS_UI_API.Services.SAPConnectionService)_sapConnectionService).oComp.GetLastErrorCode() + Environment.NewLine +
                                           "Error message: " + ((WMS_UI_API.Services.SAPConnectionService)_sapConnectionService).oComp.GetLastErrorDescription()
                        });
                    }
                }
                else
                {
                    return BadRequest(new
                    {
                        StatusCode = "400",
                        IsSaved = _IsSaved,
                        TransSeq = 0,
                        StatusMsg = "Details not found"
                    });
                }
            }
            catch (Exception ex)
            {
                this.DeleteTransactionPOtoGRPO(_TransSeq.ToString());
                this.DeleteQRStockDetPOtoGRPO(_QRTransSeq.ToString());
                this.DeleteQRStockDetInvTrans(_QRTransSeqInvTrans.ToString());
                _logger.LogError("Error in DraftGRPOController : CreateDraftGRPO() :: {Error}", ex.ToString());
                return BadRequest(new
                {
                    StatusCode = "400",
                    IsSaved = _IsSaved,
                    StatusMsg = ex.Message.ToString()
                });
            }
            finally
            {
                
            }
        }


        private bool DeleteTransactionPOtoGRPO(string _TransSeq)
        {
            string _IsSaved = "N";
            try
            {
                _logger.LogInformation(" Calling DraftGRPOController : DeleteTransactionPOtoGRPO() ");
                string p_ErrorMsg = string.Empty;

                SqlConnection con = new SqlConnection(_QIT_connection);
                _Query = @" DELETE FROM QIT_Trans_POToGRPO WHERE TransSeq = @transSeq";
                _logger.LogInformation(" DraftGRPOController : DeleteTransactionPOtoGRPO Query : {q} ", _Query.ToString());

                cmd = new SqlCommand(_Query, con);
                cmd.Parameters.AddWithValue("@transSeq", _TransSeq);
                con.Open();
                int intNum = cmd.ExecuteNonQuery();
                con.Close();

                if (intNum > 0)
                    return true;
                else
                    return false;

            }
            catch (Exception ex)
            {
                _logger.LogError("Error in DraftGRPOController : DeleteTransactionPOtoGRPO() :: {Error}", ex.ToString());
                return false;
            }
        }

        private bool DeleteQRStockDetPOtoGRPO(string _QRTransSeq)
        {
            string _IsSaved = "N";
            try
            {
                _logger.LogInformation(" Calling DraftGRPOController : DeleteQRStockDetPOtoGRPO() ");

                string p_ErrorMsg = string.Empty;

                SqlConnection con = new SqlConnection(_QIT_connection);
                _Query = @" DELETE FROM QIT_QRStock_POToGRPO WHERE QRTransSeq = @qrtransSeq";
                _logger.LogInformation(" DraftGRPOController : DeleteQRStockDetPOtoGRPO Query : {q} ", _Query.ToString());

                cmd = new SqlCommand(_Query, con);
                cmd.Parameters.AddWithValue("@qrtransSeq", _QRTransSeq);
                con.Open();
                int intNum = cmd.ExecuteNonQuery();
                con.Close();

                if (intNum > 0)
                    return true;
                else
                    return false;

            }
            catch (Exception ex)
            {
                _logger.LogError("Error in DraftGRPOController : DeleteQRStockDetPOtoGRPO() :: {Error}", ex.ToString());
                return false;
            }
        }

        private bool DeleteTransactionInvTrans(string _TransSeq)
        {
            string _IsSaved = "N";
            try
            {
                _logger.LogInformation(" Calling DraftGRPOController : DeleteTransactionInvTrans() ");

                string p_ErrorMsg = string.Empty;

                SqlConnection con = new SqlConnection(_QIT_connection);
                _Query = @" DELETE FROM QIT_Transfer_InvTrans WHERE TransSeq = @transSeq";
                _logger.LogInformation(" DraftGRPOController : DeleteTransactionInvTrans Query : {q} ", _Query.ToString());

                cmd = new SqlCommand(_Query, con);
                cmd.Parameters.AddWithValue("@transSeq", _TransSeq);
                con.Open();
                int intNum = cmd.ExecuteNonQuery();
                con.Close();

                if (intNum > 0)
                    return true;
                else
                    return false;

            }
            catch (Exception ex)
            {
                _logger.LogError("Error in DraftGRPOController : DeleteTransactionInvTrans() :: {Error}", ex.ToString());
                return false;
            }
        }

        private bool DeleteQRStockDetInvTrans(string _QRTransSeq)
        {
            string _IsSaved = "N";
            try
            {
                _logger.LogInformation(" Calling DraftGRPOController : DeleteQRStockDetInvTrans() ");

                string p_ErrorMsg = string.Empty;

                SqlConnection con = new SqlConnection(_QIT_connection);
                _Query = @" DELETE FROM QIT_QRStock_InvTrans WHERE QRTransSeq = @qrtransSeq";
                _logger.LogInformation(" DraftGRPOController : DeleteQRStockDetInvTrans Query : {q} ", _Query.ToString());

                cmd = new SqlCommand(_Query, con);
                cmd.Parameters.AddWithValue("@qrtransSeq", _QRTransSeq);
                con.Open();
                int intNum = cmd.ExecuteNonQuery();
                con.Close();

                if (intNum > 0)
                    return true;
                else
                    return false;

                // return Ok(new { StatusCode = "200", IsSaved = _IsSaved, StatusMsg = "Deleted Successfully!!!" });
            }
            catch (Exception ex)
            {
                _logger.LogError("Error in DraftGRPOController : DeleteQRStockDetInvTrans() :: {Error}", ex.ToString());
                return false;
                // return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = ex.ToString() });
            }
        }

        public DataTable ConvertListToDataTable<T>(List<T> dataList)
        {
            DataTable dataTable = new DataTable();
            try
            {
                if (dataList.Count > 0)
                {
                    // Get the properties of the DetailData class
                    var properties = typeof(T).GetProperties();

                    // Create columns in the DataTable based on the properties
                    foreach (var property in properties)
                    {
                        dataTable.Columns.Add(property.Name, property.PropertyType);
                    }

                    // Add rows to the DataTable based on the objects in the list
                    foreach (var item in dataList)
                    {
                        DataRow row = dataTable.NewRow();

                        foreach (var property in properties)
                        {
                            row[property.Name] = property.GetValue(item);
                        }

                        dataTable.Rows.Add(row);
                    }
                }
                return dataTable;
            }
            catch (Exception ex)
            {
                _logger.LogError("Error in DraftGRPOController : ConvertListToDataTable() :: {Error}", ex.ToString());
                return dataTable;
            }
        }


        #region Get Freight Category

        [HttpGet("GetFreightCategory")]
        public async Task<ActionResult<IEnumerable<FreightCategory>>> GetFreightCategory()
        {
            SqlConnection QITcon;
            SqlDataAdapter oAdptr;
            try
            {
                _logger.LogInformation(" Calling DraftGRPOController : GetFreightCategory() ");

                System.Data.DataTable dtVendor = new System.Data.DataTable();
                QITcon = new SqlConnection(_QIT_connection);

                _Query =
                @" SELECT A.ExpnsCode, A.ExpnsName, null Amount FROM " + Global.SAP_DB + @".dbo.OEXD A ORDER BY A.ExpnsName FOR BROWSE  ";

                _logger.LogInformation(" DraftGRPOController : GetFreightCategory() Query : {q} ", _Query.ToString());
                QITcon.Open();
                oAdptr = new SqlDataAdapter(_Query, QITcon);
                oAdptr.Fill(dtVendor);
                QITcon.Close();

                if (dtVendor.Rows.Count > 0)
                {
                    List<FreightCategory> obj = new List<FreightCategory>();
                    dynamic arData = JsonConvert.SerializeObject(dtVendor);
                    obj = JsonConvert.DeserializeObject<List<FreightCategory>>(arData.ToString());
                    return obj;
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "No data found" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(" Error in DraftGRPOController : GetFreightCategory() :: {Error}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
            }
        }

        #endregion
    }
}
