//using Microsoft.AspNetCore.Http;
//using Microsoft.AspNetCore.Mvc;
//using Newtonsoft.Json;
//using SAPbobsCOM;
//using SAPbouiCOM;
//using System.Data;
//using System.Data.SqlClient;
//using System.Numerics;
//using WMS_UI_API.Common;
//using WMS_UI_API.Models;
//using static System.Net.Mime.MediaTypeNames;

//namespace WMS_UI_API.Controllers
//{
//    [Route("api/[controller]")]
//    [ApiController]
//    public class PickListController : ControllerBase
//    {
//        private string _ApplicationApiKey = string.Empty;
//        private string _connection = string.Empty;
//        private string _QIT_connection = string.Empty;
//        private string _QIT_DB = string.Empty;
//        private string _Query = string.Empty;
//        private SqlCommand cmd;
//        public Global objGlobal;

//        public IConfiguration Configuration { get; }
//        private readonly ILogger<PickListController> _logger;

//        public PickListController(IConfiguration configuration, ILogger<PickListController> logger)
//        {
//            if (objGlobal == null)
//                objGlobal = new Global();
//            _logger = logger;
//            try
//            {
//                Configuration = configuration;
//                _ApplicationApiKey = Configuration["connectApp:ServiceApiKey"];
//                _connection = Configuration["connectApp:ConnString"];
//                _QIT_connection = Configuration["connectApp:QITConnString"];

//                _QIT_DB = Configuration["QITDB"];
//                Global.QIT_DB = _QIT_DB;
//                Global.SAP_DB = Configuration["CompanyDB"];

//                objGlobal.gServer = Configuration["Server"];
//                objGlobal.gSqlVersion = Configuration["SQLVersion"];
//                objGlobal.gCompanyDB = Configuration["CompanyDB"];
//                objGlobal.gLicenseServer = Configuration["LicenseServer"];
//                objGlobal.gSAPUserName = Configuration["SAPUserName"];
//                objGlobal.gSAPPassword = Configuration["SAPPassword"];
//                objGlobal.gDBUserName = Configuration["DBUserName"];
//                objGlobal.gDBPassword = Configuration["DbPassword"];

//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(" Error in PickListController :: {Error}" + ex.ToString());
//            }
//        }


//        [HttpGet("GetSalesOrderList")]
//        public async Task<ActionResult<IEnumerable<SalesOrderList>>> GetSalesOrderList()
//        {
//            SqlConnection SAPcon;
//            SqlDataAdapter oAdptr;
//            try
//            {
//                _logger.LogInformation(" Calling PickListController : GetSalesOrderList() ");
//                List<SalesOrderList> obj = new List<SalesOrderList>();
//                System.Data.DataTable dtData = new System.Data.DataTable();
//                SAPcon = new SqlConnection(_connection);

//                _Query = @" 
//                SELECT A.DocEntry, A.DocNum, A.Series, A.DocDate, A.CardCode, A.CardName, A.NumAtCard, A.Comments 
//                FROM ORDR A
//                WHERE A.CANCELED = 'N' and A.DocStatus = 'O' and A.DocType = 'I'  
//                ORDER BY A.DocEntry
//                ";

//                _logger.LogInformation(" PickListController : GetSalesOrderList() Query : {q} ", _Query.ToString());
//                SAPcon.Open();
//                oAdptr = new SqlDataAdapter(_Query, SAPcon);
//                oAdptr.Fill(dtData);
//                SAPcon.Close();

//                if (dtData.Rows.Count > 0)
//                {
//                    dynamic arData = JsonConvert.SerializeObject(dtData);
//                    obj = JsonConvert.DeserializeObject<List<SalesOrderList>>(arData.ToString());
//                    return obj;
//                }
//                else
//                {
//                    return BadRequest(new { StatusCode = "400", StatusMsg = "No data found" });
//                }
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(" Error in PickListController : GetSalesOrderList() :: {Error}", ex.ToString());
//                return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
//            }
//        }


