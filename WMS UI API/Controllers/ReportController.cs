using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;
using System.Data.SqlClient;
using System.Security.Cryptography;
using WMS_UI_API.Common;
using WMS_UI_API.Models;
using static WMS_UI_API.Models.GRN_Qc_DetailsReport;

namespace WMS_UI_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReportController : ControllerBase
    {
        private string _ApplicationApiKey = string.Empty;
        private string _connection = string.Empty;
        private string _QIT_connection = string.Empty;
        private string _QIT_DB = string.Empty;
        private string _Query = string.Empty;
         
        SqlConnection QITcon;
        SqlDataAdapter oAdptr;
        private SqlCommand cmd;
        public Global objGlobal;

        public IConfiguration Configuration { get; }
        private readonly ILogger<ReportController> _logger;

        public ReportController(IConfiguration configuration, ILogger<ReportController> logger)
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
                _logger.LogError(" Error in ReportController :: {Error}" + ex.ToString());
            }
        }


        #region Purchase Order Wise GateIN Report

        [HttpGet("GetPOList")]
        public async Task<ActionResult<IEnumerable<PoIList>>> GetPOList()
        {
            try
            {
                _logger.LogInformation(" Calling ReportController : GetPOList() ");
                List<PoIList> obj = new List<PoIList>();
                System.Data.DataTable dtData = new System.Data.DataTable();
                QITcon = new SqlConnection(_QIT_connection);

                string _where = string.Empty;

                _Query = @"SELECT O.DocEntry, DocNUm, CardCode, CardName  
                         FROM " + Global.SAP_DB + @".dbo.OPOR O 
                              INNER JOIN(SELECT DISTINCT DocEntry, ObjType, BranchID FROM [QIT_GateIN] where Canceled= 'N' ) AS UniqueQIT_GateIn
                          ON O.DocEntry = UniqueQIT_GateIn.DocEntry AND O.ObjType COLLATE SQL_Latin1_General_CP1_CI_AS = UniqueQIT_GateIn.ObjType AND BPLId = UniqueQIT_GateIn.BranchID ";

                _logger.LogInformation(" ReportController : GetPOList() Query : {q} ", _Query.ToString());
                QITcon.Open();
                oAdptr = new SqlDataAdapter(_Query, QITcon);

                oAdptr.Fill(dtData);
                QITcon.Close();

                if (dtData.Rows.Count > 0)
                {
                    dynamic arData = JsonConvert.SerializeObject(dtData);
                    obj = JsonConvert.DeserializeObject<List<PoIList>>(arData.ToString());
                    return obj;
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "No data found" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(" Error in ReportController : GetPOList() :: {Error}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
            }
        }


        [HttpPost("GateInDetails")]
        public async Task<ActionResult<IEnumerable<GateInDetailsReport>>> GateInDetails(GateInDetails payload)
        {
            try
            {
                _logger.LogInformation(" Calling ReportController : GateInDetails() ");
                string _wherePODocEntry = string.Empty;

                if (payload.BranchID <= 0 || payload.BranchID == null)
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Provide Branch" });
                }

                if (payload.FromDate == string.Empty || payload.FromDate.ToLower() == "string")
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Provide FromDate" });
                }

                if (payload.ToDate == string.Empty || payload.ToDate.ToLower() == "string")
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Provide ToDate" });
                }

                if (payload.PODocEntry != null)
                {
                    if (payload.PODocEntry > 0)
                    {
                        _wherePODocEntry = "and DocEntry=@PODocEntry";
                    }
                }

                QITcon = new SqlConnection(_QIT_connection);
                System.Data.DataTable dtData = new System.Data.DataTable();
                _Query = " select A.DocEntry, A.DocNum, GateInNo, RecDate GateInDate, A.RecQty GateInQty,D.Quantity OrderQty,ISNULL(ISNULL(D.Quantity,0) - ISNULL(A.RecQty,0),0) OpenQty, A.ItemCode, B.ItemName, A.Project, A.UomCode, VehicleNo, TransporterCode, LRNo, LRDate, GRPODocDate invoice_date,GRPOVendorRefNo invoice_no,C.CardCode vendorCode,C.CardName vendorName from [dbo].[QIT_GateIN] A inner join QIT_Item_Master B ON A.ItemCode = B.ItemCode inner join " + Global.SAP_DB + @".dbo.OPOR C ON C.DocNum=A.DocNum AND C.DocEntry=A.DocEntry inner join " + Global.SAP_DB + @".dbo.POR1 D on D.DocEntry = C.DocEntry and D.DocEntry = A.DocEntry AND D.ItemCode COLLATE SQL_Latin1_General_CP1_CI_AS = A.ItemCode where RecDate >= @frDate and RecDate <= @toDate and ISNULL(A.BranchID, @bID) = @bID " + _wherePODocEntry;
                _logger.LogInformation(" ReportController :  GateInDetails Query : {q} ", _Query.ToString());
                oAdptr = new SqlDataAdapter(_Query, QITcon);
                oAdptr.SelectCommand.Parameters.AddWithValue("@frDate", payload.FromDate);
                oAdptr.SelectCommand.Parameters.AddWithValue("@toDate", payload.ToDate);
                oAdptr.SelectCommand.Parameters.AddWithValue("@bID", payload.BranchID);
                oAdptr.SelectCommand.Parameters.AddWithValue("@PODocEntry", payload.PODocEntry);

                QITcon.Open();
                oAdptr.Fill(dtData);
                QITcon.Close();

                if (dtData.Rows.Count > 0)
                {
                    List<GateInDetailsReport> obj = new List<GateInDetailsReport>();

                    dynamic arData = JsonConvert.SerializeObject(dtData);
                    obj = JsonConvert.DeserializeObject<List<GateInDetailsReport>>(arData.ToString().Replace("~", " "));
                    return obj;
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "No data found" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(" Error in ReportController : GateInDetails() :: {Error}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
            }
        }

        #endregion


        #region GRPO Vs GateIN Report

        [HttpPost("GRNDetails")]
        public async Task<ActionResult<IEnumerable<GRNDetailsReport>>> GRNDetails(GateInDetails payload)
        {
            try
            {
                _logger.LogInformation(" Calling ReportController : GRNDetails() ");
                string _wherePODocEntry = string.Empty;

                if (payload.BranchID <= 0 || payload.BranchID == null)
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Provide Branch" });
                }

                if (payload.FromDate == string.Empty || payload.FromDate.ToLower() == "string")
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Provide FromDate" });
                }

                if (payload.ToDate == string.Empty || payload.ToDate.ToLower() == "string")
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Provide ToDate" });
                }


                if (payload.PODocEntry != null)
                {
                    if (payload.PODocEntry > 0)
                    {
                        _wherePODocEntry = "and A.DocEntry=@PODocEntry";
                    }
                }

                QITcon = new SqlConnection(_QIT_connection);
                System.Data.DataTable dtData = new System.Data.DataTable();
                _Query = @"
                SELECT DISTINCT A.DocNum AS PO_DocNum, A.DocEntry AS PO_DocEntry, A.GateInNo, A.RecDate AS GateIn_Date, F.DocDate AS PO_DocDate,
                       G.WhsCode AS PO_WhsCode, E.WhsCode AS GRN_WhsCode, A.ItemCode, G.Dscription AS ItemName, A.RecQty AS GateInQty,
                       G.Quantity AS PO_Qty, E.Quantity as GRN_Qty, D.DocNum AS GRN_DocNum, D.DocDate AS GRN_DocDate, 
                       F.CardCode AS Vendor_Code, F.CardName AS Vendor_Name
                FROM QIT_GateIN A
                     LEFT JOIN QIT_QRStock_POToGRPO B ON A.GateInNo = B.GateInNo AND A.ItemCode = B.ItemCode
                     LEFT JOIN QIT_Trans_POToGRPO C ON B.TransSeq = C.Transseq AND C.BaseDocEntry = A.DocEntry
                     LEFT JOIN " + Global.SAP_DB + @".dbo.OPOR F ON F.DocEntry = A.DocEntry AND 
                          F.ObjType COLLATE SQL_Latin1_General_CP1_CI_AS = A.ObjType AND F.BPLId = A.BranchID
                     LEFT JOIN " + Global.SAP_DB + @".dbo.POR1 G ON F.DocEntry = G.DocEntry AND 
                          A.ItemCode COLLATE SQL_Latin1_General_CP1_CI_AS = G.ItemCode AND A.LineNum = G.LineNum
                     LEFT JOIN " + Global.SAP_DB + @".dbo.OPDN D ON C.DocEntry = D.DocEntry AND C.DocNum = D.DocNum
                     LEFT JOIN " + Global.SAP_DB + @".dbo.PDN1 E ON F.DocNum = E.BaseRef AND G.DocEntry = E.BaseEntry AND E.DocEntry = C.DocEntry AND 
                          E.ItemCode  COLLATE SQL_Latin1_General_CP1_CI_AS=G.ItemCode AND E.BaseLine = G.LineNum AND
                          F.ObjType COLLATE SQL_Latin1_General_CP1_CI_AS = E.BaseType
                WHERE A.RecDate >= @frDate AND A.RecDate <= @toDate and ISNULL(A.BranchID, @bID) = @bID " + _wherePODocEntry;

                _logger.LogInformation(" ReportController :  GRNDetails Query : {q} ", _Query.ToString());

                oAdptr = new SqlDataAdapter(_Query, QITcon);

                oAdptr.SelectCommand.Parameters.AddWithValue("@frDate", payload.FromDate);
                oAdptr.SelectCommand.Parameters.AddWithValue("@toDate", payload.ToDate);
                oAdptr.SelectCommand.Parameters.AddWithValue("@bID", payload.BranchID);
                oAdptr.SelectCommand.Parameters.AddWithValue("@PODocEntry", payload.PODocEntry);

                QITcon.Open();
                oAdptr.Fill(dtData);
                QITcon.Close();

                if (dtData.Rows.Count > 0)
                {
                    List<GRNDetailsReport> obj = new List<GRNDetailsReport>();
                    dynamic arData = JsonConvert.SerializeObject(dtData);
                    obj = JsonConvert.DeserializeObject<List<GRNDetailsReport>>(arData.ToString().Replace("~", " "));
                    return obj;
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "No data found" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(" Error in ReportController : GRNDetails() :: {Error}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
            }
        }

        #endregion


        #region GRN Vs QC Report

        //[HttpPost("QcDetails")]
        //public async Task<ActionResult<IEnumerable<QcDetailsReport>>> QcDetails(GateInDetails payload)
        //{
        //    try
        //    {
        //        _logger.LogInformation(" Calling ReportController : QcDetails() ");
        //        string _wherePODocEntry = string.Empty;

        //        if (payload.BranchID <= 0 || payload.BranchID == null)
        //        {
        //            return BadRequest(new { StatusCode = "400", StatusMsg = "Provide Branch" });
        //        }

        //        if (payload.FromDate == string.Empty || payload.FromDate.ToLower() == "string")
        //        {
        //            return BadRequest(new { StatusCode = "400", StatusMsg = "Provide FromDate" });
        //        }

        //        if (payload.ToDate == string.Empty || payload.ToDate.ToLower() == "string")
        //        {
        //            return BadRequest(new { StatusCode = "400", StatusMsg = "Provide ToDate" });
        //        }

        //        if (payload.PODocEntry != null)
        //        {
        //            if (payload.PODocEntry > 0)
        //            {
        //                _wherePODocEntry = " and A.BaseDocEntry = @PODocEntry ";
        //            }
        //        }

        //        SqlConnection _QITConn = new SqlConnection(_QIT_connection);
        //        System.Data.DataTable dtData = new System.Data.DataTable();
        //        _Query = @"SELECT DISTINCT
        //             E.GateInNo,
        //             F.RecDate AS GateIn_Date,
        //             B.DocNum AS GRN_DocNum,
        //             B.DocEntry AS GRN_DocEntry,
        //             B.DocDate AS GRN_DocDate,
        //             C.ItemCode,
        //             C.Dscription AS ItemName,
        //             CASE WHEN I.U_QA IN (1, 3) THEN 'Y' ELSE 'N' END AS QARequired,
        //             H.EntryDate AS QC_Date,
        //             F.RecQty,
        //             C.Quantity AS GRN_Qty,
        //             H.Qty AS QC_Qty,
        //             C.WhsCode AS GRN_WhsCode,
        //             H.FromWhs AS QC_FromWhs,
        //             H.ToWhs AS QC_ToWhs,
        //             D.DocNum AS PO_DocNum,
        //             D.DocEntry AS PO_DocEntry,
        //             D.DocDate AS PO_DocDate,
        //             B.CardCode AS Vendor_Code,
        //             B.CardName AS Vendor_Name
        //         FROM
        //             QIT_Trans_POToGRPO A
        //         INNER JOIN
        //             " + Global.SAP_DB + @".dbo.OPOR D ON D.DocEntry = A.BaseDocEntry AND D.DocNum = A.BaseDocNum
        //         INNER JOIN
        //             " + Global.SAP_DB + @".dbo.POR1 G ON G.DocEntry = D.DocEntry AND G.ItemCode COLLATE SQL_Latin1_General_CP1_CI_AS = A.ItemCode AND G.DocEntry = A.BaseDocEntry
        //         INNER JOIN
        //             " + Global.SAP_DB + @".dbo.OPDN B ON B.DocEntry = A.DocEntry
        //         INNER JOIN
        //             " + Global.SAP_DB + @".dbo.PDN1 C ON C.BaseEntry = D.DocEntry AND C.DocEntry = B.DocEntry AND C.BaseRef = A.BaseDocNum AND C.BaseEntry = A.BaseDocEntry AND C.ItemCode COLLATE SQL_Latin1_General_CP1_CI_AS = A.ItemCode
        //         INNER JOIN
        //             QIT_QRStock_POToGRPO E ON E.TransSeq = A.TransSeq AND E.ItemCode = A.ItemCode
        //         INNER JOIN
        //             QIT_GateIN F ON F.GateInNo = E.GateInNo AND F.ItemCode = A.ItemCode
        //         INNER JOIN 
        //             " + Global.SAP_DB + @".dbo.OITM I ON I.ItemCode collate SQL_Latin1_General_CP1_CI_AS = A.ItemCode
        //         LEFT JOIN
        //             QIT_QC_Detail H ON H.GRPODocEntry = A.DocEntry AND H.GRPODocEntry = B.DocEntry AND H.GRPODocNum = A.DocNum AND H.GRPODocNum = A.DocNum AND H.ItemCode = A.ItemCode AND H.LineNum = F.LineNum and H.LineNum = G.LineNum AND H.GateInNo = E.GateInNo
        //         WHERE
        //             A.EntryDate >= @frDate
        //             AND A.EntryDate <= @toDate
        //             AND ISNULL(A.BranchID, @bID) = @bID " + _wherePODocEntry;


        //        _logger.LogInformation(" ReportController :  QcDetails Query : {q} ", _Query.ToString());
        //        SqlDataAdapter oAdptr = new SqlDataAdapter(_Query, _QITConn);
        //        oAdptr.SelectCommand.CommandTimeout = 500000;
        //        oAdptr.SelectCommand.Parameters.AddWithValue("@frDate", payload.FromDate);
        //        oAdptr.SelectCommand.Parameters.AddWithValue("@toDate", payload.ToDate);
        //        oAdptr.SelectCommand.Parameters.AddWithValue("@bID", payload.BranchID);
        //        oAdptr.SelectCommand.Parameters.AddWithValue("@PODocEntry", payload.PODocEntry);

        //        _QITConn.Open();
        //        oAdptr.Fill(dtData);
        //        _QITConn.Close();

        //        if (dtData.Rows.Count > 0)
        //        {
        //            List<QcDetailsReport> obj = new List<QcDetailsReport>();

        //            dynamic arData = JsonConvert.SerializeObject(dtData);
        //            obj = JsonConvert.DeserializeObject<List<QcDetailsReport>>(arData);
        //            return obj;
        //        }
        //        else
        //        {
        //            return BadRequest(new { StatusCode = "400", StatusMsg = "No data found" });
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(" Error in ReportController : QcDetails() :: {Error}", ex.ToString());
        //        return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
        //    }
        //}

        [HttpPost("QCDetails")]
        public async Task<ActionResult<IEnumerable<QCDetailsReport>>> QCDetails(GateInDetails payload)
        {
            try
            {
                _logger.LogInformation(" Calling ReportController : QCDetails() ");
                string _wherePODocEntry = string.Empty;

                if (payload.BranchID <= 0 || payload.BranchID == null)
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Provide Branch" });
                }

                if (payload.FromDate == string.Empty || payload.FromDate.ToLower() == "string")
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Provide FromDate" });
                }

                if (payload.ToDate == string.Empty || payload.ToDate.ToLower() == "string")
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Provide ToDate" });
                }

                if (payload.PODocEntry != null)
                {
                    if (payload.PODocEntry > 0)
                    {
                        _wherePODocEntry = " and A.BaseDocEntry = @PODocEntry ";
                    }
                }

                if (QITcon == null)
                    QITcon = new SqlConnection(_QIT_connection);
                System.Data.DataTable dtData = new System.Data.DataTable();
                _Query = @"
                SELECT A.GateInNo, A.PODocNum, B.CardCode, B.CardName, A.DocNum GRNDocNum, 
	                   ( SELECT Z.Qty FROM QIT_QRStock_POToGRPO Z WHERE Z.QRCodeID = A.QRCodeID) GRNQty,
	                   A.ItemCode, A.ItemName, 'Y' QARequired,
	                   ISNULL(( SELECT sum(ISNULL(Qty,0)) FROM QIT_QC_Detail Z 
                                WHERE Z.GRPODocEntry = A.DocEntry and Z.ItemCode = A.ItemCode and 
		                              Z.LineNum = A.LineNum and Z.Status = 'A' and Z.DocEntry <> 0 and Z.QRCodeID = A.QRCodeID)
                       ,0) ApprovedQty,
	                   ISNULL(( SELECT sum(ISNULL(Qty,0)) from QIT_QC_Detail Z where Z.GRPODocEntry = A.DocEntry and Z.ItemCode = A.ItemCode and Z.LineNum = A.LineNum and Z.Status = 'R' and Z.DocEntry <> 0 and Z.QRCodeID 
				                 = A.QRCodeID),0) RejectedQty
                FROM 
                (
	                select A.*, C.ItemName,
		                   ( select DocEntry from QIT_QR_Header Z1 inner join QIT_QR_Detail Z2 on Z1.HeaderSrNo = Z2.HeaderSrNo where Z2.QRCodeID = A.QRCodeID ) podocEntry,
		                   ( select DocNum from QIT_QR_Header Z1 inner join QIT_QR_Detail Z2 on Z1.HeaderSrNo = Z2.HeaderSrNo where Z2.QRCodeID = A.QRCodeID ) PODocNum 
	                FROM 
	                (
		                SELECT DISTINCT  A.GateInNo, B.QRCodeID, 
                               (select LineNum from QIT_QR_Detail Z where Z.QRCodeId = B.QRCodeId) Linenum, A.DocEntry, A.DocNum,
						       (select ItemCode from QIT_QR_Detail Z where Z.QRCodeId = B.QRCodeId) ItemCode
		                FROM QIT_Trans_POToGRPO  A
			                 inner join QIT_QRStock_POToGRPO B ON A.GateInNo = b.GateInNo
		                WHERE A.EntryDate >= @frDate AND A.EntryDate <= @toDate AND ISNULL(A.BranchID, @bID) = @bID AND
                              A.DocEntry <> 0 " + _wherePODocEntry + @" --and A.GateInNo = 2883
	                ) as A
	                INNER JOIN " + Global.SAP_DB + @".dbo.OITM C on C.ItemCode collate SQL_Latin1_General_CP1_CI_AS = A.ItemCode and 
                          C.U_QA IN (1,3)
                ) as A
                INNER JOIN " + Global.SAP_DB + @".dbo.OPOR B on A.podocEntry  = B.DocEntry
                ";


                _logger.LogInformation(" ReportController :  QCDetails Query : {q} ", _Query.ToString());
                oAdptr = new SqlDataAdapter(_Query, QITcon);
                oAdptr.SelectCommand.CommandTimeout = 500000;
                oAdptr.SelectCommand.Parameters.AddWithValue("@frDate", payload.FromDate);
                oAdptr.SelectCommand.Parameters.AddWithValue("@toDate", payload.ToDate);
                oAdptr.SelectCommand.Parameters.AddWithValue("@bID", payload.BranchID);
                oAdptr.SelectCommand.Parameters.AddWithValue("@PODocEntry", payload.PODocEntry);

                QITcon.Open();
                oAdptr.Fill(dtData);
                QITcon.Close();

                if (dtData.Rows.Count > 0)
                {
                    List<QCDetailsReport> obj = new List<QCDetailsReport>();

                    dynamic arData = JsonConvert.SerializeObject(dtData);
                    obj = JsonConvert.DeserializeObject<List<QCDetailsReport>>(arData);
                    return obj;
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "No data found" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(" Error in ReportController : QCDetails() :: {Error}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
            }
        }

        #endregion


        #region GateIn Vs GRN Vs QC

        [HttpPost("GRN_QC_Compression")]
        public async Task<ActionResult<IEnumerable<GRN_Qc_DetailsReport>>> GRN_QC_Compression(GateInDetails payload)
        {
            try
            {
                _logger.LogInformation(" Calling ReportController : GRN_QC_Compression() ");
                string _wherePODocEntry = string.Empty;

                if (payload.BranchID <= 0 || payload.BranchID == null)
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Provide Branch" });
                }

                if (payload.FromDate == string.Empty || payload.FromDate.ToLower() == "string")
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Provide FromDate" });
                }

                if (payload.ToDate == string.Empty || payload.ToDate.ToLower() == "string")
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Provide ToDate" });
                }

                if (payload.PODocEntry != null)
                {
                    if (payload.PODocEntry > 0)
                    {
                        _wherePODocEntry = " AND A.DocEntry=@PODocEntry";
                    }
                }

                QITcon = new SqlConnection(_QIT_connection);
                System.Data.DataTable dtData = new System.Data.DataTable();
                _Query = @"SELECT DISTINCT
                    A.DocNum AS PO_DocNum,
                    A.DocEntry AS PO_DocEntry,
                    A.GateInNo,
                    A.RecDate AS GateIn_Date,
                    F.DocDate AS PO_DocDate,
                    G.WhsCode AS PO_WhsCode,
                    E.WhsCode AS GRN_WhsCode,
                    H.ToWhs AS QC_ToWhs,
                    CASE WHEN I.U_QA IN (1, 3) THEN 'Y' ELSE 'N' END AS QARequired,
                    A.ItemCode,
                    G.Dscription AS ItemName,
                    G.Quantity AS PO_Qty,
                    A.RecQty GateInQty,
                    E.Quantity AS GRN_Qty,
                    CASE WHEN H.Status = 'A' THEN
                        (SELECT SUM(Qty) FROM QIT_QC_Detail WHERE ItemCode = A.ItemCode AND LineNum = A.LineNum AND GateInNo = A.GateInNo AND Status = 'A')
                    ELSE
                        (SELECT SUM(Qty) FROM QIT_QC_Detail WHERE ItemCode = A.ItemCode AND LineNum = A.LineNum AND GateInNo = A.GateInNo AND Status = 'R')
                    END AS QC_Qty,
                    D.DocNum AS GRN_DocNum,
                    D.DocDate AS GRN_DocDate,
                    D.CardCode AS Vendor_Code,
                    D.CardName AS Vendor_Name
                FROM
                    QIT_GateIN A
                LEFT JOIN
                    QIT_QRStock_POToGRPO B ON A.GateInNo = B.GateInNo AND A.ItemCode = B.ItemCode
                LEFT JOIN
                    QIT_Trans_POToGRPO C ON B.TransSeq = C.Transseq AND C.BaseDocEntry = A.DocEntry
                LEFT JOIN
                    " + Global.SAP_DB + @".dbo.OPOR F ON F.DocEntry = A.DocEntry AND F.ObjType COLLATE SQL_Latin1_General_CP1_CI_AS = A.ObjType AND F.BPLId = A.BranchID
                LEFT JOIN
                    " + Global.SAP_DB + @".dbo.POR1 G ON F.DocEntry = G.DocEntry AND A.ItemCode COLLATE SQL_Latin1_General_CP1_CI_AS = G.ItemCode AND A.LineNum = G.LineNum
                LEFT JOIN
                    " + Global.SAP_DB + @".dbo.OPDN D ON C.DocEntry = D.DocEntry AND C.DocNum = D.DocNum
                LEFT JOIN
                    " + Global.SAP_DB + @".dbo.PDN1 E ON F.DocNum = E.BaseRef AND G.DocEntry = E.BaseEntry AND E.DocEntry = C.DocEntry AND G.ItemCode COLLATE SQL_Latin1_General_CP1_CI_AS = E.ItemCode AND E.BaseLine = G.LineNum 
                        AND F.ObjType COLLATE SQL_Latin1_General_CP1_CI_AS = E.BaseType
                LEFT JOIN
                    QIT_QC_Detail H ON H.GRPODocEntry = C.DocEntry AND H.GRPODocNum = C.DocNum AND H.ItemCode = A.ItemCode AND H.GateInNo = A.GateInNo AND H.LineNum = A.LineNum 
                INNER JOIN
                    " + Global.SAP_DB + @".dbo.OITM I ON I.ItemCode COLLATE SQL_Latin1_General_CP1_CI_AS = A.ItemCode
                WHERE
                    RecDate >= @frDate AND RecDate <= @toDate
                    AND ISNULL(A.BranchID, @bID) = @bID
                 " + _wherePODocEntry;


                _logger.LogInformation(" ReportController :  GRN_QC_Compression Query : {q} ", _Query.ToString());
                oAdptr = new SqlDataAdapter(_Query, QITcon);
                oAdptr.SelectCommand.Parameters.AddWithValue("@frDate", payload.FromDate);
                oAdptr.SelectCommand.Parameters.AddWithValue("@toDate", payload.ToDate);
                oAdptr.SelectCommand.Parameters.AddWithValue("@bID", payload.BranchID);
                oAdptr.SelectCommand.Parameters.AddWithValue("@PODocEntry", payload.PODocEntry);

                QITcon.Open();
                oAdptr.Fill(dtData);
                QITcon.Close();

                if (dtData.Rows.Count > 0)
                {
                    List<GRN_Qc_DetailsReport> obj = new List<GRN_Qc_DetailsReport>();

                    dynamic arData = JsonConvert.SerializeObject(dtData);
                    obj = JsonConvert.DeserializeObject<List<GRN_Qc_DetailsReport>>(arData);
                    return obj;
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "No data found" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(" Error in ReportController : GRN_QC_Compression() :: {Error}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
            }
        }

        #endregion


        #region Item Wise QR Wise Stock Report based on selected Project

        [HttpPost("ItemWiseQRWiseStock")]
        public async Task<ActionResult<IEnumerable<ItemWiseQRWiseStockReport>>> ItemWiseQRWiseStock(ItemWiseQRWiseStock payload)
        {
            try
            {
                _logger.LogInformation(" Calling ReportController : ItemWiseQRWiseStock() ");

                QITcon = new SqlConnection(_QIT_connection);
                System.Data.DataTable dtData = new System.Data.DataTable();
                string _whereProject = string.Empty;

                if (payload == null)
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Payload details not found" });
                }

                if (payload.BranchID == 0)
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Provide Branch" });
                }

                if (payload.ItemCode == string.Empty || payload.ItemCode.ToLower() == "string")
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Provide ItemCode" });
                }

                if (payload.Project == string.Empty || payload.Project.ToLower() == "string")
                {
                    //return BadRequest(new { StatusCode = "400", StatusMsg = "Provide Project" });
                    _whereProject = "";
                }
                else
                {
                    _whereProject = " AND Project = @proj";
                }

                _Query = @"
                SELECT * FROM 
                (
                    SELECT ItemCode, QRCodeID, Project, WhsCode, WhsName,  
                           CASE WHEN A.POtoGRPO > 0 THEN 
                                (POtoGRPO + InQty - OutQty - IssueQty - DeliverQty + ReceiptQty)
                           ELSE 
                                (InQty - OutQty - IssueQty - DeliverQty + ReceiptQty)
                           END Stock
                    FROM 
                    (
                        SELECT B.ItemCode, B.QRCodeID, C.Project, A.WhsCode, A.WhsName,
                               (
                                    ISNULL((	
                                    SELECT sum(Z.Qty) POtoGRPOQty FROM " + Global.QIT_DB + @".dbo.[QIT_QRStock_InvTrans] Z
                                    WHERE Z.QRCodeID = B.QRCodeID and Z.FromObjType = '22' and Z.ToObjType = '67' and Z.ToWhs = A.WhsCode and ISNULL(Z.BranchID, @bID) = @bID
                                    ),0)
                               ) POtoGRPO,
                               (
                                    ISNULL((
                                    SELECT sum(Z.Qty) InQty FROM " + Global.QIT_DB + @".dbo.[QIT_QRStock_InvTrans] Z
                                    WHERE Z.QRCodeID = B.QRCodeID and (Z.FromObjType <> '22') AND 
                                          Z.ToWhs collate SQL_Latin1_General_CP850_CI_AS = A.WhsCode and ISNULL(Z.BranchID, @bID) = @bID
                                    ),0) 
                               ) InQty,
                               (
                                    ISNULL((
                                    SELECT sum(Z.Qty) OutQty from " + Global.QIT_DB + @".dbo.[QIT_QRStock_InvTrans] Z
                                    WHERE Z.QRCodeID = B.QRCodeID and (Z.FromObjType <> '22') and 
                                          Z.FromWhs collate SQL_Latin1_General_CP850_CI_AS = A.WhsCode and ISNULL(Z.BranchID, @bID) = @bID
                                    ),0)
                               ) OutQty,
                               (
                                    ISNULL((
                                    SELECT sum(Z.Qty) IssueQty from " + Global.QIT_DB + @".dbo.QIT_QRStock_ProToIssue Z
                                    WHERE Z.QRCodeID = B.QRCodeID and  
                                          Z.FromWhs collate SQL_Latin1_General_CP850_CI_AS = A.WhsCode and ISNULL(Z.BranchID, @bID) = @bID
                                    ),0)
                               ) IssueQty,
                               (
                                    ISNULL((
                                    SELECT sum(Z.Qty) DeliverQty from " + Global.QIT_DB + @".dbo.QIT_QRStock_SOToDelivery Z
                                    WHERE Z.QRCodeID = B.QRCodeID and  
                                          Z.FromWhs collate SQL_Latin1_General_CP850_CI_AS = A.WhsCode and ISNULL(Z.BranchID, @bID) = @bID
                                    ),0)
                               ) DeliverQty,
                               (
                                    ISNULL((
                                    SELECT sum(Z.Qty) ReceiptQty from " + Global.QIT_DB + @".dbo.QIT_QRStock_ProToReceipt Z
                                    WHERE Z.QRCodeID = B.QRCodeID and  
                                          Z.ToWhs collate SQL_Latin1_General_CP850_CI_AS = A.WhsCode and ISNULL(Z.BranchID, @bID) = @bID
                                    ),0)
                               ) ReceiptQty
                        FROM QIT_Warehouse_Master AS A
              LEFT JOIN QIT_QR_Detail B on 1 = 1 and ItemCode = @itemCode
              INNER JOIN QIT_GateIN C ON C.ItemCode = B.ItemCode and C.GateInNo = B.GateInNo " + _whereProject + @"
                        WHERE Locked = 'N' 
                    ) as A  
                ) as A 
                WHERE A.Stock > 0 
             ORDER BY ItemCode, QRCodeID
                ";

                _logger.LogInformation(" ReportController : ItemWiseQRWiseStock Query : {q} ", _Query.ToString());
                oAdptr = new SqlDataAdapter(_Query, QITcon);
                oAdptr.SelectCommand.Parameters.AddWithValue("@bID", payload.BranchID);
                oAdptr.SelectCommand.Parameters.AddWithValue("@itemCode", payload.ItemCode);


                if (payload.Project == string.Empty || payload.Project.ToLower() == "string")
                {
                }
                else
                {
                    oAdptr.SelectCommand.Parameters.AddWithValue("@proj", payload.Project);
                }


                QITcon.Open();
                oAdptr.Fill(dtData);
                QITcon.Close();

                if (dtData.Rows.Count > 0)
                {
                    List<ItemWiseQRWiseStockReport> obj = new List<ItemWiseQRWiseStockReport>();

                    dynamic arData = JsonConvert.SerializeObject(dtData);
                    obj = JsonConvert.DeserializeObject<List<ItemWiseQRWiseStockReport>>(arData.ToString().Replace("~", " "));
                    return obj;

                }
                else
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "No data found" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(" Error in ReportController : ItemWiseQRWiseStock() :: {Error}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
            }
        }

        #endregion


        #region Production Order based Stock Report - all items of Production Order

        [HttpGet("GetProductionList")]
        public async Task<ActionResult<IEnumerable<ProOrd>>> GetProductionList()
        {
            try
            {
                _logger.LogInformation(" Calling ReportController : GetProductionList() ");
                List<ProOrd> obj = new List<ProOrd>();
                System.Data.DataTable dtData = new System.Data.DataTable();
                QITcon = new SqlConnection(_QIT_connection);

                string _where = string.Empty;

                _Query = @" select DocEntry, DocNum, ItemCode,
                            (select ItemName from " + Global.SAP_DB + @".dbo.OITM where ItemCode = OWOR.ItemCode) ItemName
                            from " + Global.SAP_DB + @".dbo.OWOR where /*Type = 'S' and */Status ='R' ";

                _logger.LogInformation(" ReportController : GetProductionList() Query : {q} ", _Query.ToString());
                QITcon.Open();
                oAdptr = new SqlDataAdapter(_Query, QITcon);

                oAdptr.Fill(dtData);
                QITcon.Close();

                if (dtData.Rows.Count > 0)
                {
                    dynamic arData = JsonConvert.SerializeObject(dtData);
                    obj = JsonConvert.DeserializeObject<List<ProOrd>>(arData.ToString());
                    return obj;
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "No data found" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(" Error in ReportController : GetProductionList() :: {Error}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
            }
        }


        [HttpGet("GetProductionItems")]
        public async Task<ActionResult<IEnumerable<ProItems>>> GetProductionItems(int ProDocEntry)
        {
            try
            {
                _logger.LogInformation(" Calling ReportController : GetProductionItems() ");
                List<ProItems> obj = new List<ProItems>();
                System.Data.DataTable dtData = new System.Data.DataTable();
                QITcon = new SqlConnection(_QIT_connection);

                string _where = string.Empty;

                _Query = @" SELECT  OWOR.DocEntry, DocNum, LineNum, WOR1.ItemCode, ItemType,
                                    (select ItemName FROM " + Global.SAP_DB + @".dbo.OITM where ItemCode = WOR1.ItemCode) ItemName
                            FROM " + Global.SAP_DB + @".dbo.WOR1
                                 INNER JOIN " + Global.SAP_DB + @".dbo.OWOR ON OWOR.DocEntry = WOR1.DocEntry
                            where OWOR.DocEntry = @docEntry and ItemType = 4 ";
                _logger.LogInformation(" ReportController : GetProductionItems() Query : {q} ", _Query.ToString());
                QITcon.Open();
                 oAdptr = new SqlDataAdapter(_Query, QITcon);

                oAdptr.SelectCommand.Parameters.AddWithValue("@docEntry", ProDocEntry);
                oAdptr.Fill(dtData);
                QITcon.Close();

                if (dtData.Rows.Count > 0)
                {
                    dynamic arData = JsonConvert.SerializeObject(dtData);
                    obj = JsonConvert.DeserializeObject<List<ProItems>>(arData.ToString());
                    return obj;
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "No data found" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(" Error in ReportController : GetProductionItems() :: {Error}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
            }
        }


        [HttpPost("DetailQRWiseStock")]
        public async Task<ActionResult<IEnumerable<ItemWiseQRWiseStockReport>>> DetailQRWiseStock(QRWiseStock payload)
        {
            try
            {
                _logger.LogInformation(" Calling ReportController : DetailQRWiseStock() ");

                QITcon = new SqlConnection(_QIT_connection);
                System.Data.DataTable dtData = new System.Data.DataTable();
                string _whereItemCodes = string.Empty;

                #region Validation

                if (payload == null)
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Payload details not found" });
                }

                if (payload.BranchID == 0)
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Provide Branch" });
                }

                if (payload.Project == string.Empty || payload.Project.ToLower() == "string")
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Provide Project" });
                }

                if ((string.IsNullOrEmpty(payload.ItemCode) || payload.ItemCode.ToLower() == "string") &&
                      (payload.PODocEntry == null || payload.PODocEntry <= 0) &&
                      (payload.PRODocEntry == null || payload.PRODocEntry <= 0)
                   )
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "At least one of ItemCode, PODocEntry, or PRODocEntry is required." });
                }

                #endregion

                if (payload.PRODocEntry != null)
                {
                    if (payload.PRODocEntry > 0)
                    {
                        _whereItemCodes = " AND B.ItemCode collate SQL_Latin1_General_CP850_CI_AS IN (select ItemCode from " + Global.SAP_DB + @".dbo.WOR1 where DocEntry = @proDocEntry AND Project = @proj ) ";
                    }
                }

                if (payload.ItemCode.Length > 0 && payload.ItemCode.ToLower() != "string")
                {
                    _whereItemCodes += " AND B.ItemCode collate SQL_Latin1_General_CP850_CI_AS = @itemCode ";
                }

                _Query = @"
                SELECT *, ( SELECT ItemName FROM " + Global.SAP_DB + @".dbo.OITM WHERE ItemCode collate SQL_Latin1_General_CP1_CI_AS = A.ItemCode) ItemName 
                FROM 
                (
                    SELECT ItemCode, QRCodeID, Project, WhsCode, WhsName,  
                           CASE WHEN A.POtoGRPO > 0 THEN 
                                (POtoGRPO + InQty - OutQty - IssueQty - DeliverQty + ReceiptQty)
                           ELSE 
                                (InQty - OutQty - IssueQty - DeliverQty + ReceiptQty)
                           END Stock
                    FROM 
                    (
                        SELECT B.ItemCode, B.QRCodeID, C.Project, A.WhsCode, A.WhsName,
                               (
                                    ISNULL((	
                                    SELECT sum(Z.Qty) POtoGRPOQty FROM [QIT_QRStock_InvTrans] Z
                                    WHERE Z.QRCodeID = B.QRCodeID and Z.FromObjType = '22' and Z.ToObjType = '67' and Z.ToWhs = A.WhsCode and ISNULL(Z.BranchID, @bID) = @bID
                                    ),0)
                               ) POtoGRPO,
                               (
                                    ISNULL((
                                    SELECT sum(Z.Qty) InQty FROM [QIT_QRStock_InvTrans] Z
                                    WHERE Z.QRCodeID = B.QRCodeID and (Z.FromObjType <> '22') AND 
                                          Z.ToWhs collate SQL_Latin1_General_CP850_CI_AS = A.WhsCode and ISNULL(Z.BranchID, @bID) = @bID
                                    ),0) 
                               ) InQty,
                               (
                                    ISNULL((
                                    SELECT sum(Z.Qty) OutQty FROM [QIT_QRStock_InvTrans] Z
                                    WHERE Z.QRCodeID = B.QRCodeID and (Z.FromObjType <> '22') and 
                                          Z.FromWhs collate SQL_Latin1_General_CP850_CI_AS = A.WhsCode and ISNULL(Z.BranchID, @bID) = @bID
                                    ),0)
                               ) OutQty,
                               (
                                    ISNULL((
                                    SELECT sum(Z.Qty) IssueQty FROM QIT_QRStock_ProToIssue Z
                                    WHERE Z.QRCodeID = B.QRCodeID and  
                                          Z.FromWhs collate SQL_Latin1_General_CP850_CI_AS = A.WhsCode and ISNULL(Z.BranchID, @bID) = @bID
                                    ),0)
                               ) IssueQty,
                               (
                                    ISNULL((
                                    SELECT sum(Z.Qty) DeliverQty FROM QIT_QRStock_SOToDelivery Z
                                    WHERE Z.QRCodeID = B.QRCodeID and  
                                          Z.FromWhs collate SQL_Latin1_General_CP850_CI_AS = A.WhsCode and ISNULL(Z.BranchID, @bID) = @bID
                                    ),0)
                               ) DeliverQty,
                               (
                                    ISNULL((
                                    SELECT sum(Z.Qty) ReceiptQty FROM QIT_QRStock_ProToReceipt Z
                                    WHERE Z.QRCodeID = B.QRCodeID and  
                                          Z.ToWhs collate SQL_Latin1_General_CP850_CI_AS = A.WhsCode and ISNULL(Z.BranchID, @bID) = @bID
                                    ),0)
                               ) ReceiptQty
                        FROM QIT_Warehouse_Master AS A
					         LEFT JOIN QIT_QR_Detail B ON 1 = 1 " + _whereItemCodes + @"
					         INNER JOIN QIT_GateIN C ON C.ItemCode = B.ItemCode and C.GateInNo = B.GateInNo AND C.Project = @proj 
                        WHERE Locked = 'N' 
                    ) as A  
                ) as A 
                WHERE A.Stock > 0 
	            ORDER BY ItemCode, QRCodeID
                ";

                _logger.LogInformation(" ReportController : DetailQRWiseStock Query : {q} ", _Query.ToString());
                oAdptr = new SqlDataAdapter(_Query, QITcon);
                oAdptr.SelectCommand.Parameters.AddWithValue("@bID", payload.BranchID);
                oAdptr.SelectCommand.Parameters.AddWithValue("@proj", payload.Project);

                if (payload.PRODocEntry != null)
                {
                    if (payload.PRODocEntry > 0)
                    {
                        oAdptr.SelectCommand.Parameters.AddWithValue("@proDocEntry", payload.PRODocEntry);
                    }
                }
                if (payload.ItemCode.Length > 0 && payload.ItemCode.ToLower() != "string")
                {
                    oAdptr.SelectCommand.Parameters.AddWithValue("@itemCode", payload.ItemCode);
                }


                QITcon.Open();
                oAdptr.Fill(dtData);
                QITcon.Close();

                if (dtData.Rows.Count > 0)
                {
                    List<ItemWiseQRWiseStockReport> obj = new List<ItemWiseQRWiseStockReport>();

                    dynamic arData = JsonConvert.SerializeObject(dtData);
                    obj = JsonConvert.DeserializeObject<List<ItemWiseQRWiseStockReport>>(arData.ToString().Replace("~", " "));
                    return obj;

                }
                else
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "No data found" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(" Error in ReportController : DetailQRWiseStock() :: {Error}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
            }
        }


        //[HttpPost("SummarizeQRWiseStock")]
        //public async Task<ActionResult<IEnumerable<ItemWiseQRWiseStockReport>>> SummarizeQRWiseStock(QRWiseStock payload)
        //{
        //    try
        //    {
        //        _logger.LogInformation(" Calling ReportController : SummarizeQRWiseStock() ");

        //        SqlConnection _QITConn = new SqlConnection(_QIT_connection);
        //        System.Data.DataTable dtData = new System.Data.DataTable();
        //        string _whereItemCodes = string.Empty;

        //        #region Validation

        //        if (payload == null)
        //        {
        //            return BadRequest(new { StatusCode = "400", StatusMsg = "Payload details not found" });
        //        }

        //        if (payload.BranchID == 0)
        //        {
        //            return BadRequest(new { StatusCode = "400", StatusMsg = "Provide Branch" });
        //        }

        //        if (payload.Project == string.Empty || payload.Project.ToLower() == "string")
        //        {
        //            return BadRequest(new { StatusCode = "400", StatusMsg = "Provide Project" });
        //        }

        //        if ((string.IsNullOrEmpty(payload.ItemCode) || payload.ItemCode.ToLower() == "string") &&
        //              (payload.PODocEntry == null || payload.PODocEntry <= 0) &&
        //              (payload.PRODocEntry == null || payload.PRODocEntry <= 0)
        //           )
        //        {
        //            return BadRequest(new { StatusCode = "400", StatusMsg = "At least one of ItemCode, PODocEntry, or PRODocEntry is required." });
        //        }

        //        #endregion

        //        if (payload.PRODocEntry != null)
        //        {
        //            if (payload.PRODocEntry > 0)
        //            {
        //                _whereItemCodes = " AND B.ItemCode collate SQL_Latin1_General_CP850_CI_AS IN (select ItemCode from " + Global.SAP_DB + @".dbo.WOR1 where DocEntry = @proDocEntry AND Project = @proj ) ";
        //            }
        //        }

        //        if (payload.ItemCode.Length > 0 && payload.ItemCode.ToLower() != "string")
        //        {
        //            _whereItemCodes += " AND B.ItemCode collate SQL_Latin1_General_CP850_CI_AS = @itemCode ";
        //        }

        //        _Query = @"
        //        SELECT A.ItemCode, A.ItemName, A.Project, A.WhsCode, A.WhsName, SUM(A.Stock) Stock
        //        FROM 
        //        (
        //            SELECT ItemCode, ( SELECT ItemName FROM " + Global.SAP_DB + @".dbo.OITM WHERE ItemCode collate SQL_Latin1_General_CP1_CI_AS = A.ItemCode) ItemName , 
        //                   Project, WhsCode, WhsName,  
        //                   CASE WHEN A.POtoGRPO > 0 THEN 
        //                        (POtoGRPO + InQty - OutQty - IssueQty - DeliverQty + ReceiptQty)
        //                   ELSE 
        //                        (InQty - OutQty - IssueQty - DeliverQty + ReceiptQty)
        //                   END Stock
        //            FROM 
        //            (
        //                SELECT B.ItemCode, C.Project, A.WhsCode, A.WhsName,
        //                       (
        //                            ISNULL((	
        //                            SELECT sum(Z.Qty) POtoGRPOQty FROM [QIT_QRStock_InvTrans] Z
        //                            WHERE Z.QRCodeID = B.QRCodeID and Z.FromObjType = '22' and Z.ToObjType = '67' and Z.ToWhs = A.WhsCode and ISNULL(Z.BranchID, @bID) = @bID
        //                            ),0)
        //                       ) POtoGRPO,
        //                       (
        //                            ISNULL((
        //                            SELECT sum(Z.Qty) InQty FROM [QIT_QRStock_InvTrans] Z
        //                            WHERE Z.QRCodeID = B.QRCodeID and (Z.FromObjType <> '22') AND 
        //                                  Z.ToWhs collate SQL_Latin1_General_CP850_CI_AS = A.WhsCode and ISNULL(Z.BranchID, @bID) = @bID
        //                            ),0) 
        //                       ) InQty,
        //                       (
        //                            ISNULL((
        //                            SELECT sum(Z.Qty) OutQty FROM [QIT_QRStock_InvTrans] Z
        //                            WHERE Z.QRCodeID = B.QRCodeID and (Z.FromObjType <> '22') and 
        //                                  Z.FromWhs collate SQL_Latin1_General_CP850_CI_AS = A.WhsCode and ISNULL(Z.BranchID, @bID) = @bID
        //                            ),0)
        //                       ) OutQty,
        //                       (
        //                            ISNULL((
        //                            SELECT sum(Z.Qty) IssueQty FROM QIT_QRStock_ProToIssue Z
        //                            WHERE Z.QRCodeID = B.QRCodeID and  
        //                                  Z.FromWhs collate SQL_Latin1_General_CP850_CI_AS = A.WhsCode and ISNULL(Z.BranchID, @bID) = @bID
        //                            ),0)
        //                       ) IssueQty,
        //                       (
        //                            ISNULL((
        //                            SELECT sum(Z.Qty) DeliverQty FROM QIT_QR_Delivery Z
        //                            WHERE Z.QRCodeID = B.QRCodeID and  
        //                                  Z.WhsCode collate SQL_Latin1_General_CP850_CI_AS = A.WhsCode and ISNULL(Z.BranchID, @bID) = @bID
        //                            ),0)
        //                       ) DeliverQty,
        //                       (
        //                            ISNULL((
        //                            SELECT sum(Z.Qty) ReceiptQty FROM QIT_QRStock_ProToReceipt Z
        //                            WHERE Z.QRCodeID = B.QRCodeID and  
        //                                  Z.ToWhs collate SQL_Latin1_General_CP850_CI_AS = A.WhsCode and ISNULL(Z.BranchID, @bID) = @bID
        //                            ),0)
        //                       ) ReceiptQty
        //                FROM QIT_Warehouse_Master AS A
        //      LEFT JOIN QIT_QR_Detail B ON 1 = 1 " + _whereItemCodes + @"
        //      INNER JOIN QIT_GateIN C ON C.ItemCode = B.ItemCode and C.GateInNo = B.GateInNo AND C.Project = @proj 
        //                WHERE Locked = 'N' 
        //            ) as A  
        //        ) as A 
        //        WHERE A.Stock > 0 
        //        GROUP BY A.ItemCode, A.ItemName, A.Project, A.WhsCode, A.WhsName
        //     ORDER BY ItemCode 
        //        ";

        //        _logger.LogInformation(" ReportController : SummarizeQRWiseStock Query : {q} ", _Query.ToString());
        //        SqlDataAdapter oAdptr = new SqlDataAdapter(_Query, _QITConn);
        //        oAdptr.SelectCommand.Parameters.AddWithValue("@bID", payload.BranchID);
        //        oAdptr.SelectCommand.Parameters.AddWithValue("@proj", payload.Project);

        //        if (payload.PRODocEntry != null)
        //        {
        //            if (payload.PRODocEntry > 0)
        //            {
        //                oAdptr.SelectCommand.Parameters.AddWithValue("@proDocEntry", payload.PRODocEntry);
        //            }
        //        }
        //        if (payload.ItemCode.Length > 0 && payload.ItemCode.ToLower() != "string")
        //        {
        //            oAdptr.SelectCommand.Parameters.AddWithValue("@itemCode", payload.ItemCode);
        //        }


        //        _QITConn.Open();
        //        oAdptr.Fill(dtData);
        //        _QITConn.Close();

        //        if (dtData.Rows.Count > 0)
        //        {
        //            List<ItemWiseQRWiseStockReport> obj = new List<ItemWiseQRWiseStockReport>();

        //            dynamic arData = JsonConvert.SerializeObject(dtData);
        //            obj = JsonConvert.DeserializeObject<List<ItemWiseQRWiseStockReport>>(arData.ToString().Replace("~", " "));
        //            return obj;
        //        }
        //        else
        //        {
        //            return BadRequest(new { StatusCode = "400", StatusMsg = "No data found" });
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(" Error in ReportController : SummarizeQRWiseStock() :: {Error}", ex.ToString());
        //        return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
        //    }
        //}

        [HttpPost("SummarizeQRWiseStock")]
        public async Task<ActionResult<IEnumerable<ItemWiseQRWiseStockReport>>> SummarizeQRWiseStock(QRWiseStock payload)
        {
            try
            {
                _logger.LogInformation(" Calling ReportController : SummarizeQRWiseStock() ");

                QITcon = new SqlConnection(_QIT_connection);
                System.Data.DataTable dtData = new System.Data.DataTable();
                string _whereItemCodes = string.Empty;

                #region Validation

                if (payload == null)
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Payload details not found" });
                }

                if (payload.BranchID == 0)
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Provide Branch" });
                }

                if (payload.Project == string.Empty || payload.Project.ToLower() == "string")
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Provide Project" });
                }

                if ((string.IsNullOrEmpty(payload.ItemCode) || payload.ItemCode.ToLower() == "string") &&
                      (payload.PODocEntry == null || payload.PODocEntry <= 0) &&
                      (payload.PRODocEntry == null || payload.PRODocEntry <= 0)
                   )
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "At least one of ItemCode, PODocEntry, or PRODocEntry is required." });
                }

                #endregion

                if (payload.PRODocEntry != null)
                {
                    if (payload.PRODocEntry > 0)
                    {
                        _whereItemCodes = @" AND Z.QRCodeID IN (select QRCodeID from QIT_QR_Detail B inner join " + Global.SAP_DB + @".dbo.WOR1 C on C.ItemCode collate SQL_Latin1_General_CP1_CI_AS = B.ItemCode and C.DocEntry = @proDocEntry and C.Project = @proj) ";
                    }
                }

                if (payload.ItemCode.Length > 0 && payload.ItemCode.ToLower() != "string")
                {
                    _whereItemCodes += " AND Z.ItemCode collate SQL_Latin1_General_CP850_CI_AS = @itemCode ";
                }

                _Query = @"
                 SELECT * FROM (
                 SELECT  A.ItemCode, (select ItemName from QIT_Item_Master where ItemCode = A.ItemCode)  ItemName,
                         @proj Project, A.WhsCode, C.WhsName, B.BinCode, SUM(A.Stock) Stock 
                 FROM 
                 (   
                     SELECT Z.ItemCode, Z.ToWhs WhsCode, Z.ToBinAbsEntry Bin, ISNULL(sum(Z.Qty),0) * 1 Stock FROM QIT_QRStock_InvTrans Z
                     WHERE  Z.FromObjType = '22' and Z.ToObjType = '67' and ISNULL(Z.BranchID, @bID) = @bID " + _whereItemCodes + @"
                     GROUP BY Z.ItemCode, Z.ToWhs, Z.ToBinAbsEntry	
      
                     UNION 
	     
                     SELECT Z.ItemCode, Z.ToWhs WhsCode, Z.ToBinAbsEntry Bin, ISNULL(sum(Z.Qty),0) * 1 Stock FROM QIT_QRStock_InvTrans Z
                     WHERE  (Z.FromObjType <> '22') and ISNULL(Z.BranchID, @bID) = @bID " + _whereItemCodes + @"
                     GROUP BY Z.ItemCode, Z.ToWhs, Z.ToBinAbsEntry

                     UNION 

                     SELECT Z.ItemCode, Z.FromWhs WhsCode, Z.FromBinAbsEntry Bin, ISNULL(sum(Z.Qty),0) * -1 Stock FROM [QIT_QRStock_InvTrans] Z
                     WHERE  (Z.FromObjType <> '22') and ISNULL(Z.BranchID, @bID) = @bID " + _whereItemCodes + @"
                     GROUP BY Z.ItemCode, Z.FromWhs, Z.FromBinAbsEntry

                     UNION 

                     SELECT Z.ItemCode, Z.FromWhs WhsCode, Z.FromBinAbsEntry Bin, ISNULL(sum(Z.Qty),0) * -1 Stock FROM QIT_QRStock_ProToIssue Z
                     WHERE  ISNULL(Z.BranchID, @bID) = @bID " + _whereItemCodes + @"
                     GROUP BY Z.ItemCode, Z.FromWhs, Z.FromBinAbsEntry

                     UNION

                     SELECT Z.ItemCode, Z.FromWhs WhsCode, Z.FromBinAbsEntry Bin, ISNULL(sum(Z.Qty),0) * -1 Stock 
                     FROM QIT_QRStock_SOToDelivery Z
                     WHERE  ISNULL(Z.BranchID, @bID) = @bID " + _whereItemCodes + @"
                     GROUP BY Z.ItemCode, Z.FromWhs, Z.FromBinAbsEntry

                     UNION

                     SELECT Z.ItemCode, Z.ToWhs WhsCode, Z.ToBinAbsEntry Bin, ISNULL(sum(Z.Qty),0) * 1 Stock FROM QIT_QRStock_ProToReceipt Z
                     WHERE  ISNULL(Z.BranchID, @bID) = @bID " + _whereItemCodes + @"
                     GROUP BY Z.ItemCode, Z.ToWhs, Z.ToBinAbsEntry
                  ) as A
                  INNER JOIN " + Global.SAP_DB + @".dbo.OWHS C ON A.WhsCode collate SQL_Latin1_General_CP850_CI_AS = C.WhsCode
                  LEFT JOIN " + Global.SAP_DB + @".dbo.OBIN B ON A.Bin = B.AbsEntry
                  GROUP BY ItemCode,  A.WhsCode, C.WhsName, B.BinCode 
                  ) AS A WHERE A.Stock > 0
                ";

                _logger.LogInformation(" ReportController : SummarizeQRWiseStock Query : {q} ", _Query.ToString());
                oAdptr = new SqlDataAdapter(_Query, QITcon);
                oAdptr.SelectCommand.Parameters.AddWithValue("@bID", payload.BranchID);
                oAdptr.SelectCommand.Parameters.AddWithValue("@proj", payload.Project);

                if (payload.PRODocEntry != null)
                {
                    if (payload.PRODocEntry > 0)
                    {
                        oAdptr.SelectCommand.Parameters.AddWithValue("@proDocEntry", payload.PRODocEntry);
                    }
                }
                if (payload.ItemCode.Length > 0 && payload.ItemCode.ToLower() != "string")
                {
                    oAdptr.SelectCommand.Parameters.AddWithValue("@itemCode", payload.ItemCode);
                }


                QITcon.Open();
                oAdptr.Fill(dtData);
                QITcon.Close();

                if (dtData.Rows.Count > 0)
                {
                    List<ItemWiseQRWiseStockReport> obj = new List<ItemWiseQRWiseStockReport>();

                    dynamic arData = JsonConvert.SerializeObject(dtData);
                    obj = JsonConvert.DeserializeObject<List<ItemWiseQRWiseStockReport>>(arData.ToString().Replace("~", " "));
                    return obj;
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "No data found" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(" Error in ReportController : SummarizeQRWiseStock() :: {Error}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
            }
        }

        #endregion


        #region QR Scan Report        

        [HttpPost("QRScanReport")]
        public async Task<ActionResult<IEnumerable<QRScanReportOutput>>> QRScanReport(QRScanReportInput payload)
        { 
            try
            {
                _logger.LogInformation(" Calling ReportController : QRScanReport() ");
                string _wherePODocEntry = string.Empty;

                if (payload.BranchID <= 0 || payload.BranchID == null)
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Provide Branch" });
                }

                if (payload.QRCodeID == string.Empty || payload.QRCodeID.ToLower() == "string")
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Provide QR Code" });
                }

                QITcon = new SqlConnection(_QIT_connection);
                System.Data.DataTable dtData = new System.Data.DataTable();

                if (payload.QRCodeID.Replace(" ", "~").ToUpper().Contains("~PO~"))
                {
                    _Query = @"
                    SELECT @dQR QRCodeID, A.ItemCode, 
                           ( SELECT Z.ItemName FROM " + Global.SAP_DB + @".dbo.OITM Z WHERE Z.ItemCode collate SQL_Latin1_General_CP1_CI_AS = A.ItemCode) ItemName,
                           A.BatchSerialNo, A.WhsCode, A.WhsName, A.BinCode, A.Project, OBTQ.Quantity BatchQty, A.Stock
                    FROM 
                    (
	                    SELECT A.ItemCode,
			                   (select BatchSerialNo FROM QIT_QR_Detail WHERE QRCodeID = @dQR) BatchSerialNo,
			                   (select Project from dbo.QIT_GateIN where ItemCode = A.ItemCode and 
															      GateInNo = (select GateInNo from QIT_QR_Detail where QRCodeID = @dQR) and 
															      LineNum = (select LineNum from dbo.QIT_QR_Detail where QRCodeID = @dQR)
			                   ) Project,
			                   A.WhsCode, C.WhsName, B.BinCode, SUM(A.Stock) Stock 
	                    FROM 
	                    (   
		                    SELECT Z.ItemCode, Z.ToWhs WhsCode, Z.ToBinAbsEntry Bin, ISNULL(sum(Z.Qty),0) * 1 Stock 
                            FROM [QIT_QRStock_InvTrans] Z
		                    WHERE  Z.QRCodeID = @dQR and Z.FromObjType = '22' and Z.ToObjType = '67' and ISNULL(Z.BranchID, @bId) = @bId
		                    GROUP BY Z.ItemCode, Z.ToWhs, Z.ToBinAbsEntry	
      
		                    UNION 
	     
		                    SELECT Z.ItemCode, Z.ToWhs WhsCode, Z.ToBinAbsEntry Bin, ISNULL(sum(Z.Qty),0) * 1 Stock 
                            FROM [QIT_QRStock_InvTrans] Z
		                    WHERE  Z.QRCodeID = @dQR and (Z.FromObjType <> '22') and ISNULL(Z.BranchID, @bId) = @bId
		                    GROUP BY Z.ItemCode, Z.ToWhs, Z.ToBinAbsEntry

		                    UNION 

		                    SELECT Z.ItemCode, Z.FromWhs WhsCode, Z.FromBinAbsEntry Bin, ISNULL(sum(Z.Qty),0) * -1 Stock FROM [QIT_QRStock_InvTrans] Z
		                    WHERE Z.QRCodeID = @dQR and (Z.FromObjType <> '22') and ISNULL(Z.BranchID, @bId) = @bId
		                    GROUP BY Z.ItemCode, Z.FromWhs, Z.FromBinAbsEntry

		                    UNION 

		                    SELECT Z.ItemCode, Z.FromWhs WhsCode, Z.FromBinAbsEntry Bin, ISNULL(sum(Z.Qty),0) * -1 Stock 
                            FROM QIT_QRStock_ProToIssue Z
		                    WHERE Z.QRCodeID = @dQR and ISNULL(Z.BranchID, @bId) = @bId
		                    GROUP BY Z.ItemCode, Z.FromWhs, Z.FromBinAbsEntry

		                    UNION

		                    SELECT Z.ItemCode, Z.FromWhs WhsCode, Z.FromBinAbsEntry Bin, ISNULL(sum(Z.Qty),0) * -1 Stock 
                            FROM QIT_QRStock_SOToDelivery Z
		                    WHERE Z.QRCodeID = @dQR and ISNULL(Z.BranchID, @bId) = @bId
		                    GROUP BY Z.ItemCode, Z.FromWhs, Z.FromBinAbsEntry 
	                     ) as A
	                     INNER JOIN " + Global.SAP_DB + @".dbo.OWHS C ON A.WhsCode collate SQL_Latin1_General_CP850_CI_AS = C.WhsCode
	                     LEFT JOIN " + Global.SAP_DB + @".dbo.OBIN B ON A.Bin = B.AbsEntry
	                     GROUP BY A.ItemCode, A.WhsCode, C.WhsName, B.BinCode 
                    ) as A 
                    INNER JOIN " + Global.SAP_DB + @".dbo.OBTQ on OBTQ.ItemCode collate SQL_Latin1_General_CP1_CI_AS  = A.ItemCode and 
                          OBTQ.WhsCode collate SQL_Latin1_General_CP1_CI_AS = A.WhsCode
                    INNER JOIN " + Global.SAP_DB + @".dbo.OBTN on OBTQ.[ItemCode] = OBTN.[ItemCode] AND OBTQ.[SysNumber] = OBTN.[SysNumber] and 
                          OBTN.DistNumber collate SQL_Latin1_General_CP1_CI_AS = A.BatchSerialNo
                    WHERE A.Stock > 0 and OBTQ.Quantity <> 0
                    ";

                }
                else if (payload.QRCodeID.Replace(" ", "~").ToUpper().Contains("~PRO~"))
                {
                    _Query = @"
                    SELECT @dQR QRCodeID, A.ItemCode, 
                           ( SELECT Z.ItemName FROM " + Global.SAP_DB + @".dbo.OITM Z WHERE Z.ItemCode collate SQL_Latin1_General_CP1_CI_AS = A.ItemCode) ItemName,
                           A.BatchSerialNo, A.WhsCode, A.WhsName, A.BinCode, A.Project, OBTQ.Quantity BatchQty, A.Stock
                    FROM 
                    (
	                    SELECT A.ItemCode,
			                   (select BatchSerialNo FROM QIT_Production_QR_Detail WHERE QRCodeID = @dQR) BatchSerialNo,
			                   (select Project FROM QIT_Production_QR_Detail WHERE QRCodeID = @dQR) Project,
			                   A.WhsCode, C.WhsName, B.BinCode, SUM(A.Stock) Stock 
	                    FROM 
	                    (   
		                    SELECT Z.ItemCode, Z.ToWhs WhsCode, Z.ToBinAbsEntry Bin, ISNULL(sum(Z.Qty),0) * 1 Stock 
                            FROM [QIT_ProQRStock_InvTrans] Z
		                    WHERE  Z.QRCodeID = @dQR and Z.FromObjType = '202' and Z.ToObjType = '59' and ISNULL(Z.BranchID, @bId) = @bId
		                    GROUP BY Z.ItemCode, Z.ToWhs, Z.ToBinAbsEntry	
      
		                    UNION 
	     
		                    SELECT Z.ItemCode, Z.ToWhs WhsCode, Z.ToBinAbsEntry Bin, ISNULL(sum(Z.Qty),0) * 1 Stock 
                            FROM [QIT_ProQRStock_InvTrans] Z
		                    WHERE  Z.QRCodeID = @dQR and (Z.FromObjType <> '202') and ISNULL(Z.BranchID, @bId) = @bId
		                    GROUP BY Z.ItemCode, Z.ToWhs, Z.ToBinAbsEntry

		                    UNION 

		                    SELECT Z.ItemCode, Z.FromWhs WhsCode, Z.FromBinAbsEntry Bin, ISNULL(sum(Z.Qty),0) * -1 Stock 
                            FROM [QIT_ProQRStock_InvTrans] Z
		                    WHERE Z.QRCodeID = @dQR and (Z.FromObjType <> '202') and ISNULL(Z.BranchID, @bId) = @bId
		                    GROUP BY Z.ItemCode, Z.FromWhs, Z.FromBinAbsEntry

		                    UNION 

		                    SELECT Z.ItemCode, Z.FromWhs WhsCode, Z.FromBinAbsEntry Bin, ISNULL(sum(Z.Qty),0) * -1 Stock 
                            FROM QIT_QRStock_ProToIssue Z
		                    WHERE Z.QRCodeID = @dQR and ISNULL(Z.BranchID, @bId) = @bId
		                    GROUP BY Z.ItemCode, Z.FromWhs, Z.FromBinAbsEntry

		                    UNION

		                    SELECT Z.ItemCode, Z.FromWhs WhsCode, Z.FromBinAbsEntry Bin, ISNULL(sum(Z.Qty),0) * -1 Stock 
                            FROM QIT_QRStock_SOToDelivery Z
		                    WHERE Z.QRCodeID = @dQR and ISNULL(Z.BranchID, @bId) = @bId
		                    GROUP BY Z.ItemCode, Z.FromWhs, Z.FromBinAbsEntry

	                     ) as A
	                     INNER JOIN " + Global.SAP_DB + @".dbo.OWHS C ON A.WhsCode collate SQL_Latin1_General_CP850_CI_AS = C.WhsCode
	                     LEFT JOIN " + Global.SAP_DB + @".dbo.OBIN B ON A.Bin = B.AbsEntry
	                     GROUP BY A.ItemCode, A.WhsCode, C.WhsName, B.BinCode 
                    ) as A 
                    INNER JOIN " + Global.SAP_DB + @".dbo.OBTQ on OBTQ.ItemCode collate SQL_Latin1_General_CP1_CI_AS  = A.ItemCode and 
                          OBTQ.WhsCode collate SQL_Latin1_General_CP1_CI_AS = A.WhsCode
                    INNER JOIN " + Global.SAP_DB + @".dbo.OBTN on OBTQ.[ItemCode] = OBTN.[ItemCode] AND OBTQ.[SysNumber] = OBTN.[SysNumber] and 
                          OBTN.DistNumber collate SQL_Latin1_General_CP1_CI_AS = A.BatchSerialNo
                    WHERE A.Stock > 0 and OBTQ.Quantity <> 0
                    ";
                }
                else if (payload.QRCodeID.Replace(" ", "~").ToUpper().Contains("~OS~"))
                {
                    _Query = @"
                    SELECT @dQR QRCodeID, A.ItemCode, 
                           ( SELECT Z.ItemName FROM " + Global.SAP_DB + @".dbo.OITM Z WHERE Z.ItemCode collate SQL_Latin1_General_CP1_CI_AS = A.ItemCode) ItemName,
                           A.BatchSerialNo, A.WhsCode, A.WhsName, A.BinCode, A.Project, OBTQ.Quantity BatchQty, A.Stock
                    FROM 
                    (
	                    SELECT A.ItemCode,
			                   (select BatchSerialNo FROM QIT_OpeningStock_QR_Detail WHERE QRCodeID = @dQR) BatchSerialNo,
			                   (select Project FROM QIT_OpeningStock A 
			                        INNER JOIN QIT_OpeningStock_QR_Detail B on A.OpeningNo = B.OpeningNo and A.ItemCode = B.ItemCode 
                                WHERE B.QRCodeID = @dQR) Project,
			                   A.WhsCode, C.WhsName, B.BinCode, SUM(A.Stock) Stock 
	                    FROM 
	                    (   
		                    SELECT Z.ItemCode, Z.ToWhs WhsCode, Z.ToBinAbsEntry Bin, ISNULL(sum(Z.Qty),0) * 1 Stock 
                            FROM [QIT_OSQRStock_InvTrans] Z
		                    WHERE  Z.QRCodeID = @dQR and Z.FromObjType = '310000001' and Z.ToObjType = '310000001' and 
                                   ISNULL(Z.BranchID, @bId) = @bId
		                    GROUP BY Z.ItemCode, Z.ToWhs, Z.ToBinAbsEntry	
      
		                    UNION 
	     
		                    SELECT Z.ItemCode, Z.ToWhs WhsCode, Z.ToBinAbsEntry Bin, ISNULL(sum(Z.Qty),0) * 1 Stock 
                            FROM [QIT_OSQRStock_InvTrans] Z
		                    WHERE  Z.QRCodeID = @dQR and (Z.FromObjType <> '310000001') and ISNULL(Z.BranchID, @bId) = @bId
		                    GROUP BY Z.ItemCode, Z.ToWhs, Z.ToBinAbsEntry

		                    UNION 

		                    SELECT Z.ItemCode, Z.FromWhs WhsCode, Z.FromBinAbsEntry Bin, ISNULL(sum(Z.Qty),0) * -1 Stock 
                            FROM [QIT_OSQRStock_InvTrans] Z
		                    WHERE Z.QRCodeID = @dQR and (Z.FromObjType <> '310000001') and ISNULL(Z.BranchID, @bId) = @bId
		                    GROUP BY Z.ItemCode, Z.FromWhs, Z.FromBinAbsEntry

		                    UNION 

		                    SELECT Z.ItemCode, Z.FromWhs WhsCode, Z.FromBinAbsEntry Bin, ISNULL(sum(Z.Qty),0) * -1 Stock 
                            FROM QIT_QRStock_ProToIssue Z
		                    WHERE Z.QRCodeID = @dQR and ISNULL(Z.BranchID, @bId) = @bId
		                    GROUP BY Z.ItemCode, Z.FromWhs, Z.FromBinAbsEntry

		                    UNION

		                    SELECT Z.ItemCode, Z.FromWhs WhsCode, Z.FromBinAbsEntry Bin, ISNULL(sum(Z.Qty),0) * -1 Stock 
                            FROM QIT_QRStock_SOToDelivery Z
		                    WHERE Z.QRCodeID = @dQR and ISNULL(Z.BranchID, @bId) = @bId
		                    GROUP BY Z.ItemCode, Z.FromWhs, Z.FromBinAbsEntry

	                     ) as A
	                     INNER JOIN " + Global.SAP_DB + @".dbo.OWHS C ON A.WhsCode collate SQL_Latin1_General_CP850_CI_AS = C.WhsCode
	                     LEFT JOIN " + Global.SAP_DB + @".dbo.OBIN B ON A.Bin = B.AbsEntry
	                     GROUP BY A.ItemCode, A.WhsCode, C.WhsName, B.BinCode 
                    ) as A 
                    INNER JOIN " + Global.SAP_DB + @".dbo.OBTQ on OBTQ.ItemCode collate SQL_Latin1_General_CP1_CI_AS  = A.ItemCode and 
                          OBTQ.WhsCode collate SQL_Latin1_General_CP1_CI_AS = A.WhsCode
                    INNER JOIN " + Global.SAP_DB + @".dbo.OBTN on OBTQ.[ItemCode] = OBTN.[ItemCode] AND OBTQ.[SysNumber] = OBTN.[SysNumber] and 
                          OBTN.DistNumber collate SQL_Latin1_General_CP1_CI_AS = A.BatchSerialNo
                    WHERE A.Stock > 0 and OBTQ.Quantity <> 0
                    ";
                }

                _logger.LogInformation(" ReportController :  QRScanReport Query : {q} ", _Query.ToString());

                oAdptr = new SqlDataAdapter(_Query, QITcon);
                oAdptr.SelectCommand.Parameters.AddWithValue("@bID", payload.BranchID);
                oAdptr.SelectCommand.Parameters.AddWithValue("@dQR", payload.QRCodeID.Replace(" ", "~"));

                QITcon.Open();
                oAdptr.Fill(dtData);
                QITcon.Close();

                if (dtData.Rows.Count > 0)
                {
                    List<QRScanReportOutput> obj = new List<QRScanReportOutput>();
                    dynamic arData = JsonConvert.SerializeObject(dtData);
                    obj = JsonConvert.DeserializeObject<List<QRScanReportOutput>>(arData.ToString().Replace("~", " "));
                    return obj;
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "No data found" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(" Error in ReportController : QRScanReport() :: {Error}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
            }
        }

        #endregion


        #region Log Display Log

        [HttpGet("Log_Report_Modules")]
        public async Task<ActionResult<IEnumerable<LogModules>>> Log_Report_Modules()
        {
            try
            {
                _logger.LogInformation(" Calling ReportController : Log_Report_Modules() ");
                QITcon = new SqlConnection(_QIT_connection);
                System.Data.DataTable dtData = new System.Data.DataTable();
                _Query = @"SELECT Distinct Module FROM QIT_API_Log where module!='Page Refresh for GRPO'";

                _logger.LogInformation(" ReportController :  Log_Report_Modules Query : {q} ", _Query.ToString());
                oAdptr = new SqlDataAdapter(_Query, QITcon);
                QITcon.Open();
                oAdptr.Fill(dtData);
                QITcon.Close();


                if (dtData.Rows.Count > 0)
                {
                    List<LogModules> obj = new List<LogModules>();
                    dynamic arData = JsonConvert.SerializeObject(dtData);
                    obj = JsonConvert.DeserializeObject<List<LogModules>>(arData);
                    return obj;
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "No data found" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(" Error in ReportController : Log_Report() :: {Error}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
            }
        }


        [HttpPost("Log_Report")]
        public async Task<ActionResult<IEnumerable<LogReport>>> Log_Report(LogDetails payload)
        {
            try
            {
                _logger.LogInformation(" Calling ReportController : Log_Report() ");
                string _whereModule = string.Empty;

                if (payload.BranchID <= 0 || payload.BranchID == null)
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Provide Branch" });
                }

                if (payload.FromDate == string.Empty || payload.FromDate.ToLower() == "string")
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Provide FromDate" });
                }

                if (payload.ToDate == string.Empty || payload.ToDate.ToLower() == "string")
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Provide ToDate" });
                }

                if (payload.Module == string.Empty)
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Provide Module Name" });
                }

                if (payload.UserName != string.Empty && payload.UserName != "")
                {
                    _whereModule += " AND LoginUser=@userName";
                }

                if (payload.LogLevel != string.Empty && payload.LogLevel != "")
                {
                    _whereModule += " AND LogLevel=@loglevel";
                }

                QITcon = new SqlConnection(_QIT_connection);
                System.Data.DataTable dtData = new System.Data.DataTable();
                _Query = @"SELECT 
                    BranchID,
                    Module,
                    CASE 
                        WHEN LogLevel = 'S' THEN 'Success' 
                        ELSE 'Error' 
                    END AS Status,
                    LogMessage,
                    LoginUser AS userName,
                    EntryDate AS LogDate,
                    jsonPayload
                FROM 
                    QIT_API_Log 
                WHERE EntryDate>=@frDate AND EntryDate<=@toDate AND ISNULL(BranchID, @bID) = @bID AND Module=@module " + _whereModule;


                _logger.LogInformation(" ReportController :  Log_Report Query : {q} ", _Query.ToString());
                oAdptr = new SqlDataAdapter(_Query, QITcon);
                oAdptr.SelectCommand.Parameters.AddWithValue("@frDate", payload.FromDate);
                oAdptr.SelectCommand.Parameters.AddWithValue("@toDate", payload.ToDate);
                oAdptr.SelectCommand.Parameters.AddWithValue("@module", payload.Module);
                oAdptr.SelectCommand.Parameters.AddWithValue("@bID", payload.BranchID);
                oAdptr.SelectCommand.Parameters.AddWithValue("@userName", payload.UserName);
                oAdptr.SelectCommand.Parameters.AddWithValue("@loglevel", payload.LogLevel);

                QITcon.Open();
                oAdptr.Fill(dtData);
                QITcon.Close();

                if (dtData.Rows.Count > 0)
                {
                    List<LogReport> obj = new List<LogReport>();

                    dynamic arData = JsonConvert.SerializeObject(dtData);
                    obj = JsonConvert.DeserializeObject<List<LogReport>>(arData);
                    return obj;
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "No data found" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(" Error in ReportController : Log_Report() :: {Error}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
            }
        }

        #endregion


        #region Supplier Gate IN Report

        [HttpPost("SupplierGateInDetails")]
        public async Task<ActionResult<IEnumerable<SupplierGateInDetailsReport>>> SupplierGateInDetails(SupplierGateInDetails payload)
        {
            try
            {
                _logger.LogInformation(" Calling ReportController : SupplierGateInDetails() ");
                string _wherePODocEntry = string.Empty;


                if (payload.BranchID <= 0 || payload.BranchID == null)
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Provide Branch" });
                }


                if (payload.FromDate == string.Empty || payload.FromDate.ToLower() == "string")
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Provide FromDate" });
                }


                if (payload.ToDate == string.Empty || payload.ToDate.ToLower() == "string")
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Provide ToDate" });
                }


                if (payload.VendorCode == string.Empty || payload.VendorCode == null)
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Provide Vendor Code" });
                }


                if (payload.PODocEntry != null)
                {
                    if (payload.PODocEntry > 0)
                    {
                        _wherePODocEntry = "and A.DocEntry=@PODocEntry";
                    }
                }


                QITcon = new SqlConnection(_QIT_connection);
                System.Data.DataTable dtData = new System.Data.DataTable();


                _Query = @"select A.DocEntry, A.DocNum, GateInNo, RecDate GateInDate, A.RecQty GateInQty,D.Quantity OrderQty,
                    ISNULL(ISNULL(D.Quantity,0) - ISNULL(A.RecQty,0),0) OpenQty, A.ItemCode, B.ItemName, A.Project,
                    A.UomCode, C.CardCode vendorCode,C.CardName vendorName
                    from [dbo].[QIT_GateIN] A
                    inner join QIT_Item_Master B ON A.ItemCode = B.ItemCode
                    inner join " + Global.SAP_DB + @".dbo.OPOR C ON C.DocNum=A.DocNum AND C.DocEntry=A.DocEntry AND C.CardCode= @vCode
                    inner join " + Global.SAP_DB + @".dbo.POR1 D on D.DocEntry = C.DocEntry and D.DocEntry = A.DocEntry
                    AND D.ItemCode COLLATE SQL_Latin1_General_CP1_CI_AS = A.ItemCode
                    where RecDate >= @frDate and RecDate <= @toDate and ISNULL(A.BranchID, @bID) = @bID AND A.VendorGateIN='Y' " + _wherePODocEntry;
                _logger.LogInformation(" ReportController :  SupplierGateInDetails Query : {q} ", _Query.ToString());
                  oAdptr = new SqlDataAdapter(_Query, QITcon);
                oAdptr.SelectCommand.Parameters.AddWithValue("@frDate", payload.FromDate);
                oAdptr.SelectCommand.Parameters.AddWithValue("@toDate", payload.ToDate);
                oAdptr.SelectCommand.Parameters.AddWithValue("@bID", payload.BranchID);
                oAdptr.SelectCommand.Parameters.AddWithValue("@vCode", payload.VendorCode);
                oAdptr.SelectCommand.Parameters.AddWithValue("@PODocEntry", payload.PODocEntry);


                QITcon.Open();
                oAdptr.Fill(dtData);
                QITcon.Close();


                if (dtData.Rows.Count > 0)
                {
                    List<SupplierGateInDetailsReport> obj = new List<SupplierGateInDetailsReport>();


                    dynamic arData = JsonConvert.SerializeObject(dtData);
                    obj = JsonConvert.DeserializeObject<List<SupplierGateInDetailsReport>>(arData.ToString().Replace("~", " "));
                    return obj;
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "No data found" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(" Error in ReportController : SupplierGateInDetails() :: {Error}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
            }
        }

        #endregion


        #region Supplier GRN Report

        #region GRPO Vs GateIN Report

        [HttpPost("SupplierGRNDetails")]
        public async Task<ActionResult<IEnumerable<GRNDetailsReport>>> SupplierGRNDetails(SupplierGateInDetails payload)
        {
            try
            {
                _logger.LogInformation(" Calling ReportController : SupplierGRNDetails() ");
                string _wherePODocEntry = string.Empty;

                if (payload.BranchID <= 0 || payload.BranchID == null)
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Provide Branch" });
                }

                if (payload.FromDate == string.Empty || payload.FromDate.ToLower() == "string")
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Provide FromDate" });
                }

                if (payload.ToDate == string.Empty || payload.ToDate.ToLower() == "string")
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Provide ToDate" });
                }


                if (payload.PODocEntry != null)
                {
                    if (payload.PODocEntry > 0)
                    {
                        _wherePODocEntry = "and A.DocEntry=@PODocEntry";
                    }
                }

                QITcon = new SqlConnection(_QIT_connection);
                System.Data.DataTable dtData = new System.Data.DataTable();

                _Query = @"
        SELECT DISTINCT A.DocNum AS PO_DocNum, A.DocEntry AS PO_DocEntry, A.GateInNo, A.RecDate AS GateIn_Date, F.DocDate AS PO_DocDate,
               G.WhsCode AS PO_WhsCode, E.WhsCode AS GRN_WhsCode, A.ItemCode, G.Dscription AS ItemName, A.RecQty AS GateInQty,
               G.Quantity AS PO_Qty, E.Quantity as GRN_Qty, D.DocNum AS GRN_DocNum, D.DocDate AS GRN_DocDate, 
               F.CardCode AS Vendor_Code, F.CardName AS Vendor_Name
        FROM QIT_GateIN A
             LEFT JOIN QIT_QRStock_POToGRPO B ON A.GateInNo = B.GateInNo AND A.ItemCode = B.ItemCode
             LEFT JOIN QIT_Trans_POToGRPO C ON B.TransSeq = C.Transseq AND C.BaseDocEntry = A.DocEntry
             LEFT JOIN " + Global.SAP_DB + @".dbo.OPOR F ON F.DocEntry = A.DocEntry AND 
                  F.ObjType COLLATE SQL_Latin1_General_CP1_CI_AS = A.ObjType AND F.BPLId = A.BranchID AND F.CardCode=@vcode
             LEFT JOIN " + Global.SAP_DB + @".dbo.POR1 G ON F.DocEntry = G.DocEntry AND 
                  A.ItemCode COLLATE SQL_Latin1_General_CP1_CI_AS = G.ItemCode AND A.LineNum = G.LineNum
             LEFT JOIN " + Global.SAP_DB + @".dbo.OPDN D ON C.DocEntry = D.DocEntry AND C.DocNum = D.DocNum
             LEFT JOIN " + Global.SAP_DB + @".dbo.PDN1 E ON F.DocNum = E.BaseRef AND G.DocEntry = E.BaseEntry AND E.DocEntry = C.DocEntry AND 
                  E.ItemCode  COLLATE SQL_Latin1_General_CP1_CI_AS=G.ItemCode AND E.BaseLine = G.LineNum AND
                  F.ObjType COLLATE SQL_Latin1_General_CP1_CI_AS = E.BaseType
        WHERE A.RecDate >= @frDate AND A.RecDate <= @toDate and ISNULL(A.BranchID, @bID) = @bID AND A.DocEntry=F.DocEntry AND 
