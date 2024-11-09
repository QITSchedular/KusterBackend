using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using SAPbobsCOM;
using SAPbouiCOM;
using System.Data;
using System.Data.SqlClient;
using System.Reflection.Metadata;
using System.Xml.Linq;
using WMS_UI_API.Common;
using WMS_UI_API.Models;

namespace WMS_UI_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PurchaseOrdersController : ControllerBase
    {
        private string _ApplicationApiKey = string.Empty;
        private string _connection = string.Empty;
        private string _QIT_connection = string.Empty;
        private string _QIT_DB = string.Empty;
        private string _Query = string.Empty;
        private SqlCommand cmd;

        public IConfiguration Configuration { get; }
        private readonly ILogger<PurchaseOrdersController> _logger;

        public PurchaseOrdersController(IConfiguration configuration, ILogger<PurchaseOrdersController> logger)
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
                _logger.LogError(" Error in PurchaseOrdersController :: {Error}" + ex.ToString());
            }
        }


        #region Fill Data 

        [HttpGet("GetVehicleNo")]
        public async Task<ActionResult<IEnumerable<VehicleNoList>>> GetVehicleNo()
        {
            SqlConnection QITcon;
            SqlDataAdapter oAdptr;
            try
            {
                _logger.LogInformation(" Calling PurchaseOrdersController : GetVehicleNo() ");

                System.Data.DataTable dtVendor = new System.Data.DataTable();
                QITcon = new SqlConnection(_QIT_connection);

                _Query =
                @" select distinct VehicleNo from QIT_GateIN ";

                _logger.LogInformation(" PurchaseOrdersController : GetVehicleNo() Query : {q} ", _Query.ToString());
                QITcon.Open();
                oAdptr = new SqlDataAdapter(_Query, QITcon);
                oAdptr.Fill(dtVendor);
                QITcon.Close();

                if (dtVendor.Rows.Count > 0)
                {
                    List<VehicleNoList> obj = new List<VehicleNoList>();
                    dynamic arData = JsonConvert.SerializeObject(dtVendor);
                    obj = JsonConvert.DeserializeObject<List<VehicleNoList>>(arData.ToString());
                    return obj;
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "No data found" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(" Error in PurchaseOrdersController : GetVehicleNo() :: {Error}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
            }
        }


        [HttpGet("GetTransporter")]
        public async Task<ActionResult<IEnumerable<TransporterList>>> GetTransporter()
        {
            SqlConnection QITcon;
            SqlDataAdapter oAdptr;
            try
            {
                _logger.LogInformation(" Calling PurchaseOrdersController : GetTransporter() ");

                System.Data.DataTable dtTransporter = new System.Data.DataTable();
                QITcon = new SqlConnection(_QIT_connection);

                _Query =
                @" select distinct TransporterCode Transporter from QIT_GateIN ";

                _logger.LogInformation(" PurchaseOrdersController : GetTransporter() Query : {q} ", _Query.ToString());
                QITcon.Open();
                oAdptr = new SqlDataAdapter(_Query, QITcon);
                oAdptr.Fill(dtTransporter);
                QITcon.Close();

                if (dtTransporter.Rows.Count > 0)
                {
                    List<TransporterList> obj = new List<TransporterList>();
                    dynamic arData = JsonConvert.SerializeObject(dtTransporter);
                    obj = JsonConvert.DeserializeObject<List<TransporterList>>(arData.ToString());
                    return obj;
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "No data found" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(" Error in PurchaseOrdersController : GetTransporter() :: {Error}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
            }
        }

        #endregion


        #region Multiple PO GateIN Flow

        [HttpPost("GetPOList")]
        public async Task<ActionResult<IEnumerable<MultiplePOList>>> GetPOList(int BranchID, int Series)
        {
            SqlConnection QITcon;
            SqlDataAdapter oAdptr;
            try
            {
                _logger.LogInformation(" Calling PurchaseOrdersController : GetPOList() ");
                System.Data.DataTable dtData = new System.Data.DataTable();
                QITcon = new SqlConnection(_QIT_connection);

                #region Validation
                if (BranchID <= 0 || BranchID == null)
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Provide Branch" });
                }

                if (Series <= 0 || Series == null)
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Provide Series" });
                }
                #endregion

                #region Query
                _Query = @" 
                WITH PO_RecQty AS (
                    SELECT A.DocEntry, A.DocNum,  CAST(SUM(B.Quantity) as numeric(19,3)) AS POQty 
                    FROM " + Global.SAP_DB + @".dbo.OPOR A
                    INNER JOIN " + Global.SAP_DB + @".dbo.POR1 B ON A.DocEntry = B.DocEntry
                    WHERE Series = @series AND CANCELED = 'N' AND DocStatus = 'O' AND DocType = 'I' and 
                          -- B.LineStatus = 'O' and 
                          ISNULL(A.BPLId,@bId) = @bId
                    GROUP BY A.DocEntry, A.DocNum
                ),
                Rec_RecQty AS (
                    SELECT DocEntry, CAST(SUM(RecQty) as numeric(19,3)) AS RecQty
                    FROM QIT_GateIN 
                    WHERE Series = @series AND ObjType = '22' and ISNULL(BranchID,@bId) = @bId and Canceled = 'N'
                    GROUP BY DocEntry
                ) 
                SELECT A.DocEntry, A.DocNum, B.DocDate, B.CardCode, B.CardName,
                       case when (select count(1) FROM " + Global.SAP_DB + @".dbo.POR3 WHERE DocEntry = A.DocEntry) > 0 then 'Y' else 'N' end FreightApplied,
                       ( SELECT count(Z1.DocEntry) FROM " + Global.SAP_DB + @".dbo.OPDN Z1 INNER JOIN " + Global.SAP_DB + @".dbo.PDN1 Z2 ON Z1.DocEntry = Z2.DocEntry
		                 WHERE Z2.BaseRef = B.DocNum and Z2.BaseType = B.ObjType and 
                               Z2.BaseEntry = B.DocEntry and Z1.U_QIT_FromWeb = 'N'
	                   ) SAPEntryCount
                FROM 
                (
                    SELECT PO.DocEntry, PO.DocNum, CAST(PO.POQty as numeric(19,3)) POQty, CAST(ISNULL(Rec.RecQty, 0) as numeric(19,3)) AS RecQty
                    FROM PO_RecQty PO
                    LEFT JOIN Rec_RecQty Rec ON PO.DocEntry = Rec.DocEntry
                    WHERE PO.POQty <> ISNULL(Rec.RecQty, 0)
                ) as A INNER JOIN " + Global.SAP_DB + @".dbo.OPOR B ON A.DocEntry = B.DocEntry    
                ";
                #endregion

                _logger.LogInformation(" PurchaseOrdersController : GetPOList() Query : {q} ", _Query.ToString());
                QITcon.Open();
                oAdptr = new SqlDataAdapter(_Query, QITcon);
                oAdptr.SelectCommand.Parameters.AddWithValue("@series", Series);
                oAdptr.SelectCommand.Parameters.AddWithValue("@bId", BranchID);
                oAdptr.Fill(dtData);
                QITcon.Close();

                if (dtData.Rows.Count > 0)
                {
                    List<MultiplePOList> oPOList = new List<MultiplePOList>();
                    dynamic arPODet = Newtonsoft.Json.JsonConvert.SerializeObject(dtData);
                    oPOList = Newtonsoft.Json.JsonConvert.DeserializeObject<List<MultiplePOList>>(arPODet.ToString());

                    return oPOList;
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "No data found" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(" Error in PurchaseOrdersController : GetPOList() :: {Error}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
            }
        }


        [HttpPost("GetPOItems")]
        public async Task<ActionResult<IEnumerable<POItems>>> GetPOItems(DocList payload)
        {
            SqlConnection QITcon;
            SqlDataAdapter oAdptr;
            try
            {

                _logger.LogInformation(" Calling PurchaseOrdersController : GetPOItems() ");

                #region Validation

                if (payload.BranchID <= 0 || payload.BranchID == null)
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Provide Branch" });
                }

                if (payload.Series <= 0 || payload.Series == null)
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Provide Series" });
                }

                List<int> docEntryValues = payload.DocEntryList.Select(x => x.DocEntry).ToList();
                string dynamicDocEntries = string.Join(",", docEntryValues);

                if (dynamicDocEntries == string.Empty)
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Provide Purchase Order(s)" });
                }
                QITcon = new SqlConnection(_QIT_connection);
                System.Data.DataTable dtPOCardCodes = new System.Data.DataTable();

                _Query = @" SELECT DISTINCT cardcode FROM " + Global.SAP_DB + @".dbo.OPOR WHERE DocEntry in (" + dynamicDocEntries + @") ";
                _logger.LogInformation(" PurchaseOrdersController : Cardcode Query : {q} ", _Query.ToString());
                QITcon.Open();
                oAdptr = new SqlDataAdapter(_Query, QITcon);
                oAdptr.Fill(dtPOCardCodes);
                QITcon.Close();

                if (dtPOCardCodes.Rows.Count > 1)
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "There are multiple vendors" });
                }


                System.Data.DataTable dtTaxCodes = new System.Data.DataTable();
                QITcon = new SqlConnection(_QIT_connection);
                _Query = @" SELECT DISTINCT TaxCode FROM " + Global.SAP_DB + @".dbo.POR1 WHERE DocEntry in (" + dynamicDocEntries + @") ";
                _logger.LogInformation(" PurchaseOrdersController : TaxCode Query : {q} ", _Query.ToString());
                QITcon.Open();
                oAdptr = new SqlDataAdapter(_Query, QITcon);
                oAdptr.Fill(dtTaxCodes);
                QITcon.Close();

                if (dtTaxCodes.Rows.Count > 1)
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "There are multiple Tax Codes" });
                }
                #endregion

                List<POItems> obj = new List<POItems>();
                System.Data.DataTable dtData = new System.Data.DataTable();
                QITcon = new SqlConnection(_QIT_connection);

                _Query = @" 
                    SELECT A.Series, A.DocEntry, A.DocNum, A.ObjType, A.DocDate, A.CardCode, A.CardName ,  
                           A.ItemCode, A.ItemName, A.LineNum, CAST(A.Qty as numeric(19,3)) Qty, 
                           CAST(ISNULL(A.RecQty,0) as numeric(19,3)) ReceivedQty, CAST(A.Price as numeric(19,3)) as Price, 
	                       case when A.OpenQty < 0 and 
                                     lower(UomCode) in ('kgs', 'kg', 'gram','ltr', 'cu.ft.', 'mtr', 'mtr.', 'rft','sq. ft','sq. ft.','sq. mtr',
                                                        'sq.mtr.','sq.f','sq.ft','sq.mtr'  ) then 
			                    CAST((ISNULL(A.Quantity,0) + ((ISNULL(A.Quantity,0) * 5)  / 100) - ISNULL(A.RecQty,0)) as numeric(19,3))
	                       else CAST(A.OpenQty as numeric(19,3)) end OpenQty ,  
                           A.ItemMngBy, A.QRMngBy, A.Project, A.UomCode, A.WhsCode, A.U_LRNo,
                           case when (select count(1) FROM " + Global.SAP_DB + @".dbo.POR3 WHERE DocEntry = A.DocEntry) > 0 then 'Y' else 'N' end FreightApplied
                    FROM 
                    (
                        SELECT A.Series, A.DocEntry, A.DocNum, B.LineNum, A.ObjType, A.DocDate, A.CardCode, A.CardName , 
                               B.ItemCode, B.Dscription ItemName, CAST(B.Quantity as numeric(19,3)) Qty, 
                               CAST(B.Price as numeric(19,3)) Price, CAST(B.Quantity as numeric(19,3)) Quantity, 
                               CAST(C.RecQty as numeric(19,3)) RecQty, 
                               CAST((ISNULL(ISNULL(B.Quantity,0) - ISNULL(C.RecQty,0),0)) as numeric(19,3)) OpenQty, 
                               D.ItemMngBy, D.QRMngBy, B.Project, B.unitMsr UomCode, B.WhsCode, A.U_LRNo
                        FROM  " + Global.SAP_DB + @".dbo.OPOR A inner join " + Global.SAP_DB + @".dbo.POR1 B on 
                              A.DocEntry = B.DocEntry and A.DocEntry IN(" + dynamicDocEntries + @")
                        LEFT JOIN 
                        ( 
                            SELECT AA.DocEntry, AA.ObjType, AA.ItemCode, AA.LineNum, AA.BranchID, CAST(SUM(RecQty) as numeric(19,3)) RecQty 
                            FROM QIT_GateIN AA 
                            WHERE ISNULL(AA.BranchID, @bId) = @bId and AA.ObjType = '22' and AA.CANCELED = 'N' and AA.DocEntry IN(" + dynamicDocEntries + @")
                            GROUP BY AA.DocEntry, AA.ObjType, AA.ItemCode, AA.LineNum, AA.BranchID 
                        ) C ON A.ObjType COLLATE SQL_Latin1_General_CP1_CI_AS = C.ObjType AND 
                                A.DocEntry = C.DocEntry AND ISNULL(A.BPLId,1) = C.BranchID AND 
                                B.ItemCode COLLATE SQL_Latin1_General_CP1_CI_AS = C.ItemCode AND
                                B.LineNum = C.LineNum
                        INNER JOIN  
                        ( 
                            SELECT A.ItemCode, A.ItemName, QRMngBy, ItemMngBy  
                            FROM QIT_Item_Master A 
                            INNER JOIN " + Global.SAP_DB + @".dbo.OITM B ON A.ItemCode COLLATE SQL_Latin1_General_CP850_CI_AS = B.ItemCode and 
                                  B.InvntItem = 'Y'
                        ) D ON B.ItemCode COLLATE SQL_Latin1_General_CP1_CI_AS  = D.ItemCode 
                        WHERE A.DocEntry IN(" + dynamicDocEntries + @") and A.Series = @series and ISNULL(A.BPLId, @bId) = @bId AND 
                                A.CANCELED = 'N' AND B.LineStatus = 'O'
                    ) as A
					ORDER BY A.DocEntry, A.LineNum  ";


                _logger.LogInformation(" PurchaseOrdersController : GetPOItems() Query : {q} ", _Query.ToString());
                QITcon.Open();
                oAdptr = new SqlDataAdapter(_Query, QITcon);
                oAdptr.SelectCommand.Parameters.AddWithValue("@series", payload.Series);
                oAdptr.SelectCommand.Parameters.AddWithValue("@bId", payload.BranchID);
                oAdptr.Fill(dtData);
                QITcon.Close();

                if (dtData.Rows.Count > 0)
                {
                    dynamic arPODet = Newtonsoft.Json.JsonConvert.SerializeObject(dtData);
                    obj = Newtonsoft.Json.JsonConvert.DeserializeObject<List<POItems>>(arPODet.ToString());
                    return obj;
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "No data found" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(" Error in PurchaseOrdersController : GetPOItems() :: {Error}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
            }
        }


        [HttpPost("SaveGateIN")]
        public IActionResult SaveGateIN(SaveGateINHeader payload)
        {
            string _IsSaved = "N";
            SqlConnection QITcon;
            SqlDataAdapter oAdptr;
            try
            {
                _logger.LogInformation(" Calling PurchaseOrdersController : SaveGateIN() ");

                int _totalGateIN = 0;
                int _SaveGateIN = 0;

                if (payload != null)
                {
                    foreach (var itemDet in payload.gateInDetails)
                    {
                        System.Data.DataTable dtPO = new System.Data.DataTable();
                        QITcon = new SqlConnection(_QIT_connection);
                        _Query = @" 
                        SELECT A.DocEntry, A.DocNum, A.Series, A.ObjType, A.DocDate, A.CardCode, A.CardName ,
                               B.LineNum, B.ItemCode, B.Dscription ItemName, CAST(B.Quantity as numeric(19,3)) Qty, 
                               B.Project, B.unitMsr UomCode, B.WhsCode   
                        FROM " + Global.SAP_DB + @".dbo.OPOR A INNER JOIN " + Global.SAP_DB + @".dbo.POR1 B on A.DocEntry = B.DocEntry
                        WHERE A.DocEntry = @docEntry and B.ItemCode = @iCode and B.LineNum = @line and ISNULL(A.BPLId, @bid) = @bid
                              ";
                        QITcon.Open();
                        oAdptr = new SqlDataAdapter(_Query, QITcon);
                        oAdptr.SelectCommand.Parameters.AddWithValue("@docEntry", itemDet.DocEntry);
                        oAdptr.SelectCommand.Parameters.AddWithValue("@iCode", itemDet.ItemCode);
                        oAdptr.SelectCommand.Parameters.AddWithValue("@line", itemDet.LineNum);
                        oAdptr.SelectCommand.Parameters.AddWithValue("@bid", payload.BranchID);
                        oAdptr.Fill(dtPO);
                        QITcon.Close();

                        if (dtPO.Rows.Count > 0)
                        {
                            double dblQty = double.Parse(dtPO.Rows[0]["Qty"].ToString());

                            if (dtPO.Rows[0]["UomCode"].ToString().ToLower() == "kgs" ||
                                dtPO.Rows[0]["UomCode"].ToString().ToLower() == "kg" ||
                                dtPO.Rows[0]["UomCode"].ToString().ToLower() == "gram" ||
                                 dtPO.Rows[0]["UomCode"].ToString().ToLower() == "ltr" ||
                                dtPO.Rows[0]["UomCode"].ToString().ToLower() == "cu.ft." || dtPO.Rows[0]["UomCode"].ToString().ToLower() == "mtr" ||
                                dtPO.Rows[0]["UomCode"].ToString().ToLower() == "mtr." || dtPO.Rows[0]["UomCode"].ToString().ToLower() == "rft" ||
                                dtPO.Rows[0]["UomCode"].ToString().ToLower() == "sq. ft" || dtPO.Rows[0]["UomCode"].ToString().ToLower() == "sq. ft." ||
                                dtPO.Rows[0]["UomCode"].ToString().ToLower() == "sq. mtr" || dtPO.Rows[0]["UomCode"].ToString().ToLower() == "sq. mtr." ||
                                dtPO.Rows[0]["UomCode"].ToString().ToLower() == "sq.f" || dtPO.Rows[0]["UomCode"].ToString().ToLower() == "sq.ft" ||
                                dtPO.Rows[0]["UomCode"].ToString().ToLower() == "sq.mtr"
                               )
                                dblQty = dblQty + ((dblQty * 5) / 100);

                            if (dblQty < double.Parse(itemDet.RecQty))
                            {
                                return BadRequest(new
                                {
                                    StatusCode = "400",
                                    IsSaved = _IsSaved,
                                    StatusMsg = "PO : " + itemDet.DocNum + Environment.NewLine +
                                                "ItemCode : " + itemDet.ItemCode + Environment.NewLine +
                                                "Ordered Quantity is " + dblQty
                                });
                            }

                            #region Check for exist record
                            System.Data.DataTable dt = new System.Data.DataTable();
                            QITcon = new SqlConnection(_QIT_connection);
                            _Query = @" SELECT CAST(SUM(RecQty) as numeric(19,3)) RecQty FROM QIT_GateIN A  
                                    WHERE A.DocEntry = @docEntry and A.ItemCode = @iCode AND A.LineNum = @line and A.BranchID = @bid and A.ObjType = @objType and A.Canceled = 'N'
                                  ";
                            QITcon.Open();
                            oAdptr = new SqlDataAdapter(_Query, QITcon);
                            oAdptr.SelectCommand.Parameters.AddWithValue("@docEntry", itemDet.DocEntry);
                            oAdptr.SelectCommand.Parameters.AddWithValue("@iCode", itemDet.ItemCode);
                            oAdptr.SelectCommand.Parameters.AddWithValue("@line", itemDet.LineNum);
                            oAdptr.SelectCommand.Parameters.AddWithValue("@bid", payload.BranchID);
                            oAdptr.SelectCommand.Parameters.AddWithValue("@objType", payload.ObjType);
                            oAdptr.Fill(dt);
                            QITcon.Close();

                            if (dt.Rows.Count > 0)
                            {
                                double dblGateINQty = dt.Rows[0]["RecQty"].ToString() == String.Empty ? 0 : double.Parse(dt.Rows[0]["RecQty"].ToString());
                                if (dblQty == dblGateINQty)
                                    return BadRequest(new
                                    {
                                        StatusCode = "400",
                                        IsSaved = _IsSaved,
                                        StatusMsg = "PO : " + itemDet.DocNum + Environment.NewLine +
                                                    "ItemCode : " + itemDet.ItemCode + Environment.NewLine +
                                                    "Ordered Quantity is " + dblQty + Environment.NewLine +
                                                    "Gate IN Quantity is " + dblGateINQty
                                    });
                                else if ((dblQty - dblGateINQty) < double.Parse(itemDet.RecQty))
                                    return BadRequest(new
                                    {
                                        StatusCode = "400",
                                        IsSaved = _IsSaved,
                                        StatusMsg = "PO : " + itemDet.DocNum + Environment.NewLine +
                                                    "ItemCode : " + itemDet.ItemCode + Environment.NewLine +
                                                    (dblQty - dblGateINQty) + " Quantity is pending to be received"
                                    });
                            }
                            #endregion 
                        }
                        else
                        {
                            return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = " Details not found" });
                        }
                        _totalGateIN = _totalGateIN + 1;
                    }

                    #region Check for GRPO Ref NO

                    #region Get Series data
                    QITcon = new SqlConnection(_QIT_connection);
                    System.Data.DataTable dtSeries = new System.Data.DataTable();
                    _Query = @" 
                    SELECT * FROM " + Global.SAP_DB + @".dbo.NNM1 
                    WHERE Series = (SELECT GRPOSeries FROM QIT_Config_Master A WHERE ISNULL(A.BranchID, @bID) = @bID )  ";
                    _logger.LogInformation(" PurchaseOrdersController : Configuration  Query : {q} ", _Query.ToString());
                    QITcon.Open();
                    oAdptr = new SqlDataAdapter(_Query, QITcon);
                    oAdptr.SelectCommand.Parameters.AddWithValue("@bID", payload.BranchID);
                    oAdptr.Fill(dtSeries);
                    QITcon.Close();

                    if (dtSeries.Rows.Count > 0)
                    {
                        //if (dtConfig.Rows[0]["GRPOSeries"].ToString().Trim().Length <= 0)
                        //    return BadRequest(new { StatusCode = "400", StatusMsg = "Define GRPO Series in Configuration" });
                    }
                    else
                    {
                        return BadRequest(new { StatusCode = "400", StatusMsg = "Configuration not found" });
                    }
                    #endregion

                    System.Data.DataTable dtRefNo = new System.Data.DataTable();
                    QITcon = new SqlConnection(_QIT_connection);
                    _Query = @" 
                        SELECT *
                        FROM   " + Global.SAP_DB + @".dbo.OPDN WITH(NOLOCK) 
                        WHERE  CardCode = @cardCode AND NumAtCard = @numAtCard AND CANCELED = 'N' AND PIndicator = @pi
                        ";
                    QITcon.Open();
                    oAdptr = new SqlDataAdapter(_Query, QITcon);
                    oAdptr.SelectCommand.Parameters.AddWithValue("@cardCode", payload.CardCode);
                    oAdptr.SelectCommand.Parameters.AddWithValue("@numAtCard", payload.GRPOVendorRefNo);
                    oAdptr.SelectCommand.Parameters.AddWithValue("@pi", dtSeries.Rows[0]["Indicator"].ToString());
                    oAdptr.Fill(dtRefNo);
                    QITcon.Close();

                    if (dtRefNo.Rows.Count > 0)
                    {
                        return BadRequest(new
                        {
                            StatusCode = "400",
                            IsSaved = _IsSaved,
                            StatusMsg = "This Reference Number Is Already Present For Other Document in " + dtSeries.Rows[0]["Indicator"].ToString()
                        });
                    }
                    #endregion

                    foreach (var itemDet in payload.gateInDetails)
                    {
                        QITcon = new SqlConnection(_QIT_connection);
                        _Query = @" 
                        INSERT INTO QIT_GateIN
                        (
                            SrNo, GateInNo, BranchID, ObjType, DocEntry, DocNum, Series, LineNum, ItemCode, 
                            GateIN, RecQty, RecDate, Project, UoMCode, WhsCode, VehicleNo, TransporterCode, ToleranceApplied, 
                            LRNo, LRDate, GRPOVendorRefNo, GRPODocDate, EntryDate, VendorGateIn, Canceled, UpdateDate
                        )
                        VALUES( (select ISNULL(max(SrNo),0) + 1 from QIT_GateIN), @gateInNo, @bID, @objType, @docEntry, @docNum, @series, @line, @iCode, @gateIN, @recQty, @recDate, @proj, @uom, @whs, @vehicleNo, @transporter, @tolApplied, @lrNo, @lrDate, @refNo, @docDate, @entryDate, 'N', @cancel, @uDate)
                        ";
                        _logger.LogInformation(" PurchaseOrdersController : SaveGateIN() Query : {q} ", _Query.ToString());

                        cmd = new SqlCommand(_Query, QITcon);
                        cmd.Parameters.AddWithValue("@gateInNo", payload.GateInNo);
                        cmd.Parameters.AddWithValue("@bID", payload.BranchID);
                        cmd.Parameters.AddWithValue("@objType", payload.ObjType);
                        cmd.Parameters.AddWithValue("@docEntry", itemDet.DocEntry);
                        cmd.Parameters.AddWithValue("@docNum", itemDet.DocNum);
                        cmd.Parameters.AddWithValue("@series", payload.Series);
                        cmd.Parameters.AddWithValue("@line", itemDet.LineNum);
                        cmd.Parameters.AddWithValue("@iCode", itemDet.ItemCode);
                        cmd.Parameters.AddWithValue("@gateIN", "Y");
                        cmd.Parameters.AddWithValue("@recQty", itemDet.RecQty);
                        cmd.Parameters.AddWithValue("@recDate", DateTime.Now);
                        cmd.Parameters.AddWithValue("@proj", itemDet.Project);
                        cmd.Parameters.AddWithValue("@uom", itemDet.UomCode);
                        cmd.Parameters.AddWithValue("@whs", itemDet.WhsCode);
                        cmd.Parameters.AddWithValue("@vehicleNo", payload.VehicleNo);
                        cmd.Parameters.AddWithValue("@transporter", payload.Transporter);
                        cmd.Parameters.AddWithValue("@tolApplied", itemDet.TolApplied);
                        cmd.Parameters.AddWithValue("@lrNo", payload.LRNo);
                        cmd.Parameters.AddWithValue("@lrDate", payload.LRDate == string.Empty ? DBNull.Value : payload.LRDate);
                        cmd.Parameters.AddWithValue("@refNo", payload.GRPOVendorRefNo);
                        cmd.Parameters.AddWithValue("@docDate", payload.GRPODocDate);
                        cmd.Parameters.AddWithValue("@entryDate", DateTime.Now);
                        cmd.Parameters.AddWithValue("@cancel", "N");
                        cmd.Parameters.AddWithValue("@uDate", DBNull.Value);

                        QITcon.Open();
                        int intNum = cmd.ExecuteNonQuery();
                        QITcon.Close();

                        if (intNum > 0)
                        {
                            _IsSaved = "Y";
                            _SaveGateIN = _SaveGateIN + 1;
                        }
                        else
                        {
                            _IsSaved = "N";
                            bool bln = DeleteGateIN(payload.GateInNo);
                            return BadRequest(new
                            {
                                StatusCode = "400",
                                IsSaved = "N",
                                StatusMsg = "Problem in saving data for :" + Environment.NewLine +
                                            "PO : " + itemDet.DocNum + Environment.NewLine +
                                            "ItemCode : " + itemDet.ItemCode + Environment.NewLine +
                                            "Project : " + itemDet.Project
                            });
                        }
                    }

                    if (_totalGateIN == _SaveGateIN && _SaveGateIN > 0)
                    {
                        return Ok(new { StatusCode = "200", IsSaved = _IsSaved, GateInNo = payload.GateInNo, StatusMsg = "Saved Successfully!!!" });
                    }
                    else
                    {
                        return BadRequest(new { StatusCode = "400", IsSaved = "N", GateInNo = payload.GateInNo, StatusMsg = "Problem in saving GateIn data" });
                    }
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = " Details not found" });
                }
            }
            catch (Exception ex)
            {
                bool bln = DeleteGateIN(payload.GateInNo);
                _logger.LogError(" Error in PurchaseOrdersController : SaveGateIN() :: {Error}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
            }
        }

        private bool DeleteGateIN(int p_GateIN)
        {
            SqlConnection QITcon;
            try
            {
                _logger.LogInformation(" Calling PurchaseOrdersController : DeleteGateIN() ");
                string p_ErrorMsg = string.Empty;

                QITcon = new SqlConnection(_QIT_connection);
                _Query = @" DELETE FROM QIT_GateIN WHERE GateInNo = @gateInNo ";
                _logger.LogInformation(" PurchaseOrdersController : DeleteGateIN Query : {q} ", _Query.ToString());

                cmd = new SqlCommand(_Query, QITcon);
                cmd.Parameters.AddWithValue("@gateInNo", p_GateIN);
                QITcon.Open();
                int intNum = cmd.ExecuteNonQuery();
                QITcon.Close();

                if (intNum > 0)
                    return true;
                else
                    return false;
            }
            catch (Exception ex)
            {
                _logger.LogError("Error in PurchaseOrdersController : DeleteGateIN() :: {Error}", ex.ToString());
                return false;
            }
        }


        [HttpGet("GateINList")]
        public async Task<ActionResult<IEnumerable<GateINList>>> GateINList(string BranchID)
        {
            SqlConnection QITcon;
            SqlDataAdapter oAdptr;
            string _IsSaved = "N";
            try
            {
                _logger.LogInformation(" Calling PurchaseOrdersController : GateINList() ");

                System.Data.DataTable dtGateIN = new System.Data.DataTable();
                QITcon = new SqlConnection(_QIT_connection);
                _Query = @" SELECT A.SrNo, A.GateInNo, A.DocEntry, A.DocNum, A.Series, A.BranchID, 
                                   B.RecDate, B.VehicleNo, B.TransporterCode, B.Canceled 
                            FROM 
                            (
                                SELECT MAX(A.SrNo) SrNo, A.GateInNo, A.DocEntry, A.DocNum, A.Series, A.BranchID 
                                FROM QIT_GateIN A
                                WHERE BranchID = @bid
                                GROUP BY A.DocEntry, A.DocNum, A.GateInNo,A.Series, A.BranchID 
                            ) as A
                            INNER JOIN QIT_GateIN B ON A.SrNo = B.SrNo
                            ORDER BY B.DocEntry, B.GateInNo
                         ";
                QITcon.Open();
                oAdptr = new SqlDataAdapter(_Query, QITcon);
                oAdptr.SelectCommand.Parameters.AddWithValue("@bid", BranchID);
                oAdptr.Fill(dtGateIN);
                QITcon.Close();

                if (dtGateIN.Rows.Count > 0)
                {
                    List<GateINList> obj = new List<GateINList>();
                    dynamic arData = JsonConvert.SerializeObject(dtGateIN);
                    obj = JsonConvert.DeserializeObject<List<GateINList>>(arData.ToString());
                    return obj;
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "No data found" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(" Error in PurchaseOrdersController : GateINList() :: {Error}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
            }
        }


        [HttpPut("CancelGateIN")]
        public IActionResult CancelGateIN(int BranchID, int GateInNo)
        {
            SqlConnection QITcon;
            SqlDataAdapter oAdptr;
            string _IsSaved = "N";
            try
            {
                _logger.LogInformation(" Calling PurchaseOrdersController : CancelGateIN() ");
                System.Data.DataTable dtGateIN = new System.Data.DataTable();
                QITcon = new SqlConnection(_QIT_connection);
                _Query = @" SELECT * FROM QIT_GateIN A  
                            WHERE A.GateInNo = @gateInNo and ISNULL(A.BranchId, @bId) = @bId
                         ";
                _logger.LogInformation(" PurchaseOrdersController : CancelGateIN : Exist or Not Query : {q} ", _Query.ToString());

                oAdptr = new SqlDataAdapter(_Query, QITcon);
                oAdptr.SelectCommand.Parameters.AddWithValue("@gateInNo", GateInNo);
                oAdptr.SelectCommand.Parameters.AddWithValue("@bId", BranchID);
                oAdptr.Fill(dtGateIN);
                QITcon.Close();

                if (dtGateIN.Rows.Count > 0)
                {
                    System.Data.DataTable dtQR = new System.Data.DataTable();
                    _Query = @" 
                    SELECT C.* from QIT_QR_Header B inner join QIT_QR_Detail C 
                    ON B.HeaderSrNo = C.HeaderSrNo AND C.GateInNo = @gateInNo and ISNULL(B.BranchId, @bId) = @bId
                    ";
                    _logger.LogInformation(" PurchaseOrdersController : CancelGateIN : Query : {q} ", _Query.ToString());
                    QITcon = new SqlConnection(_QIT_connection);
                    QITcon.Open();
                    oAdptr = new SqlDataAdapter(_Query, QITcon);
                    oAdptr.SelectCommand.Parameters.AddWithValue("@gateInNo", GateInNo);
                    oAdptr.SelectCommand.Parameters.AddWithValue("@bId", BranchID);
                    oAdptr.Fill(dtQR);
                    QITcon.Close();

                    if (dtQR.Rows.Count > 0)
                    {
                        System.Data.DataTable dtGRPO = new System.Data.DataTable();
                        _Query = @" 
                        SELECT A.* from QIT_QRStock_POToGRPO A WHERE A.GateInNo = @gateInNo and ISNULL(A.BranchId, @bId) = @bId
                        ";
                        _logger.LogInformation(" PurchaseOrdersController : CancelGateIN : Query : {q} ", _Query.ToString());
                        QITcon = new SqlConnection(_QIT_connection);
                        QITcon.Open();
                        oAdptr = new SqlDataAdapter(_Query, QITcon);
                        oAdptr.SelectCommand.Parameters.AddWithValue("@gateInNo", GateInNo);
                        oAdptr.SelectCommand.Parameters.AddWithValue("@bId", BranchID);
                        oAdptr.Fill(dtGRPO);
                        QITcon.Close();

                        if (dtGRPO.Rows.Count > 0)
                        {
                            return BadRequest(new
                            {
                                StatusCode = "400",
                                IsSaved = _IsSaved,
                                StatusMsg = "Unable to cancel Gate IN " + Environment.NewLine +
                                            "GRPO for Gate In No " + GateInNo + " is done"
                            });
                        }
                        else
                        {
                            #region Update Cancel status

                            _Query = @" 
                            UPDATE QIT_GateIN SET Canceled = @cancel, UpdateDate = @uDate 
                            WHERE GateInNo = @gateInNo and ISNULL(BranchId, @bId) = @bId

                            UPDATE QIT_QR_Detail SET Canceled = @cancel, UpdateDate = @uDate
                            WHERE GateInNo = @gateInNo and ISNULL(BranchId, @bId) = @bId

                            ";

                            _logger.LogInformation(" PurchaseOrdersController : CancelGateIN() Query : {q} ", _Query.ToString());
                            QITcon = new SqlConnection(_QIT_connection);
                            cmd = new SqlCommand(_Query, QITcon);
                            cmd.Parameters.AddWithValue("@cancel", "Y");
                            cmd.Parameters.AddWithValue("@uDate", DateTime.Now);
                            cmd.Parameters.AddWithValue("@gateInNo", GateInNo);
                            cmd.Parameters.AddWithValue("@bId", BranchID);

                            QITcon.Open();
                            int intNum = cmd.ExecuteNonQuery();
                            QITcon.Close();

                            if (intNum > 0)
                                _IsSaved = "Y";
                            else
                                _IsSaved = "N";

                            return Ok(new { StatusCode = "200", IsSaved = _IsSaved, StatusMsg = "Updated Successfully!!!" });
                            #endregion
                        }
                    }
                    else
                    {
                        #region Update Cancel status

                        _Query = @" 
                        UPDATE QIT_GateIN SET Canceled = @cancel, UpdateDate = @uDate 
                        WHERE GateInNo = @gateInNo and ISNULL(BranchId, @bId) = @bId";

                        _logger.LogInformation(" PurchaseOrdersController : CancelGateIN() Query : {q} ", _Query.ToString());
                        QITcon = new SqlConnection(_QIT_connection);
                        cmd = new SqlCommand(_Query, QITcon);
                        cmd.Parameters.AddWithValue("@cancel", "Y");
                        cmd.Parameters.AddWithValue("@uDate", DateTime.Now);
                        cmd.Parameters.AddWithValue("@gateInNo", GateInNo);
                        cmd.Parameters.AddWithValue("@bId", BranchID);

                        QITcon.Open();
                        int intNum = cmd.ExecuteNonQuery();
                        QITcon.Close();

                        if (intNum > 0)
                            _IsSaved = "Y";
                        else
                            _IsSaved = "N";

                        return Ok(new { StatusCode = "200", IsSaved = _IsSaved, StatusMsg = "Updated Successfully!!!" });
                        #endregion
                    }
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "No such Gate IN data exist !!!" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(" Error in PurchaseOrdersController : CancelGateIN() :: {Error}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
            }
        }


        [HttpPost("GetGateInDetails")]

        public async Task<ActionResult<IEnumerable<POHeader>>> GetGateInDetails(PurchaseOrderFilter payload)
        {
            SqlConnection QITcon;
            SqlDataAdapter oAdptr;
            try
            {
                _logger.LogInformation(" Calling PurchaseOrdersController : GetGateInDetails() ");

                List<POHeader> obj = new List<POHeader>();
                System.Data.DataTable dtData = new System.Data.DataTable();
                QITcon = new SqlConnection(_QIT_connection);

                #region Validation
                if (payload.BranchID <= 0 || payload.BranchID == null)
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Provide Branch" });
                }

                if (payload.Series <= 0 || payload.Series == null)
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Provide Series" });
                }


                #endregion

                string _where = string.Empty;
                string _w = string.Empty;

                if (payload.GateInNo > 0)
                {
                    _where = " AND AA.GateInNo = @gateInNo";
                }

                if (payload.Canceled.ToUpper() == "N")
                {
                    _where += " AND AA.Canceled = 'N' ";
                    _w += " AND A.Canceled = 'N' ";
                }
                if (payload.Canceled.ToUpper() == "Y")
                {
                    _where += " AND AA.Canceled = 'Y' ";
                    _w += " AND A.Canceled = 'Y' ";
                }
                if (payload.DocNum <= 0 || payload.DocNum == null)
                {
                    // return BadRequest(new { StatusCode = "400", StatusMsg = "Provide Purchase Order" });
                }
                else
                {
                    _where += " AND AA.DocNum = @docNum";
                    _w += " AND A.DocNum = @docNum";
                }



                #region Query
                _Query = @" 
                SELECT A.Series, A.DocEntry, A.DocNum, B.LineNum, A.ObjType, A.DocDate, A.CardCode, A.CardName, 
                       B.LineNum, B.ItemCode, B.Dscription ItemName, CAST(B.Quantity as numeric(19,3)) Qty, 
                       CAST(B.Price as numeric(19,3)) Price, CAST(ISNULL(C.RecQty,0) as numeric(19,3)) OpenQty, 
                       D.ItemMngBy, D.QRMngBy, cast(C.RecDate as nvarchar(25)) RecDate, C.GateInNo, 
                       B.Project, B.unitMsr UomCode, B.WhsCode, A.U_LRNo,
                       case when (select count(1) FROM " + Global.SAP_DB + @".dbo.POR3 WHERE DocEntry = A.DocEntry) > 0 then 'Y' else 'N' end FreightApplied
                FROM " + Global.SAP_DB + @".dbo.OPOR A inner join " + Global.SAP_DB + @".dbo.POR1 B on A.DocEntry = B.DocEntry 
                INNER JOIN 
                ( 
                    SELECT AA.GateInNo, AA.DocEntry, AA.ObjType, AA.ItemCode, AA.LineNum, AA.BranchID, RecQty, RecDate  
                    FROM QIT_GateIN AA 
                    WHERE ISNULL(AA.BranchID, @bId) = @bId and AA.ObjType = '22'  " + _where + @"
                ) C ON A.ObjType COLLATE SQL_Latin1_General_CP1_CI_AS = C.ObjType AND 
                        A.DocEntry = C.DocEntry AND ISNULL(A.BPLId,1) = C.BranchID AND 
                        B.ItemCode COLLATE SQL_Latin1_General_CP1_CI_AS = C.ItemCode AND 
                        B.LineNum = C.LineNum
                INNER JOIN 
                ( 
                    SELECT A.ItemCode, A.ItemName, QRMngBy, ItemMngBy  
                    FROM QIT_Item_Master A 
                ) D ON B.ItemCode COLLATE SQL_Latin1_General_CP1_CI_AS  = D.ItemCode 
                WHERE A.Series = @series and ISNULL(A.BPLId, @bId) = @bId " + _w + @"
                ORDER BY A.DocEntry,C.GateInNo , B.LineNum";
                #endregion

                _logger.LogInformation(" PurchaseOrdersController : GetGateInDetails() Query : {q} ", _Query.ToString());
                QITcon.Open();
                oAdptr = new SqlDataAdapter(_Query, QITcon);

                if (payload.DocNum > 0)
                    oAdptr.SelectCommand.Parameters.AddWithValue("@docNum", payload.DocNum);
                oAdptr.SelectCommand.Parameters.AddWithValue("@series", payload.Series);
                oAdptr.SelectCommand.Parameters.AddWithValue("@bId", payload.BranchID);
                if (payload.GateInNo > 0)
                {
                    oAdptr.SelectCommand.Parameters.AddWithValue("@gateInNo", payload.GateInNo);
                }
                oAdptr.Fill(dtData);
                QITcon.Close();

                if (dtData.Rows.Count > 0)
                {
                    List<PODetail> oPODet = new List<PODetail>();
                    dynamic arPODet = Newtonsoft.Json.JsonConvert.SerializeObject(dtData);
                    oPODet = Newtonsoft.Json.JsonConvert.DeserializeObject<List<PODetail>>(arPODet.ToString());

                    obj.Add(new POHeader()
                    {
                        DocEntry = int.Parse(dtData.Rows[0]["DocEntry"].ToString()),
                        DocNum = int.Parse(dtData.Rows[0]["DocNum"].ToString()),
                        ObjType = dtData.Rows[0]["ObjType"].ToString(),
                        DocDate = dtData.Rows[0]["DocDate"].ToString(),
                        CardCode = dtData.Rows[0]["CardCode"].ToString(),
                        CardName = dtData.Rows[0]["CardName"].ToString(),
                        U_LRNo = dtData.Rows[0]["U_LRNo"].ToString(),
                        FreightApplied = dtData.Rows[0]["FreightApplied"].ToString(),
                        poDet = oPODet
                    });

                    return obj;
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "No data found" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(" Error in PurchaseOrdersController : GetGateInDetails() :: {Error}", ex.ToString());
                return BadRequest(new
                {
                    StatusCode = "400",
                    StatusMsg = ex.ToString()
                });
            }
        }


        #endregion


        #region Generate and Print Form

        [HttpGet("FillGateInNo")]
        public async Task<ActionResult<IEnumerable<FillGateInNo>>> FillGateInNo()
        {
            SqlConnection QITcon;
            SqlDataAdapter oAdptr;
            try
            {
                _logger.LogInformation(" Calling PurchaseOrdersController : FillGateInNo() ");

                System.Data.DataTable dtGateInNo = new System.Data.DataTable();
                QITcon = new SqlConnection(_QIT_connection);

                _Query = @" SELECT DISTINCT A.GateInNo FROM QIT_GateIN A WHERE A.Canceled = 'N' ";
                _logger.LogInformation(" PurchaseOrdersController : FillGateInNo() Query : {q} ", _Query.ToString());
                QITcon.Open();
                oAdptr = new SqlDataAdapter(_Query, QITcon);
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
                _logger.LogError(" Error in PurchaseOrdersController : FillGateInNo() :: {Error}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
            }
        }


        [HttpPost("GetItemsByGateInNo")]
        public IActionResult GetItemsByGateInNo(GPPayload payload)
        {
            SqlConnection QITcon;
            SqlDataAdapter oAdptr;
            try
            {
                // In payload, BranchId, GateInNo and Canceld values are required
                _logger.LogInformation(" Calling PurchaseOrdersController : GetItemsByGateInNo() ");

                List<POHeader> obj = new List<POHeader>();
                System.Data.DataTable dtData = new System.Data.DataTable();
                QITcon = new SqlConnection(_QIT_connection);

                #region Validation

                if (payload.BranchID <= 0 || payload.BranchID == null)
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Provide Branch" });
                }
                if (payload.GateInNo <= 0 || payload.GateInNo == null)
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Provide GateIn No" });
                }

                #region Check Gate IN No

                System.Data.DataTable dtGateIn = new System.Data.DataTable();
                QITcon = new SqlConnection(_QIT_connection);
                _Query = @" SELECT * FROM QIT_GateIN A where GateInNo = @gateInNo and ISNULL(A.BranchId, @bId) = @bId ";
                _logger.LogInformation(" SupplierGateInController : GateIN No Query : {q} ", _Query.ToString());
                QITcon.Open();
                oAdptr = new SqlDataAdapter(_Query, QITcon);
                oAdptr.SelectCommand.Parameters.AddWithValue("@bId", payload.BranchID);
                oAdptr.SelectCommand.Parameters.AddWithValue("@gateInNo", payload.GateInNo);
                oAdptr.Fill(dtGateIn);
                QITcon.Close();

                if (dtGateIn.Rows.Count <= 0)
                    return BadRequest(new
                    {
                        StatusCode = "400",
                        StatusMsg = "No such Gate IN exist"
                    });

                if (dtGateIn.Rows[0]["GateIn"].ToString().ToUpper() == "N" && dtGateIn.Rows[0]["VendorGateIn"].ToString().ToUpper() == "Y")
                    return BadRequest(new
                    {
                        StatusCode = "400",
                        StatusMsg = "This vendor GateIn is pending to be confirmed"
                    });

                #endregion

                #endregion

                #region Query

                _Query = @" 
                SELECT A.Series, A.DocEntry, A.DocNum, B.LineNum, A.ObjType, cast(CONVERT(DATE, A.DocDate) as nvarchar(10)) DocDate, 
                       A.CardCode, A.CardName, 
                       B.LineNum, B.ItemCode, B.Dscription ItemName, CAST(B.Quantity as numeric(19,3)) Qty, 
                       CAST(B.Price as numeric(19,3)) Price, CAST(ISNULL(C.RecQty,0) as numeric(19,3)) OpenQty, 
                       D.ItemMngBy, D.QRMngBy, 
                       CAST((select CONVERT(DATE,MAX(Z.RecDate)) from QIT_GateIN Z where Z.GateInNo = C.GateInNo) as nvarchar(10)) RecDate,
                       C.GateInNo, B.Project, B.unitMsr UomCode, B.WhsCode, A.U_LRNo,
                       case when (select count(1) FROM " + Global.SAP_DB + @".dbo.POR3 WHERE DocEntry = A.DocEntry) > 0 then 'Y' else 'N' end FreightApplied
                FROM " + Global.SAP_DB + @".dbo.OPOR A inner join " + Global.SAP_DB + @".dbo.POR1 B on A.DocEntry = B.DocEntry 
                INNER JOIN 
                ( 
                    SELECT AA.GateInNo, AA.DocEntry, AA.ObjType, AA.ItemCode, AA.LineNum, AA.BranchID, RecQty--, RecDate  
                    FROM QIT_GateIN AA 
                    WHERE ISNULL(AA.BranchID, @bId) = @bId and AA.ObjType = '22' AND AA.GateInNo = @gateInNo AND 
                          AA.Canceled = 'N' AND AA.GateIN = 'Y'
                ) C ON A.ObjType COLLATE SQL_Latin1_General_CP1_CI_AS = C.ObjType AND 
                        A.DocEntry = C.DocEntry AND ISNULL(A.BPLId,1) = C.BranchID AND 
                        B.ItemCode COLLATE SQL_Latin1_General_CP1_CI_AS = C.ItemCode AND 
                        B.LineNum = C.LineNum
                INNER JOIN 
                ( 
                    SELECT A.ItemCode, A.ItemName, QRMngBy, ItemMngBy  
                    FROM QIT_Item_Master A 
                ) D ON B.ItemCode COLLATE SQL_Latin1_General_CP1_CI_AS  = D.ItemCode 
                WHERE ISNULL(A.BPLId, @bId) = @bId AND A.Canceled = 'N'
                ORDER BY A.DocEntry,C.GateInNo , B.LineNum";

                #endregion

                _logger.LogInformation(" PurchaseOrdersController : GetItemsByGateInNo() Query : {q} ", _Query.ToString());
                QITcon.Open();
                oAdptr = new SqlDataAdapter(_Query, QITcon);
                oAdptr.SelectCommand.Parameters.AddWithValue("@bId", payload.BranchID);
                oAdptr.SelectCommand.Parameters.AddWithValue("@gateInNo", payload.GateInNo);
                oAdptr.Fill(dtData);
                QITcon.Close();

                if (dtData.Rows.Count > 0)
                {
                    var result = dtData.AsEnumerable()
                    .GroupBy(row => new { GateInNo = row.Field<int>("GateInNo"), Series = row.Field<int>("Series"), RecDate = row.Field<string>("RecDate") })
                    .Select(g => new GP_GateInDetails
                    {
                        BranchID = payload.BranchID, // You may set the appropriate BranchID here
                        GateInNo = g.Key.GateInNo,
                        GateInDate = g.Key.RecDate,
                        gpPOHeader = g.GroupBy(h => new
                        {
                            DocEntry = h.Field<int>("DocEntry"),
                            DocNum = h.Field<int>("DocNum"),
                            Series = h.Field<int>("Series"),
                            ObjType = h.Field<string>("ObjType"),
                            DocDate = h.Field<string>("DocDate"),
                            CardCode = h.Field<string>("CardCode"),
                            CardName = h.Field<string>("CardName")
                        })
                        .Select(h => new GP_POHeader
                        {
                            DocEntry = h.Key.DocEntry,
                            DocNum = h.Key.DocNum,
                            Series = h.Key.Series,
                            ObjType = h.Key.ObjType,
                            DocDate = h.Key.DocDate,
                            CardCode = h.Key.CardCode,
                            CardName = h.Key.CardName,
                            gpPODetail = h.Select(d => new GP_PODetail
                            {
                                DocEntry = d.Field<int>("DocEntry"),
                                LineNum = d.Field<int>("LineNum"),
                                ItemCode = d.Field<string>("ItemCode"),
                                ItemName = d.Field<string>("ItemName"),
                                Qty = d.Field<decimal>("Qty"),
                                OpenQty = d.Field<decimal>("OpenQty"),
                                Price = d.Field<decimal>("Price"),
                                ItemMngBy = d.Field<string>("ItemMngBy"),
                                QRMngBy = d.Field<string>("QRMngBy"),
                                RecDate = d.Field<string>("RecDate"),
                                Project = d.Field<string>("Project"),
                                UomCode = d.Field<string>("UomCode"),
                                WhsCode = d.Field<string>("WhsCode")
                            }).ToList()
                        }).ToList()
                    }).ToList();

                    return Ok(result);
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "No data found" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(" Error in PurchaseOrdersController : GetItemsByGateInNo() :: {Error}", ex.ToString());
                return BadRequest(new
                {
                    StatusCode = "400",
                    StatusMsg = ex.ToString()
                });
            }
        }

        #endregion
    }
}