//        [HttpPost("ValidateItemQR")]
//        public async Task<ActionResult<IEnumerable<PickListValidItem>>> ValidateItemQR(SOItemValidateCls payload)
//        {
//            SqlConnection QITcon;
//            SqlDataAdapter oAdptr;
//            try
//            {
//                _logger.LogInformation(" Calling PickListController : ValidateItemQR() ");
//                List<PickListValidItem> obj = new List<PickListValidItem>();
//                System.Data.DataTable dtStock = new System.Data.DataTable();
//                System.Data.DataTable dtData = new System.Data.DataTable();

//                QITcon = new SqlConnection(_QIT_connection);

//                #region Get Item QR Stock by Warehouse
//                _Query = @"  SELECT * FROM (
//                     SELECT A.WhsCode, A.WhsName, 
//                            CASE WHEN A.POtoGRPO > 0 THEN (POtoGRPO + InQty - OutQty - IssueQty - DeliverQty + ReceiptQty) ELSE (InQty - OutQty - IssueQty - DeliverQty + ReceiptQty) END Stock
//                     FROM 
//                     (
//                         SELECT A.WhsCode, A.WhsName,
//                         (
//                             ISNULL((	
//                                 SELECT sum(Z.Qty) POtoGRPOQty FROM " + Global.QIT_DB + @".dbo.[QIT_QRStock_InvTrans] Z
//                                 WHERE Z.QRCodeID = @qrCode and Z.FromObjType = '22' and Z.ToObjType = '67' and Z.ToWhs = A.WhsCode and ISNULL(Z.BranchID, @bID) = @bID
//                             ),0)
//                         ) POtoGRPO,
//                         (ISNULL((
//                                 SELECT sum(Z.Qty) InQty FROM " + Global.QIT_DB + @".dbo.[QIT_QRStock_InvTrans] Z
//                                 WHERE Z.QRCodeID = @qrCode and (Z.FromObjType <> '22') AND 
//                                       Z.ToWhs collate SQL_Latin1_General_CP850_CI_AS = A.WhsCode and ISNULL(Z.BranchID, @bID) =@bID
//                             ),0) 
//                         ) InQty,
//                         (
//                             ISNULL((
//                                 SELECT sum(Z.Qty) OutQty from " + Global.QIT_DB + @".dbo.[QIT_QRStock_InvTrans] Z
//                                 WHERE Z.QRCodeID = @qrCode and (Z.FromObjType <> '22') and 
//                                       Z.FromWhs collate SQL_Latin1_General_CP850_CI_AS = A.WhsCode and ISNULL(Z.BranchID, @bID) = @bID
//                             ),0)
//                         ) OutQty,
//                         (
//                             ISNULL((
//                                 SELECT sum(Z.Qty) IssueQty from " + Global.QIT_DB + @".dbo.QIT_QRStock_ProToIssue Z
//                                 WHERE Z.QRCodeID = @qrCode and  
//                                       Z.FromWhs collate SQL_Latin1_General_CP850_CI_AS = A.WhsCode and ISNULL(Z.BranchID, @bID) = @bID
//                             ),0)
//                         ) IssueQty,
//                         (
//                             ISNULL((
//                                 SELECT sum(Z.Qty) DeliverQty from " + Global.QIT_DB + @".dbo.QIT_QRStock_PickListToDelivery Z
//                                 WHERE Z.QRCodeID = @qrCode and  
//                                       Z.ToWhs collate SQL_Latin1_General_CP850_CI_AS = A.WhsCode and ISNULL(Z.BranchID, @bID) = @bID
//                             ),0)
//                         ) DeliverQty,
//                         (
//                             ISNULL((
//                                 SELECT sum(Z.Qty) ReceiptQty from " + Global.QIT_DB + @".dbo.QIT_QRStock_ProToReceipt Z
//                                 WHERE Z.QRCodeID = @qrCode and  
//                                       Z.ToWhs collate SQL_Latin1_General_CP850_CI_AS = A.WhsCode and ISNULL(Z.BranchID, @bID) = @bID
//                             ),0)
//                         ) ReceiptQty
//                     FROM QIT_Warehouse_Master AS A  
//                     where Locked = 'N' 
//                     ) as A ) as B where B.Stock> 0 ";
//                _logger.LogInformation(" PickListController : Item Stock Query : {q} ", _Query.ToString());
//                QITcon.Open();
//                oAdptr = new SqlDataAdapter(_Query, QITcon);
//                oAdptr.SelectCommand.Parameters.AddWithValue("@bID", payload.BranchID);
//                oAdptr.SelectCommand.Parameters.AddWithValue("@qrCode", payload.DetailQRCodeID.Replace(" ", "~"));
//                oAdptr.Fill(dtStock);
//                QITcon.Close();

