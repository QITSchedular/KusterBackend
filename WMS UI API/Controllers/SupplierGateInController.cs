using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using SAPbobsCOM;
using System.Data;
using System.Data.SqlClient;
using System.Xml.Linq;
using WMS_UI_API.Common;
using WMS_UI_API.Models;

namespace WMS_UI_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SupplierGateInController : ControllerBase
    {
        private string _ApplicationApiKey = string.Empty;
        private string _connection = string.Empty;
        private string _QIT_connection = string.Empty;
        private string _QIT_DB = string.Empty;
        private string _Query = string.Empty;
        private SqlCommand cmd;
        public Global objGlobal;

        public IConfiguration Configuration { get; }
        private readonly ILogger<SupplierGateInController> _logger;

        public SupplierGateInController(IConfiguration configuration, ILogger<SupplierGateInController> logger)
        {
            if (objGlobal == null)
                objGlobal = new Global();
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
                _logger.LogError("Error in SupplierGateInController :: {Error}" + ex.ToString());
            }
        }


        #region Save GateIN at Vendor end

        [HttpGet("GetPOHelp")]
        public async Task<ActionResult<IEnumerable<POHelp>>> GetPOHelp(int BranchID, string CardCode)
        {
            SqlConnection QITcon;
            SqlDataAdapter oAdptr;
            try
            {
                _logger.LogInformation(" Calling SupplierGateInController : GetPOHelp() ");

                #region Validation

                if (BranchID <= 0)
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Provide BranchID" });
                }

                if (CardCode.ToString().Length <= 0 || CardCode.ToString().ToLower() == "string")
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Provide Supplier Code" });
                }

                #endregion

                #region Query

                QITcon = new SqlConnection(_QIT_connection);
                System.Data.DataTable dtPO = new System.Data.DataTable();
                _Query = @"
                WITH PO_RecQty AS (
                    SELECT A.DocEntry, A.DocNum,  CAST(SUM(B.Quantity) as numeric(19,3)) AS POQty 
                    FROM " + Global.SAP_DB + @".dbo.OPOR A
                    INNER JOIN " + Global.SAP_DB + @".dbo.POR1 B ON A.DocEntry = B.DocEntry
                    WHERE A.CardCode = @cardCode AND CANCELED = 'N' AND DocStatus = 'O' AND DocType = 'I' and 
                          B.LineStatus = 'O' and ISNULL(A.BPLId, @bId) = @bId
                    GROUP BY A.DocEntry, A.DocNum
                ),
                Rec_RecQty AS (
                    SELECT DocEntry, CAST(SUM(RecQty) as numeric(19,3)) AS RecQty
                    FROM QIT_GateIN 
                    WHERE ObjType = '22' and ISNULL(BranchID, @bId) = @bId and Canceled = 'N'
                    GROUP BY DocEntry
                ) 
                SELECT A.DocEntry, A.DocNum, B.DocDate, B.CardCode, B.CardName, B.Series, C.SeriesName,
                       case when (select count(1) FROM " + Global.SAP_DB + @".dbo.POR3 WHERE DocEntry = A.DocEntry) > 0 then 'Y' else 'N' end FreightApplied
                FROM 
                (
                    SELECT PO.DocEntry, PO.DocNum, CAST(PO.POQty as numeric(19,3)) POQty, CAST(ISNULL(Rec.RecQty, 0) as numeric(19,3)) AS RecQty
                    FROM PO_RecQty PO
                    LEFT JOIN Rec_RecQty Rec ON PO.DocEntry = Rec.DocEntry
                    WHERE PO.POQty <> ISNULL(Rec.RecQty, 0)
                ) as A 
                INNER JOIN " + Global.SAP_DB + @".dbo.OPOR B ON A.DocEntry = B.DocEntry    
                INNER JOIN " + Global.SAP_DB + @".dbo.NNM1 C ON C.Series = B.Series    
                ";
                _logger.LogInformation(" SupplierGateInController : Query : {q} ", _Query.ToString());
                QITcon.Open();
                oAdptr = new SqlDataAdapter(_Query, QITcon);
                oAdptr.SelectCommand.Parameters.AddWithValue("@bId", BranchID);

                oAdptr.SelectCommand.Parameters.AddWithValue("@cardCode", CardCode);
                oAdptr.Fill(dtPO);
                QITcon.Close();

                #endregion

                if (dtPO.Rows.Count > 0)
                {
                    List<POHelp> obj = new List<POHelp>();
                    dynamic arData = JsonConvert.SerializeObject(dtPO);
                    obj = JsonConvert.DeserializeObject<List<POHelp>>(arData.ToString().Replace("~", " "));
                    return obj;
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "No data found" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(" Error in SupplierGateInController : GetPOHelp() :: {Error}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
            }
        }


        [HttpPost("GetPOItems")]
        public async Task<ActionResult<IEnumerable<POItems>>> GetPOItems(SDocList payload)
        {
            SqlConnection QITcon;
            SqlDataAdapter oAdptr;
            try
            {

                _logger.LogInformation(" Calling SupplierGateInController : GetPOItems() ");

                #region Validation

                if (payload.BranchID <= 0 || payload.BranchID == null)
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Provide Branch" });
                }

                if (payload.Series <= 0 || payload.Series == null)
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Provide Series" });
                }

                if (payload.CardCode.ToString().Length <= 0 || payload.CardCode.ToString().ToLower() == "string")
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Provide Supplier Code" });
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
                _logger.LogInformation(" SupplierGateInController : Cardcode Query : {q} ", _Query.ToString());
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
                _logger.LogInformation(" SupplierGateInController : TaxCode Query : {q} ", _Query.ToString());
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
                            WHERE ISNULL(AA.BranchID, @bId) = @bId and AA.ObjType = '22' and AA.CANCELED = 'N' and 
                                  AA.DocEntry IN(" + dynamicDocEntries + @")
                            GROUP BY AA.DocEntry, AA.ObjType, AA.ItemCode, AA.LineNum, AA.BranchID 
                        ) C ON A.ObjType COLLATE SQL_Latin1_General_CP1_CI_AS = C.ObjType AND 
                                A.DocEntry = C.DocEntry AND ISNULL(A.BPLId,1) = C.BranchID AND 
                                B.ItemCode COLLATE SQL_Latin1_General_CP1_CI_AS = C.ItemCode AND
                                B.LineNum = C.LineNum
                        INNER JOIN  
                        ( 
                            SELECT A.ItemCode, A.ItemName, QRMngBy, ItemMngBy  
                            FROM QIT_Item_Master A 
                        ) D ON B.ItemCode COLLATE SQL_Latin1_General_CP1_CI_AS  = D.ItemCode 
                        WHERE A.DocEntry IN(" + dynamicDocEntries + @") and A.Series = @series and ISNULL(A.BPLId, @bId) = @bId AND 
                                A.CANCELED = 'N' AND B.LineStatus = 'O'
                    ) as A
					ORDER BY A.DocEntry, A.LineNum  ";


                _logger.LogInformation(" SupplierGateInController : GetPOItems() Query : {q} ", _Query.ToString());
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
                _logger.LogError(" Error in SupplierGateInController : GetPOItems() :: {Error}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
            }
        }


        [HttpPost("SaveVendorGateIN")]
        public IActionResult SaveVendorGateIN(SaveVendorGateIN payload)
        {
            string _IsSaved = "N";
            SqlConnection QITcon;
            SqlDataAdapter oAdptr;
            object _vendorGateInNo = 0;
            try
            {
                _logger.LogInformation(" Calling SupplierGateInController : SaveVendorGateIN() ");

                int _totalGateIN = 0;
                int _SaveVendorGateIN = 0;


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
                        WHERE A.DocEntry = @docEntry and 
                              B.ItemCode = @iCode and B.LineNum = @line and ISNULL(A.BPLId, @bid) = @bid
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
                                dtPO.Rows[0]["UomCode"].ToString().ToLower() == "cu.ft." ||
                                dtPO.Rows[0]["UomCode"].ToString().ToLower() == "mtr" ||
                                dtPO.Rows[0]["UomCode"].ToString().ToLower() == "mtr." ||
                                dtPO.Rows[0]["UomCode"].ToString().ToLower() == "rft" ||
                                dtPO.Rows[0]["UomCode"].ToString().ToLower() == "sq. ft" ||
                                dtPO.Rows[0]["UomCode"].ToString().ToLower() == "sq. ft." ||
                                dtPO.Rows[0]["UomCode"].ToString().ToLower() == "sq. mtr" ||
                                dtPO.Rows[0]["UomCode"].ToString().ToLower() == "sq. mtr." ||
                                dtPO.Rows[0]["UomCode"].ToString().ToLower() == "sq.f" ||
                                dtPO.Rows[0]["UomCode"].ToString().ToLower() == "sq.ft" ||
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

                            _Query = @" 
                            SELECT CAST(SUM(RecQty) as numeric(19,3)) RecQty 
                            FROM QIT_GateIN A  
                            WHERE A.DocEntry = @docEntry and A.ItemCode = @iCode AND 
                                  A.LineNum = @line and A.BranchID = @bid and A.ObjType = @objType and A.Canceled = 'N' and A.VendorGateIN = 'Y'
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

                    #region Get Vendor Gate IN No

                    QITcon = new SqlConnection(_QIT_connection);
                    _Query = @" SELECT ISNULL(max(GateInNo),0) + 1 FROM QIT_GateIN A  ";
                    _logger.LogInformation(" SupplierGateInController : Get GateIN No Query : {q} ", _Query.ToString());
                    cmd = new SqlCommand(_Query, QITcon);
                    QITcon.Open();
                    _vendorGateInNo = cmd.ExecuteScalar();
                    QITcon.Close();

                    #endregion

                    foreach (var itemDet in payload.gateInDetails)
                    {
                        QITcon = new SqlConnection(_QIT_connection);
                        _Query = @" 
                        INSERT INTO QIT_GateIN
                        (
                            SrNo, GateInNo, BranchID, ObjType, DocEntry, DocNum, Series, LineNum, ItemCode, 
                            GateIN, RecQty, RecDate, Project, WhsCode, UoMCode, ToleranceApplied, Canceled, VendorGateIN,
                            EntryDate 
                        )
                        VALUES
                        (   (select ISNULL(max(SrNo),0) + 1 from QIT_GateIN), 
                            @vgateInNo, @bID, @objType, @docEntry, @docNum, @series, @line, @iCode, 
                             @gateIN, @recQty, @recDate, @proj, @whs, @uom, @tolApplied, @cancel, 'Y', 
                            @entryDate 
                        )
                        ";
                        _logger.LogInformation(" SupplierGateInController : SaveVendorGateIN() Query : {q} ", _Query.ToString());

                        cmd = new SqlCommand(_Query, QITcon);
                        cmd.Parameters.AddWithValue("@vgateInNo", _vendorGateInNo);
                        cmd.Parameters.AddWithValue("@bID", payload.BranchID);
                        cmd.Parameters.AddWithValue("@objType", payload.ObjType);
                        cmd.Parameters.AddWithValue("@docEntry", itemDet.DocEntry);
                        cmd.Parameters.AddWithValue("@docNum", itemDet.DocNum);
                        cmd.Parameters.AddWithValue("@series", payload.Series);
                        cmd.Parameters.AddWithValue("@line", itemDet.LineNum);
                        cmd.Parameters.AddWithValue("@iCode", itemDet.ItemCode);
                        cmd.Parameters.AddWithValue("@gateIN", "N");
                        cmd.Parameters.AddWithValue("@recQty", itemDet.RecQty);
                        cmd.Parameters.AddWithValue("@recDate", DateTime.Now);
                        cmd.Parameters.AddWithValue("@proj", itemDet.Project);
                        cmd.Parameters.AddWithValue("@uom", itemDet.UomCode);
                        cmd.Parameters.AddWithValue("@whs", itemDet.WhsCode);

                        cmd.Parameters.AddWithValue("@tolApplied", itemDet.TolApplied);
                        cmd.Parameters.AddWithValue("@cancel", "N");

                        cmd.Parameters.AddWithValue("@entryDate", DateTime.Now);
                        //cmd.Parameters.AddWithValue("@entryUser", payload.EntryUser);

                        QITcon.Open();
                        int intNum = cmd.ExecuteNonQuery();
                        QITcon.Close();

                        if (intNum > 0)
                        {
                            _IsSaved = "Y";
                            _SaveVendorGateIN = _SaveVendorGateIN + 1;
                        }
                        else
                        {
                            _IsSaved = "N";
                            bool bln = DeleteVendorGateIN((int)_vendorGateInNo);
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

                    if (_totalGateIN == _SaveVendorGateIN && _SaveVendorGateIN > 0)
                    {
                        return Ok(new
                        {
                            StatusCode = "200",
                            IsSaved = _IsSaved,
                            VendorGateInNo = _vendorGateInNo,
                            StatusMsg = "Saved Successfully!!!"
                        });
                    }
                    else
                    {
                        return BadRequest(new { StatusCode = "400", IsSaved = "N", StatusMsg = "Problem in saving GateIn data" });
                    }
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = " Details not found" });
                }
            }
            catch (Exception ex)
            {
                bool bln = DeleteVendorGateIN((int)_vendorGateInNo);
                _logger.LogError(" Error in SupplierGateInController : SaveVendorGateIN() :: {Error}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
            }
        }


        private bool DeleteVendorGateIN(int p_VendorGateInNo)
        {
            SqlConnection QITcon;
            try
            {
                _logger.LogInformation(" Calling SupplierGateInController : DeleteVendorGateIN() ");
                string p_ErrorMsg = string.Empty;

                QITcon = new SqlConnection(_QIT_connection);
                _Query = @" DELETE FROM QIT_GateIN WHERE GateInNo = @gateInNo ";
                _logger.LogInformation(" SupplierGateInController : DeleteVendorGateIN Query : {q} ", _Query.ToString());

                cmd = new SqlCommand(_Query, QITcon);
                cmd.Parameters.AddWithValue("@gateInNo", p_VendorGateInNo);
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
                _logger.LogError("Error in SupplierGateInController : DeleteVendorGateIN() :: {Error}", ex.ToString());
                return false;
            }
        }

        #endregion


        #region Handle Vendor Gate In at Kuster end 

        [HttpGet("ValidatePOQR")]
        public async Task<ActionResult<IEnumerable<HeaderQRData>>> ValidatePOQR(string HeaderQR)
        {
            SqlConnection QITcon;
            SqlDataAdapter oAdptr;
            try
            {
                _logger.LogInformation(" Calling SupplierGateInController : ValidatePOQR() ");
                QITcon = new SqlConnection(_QIT_connection);
                System.Data.DataTable dtData = new System.Data.DataTable();

                _Query = @" SELECT * FROM QIT_QR_Header WHERE QRCodeID = @headerQR  ";
                _logger.LogInformation(" SupplierGateInController : ValidatePOQR() Query : {q} ", _Query.ToString());

                oAdptr = new SqlDataAdapter(_Query, QITcon);
                oAdptr.SelectCommand.Parameters.AddWithValue("@headerQR", HeaderQR.Replace(" ", "~"));
                QITcon.Open();
                oAdptr.Fill(dtData);
                QITcon.Close();

                if (dtData.Rows.Count > 0)
                {
                    dtData = new System.Data.DataTable();
                    _Query = @"
                    SELECT A.DocEntry, A.DocNum, A.Series, A.SeriesName, A.CardCode, A.CardName, A.QRCodeID, A.GateInNo, 
                           COUNT(A.ItemCode) ItemCount, SUM(A.RecQty) TotalRecQty 
                    FROM 
                    (
                        SELECT A.DocEntry, A.DocNum, A.Series, C.SeriesName, D.CardCode, D.CardName, A.QRCodeID, 
                               B.GateInNo, E.ItemCode, E.RecQty
                        FROM QIT_QR_Header A
                             INNER JOIN QIT_QR_Detail B ON A.HeaderSrNo = B.HeaderSrNo
                             INNER JOIN " + Global.SAP_DB + @".dbo.NNM1 C ON C.Series = A.Series
                             INNER JOIN " + Global.SAP_DB + @".dbo.OPOR D ON A.DocEntry = D.DocEntry
                             INNER JOIN QIT_GateIN E ON E.GateInNo = B.GateInNo and E.ItemCode = B.ItemCode and E.LineNum = B.LineNum and 
		                           E.DocEntry = A.DocEntry and E.VendorGateIN = 'Y' and E.GateIN = 'N'
                        WHERE A.QRCodeID = @headerQR and B.VendorGateIN IN ('Y') 
                    ) AS A
                    GROUP BY A.DocEntry, A.DocNum, A.Series, A.SeriesName, A.CardCode, A.CardName, A.QRCodeID, A.GateInNo
                    ";

                    _logger.LogInformation(" SupplierGateInController : ValidatePOQR() Query : {q} ", _Query.ToString());

                    QITcon.Open();
                    oAdptr = new SqlDataAdapter(_Query, QITcon);
                    oAdptr.SelectCommand.Parameters.AddWithValue("@headerQR", HeaderQR.Replace(" ", "~"));

                    oAdptr.Fill(dtData);
                    QITcon.Close();

                    if (dtData.Rows.Count > 0)
                    {
                        List<HeaderQRData> obj = new List<HeaderQRData>();
                        dynamic arData = JsonConvert.SerializeObject(dtData);
                        obj = JsonConvert.DeserializeObject<List<HeaderQRData>>(arData.ToString().Replace("~", " "));
                        return obj;
                    }
                    else
                    {
                        return BadRequest(new { StatusCode = "400", StatusMsg = "No data found" });
                    }
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", IsExist = "N", StatusMsg = "Header QR does not exist" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(" Error in SupplierGateInController : ValidatePOQR() :: {Error}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
            }
        }


        [HttpPost("ValidatePOItemQR")]
        public async Task<ActionResult<IEnumerable<ValidPOItemQRData>>> ValidatePOItemQR(ValidatePOItemQR payload)
        {
            SqlConnection QITcon;
            SqlDataAdapter oAdptr;
            try
            {
                _logger.LogInformation(" Calling SupplierGateInController : ValidatePOItemQR() ");
                QITcon = new SqlConnection(_QIT_connection);
                System.Data.DataTable dtData = new System.Data.DataTable();


                #region Validate Header QR

                _Query = @" SELECT * FROM QIT_QR_Header WHERE QRCodeID = @headerQR  ";
                _logger.LogInformation(" SupplierGateInController : ValidatePOItemQR() Query : {q} ", _Query.ToString());

                oAdptr = new SqlDataAdapter(_Query, QITcon);
                oAdptr.SelectCommand.Parameters.AddWithValue("@headerQR", payload.HeaderQRCodeID.Replace(" ", "~"));
                QITcon.Open();
                oAdptr.Fill(dtData);
                QITcon.Close();

                if (dtData.Rows.Count <= 0)
                {
                    return BadRequest(new { StatusCode = "400", IsExist = "N", StatusMsg = "Header QR does not exist" });
                }
                #endregion


                #region Get Data
                dtData = new System.Data.DataTable();
                _Query = @" 
                SELECT B.BranchID, A.DocEntry, A.DocNum, A.QRCodeID HeaderQRCodeId, B.QRCodeId DetailQRCodeId, 
                       B.GateInNo, A.Series, A.DocDate, E.CardCode, E.CardName,
	                   B.ItemCode, D.ItemName, B.LineNum, 
	                   CAST(F.Quantity as numeric(19,3)) OrderedQty, CAST(C.RecQty as numeric(19,3)) RecQty, 
                       CAST(F.Price as numeric(19,3)) Price,  
	                   G.ItemMngBy, G.QRMngBy, F.Project, F.unitMsr UomCode, F.WhsCode,
	                   CASE WHEN ( select count(1) FROM " + Global.SAP_DB + @".dbo.POR3 WHERE DocEntry = A.DocEntry) > 0 then 'Y' 
                            else 'N' end FreightApplied
                FROM QIT_QR_Header A
                     INNER JOIN QIT_QR_Detail B ON A.HeaderSrNo = B.HeaderSrNo
                     INNER JOIN QIT_GateIN C ON C.GateInNo = B.GateInNo and C.VendorGateIN = 'Y' and C.GateIN = 'N' and
		                   C.ItemCode = B.ItemCode and C.LineNum = B.LineNum and C.DocEntry = A.DocEntry
	                 INNER JOIN " + Global.SAP_DB + @".dbo.OITM D ON D.ItemCode collate SQL_Latin1_General_CP1_CI_AS = C.ItemCode
	                 INNER JOIN " + Global.SAP_DB + @".dbo.OPOR E ON E.DocEntry = A.DocEntry and E.DocEntry = C.DocEntry and 
		                   E.ObjType collate SQL_Latin1_General_CP1_CI_AS = C.ObjType and E.ObjType collate SQL_Latin1_General_CP1_CI_AS = A.ObjType
	                 INNER JOIN " + Global.SAP_DB + @".dbo.POR1 F ON E.DocEntry = F.DocEntry and F.ItemCode = D.ItemCode
	                 INNER JOIN QIT_Item_Master G ON G.ItemCode collate SQL_Latin1_General_CP850_CI_AS = F.ItemCode 
                WHERE A.QRCodeID = @headerQR and B.QRCodeID = @dQR and B.GateInNo = @gateInNo and A.DocEntry = @docEntry and
                      B.VendorGateIN IN ('Y') and C.VendorGateIN IN ('Y') and C.GateIN = 'N' and ISNULL(A.BranchId, @bId) = @bId
                ";
                _logger.LogInformation(" SupplierGateInController : ValidatePOItemQR() Query : {q} ", _Query.ToString());

                oAdptr = new SqlDataAdapter(_Query, QITcon);
                oAdptr.SelectCommand.Parameters.AddWithValue("@headerQR", payload.HeaderQRCodeID.Replace(" ", "~"));
                oAdptr.SelectCommand.Parameters.AddWithValue("@dQR", payload.DetailQRCodeID.Replace(" ", "~"));
                oAdptr.SelectCommand.Parameters.AddWithValue("@gateInNo", payload.GateInNo);
                oAdptr.SelectCommand.Parameters.AddWithValue("@docEntry", payload.PODocEntry);
                oAdptr.SelectCommand.Parameters.AddWithValue("@bId", payload.BranchId);
                QITcon.Open();
                oAdptr.Fill(dtData);
                QITcon.Close();

                #endregion

                if (dtData.Rows.Count > 0)
                {
                    List<ValidPOItemQRData> obj = new List<ValidPOItemQRData>();
                    dynamic arData = JsonConvert.SerializeObject(dtData);
                    obj = JsonConvert.DeserializeObject<List<ValidPOItemQRData>>(arData.ToString().Replace("~", " "));
                    return obj;
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", IsExist = "N", StatusMsg = "No data found" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(" Error in SupplierGateInController : ValidatePOItemQR() :: {Error}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
            }
        }


        [HttpPut("UpdateVendorGateIN")]
        public IActionResult UpdateVendorGateIN(UpdateVendorGateIN payload)
        {
            string _IsSaved = "N";
            SqlConnection QITcon;
            SqlDataAdapter oAdptr;

            try
            {
                _logger.LogInformation(" Calling SupplierGateInController : UpdateVendorGateIN() ");
                  
                if (payload != null)
                { 
                    #region Check Gate IN No

                    DataTable dtGateIn = new DataTable();
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
                            IsSaved = _IsSaved,
                            StatusMsg = "No such Gate IN exist"
                        });

                    if (dtGateIn.Rows[0]["GateIn"].ToString().ToUpper() == "Y")
                        return BadRequest(new
                        {
                            StatusCode = "400",
                            IsSaved = _IsSaved,
                            StatusMsg = "Gate IN already done for the Gate In : " + payload.GateInNo
                        });

                    #endregion


                    QITcon = new SqlConnection(_QIT_connection);
                    _Query = @" 
                    UPDATE QIT_GateIN
                    SET VehicleNo = @vNo,
                        TransporterCode = @tCode,
                        LRNo = @lNo,
                        LRDate = @lDate,
                        GRPOVendorRefNo = @iNo,
                        GRPODocDate = @iDate,
                        RecDate = @recDate,
                        GateIn = 'Y'
                    WHERE BranchId = @bId and GateInNo = @gateInNo and GateIn = 'N'
                    ";
                    
                    _logger.LogInformation(" SupplierGateInController : UpdateVendorGateIN() Query : {q} ", _Query.ToString());

                    cmd = new SqlCommand(_Query, QITcon);
                    cmd.Parameters.AddWithValue("@vNo", payload.VehicleNo);
                    cmd.Parameters.AddWithValue("@tCode", payload.Transporter);
                    cmd.Parameters.AddWithValue("@lNo", payload.LRNo);
                    cmd.Parameters.AddWithValue("@lDate", payload.LRDate == string.Empty ? DBNull.Value : payload.LRDate);
                    cmd.Parameters.AddWithValue("@iNo", payload.GRPOVendorRefNo);
                    cmd.Parameters.AddWithValue("@iDate", payload.GRPODocDate);
                    cmd.Parameters.AddWithValue("@recDate", DateTime.Now);
                    cmd.Parameters.AddWithValue("@bId", payload.BranchID);
                    cmd.Parameters.AddWithValue("@gateInNo", payload.GateInNo);
                   
                    QITcon.Open();
                    int intNum = cmd.ExecuteNonQuery();
                    QITcon.Close();

                    if (intNum > 0)
                    {
                        return Ok(new
                        {
                            StatusCode = "200",
                            IsSaved = _IsSaved,
                            VendorGateInNo = payload.GateInNo,
                            StatusMsg = "Updated Successfully!!!"
                        });
                    }
                    else
                    {
                        _IsSaved = "N"; 
                        return BadRequest(new
                        {
                            StatusCode = "400",
                            IsSaved = "N",
                            StatusMsg = "Problem in updating data for :" + Environment.NewLine +
                                        "Gate In No  : " + payload.GateInNo  
                        }); 
                    } 
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = " Details not found" });
                }
            }
            catch (Exception ex)
            { 
                _logger.LogError(" Error in SupplierGateInController : UpdateVendorGateIN() :: {Error}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
            }
        }

        #endregion
    }
}
