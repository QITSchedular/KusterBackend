using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Data.SqlClient;
using System.Data;
using WMS_UI_API.Common;
using WMS_UI_API.Models;
using SAPbobsCOM;
using WMS_UI_API.Services;
using Microsoft.Extensions.Configuration;

namespace WMS_UI_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class InprocessQCController : ControllerBase
    {
        private string _ApplicationApiKey = string.Empty;
        private string _connection = string.Empty;
        private string _QIT_connection = string.Empty;
        private string _QIT_DB = string.Empty;
        private string _Query = string.Empty;
        private SqlCommand cmd;
        public Global objGlobal;
        public string _BaseUri = string.Empty;

        public IConfiguration Configuration { get; }
        private readonly ILogger<InprocessQCController> _logger;
        private readonly ILogger<InventoryTransferController> _invTrnflogger;
        private readonly ISAPConnectionService _sapConnectionService;

        public InprocessQCController(IConfiguration configuration, ILogger<InprocessQCController> logger, ISAPConnectionService sapConnectionService)
        {
            objGlobal = new Global();
            _logger = logger;
          
            Configuration = configuration;
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
                _logger.LogError(" Error in InprocessQCController :: {Error}" + ex.ToString());
            }
        }


        [HttpPost("GetPRDList")]
        public async Task<ActionResult<IEnumerable<PRDList>>> GetPRDList(RECPRDListFilter payload)
        {
            try
            {
                _logger.LogInformation(" Calling InprocessQCController : GetPRDList() ");
                List<PRDList> obj = new List<PRDList>();
                System.Data.DataTable dtData = new System.Data.DataTable();
                SqlConnection con = new SqlConnection(_connection);
                string _where = string.Empty;

                if (payload.BranchID == 0)
                {
                    return BadRequest(new
                    {
                        StatusCode = "400",
                        StatusMsg = "Provide Branch"
                    });
                }

                if (payload.FromDate != String.Empty && payload.FromDate.ToLower() != "string")
                {
                    _where += " AND A.DocDate >= @frDate";
                }

                if (payload.ToDate != String.Empty && payload.ToDate.ToLower() != "string")
                {
                    _where += " AND A.DocDate <= @toDate";
                }

                _Query = @" SELECT DISTINCT T0.QRCodeID, T0.DocNum ProOrdDocNum, T0.DocEntry ProOrdDocEntry, '' CardCode, '' CardName, T3.Series, T3.SeriesName
                            FROM " + Global.QIT_DB + @".dbo.QIT_Production_QR_Header T0
							INNER JOIN " + Global.QIT_DB + @".dbo.QIT_Trans_ProToReceipt T1 ON T0.DocEntry = T1.BaseDocEntry 
							INNER JOIN OWOR T2 on T0.DocEntry = T2.DocEntry --and T2.Status = 'R' 
							INNER JOIN NNM1 T3 on T2.Series = T3.Series
							WHERE ISNULL(T0.BranchID, 1) = 1 " + _where + @"
                          ";

                _logger.LogInformation(" InprocessQCController : GetPRDList() Query : {q} ", _Query.ToString());

                con.Open();
                SqlDataAdapter oAdptr = new SqlDataAdapter(_Query, con);
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
                con.Close();
                if (dtData.Rows.Count > 0)
                {
                    dynamic arData = JsonConvert.SerializeObject(dtData);
                    obj = JsonConvert.DeserializeObject<List<PRDList>>(arData.ToString().Replace("~", " "));
                    return obj;
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "No data found" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(" Error in InprocessQCController : GetPRDList() :: {Error}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
            }
            finally { }
        }


        [HttpPost("GetRECListByPRD")]
        public async Task<ActionResult<IEnumerable<ProReceiptList>>> GetRecListByPRD(RECPRDListFilter payload)
        {
            try
            {
                _logger.LogInformation(" Calling InprocessQCController : GetRecListByPRD() ");
                List<ProReceiptList> obj = new List<ProReceiptList>();
                System.Data.DataTable dtData = new System.Data.DataTable();
                SqlConnection con = new SqlConnection(_connection);
                string _where = string.Empty;

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

                if (payload.FromDate != String.Empty && payload.FromDate.ToLower() != "string")
                {
                    _where += " AND A.DocDate >= @frDate";
                }

                if (payload.ToDate != String.Empty && payload.ToDate.ToLower() != "string")
                {
                    _where += " AND A.DocDate <= @toDate";
                }

                _Query = @" SELECT '" + payload.HeaderQRCodeID.Replace("~", " ") + @"' HeaderQRCodeID,C.DocEntry, C.DocNum, '' CardCode, '' CardName, '' NumAtCard,
		                           C.Series, D.SeriesName, C.DocDate PostDate, C.TaxDate DocDate, C.Project, 
		                           A.ProOrdDocEntry, A.ProOrdDocNum , C.Comments 
                            FROM 
                            (
                                 SELECT DISTINCT B.DocEntry, A.DocNum ProOrdDocNum, A.DocEntry ProOrdDocEntry 
                                 FROM " + Global.QIT_DB + @".dbo.QIT_Production_QR_Header A 
                                 INNER JOIN " + Global.QIT_DB + @".dbo.QIT_Trans_ProToReceipt B ON A.DocEntry = B.BaseDocEntry and A.DocNum = B.BaseDocNum 
                                 WHERE A.QRCodeID = @hQR AND ISNULL(A.BranchID, @bID) = @bID " + _where + @"
                            ) as A
                            INNER JOIN OIGN C ON A.DocEntry = C.DocEntry 
                            INNER JOIN NNM1 D ON C.Series = D.Series 
                            WHERE C.CANCELED = 'N' ";

                _logger.LogInformation(" InprocessQCController : GetRecListByPRD() Query : {q} ", _Query.ToString());
                con.Open();
                SqlDataAdapter oAdptr = new SqlDataAdapter(_Query, con);
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
                con.Close();

                if (dtData.Rows.Count > 0)
                {
                    dynamic arData = JsonConvert.SerializeObject(dtData);
                    obj = JsonConvert.DeserializeObject<List<ProReceiptList>>(arData.ToString().Replace("~", " "));
                    return obj;
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "No data found" });
                }

            }
            catch (Exception ex)
            {
                _logger.LogError(" Error in InprocessQCController : GetRECListByPRD() :: {Error}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
            }
        }


        [HttpPost("ValidateItem")]
        public async Task<ActionResult<IEnumerable<RECItem>>> ValidateItem(RECPRDItemFilter payload)
        {
            try
            {
                _logger.LogInformation(" Calling InprocessQCController : ValidateItem() ");
                List<RECItem> obj = new List<RECItem>();
                System.Data.DataTable dtData = new System.Data.DataTable();
                SqlConnection con = new SqlConnection(_connection);
                string _where = string.Empty;

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

                if (payload.RECDocEntry == 0)
                {
                    return BadRequest(new
                    {
                        StatusCode = "400",
                        StatusMsg = "Provide Receive Document Entry"
                    });
                }

                #region Check for QC applicable or not 
                _Query = @" SELECT A.ItemCode, A.QRCodeID, B.U_QA
                            FROM " + Global.QIT_DB + @".dbo.QIT_QRStock_ProToReceipt A 
                            INNER JOIN OITM B ON A.ItemCode collate SQL_Latin1_General_CP850_CI_AS = B.ItemCode
                            WHERE A.QRCodeID = @dQR AND B.U_QA in (2) AND ISNULL(A.BranchID, @bID) = @bID ";

                _logger.LogInformation(" InprocessQCController : ValidateItem() Query : {q} ", _Query.ToString());
                con.Open();
                SqlDataAdapter oAdptr = new SqlDataAdapter(_Query, con);
                oAdptr.SelectCommand.Parameters.AddWithValue("@bID", payload.BranchID);
                oAdptr.SelectCommand.Parameters.AddWithValue("@dQR", payload.DetailQRCodeID.Replace(" ", "~"));
                oAdptr.Fill(dtData);
                con.Close();

                if (dtData.Rows.Count <= 0)
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "QC is not applicable for QR : " + payload.DetailQRCodeID.Replace("~", " ") });
                }
                #endregion

                dtData = new DataTable();
                _Query = @" SELECT A.* FROM
                            (
	                            SELECT A.DocEntry ProOrdDocEntry, A.DocNum ProOrdDocNum, A.QRCodeID HeaderQRCodeID, B.QRCodeID DetailQRCodeID,
                                       C.DocEntry RecDocEntry, C.DocNum RecDocNum, C.CardCode, C.CardName,
		                               C.DocDate, D.ItemCode, D.Dscription ItemName, E.Qty RecQty, D.Project, F.UomCode, D.WhsCode,
                                       E.ToWhs FromWhs, ISNULL(E.ToBinAbsEntry,0) FromBin, (select BInCode from OBIN where AbsEntry = E.ToBinAbsEntry) FromBinCode
	                            FROM " + Global.QIT_DB + @".dbo.QIT_Production_QR_Header A 
		                                INNER JOIN " + Global.QIT_DB + @".dbo.QIT_Production_QR_Detail B ON A.HeaderSrNo = B.HeaderSrNo
		                                INNER JOIN OIGN C ON C.CANCELED = 'N' AND C.DocEntry = @recDocEntry
		                                INNER JOIN IGN1 D ON D.DocEntry = C.DocEntry AND D.ItemCode collate SQL_Latin1_General_CP850_CI_AS = B.ItemCode 
                                        INNER JOIN " + Global.QIT_DB + @".dbo.QIT_QRStock_ProToReceipt E ON E.QRCodeID = B.QRCodeID
			                            INNER JOIN " + Global.QIT_DB + @".dbo.QIT_Item_Master F ON F.ItemCode collate SQL_Latin1_General_CP850_CI_AS = D.ItemCode
	                            WHERE A.QRCodeID = @hQR AND B.QRCodeID = @dQR AND ISNULL(A.BranchID, @bID) = @bID  
                            ) as A ";

                _logger.LogInformation(" InprocessQCController : ValidateItem() Query : {q} ", _Query.ToString());
                con.Open();
                oAdptr = new SqlDataAdapter(_Query, con);
                oAdptr.SelectCommand.Parameters.AddWithValue("@bID", payload.BranchID);
                oAdptr.SelectCommand.Parameters.AddWithValue("@hQR", payload.HeaderQRCodeID.Replace(" ", "~"));
                oAdptr.SelectCommand.Parameters.AddWithValue("@dQR", payload.DetailQRCodeID.Replace(" ", "~"));
                oAdptr.SelectCommand.Parameters.AddWithValue("@recDocEntry", payload.RECDocEntry);
                oAdptr.Fill(dtData);
                con.Close();

                if (dtData.Rows.Count > 0)
                {
                    dynamic arData = JsonConvert.SerializeObject(dtData);
                    obj = JsonConvert.DeserializeObject<List<RECItem>>(arData.ToString().Replace("~", " "));
                    return obj;
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "No data found" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(" Error in InprocessQCController : ValidateItem() :: {Error}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
            }
        }


        [HttpPost("InProcessQC")]
        public async Task<ActionResult<IEnumerable<string>>> InProcessQC(INPROQCPayload payload)
        {
            try
            {
                _logger.LogInformation(" Calling InprocessQCController : InProcessQC() ");

                System.Data.DataTable dtQRData = new System.Data.DataTable();
                System.Data.DataTable dtRECItemData = new System.Data.DataTable();
                DataTable dtConfig = new DataTable();
                System.Data.DataTable dtQCData = new System.Data.DataTable();
                string _ToWhsCode = string.Empty;
                int _ToBinAbsEntry = 0;

                #region Validation

                if (payload.BranchID == 0)
                {
                    return BadRequest(new
                    {
                        StatusCode = "400",
                        StatusMsg = "Provide Branch"
                    });
                }

                if (payload.RECDocEntry == 0)
                {
                    return BadRequest(new
                    {
                        StatusCode = "400",
                        StatusMsg = "Provide Receive Document Entry"
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

                #region Check already QC is done or not
                SqlConnection QITcon = new SqlConnection(_QIT_connection);

                _Query = @" SELECT A.* FROM QIT_InProcQC_Detail A   
                       WHERE A.QRCodeID = @dQR AND ISNULL(A.BranchID, @bID) = @bID  ";
                _logger.LogInformation(" InprocessQCController : QC already done Query : {q} ", _Query.ToString());
                QITcon.Open();
                SqlDataAdapter oAdptr = new SqlDataAdapter(_Query, QITcon);
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

                _Query = @" SELECT A.*, B.ItemMngBy 
                       FROM QIT_Production_QR_Detail A  
                       INNER JOIN QIT_Item_Master B on A.ItemCode = B.ItemCode
                       WHERE A.QRCodeID = @dQR AND ISNULL(A.BranchID, @bID) = @bID  ";
                _logger.LogInformation(" InprocessQCController : QRCode Query : {q} ", _Query.ToString());
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
                _logger.LogInformation(" InprocessQCController : Configuration  Query : {q} ", _Query.ToString());
                QITcon.Open();
                oAdptr = new SqlDataAdapter(_Query, QITcon);
                oAdptr.SelectCommand.Parameters.AddWithValue("@bID", payload.BranchID);
                oAdptr.Fill(dtConfig);
                QITcon.Close();
                #endregion

                #region Check for Valid Whs
                if (payload.Action.ToUpper() == "A" && dtConfig.Rows[0]["ApprovedWhs"].ToString().Trim().Length <= 0)
                    return BadRequest(new
                    {
                        StatusCode = "400",
                        StatusMsg = "Define Approve Warehouse in Configuration"
                    });

                if (payload.Action.ToUpper() == "R" && dtConfig.Rows[0]["RejectedWhs"].ToString().Trim().Length <= 0)
                    return BadRequest(new
                    {
                        StatusCode = "400",
                        StatusMsg = "Define Reject Warehouse in Configuration"
                    });

                _ToWhsCode = payload.Action.ToUpper() == "A" ? dtConfig.Rows[0]["ApprovedWhs"].ToString().Trim() : dtConfig.Rows[0]["RejectedWhs"].ToString().Trim();
                _ToBinAbsEntry = payload.Action.ToUpper() == "A" ? int.Parse(dtConfig.Rows[0]["ApproveBin"].ToString().Trim()) : int.Parse(dtConfig.Rows[0]["RejectBin"].ToString().Trim());
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
                    #region Get Receipt Detail
                    SqlConnection con = new SqlConnection(_connection);

                    _Query = @" SELECT A.DocEntry, A.DocNum, A.CardCode, A.CardName, A.Series, A.DocDate, A.TaxDate, 
                              B.ItemCode, B.Dscription ItemName, B.Project, B.Quantity
                       FROM OIGN A INNER JOIN IGN1 B ON A.DocEntry = B.DocEntry 
                       WHERE A.DocEntry = @recDocEntry and B.ItemCode = @itemCode and A.CANCELED = 'N' and ISNULL(A.BPLId, @bID) = @bID ";
                    _logger.LogInformation(" InprocessQCController : Receipt Data Query : {q} ", _Query.ToString());
                    con.Open();
                    SqlDataAdapter oAdptr1 = new SqlDataAdapter(_Query, con);
                    oAdptr1.SelectCommand.Parameters.AddWithValue("@bID", payload.BranchID);
                    oAdptr1.SelectCommand.Parameters.AddWithValue("@recDocEntry", payload.RECDocEntry);
                    oAdptr1.SelectCommand.Parameters.AddWithValue("@itemCode", dtQRData.Rows[0]["ItemCode"].ToString());
                    oAdptr1.Fill(dtRECItemData);
                    con.Close();
                    #endregion

                    if (dtRECItemData.Rows.Count > 0)
                    {
                        #region Get TransID
                        QITcon = new SqlConnection(_QIT_connection);
                        _Query = @" SELECT ISNULL(max(TransId),0) + 1 FROM QIT_InProcQC_Detail A  ";
                        _logger.LogInformation(" InprocessQCController : Get TransID Query : {q} ", _Query.ToString());

                        SqlCommand cmd = new SqlCommand(_Query, QITcon);
                        QITcon.Open();
                        Object _TransId = cmd.ExecuteScalar();
                        QITcon.Close();
                        #endregion

                        QITcon = new SqlConnection(_QIT_connection);
                        _Query = @"
                   INSERT INTO QIT_InProcQC_Detail
                   (TransId, BranchId, RecDocEntry, RecDocNum, RecSeries, QRCodeID, ItemCode, RecNo, BatchSerialNo, 
                    FromWhs, ToWhs, FromBinAbsEntry, ToBinAbsEntry, Status, Qty, RejComment)
                   VALUES (@transId, @bID, @recDocEntry, @recDocNum, @recSeries, @qr, @itemCode, @recNo, @batchSerial, 
                    @fromWhs, @toWhs, @fromBin, @toBin, @status, @qty, @rejComment)
                   ";

                        _logger.LogInformation(" InprocessQCController : InProcess QC Query : {q} ", _Query.ToString());

                        cmd = new SqlCommand(_Query, QITcon);
                        cmd.Parameters.AddWithValue("@transId", _TransId);
                        cmd.Parameters.AddWithValue("@bID", payload.BranchID);
                        cmd.Parameters.AddWithValue("@recDocEntry", dtRECItemData.Rows[0]["DocEntry"]);
                        cmd.Parameters.AddWithValue("@recDocNum", dtRECItemData.Rows[0]["DocNum"]);
                        cmd.Parameters.AddWithValue("@recSeries", dtRECItemData.Rows[0]["Series"]);
                        cmd.Parameters.AddWithValue("@qr", payload.DetailQRCodeID.Replace(" ", "~"));
                        cmd.Parameters.AddWithValue("@itemCode", dtQRData.Rows[0]["ItemCode"]);
                        cmd.Parameters.AddWithValue("@recNo", dtQRData.Rows[0]["RecNo"]);
                        cmd.Parameters.AddWithValue("@batchSerial", dtQRData.Rows[0]["BatchSerialNo"]);
                        cmd.Parameters.AddWithValue("@fromWhs", payload.FromWhs);
                        cmd.Parameters.AddWithValue("@toWhs", _ToWhsCode);
                        cmd.Parameters.AddWithValue("@fromBin", payload.FromBin);
                        cmd.Parameters.AddWithValue("@toBin", _ToBinAbsEntry);
                        cmd.Parameters.AddWithValue("@status", payload.Action);
                        cmd.Parameters.AddWithValue("@qty", payload.qty);
                        cmd.Parameters.AddWithValue("@rejComment", payload.Action.ToUpper() == "A" ? string.Empty : payload.RejectComment);

                        int intNum = 0;
                        try
                        {
                            QITcon.Open();
                            intNum = cmd.ExecuteNonQuery();
                            QITcon.Close();

                            var jsonObject = new
                            {
                                BranchID = payload.BranchID,
                                Series = int.Parse(dtConfig.Rows[0]["QCITSeries"].ToString()),
                                CardCode = dtRECItemData.Rows[0]["CardCode"].ToString(),
                                FromWhsCode = payload.FromWhs,
                                ToWhsCode = _ToWhsCode,
                                Comments = payload.Action == "R" ? payload.RejectComment : String.Empty,
                                FromObjType = "59",
                                ToObjType = "67",
                                FromBinAbsEntry = payload.FromBin,
                                ToBinAbsEntry = _ToBinAbsEntry,
                                itDetails = dtQRData.AsEnumerable().GroupBy(row => row["ItemCode"]).Select(itemGroup => new
                                {
                                    itemCode = itemGroup.Key.ToString(),
                                    TotalItemQty = payload.qty,
                                    Project = dtRECItemData.Rows[0]["Project"].ToString(),
                                    itemMngBy = itemGroup.First()["itemMngBy"].ToString(),
                                    itQRDetails = itemGroup.Select(row => new
                                    {
                                        gateInNo = row["RecNo"].ToString(),
                                        itemCode = row["ItemCode"].ToString(),
                                        detailQRCodeID = row["QRCodeID"].ToString(),
                                        batchSerialNo = row["BatchSerialNo"].ToString(),
                                        qty = payload.qty
                                    }).ToList()
                                }).ToList()
                            };

                            var content = new StringContent(jsonObject.ToString());

                            HttpClient client = new HttpClient();
                            client.BaseAddress = new System.Uri(_BaseUri);
                            client.Timeout = TimeSpan.FromMinutes(2);
                            HttpResponseMessage response = await client.PostAsJsonAsync("api/InventoryTransfer/InventoryTransfer", jsonObject);
                            var r = response.Content.ReadAsStringAsync();
                            ApiResponses_Inv _res = JsonConvert.DeserializeObject<ApiResponses_Inv>(r.Result);
                            if (_res.IsSaved == "Y")
                            {
                                return Ok(new
                                {
                                    StatusCode = "200",
                                    IsSaved = "Y",
                                    StatusMsg = "QC done successfully !!!"
                                });
                            }
                            else
                            {
                                this.DeleteInprocessQCDetail(_TransId.ToString());

                                return BadRequest(new
                                {
                                    StatusCode = "400",
                                    IsSaved = "N",
                                    TransSeq = _TransId,
                                    StatusMsg = _res.StatusMsg
                                });
                            }
                        }
                        catch (Exception ex1)
                        {
                            this.DeleteInprocessQCDetail(_TransId.ToString());

                            return BadRequest(new
                            {
                                StatusCode = "400",
                                IsSaved = "N",
                                TransSeq = _TransId,
                                StatusMsg = "Problem while saving Inprocess QC data"
                            });

                        }
                    }
                    else
                    {
                        return BadRequest(new { StatusCode = "400", StatusMsg = "GRPO Details not found" });
                    }
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "QR Code does not exist : " + payload.DetailQRCodeID.Replace("~", " ") });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(" Error in InprocessQCController : InProcessQC() :: {Error}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
            }
        }

        private bool DeleteInprocessQCDetail(string _TransSeq)
        {
            string _IsSaved = "N";
            SqlConnection QITcon;
            try
            {
                _logger.LogInformation(" Calling InprocessQCController : DeleteInprocessQCDetail() ");

                QITcon = new SqlConnection(_QIT_connection);
                _Query = @" DELETE FROM QIT_InProcQC_Detail WHERE TransSeq = @transSeq";
                _logger.LogInformation(" InprocessQCController : DeleteInprocessQCDetail Query : {q} ", _Query.ToString());

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
                _logger.LogError("Error in InprocessQCController : DeleteInprocessQCDetail() :: {Error}", ex.ToString());
                return false;
            }
        }


        [HttpPost("ValidateItemNewFlow")]
        public async Task<ActionResult<IEnumerable<RECItem>>> ValidateItemNewFlow(ValidateItemQRInputQC payload)
        {
            SqlConnection QITcon;
            SqlDataAdapter oAdptr;
            try
            {
                _logger.LogInformation(" Calling InprocessQCController : ValidateItemNewFlow() ");
                List<RECItem> obj = new List<RECItem>();
                System.Data.DataTable dtQRData = new System.Data.DataTable();
                System.Data.DataTable dtData = new System.Data.DataTable();
                System.Data.DataTable dtConfig = new System.Data.DataTable();
                QITcon = new SqlConnection(_QIT_connection);
                string _where = string.Empty;
                string _InProcessQCWhs = string.Empty;

                #region Get Configuration
                QITcon = new SqlConnection(_QIT_connection);
                _Query = @" SELECT * FROM QIT_Config_Master A WHERE ISNULL(A.BranchID, @bID) = @bID  ";
                _logger.LogInformation(" InprocessQCController : Configuration  Query : {q} ", _Query.ToString());
                QITcon.Open();
                oAdptr = new SqlDataAdapter(_Query, QITcon);
                oAdptr.SelectCommand.Parameters.AddWithValue("@bID", payload.BranchID);
                oAdptr.Fill(dtConfig);
                QITcon.Close();

                if (dtConfig.Rows.Count > 0)
                {
                    if (dtConfig.Rows[0]["InProcessQCWhs"].ToString().Trim().Length <= 0)
                        return BadRequest(new { StatusCode = "400", StatusMsg = "Define InProcess QC Warehouse in Configuration" });
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Configuration not found" });
                }
                _InProcessQCWhs = dtConfig.Rows[0]["InProcessQCWhs"].ToString().Trim();

                #endregion

                #region Validation

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
                _Query = @"
                SELECT * FROM QIT_Production_QR_Detail A 
                WHERE ISNULL(A.BranchID, @bID) = @bID and A.QRCodeId = @dQR ";
                _logger.LogInformation(" InprocessQCController : Query : {q} ", _Query.ToString());
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

                #region Check Production Receipt is done or not for the QR

                _Query = @" SELECT A.* FROM QIT_QRStock_ProToReceipt A WHERE A.QRCodeID = @dQR AND ISNULL(A.BranchID, @bID) = @bID ";
                _logger.LogInformation(" InprocessQCController : Query : {q} ", _Query.ToString());
                QITcon.Open();
                oAdptr = new SqlDataAdapter(_Query, QITcon);
                oAdptr.SelectCommand.Parameters.AddWithValue("@bID", payload.BranchID);
                oAdptr.SelectCommand.Parameters.AddWithValue("@dQR", payload.DetailQRCodeID.Replace(" ", "~"));
                oAdptr.Fill(dtData);
                QITcon.Close();

                if (dtData.Rows.Count <= 0)
                {
                    return BadRequest(new
                    {
                        StatusCode = "400",
                        StatusMsg = "Production Receipt is pending for the QR : " + payload.DetailQRCodeID.Replace("~", " ")
                    });
                }

                #endregion

                #region Check for QC applicable or not 
                dtData = new DataTable();
                _Query = @" SELECT A.ItemCode, A.QRCodeID, B.U_QA
                            FROM QIT_QRStock_ProToReceipt A 
                            INNER JOIN " + Global.SAP_DB + @".dbo.OITM B ON A.ItemCode collate SQL_Latin1_General_CP850_CI_AS = B.ItemCode
                            WHERE A.QRCodeID = @dQR AND B.U_QA in (2) AND ISNULL(A.BranchID, @bID) = @bID ";

                _logger.LogInformation(" InprocessQCController : ValidateItemNewFlow() Query : {q} ", _Query.ToString());
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
                        SELECT ISNULL(SUM(A.Qty),0) from QIT_Production_QR_Detail A
                        WHERE A.QRCodeID = @dQR AND ISNULL(A.BranchID, @bID) = @bID
                    ) -
                    (
                        SELECT ISNULL(SUM(A.Qty),0) QCQty FROM QIT_InProcQC_Detail A   
                        WHERE A.QRCodeID = @dQR AND ISNULL(A.BranchID, @bID) = @bID
                    ) as PendQty
                ) as A
                WHERE A.PendQty = 0  ";
                _logger.LogInformation(" InprocessQCController : ValidateItemNewFlow : QC already done Query : {q} ", _Query.ToString());
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

                #region Stock Query
                _Query = @"  
                SELECT * FROM 
                (
                     SELECT A.WhsCode, A.WhsName, 
                            CASE WHEN A.ProToReceipt > 0 THEN (ProToReceipt + InQty - OutQty - IssueQty - DeliverQty + ReceiptQty) 
                                 ELSE (InQty - OutQty - IssueQty - DeliverQty + ReceiptQty) END Stock
                     FROM 
                     (
                         SELECT A.WhsCode, A.WhsName,
                         (
                             ISNULL((	
                                 SELECT sum(Z.Qty) ProToReceipt FROM [QIT_ProQRStock_InvTrans] Z
                                 WHERE Z.QRCodeID = @qrCode and Z.FromObjType = '202' and Z.ToObjType = '59' and 
                                       Z.ToWhs = A.WhsCode and ISNULL(Z.BranchID, @bID) = @bID
                             ),0)
                         ) ProToReceipt,
                         (
                             ISNULL((
                                 SELECT sum(Z.Qty) InQty FROM [QIT_ProQRStock_InvTrans] Z
                                 WHERE Z.QRCodeID = @qrCode and (Z.FromObjType <> '202') AND 
                                       Z.ToWhs collate SQL_Latin1_General_CP850_CI_AS = A.WhsCode and ISNULL(Z.BranchID, @bID) = @bID
                             ),0) 
                         ) InQty,
                         (
                             ISNULL((
                                 SELECT sum(Z.Qty) OutQty from [QIT_ProQRStock_InvTrans] Z
                                 WHERE Z.QRCodeID = @qrCode and (Z.FromObjType <> '202') and 
                                       Z.FromWhs collate SQL_Latin1_General_CP850_CI_AS = A.WhsCode and ISNULL(Z.BranchID, @bID) = @bID
                             ),0)
                         ) OutQty,
                         (
                             ISNULL((
                                 SELECT sum(Z.Qty) IssueQty from QIT_QRStock_ProToIssue Z
                                 WHERE Z.QRCodeID = @qrCode and  
                                       Z.FromWhs collate SQL_Latin1_General_CP850_CI_AS = A.WhsCode and ISNULL(Z.BranchID, @bID) = @bID
                             ),0)
                         ) IssueQty,
                         (
                             ISNULL((
                                 SELECT sum(Z.Qty) DeliverQty from QIT_QRStock_SOToDelivery Z
                                 WHERE Z.QRCodeID = @qrCode and  
                                       Z.ToWhs collate SQL_Latin1_General_CP850_CI_AS = A.WhsCode and ISNULL(Z.BranchID, @bID) = @bID
                             ),0)
                         ) DeliverQty,
                         (
                             ISNULL((
                                 SELECT sum(Z.Qty) ReceiptQty from QIT_QRStock_ProToReceipt Z
                                 WHERE Z.QRCodeID = @qrCode and  
                                       Z.ToWhs collate SQL_Latin1_General_CP850_CI_AS = A.WhsCode and ISNULL(Z.BranchID, @bID) = @bID
                             ),0)
                         ) ReceiptQty
                     FROM QIT_Warehouse_Master AS A  
                     where Locked = 'N' 
                   ) as A 
                ) as B where B.Stock > 0  ";

                _logger.LogInformation(" InprocessQCController : Item Stock Query : {q} ", _Query.ToString());
                QITcon.Open();
                DataTable dtStock = new DataTable();
                oAdptr = new SqlDataAdapter(_Query, QITcon);
                oAdptr.SelectCommand.Parameters.AddWithValue("@bID", payload.BranchID);
                oAdptr.SelectCommand.Parameters.AddWithValue("@qrCode", payload.DetailQRCodeID.Replace(" ", "~"));
                oAdptr.Fill(dtStock);
                QITcon.Close();

                if (dtStock.Rows.Count <= 0)
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Item Stock is not available for the QR : " + payload.DetailQRCodeID.Replace("~", " ") });
                }
                else
                {
                    bool validWhs = dtStock.AsEnumerable().Any(row => row.Field<string>("WhsCode") == _InProcessQCWhs);
                    if (!validWhs)
                        return BadRequest(new
                        {
                            StatusCode = "400",
                            StatusMsg = "Item Stock is not available for the warehouse : " + _InProcessQCWhs
                        });
                }
                #endregion

                #region Query

                dtData = new DataTable();
                _Query = @" 
                SELECT A.* FROM
                (
                    SELECT A.DocEntry ProOrdDocEntry, A.DocNum ProOrdDocNum, A.QRCodeID HeaderQRCodeID, B.QRCodeID DetailQRCodeID, B.RecNo,
                           C.DocEntry RecDocEntry, C.DocNum RecDocNum, C.CardCode, C.CardName,
		                   C.DocDate, D.ItemCode, D.Dscription ItemName, E.Qty RecQty, B.Qty QRQty, 
                           ( SELECT ISNULL(sum(Qty),0) FROM QIT_InProcQC_Detail WHERE QRCodeID = @dQR ) QCQty,          
                           D.Project, F.UomCode, D.WhsCode,
                           E.ToWhs FromWhs, ISNULL(E.ToBinAbsEntry,0) FromBin, 
                           ( SELECT BinCode FROM " + Global.SAP_DB + @".dbo.OBIN WHERE AbsEntry = E.ToBinAbsEntry) FromBinCode
                    FROM QIT_Production_QR_Header A 
                         INNER JOIN QIT_Production_QR_Detail B ON A.HeaderSrNo = B.HeaderSrNo
                         INNER JOIN " + Global.SAP_DB + @".dbo.OIGN C ON C.CANCELED = 'N' AND C.DocEntry = 
                               ( SELECT DISTINCT DocEntry FROM QIT_Trans_ProToReceipt Z1 
                                 INNER JOIN QIT_QRStock_ProToReceipt Z2 ON Z1.TransSeq = Z2.TransSeq 
                                 WHERE Z2.QRCodeID = @dQR 
                               )
                         INNER JOIN " + Global.SAP_DB + @".dbo.IGN1 D ON D.DocEntry = C.DocEntry AND 
                               D.ItemCode collate SQL_Latin1_General_CP850_CI_AS = B.ItemCode  
                         INNER JOIN QIT_ProQRStock_InvTrans E ON E.QRCodeID = B.QRCodeID
                         INNER JOIN QIT_Item_Master F ON F.ItemCode collate SQL_Latin1_General_CP850_CI_AS = D.ItemCode
                    WHERE B.QRCodeID = @dQR AND ISNULL(A.BranchID, @bID) = @bID AND
                          E.TransId = ( SELECT MIN(TransId) FROM QIT_ProQRStock_InvTrans 
                                        WHERE QRCodeID = @dQR and ItemCode = B.ItemCode AND RecNo = B.RecNo 
                                      )
                ) as A
                ";

                #endregion

                _logger.LogInformation(" InprocessQCController : ValidateItemNewFlow() Query : {q} ", _Query.ToString());
                QITcon.Open();
                oAdptr = new SqlDataAdapter(_Query, QITcon);
                oAdptr.SelectCommand.Parameters.AddWithValue("@bID", payload.BranchID);
                oAdptr.SelectCommand.Parameters.AddWithValue("@dQR", payload.DetailQRCodeID.Replace(" ", "~"));
                oAdptr.Fill(dtData);
                QITcon.Close();

                if (dtData.Rows.Count > 0)
                {
                    dynamic arData = JsonConvert.SerializeObject(dtData);
                    obj = JsonConvert.DeserializeObject<List<RECItem>>(arData.ToString().Replace("~", " "));
                    return obj;
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "No data found" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(" Error in InprocessQCController : ValidateItemNewFlow() :: {Error}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
            }
        }


        [HttpPost("InProcessQCNewFlow")]
        public async Task<ActionResult<IEnumerable<string>>> InProcessQCNewFlow(InProcessQCPayloadNewFlow payload)
        {
            SqlConnection QITcon;
            SqlDataAdapter oAdptr;

            Object _QRTransSeqInvTrans = 0;
            object _TransSeq = 0;
            string p_ErrorMsg = string.Empty;
            try
            {
                _logger.LogInformation(" Calling InprocessQCController : InProcessQCNewFlow() ");

                System.Data.DataTable dtQRData = new System.Data.DataTable();
                System.Data.DataTable dtProReceiptItemData = new System.Data.DataTable();
                DataTable dtConfig = new DataTable();
                System.Data.DataTable dtQCData = new System.Data.DataTable();
                string _ToApprovedWhsCode = string.Empty;
                string _ToRejectedWhsCode = string.Empty;
                int _ToApprovedBinAbsEntry = 0;
                int _ToRejectedBinAbsEntry = 0;

                if (((WMS_UI_API.Services.SAPConnectionService)_sapConnectionService).oComp.Connected)
                {
                    #region Validation

                    if (payload.BranchID == 0)
                    {
                        return BadRequest(new
                        {
                            StatusCode = "400",
                            StatusMsg = "Provide Branch"
                        });
                    }

                    if (payload.ReceiptDocEntry == 0)
                    {
                        return BadRequest(new
                        {
                            StatusCode = "400",
                            StatusMsg = "Provide Production Receipt Document Entry"
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
                                    SELECT ISNULL(SUM(A.Qty),0) FROM QIT_Production_QR_Detail A
                                    WHERE A.QRCodeID = @dQR AND ISNULL(A.BranchID, @bID) = @bID
                                ) -
                                (
                                    SELECT ISNULL(SUM(A.Qty),0) QCQty FROM QIT_InProcQC_Detail A   
                                    WHERE A.QRCodeID = @dQR AND ISNULL(A.BranchID, @bID) = @bID
                                ) as PendQty
                            ) as A
                            WHERE A.PendQty = 0  ";
                    _logger.LogInformation(" InprocessQCController : QC already done Query : {q} ", _Query.ToString());
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

                    _Query = @" SELECT A.*, B.ItemMngBy 
                            FROM QIT_Production_QR_Detail A  
                            INNER JOIN QIT_Item_Master B on A.ItemCode = B.ItemCode
                            WHERE A.QRCodeID = @dQR AND ISNULL(A.BranchID, @bID) = @bID  ";
                    _logger.LogInformation(" InprocessQCController : QRCode Query : {q} ", _Query.ToString());
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
                    _logger.LogInformation(" InprocessQCController : Configuration  Query : {q} ", _Query.ToString());
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
                        #region Get Production receipt Detail

                        _Query = @" 
                    SELECT A.DocEntry, A.DocNum, A.CardCode, A.CardName, A.Series, A.DocDate, A.TaxDate, 
                           B.ItemCode, B.Dscription ItemName, B.Project, B.Quantity
                    FROM " + Global.SAP_DB + @".dbo.OIGN A INNER JOIN " + Global.SAP_DB + @".dbo.IGN1 B ON A.DocEntry = B.DocEntry 
                    WHERE A.DocEntry = @rDocEntry and B.ItemCode = @itemCode and -- B.BaseLine = @line and 
                          A.CANCELED = 'N' and ISNULL(A.BPLId, @bID) = @bID ";
                        _logger.LogInformation(" InprocessQCController : Production Receipt Data Query : {q} ", _Query.ToString());
                        QITcon.Open();
                        oAdptr = new SqlDataAdapter(_Query, QITcon);
                        oAdptr.SelectCommand.Parameters.AddWithValue("@bID", payload.BranchID);
                        oAdptr.SelectCommand.Parameters.AddWithValue("@rDocEntry", payload.ReceiptDocEntry);
                        oAdptr.SelectCommand.Parameters.AddWithValue("@itemCode", dtQRData.Rows[0]["ItemCode"].ToString());
                        oAdptr.Fill(dtProReceiptItemData);
                        QITcon.Close();
                        #endregion

                        if (dtProReceiptItemData.Rows.Count > 0)
                        {
                            #region Get TransSeq
                            QITcon = new SqlConnection(_QIT_connection);
                            _Query = @" SELECT ISNULL(max(TransSeq),0) + 1 FROM QIT_InProcQC_Detail A  ";
                            _logger.LogInformation(" InprocessQCController : Get TransSeq Query : {q} ", _Query.ToString());

                            cmd = new SqlCommand(_Query, QITcon);
                            QITcon.Open();
                            _TransSeq = cmd.ExecuteScalar();
                            QITcon.Close();
                            #endregion

                            QITcon = new SqlConnection(_QIT_connection);

                            #region QIT QC Entry - Approve
                            if (payload.ApprovedQty > 0)
                            {
                                _Query = @"
                            INSERT INTO QIT_InProcQC_Detail
                            (   TransId, TransSeq, BranchId, RecDocEntry, RecDocNum, RecSeries, QRCodeID, ItemCode, RecNo,   
                                BatchSerialNo, FromWhs, ToWhs, FromBinAbsEntry, ToBinAbsEntry, Status, Qty, Comment
                            )
                            VALUES 
                            (   (SELECT ISNULL(max(TransId),0) + 1 FROM QIT_InProcQC_Detail A), @transSeq, @bID, @rDocEntry, @rDocNum, 
                                @rSeries, @qr, @itemCode, @recNo, 
                                @batchSerial, @fromWhs, @toWhs, @fromBin, @toBin, @status, @qty, @Comment
                            )
                            ";

                                _logger.LogInformation(" InprocessQCController : InprocessQC Query : {q} ", _Query.ToString());
                                cmd = new SqlCommand(_Query, QITcon);
                                cmd.Parameters.AddWithValue("@transSeq", _TransSeq);
                                cmd.Parameters.AddWithValue("@bID", payload.BranchID);
                                cmd.Parameters.AddWithValue("@rDocEntry", dtProReceiptItemData.Rows[0]["DocEntry"]);
                                cmd.Parameters.AddWithValue("@rDocNum", dtProReceiptItemData.Rows[0]["DocNum"]);
                                cmd.Parameters.AddWithValue("@rSeries", dtProReceiptItemData.Rows[0]["Series"]);
                                cmd.Parameters.AddWithValue("@qr", payload.DetailQRCodeID.Replace(" ", "~"));
                                cmd.Parameters.AddWithValue("@itemCode", dtQRData.Rows[0]["ItemCode"]);
                                cmd.Parameters.AddWithValue("@recNo", dtQRData.Rows[0]["recNo"]);
                                cmd.Parameters.AddWithValue("@batchSerial", dtQRData.Rows[0]["BatchSerialNo"]);
                                cmd.Parameters.AddWithValue("@fromWhs", payload.FromWhs);
                                cmd.Parameters.AddWithValue("@toWhs", _ToApprovedWhsCode);
                                cmd.Parameters.AddWithValue("@fromBin", payload.FromBin);
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
                                    this.DeleteInprocessQCDetail(_TransSeq.ToString());
                                    string msg;
                                    msg = "Error message: " + exApprove.Message.ToString();
                                    _logger.LogInformation(" InprocessQCController : InProcessQC Exception Error : {0} ", msg);
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
                                _Query = @"
                            INSERT INTO QIT_InProcQC_Detail
                            (   TransId, TransSeq, BranchId, RecDocEntry, RecDocNum, RecSeries, QRCodeID, ItemCode, RecNo,   
                                BatchSerialNo, FromWhs, ToWhs, FromBinAbsEntry, ToBinAbsEntry, Status, Qty, Comment
                            )
                            VALUES 
                            (   (SELECT ISNULL(max(TransId),0) + 1 FROM QIT_InProcQC_Detail A), @transSeq, @bID, @rDocEntry, @rDocNum, 
                                @rSeries, @qr, @itemCode, @recNo, 
                                @batchSerial, @fromWhs, @toWhs, @fromBin, @toBin, @status, @qty, @Comment
                            )
                            ";

                                _logger.LogInformation(" InprocessQCController : InProcessQC Query : {q} ", _Query.ToString());
                                cmd = new SqlCommand(_Query, QITcon);
                                cmd.Parameters.AddWithValue("@transSeq", _TransSeq);
                                cmd.Parameters.AddWithValue("@bID", payload.BranchID);
                                cmd.Parameters.AddWithValue("@rDocEntry", dtProReceiptItemData.Rows[0]["DocEntry"]);
                                cmd.Parameters.AddWithValue("@rDocNum", dtProReceiptItemData.Rows[0]["DocNum"]);
                                cmd.Parameters.AddWithValue("@rSeries", dtProReceiptItemData.Rows[0]["Series"]);
                                cmd.Parameters.AddWithValue("@qr", payload.DetailQRCodeID.Replace(" ", "~"));
                                cmd.Parameters.AddWithValue("@itemCode", dtQRData.Rows[0]["ItemCode"]);
                                cmd.Parameters.AddWithValue("@recNo", dtQRData.Rows[0]["GateInNo"]);
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
                                    this.DeleteInprocessQCDetail(_TransSeq.ToString());
                                    string msg;
                                    msg = "Error message: " + exReject.Message.ToString();
                                    _logger.LogInformation(" InprocessQCController : QC Exception Error : {0} ", msg);
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

                            if (1 == 1)
                            {
                                DateTime _docDate = DateTime.Today;
                                int _TotalItemCount = 1;
                                int _SuccessCount = 0;
                                int _FailCount = 0;

                                #region Get QR TransSeq No - Inventory Transfer
                                QITcon = new SqlConnection(_QIT_connection);
                                _Query = @" SELECT ISNULL(max(QRTransSeq),0) + 1 FROM QIT_ProQRStock_InvTrans A  ";
                                _logger.LogInformation(" InprocessQCController : GetQRTransSeqNo(Inv Trans) Query : {q} ", _Query.ToString());

                                cmd = new SqlCommand(_Query, QITcon);
                                QITcon.Open();
                                _QRTransSeqInvTrans = cmd.ExecuteScalar();
                                QITcon.Close();
                                #endregion

                                #region Insert in QR Stock Table

                                if (payload.ApprovedQty > 0)
                                {
                                    QITcon = new SqlConnection(_QIT_connection);
                                    _Query = @"
                                INSERT INTO QIT_ProQRStock_InvTrans 
                                ( BranchID, TransId, TransSeq, QRTransSeq, QRCodeID, RecNo, ItemCode, BatchSerialNo, FromObjType, ToObjType, FromWhs, ToWhs, Qty, FromBinAbsEntry, ToBinAbsEntry
                                ) 
                                VALUES 
                                ( @bID, (SELECT ISNULL(max(TransId),0) + 1 FROM QIT_ProQRStock_InvTrans), 
                                  @transSeq, @qrtransSeq, @qrCodeID, @recNo, @itemCode, @bsNo, @frObjType, @toObjType, 
                                  @fromWhs, @toWhs, @qty, @fromBin, @toBin   
                                )";
                                    _logger.LogInformation("InprocessQCController : QR Stock Table Query : {q} ", _Query.ToString());

                                    cmd = new SqlCommand(_Query, QITcon);
                                    cmd.Parameters.AddWithValue("@bID", payload.BranchID);
                                    cmd.Parameters.AddWithValue("@qrtransSeq", _QRTransSeqInvTrans);
                                    cmd.Parameters.AddWithValue("@transSeq", 0);
                                    cmd.Parameters.AddWithValue("@qrCodeID", payload.DetailQRCodeID.Replace(" ", "~"));
                                    cmd.Parameters.AddWithValue("@recNo", dtQRData.Rows[0]["RecNo"].ToString());
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
                                        this.DeleteInprocessQCDetail(_TransSeq.ToString());
                                        DeleteQRStockITDet(_QRTransSeqInvTrans.ToString());
                                        string msg;
                                        msg = "Error message: " + exInvApprove.Message.ToString();
                                        _logger.LogInformation(" InprocessQCController : QC INV() Exception Error : {0} ", msg);
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
                                INSERT INTO QIT_ProQRStock_InvTrans 
                                ( BranchID, TransId, TransSeq, QRTransSeq, QRCodeID, RecNo, ItemCode, BatchSerialNo, FromObjType, ToObjType, FromWhs, ToWhs, Qty, FromBinAbsEntry, ToBinAbsEntry
                                ) 
                                VALUES 
                                ( @bID, (SELECT ISNULL(max(TransId),0) + 1 FROM QIT_ProQRStock_InvTrans), 
                                  @transSeq, @qrtransSeq, @qrCodeID, @recNo, @itemCode, @bsNo, @frObjType, @toObjType, 
                                  @fromWhs, @toWhs, @qty, @fromBin, @toBin   
                                )";

                                    _logger.LogInformation("InprocessQCController : QR Stock Table Query : {q} ", _Query.ToString());

                                    cmd = new SqlCommand(_Query, QITcon);
                                    cmd.Parameters.AddWithValue("@bID", payload.BranchID);
                                    cmd.Parameters.AddWithValue("@qrtransSeq", _QRTransSeqInvTrans);
                                    cmd.Parameters.AddWithValue("@transSeq", 0);
                                    cmd.Parameters.AddWithValue("@qrCodeID", payload.DetailQRCodeID.Replace(" ", "~"));
                                    cmd.Parameters.AddWithValue("@recNo", dtQRData.Rows[0]["RecNo"].ToString());
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
                                        this.DeleteInprocessQCDetail(_TransSeq.ToString());
                                        DeleteQRStockITDet(_QRTransSeqInvTrans.ToString());
                                        string msg;
                                        msg = "Error message: " + exInvReject.Message.ToString();
                                        _logger.LogInformation(" InprocessQCController : QC INV() Exception Error : {0} ", msg);
                                        return BadRequest(new
                                        {
                                            StatusCode = "400",
                                            IsSaved = "N",
                                            TransSeq = _TransSeq,
                                            StatusMsg = exInvReject.Message.ToString()
                                        });

                                    }
                                }

                                #region IT with APPROVE and REJECT

                                int _Line = 0;
                                StockTransfer oStockTransfer = (StockTransfer)((WMS_UI_API.Services.SAPConnectionService)_sapConnectionService).oComp.GetBusinessObject(BoObjectTypes.oStockTransfer);
                                oStockTransfer.DocObjectCode = BoObjectTypes.oStockTransfer;
                                oStockTransfer.Series = int.Parse(dtConfig.Rows[0]["QCITSeries"].ToString());
                                oStockTransfer.CardCode = dtProReceiptItemData.Rows[0]["CardCode"].ToString();
                                oStockTransfer.Comments = payload.Comment;
                                oStockTransfer.DocDate = _docDate;
                                oStockTransfer.FromWarehouse = payload.FromWhs;
                                oStockTransfer.UserFields.Fields.Item("U_QIT_FromWeb").Value = "Y";

                                if (payload.ApprovedQty > 0)
                                {
                                    oStockTransfer.Lines.ItemCode = dtQRData.Rows[0]["ItemCode"].ToString();
                                    oStockTransfer.Lines.Quantity = payload.ApprovedQty;
                                    oStockTransfer.Lines.ProjectCode = dtProReceiptItemData.Rows[0]["Project"].ToString();
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
                                    oStockTransfer.Lines.ProjectCode = dtProReceiptItemData.Rows[0]["Project"].ToString();
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
                                    this.DeleteInprocessQCDetail(_TransSeq.ToString());
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
                                    _logger.LogInformation(" InprocessQCController : Get IT Data : Query : {q} ", _Query.ToString());
                                    QITcon.Open();
                                    oAdptr = new SqlDataAdapter(_Query, QITcon);
                                    oAdptr.SelectCommand.Parameters.AddWithValue("@docEntry", docEntry);
                                    oAdptr.Fill(dtIT);
                                    QITcon.Close();
                                    #endregion

                                    #region Update Transaction Table

                                    _Query = @" 
                                UPDATE QIT_InProcQC_Detail 
                                SET DocEntry = @docEntry, DocNum = @docNum 
                                WHERE TransSeq = @code";
                                    _logger.LogInformation(" InprocessQCController : Update Transaction Table Query : {q} ", _Query.ToString());

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
                                        this.DeleteInprocessQCDetail(_TransSeq.ToString());
                                        DeleteQRStockITDet(_QRTransSeqInvTrans.ToString());
                                        return BadRequest(new
                                        {
                                            StatusCode = "400",
                                            IsSaved = "N",
                                            TransSeq = _TransSeq,
                                            StatusMsg = "QC failed"
                                        });
                                    }
                                    #endregion
                                }
                            }
                            else
                            {
                                this.DeleteInprocessQCDetail(_TransSeq.ToString());
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

                            #endregion

                            #endregion

                        }
                        else
                        {
                            this.DeleteInprocessQCDetail(_TransSeq.ToString());
                            DeleteQRStockITDet(_QRTransSeqInvTrans.ToString());
                            return BadRequest(new { StatusCode = "400", StatusMsg = "GRPO Details not found" });
                        }
                    }
                    else
                    {
                        this.DeleteInprocessQCDetail(_TransSeq.ToString());
                        DeleteQRStockITDet(_QRTransSeqInvTrans.ToString());
                        return BadRequest(new { StatusCode = "400", StatusMsg = "QR Code does not exist : " + payload.DetailQRCodeID.Replace("~", " ") });
                    }
                }
                else
                {
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
                this.DeleteInprocessQCDetail(_TransSeq.ToString());
                DeleteQRStockITDet(_QRTransSeqInvTrans.ToString());
                _logger.LogError(" Error in InprocessQCController : QC() :: {Error}", ex.ToString());
                return BadRequest(new
                {
                    StatusCode = "400",
                    StatusMsg = ex.ToString()
                });
            }
        }

        private bool DeleteQRStockITDet(string _QRTransSeq)
        {
            string _IsSaved = "N";
            try
            {
                _logger.LogInformation(" Calling InprocessQCController : DeleteQRStockITDet() ");

                string p_ErrorMsg = string.Empty;

                SqlConnection con = new SqlConnection(_QIT_connection);
                _Query = @" DELETE FROM QIT_ProQRStock_InvTrans WHERE QRTransSeq = @qrtransSeq";
                _logger.LogInformation(" InprocessQCController : DeleteQRStockITDet Query : {q} ", _Query.ToString());

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
                _logger.LogError("Error in InprocessQCController : DeleteQRStockITDet() :: {Error}", ex.ToString());
                return false;
            }
        }

    }
}