//                if (dtStock.Rows.Count <= 0)
//                {
//                    return BadRequest(new { StatusCode = "400", StatusMsg = "Item Stock is not available for the QR : " + payload.DetailQRCodeID.Replace(" ", "~") });
//                }
//                else
//                {
//                    bool containsVD_FG = dtStock.AsEnumerable().Any(row => row.Field<string>("WhsCode") == "VD-FG");
//                    if (!containsVD_FG)
//                        return BadRequest(new { StatusCode = "400", StatusMsg = "Item must be in VD-FG warehouse" });

//                }
//                #endregion

//                var quantityForVD_FG = dtStock.AsEnumerable()
//                                       .Where(row => row.Field<string>("WhsCode") == "VD-FG")
//                                       .Select(row => row.Field<decimal>("Stock"))
//                                       .FirstOrDefault(); // Use FirstOrDefault to get the first matching row or default value if not found

//                _Query = @" 
//                 SELECT DocEntry, DocNum, LineNum, CardCode, CardName, ItemCode, ItemName, ItemMngBy, WhsCode, QRCodeID, BatchSerialNo, 
//                        GateRecNo, Project, UomCode,
//	                    OrderedQty, QRQty, AvailQtyInWhs, case when A.CR  > QRQty then  QRQty else CR end canReleaseQty 
//                FROM 
//                (
//	                SELECT A.*, CASE WHEN A.CanRelease > AvailQtyInWhs THEN AvailQtyInWhs ELSE CanRelease END CR 
//	                FROM 
//	                (
//                        SELECT A.DocEntry, A.DocNum, B.LineNum, A.CardCode, A.CardName, B.ItemCode, B.Dscription ItemName, E.ItemMngBy, 
//				               D.QRCodeID, D.BatchSerialNo, D.GateInNo GateRecNo, 'VD-FG' WhsCode, A.Project, E.UomCode, D.Qty QRQty,
//				               B.Quantity OrderedQty,
//				               " + quantityForVD_FG + @" AvailQtyInWhs, 
//                               B.Quantity - isnull(( SELECT SUM(RelQtty) FROM " + Global.SAP_DB + @".dbo.PKL1 WHERE OrderEntry = A.DocEntry ),0) CanRelease 
//		                FROM " + Global.SAP_DB + @".dbo.ORDR A
//				                INNER JOIN " + Global.SAP_DB + @".dbo.RDR1 B ON A.DocEntry = B.DocEntry
//				                INNER JOIN QIT_QR_Detail D ON D.QRCodeID = @dQR and B.ItemCode collate SQL_Latin1_General_CP1_CI_AS = D.ItemCode
//				                INNER JOIN QIT_Item_Master E ON E.ItemCode collate SQL_Latin1_General_CP850_CI_AS = B.ItemCode
//		                WHERE A.DocEntry = @docEntry AND B.LineStatus = 'O' AND A.DocStatus = 'O' AND ISNULL(D.BranchId, @bId) = @bId AND
//                              EXISTS ( SELECT 'Y' FROM QIT_QRStock_InvTrans C where C.QRCodeID = D.QRCodeID ) 
//                    ) as A
//                ) as A
//                    ";

