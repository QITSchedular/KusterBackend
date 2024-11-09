using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using NLog.LayoutRenderers;
using System.Data.SqlClient;
using System.Net.Http;
using System.Xml.Linq;
using WMS_UI_API.Common;
using WMS_UI_API.Models;

using System.Net.Http;
using System.Data;
using Microsoft.Extensions.Logging.Abstractions;
using SAPbobsCOM;
using WMS_UI_API.Services;

namespace WMS_UI_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class IncomingQCController : ControllerBase
    {
        private string _ApplicationApiKey = string.Empty;
        private string _connection = string.Empty;
        private string _QIT_connection = string.Empty;
        private string _QIT_DB = string.Empty;
        private string _Query = string.Empty;
        private SqlCommand cmd;
        public Global objGlobal;
       
        public IConfiguration Configuration { get; }
        private readonly ILogger<IncomingQCController> _logger;
        private readonly ISAPConnectionService _sapConnectionService;

        public IncomingQCController(IConfiguration configuration, ILogger<IncomingQCController> logger, ISAPConnectionService sapConnectionService)
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

            }
            catch (Exception ex)
            {
                _logger.LogError(" Error in IncomingQCController :: {Error}" + ex.ToString());
            }
        }


        [HttpPost("GetPOList")]
        public async Task<ActionResult<IEnumerable<POList>>> GetPOList(GRPOListFilter payload)
        {
            SqlConnection QITcon;
            SqlDataAdapter oAdptr;
            try
            {
                _logger.LogInformation(" Calling IncomingQCController : GetPOList() ");
                List<POList> obj = new List<POList>();
                System.Data.DataTable dtData = new System.Data.DataTable();
                QITcon = new SqlConnection(_QIT_connection);
                string _where = string.Empty;

                #region Validation
                if (payload.BranchID == 0)
                {
                    return BadRequest(new
                    {
                        StatusCode = "400",
                        StatusMsg = "Provide Branch"
                    });
                }
                #endregion

                #region Filter
                if (payload.FromDate != String.Empty && payload.FromDate.ToLower() != "string")
                {
                    _where += " AND A.DocDate >= @frDate";
                }

                if (payload.ToDate != String.Empty && payload.ToDate.ToLower() != "string")
                {
                    _where += " AND A.DocDate <= @toDate";
                }
                #endregion

                #region Query
                _Query = @"
                SELECT DISTINCT A.QRCodeID, A.PODocEntry, A.PODocNum, B.CardCode, B.CardName, B.NumAtCard, B.Series, C.SeriesName
                FROM 
                (
	                SELECT Z.*, CASE WHEN Z.QCQty = 0 THEN 'Pending' WHEN Z.GRPOQtyCount = Z.QCQty THEN 'Done' ELSE 'Partial' END Status
	                FROM 
	                (
		                SELECT *,
				               ISNULL((SELECT SUM(B.Qty) FROM QIT_QRStock_POToGRPO B 
						               WHERE B.TransSeq in ( SELECT TransSeq FROM QIT_Trans_POToGRPO A WHERE A.DocNum = Z.GRPODocNum ))
				               ,0) GRPOQtyCount,
				               ISNULL((SELECT sum(Qty) FROM QIT_QC_Detail where GRPODocNum = Z.GRPODocNum),0) QCQty  
		                FROM 
		                (
			                SELECT DISTINCT A.QRCodeID, A.DocNum PODocNum, A.DocEntry PODocEntry, B.DocNum GRPODocNum, 
                                            C.CardCode, C.CardName, C.NumAtCard, C.Series, D.SeriesName
                            FROM QIT_QR_Header A
				                 INNER JOIN QIT_Trans_POToGRPO B ON A.DocEntry = B.BaseDocEntry AND A.DocNum = B.BaseDocNum 
				                 INNER JOIN " + Global.SAP_DB + @".dbo.OPOR C on A.DocEntry = C.DocEntry and C.Canceled = 'N'
				                 INNER JOIN " + Global.SAP_DB + @".dbo.POR1 F ON C.DocEntry = F.DocEntry
				                 INNER JOIN " + Global.SAP_DB + @".dbo.NNM1 D on C.Series = D.Series
				                 INNER JOIN " + Global.SAP_DB + @".dbo.OITM E ON E.ItemCode = F.ItemCode and E.U_QA in (1,3)
			                WHERE ISNULL(A.BranchID, @bID) = @bID  " + _where + @"
		                ) as Z
	                ) as Z
                )  as A
                INNER JOIN " + Global.SAP_DB + @".dbo.OPOR B ON A.PODocEntry = B.DocEntry
                INNER JOIN " + Global.SAP_DB + @".dbo.NNM1 C on C.Series = B.Series
                WHERE A.Status in ('Partial','Pending')
                ";
                #endregion

                _logger.LogInformation(" IncomingQCController : GetPOList() Query : {q} ", _Query.ToString());
                QITcon.Open();
                oAdptr = new SqlDataAdapter(_Query, QITcon);
                oAdptr.SelectCommand.Parameters.AddWithValue("@bID", payload.BranchID);
                if (payload.FromDate != String.Empty && payload.FromDate.ToLower() != "string")
                {
                    oAdptr.SelectCommand.Parameters.AddWithValue("@frDate", payload.FromDate);
                }

                if (payload.ToDate != String.Empty && payload.ToDate.ToLower() != "string")
                {
                    oAdptr.SelectCommand.Parameters.AddWithValue("@toDate", payload.ToDate);
                }
                oAdptr.Fill(dtData);
                QITcon.Close();

                if (dtData.Rows.Count > 0)
                {
                    dynamic arData = JsonConvert.SerializeObject(dtData);
                    obj = JsonConvert.DeserializeObject<List<POList>>(arData.ToString().Replace("~", " "));
                    return obj;
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "No data found" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(" Error in IncomingQCController : GetPOList() :: {Error}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
            }
        }


        [HttpPost("GetGRPOListByPO")]
        public async Task<ActionResult<IEnumerable<GRPOList>>> GetGRPOListByPO(GRPOListFilter payload)
        {
            SqlConnection SAPcon;
            try
            {
                _logger.LogInformation(" Calling IncomingQCController : GetGRPOListByPO() ");
                List<GRPOList> obj = new List<GRPOList>();
                System.Data.DataTable dtData = new System.Data.DataTable();
                SAPcon = new SqlConnection(_connection);
                string _where = string.Empty;

                #region Validation

                if (payload.BranchID == 0)
                {
                    return BadRequest(new
                    {
                        StatusCode = "400",
                        StatusMsg = "Provide Branch"
                    });
                }

                if (payload.HeaderQRCodeID == String.Empty || payload.HeaderQRCodeID.ToLower() == "string")
                {
                    return BadRequest(new
                    {
                        StatusCode = "400",
                        StatusMsg = "Provide Header QR"
                    });
                }

                #endregion

                #region Filter

                if (payload.FromDate != String.Empty && payload.FromDate.ToLower() != "string")
                {
                    _where += " AND A.DocDate >= @frDate";
                }

                if (payload.ToDate != String.Empty && payload.ToDate.ToLower() != "string")
                {
                    _where += " AND A.DocDate <= @toDate";
                }
                #endregion

                #region Query
                _Query = @" 
                SELECT * FROM (
                SELECT Z.*, CASE WHEN Z.QCQty = 0 THEN 'Pending' WHEN Z.GRPOQtyCount = Z.QCQty THEN 'Done' ELSE 'Partial' END Status
                FROM (
                    SELECT Z.*, 
                           ISNULL((select SUM(B.Qty) from " + Global.QIT_DB + @".dbo.QIT_QRStock_POToGRPO B 
                                   where B.TransSeq in ( select TransSeq from " + Global.QIT_DB + @".dbo.QIT_TRAns_POToGRPO A where A.DocNum = Z.DocNum ))
                           ,0) GRPOQtyCount,
                           ISNULL((select sum(Qty) from " + Global.QIT_DB + @".dbo.QIT_QC_Detail where GRPODocNum = Z.DocNum),0) QCQty 
                    FROM 
                    (
                        SELECT '" + payload.HeaderQRCodeID.Replace("~", " ") + @"' HeaderQRCodeID, C.DocEntry, C.DocNum, C.CardCode, C.CardName, C.NumAtCard, C.Series, D.SeriesName, 
                               CONCAT(CONVERT(VARCHAR(10), C.DocDate, 120), ' ', 
							   STUFF(STUFF(RIGHT('000000' + CONVERT(VARCHAR(6), C.CreateTS, 114), 6), 3, 0, ':'), 6, 0, ':')) AS PostDate, 
                               C.TaxDate DocDate, C.Project, A.PODocEntry, A.PODocNum , C.Comments 
                        FROM 
                        (
                            SELECT DISTINCT B.DocEntry, A.DocNum PODocNum, A.DocEntry PODocEntry
                            FROM " + Global.QIT_DB + @".dbo.QIT_QR_Header  A
                                 INNER JOIN " + Global.QIT_DB + @".dbo.QIT_Trans_POToGRPO B ON A.DocEntry = B.BaseDocEntry AND A.DocNum = B.BaseDocNum 
                            WHERE A.QRCodeID = @hQR AND ISNULL(A.BranchID, 1) = @bID " + _where + @"
                        ) as A INNER JOIN OPDN C ON A.DocEntry = C.DocEntry INNER JOIN nnm1 D ON D.Series = C.Series
                        WHERE C.CANCELED = 'N' 
                    ) as Z
                ) as Z
                ) as Z WHERE Z.Status in ('Pending', 'Partial')
                ";

                #endregion

                _logger.LogInformation(" IncomingQCController : GetGRPOListByPO() Query : {q} ", _Query.ToString());
                SAPcon.Open();
                SqlDataAdapter oAdptr = new SqlDataAdapter(_Query, SAPcon);
                oAdptr.SelectCommand.Parameters.AddWithValue("@bID", payload.BranchID);
                oAdptr.SelectCommand.Parameters.AddWithValue("@hQR", payload.HeaderQRCodeID.Replace(" ", "~"));
                if (payload.FromDate != String.Empty && payload.FromDate.ToLower() != "string")
                {
                    oAdptr.SelectCommand.Parameters.AddWithValue("@frDate", payload.FromDate);
                }

                if (payload.ToDate != String.Empty && payload.ToDate.ToLower() != "string")
                {
                    oAdptr.SelectCommand.Parameters.AddWithValue("@toDate", payload.ToDate);
                }
                oAdptr.Fill(dtData);
                SAPcon.Close();

                if (dtData.Rows.Count > 0)
                {
                    dynamic arData = JsonConvert.SerializeObject(dtData);
                    obj = JsonConvert.DeserializeObject<List<GRPOList>>(arData.ToString().Replace("~", " "));
                    return obj;
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "No data found" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(" Error in IncomingQCController : GetGRPOListByPO() :: {Error}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
            }
        }


        [HttpPost("ValidateItem")]
        public async Task<ActionResult<IEnumerable<GRPOItem>>> ValidateItem(GRPOItemFilter payload)
        {
            SqlConnection QITcon;
            SqlDataAdapter oAdptr;
            try
            {
                _logger.LogInformation(" Calling IncomingQCController : ValidateItem() ");
                List<GRPOItem> obj = new List<GRPOItem>();
                System.Data.DataTable dtData = new System.Data.DataTable();
                QITcon = new SqlConnection(_QIT_connection);
                string _where = string.Empty;

                #region Validation
                if (payload.BranchID == 0)
                {
                    return BadRequest(new
                    {
                        StatusCode = "400",
                        StatusMsg = "Provide Branch"
                    });
                }

                if (payload.HeaderQRCodeID == String.Empty || payload.HeaderQRCodeID.ToLower() == "string")
                {
                    return BadRequest(new
                    {
                        StatusCode = "400",
                        StatusMsg = "Provide Header QR"
                    });
                }

                if (payload.DetailQRCodeID == String.Empty || payload.DetailQRCodeID.ToLower() == "string")
                {
                    return BadRequest(new
                    {
                        StatusCode = "400",
                        StatusMsg = "Provide Detail QR"
                    });
                }

                if (payload.GRPODocEntry == 0)
                {
                    return BadRequest(new
                    {
                        StatusCode = "400",
                        StatusMsg = "Provide GRPO Document Entry"
                    });
                }
                #endregion

                #region Check for QC applicable or not 
                _Query = @" SELECT A.ItemCode, A.QRCodeID, B.U_QA
                            FROM QIT_QRStock_POToGRPO A 
                            INNER JOIN " + Global.SAP_DB + @".dbo.OITM B ON A.ItemCode collate SQL_Latin1_General_CP850_CI_AS = B.ItemCode
                            WHERE A.QRCodeID = @dQR AND B.U_QA in (1,3) AND ISNULL(A.BranchID, @bID) = @bID ";

                _logger.LogInformation(" IncomingQCController : ValidateItem() Query : {q} ", _Query.ToString());
                QITcon.Open();
                oAdptr = new SqlDataAdapter(_Query, QITcon);
                oAdptr.SelectCommand.Parameters.AddWithValue("@bID", payload.BranchID);
                oAdptr.SelectCommand.Parameters.AddWithValue("@dQR", payload.DetailQRCodeID.Replace(" ", "~"));
                oAdptr.Fill(dtData);
                QITcon.Close();

                if (dtData.Rows.Count <= 0)
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "QC is not applicable for QR : " + payload.DetailQRCodeID.Replace("~", " ") });
                }
                #endregion

                #region Check already QC is done or not
                DataTable dtQCData = new DataTable();

                _Query = @" SELECT * FROM 
                            (
                                SELECT 
                                (
                                    SELECT ISNULL(SUM(A.Qty),0) from QIT_QR_detail A
                                    WHERE A.QRCodeID = @dQR AND ISNULL(A.BranchID, @bID) = @bID
                                ) -
                                (
                                    SELECT ISNULL(SUM(A.Qty),0) QCQty FROM QIT_QC_Detail A   
                                    WHERE A.QRCodeID = @dQR AND ISNULL(A.BranchID, @bID) = @bID
                                ) as PendQty
                            ) as A
                            where A.PendQty = 0  ";
                _logger.LogInformation(" IncomingQCController : ValidateItemQR : QC already done Query : {q} ", _Query.ToString());
                QITcon.Open();
                oAdptr = new SqlDataAdapter(_Query, QITcon);
                oAdptr.SelectCommand.Parameters.AddWithValue("@bID", payload.BranchID);
                oAdptr.SelectCommand.Parameters.AddWithValue("@dQR", payload.DetailQRCodeID.Replace(" ", "~"));
                oAdptr.Fill(dtQCData);
                QITcon.Close();

                if (dtQCData.Rows.Count > 0)
                {
                    return BadRequest(new
                    {
                        StatusCode = "400",
                        StatusMsg = "QC is already done for : " + payload.DetailQRCodeID.Replace("~", " ")
                    });
                }

                #endregion

                #region Query
                dtData = new DataTable();
                _Query = @" 
                SELECT A.* FROM
                (
	                SELECT A.DocEntry PODocEntry, A.DocNum PODocNum, A.QRCodeID HeaderQRCodeID, B.QRCodeID DetailQRCodeID, B.GateInNo,
                           C.DocEntry GRPODocEntry, C.DocNum GRPODocNum, C.CardCode, C.CardName,
		                   C.DocDate, D.ItemCode, D.Dscription ItemName, D.Quantity RecQty, B.Qty QRQty, 
			               ( SELECT ISNULL(sum(Qty),0) FROM QIT_QC_Detail WHERE QRCodeID = @dQR ) QCQty,
                           D.Project, D.UomCode, D.WhsCode,
                           E.ToWhs FromWhs, ISNULL(E.ToBinAbsEntry,0) FromBin, 
                           (select BinCode from " + Global.SAP_DB + @".dbo.OBIN where AbsEntry = E.ToBinAbsEntry) FromBinCode
	                FROM   QIT_QR_Header A 
		                   INNER JOIN QIT_QR_Detail B ON A.HeaderSrNo = B.HeaderSrNo
		                   INNER JOIN " + Global.SAP_DB + @".dbo.OPDN C ON C.CANCELED = 'N' AND C.DocEntry = @grpoDocEntry and C.DocEntry = 
			                            ( SELECT distinct DocEntry FROM QIT_Trans_POToGRPO Z1 
                                        inner join QIT_QRStock_POToGRPO Z2 ON                     
                                        Z1.TransSeq = Z2.TransSeq where Z2.QRCodeID = @dQR)
		                   INNER JOIN " + Global.SAP_DB + @".dbo.PDN1 D ON D.DocEntry = C.DocEntry AND 
                                                D.ItemCode collate SQL_Latin1_General_CP850_CI_AS = B.ItemCode AND D.BaseLine = B.LineNum
                           INNER JOIN QIT_QRStock_InvTrans E ON E.QRCodeID = B.QRCodeID
                           INNER JOIN " + Global.SAP_DB + @".dbo.POR1 F ON F.DocEntry = D.BaseEntry and F.LineNum = D.BaseLine
                           INNER JOIN " + Global.SAP_DB + @".dbo.OPOR G ON G.DocEntry = F.DocEntry and G.ObjType = D.BaseType and 
                                        G.DocEntry = A.DocEntry		
	                WHERE A.QRCodeID = @hQR AND B.QRCodeID = @dQR AND ISNULL(A.BranchID, @bID) = @bID AND
                            E.TransId = (select max(TRansID) FROM QIT_QRStock_InvTrans 
where QRCodeID = @dQR and FromObjType = '22' and ToObjType = '67' and 
				ItemCode = B.ItemCode AND GateInNo = B.GateInNo )
                ) as A ";

                #endregion

                _logger.LogInformation(" IncomingQCController : ValidateItem() Query : {q} ", _Query.ToString());
                QITcon.Open();
                oAdptr = new SqlDataAdapter(_Query, QITcon);
                oAdptr.SelectCommand.Parameters.AddWithValue("@bID", payload.BranchID);
                oAdptr.SelectCommand.Parameters.AddWithValue("@hQR", payload.HeaderQRCodeID.Replace(" ", "~"));
                oAdptr.SelectCommand.Parameters.AddWithValue("@dQR", payload.DetailQRCodeID.Replace(" ", "~"));
                oAdptr.SelectCommand.Parameters.AddWithValue("@grpoDocEntry", payload.GRPODocEntry);
                oAdptr.Fill(dtData);
                QITcon.Close();

                if (dtData.Rows.Count > 0)
                {
                    dynamic arData = JsonConvert.SerializeObject(dtData);
                    obj = JsonConvert.DeserializeObject<List<GRPOItem>>(arData.ToString().Replace("~", " "));
                    return obj;
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "No data found" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(" Error in IncomingQCController : ValidateItem() :: {Error}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
            }
        }


        //[HttpPost("QC")]
        //public async Task<ActionResult<IEnumerable<string>>> QC(QCPayload payload)
        //{
        //    SqlConnection QITcon;
        //    try
        //    {
        //        _logger.LogInformation(" Calling IncomingQCController : QC() ");

        //        System.Data.DataTable dtQRData = new System.Data.DataTable();
        //        System.Data.DataTable dtGRPOItemData = new System.Data.DataTable();
        //        DataTable dtConfig = new DataTable();
        //        System.Data.DataTable dtQCData = new System.Data.DataTable();
        //        string _ToWhsCode = string.Empty;
        //        int _ToBinAbsEntry = 0;

        //        #region Validation
        //        if (payload.BranchID == 0)
        //        {
        //            return BadRequest(new
        //            {
        //                StatusCode = "400",
        //                StatusMsg = "Provide Branch"
        //            });
        //        }

        //        if (payload.GRPODocEntry == 0)
        //        {
        //            return BadRequest(new
        //            {
        //                StatusCode = "400",
        //                StatusMsg = "Provide GRPO Document Entry"
        //            });
        //        }

        //        if (payload.DetailQRCodeID == String.Empty || payload.DetailQRCodeID.ToLower() == "string")
        //        {
        //            return BadRequest(new
        //            {
        //                StatusCode = "400",
        //                StatusMsg = "Provide Detail QR"
        //            });
        //        }

        //        if (payload.Comment == String.Empty || payload.Comment.ToLower() == "string")
        //        {
        //            return BadRequest(new
        //            {
        //                StatusCode = "400",
        //                StatusMsg = "Provide Comment"
        //            });
        //        }

        //        #endregion

        //        #region Check already QC is done or not
        //        QITcon = new SqlConnection(_QIT_connection);

        //        _Query = @" SELECT * FROM 
        //                    (
        //                        SELECT 
        //                        (
        //                            SELECT ISNULL(SUM(A.Qty),0) from QIT_QR_detail A
        //                            WHERE A.QRCodeID = @dQR AND ISNULL(A.BranchID, @bID) = @bID
        //                        ) -
        //                        (
        //                            SELECT ISNULL(SUM(A.Qty),0) QCQty FROM QIT_QC_Detail A   
        //                            WHERE A.QRCodeID = @dQR AND ISNULL(A.BranchID, @bID) = @bID
        //                        ) as PendQty
        //                    ) as A
        //                    where A.PendQty = 0  ";
        //        _logger.LogInformation(" IncomingQCController : QC already done Query : {q} ", _Query.ToString());
        //        QITcon.Open();
        //        SqlDataAdapter oAdptr = new SqlDataAdapter(_Query, QITcon);
        //        oAdptr.SelectCommand.Parameters.AddWithValue("@bID", payload.BranchID);
        //        oAdptr.SelectCommand.Parameters.AddWithValue("@dQR", payload.DetailQRCodeID.Replace(" ", "~"));
        //        oAdptr.Fill(dtQCData);
        //        QITcon.Close();

        //        if (dtQCData.Rows.Count > 0)
        //        {
        //            return BadRequest(new
        //            {
        //                StatusCode = "400",
        //                StatusMsg = "QC is already done for : " + payload.DetailQRCodeID.Replace("~", " ")
        //            });
        //        }

        //        #endregion

        //        #region Get Item data from DetailQRCodeID
        //        QITcon = new SqlConnection(_QIT_connection);

        //        _Query = @" SELECT A.*, B.ItemMngBy, C.DocNum
        //                    FROM QIT_QR_Detail A  
        //                    INNER JOIN QIT_Item_Master B on A.ItemCode = B.ItemCode
        //                    INNER JOIN QIT_QR_Header C on A.HeaderSrNo = C.HeaderSrNo
        //                    WHERE A.QRCodeID = @dQR AND ISNULL(A.BranchID, @bID) = @bID  ";
        //        _logger.LogInformation(" IncomingQCController : QRCode Query : {q} ", _Query.ToString());
        //        QITcon.Open();
        //        oAdptr = new SqlDataAdapter(_Query, QITcon);
        //        oAdptr.SelectCommand.Parameters.AddWithValue("@bID", payload.BranchID);
        //        oAdptr.SelectCommand.Parameters.AddWithValue("@dQR", payload.DetailQRCodeID.Replace(" ", "~"));
        //        oAdptr.Fill(dtQRData);
        //        QITcon.Close();
        //        #endregion

        //        #region Get Approve and Reject Warehouse
        //        QITcon = new SqlConnection(_QIT_connection);

        //        _Query = @" SELECT * FROM QIT_Config_Master A WHERE ISNULL(A.BranchID, @bID) = @bID  ";
        //        _logger.LogInformation(" IncomingQCController : Configuration  Query : {q} ", _Query.ToString());
        //        QITcon.Open();
        //        oAdptr = new SqlDataAdapter(_Query, QITcon);
        //        oAdptr.SelectCommand.Parameters.AddWithValue("@bID", payload.BranchID);
        //        oAdptr.Fill(dtConfig);
        //        QITcon.Close();
        //        #endregion

        //        #region Check for Valid Whs
        //        if (payload.Action.ToUpper() == "A" && dtConfig.Rows[0]["ApprovedWhs"].ToString().Trim().Length <= 0)
        //            return BadRequest(new
        //            {
        //                StatusCode = "400",
        //                StatusMsg = "Define Approve Warehouse in Configuration"
        //            });

        //        if (payload.Action.ToUpper() == "R" && dtConfig.Rows[0]["RejectedWhs"].ToString().Trim().Length <= 0)
        //            return BadRequest(new
        //            {
        //                StatusCode = "400",
        //                StatusMsg = "Define Reject Warehouse in Configuration"
        //            });

        //        _ToWhsCode = payload.Action.ToUpper() == "A" ? dtConfig.Rows[0]["ApprovedWhs"].ToString().Trim() : dtConfig.Rows[0]["RejectedWhs"].ToString().Trim();
        //        _ToBinAbsEntry = payload.Action.ToUpper() == "A" ? int.Parse(dtConfig.Rows[0]["ApproveBin"].ToString().Trim()) : int.Parse(dtConfig.Rows[0]["RejectBin"].ToString().Trim());
        //        #endregion

        //        #region Check for QC IT Series
        //        if (dtConfig.Rows[0]["QCITSeries"].ToString().Trim().Length <= 0)
        //            return BadRequest(new
        //            {
        //                StatusCode = "400",
        //                StatusMsg = "Define QC Inventory Transfer Series in Configuration"
        //            });
        //        #endregion

        //        if (dtQRData.Rows.Count > 0)
        //        {
        //            #region Get GRPO Detail

        //            _Query = @" 
        //            SELECT A.DocEntry, A.DocNum, A.CardCode, A.CardName, A.Series, A.DocDate, A.TaxDate, 
        //                   B.ItemCode, B.Dscription ItemName, B.Project, B.Quantity
        //            FROM " + Global.SAP_DB + @".dbo.OPDN A INNER JOIN " + Global.SAP_DB + @".dbo.PDN1 B ON A.DocEntry = B.DocEntry 
        //            WHERE A.DocEntry = @grpoDocEntry and B.ItemCode = @itemCode and B.BaseLine = @line and B.BaseDocNum = @bDocNum and 
        //                  A.CANCELED = 'N' and ISNULL(A.BPLId, @bID) = @bID ";
        //            _logger.LogInformation(" IncomingQCController : GRPO Data Query : {q} ", _Query.ToString());
        //            QITcon.Open();
        //            SqlDataAdapter oAdptr1 = new SqlDataAdapter(_Query, QITcon);
        //            oAdptr1.SelectCommand.Parameters.AddWithValue("@bID", payload.BranchID);
        //            oAdptr1.SelectCommand.Parameters.AddWithValue("@grpoDocEntry", payload.GRPODocEntry);
        //            oAdptr1.SelectCommand.Parameters.AddWithValue("@itemCode", dtQRData.Rows[0]["ItemCode"].ToString());
        //            oAdptr1.SelectCommand.Parameters.AddWithValue("@line", dtQRData.Rows[0]["LineNum"].ToString());
        //            oAdptr1.SelectCommand.Parameters.AddWithValue("@bDocNum", dtQRData.Rows[0]["DocNum"].ToString());
        //            oAdptr1.Fill(dtGRPOItemData);
        //            QITcon.Close();
        //            #endregion

        //            if (dtGRPOItemData.Rows.Count > 0)
        //            {
        //                #region Get TransID
        //                QITcon = new SqlConnection(_QIT_connection);
        //                _Query = @" SELECT ISNULL(max(TransId),0) + 1 FROM QIT_QC_Detail A  ";
        //                _logger.LogInformation(" IncomingQCController : Get TransID Query : {q} ", _Query.ToString());

        //                SqlCommand cmd = new SqlCommand(_Query, QITcon);
        //                QITcon.Open();
        //                Object _TransId = cmd.ExecuteScalar();
        //                QITcon.Close();
        //                #endregion

        //                QITcon = new SqlConnection(_QIT_connection);
        //                _Query = @"
        //                INSERT INTO QIT_QC_Detail
        //                (TransId, BranchId, GRPODocEntry, GRPODocNum, GRPOSeries, QRCodeID, ItemCode, LineNum, GateInNo, BatchSerialNo, 
        //                 FromWhs, ToWhs, FromBinAbsEntry, ToBinAbsEntry, Status, Qty, Comment)
        //                VALUES (@transId, @bID, @grpoDocEntry, @grpoDocNum, @grpoSeries, @qr, @itemCode, @line, @gateInNo, @batchSerial, 
        //                 @fromWhs, @toWhs, @fromBin, @toBin, @status, @qty, @Comment)
        //                ";

        //                _logger.LogInformation(" IncomingQCController : QC() Query : {q} ", _Query.ToString());

        //                cmd = new SqlCommand(_Query, QITcon);
        //                cmd.Parameters.AddWithValue("@transId", _TransId);
        //                cmd.Parameters.AddWithValue("@bID", payload.BranchID);
        //                cmd.Parameters.AddWithValue("@grpoDocEntry", dtGRPOItemData.Rows[0]["DocEntry"]);
        //                cmd.Parameters.AddWithValue("@grpoDocNum", dtGRPOItemData.Rows[0]["DocNum"]);
        //                cmd.Parameters.AddWithValue("@grpoSeries", dtGRPOItemData.Rows[0]["Series"]);
        //                cmd.Parameters.AddWithValue("@qr", payload.DetailQRCodeID.Replace(" ", "~"));
        //                cmd.Parameters.AddWithValue("@itemCode", dtQRData.Rows[0]["ItemCode"]);
        //                cmd.Parameters.AddWithValue("@line", dtQRData.Rows[0]["LineNum"]);
        //                cmd.Parameters.AddWithValue("@gateInNo", dtQRData.Rows[0]["GateInNo"]);
        //                cmd.Parameters.AddWithValue("@batchSerial", dtQRData.Rows[0]["BatchSerialNo"]);
        //                cmd.Parameters.AddWithValue("@fromWhs", payload.FromWhs);
        //                cmd.Parameters.AddWithValue("@toWhs", _ToWhsCode);
        //                cmd.Parameters.AddWithValue("@fromBin", payload.FromBin);
        //                cmd.Parameters.AddWithValue("@toBin", _ToBinAbsEntry);
        //                cmd.Parameters.AddWithValue("@status", payload.Action);
        //                cmd.Parameters.AddWithValue("@qty", payload.qty);
        //                cmd.Parameters.AddWithValue("@Comment", payload.Comment);

        //                int intNum = 0;
        //                try
        //                {
        //                    QITcon.Open();
        //                    intNum = cmd.ExecuteNonQuery();
        //                    QITcon.Close();

        //                    var jsonObject = new
        //                    {
        //                        BranchID = payload.BranchID,
        //                        Series = int.Parse(dtConfig.Rows[0]["QCITSeries"].ToString()),
        //                        CardCode = dtGRPOItemData.Rows[0]["CardCode"].ToString(),
        //                        FromWhsCode = payload.FromWhs,
        //                        ToWhsCode = _ToWhsCode,
        //                        ToBinAbsEntry = _ToBinAbsEntry,
        //                        FromIT = "N",
        //                        Comments = payload.Comment,
        //                        FromBinAbsEntry = payload.FromBin,
        //                        itDetails = dtQRData.AsEnumerable().GroupBy(row => row["ItemCode"]).Select(itemGroup => new
        //                        {
        //                            itemCode = itemGroup.Key.ToString(),
        //                            TotalItemQty = payload.qty,
        //                            Project = dtGRPOItemData.Rows[0]["Project"].ToString(),
        //                            Reason = payload.Comment,
        //                            itemMngBy = itemGroup.First()["itemMngBy"].ToString(),
        //                            itQRDetails = itemGroup.Select(row => new
        //                            {
        //                                gateInNo = row["gateInNo"].ToString(),
        //                                detailQRCodeID = row["QRCodeID"].ToString(),
        //                                batchSerialNo = row["BatchSerialNo"].ToString(),
        //                                FromWhsCode = payload.FromWhs,
        //                                FromBinAbsEntry = payload.FromBin,
        //                                Qty = payload.qty
        //                            }).ToList()
        //                        }).ToList()
        //                    };

        //                    var content = new StringContent(jsonObject.ToString());
        //                    HttpClientHandler clientHandler = new HttpClientHandler();
        //                    clientHandler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => { return true; };

        //                    HttpClient client = new HttpClient(clientHandler);
        //                    client.BaseAddress = new System.Uri(_BaseUri);
        //                    client.Timeout = TimeSpan.FromMinutes(10);
        //                    HttpResponseMessage response = await client.PostAsJsonAsync("api/InventoryTransfer/InventoryTransfer", jsonObject);
        //                    var r = response.Content.ReadAsStringAsync();
        //                    ApiResponses_Inv _res = JsonConvert.DeserializeObject<ApiResponses_Inv>(r.Result);
        //                    if (_res.IsSaved == "Y")
        //                    {
        //                        return Ok(new
        //                        {
        //                            StatusCode = "200",
        //                            IsSaved = "Y",
        //                            StatusMsg = "QC done successfully !!!"
        //                        });
        //                    }
        //                    else
        //                    {
        //                        this.DeleteQCDetail(_TransId.ToString());

        //                        return BadRequest(new
        //                        {
        //                            StatusCode = "400",
        //                            IsSaved = "N",
        //                            TransSeq = _TransId,
        //                            StatusMsg = _res.StatusMsg
        //                        });
        //                    }
        //                }
        //                catch (Exception ex1)
        //                {
        //                    this.DeleteQCDetail(_TransId.ToString());
        //                    string msg;
        //                    msg = "Error message: " + ex1.Message.ToString();
        //                    _logger.LogInformation(" IncomingQCController : QC() Exception Error : {0} ", msg);
        //                    return BadRequest(new
        //                    {
        //                        StatusCode = "400",
        //                        IsSaved = "N",
        //                        TransSeq = _TransId,
        //                        StatusMsg = ex1.Message.ToString()
        //                    });

        //                }
        //            }
        //            else
        //            {
        //                return BadRequest(new { StatusCode = "400", StatusMsg = "GRPO Details not found" });
        //            }
        //        }
        //        else
        //        {
        //            return BadRequest(new { StatusCode = "400", StatusMsg = "QR Code does not exist : " + payload.DetailQRCodeID.Replace("~", " ") });
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(" Error in IncomingQCController : QC() :: {Error}", ex.ToString());
        //        return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
        //    }
        //}


        [HttpPost("QCNewFlow")]
        public async Task<IActionResult> QCNewFlow(QCPayloadNewFlow payload)
        {
            SqlConnection QITcon;
            SqlDataAdapter oAdptr;

            Object _QRTransSeqInvTrans = 0;
            object _TransSeq = 0;
            string p_ErrorMsg = string.Empty;
            try
            {
                _logger.LogInformation(" Calling IncomingQCController : QCNewFlow() ");

                System.Data.DataTable dtQRData = new System.Data.DataTable();
                System.Data.DataTable dtGRPOItemData = new System.Data.DataTable();
                DataTable dtConfig = new DataTable();
                System.Data.DataTable dtQCData = new System.Data.DataTable();
                string _ToApprovedWhsCode = string.Empty;
                string _ToRejectedWhsCode = string.Empty;
                int _ToApprovedBinAbsEntry = 0;
                int _ToRejectedBinAbsEntry = 0;

                if (((WMS_UI_API.Services.SAPConnectionService)_sapConnectionService).oComp.Connected)
                {
                    _logger.LogInformation(" Calling IncomingQCController : QCNewFlow() :: 1 :: SAP Connected ");
                 
                    #region Validation
                    if (payload.BranchID == 0)
                    {
                        return BadRequest(new
                        {
                            StatusCode = "400",
                            StatusMsg = "Provide Branch"
                        });
                    }

                    if (payload.GRPODocEntry == 0)
                    {
                        return BadRequest(new
                        {
                            StatusCode = "400",
                            StatusMsg = "Provide GRPO Document Entry"
                        });
                    }

                    if (payload.DetailQRCodeID == String.Empty || payload.DetailQRCodeID.ToLower() == "string")
                    {
                        return BadRequest(new
                        {
                            StatusCode = "400",
                            StatusMsg = "Provide Detail QR"
                        });
                    }

                    if (payload.Comment == String.Empty || payload.Comment.ToLower() == "string")
                    {
                        return BadRequest(new
                        {
                            StatusCode = "400",
                            StatusMsg = "Provide Comment"
                        });
                    }

                    #endregion

                    #region Check already QC is done or not
                    QITcon = new SqlConnection(_QIT_connection);

                    _Query = @" SELECT * FROM 
                            (
                                SELECT 
                                (
                                    SELECT ISNULL(SUM(A.Qty),0) from QIT_QR_detail A
                                    WHERE A.QRCodeID = @dQR AND ISNULL(A.BranchID, @bID) = @bID
                                ) -
                                (
                                    SELECT ISNULL(SUM(A.Qty),0) QCQty FROM QIT_QC_Detail A   
                                    WHERE A.QRCodeID = @dQR AND ISNULL(A.BranchID, @bID) = @bID -- AND A.DocEntry <> 0
                                ) as PendQty
                            ) as A
                            where A.PendQty = 0  ";
                    _logger.LogInformation(" IncomingQCController : QC already done Query : {q} ", _Query.ToString());
                    QITcon.Open();
                    oAdptr = new SqlDataAdapter(_Query, QITcon);
                    oAdptr.SelectCommand.Parameters.AddWithValue("@bID", payload.BranchID);
                    oAdptr.SelectCommand.Parameters.AddWithValue("@dQR", payload.DetailQRCodeID.Replace(" ", "~"));
                    oAdptr.Fill(dtQCData);
                    QITcon.Close();

                    if (dtQCData.Rows.Count > 0)
                    {
                        return BadRequest(new
                        {
                            StatusCode = "400",
                            StatusMsg = "QC is already done for : " + payload.DetailQRCodeID.Replace("~", " ")
                        });
                    }

                    #endregion

                    #region Get Item data from DetailQRCodeID
                    QITcon = new SqlConnection(_QIT_connection);

                    _Query = @" SELECT A.*, B.ItemMngBy, C.DocNum  
                            FROM QIT_QR_Detail A  
                            INNER JOIN QIT_Item_Master B on A.ItemCode = B.ItemCode
                            INNER JOIN QIT_QR_Header C ON C.HeaderSrNo = A.HeaderSrNo
                            WHERE A.QRCodeID = @dQR AND ISNULL(A.BranchID, @bID) = @bID  ";
                    _logger.LogInformation(" IncomingQCController : QRCode Query : {q} ", _Query.ToString());
                    QITcon.Open();
                    oAdptr = new SqlDataAdapter(_Query, QITcon);
                    oAdptr.SelectCommand.Parameters.AddWithValue("@bID", payload.BranchID);
                    oAdptr.SelectCommand.Parameters.AddWithValue("@dQR", payload.DetailQRCodeID.Replace(" ", "~"));
                    oAdptr.Fill(dtQRData);
                    QITcon.Close();
                    #endregion

                    #region Get Approve and Reject Warehouse
                    QITcon = new SqlConnection(_QIT_connection);

                    _Query = @" SELECT * FROM QIT_Config_Master A WHERE ISNULL(A.BranchID, @bID) = @bID  ";
                    _logger.LogInformation(" IncomingQCController : Configuration  Query : {q} ", _Query.ToString());
                    QITcon.Open();
                    oAdptr = new SqlDataAdapter(_Query, QITcon);
                    oAdptr.SelectCommand.Parameters.AddWithValue("@bID", payload.BranchID);
                    oAdptr.Fill(dtConfig);
                    QITcon.Close();
                    #endregion

                    #region Check for Valid Whs
                    if (dtConfig.Rows[0]["ApprovedWhs"].ToString().Trim().Length <= 0)
                        return BadRequest(new
                        {
                            StatusCode = "400",
                            StatusMsg = "Define Approve Warehouse in Configuration"
                        });

                    if (dtConfig.Rows[0]["RejectedWhs"].ToString().Trim().Length <= 0)
                        return BadRequest(new
                        {
                            StatusCode = "400",
                            StatusMsg = "Define Reject Warehouse in Configuration"
                        });

                    _ToApprovedWhsCode = dtConfig.Rows[0]["ApprovedWhs"].ToString().Trim();
                    _ToRejectedWhsCode = dtConfig.Rows[0]["RejectedWhs"].ToString().Trim();
                    _ToApprovedBinAbsEntry = int.Parse(dtConfig.Rows[0]["ApproveBin"].ToString().Trim());
                    _ToRejectedBinAbsEntry = int.Parse(dtConfig.Rows[0]["RejectBin"].ToString().Trim());
                    #endregion

                    #region Check for QC IT Series
                    if (dtConfig.Rows[0]["QCITSeries"].ToString().Trim().Length <= 0)
                        return BadRequest(new
                        {
                            StatusCode = "400",
                            StatusMsg = "Define QC Inventory Transfer Series in Configuration"
                        });
                    #endregion

                    if (dtQRData.Rows.Count > 0)
                    {
                        #region Get GRPO Detail

                        _Query = @" 
                    SELECT A.DocEntry, A.DocNum, A.CardCode, A.CardName, A.Series, A.DocDate, A.TaxDate, 
                           B.ItemCode, B.Dscription ItemName, B.Project, B.Quantity
                    FROM " + Global.SAP_DB + @".dbo.OPDN A INNER JOIN " + Global.SAP_DB + @".dbo.PDN1 B ON A.DocEntry = B.DocEntry 
                    WHERE A.DocEntry = @grpoDocEntry and B.ItemCode = @itemCode and B.BaseLine = @line and B.BaseDocNum = @bDocNum and 
                          A.CANCELED = 'N' and ISNULL(A.BPLId, @bID) = @bID ";
                        _logger.LogInformation(" IncomingQCController : GRPO Data Query : {q} ", _Query.ToString());
                        QITcon.Open();
                        oAdptr = new SqlDataAdapter(_Query, QITcon);
                        oAdptr.SelectCommand.Parameters.AddWithValue("@bID", payload.BranchID);
                        oAdptr.SelectCommand.Parameters.AddWithValue("@grpoDocEntry", payload.GRPODocEntry);
                        oAdptr.SelectCommand.Parameters.AddWithValue("@itemCode", dtQRData.Rows[0]["ItemCode"].ToString());
                        oAdptr.SelectCommand.Parameters.AddWithValue("@line", dtQRData.Rows[0]["LineNum"].ToString());
                        oAdptr.SelectCommand.Parameters.AddWithValue("@bDocNum", dtQRData.Rows[0]["DocNum"].ToString());
                        oAdptr.Fill(dtGRPOItemData);
                        QITcon.Close();
                        #endregion

                        if (dtGRPOItemData.Rows.Count > 0)
                        {
                            #region Get TransSeq
                            QITcon = new SqlConnection(_QIT_connection);
                            _Query = @" SELECT ISNULL(max(TransSeq),0) + 1 FROM QIT_QC_Detail A  ";
                            _logger.LogInformation(" IncomingQCController : Get TransSeq Query : {q} ", _Query.ToString());

                            cmd = new SqlCommand(_Query, QITcon);
                            QITcon.Open();
                            _TransSeq = cmd.ExecuteScalar();
                            QITcon.Close();
                            #endregion

                            QITcon = new SqlConnection(_QIT_connection);

                            #region QIT QC Entry - Approve
                            if (payload.ApprovedQty > 0)
                            {
                                // _TransSeq = _TransSeq + 1;
                                _Query = @"
                            INSERT INTO QIT_QC_Detail
                            (   TransId, TransSeq, BranchId, GRPODocEntry, GRPODocNum, GRPOSeries, QRCodeID, ItemCode, LineNum, GateInNo,   
                                BatchSerialNo, FromWhs, ToWhs, FromBinAbsEntry, ToBinAbsEntry, Status, Qty, Comment
                            )
                            VALUES 
                            (   (SELECT ISNULL(max(TransId),0) + 1 FROM QIT_QC_Detail A), @transSeq, @bID, @grpoDocEntry, @grpoDocNum, 
                                @grpoSeries, @qr, @itemCode, @line, @gateInNo, 
                                @batchSerial, @fromWhs, @toWhs, @fromBin, @toBin, @status, @qty, @Comment
                            )
                            ";

                                _logger.LogInformation(" IncomingQCController : QC() Query : {q} ", _Query.ToString());
                                cmd = new SqlCommand(_Query, QITcon);
                                //cmd.Parameters.AddWithValue("@transId", _TransId);
                                cmd.Parameters.AddWithValue("@transSeq", _TransSeq);
                                cmd.Parameters.AddWithValue("@bID", payload.BranchID);
                                cmd.Parameters.AddWithValue("@grpoDocEntry", dtGRPOItemData.Rows[0]["DocEntry"]);
                                cmd.Parameters.AddWithValue("@grpoDocNum", dtGRPOItemData.Rows[0]["DocNum"]);
                                cmd.Parameters.AddWithValue("@grpoSeries", dtGRPOItemData.Rows[0]["Series"]);
                                cmd.Parameters.AddWithValue("@qr", payload.DetailQRCodeID.Replace(" ", "~"));
                                cmd.Parameters.AddWithValue("@itemCode", dtQRData.Rows[0]["ItemCode"]);
                                cmd.Parameters.AddWithValue("@line", dtQRData.Rows[0]["LineNum"]);
                                cmd.Parameters.AddWithValue("@gateInNo", dtQRData.Rows[0]["GateInNo"]);
                                cmd.Parameters.AddWithValue("@batchSerial", dtQRData.Rows[0]["BatchSerialNo"]);
                                cmd.Parameters.AddWithValue("@fromWhs", payload.FromWhs);
                                cmd.Parameters.AddWithValue("@fromBin", payload.FromBin);
                                cmd.Parameters.AddWithValue("@toWhs", _ToApprovedWhsCode);
                                cmd.Parameters.AddWithValue("@toBin", _ToApprovedBinAbsEntry);
                                cmd.Parameters.AddWithValue("@status", "A");
                                cmd.Parameters.AddWithValue("@qty", payload.ApprovedQty);
                                cmd.Parameters.AddWithValue("@Comment", payload.Comment);

                                int intNum = 0;
                                try
                                {
                                    QITcon.Open();
                                    intNum = cmd.ExecuteNonQuery();
                                    QITcon.Close();
                                }
                                catch (Exception exApprove)
                                {
                                    DeleteQCDetail(_TransSeq.ToString());
                                    string msg;
                                    msg = "Error message: " + exApprove.Message.ToString();
                                    _logger.LogInformation(" IncomingQCController : QC() Exception Error : {0} ", msg);
                                    return BadRequest(new
                                    {
                                        StatusCode = "400",
                                        IsSaved = "N",
                                        TransSeq = _TransSeq,
                                        StatusMsg = exApprove.Message.ToString()
                                    });

                                }
                            }
                            #endregion

                            #region QIT QC Entry - Reject
                            if (payload.RejectedQty > 0)
                            {
                                // _TransSeq = _TransSeq + 1;
                                _Query = @"
                            INSERT INTO QIT_QC_Detail
                            (   TransId, TransSeq, BranchId, GRPODocEntry, GRPODocNum, GRPOSeries, QRCodeID, ItemCode, LineNum, GateInNo,   
                                BatchSerialNo, FromWhs, ToWhs, FromBinAbsEntry, ToBinAbsEntry, Status, Qty, Comment
                            )
                            VALUES 
                            (   (SELECT ISNULL(max(TransId),0) + 1 FROM QIT_QC_Detail A), @transSeq, @bID, @grpoDocEntry, @grpoDocNum, 
                                @grpoSeries, @qr, @itemCode, @line, @gateInNo, 
                                @batchSerial, @fromWhs, @toWhs, @fromBin, @toBin, @status, @qty, @Comment
                            )
                            ";

                                _logger.LogInformation(" IncomingQCController : QC() Query : {q} ", _Query.ToString());
                                cmd = new SqlCommand(_Query, QITcon);
                                //cmd.Parameters.AddWithValue("@transId", _TransId);
                                cmd.Parameters.AddWithValue("@transSeq", _TransSeq);
                                cmd.Parameters.AddWithValue("@bID", payload.BranchID);
                                cmd.Parameters.AddWithValue("@grpoDocEntry", dtGRPOItemData.Rows[0]["DocEntry"]);
                                cmd.Parameters.AddWithValue("@grpoDocNum", dtGRPOItemData.Rows[0]["DocNum"]);
                                cmd.Parameters.AddWithValue("@grpoSeries", dtGRPOItemData.Rows[0]["Series"]);
                                cmd.Parameters.AddWithValue("@qr", payload.DetailQRCodeID.Replace(" ", "~"));
                                cmd.Parameters.AddWithValue("@itemCode", dtQRData.Rows[0]["ItemCode"]);
                                cmd.Parameters.AddWithValue("@line", dtQRData.Rows[0]["LineNum"]);
                                cmd.Parameters.AddWithValue("@gateInNo", dtQRData.Rows[0]["GateInNo"]);
                                cmd.Parameters.AddWithValue("@batchSerial", dtQRData.Rows[0]["BatchSerialNo"]);
                                cmd.Parameters.AddWithValue("@fromWhs", payload.FromWhs);
                                cmd.Parameters.AddWithValue("@fromBin", payload.FromBin);
                                cmd.Parameters.AddWithValue("@toWhs", _ToRejectedWhsCode);
                                cmd.Parameters.AddWithValue("@toBin", _ToRejectedBinAbsEntry);
                                cmd.Parameters.AddWithValue("@status", "R");
                                cmd.Parameters.AddWithValue("@qty", payload.RejectedQty);
                                cmd.Parameters.AddWithValue("@Comment", payload.Comment);

                                int intNum = 0;
                                try
                                {
                                    QITcon.Open();
                                    intNum = cmd.ExecuteNonQuery();
                                    QITcon.Close();
                                }
                                catch (Exception exReject)
                                {
                                    DeleteQCDetail(_TransSeq.ToString());
                                    string msg;
                                    msg = "Error message: " + exReject.Message.ToString();
                                    _logger.LogInformation(" IncomingQCController : QC() Exception Error : {0} ", msg);
                                    return BadRequest(new
                                    {
                                        StatusCode = "400",
                                        IsSaved = "N",
                                        TransSeq = _TransSeq,
                                        StatusMsg = exReject.Message.ToString()
                                    });

                                }
                            }
                            #endregion

                            #region Inventory Transfer
                            //if (objGlobal.ConnectSAP(out p_ErrorMsg))

                            if (1 == 1)
                            {
                                DateTime _docDate = DateTime.Today;
                                int _TotalItemCount = 1;
                                int _SuccessCount = 0;
                                int _FailCount = 0;

                                #region Get QR TransSeq No - Inventory Transfer
                                QITcon = new SqlConnection(_QIT_connection);
                                _Query = @" SELECT ISNULL(max(QRTransSeq),0) + 1 FROM QIT_QRStock_InvTrans A  ";
                                _logger.LogInformation(" IncomingQCController : GetQRTransSeqNo(Inv Trans) Query : {q} ", _Query.ToString());

                                cmd = new SqlCommand(_Query, QITcon);
                                QITcon.Open();
                                _QRTransSeqInvTrans = cmd.ExecuteScalar();
                                QITcon.Close();
                                #endregion

                                #region Insert in QR Stock Table

                                if (payload.ApprovedQty > 0)
                                {
                                    QITcon = new SqlConnection(_QIT_connection);
                                    _Query = @"INSERT INTO QIT_QRStock_InvTrans (BranchID, TransId, TransSeq, QRTransSeq, QRCodeID, GateInNo, ItemCode,BatchSerialNo, FromObjType, ToObjType, FromWhs, ToWhs, Qty, FromBinAbsEntry, ToBinAbsEntry) 
                         VALUES ( @bID,  (SELECT ISNULL(max(TransId),0) + 1 FROM QIT_QRStock_InvTrans), 
                                   @transSeq, @qrtransSeq, @qrCodeID, @gateInNo, @itemCode, @bsNo, @frObjType, @toObjType, @fromWhs, @toWhs, @qty, @fromBin, @toBin   
                         )";
                                    _logger.LogInformation("IncomingQCController : QR Stock Table Query : {q} ", _Query.ToString());

                                    cmd = new SqlCommand(_Query, QITcon);
                                    cmd.Parameters.AddWithValue("@bID", payload.BranchID);
                                    cmd.Parameters.AddWithValue("@qrtransSeq", _QRTransSeqInvTrans);
                                    cmd.Parameters.AddWithValue("@transSeq", 0);
                                    cmd.Parameters.AddWithValue("@qrCodeID", payload.DetailQRCodeID.Replace(" ", "~"));
                                    cmd.Parameters.AddWithValue("@gateInNo", dtQRData.Rows[0]["GateInNo"].ToString());
                                    cmd.Parameters.AddWithValue("@itemCode", dtQRData.Rows[0]["ItemCode"].ToString());
                                    cmd.Parameters.AddWithValue("@bsNo", dtQRData.Rows[0]["BatchSerialNo"].ToString());
                                    cmd.Parameters.AddWithValue("@frObjType", "67");
                                    cmd.Parameters.AddWithValue("@toObjType", "67");
                                    cmd.Parameters.AddWithValue("@fromWhs", payload.FromWhs);
                                    cmd.Parameters.AddWithValue("@toWhs", _ToApprovedWhsCode);
                                    cmd.Parameters.AddWithValue("@fromBin", payload.FromBin);
                                    cmd.Parameters.AddWithValue("@toBin", _ToApprovedBinAbsEntry);
                                    cmd.Parameters.AddWithValue("@qty", payload.ApprovedQty);

                                    int intNum = 0;
                                    try
                                    {
                                        QITcon.Open();
                                        intNum = cmd.ExecuteNonQuery();
                                        QITcon.Close();
                                    }
                                    catch (Exception exInvApprove)
                                    {
                                        DeleteQCDetail(_TransSeq.ToString());
                                        DeleteQRStockITDet(_QRTransSeqInvTrans.ToString());
                                        string msg;
                                        msg = "Error message: " + exInvApprove.Message.ToString();
                                        _logger.LogInformation(" IncomingQCController : QC INV() Exception Error : {0} ", msg);
                                        return BadRequest(new
                                        {
                                            StatusCode = "400",
                                            IsSaved = "N",
                                            TransSeq = _TransSeq,
                                            StatusMsg = exInvApprove.Message.ToString()
                                        });

                                    }
                                }

                                if (payload.RejectedQty > 0)
                                {
                                    QITcon = new SqlConnection(_QIT_connection);
                                    _Query = @"
                                INSERT INTO QIT_QRStock_InvTrans 
                                (
                                    BranchID, TransId, TransSeq, QRTransSeq, QRCodeID, GateInNo, ItemCode,BatchSerialNo, FromObjType, ToObjType,
                                    FromWhs, ToWhs, Qty, FromBinAbsEntry, ToBinAbsEntry
                                ) 
                                VALUES 
                                ( @bID,  (SELECT ISNULL(max(TransId),0) + 1 FROM QIT_QRStock_InvTrans), 
                                   @transSeq, @qrtransSeq, @qrCodeID, @gateInNo, @itemCode, @bsNo, @frObjType, @toObjType, @fromWhs, @toWhs, @qty, @fromBin, @toBin   
                         )";
                                    _logger.LogInformation("IncomingQCController : QR Stock Table Query : {q} ", _Query.ToString());

                                    cmd = new SqlCommand(_Query, QITcon);
                                    cmd.Parameters.AddWithValue("@bID", payload.BranchID);
                                    cmd.Parameters.AddWithValue("@qrtransSeq", _QRTransSeqInvTrans);
                                    cmd.Parameters.AddWithValue("@transSeq", 0);
                                    cmd.Parameters.AddWithValue("@qrCodeID", payload.DetailQRCodeID.Replace(" ", "~"));
                                    cmd.Parameters.AddWithValue("@gateInNo", dtQRData.Rows[0]["GateInNo"].ToString());
                                    cmd.Parameters.AddWithValue("@itemCode", dtQRData.Rows[0]["ItemCode"].ToString());
                                    cmd.Parameters.AddWithValue("@bsNo", dtQRData.Rows[0]["BatchSerialNo"].ToString());
                                    cmd.Parameters.AddWithValue("@frObjType", "67");
                                    cmd.Parameters.AddWithValue("@toObjType", "67");
                                    cmd.Parameters.AddWithValue("@fromWhs", payload.FromWhs);
                                    cmd.Parameters.AddWithValue("@toWhs", _ToRejectedWhsCode);
                                    cmd.Parameters.AddWithValue("@fromBin", payload.FromBin);
                                    cmd.Parameters.AddWithValue("@toBin", _ToRejectedBinAbsEntry);
                                    cmd.Parameters.AddWithValue("@qty", payload.RejectedQty);

                                    int intNum = 0;
                                    try
                                    {
                                        QITcon.Open();
                                        intNum = cmd.ExecuteNonQuery();
                                        QITcon.Close();
                                    }
                                    catch (Exception exInvReject)
                                    {
                                        DeleteQCDetail(_TransSeq.ToString());
                                        DeleteQRStockITDet(_QRTransSeqInvTrans.ToString());
                                        string msg;
                                        msg = "Error message: " + exInvReject.Message.ToString();
                                        _logger.LogInformation(" IncomingQCController : QC INV() Exception Error : {0} ", msg);
                                        return BadRequest(new
                                        {
                                            StatusCode = "400",
                                            IsSaved = "N",
                                            TransSeq = _TransSeq,
                                            StatusMsg = exInvReject.Message.ToString()
                                        });

                                    }
                                }

                                #endregion

                                #region IT with APPROVE and REJECT

                                int _Line = 0;
                                StockTransfer oStockTransfer = (StockTransfer)((WMS_UI_API.Services.SAPConnectionService)_sapConnectionService).oComp.GetBusinessObject(BoObjectTypes.oStockTransfer);
                                oStockTransfer.DocObjectCode = BoObjectTypes.oStockTransfer;
                                oStockTransfer.Series = int.Parse(dtConfig.Rows[0]["QCITSeries"].ToString());
                                oStockTransfer.CardCode = dtGRPOItemData.Rows[0]["CardCode"].ToString();
                                oStockTransfer.Comments = payload.Comment;
                                oStockTransfer.DocDate = _docDate;
                                oStockTransfer.FromWarehouse = payload.FromWhs;
                                oStockTransfer.UserFields.Fields.Item("U_QIT_FromWeb").Value = "Y";

                                if (payload.ApprovedQty > 0)
                                {
                                    oStockTransfer.Lines.ItemCode = dtQRData.Rows[0]["ItemCode"].ToString();
                                    oStockTransfer.Lines.Quantity = payload.ApprovedQty;
                                    oStockTransfer.Lines.ProjectCode = dtGRPOItemData.Rows[0]["Project"].ToString();
                                    oStockTransfer.Lines.UserFields.Fields.Item("U_reason").Value = payload.Comment;

                                    if (dtQRData.Rows[0]["ItemMngBy"].ToString().ToLower() == "s")
                                    {
                                        int i = 0;
                                        oStockTransfer.Lines.WarehouseCode = _ToApprovedWhsCode;
                                        if (!string.IsNullOrEmpty(dtQRData.Rows[0]["BatchSerialNo"].ToString()))
                                        {
                                            oStockTransfer.Lines.FromWarehouseCode = payload.FromWhs;
                                            oStockTransfer.Lines.SerialNumbers.SetCurrentLine(i);
                                            oStockTransfer.Lines.BatchNumbers.BaseLineNumber = _Line;
                                            oStockTransfer.Lines.SerialNumbers.InternalSerialNumber = dtQRData.Rows[0]["BatchSerialNo"].ToString();
                                            oStockTransfer.Lines.SerialNumbers.ManufacturerSerialNumber = dtQRData.Rows[0]["BatchSerialNo"].ToString();
                                            oStockTransfer.Lines.SerialNumbers.Quantity = Convert.ToDouble(payload.ApprovedQty);
                                            oStockTransfer.Lines.SerialNumbers.Add();

                                            if (payload.FromBin > 0) // Enter in this code block only when From Whs has Bin Allocation
                                            {
                                                oStockTransfer.Lines.BinAllocations.BinActionType = BinActionTypeEnum.batFromWarehouse;
                                                oStockTransfer.Lines.BinAllocations.BinAbsEntry = payload.FromBin;
                                                oStockTransfer.Lines.BinAllocations.Quantity = Convert.ToDouble(payload.ApprovedQty);
                                                oStockTransfer.Lines.BinAllocations.BaseLineNumber = _Line;
                                                oStockTransfer.Lines.BinAllocations.SerialAndBatchNumbersBaseLine = i;
                                                oStockTransfer.Lines.BinAllocations.Add();
                                            }
                                            if (_ToApprovedBinAbsEntry > 0) // Enter in this code block only when To Whs has Bin Allocation
                                            {
                                                oStockTransfer.Lines.BinAllocations.BinActionType = BinActionTypeEnum.batToWarehouse;
                                                oStockTransfer.Lines.BinAllocations.BinAbsEntry = _ToApprovedBinAbsEntry;
                                                oStockTransfer.Lines.BinAllocations.Quantity = Convert.ToDouble(payload.ApprovedQty);
                                                oStockTransfer.Lines.BinAllocations.BaseLineNumber = _Line;
                                                oStockTransfer.Lines.BinAllocations.SerialAndBatchNumbersBaseLine = i;
                                                oStockTransfer.Lines.BinAllocations.Add();
                                            }
                                            i = i + 1;
                                        }
                                    }
                                    else if (dtQRData.Rows[0]["ItemMngBy"].ToString().ToLower() == "b")
                                    {
                                        int _batchLine = 0;

                                        oStockTransfer.Lines.WarehouseCode = _ToApprovedWhsCode;
                                        if (!string.IsNullOrEmpty(dtQRData.Rows[0]["BatchSerialNo"].ToString()))
                                        {
                                            oStockTransfer.Lines.FromWarehouseCode = payload.FromWhs;
                                            oStockTransfer.Lines.BatchNumbers.BaseLineNumber = _Line;
                                            oStockTransfer.Lines.BatchNumbers.BatchNumber = dtQRData.Rows[0]["BatchSerialNo"].ToString();
                                            oStockTransfer.Lines.BatchNumbers.Quantity = Convert.ToDouble(payload.ApprovedQty);
                                            oStockTransfer.Lines.BatchNumbers.Add();

                                            if (payload.FromBin > 0) // Enter in this code block only when From Whs has Bin Allocation
                                            {
                                                oStockTransfer.Lines.BinAllocations.BinActionType = BinActionTypeEnum.batFromWarehouse;
                                                oStockTransfer.Lines.BinAllocations.BinAbsEntry = payload.FromBin;
                                                oStockTransfer.Lines.BinAllocations.Quantity = Convert.ToDouble(payload.ApprovedQty);
                                                oStockTransfer.Lines.BinAllocations.BaseLineNumber = _Line;
                                                oStockTransfer.Lines.BinAllocations.SerialAndBatchNumbersBaseLine = _batchLine;
                                                oStockTransfer.Lines.BinAllocations.Add();
                                            }
                                            if (_ToApprovedBinAbsEntry > 0) // Enter in this code block only when To Whs has Bin Allocation
                                            {
                                                oStockTransfer.Lines.BinAllocations.BinActionType = BinActionTypeEnum.batToWarehouse;
                                                oStockTransfer.Lines.BinAllocations.BinAbsEntry = _ToApprovedBinAbsEntry;
                                                oStockTransfer.Lines.BinAllocations.Quantity = Convert.ToDouble(payload.ApprovedQty);
                                                oStockTransfer.Lines.BinAllocations.BaseLineNumber = _Line;
                                                oStockTransfer.Lines.BinAllocations.SerialAndBatchNumbersBaseLine = _batchLine;
                                                oStockTransfer.Lines.BinAllocations.Add();
                                            }
                                            _batchLine = _batchLine + 1;
                                        }
                                    }
                                    oStockTransfer.Lines.Add();
                                    _Line = _Line + 1;

                                }

                                if (payload.RejectedQty > 0)
                                {
                                    oStockTransfer.Lines.ItemCode = dtQRData.Rows[0]["ItemCode"].ToString();
                                    oStockTransfer.Lines.Quantity = payload.RejectedQty;
                                    oStockTransfer.Lines.ProjectCode = dtGRPOItemData.Rows[0]["Project"].ToString();
                                    oStockTransfer.Lines.UserFields.Fields.Item("U_reason").Value = payload.Comment;

                                    if (dtQRData.Rows[0]["ItemMngBy"].ToString().ToLower() == "s")
                                    {
                                        int i = 0;
                                        oStockTransfer.Lines.WarehouseCode = _ToRejectedWhsCode;
                                        if (!string.IsNullOrEmpty(dtQRData.Rows[0]["BatchSerialNo"].ToString()))
                                        {
                                            oStockTransfer.Lines.FromWarehouseCode = payload.FromWhs;
                                            oStockTransfer.Lines.SerialNumbers.SetCurrentLine(i);
                                            oStockTransfer.Lines.BatchNumbers.BaseLineNumber = _Line;
                                            oStockTransfer.Lines.SerialNumbers.InternalSerialNumber = dtQRData.Rows[0]["BatchSerialNo"].ToString();
                                            oStockTransfer.Lines.SerialNumbers.ManufacturerSerialNumber = dtQRData.Rows[0]["BatchSerialNo"].ToString();
                                            oStockTransfer.Lines.SerialNumbers.Quantity = Convert.ToDouble(payload.RejectedQty);
                                            oStockTransfer.Lines.SerialNumbers.Add();

                                            if (payload.FromBin > 0) // Enter in this code block only when From Whs has Bin Allocation
                                            {
                                                oStockTransfer.Lines.BinAllocations.BinActionType = BinActionTypeEnum.batFromWarehouse;
                                                oStockTransfer.Lines.BinAllocations.BinAbsEntry = payload.FromBin;
                                                oStockTransfer.Lines.BinAllocations.Quantity = Convert.ToDouble(payload.RejectedQty);
                                                oStockTransfer.Lines.BinAllocations.BaseLineNumber = _Line;
                                                oStockTransfer.Lines.BinAllocations.SerialAndBatchNumbersBaseLine = i;
                                                oStockTransfer.Lines.BinAllocations.Add();
                                            }
                                            if (_ToRejectedBinAbsEntry > 0) // Enter in this code block only when To Whs has Bin Allocation
                                            {
                                                oStockTransfer.Lines.BinAllocations.BinActionType = BinActionTypeEnum.batToWarehouse;
                                                oStockTransfer.Lines.BinAllocations.BinAbsEntry = _ToRejectedBinAbsEntry;
                                                oStockTransfer.Lines.BinAllocations.Quantity = Convert.ToDouble(payload.RejectedQty);
                                                oStockTransfer.Lines.BinAllocations.BaseLineNumber = _Line;
                                                oStockTransfer.Lines.BinAllocations.SerialAndBatchNumbersBaseLine = i;
                                                oStockTransfer.Lines.BinAllocations.Add();
                                            }
                                            i = i + 1;
                                        }
                                    }
                                    else if (dtQRData.Rows[0]["ItemMngBy"].ToString().ToLower() == "b")
                                    {
                                        int _batchLine = 0;

                                        oStockTransfer.Lines.WarehouseCode = _ToRejectedWhsCode;
                                        if (!string.IsNullOrEmpty(dtQRData.Rows[0]["BatchSerialNo"].ToString()))
                                        {
                                            oStockTransfer.Lines.FromWarehouseCode = payload.FromWhs;
                                            oStockTransfer.Lines.BatchNumbers.BaseLineNumber = _Line;
                                            oStockTransfer.Lines.BatchNumbers.BatchNumber = dtQRData.Rows[0]["BatchSerialNo"].ToString();
                                            oStockTransfer.Lines.BatchNumbers.Quantity = Convert.ToDouble(payload.RejectedQty);
                                            oStockTransfer.Lines.BatchNumbers.Add();

                                            if (payload.FromBin > 0) // Enter in this code block only when From Whs has Bin Allocation
                                            {
                                                oStockTransfer.Lines.BinAllocations.BinActionType = BinActionTypeEnum.batFromWarehouse;
                                                oStockTransfer.Lines.BinAllocations.BinAbsEntry = payload.FromBin;
                                                oStockTransfer.Lines.BinAllocations.Quantity = Convert.ToDouble(payload.RejectedQty);
                                                oStockTransfer.Lines.BinAllocations.BaseLineNumber = _Line;
                                                oStockTransfer.Lines.BinAllocations.SerialAndBatchNumbersBaseLine = _batchLine;
                                                oStockTransfer.Lines.BinAllocations.Add();
                                            }
                                            if (_ToRejectedBinAbsEntry > 0) // Enter in this code block only when To Whs has Bin Allocation
                                            {
                                                oStockTransfer.Lines.BinAllocations.BinActionType = BinActionTypeEnum.batToWarehouse;
                                                oStockTransfer.Lines.BinAllocations.BinAbsEntry = _ToRejectedBinAbsEntry;
                                                oStockTransfer.Lines.BinAllocations.Quantity = Convert.ToDouble(payload.RejectedQty);
                                                oStockTransfer.Lines.BinAllocations.BaseLineNumber = _Line;
                                                oStockTransfer.Lines.BinAllocations.SerialAndBatchNumbersBaseLine = _batchLine;
                                                oStockTransfer.Lines.BinAllocations.Add();
                                            }
                                            _batchLine = _batchLine + 1;
                                        }
                                    }
                                    oStockTransfer.Lines.Add();
                                    _Line = _Line + 1;

                                }

                                int addResult = oStockTransfer.Add();

                                if (addResult != 0)
                                {
                                    DeleteQCDetail(_TransSeq.ToString());
                                    DeleteQRStockITDet(_QRTransSeqInvTrans.ToString());
                                    return BadRequest(new
                                    {
                                        StatusCode = "400",
                                        IsSaved = "N",
                                        TransSeq = _QRTransSeqInvTrans,
                                        StatusMsg = "Error code: " + addResult + Environment.NewLine +
                                                    "Error message: " + ((WMS_UI_API.Services.SAPConnectionService)_sapConnectionService).oComp.GetLastErrorDescription()
                                    });
                                }
                                else
                                {
                                    int docEntry = int.Parse(((WMS_UI_API.Services.SAPConnectionService)_sapConnectionService).oComp.GetNewObjectKey());
                                    QITcon = new SqlConnection(_QIT_connection);

                                    #region Get IT Data
                                    System.Data.DataTable dtIT = new System.Data.DataTable();
                                    _Query = @"  SELECT * FROM " + Global.SAP_DB + @".dbo.OWTR WHERE DocEntry = @docEntry  ";
                                    _logger.LogInformation(" IncomingQCController : Get IT Data : Query : {q} ", _Query.ToString());
                                    QITcon.Open();
                                    oAdptr = new SqlDataAdapter(_Query, QITcon);
                                    oAdptr.SelectCommand.Parameters.AddWithValue("@docEntry", docEntry);
                                    oAdptr.Fill(dtIT);
                                    QITcon.Close();
                                    #endregion

                                    #region Update Transaction Table

                                    _Query = @" 
                                UPDATE QIT_QC_Detail 
                                SET DocEntry = @docEntry, DocNum = @docNum 
                                where TransSeq = @code";
                                    _logger.LogInformation(" IncomingQCController : Update Transaction Table Query : {q} ", _Query.ToString());

                                    cmd = new SqlCommand(_Query, QITcon);
                                    cmd.Parameters.AddWithValue("@docEntry", docEntry);
                                    cmd.Parameters.AddWithValue("@docNum", dtIT.Rows[0]["DocNum"]);
                                    cmd.Parameters.AddWithValue("@code", _TransSeq);

                                    QITcon.Open();
                                    int intNum = cmd.ExecuteNonQuery();
                                    QITcon.Close();

                                    if (intNum > 0)
                                    {
                                        return Ok(new { StatusCode = "200", IsSaved = "Y", StatusMsg = "Saved Successfully" });
                                    }
                                    else
                                    {
                                        //DeleteQCDetail(_TransSeq.ToString());
                                        //DeleteQRStockITDet(_QRTransSeqInvTrans.ToString());
                                        return BadRequest(new
                                        {
                                            StatusCode = "400",
                                            IsSaved = "N",
                                            TransSeq = _TransSeq,
                                            DocEntry = docEntry,
                                            StatusMsg = "QC failed"
                                        });
                                    }
                                    #endregion
                                }


                                #endregion
                            }
                            else
                            {
                                DeleteQCDetail(_TransSeq.ToString());
                                DeleteQRStockITDet(_QRTransSeqInvTrans.ToString());
                                return BadRequest(new
                                {
                                    StatusCode = "400",
                                    IsSaved = "N",
                                    TransSeq = _TransSeq,
                                    StatusMsg = "Error code: " + ((WMS_UI_API.Services.SAPConnectionService)_sapConnectionService).oComp.GetLastErrorCode() + Environment.NewLine +
                                                "Error message: " + ((WMS_UI_API.Services.SAPConnectionService)_sapConnectionService).oComp.GetLastErrorDescription()
                                });
                            }


                            #endregion

                        }
                        else
                        {
                            DeleteQCDetail(_TransSeq.ToString());
                            DeleteQRStockITDet(_QRTransSeqInvTrans.ToString());
                            return BadRequest(new { StatusCode = "400", StatusMsg = "GRPO Details not found" });
                        }
                    }
                    else
                    {
                        DeleteQCDetail(_TransSeq.ToString());
                        DeleteQRStockITDet(_QRTransSeqInvTrans.ToString());
                        return BadRequest(new { StatusCode = "400", StatusMsg = "QR Code does not exist : " + payload.DetailQRCodeID.Replace("~", " ") });
                    }
                }
                else
                {
                    _logger.LogInformation(" Calling IncomingQCController : QCNewFlow() :: 2 :: SAP Failed ");
                    return BadRequest(new
                    {
                        StatusCode = "400",
                        IsSaved = "N",
                        TransSeq = _TransSeq,
                        StatusMsg = "Error code: " + ((WMS_UI_API.Services.SAPConnectionService)_sapConnectionService).oComp.GetLastErrorCode() + Environment.NewLine +
                                       "Error message: " + ((WMS_UI_API.Services.SAPConnectionService)_sapConnectionService).oComp.GetLastErrorDescription()
                    });
                }

            }
            catch (Exception ex)
            {
                DeleteQCDetail(_TransSeq.ToString());
                DeleteQRStockITDet(_QRTransSeqInvTrans.ToString());
                _logger.LogError(" Error in IncomingQCController : QC() :: {Error}", ex.ToString());
                return BadRequest(new
                {
                    StatusCode = "400",
                    StatusMsg = ex.ToString()
                });
            }
        }


        private bool DeleteQCDetail(string _TransSeq)
        {
            string _IsSaved = "N";
            SqlConnection QITcon;
            try
            {
                _logger.LogInformation(" Calling IncomingQCController : DeleteQCDetail() ");

                QITcon = new SqlConnection(_QIT_connection);
                _Query = @" DELETE FROM QIT_QC_Detail WHERE TransSeq = @transSeq";
                _logger.LogInformation(" IncomingQCController : DeleteQCDetail Query : {q} ", _Query.ToString());

                cmd = new SqlCommand(_Query, QITcon);
                cmd.Parameters.AddWithValue("@transSeq", _TransSeq);
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
                _logger.LogError("Error in IncomingQCController : DeleteQCDetail() :: {Error}", ex.ToString());
                return false;
            }
        }

        private bool DeleteQRStockITDet(string _QRTransSeq)
        {
            string _IsSaved = "N";
            try
            {
                _logger.LogInformation(" Calling IncomingQCController : DeleteQRStockITDet() ");

                string p_ErrorMsg = string.Empty;

                SqlConnection con = new SqlConnection(_QIT_connection);
                _Query = @" DELETE FROM QIT_QRStock_InvTrans WHERE QRTransSeq = @qrtransSeq and FromObjType = '67' and ToObjType = '67' ";
                _logger.LogInformation(" IncomingQCController : DeleteQRStockITDet Query : {q} ", _Query.ToString());

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
                _logger.LogError("Error in IncomingQCController : DeleteQRStockITDet() :: {Error}", ex.ToString());
                return false;
            }
        }


        #region New flow - scan QR

        [HttpPost("ValidateItemQRNewFlow")]
        public async Task<ActionResult<IEnumerable<GRPOItem>>> ValidateItemQRNewFlow(ValidateItemQRInput payload)
        {
            SqlConnection QITcon;
            SqlDataAdapter oAdptr;
            try
            {
                _logger.LogInformation(" Calling IncomingQCController : ValidateItemQR() ");
                List<GRPOItem> obj = new List<GRPOItem>();
                System.Data.DataTable dtQRData = new System.Data.DataTable();
                System.Data.DataTable dtData = new System.Data.DataTable();
                QITcon = new SqlConnection(_QIT_connection);
                string _where = string.Empty;

                #region Validate Inputs

                if (payload.BranchID == 0)
                {
                    return BadRequest(new
                    {
                        StatusCode = "400",
                        StatusMsg = "Provide Branch"
                    });
                }

                if (payload.DetailQRCodeID == String.Empty || payload.DetailQRCodeID.ToLower() == "string")
                {
                    return BadRequest(new
                    {
                        StatusCode = "400",
                        StatusMsg = "Provide Detail QR"
                    });
                }

                #endregion

                #region Get QR Details
                _Query = " select * from QIT_QR_Detail A where ISNULL(A.BranchID, @bID) = @bID and A.QRCodeId = @dQR and A.Canceled = 'N' ";
                _logger.LogInformation(" IncomingQCController : Query : {q} ", _Query.ToString());
                QITcon.Open();
                oAdptr = new SqlDataAdapter(_Query, QITcon);
                oAdptr.SelectCommand.Parameters.AddWithValue("@bID", payload.BranchID);
                oAdptr.SelectCommand.Parameters.AddWithValue("@dQR", payload.DetailQRCodeID.Replace(" ", "~"));

                oAdptr.Fill(dtQRData);
                QITcon.Close();

                if (dtQRData.Rows.Count <= 0)
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "No such QR exist" });
                }
                #endregion

                #region Check GRPO is done or not for the QR

                _Query = @" SELECT A.* FROM QIT_QRStock_POToGRPO A WHERE A.QRCodeID = @dQR AND ISNULL(A.BranchID, @bID) = @bID ";
                _logger.LogInformation(" IncomingQCController : Query : {q} ", _Query.ToString());
                QITcon.Open();
                oAdptr = new SqlDataAdapter(_Query, QITcon);
                oAdptr.SelectCommand.Parameters.AddWithValue("@bID", payload.BranchID);
                oAdptr.SelectCommand.Parameters.AddWithValue("@dQR", payload.DetailQRCodeID.Replace(" ", "~"));
                oAdptr.Fill(dtData);
                QITcon.Close();

                if (dtData.Rows.Count <= 0)
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "GRPO is pending for the QR : " + payload.DetailQRCodeID.Replace("~", " ") });
                }

                #endregion

                #region Check for QC applicable or not 
                _Query = @" 
                SELECT A.ItemCode, A.QRCodeID, B.U_QA
                FROM QIT_QRStock_POToGRPO A 
                     INNER JOIN " + Global.SAP_DB + @".dbo.OITM B ON A.ItemCode collate SQL_Latin1_General_CP850_CI_AS = B.ItemCode
                WHERE A.QRCodeID = @dQR AND B.U_QA in (1,3) AND ISNULL(A.BranchID, @bID) = @bID ";

                _logger.LogInformation(" IncomingQCController : ValidateItemQR() Query : {q} ", _Query.ToString());
                dtData = new DataTable();
                QITcon.Open();
                oAdptr = new SqlDataAdapter(_Query, QITcon);
                oAdptr.SelectCommand.Parameters.AddWithValue("@bID", payload.BranchID);
                oAdptr.SelectCommand.Parameters.AddWithValue("@dQR", payload.DetailQRCodeID.Replace(" ", "~"));
                oAdptr.Fill(dtData);
                QITcon.Close();

                if (dtData.Rows.Count <= 0)
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "QC is not applicable for QR : " + payload.DetailQRCodeID.Replace("~", " ") });
                }
                #endregion

                #region Check already QC is done or not
                DataTable dtQCData = new DataTable();

                _Query = @" 
                SELECT * FROM 
                (
                    SELECT 
                    (
                        SELECT ISNULL(SUM(A.Qty),0) from QIT_QR_detail A
                        WHERE A.QRCodeID = @dQR AND ISNULL(A.BranchID, @bID) = @bID
                    ) -
                    (
                        SELECT ISNULL(SUM(A.Qty),0) QCQty FROM QIT_QC_Detail A   
                        WHERE A.QRCodeID = @dQR AND ISNULL(A.BranchID, @bID) = @bID
                    ) as PendQty
                ) as A
                where A.PendQty = 0  ";
                _logger.LogInformation(" IncomingQCController : ValidateItemQR : QC already done Query : {q} ", _Query.ToString());
                QITcon.Open();
                oAdptr = new SqlDataAdapter(_Query, QITcon);
                oAdptr.SelectCommand.Parameters.AddWithValue("@bID", payload.BranchID);
                oAdptr.SelectCommand.Parameters.AddWithValue("@dQR", payload.DetailQRCodeID.Replace(" ", "~"));
                oAdptr.Fill(dtQCData);
                QITcon.Close();

                if (dtQCData.Rows.Count > 0)
                {
                    return BadRequest(new
                    {
                        StatusCode = "400",
                        StatusMsg = "QC is already done for : " + payload.DetailQRCodeID.Replace("~", " ")
                    });
                }

                #endregion

                dtData = new DataTable();
                _Query = @" 
                    SELECT A.* FROM
                    (
                     SELECT A.DocEntry PODocEntry, A.DocNum PODocNum, A.QRCodeID HeaderQRCodeID, B.QRCodeID DetailQRCodeID, B.GateInNo,
                                C.DocEntry GRPODocEntry, C.DocNum GRPODocNum, C.CardCode, C.CardName,
                          C.DocDate, D.ItemCode, D.Dscription ItemName, D.Quantity RecQty, B.Qty QRQty, 
                       ( SELECT ISNULL(sum(Qty),0) FROM QIT_QC_Detail WHERE QRCodeID = @dQR ) QCQty,
                                D.Project, D.UomCode, D.WhsCode,
                                E.ToWhs FromWhs, ISNULL(E.ToBinAbsEntry,0) FromBin, 
                                (select BinCode from " + Global.SAP_DB + @".dbo.OBIN where AbsEntry = E.ToBinAbsEntry) FromBinCode
                     FROM QIT_QR_Header A 
                          INNER JOIN QIT_QR_Detail B ON A.HeaderSrNo = B.HeaderSrNo
                          INNER JOIN " + Global.SAP_DB + @".dbo.OPDN C ON C.CANCELED = 'N' AND C.DocEntry = 
                                           ( select distinct DocEntry from QIT_Trans_POToGRPO Z1 
                                                    inner join QIT_QRStock_POToGRPO Z2 ON                     
                                                    Z1.TransSeq = Z2.TransSeq where Z2.QRCodeID = @dQR)
                          INNER JOIN " + Global.SAP_DB + @".dbo.PDN1 D ON D.DocEntry = C.DocEntry AND 
                                     D.ItemCode collate SQL_Latin1_General_CP850_CI_AS = B.ItemCode AND 
                                     D.BaseLine = B.LineNum AND D.BaseEntry = A.DocEntry
                                INNER JOIN QIT_QRStock_InvTrans E ON E.QRCodeID = B.QRCodeID
                     WHERE B.QRCodeID = @dQR AND ISNULL(A.BranchID, 1) = @bID AND
                                E.TransId = (select max(TRansID) from QIT_QRStock_InvTrans where QRCodeID = @dQR and FromObjType = '22' and ToObjType = '67' and 
        ItemCode = B.ItemCode AND GateInNo = B.GateInNo )
                    ) as A ";

                _logger.LogInformation(" IncomingQCController : ValidateItemQR() Query : {q} ", _Query.ToString());
                QITcon.Open();
                oAdptr = new SqlDataAdapter(_Query, QITcon);
                oAdptr.SelectCommand.Parameters.AddWithValue("@bID", payload.BranchID);
                oAdptr.SelectCommand.Parameters.AddWithValue("@dQR", payload.DetailQRCodeID.Replace(" ", "~"));
                oAdptr.Fill(dtData);
                QITcon.Close();

                if (dtData.Rows.Count > 0)
                {
                    dynamic arData = JsonConvert.SerializeObject(dtData);
                    obj = JsonConvert.DeserializeObject<List<GRPOItem>>(arData.ToString().Replace("~", " "));
                    return obj;
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "No data found" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(" Error in IncomingQCController : ValidateItemQR() :: {Error}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
            }
        }

        #endregion
    }
}