A.DocNum=F.DocNum AND A.VendorGateIn = 'Y' " + _wherePODocEntry;

                _logger.LogInformation(" ReportController :  SupplierGRNDetails Query : {q} ", _Query.ToString());

                 oAdptr = new SqlDataAdapter(_Query, QITcon);

                oAdptr.SelectCommand.Parameters.AddWithValue("@frDate", payload.FromDate);
                oAdptr.SelectCommand.Parameters.AddWithValue("@toDate", payload.ToDate);
                oAdptr.SelectCommand.Parameters.AddWithValue("@bID", payload.BranchID);
                oAdptr.SelectCommand.Parameters.AddWithValue("@vcode", payload.VendorCode);
                oAdptr.SelectCommand.Parameters.AddWithValue("@PODocEntry", payload.PODocEntry);

                QITcon.Open();
                oAdptr.Fill(dtData);
                QITcon.Close();

                if (dtData.Rows.Count > 0)
                {
                    List<GRNDetailsReport> obj = new List<GRNDetailsReport>();
                    dynamic arData = JsonConvert.SerializeObject(dtData);
                    obj = JsonConvert.DeserializeObject<List<GRNDetailsReport>>(arData.ToString().Replace("~", " "));
                    return obj;
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "No data found" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(" Error in ReportController : SupplierGRNDetails() :: {Error}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
            }
        }

        #endregion

        #endregion


        #region New Production Report in Production Menu

        // First bind Project 
        // Display help of Production Order by selected Project

        [HttpGet("GetProductionListByProject")]
        public async Task<ActionResult<IEnumerable<ProOrd>>> GetProductionListByProject(string Project)
        {
            try
            {
                _logger.LogInformation(" Calling ReportController : GetProductionListByProject() ");
                List<ProOrd> obj = new List<ProOrd>();
                System.Data.DataTable dtData = new System.Data.DataTable();
                QITcon = new SqlConnection(_QIT_connection);

                string _where = string.Empty;

                _Query = @" select DocEntry, DocNum, ItemCode,
                            (select ItemName from " + Global.SAP_DB + @".dbo.OITM where ItemCode = OWOR.ItemCode) ItemName
                            from " + Global.SAP_DB + @".dbo.OWOR where /*Type = 'S' and */Status ='R' and Project = @proj ";

                _logger.LogInformation(" ReportController : GetProductionListByProject() Query : {q} ", _Query.ToString());
                QITcon.Open();
                oAdptr = new SqlDataAdapter(_Query, QITcon);
                oAdptr.SelectCommand.Parameters.AddWithValue("@proj", Project);
                oAdptr.Fill(dtData);
                QITcon.Close();

                if (dtData.Rows.Count > 0)
                {
                    dynamic arData = JsonConvert.SerializeObject(dtData);
                    obj = JsonConvert.DeserializeObject<List<ProOrd>>(arData.ToString());
                    return obj;
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "No data found" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(" Error in ReportController : GetProductionListByProject() :: {Error}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
            }
        }


        [HttpPost("ProductionItemStock")]
        public async Task<ActionResult<IEnumerable<ItemWiseQRWiseStockReport>>> ProductionItemStock(int BranchId, int ProOrdDocEntry, string Project)
        {
            try
            {
                _logger.LogInformation(" Calling ReportController : ProductionItemStock() ");

                QITcon = new SqlConnection(_QIT_connection);
                System.Data.DataTable dtData = new System.Data.DataTable();
                string _whereItemCodes = string.Empty;

                #region Validation

                if (BranchId == 0)
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Provide Branch" });
                }

                if (Project == string.Empty || Project.ToLower() == "string")
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Provide Project" });
                }

                if (ProOrdDocEntry == 0)
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Provide Production DocEntry" });
                }

                #endregion


                _whereItemCodes = @" AND Z.QRCodeID IN (select QRCodeID from QIT_QR_Detail B inner join " + Global.SAP_DB + @".dbo.WOR1 C on C.ItemCode collate SQL_Latin1_General_CP1_CI_AS = B.ItemCode and C.DocEntry = @proDocEntry and C.Project = @proj) ";


                _Query = @"
                 SELECT  A.ItemCode, (select ItemName from QIT_Item_Master where ItemCode = A.ItemCode)  ItemName,
                         @proj Project, A.WhsCode, C.WhsName, B.BinCode, SUM(A.Stock) Stock 
                 FROM 
                 (   
                     SELECT Z.ItemCode, Z.ToWhs WhsCode, Z.ToBinAbsEntry Bin, ISNULL(sum(Z.Qty),0) * 1 Stock FROM QIT_QRStock_InvTrans Z
                     WHERE  Z.FromObjType = '22' and Z.ToObjType = '67' and ISNULL(Z.BranchID, @bID) = @bID " + _whereItemCodes + @"
                     GROUP BY Z.ItemCode, Z.ToWhs, Z.ToBinAbsEntry	
      
                     UNION 
	     
                     SELECT Z.ItemCode, Z.ToWhs WhsCode, Z.ToBinAbsEntry Bin, ISNULL(sum(Z.Qty),0) * 1 Stock FROM QIT_QRStock_InvTrans Z
                     WHERE  (Z.FromObjType <> '22') and ISNULL(Z.BranchID, @bID) = @bID " + _whereItemCodes + @"
                     GROUP BY Z.ItemCode, Z.ToWhs, Z.ToBinAbsEntry

                     UNION 

                     SELECT Z.ItemCode, Z.FromWhs WhsCode, Z.FromBinAbsEntry Bin, ISNULL(sum(Z.Qty),0) * -1 Stock FROM [QIT_QRStock_InvTrans] Z
                     WHERE  (Z.FromObjType <> '22') and ISNULL(Z.BranchID, @bID) = @bID " + _whereItemCodes + @"
                     GROUP BY Z.ItemCode, Z.FromWhs, Z.FromBinAbsEntry

                     UNION 

                     SELECT Z.ItemCode, Z.FromWhs WhsCode, Z.FromBinAbsEntry Bin, ISNULL(sum(Z.Qty),0) * -1 Stock FROM QIT_QRStock_ProToIssue Z
                     WHERE  ISNULL(Z.BranchID, @bID) = @bID " + _whereItemCodes + @"
                     GROUP BY Z.ItemCode, Z.FromWhs, Z.FromBinAbsEntry

                     UNION

                     SELECT Z.ItemCode, Z.FromWhs WhsCode, Z.FromBinAbsEntry Bin, ISNULL(sum(Z.Qty),0) * -1 Stock 
                     FROM QIT_QRStock_SOToDelivery Z
                     WHERE  ISNULL(Z.BranchID, @bID) = @bID " + _whereItemCodes + @"
                     GROUP BY Z.ItemCode, Z.FromWhs, Z.FromBinAbsEntry

                     UNION

                     SELECT Z.ItemCode, Z.ToWhs WhsCode, Z.ToBinAbsEntry Bin, ISNULL(sum(Z.Qty),0) * 1 Stock FROM QIT_QRStock_ProToReceipt Z
                     WHERE  ISNULL(Z.BranchID, @bID) = @bID " + _whereItemCodes + @"
                     GROUP BY Z.ItemCode, Z.ToWhs, Z.ToBinAbsEntry

                  ) as A
                  INNER JOIN " + Global.SAP_DB + @".dbo.OWHS C ON A.WhsCode collate SQL_Latin1_General_CP850_CI_AS = C.WhsCode
                  LEFT JOIN " + Global.SAP_DB + @".dbo.OBIN B ON A.Bin = B.AbsEntry
                  GROUP BY ItemCode,  A.WhsCode, C.WhsName, B.BinCode 
                  HAVING SUM(A.Stock) > 0
                ";

                _logger.LogInformation(" ReportController : ProductionItemStock Query : {q} ", _Query.ToString());
                oAdptr = new SqlDataAdapter(_Query, QITcon);
                oAdptr.SelectCommand.Parameters.AddWithValue("@bID", BranchId);
                oAdptr.SelectCommand.Parameters.AddWithValue("@proj", Project);
                oAdptr.SelectCommand.Parameters.AddWithValue("@proDocEntry", ProOrdDocEntry);

                QITcon.Open();
                oAdptr.Fill(dtData);
                QITcon.Close();

                if (dtData.Rows.Count > 0)
                {
                    List<ItemWiseQRWiseStockReport> obj = new List<ItemWiseQRWiseStockReport>();
                    dynamic arData = JsonConvert.SerializeObject(dtData);
                    obj = JsonConvert.DeserializeObject<List<ItemWiseQRWiseStockReport>>(arData.ToString().Replace("~", " "));
                    return obj;
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "No data found" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(" Error in ReportController : ProductionItemStock() :: {Error}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
            }
        }

        #endregion

    }
}