//                _logger.LogInformation(" PickListController : ValidateItemQR() Query : {q} ", _Query.ToString());
//                QITcon.Open();
//                oAdptr = new SqlDataAdapter(_Query, QITcon);
//                oAdptr.SelectCommand.Parameters.AddWithValue("@bId", payload.BranchID);
//                oAdptr.SelectCommand.Parameters.AddWithValue("@docEntry", payload.SODocEntry);
//                oAdptr.SelectCommand.Parameters.AddWithValue("@dQR", payload.DetailQRCodeID.Replace(" ", "~"));
//                oAdptr.Fill(dtData);
//                QITcon.Close();

//                if (dtData.Rows.Count > 0)
//                {
//                    dynamic arData = JsonConvert.SerializeObject(dtData);
//                    obj = JsonConvert.DeserializeObject<List<PickListValidItem>>(arData.ToString().Replace("~", " "));
//                    return obj;
//                }
//                else
//                {
//                    return BadRequest(new { StatusCode = "400", StatusMsg = "No data found" });
//                }
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(" Error in PickListController : ValidateItemQR() :: {Error}", ex.ToString());
//                return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
//            }
//        }


//        [HttpPost("PickList")]
//        public async Task<IActionResult> PickList([FromBody] PickList payload)
//        {
//            if (objGlobal == null)
//                objGlobal = new Global();
//            string p_ErrorMsg = string.Empty;
//            string _IsSaved = "N";
//            Object _TransSeq = 0;
//            Object _QRTransSeq = 0;
//            try
//            {

//                _logger.LogInformation(" Calling PickListController : PickList() ");

//                if (payload != null)
//                {
//                    var (success, errorMsg) = await objGlobal.ConnectSAP();
//                    if (success)
//                    {
//                        DateTime _docDate = DateTime.Today;
//                        int _TotalItemCount = payload.plItems.Count;
//                        int _SuccessCount = 0, _DetSuccessCount = 0;
//                        int _FailCount = 0, _DetFailCount = 0;

//                        #region Get TransSeq No - SO to PickList
//                        SqlConnection con = new SqlConnection(_QIT_connection);
//                        _Query = @" SELECT ISNULL(max(TransSeq),0) + 1 FROM QIT_Trans_SOToPickList A  ";
//                        _logger.LogInformation(" PickListController : GetTransSeqNo Query : {q} ", _Query.ToString());

//                        SqlCommand cmd = new SqlCommand(_Query, con);
//                        con.Open();
//                        _TransSeq = cmd.ExecuteScalar();
//                        con.Close();
//                        #endregion

//                        #region Get QR TransSeq No - SO to PickList
//                        con = new SqlConnection(_QIT_connection);
//                        _Query = @" SELECT ISNULL(max(QRTransSeq),0) + 1 FROM QIT_QRStock_SOToPickList A  ";
//                        _logger.LogInformation(" PickListController : GetQRTransSeqNo(PO to PickList) Query : {q} ", _Query.ToString());

//                        cmd = new SqlCommand(_Query, con);
//                        con.Open();
//                        _QRTransSeq = cmd.ExecuteScalar();
//                        con.Close();
//                        #endregion

//                        foreach (var item in payload.plItems)
//                        {
//                            #region Insert in Transaction Table - SO to PickList
//                            con = new SqlConnection(_QIT_connection);
//                            _Query = @"
//                             INSERT INTO QIT_Trans_SOToPickList
//                             (BranchID, TransId, TransSeq, FromObjType, ToObjType,BaseDocEntry, BaseDocNum, DocEntry, DocNum,  
//                              ItemCode,  Qty, UoMCode, FromWhs, ToWhs, Remark
//                             ) 
//                             VALUES 
//                             ( @bID, (SELECT ISNULL(max(TransId),0) + 1 FROM QIT_Trans_SOToPickList), @transSeq, '17', '156', @baseDocEntry, @baseDocNum, 
//                               @docEntry, @docNum, @itemCode,  @qty, @uom, @fromWhs, @toWhs, @remark 
//                             )
//                            ";
//                            _logger.LogInformation(" PickListController : Transfer Table Query : {q} ", _Query.ToString());

