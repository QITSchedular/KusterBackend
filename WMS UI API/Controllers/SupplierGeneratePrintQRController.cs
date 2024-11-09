using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Data.SqlClient;
using WMS_UI_API.Common;
using WMS_UI_API.Models;
using System.Linq;
using System.Data;
using Newtonsoft.Json;

namespace WMS_UI_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SupplierGeneratePrintQRController : ControllerBase
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

        public IConfiguration Configuration { get; }
        private readonly ILogger<SupplierGeneratePrintQRController> _logger;

        public SupplierGeneratePrintQRController(IConfiguration configuration, ILogger<SupplierGeneratePrintQRController> logger)
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
                _logger.LogError(" Error in SupplierGeneratePrintQRController :: {Error}" + ex.ToString());
            }
        }


        [HttpPost("GetItemsByVendorGateInNo")]
        public IActionResult GetItemsByVendorGateInNo(SPayload payload)
        {
            SqlConnection QITcon;
            SqlDataAdapter oAdptr;
            try
            {
                _logger.LogInformation(" Calling SupplierGeneratePrintQRController : GetItemsByVendorGateInNo() ");

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
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Provide Vendor GateIn No" });
                }
                if (payload.CardCode.Trim().Length <= 0 || payload.CardCode == null)
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Provide Vendor Code" });
                }

                #endregion

                #region Check for valid Vendor Gate IN No

                System.Data.DataTable dtVendorGateIn = new System.Data.DataTable();
                QITcon = new SqlConnection(_QIT_connection);
                _Query = @" SELECT * FROM QIT_GateIN WHERE GateInNo = @vGateIn and VendorGateIN = 'Y' ";
                QITcon.Open();
                oAdptr = new SqlDataAdapter(_Query, QITcon);
                oAdptr.SelectCommand.Parameters.AddWithValue("@vGateIn", payload.GateInNo);
                oAdptr.Fill(dtVendorGateIn);
                QITcon.Close();

                if (dtVendorGateIn.Rows.Count <= 0)
                    return BadRequest(new
                    {
                        StatusCode = "400",
                        StatusMsg = "No such Vendor Gate IN exist."
                    });

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
                    WHERE ISNULL(AA.BranchID, 1) = 1 and AA.ObjType = '22' AND AA.GateInNo = @vGateInNo AND 
                          AA.Canceled = 'N' and AA.VendorGateIN = 'Y'
                ) C ON A.ObjType COLLATE SQL_Latin1_General_CP1_CI_AS = C.ObjType AND 
                        A.DocEntry = C.DocEntry AND ISNULL(A.BPLId,1) = C.BranchID AND 
                        B.ItemCode COLLATE SQL_Latin1_General_CP1_CI_AS = C.ItemCode AND 
                        B.LineNum = C.LineNum
                INNER JOIN 
                ( 
                    SELECT A.ItemCode, A.ItemName, QRMngBy, ItemMngBy  
                    FROM QIT_Item_Master A 
                ) D ON B.ItemCode COLLATE SQL_Latin1_General_CP1_CI_AS  = D.ItemCode 
                WHERE ISNULL(A.BPLId, @bId) = @bId AND A.Canceled = 'N' and A.CardCode = @cardCode
                ORDER BY A.DocEntry,C.GateInNo , B.LineNum
                ";

                #endregion

                _logger.LogInformation(" SupplierGeneratePrintQRController : GetItemsByVendorGateInNo() Query : {q} ", _Query.ToString());
                QITcon.Open();
                oAdptr = new SqlDataAdapter(_Query, QITcon);
                oAdptr.SelectCommand.Parameters.AddWithValue("@bId", payload.BranchID);
                oAdptr.SelectCommand.Parameters.AddWithValue("@vGateInNo", payload.GateInNo);
                oAdptr.SelectCommand.Parameters.AddWithValue("@cardCode", payload.CardCode);
                oAdptr.Fill(dtData);
                QITcon.Close();

                if (dtData.Rows.Count > 0)
                {
                    var result = dtData.AsEnumerable()
                    .GroupBy(row => new { GateInNo = row.Field<int>("GateInNo"), Series = row.Field<int>("Series"), RecDate = row.Field<string>("RecDate") })
                    .Select(g => new GP_GateInDetails
                    {
                        BranchID = payload.BranchID,
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
                _logger.LogError(" Error in SupplierGeneratePrintQRController : GetItemsByVendorGateInNo() :: {Error}", ex.ToString());
                return BadRequest(new
                {
                    StatusCode = "400",
                    StatusMsg = ex.ToString()
                });
            }
        }


        #region Purchase Order : QR Generation

        [HttpPost("IsHeaderQRExist")]
        public ActionResult<bool> IsHeaderQRExist(CheckHeaderPO payload)
        {
            try
            {
                System.Data.DataTable dtData = new System.Data.DataTable();
                _logger.LogInformation(" Calling SupplierGeneratePrintQRController : IsHeaderQRExist() ");

                QITcon = new SqlConnection(_QIT_connection);

                _Query = @" 
                SELECT A.* from QIT_QR_Header A inner join QIT_QR_Detail B on A.HeaderSrNo = B.HeaderSrNo
                WHERE A.BranchID = @bId AND  
                        A.DocEntry = @docEntry AND
                        A.DocNum = @docNum AND
                        A.Series = @series AND
                        A.ObjType = @objType AND 
                        B.GateInNo = @gateInNo
                ";

                _logger.LogInformation(" SupplierGeneratePrintQRController : IsHeaderQRExist() Query : {q} ", _Query.ToString());
                oAdptr = new SqlDataAdapter(_Query, QITcon);

                oAdptr.SelectCommand.Parameters.AddWithValue("@bId", payload.BranchID);
                //oAdptr.SelectCommand.Parameters.AddWithValue("@CardCode", payload.CardCode);
                oAdptr.SelectCommand.Parameters.AddWithValue("@docEntry", payload.DocEntry);
                oAdptr.SelectCommand.Parameters.AddWithValue("@docNum", payload.DocNum);
                oAdptr.SelectCommand.Parameters.AddWithValue("@series", payload.Series);
                oAdptr.SelectCommand.Parameters.AddWithValue("@objType", payload.ObjType);
                oAdptr.SelectCommand.Parameters.AddWithValue("@gateInNo", payload.GateInNo);
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
                _logger.LogError(" Error in SupplierGeneratePrintQRController : IsHeaderQRExist() :: {Error}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
            }
        }


        [HttpPost("GetHeaderQR")]
        public ActionResult<string> GetHeaderQR(CheckHeaderPO payload)
        {

            try
            {
                _logger.LogInformation(" Calling SupplierGeneratePrintQRController : GetHeaderQR() ");

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

                _logger.LogInformation(" SupplierGeneratePrintQRController : GetHeaderQR() Query : {q} ", _Query.ToString());

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
                _logger.LogError(" Error in SupplierGeneratePrintQRController : GetHeaderQR() :: {Error}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
            }
        }


        [HttpPost("SaveHeaderQR")]
        public IActionResult SaveHeaderQR(SaveHeaderQR payload)
        {
            string _IsSaved = "N";
            try
            {
                _logger.LogInformation(" Calling SupplierGeneratePrintQRController : SaveHeaderQR() ");

                if (payload != null)
                {
                    if (payload.QRCodeID.Replace(" ", "~").Split('~')[2].ToString() != payload.IncNo)
                    {
                        return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Inc No must be " + payload.QRCodeID.Replace(" ", "~").Split('~')[2].ToString() });
                    }

                    QITcon = new SqlConnection(_QIT_connection);
                    _Query = @" 
                    INSERT INTO QIT_QR_Header
                    (HeaderSrNo, BranchID, QRCodeID, DocEntry, DocNum, Series, DocDate, ObjType, Inc_No, EntryDate)
                    VALUES( (select ISNULL(max(HeaderSrNo),0) + 1 from QIT_QR_Header), @bId, @qr, @docEntry, @docNum, @series, 
                        @docDate, @objType, @incNo, @entryDate)
                              ";
                    _logger.LogInformation(" SupplierGeneratePrintQRController : SaveHeaderQR() Query : {q} ", _Query.ToString());

                    cmd = new SqlCommand(_Query, QITcon);
                    cmd.Parameters.AddWithValue("@bID", payload.BranchID);
                    cmd.Parameters.AddWithValue("@qr", payload.QRCodeID.Replace(" ", "~"));
                    //cmd.Parameters.AddWithValue("@cardCode", payload.CardCode);
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
                _logger.LogError(" Error in SupplierGeneratePrintQRController : SaveHeaderQR() :: {Error}", ex.ToString());
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


        [HttpPost("GetHeaderDataQR")]
        public ActionResult<string> GetHeaderDataQR(CheckHeaderPOQR payload)
        {
            try
            {
                _logger.LogInformation(" Calling SupplierGeneratePrintQRController : GetHeaderDataQR() ");

                DataTable dtData = new DataTable();
                QITcon = new SqlConnection(_QIT_connection);

                _Query = @" 
                SELECT DISTINCT A.QRCodeID
                FROM  QIT_QR_Header A  
                INNER JOIN QIT_QR_Detail B ON A.HeaderSrNo = B.HeaderSrNo
                WHERE ISNULL(A.BranchID, @bid) = @bid AND A.DocEntry = @docEntry and  
                      B.VendorGateIN = 'Y' and 
                      A.Series = @series and A.ObjType = @objType AND B.GateInNo = @gateInNo ";

                _logger.LogInformation(" SupplierGeneratePrintQRController : GetHeaderDataQR() Query : {q} ", _Query.ToString());
                QITcon.Open();
                oAdptr = new SqlDataAdapter(_Query, QITcon);
                oAdptr.SelectCommand.Parameters.AddWithValue("@bid", payload.BranchID);
                oAdptr.SelectCommand.Parameters.AddWithValue("@docEntry", payload.DocEntry);
                oAdptr.SelectCommand.Parameters.AddWithValue("@series", payload.Series);
                oAdptr.SelectCommand.Parameters.AddWithValue("@objType", payload.ObjType);
                oAdptr.SelectCommand.Parameters.AddWithValue("@gateInNo", payload.GateInNo);

                oAdptr.Fill(dtData);
                QITcon.Close();

                if (dtData.Rows.Count > 0)
                {
                    return dtData.Rows[0][0].ToString().Replace("~", " ");
                }
                else
                {
                    return string.Empty;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(" Error in SupplierGeneratePrintQRController : GetHeaderDataQR() :: {Error}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
            }
        }

        [HttpPost("GetDetailQR")]
        public ActionResult<string> GetDetailQR(string HeaderQR)
        {

            try
            {
                _logger.LogInformation(" Calling SupplierGeneratePrintQRController : GetDetailQR() ");
                QITcon = new SqlConnection(_QIT_connection);
                DataTable dtData = new DataTable();

                _Query = @" SELECT * FROM QIT_QR_Header WHERE QRCodeID = @headerQR  ";
                _logger.LogInformation(" SupplierGeneratePrintQRController : GetDetailQR() Query : {q} ", _Query.ToString());

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
                    _logger.LogInformation(" SupplierGeneratePrintQRController : GetDetailQR() Query : {q} ", _Query.ToString());

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
                _logger.LogError(" Error in SupplierGeneratePrintQRController : GetDetailQR() :: {Error}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
            }
        }


        [HttpPost("IsDetailQRExist")]
        public ActionResult<bool> IsDetailQRExist(CheckDetailPO payload)
        {
            try
            {
                System.Data.DataTable dtData = new System.Data.DataTable();
                _logger.LogInformation(" Calling SupplierGeneratePrintQRController : IsDetailQRExist() ");

                QITcon = new SqlConnection(_QIT_connection);

                _Query = @" SELECT A.* from QIT_QR_Detail A inner join QIT_QR_Header B on A.HeaderSrNo = B.HeaderSrNo
                            WHERE B.BranchID = @bId AND 
                                  B.DocEntry = @docEntry AND
                                  B.DocNum = @docNum AND
                                  B.Series = @series AND
                                  B.ObjType = @objType AND 
                                  A.ItemCode = @iCode AND A.GateInNo = @gateInNo AND A.LineNum = @line and A.VendorGateIN = 'Y'
                         ";

                _logger.LogInformation(" SupplierGeneratePrintQRController : IsDetailQRExist() Query : {q} ", _Query.ToString());
                oAdptr = new SqlDataAdapter(_Query, QITcon);

                oAdptr.SelectCommand.Parameters.AddWithValue("@bId", payload.BranchID);
                //oAdptr.SelectCommand.Parameters.AddWithValue("@cardCode", payload.CardCode);
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
                _logger.LogError(" Error in SupplierGeneratePrintQRController : IsDetailQRExist() :: {Error}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
            }
        }


        [HttpPost("SaveDetailQR")]
        public IActionResult SaveDetailQR(SaveDetailQR payload)
        {
            string _IsSaved = "N";
            try
            {
                _logger.LogInformation(" Calling SupplierGeneratePrintQRController : SaveDetailQR() ");

                if (payload != null)
                {
                    if (payload.GateInNo <= 0)
                        return BadRequest(new { StatusCode = "400", IsExist = "N", StatusMsg = "Provide GateIn No" });

                    QITcon = new SqlConnection(_QIT_connection);
                    DataTable dtData = new DataTable();

                    _Query = @" SELECT * FROM QIT_QR_Header WHERE QRCodeID = @headerQR ";
                    _logger.LogInformation(" SupplierGeneratePrintQRController : DetailIncNo() Query : {q} ", _Query.ToString());

                    oAdptr = new SqlDataAdapter(_Query, QITcon);
                    oAdptr.SelectCommand.Parameters.AddWithValue("@headerQR", payload.HeaderQRCodeID.Replace(" ", "~"));
                    QITcon.Open();
                    oAdptr.Fill(dtData);
                    QITcon.Close();

                    if (dtData.Rows.Count > 0)
                    {
                        _Query = @" 
                        INSERT INTO QIT_QR_Detail  
                        (   DetailSrNo, HeaderSrNo, GateInNo, BranchID, QRCodeID, Inc_No, ItemCode, LineNum, QRMngBy,  
                            BatchSerialNo, Qty, Remark, VendorGateIN, EntryDate 
                        )  
                        VALUES  
                        (  
                            (select ISNULL(max(DetailSrNo),0) + 1 from QIT_QR_Detail),  
                            @hSrNo, @gateInNo,  @bId, @qr, @incNo, @iCode, @line, @qrMngBy,  
                            case when '" + payload.QRMngBy + @"' = 'N' then '-' else ( 
                                 case when (select count(*) from QIT_QR_Detail 
                                            where YEAR(EntryDate) = YEAR(GETDATE()) and MONTH(EntryDate) = MONTH(GETDATE())) > 0 then (
                                            select top 1 RIGHT(YEAR(GetDate()),2) + RIGHT('00' + CONVERT(VARCHAR,MONTH(GETDATE()),2), 2) +  
                                                   case when BatchSerialNo is null then RIGHT('000000' + CONVERT(VARCHAR, 1), 6)  
                                                   else RIGHT('000000' + CONVERT(VARCHAR,  (right(left(BatchSerialNo,10), 6) + 1)), 6)  end +  
                                        '" + payload.QRMngBy + "' + " + @"'PO' 
                                            from QIT_QR_Detail  
                                            where YEAR(EntryDate) = YEAR(GETDATE()) and MONTH(EntryDate) = MONTH(GETDATE()) 
                                            order by DetailSrNo desc ) 
                                            else 
                                            RIGHT(YEAR(GetDate()),2) + RIGHT('00' + CONVERT(VARCHAR,MONTH(GETDATE()),2), 2) + 
                                            RIGHT('000000' + CONVERT(VARCHAR, 1), 6) +  
                                        '" + payload.QRMngBy + "' + " + @"'PO' 
                                  end   
                                        ) end ,  
                                        @qty, @remark, 'Y', @entryDate  
                                   )";

                        _logger.LogInformation(" SupplierGeneratePrintQRController : SaveDetailQR() Query : {q} ", _Query.ToString());

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
                _logger.LogError(" Error in SupplierGeneratePrintQRController : SaveDetailQR() :: {Error}", ex.ToString());
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
        public async Task<ActionResult<IEnumerable<SaveDetailQR>>> DetailDataQR(string CardCode, CheckDetailPO payload)
        {
            try
            {
                _logger.LogInformation(" Calling SupplierGeneratePrintQRController : DetailDataQR() ");

                DataTable dtData = new DataTable();
                QITcon = new SqlConnection(_QIT_connection);

                _Query = @" 
                SELECT  A.BranchID BranchID, B.DocNum, G.CardCode, B.QRCodeID HeaderQRCodeID, A.QRCodeID DetailQRCodeID, A.GateInNo,
                        A.Inc_No IncNo, A.ItemCode ItemCode, E.ItemName, A.LineNum, A.QRMngBy QRMngBy, A.Qty Qty, 
                        A.Remark, F.Project, A.BatchSerialNo, D.BinCode DefaultBin
                FROM QIT_QR_Detail A 
                INNER JOIN QIT_QR_Header B on A.HeaderSrNo = B.HeaderSrNo
                INNER JOIN " + Global.SAP_DB + @".dbo.OPOR G ON G.DocEntry = B.DocEntry
                INNER JOIN QIT_GateIN F ON F.GateInNo = A.GateInNo and F.ItemCode = A.ItemCode and 
                           F.LineNum = A.LineNum and F.DocEntry = B.DocEntry
                LEFT JOIN " + Global.SAP_DB + @".dbo.OITW C ON A.ItemCode collate SQL_Latin1_General_CP850_CI_AS = C.ItemCode AND 
                          C.DftBinAbs is not null
	            LEFT JOIN " + Global.SAP_DB + @".dbo.OBIN D ON D.AbsEntry = C.DftBinAbs
                LEFT JOIN " + Global.SAP_DB + @".dbo.OITM E ON E.ItemCode collate SQL_Latin1_General_CP1_CI_AS = A.ItemCode
                WHERE ISNULL(A.BranchID, @bid) = @bid AND B.DocEntry = @docEntry and B.DocNum = @docNum and 
                      A.VendorGateIN = 'Y' AND F.VendorGateIN = 'Y' AND G.CardCode = @cardCode and 
                      B.Series = @series and B.ObjType = @objType and A.ItemCode = @iCode and A.LineNum = @line and A.GateInNo = @gateInNo ";

                _logger.LogInformation(" SupplierGeneratePrintQRController : DetailDataQR() Query : {q} ", _Query.ToString());
                QITcon.Open();
                oAdptr = new SqlDataAdapter(_Query, QITcon);
                oAdptr.SelectCommand.Parameters.AddWithValue("@bid", payload.BranchID);
                oAdptr.SelectCommand.Parameters.AddWithValue("@docEntry", payload.DocEntry);
                oAdptr.SelectCommand.Parameters.AddWithValue("@docNum", payload.DocNum);
                oAdptr.SelectCommand.Parameters.AddWithValue("@cardCode", CardCode);
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
                _logger.LogError(" Error in SupplierGeneratePrintQRController : DetailDataQR() :: {Error}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
            }
        }


        [HttpPost("GetAllDetailDataQR")]
        public async Task<ActionResult<IEnumerable<SaveDetailQR>>> GetAllDetailDataQR(CheckAllDetailPO payload)
        {
            try
            {
                _logger.LogInformation(" Calling SupplierGeneratePrintQRController : GetAllDetailDataQR() ");

                DataTable dtData = new DataTable();
                QITcon = new SqlConnection(_QIT_connection);

                _Query = @" 
                SELECT  A.BranchID BranchID, B.DocNum, G.CardCode, B.QRCodeID HeaderQRCodeID, A.QRCodeID DetailQRCodeID, A.GateInNo, 
                        A.Inc_No IncNo, A.ItemCode ItemCode, E.ItemName, A.LineNum, A.QRMngBy QRMngBy, 
                        A.Qty Qty, A.Remark, F.Project, A.BatchSerialNo, D.BinCode DefaultBin
                FROM QIT_QR_Detail A 
                INNER JOIN QIT_QR_Header B on A.HeaderSrNo = B.HeaderSrNo
                INNER JOIN " + Global.SAP_DB + @".dbo.OPOR G ON G.DocEntry = B.DocEntry
                INNER JOIN QIT_GateIN F ON F.GateInNo = A.GateInNo and F.ItemCode = A.ItemCode and 
                           F.LineNum = A.LineNum and F.DocEntry = B.DocEntry
                LEFT JOIN " + Global.SAP_DB + @".dbo.OITW C ON A.ItemCode collate SQL_Latin1_General_CP850_CI_AS = C.ItemCode AND 
                          C.DftBinAbs is not null
	            LEFT JOIN " + Global.SAP_DB + @".dbo.OBIN D ON D.AbsEntry = C.DftBinAbs
                LEFT JOIN " + Global.SAP_DB + @".dbo.OITM E ON E.ItemCode collate SQL_Latin1_General_CP1_CI_AS = A.ItemCode
                WHERE ISNULL(A.BranchID, @bid) = @bid and A.GateInNo = @gateInNo and A.VendorGateIN = 'Y' ";

                _logger.LogInformation(" SupplierGeneratePrintQRController : GetAllDetailDataQR() Query : {q} ", _Query.ToString());
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
                _logger.LogError(" Error in SupplierGeneratePrintQRController : GetAllDetailDataQR() :: {Error}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
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