//                            cmd = new SqlCommand(_Query, con);
//                            cmd.Parameters.AddWithValue("@bID", payload.BranchID);
//                            cmd.Parameters.AddWithValue("@transSeq", _TransSeq);
//                            cmd.Parameters.AddWithValue("@baseDocEntry", payload.SODocEntry);
//                            cmd.Parameters.AddWithValue("@baseDocNum", payload.SODocNum);
//                            cmd.Parameters.AddWithValue("@docEntry", 0);
//                            cmd.Parameters.AddWithValue("@docNum", 0);
//                            cmd.Parameters.AddWithValue("@itemCode", item.ItemCode);
//                            cmd.Parameters.AddWithValue("@qty", Convert.ToDouble(item.TotalQty));
//                            cmd.Parameters.AddWithValue("@uom", item.UoMCode);
//                            cmd.Parameters.AddWithValue("@fromWhs", item.WhsCode);
//                            cmd.Parameters.AddWithValue("@toWhs", item.WhsCode);
//                            cmd.Parameters.AddWithValue("@remark", payload.Comment);
//                            int intNum = 0;

//                            con.Open();
//                            intNum = cmd.ExecuteNonQuery();
//                            con.Close();


//                            if (intNum > 0)
//                                _SuccessCount = _SuccessCount + 1;
//                            else
//                                _FailCount = _FailCount + 1;

//                            #endregion

//                            #region Insert in QR Stock Table - SO to PickList
//                            foreach (var itemDet in item.plBatchSerial)
//                            {
//                                con = new SqlConnection(_QIT_connection);
//                                _Query = @"
//                                INSERT INTO QIT_QRStock_SOToPickList 
//                                (      BranchID, TransId, TransSeq, QRTransSeq, QRCodeID, GateRecNo, ItemCode,BatchSerialNo, 
//                                       FromObjType, ToObjType, FromWhs, ToWhs, FromBinAbsEntry, ToBinAbsEntry, Qty
//                                ) 
//                                VALUES 
//                                ( @bID, (SELECT ISNULL(max(TransId),0) + 1 FROM QIT_QRStock_SOToPickList), 
//                                  @transSeq, @qrtransSeq, @qrCodeID, @gateInNo, @itemCode, @bsNo, '17', '156', @fromWhs, @toWhs, @fromBin, @toBin, @qty   
//                                )

//                                ";
//                                _logger.LogInformation("PickListController : Transfer Table Query : {q} ", _Query.ToString());

//                                cmd = new SqlCommand(_Query, con);
//                                cmd.Parameters.AddWithValue("@bID", payload.BranchID);
//                                cmd.Parameters.AddWithValue("@qrtransSeq", _QRTransSeq);
//                                cmd.Parameters.AddWithValue("@transSeq", _TransSeq);
//                                cmd.Parameters.AddWithValue("@qrCodeID", itemDet.DetailQRCodeID.Replace(" ", "~"));
//                                cmd.Parameters.AddWithValue("@gateInNo", itemDet.GateRecNo);
//                                cmd.Parameters.AddWithValue("@itemCode", item.ItemCode);
//                                cmd.Parameters.AddWithValue("@bsNo", itemDet.BatchSerialNo);
//                                cmd.Parameters.AddWithValue("@fromWhs", item.WhsCode);
//                                cmd.Parameters.AddWithValue("@toWhs", item.WhsCode);
//                                cmd.Parameters.AddWithValue("@fromBin", 0);
//                                cmd.Parameters.AddWithValue("@toBin", itemDet.BinAbsEntry);
//                                cmd.Parameters.AddWithValue("@qty", itemDet.Qty);
//                                //ub_DetailQRCodeID
//                                intNum = 0;
//                                try
//                                {
//                                    con.Open();
//                                    intNum = cmd.ExecuteNonQuery();
//                                    con.Close();
//                                }
//                                catch (Exception ex1)
//                                {
//                                    this.DeleteTransactionSOtoPickList(_TransSeq.ToString());
//                                    this.DeleteQRStockDetSOtoPickList(_QRTransSeq.ToString());

//                                    return BadRequest(new
//                                    {
//                                        StatusCode = "400",
//                                        IsSaved = "N",
//                                        TransSeq = _TransSeq,
//                                        StatusMsg = ex1.ToString()
//                                    });

//                                }


//                                if (intNum > 0)
//                                    _DetSuccessCount = _DetSuccessCount + 1;
//                                else
//                                    _DetFailCount = _DetFailCount + 1;
//                            }

//                            #endregion
//                        }

//                        if (_TotalItemCount == _SuccessCount)
//                        {
//                            int _Line = 0;

//                            SAPbobsCOM.Documents oDocuments;
//                            SAPbobsCOM.PickLists oPickLists;


//                            oDocuments = (SAPbobsCOM.Documents)objGlobal.oComp.GetBusinessObject(SAPbobsCOM.BoObjectTypes.oOrders);
//                            oDocuments.GetByKey(payload.SODocEntry);
//                            oPickLists = (SAPbobsCOM.PickLists)objGlobal.oComp.GetBusinessObject(SAPbobsCOM.BoObjectTypes.oPickLists);


//                            oPickLists.PickDate = _docDate;
//                            oPickLists.Remarks = payload.Comment;

//                            for (int i = 0; i < oDocuments.Lines.Count; i++)
//                            {
//                                // oDocuments.Lines.SetCurrentLine(i);
//                                foreach (var item in payload.plItems)
//                                {
//                                    if (oDocuments.Lines.LineNum == item.LineNum)
//                                    {
//                                        oPickLists.Lines.BaseObjectType = "17";
//                                        oPickLists.Lines.OrderEntry = payload.SODocEntry;
//                                        oPickLists.Lines.OrderRowID = item.LineNum;
//                                        oPickLists.Lines.ReleasedQuantity = double.Parse(item.TotalQty);

//                                        if (item.ItemMngBy.ToLower() == "b")
//                                        {
//                                            int _batchLine = 0;
//                                            foreach (var batch in item.plBatchSerial)
//                                            {
//                                                if (!string.IsNullOrEmpty(batch.BatchSerialNo))
//                                                {
//                                                    oPickLists.Lines.BatchNumbers.BaseLineNumber = _Line;
//                                                    oPickLists.Lines.BatchNumbers.BatchNumber = batch.BatchSerialNo;
//                                                    oPickLists.Lines.BatchNumbers.Quantity = Convert.ToDouble(batch.Qty);
//                                                    oPickLists.Lines.BatchNumbers.Add();

//                                                    if (batch.BinAbsEntry > 0)
//                                                    {
//                                                        oPickLists.Lines.BinAllocations.BinAbsEntry = batch.BinAbsEntry;
//                                                        oPickLists.Lines.BinAllocations.Quantity = Convert.ToDouble(batch.Qty);
//                                                        oPickLists.Lines.BinAllocations.SerialAndBatchNumbersBaseLine = _batchLine;
//                                                        oPickLists.Lines.BinAllocations.BaseLineNumber = _Line;
//                                                        oPickLists.Lines.BinAllocations.Add();
//                                                    }
//                                                    _batchLine = _batchLine + 1;
//                                                }
//                                            }
//                                        }
//                                        else if (item.ItemMngBy.ToLower() == "s")
//                                        {
//                                            int _serialLine = 0;
//                                            foreach (var serial in item.plBatchSerial)
//                                            {
//                                                if (!string.IsNullOrEmpty(serial.BatchSerialNo))
//                                                {
//                                                    oPickLists.Lines.BatchNumbers.BaseLineNumber = _Line;
//                                                    oPickLists.Lines.BatchNumbers.BatchNumber = serial.BatchSerialNo;
//                                                    oPickLists.Lines.BatchNumbers.Quantity = Convert.ToDouble(serial.Qty);
//                                                    oPickLists.Lines.BatchNumbers.Add();

//                                                    if (serial.BinAbsEntry > 0)
//                                                    {
//                                                        oPickLists.Lines.BinAllocations.BinAbsEntry = serial.BinAbsEntry;
//                                                        oPickLists.Lines.BinAllocations.Quantity = Convert.ToDouble(serial.Qty);
//                                                        oPickLists.Lines.BinAllocations.SerialAndBatchNumbersBaseLine = _serialLine;
//                                                        oPickLists.Lines.BinAllocations.BaseLineNumber = _Line;
//                                                        oPickLists.Lines.BinAllocations.Add();
//                                                    }
//                                                    _serialLine = _serialLine + 1;
//                                                }
//                                            }
//                                        }
//                                        oPickLists.Lines.Add();
//                                    }
//                                }
//                            }
//                            int addResult = oPickLists.Add();

//                            if (addResult != 0)
//                            {
//                                this.DeleteTransactionSOtoPickList(_TransSeq.ToString());
//                                this.DeleteQRStockDetSOtoPickList(_QRTransSeq.ToString());
//                                string msg;
//                                msg = "Error code: " + objGlobal.oComp.GetLastErrorCode() + Environment.NewLine +
//                                                "Error message: " + objGlobal.oComp.GetLastErrorDescription();
//                                _logger.LogInformation(" Calling PickListController : Error " + msg);
//                                return BadRequest(new
//                                {
//                                    StatusCode = "400",
//                                    IsSaved = _IsSaved,
//                                    TransSeq = _TransSeq,
//                                    StatusMsg = "Error code: " + addResult + Environment.NewLine +
//                                                "Error message: " + objGlobal.oComp.GetLastErrorDescription()
//                                });
//                            }
//                            else
//                            {
//                                int docEntry = int.Parse(objGlobal.oComp.GetNewObjectKey());

//                                #region Update Transaction Table
//                                con = new SqlConnection(_connection);
//                                _Query = @" UPDATE " + Global.QIT_DB + @".dbo.QIT_Trans_SOToPickList SET DocEntry = @docEntry, DocNum = @docEntry where TransSeq = @code";
//                                _logger.LogInformation(" PickListController : Update Transaction Table Query : {q} ", _Query.ToString());

//                                cmd = new SqlCommand(_Query, con);
//                                cmd.Parameters.AddWithValue("@docEntry", docEntry);
//                                cmd.Parameters.AddWithValue("@code", _TransSeq);

//                                con.Open();
//                                int intNum = cmd.ExecuteNonQuery();
//                                con.Close();

//                                if (intNum > 0)
//                                {
//                                    _IsSaved = "Y";
//                                    return Ok(new
//                                    {
//                                        StatusCode = "200",
//                                        IsSaved = "Y",
//                                        TransSeq = _TransSeq,
//                                        DocEntry = docEntry,
//                                        StatusMsg = "PickList added successfully !!!"
//                                    });
//                                }
//                                else
//                                {
//                                    _IsSaved = "N";
//                                    return Ok(new
//                                    {
//                                        StatusCode = "200",
//                                        IsSaved = "N",
//                                        TransSeq = _TransSeq,
//                                        DocEntry = docEntry,
//                                        StatusMsg = "Problem in updating Transaction Table"
//                                    });
//                                }
//                                #endregion

//                            }
//                        }
//                        else
//                        {
//                            this.DeleteTransactionSOtoPickList(_TransSeq.ToString());
//                            this.DeleteQRStockDetSOtoPickList(_QRTransSeq.ToString());
//                            return BadRequest(new
//                            {
//                                StatusCode = "400",
//                                IsSaved = "N",
//                                TransSeq = _TransSeq,
//                                StatusMsg = "Problem while saving Transaction data"
//                            });
//                        }
//                    }
//                    else
//                    {
//                        string msg;
//                        msg = "Error code: " + objGlobal.oComp.GetLastErrorCode() + Environment.NewLine +
//                                        "Error message: " + objGlobal.oComp.GetLastErrorDescription();
//                        return BadRequest(new
//                        {
//                            StatusCode = "400",
//                            IsSaved = _IsSaved,
//                            TransSeq = _TransSeq,
//                            StatusMsg = "Error code: " + objGlobal.oComp.GetLastErrorCode() + Environment.NewLine +
//                                        "Error message: " + objGlobal.oComp.GetLastErrorDescription()
//                        });
//                    }
//                }
//                else
//                {
//                    return BadRequest(new
//                    {
//                        StatusCode = "400",
//                        IsSaved = _IsSaved,
//                        TransSeq = 0,
//                        StatusMsg = "Details not found"
//                    });
//                }
//            }
//            catch (Exception ex)
//            {
//                this.DeleteTransactionSOtoPickList(_TransSeq.ToString());
//                this.DeleteQRStockDetSOtoPickList(_QRTransSeq.ToString());
//                _logger.LogError("Error in PickListController : PickList() :: {Error}", ex.ToString());
//                return BadRequest(new
//                {
//                    StatusCode = "400",
//                    IsSaved = _IsSaved,
//                    StatusMsg = ex.Message.ToString()
//                });
//            }
//            finally
//            {
//                objGlobal.oComp.Disconnect();
//            }
//        }


//        private bool DeleteTransactionSOtoPickList(string _TransSeq)
//        {
//            try
//            {
//                _logger.LogInformation(" Calling PickListController : DeleteTransactionSOtoPickList() ");
//                string p_ErrorMsg = string.Empty;

//                SqlConnection con = new SqlConnection(_QIT_connection);
//                _Query = @" DELETE FROM QIT_Trans_SOToPickList WHERE TransSeq = @transSeq ";
//                _logger.LogInformation(" PickListController : DeleteTransactionSOtoPickList Query : {q} ", _Query.ToString());

//                cmd = new SqlCommand(_Query, con);
//                cmd.Parameters.AddWithValue("@transSeq", _TransSeq);
//                con.Open();
//                int intNum = cmd.ExecuteNonQuery();
//                con.Close();

//                if (intNum > 0)
//                    return true;
//                else
//                    return false;
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError("Error in PickListController : DeleteTransactionSOtoPickList() :: {Error}", ex.ToString());
//                return false;
//            }
//        }


//        private bool DeleteQRStockDetSOtoPickList(string _QRTransSeq)
//        {
//            try
//            {
//                _logger.LogInformation(" Calling PickListController : DeleteQRStockDetSOtoPickList() ");
//                string p_ErrorMsg = string.Empty;

//                SqlConnection con = new SqlConnection(_QIT_connection);
//                _Query = @" DELETE FROM QIT_QRStock_SOToPickList WHERE QRTransSeq = @qrtransSeq ";
//                _logger.LogInformation(" PickListController : DeleteQRStockDetSOtoPickList Query : {q} ", _Query.ToString());

//                cmd = new SqlCommand(_Query, con);
//                cmd.Parameters.AddWithValue("@qrtransSeq", _QRTransSeq);
//                con.Open();
//                int intNum = cmd.ExecuteNonQuery();
//                con.Close();

//                if (intNum > 0)
//                    return true;
//                else
//                    return false;
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError("Error in PickListController : DeleteQRStockDetSOtoPickList() :: {Error}", ex.ToString());
//                return false;
//            }
//        } 
//    }
//}
