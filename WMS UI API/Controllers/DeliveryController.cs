using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using SAPbobsCOM;
using System.Data;
using System.Data.SqlClient;
using System.Numerics;
using WMS_UI_API.Common;
using WMS_UI_API.Models;
using WMS_UI_API.Services;

namespace WMS_UI_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DeliveryController : ControllerBase
    {
        private string _ApplicationApiKey = string.Empty;
        private string _connection = string.Empty;
        private string _QIT_connection = string.Empty;
        private string _QIT_DB = string.Empty;
        private string _Query = string.Empty;
        private SqlCommand cmd;
        public Global objGlobal;

        public IConfiguration Configuration { get; }
        private readonly ILogger<DeliveryController> _logger;
        private readonly ISAPConnectionService _sapConnectionService;

        public DeliveryController(IConfiguration configuration, ILogger<DeliveryController> logger, ISAPConnectionService sapConnectionService)
        {
            if (objGlobal == null)
                objGlobal = new Global();
            _logger = logger;
            try
            {
                Configuration = configuration;
                _ApplicationApiKey = Configuration["connectApp:ServiceApiKey"];
                _sapConnectionService = sapConnectionService;
                _connection = Configuration["connectApp:ConnString"];
                _QIT_connection = Configuration["connectApp:QITConnString"];

                _QIT_DB = Configuration["QITDB"];
                Global.QIT_DB = _QIT_DB;
                Global.SAP_DB = Configuration["CompanyDB"];

            }
            catch (Exception ex)
            {
                _logger.LogError(" Error in DeliveryController :: {Error}" + ex.ToString());
            }
        }


        [HttpGet("GetSalesOrderList")]
        public async Task<ActionResult<IEnumerable<SalesOrderList>>> GetSalesOrderList()
        {
            SqlConnection QITcon;
            SqlDataAdapter oAdptr;
            try
            {
                _logger.LogInformation(" Calling DeliveryController : GetSalesOrderList() ");
                List<SalesOrderList> obj = new List<SalesOrderList>();
                System.Data.DataTable dtData = new System.Data.DataTable();
                QITcon = new SqlConnection(_QIT_connection);

                _Query = @" 
                SELECT distinct A.DocEntry, A.DocNum, A.Series, A.DocDate, A.CardCode, A.CardName, A.NumAtCard, A.Comments 
                FROM " + Global.SAP_DB + @".dbo.ORDR A
                WHERE A.CANCELED = 'N' and A.DocStatus = 'O' and A.DocType = 'I'  
                ORDER BY A.DocEntry
                ";

                _logger.LogInformation(" DeliveryController : GetSalesOrderList() Query : {q} ", _Query.ToString());
                QITcon.Open();
                oAdptr = new SqlDataAdapter(_Query, QITcon);
                oAdptr.Fill(dtData);
                QITcon.Close();

                if (dtData.Rows.Count > 0)
                {
                    dynamic arData = JsonConvert.SerializeObject(dtData);
                    obj = JsonConvert.DeserializeObject<List<SalesOrderList>>(arData.ToString());
                    return obj;
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "No data found" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(" Error in DeliveryController : GetSalesOrderList() :: {Error}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
            }
        }


        //[HttpGet("GetPickedList")]
        //public async Task<ActionResult<IEnumerable<PickedItemsList>>> GetPickedList(int BranchId, int SODocEntry)
        //{
        //    SqlConnection QITcon;
        //    SqlDataAdapter oAdptr;
        //    try
        //    {
        //        _logger.LogInformation(" Calling DeliveryController : GetPickedList() ");

        //        #region Validation

        //        if (BranchId <=0 )
        //            return BadRequest(new { StatusCode = "400", StatusMsg = "Provide Branch" });

        //        if (SODocEntry <= 0)
        //            return BadRequest(new { StatusCode = "400", StatusMsg = "Provide Sales Order" });

        //        #region Check for valid Sales Order

        //        QITcon = new SqlConnection(_QIT_connection);
        //        DataTable dtSO = new DataTable();
        //        _Query = @" SELECT * FROM " + Global.SAP_DB + @".dbo.ORDR A WHERE A.DocEntry = @docEntry AND A.Canceled = 'N' ";

        //        _logger.LogInformation(" DeliveryController : Check for valid SO Query : {q} ", _Query.ToString());
        //        QITcon.Open();
        //        oAdptr = new SqlDataAdapter(_Query, QITcon);
        //        oAdptr.SelectCommand.Parameters.AddWithValue("@docEntry", SODocEntry);
        //        oAdptr.Fill(dtSO);
        //        QITcon.Close();

        //        if (dtSO.Rows.Count <= 0)
        //        {
        //            return BadRequest(new { StatusCode = "400", StatusMsg = "No such Sales Order exist" });
        //        }

        //        #endregion

        //        #endregion

        //        System.Data.DataTable dtData = new System.Data.DataTable();
        //        QITcon = new SqlConnection(_QIT_connection);

        //        _Query = @" 
        //        SELECT A.BranchID, A.BaseDocEntry SODocEntry, A.BaseDocNum SODocNum, A.DocEntry PickNo, 
        //            A.ItemCode, A.Qty, A.UoMCode, A.Remark
        //        FROM QIT_Trans_SOToPickList A
        //        WHERE BaseDocEntry = @docEntry and ISNULL(A.BranchID, @bId) = @bId
        //        ";

        //        _logger.LogInformation(" DeliveryController : GetPickedList() Query : {q} ", _Query.ToString());
        //        QITcon.Open();
        //        oAdptr = new SqlDataAdapter(_Query, QITcon);
        //        oAdptr.SelectCommand.Parameters.AddWithValue("@bId", BranchId);
        //        oAdptr.SelectCommand.Parameters.AddWithValue("@docEntry", SODocEntry);
        //        oAdptr.Fill(dtData);
        //        QITcon.Close();

        //        if (dtData.Rows.Count > 0)
        //        {
        //            List<PickedItemsList> obj = new List<PickedItemsList>();
        //            dynamic arData = JsonConvert.SerializeObject(dtData);
        //            obj = JsonConvert.DeserializeObject<List<PickedItemsList>>(arData.ToString());
        //            return obj;
        //        }
        //        else
        //        {
        //            return BadRequest(new { StatusCode = "400", StatusMsg = "No data found" });
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(" Error in DeliveryController : GetPickedList() :: {Error}", ex.ToString());
        //        return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
        //    }
        //}


        [HttpPost("ValidateItemQR")]
        public async Task<ActionResult<IEnumerable<SOItemData>>> ValidateItemQR(SOItemValidateCls payload)
        {
            SqlConnection QITcon;
            SqlDataAdapter oAdptr;
            try
            {
                _logger.LogInformation(" Calling DeliveryController : ValidateItemQR() ");
                List<SOItemData> obj = new List<SOItemData>();
                DataTable dtConfig = new DataTable();
                DataTable dtStock = new DataTable();
                System.Data.DataTable dtData = new System.Data.DataTable();
                string _DeliveryWhs = string.Empty;

                #region Get Configuration
                QITcon = new SqlConnection(_QIT_connection);
                _Query = @" SELECT * FROM QIT_Config_Master A WHERE ISNULL(A.BranchID, @bID) = @bID  ";
                _logger.LogInformation(" DeliveryController : Configuration  Query : {q} ", _Query.ToString());
                QITcon.Open();
                oAdptr = new SqlDataAdapter(_Query, QITcon);
                oAdptr.SelectCommand.Parameters.AddWithValue("@bID", payload.BranchID);
                oAdptr.Fill(dtConfig);
                QITcon.Close();

                if (dtConfig.Rows.Count > 0)
                {
                    if (dtConfig.Rows[0]["DeliveryWhs"].ToString().Trim().Length <= 0)
                        return BadRequest(new { StatusCode = "400", StatusMsg = "Define Delivery Warehouse in Configuration" });
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Configuration not found" });
                }
                _DeliveryWhs = dtConfig.Rows[0]["DeliveryWhs"].ToString().Trim();

                #endregion


                #region Get Item QR Stock by Warehouse
                QITcon = new SqlConnection(_QIT_connection);

                #region Stock Query

                if (payload.DetailQRCodeID.Replace(" ", "~").ToUpper().Contains("~PO~"))
                {
                    #region Stock Query

                    _Query = @"  SELECT * FROM (
                     SELECT A.WhsCode, A.WhsName, 
                            CASE WHEN A.POtoGRPO > 0 THEN (POtoGRPO + InQty - OutQty - IssueQty - DeliverQty + ReceiptQty) ELSE (InQty - OutQty - IssueQty - DeliverQty + ReceiptQty) END Stock
                     FROM 
                     (
                         SELECT A.WhsCode, A.WhsName,
                         (
                             ISNULL((	
                                 SELECT sum(Z.Qty) POtoGRPOQty FROM [QIT_QRStock_InvTrans] Z
                                 WHERE Z.QRCodeID = @qrCode and Z.FromObjType = '22' and Z.ToObjType = '67' and Z.ToWhs = A.WhsCode and ISNULL(Z.BranchID, @bID) = @bID
                             ),0)
                         ) POtoGRPO,
                         (ISNULL((
                                 SELECT sum(Z.Qty) InQty FROM [QIT_QRStock_InvTrans] Z
                                 WHERE Z.QRCodeID = @qrCode and (Z.FromObjType <> '22') AND 
                                       Z.ToWhs collate SQL_Latin1_General_CP850_CI_AS = A.WhsCode and ISNULL(Z.BranchID, @bID) =@bID
                             ),0) 
                         ) InQty,
                         (
                             ISNULL((
                                 SELECT sum(Z.Qty) OutQty from [QIT_QRStock_InvTrans] Z
                                 WHERE Z.QRCodeID = @qrCode and (Z.FromObjType <> '22') and 
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
                     ) as A ) as B where B.Stock> 0 ";

                    #endregion
                }
                else if (payload.DetailQRCodeID.Replace(" ", "~").ToUpper().Contains("~PRO~"))
                {
                    #region Stock Query

                    _Query = @"  SELECT * FROM (
                     SELECT A.WhsCode, A.WhsName, 
                            CASE WHEN A.POtoGRPO > 0 THEN (POtoGRPO + InQty - OutQty - IssueQty - DeliverQty ) ELSE (InQty - OutQty - IssueQty - DeliverQty) END Stock
                     FROM 
                     (
                         SELECT A.WhsCode, A.WhsName,
                         (
                             ISNULL((	
                                 SELECT sum(Z.Qty) POtoGRPOQty FROM [QIT_ProQRStock_InvTrans] Z
                                 WHERE Z.QRCodeID = @qrCode and Z.FromObjType = '202' and Z.ToObjType = '59' and Z.ToWhs = A.WhsCode and ISNULL(Z.BranchID, @bID) = @bID
                             ),0)
                         ) POtoGRPO,
                         (ISNULL((
                                 SELECT sum(Z.Qty) InQty FROM [QIT_ProQRStock_InvTrans] Z
                                 WHERE Z.QRCodeID = @qrCode and (Z.FromObjType <> '202') AND 
                                       Z.ToWhs collate SQL_Latin1_General_CP850_CI_AS = A.WhsCode and ISNULL(Z.BranchID, @bID) =@bID
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
                         ) DeliverQty
                     FROM QIT_Warehouse_Master AS A  
                     where Locked = 'N' 
                     ) as A ) as B where B.Stock> 0 ";

                    #endregion
                }
                else if (payload.DetailQRCodeID.Replace(" ", "~").ToUpper().Contains("~OS~"))
                {
                    #region Stock Query

                    _Query = @"  SELECT * FROM (
                     SELECT A.WhsCode, A.WhsName, 
                            CASE WHEN A.POtoGRPO > 0 THEN (POtoGRPO + InQty - OutQty - IssueQty - DeliverQty ) ELSE (InQty - OutQty - IssueQty - DeliverQty) END Stock
                     FROM 
                     (
                         SELECT A.WhsCode, A.WhsName,
                         (
                             ISNULL((	
                                 SELECT sum(Z.Qty) POtoGRPOQty FROM [QIT_OSQRStock_InvTrans] Z
                                 WHERE Z.QRCodeID = @qrCode and Z.FromObjType = '202' and Z.ToObjType = '59' and Z.ToWhs = A.WhsCode and ISNULL(Z.BranchID, @bID) = @bID
                             ),0)
                         ) POtoGRPO,
                         (ISNULL((
                                 SELECT sum(Z.Qty) InQty FROM [QIT_OSQRStock_InvTrans] Z
                                 WHERE Z.QRCodeID = @qrCode and (Z.FromObjType <> '202') AND 
                                       Z.ToWhs collate SQL_Latin1_General_CP850_CI_AS = A.WhsCode and ISNULL(Z.BranchID, @bID) =@bID
                             ),0) 
                         ) InQty,
                         (
                             ISNULL((
                                 SELECT sum(Z.Qty) OutQty from [QIT_OSQRStock_InvTrans] Z
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
                         ) DeliverQty
                     FROM QIT_Warehouse_Master AS A  
                     where Locked = 'N' 
                     ) as A ) as B where B.Stock> 0 ";

                    #endregion
                }

                #endregion

                _logger.LogInformation(" DeliveryController : Item Stock Query : {q} ", _Query.ToString());
                QITcon.Open();
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
                    bool validWhs = dtStock.AsEnumerable().Any(row => row.Field<string>("WhsCode") == _DeliveryWhs);
                    if (!validWhs)
                        return BadRequest(new { StatusCode = "400", StatusMsg = "Item Stock is not available for the warehouse : " + _DeliveryWhs });
                }
                #endregion

                var quantityForVD_FG = dtStock.AsEnumerable()
                                     .Where(row => row.Field<string>("WhsCode") == _DeliveryWhs)
                                     .Select(row => row.Field<decimal>("Stock"))
                                     .FirstOrDefault(); // Use FirstOrDefault to get the first matching row or default value if not found

                QITcon = new SqlConnection(_QIT_connection);

                _Query = @" 
                SELECT A.SODocEntry, A.SODocNum, A.SOObjType, A.SODocDate, A.CardCode, A.CardName,
                       A.LineNum, A.ItemCode, A.ItemName, A.WhsCode, B.ItemMngBy, B.QRMngBy, B.UomCode, 
                       A.BaseObjType ,A.BaseDocEntry, A.BaseDocNum, A.GateRecNo, A.QRCodeID, A.BatchSerialNo,  
                       A.Quantity OrderedQty, A.QRQty, " + quantityForVD_FG + @" QtyInWhs,
                       ISNULL(( SELECT sum(Qty) FROM QIT_QRStock_SOToDelivery Z where Z.QRCodeID = A.QRCodeID),0) SOQRItemPrevDeliver,
	                   ISNULL(( SELECT sum(Z.Qty) FROM QIT_QRStock_SOToDelivery Z 
                                INNER JOIN QIT_Trans_SOToDelivery Z1 ON Z.TransSeq = Z1.TransSeq 
                                WHERE Z.QRCodeID = A.QRCodeID and Z1.BaseDocEntry = A.SoDocEntry),0) SOItemPrevDeliver
                FROM 
                (
                    SELECT A.DocEntry SODocEntry, A.DocNum SODocNum, A.ObjType SOObjType, A.DocDate SODocDate, A.CardCode, A.CardName,
                           B.LineNum, B.ItemCode, B.Dscription ItemName, B.Quantity, '" + _DeliveryWhs + @"' WhsCode,
                           '202' BaseObjType ,C.BaseDocEntry BaseDocEntry, D.DocNum BaseDocNum,
                            E.RecNo GateRecNo, E.QRCodeID, E.BatchSerialNo, C.Qty QRQty 
                    FROM " + Global.SAP_DB + @".dbo.ORDR A 
                            INNER JOIN " + Global.SAP_DB + @".dbo.RDR1 B ON A.DocEntry = B.DocEntry
                            INNER JOIN QIT_Trans_ProToReceipt C ON C.ItemCode collate SQL_Latin1_General_CP850_CI_AS = B.ItemCode AND
                                                        C.ToWhs collate SQL_Latin1_General_CP850_CI_AS = B.WhsCode  
                            INNER JOIN QIT_QRStock_ProToReceipt E ON E.TransSeq = C.TransSeq and E.ItemCode = C.ItemCode
                            INNER JOIN " + Global.SAP_DB + @".dbo.OWOR D on D.DocEntry = C.BaseDocEntry
                    WHERE A.DocEntry = @docEntry AND A.CANCELED = 'N' AND A.DocStatus = 'O' AND LineStatus = 'O'
                            AND E.QRCodeID = @dQR
                    
                    UNION

                    SELECT A.DocEntry SODocEntry, A.DocNum SODocNum, A.ObjType SOObjType, A.DocDate SODocDate, A.CardCode, A.CardName,
                        B.LineNum, B.ItemCode, B.Dscription ItemName, B.Quantity, '" + _DeliveryWhs + @"' WhsCode,
                        D.FromObjType BaseObjType, D.BaseDocEntry BaseDocEntry, D.BaseDocNum BaseDocNum, 
                            C.GateInNo GateRecNo, C.QRCodeID, C.BatchSerialNo, C.Qty QRQty 
                    FROM " + Global.SAP_DB + @".dbo.ORDR A 
                            INNER JOIN " + Global.SAP_DB + @".dbo.RDR1 B ON A.DocEntry = B.DocEntry
                            INNER JOIN QIT_QRStock_POToGRPO C ON C.ItemCode collate SQL_Latin1_General_CP850_CI_AS = B.ItemCode
                            INNER JOIN QIT_Trans_POToGRPO D ON C.TransSeq = D.TransId
                    WHERE A.DocEntry = @docEntry AND A.CANCELED = 'N' AND A.DocStatus = 'O' AND LineStatus = 'O' AND 
                            C.QRCodeID = @dQR  

                    UNION

                    SELECT A.DocEntry SODocEntry, A.DocNum SODocNum, A.ObjType SOObjType, A.DocDate SODocDate, A.CardCode, A.CardName,
                        B.LineNum, B.ItemCode, B.Dscription ItemName, B.Quantity, '"" + _DeliveryWhs + @""' WhsCode,
                        '310000001' BaseObjType, 0 BaseDocEntry, 0 BaseDocNum, 
                            C.OpeningNo GateRecNo, C.QRCodeID, C.BatchSerialNo, C.Qty QRQty 
                    FROM " + Global.SAP_DB + @".dbo.ORDR A 
                            INNER JOIN " + Global.SAP_DB + @".dbo.RDR1 B ON A.DocEntry = B.DocEntry
                            INNER JOIN QIT_OpeningStock_QR_Detail C ON C.ItemCode collate SQL_Latin1_General_CP850_CI_AS = B.ItemCode 
                    WHERE A.DocEntry = @docEntry AND A.CANCELED = 'N' AND A.DocStatus = 'O' AND LineStatus = 'O' AND C.QRCodeID = @dQR   
                ) as A INNER JOIN QIT_Item_Master B ON A.ItemCode collate SQL_Latin1_General_CP1_CI_AS = B.ItemCode
                ";


                //_Query = @"
                //SELECT A.PickDocEntry, A.PickDocNum,
                //       A.QRCodeId, A.BatchSerialNo, 
                //       B.DocEntry SODocEntry, B.DocNum SODocNum, B.ObjType SOObjType, B.DocDate SODocDate, 
                //    B.CardCode, B.CardName, C.LineNum, C.ItemCode, C.Dscription ItemName, 'VD-FG' WhsCode,
                //    C.Quantity SOOrderedQty, A.SOQRPickedQty, 
                //    ISNULL(( SELECT sum(Z1.Qty) FROM QIT_QRStock_PickListToDelivery Z1
                //    INNER JOIN QIT_Trans_PickListToDelivery Z2 on Z1.TransSeq = Z2.TransSeq
                //    WHERE Z1.QRCodeID = A.QRCodeID and Z2.SODocEntry = B.DocEntry ),0) PrevDeliveredQty,
                //       D.ItemMngBy, D.QRMngBy
                //FROM
                //(
                // SELECT A.BaseDocEntry SODocEntry, A.DocEntry PickDocEntry, A.DocNum PickDocNum, 
                //           B.QRCodeID, B.BatchSerialNo, B.ItemCode, A.Qty SOQRPickedQty
                // FROM QIT_Trans_SOToPickList A
                //   INNER JOIN QIT_QRStock_SOToPickList B ON A.TransSeq = B.TransSeq AND A.ItemCode = B.ItemCode
                // WHERE A.BaseDocEntry = @docEntry AND B.QRCodeID = @dQR AND ISNULL(A.BranchID, @bId) = @bId
                // -- GROUP BY A.BaseDocEntry, B.QRCodeID, B.BatchSerialNo, B.ItemCode
                //) as A
                //INNER JOIN " + Global.SAP_DB + @".dbo.ORDR B ON A.SODocEntry = B.DocEntry
                //INNER JOIN " + Global.SAP_DB + @".dbo.RDR1 C ON B.DocEntry = C.DocEntry AND 
                //      A.ItemCode collate SQL_Latin1_General_CP850_CI_AS = C.ItemCode
                //INNER JOIN QIT_Item_Master D ON D.ItemCode collate SQL_Latin1_General_CP850_CI_AS = C.ItemCode
                //";

                _logger.LogInformation(" DeliveryController : ValidateItemQR() Query : {q} ", _Query.ToString());
                QITcon.Open();
                oAdptr = new SqlDataAdapter(_Query, QITcon);
                oAdptr.SelectCommand.Parameters.AddWithValue("@bId", payload.BranchID);
                oAdptr.SelectCommand.Parameters.AddWithValue("@docEntry", payload.SODocEntry);
                oAdptr.SelectCommand.Parameters.AddWithValue("@dQR", payload.DetailQRCodeID.Replace(" ", "~"));
                oAdptr.Fill(dtData);
                QITcon.Close();

                if (dtData.Rows.Count > 0)
                {
                    dynamic arData = JsonConvert.SerializeObject(dtData);
                    obj = JsonConvert.DeserializeObject<List<SOItemData>>(arData.ToString().Replace("~", " "));
                    return obj;
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "No data found" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(" Error in DeliveryController : ValidateItemQR() :: {Error}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
            }
        }


        [HttpPost("SalesDelivery")]
        public async Task<IActionResult> SalesDelivery(SalesDelivery payload)
        {
            SqlConnection QITcon;
            Object _TransSeq = 0;
            Object _QRTransSeq = 0;
            int _Success = 0;
            int _Fail = 0;

            string p_ErrorMsg = string.Empty;
            try
            {
                _logger.LogInformation(" Calling DeliveryController : SalesDelivery() ");

                if (payload != null)
                {

                    if (((WMS_UI_API.Services.SAPConnectionService)_sapConnectionService).oComp.Connected)
                    {
                        #region Get TransSeq - Sales Delivery
                        QITcon = new SqlConnection(_QIT_connection);
                        _Query = @" SELECT ISNULL(max(TransSeq),0) + 1 FROM QIT_Trans_SOToDelivery A  ";
                        _logger.LogInformation("DeliveryController : TransSeq Query : {q} ", _Query.ToString());
                        cmd = new SqlCommand(_Query, QITcon);
                        QITcon.Open();
                        _TransSeq = cmd.ExecuteScalar();
                        QITcon.Close();
                        #endregion

                        #region Get QR TransSeq No - SO to Delivery
                        QITcon = new SqlConnection(_QIT_connection);
                        _Query = @" SELECT ISNULL(max(QRTransSeq),0) + 1 FROM QIT_QRStock_SOToDelivery A  ";
                        _logger.LogInformation(" DeliveryController : SalesDelivery Query : {q} ", _Query.ToString());
                        cmd = new SqlCommand(_Query, QITcon);
                        QITcon.Open();
                        _QRTransSeq = cmd.ExecuteScalar();
                        QITcon.Close();
                        #endregion


                        foreach (var item in payload.sdItems)
                        {
                            #region Insert in Transaction table

                            QITcon = new SqlConnection(_QIT_connection);
                            _Query = @"
                                INSERT INTO QIT_Trans_SOToDelivery 
                                (  BranchID, TransId, TransSeq, FromObjType, ToObjType, BaseDocEntry, BaseDocNum, DocEntry, DocNum,
                                   ItemCode, Qty, UoMCode, FromWhs, ToWhs, Remark, EntryDate
                                ) 
                                VALUES 
                                (  @bID, (SELECT ISNULL(max(TransId),0) + 1 FROM QIT_Trans_SOToDelivery), @transSeq,
                                   @frObj, @toObj, @baseDocEntry, @baseDocNum, @docEntry, @docNum,
                                   @itemCode, @qty, @uom, @frWhs, @toWhs, @remark, @eDate
                                )  
                                ";

                            _logger.LogInformation("DeliveryController : QIT Delivery Table Query : {q} ", _Query.ToString());

                            cmd = new SqlCommand(_Query, QITcon);
                            cmd.Parameters.AddWithValue("@bID", payload.BranchID);
                            cmd.Parameters.AddWithValue("@transSeq", _TransSeq);
                            cmd.Parameters.AddWithValue("@frObj", "17");
                            cmd.Parameters.AddWithValue("@toObj", "15");
                            cmd.Parameters.AddWithValue("@baseDocEntry", payload.SODocEntry);
                            cmd.Parameters.AddWithValue("@baseDocNum", payload.SODocNum);
                            cmd.Parameters.AddWithValue("@docEntry", 0);
                            cmd.Parameters.AddWithValue("@docNum", 0);
                            cmd.Parameters.AddWithValue("@itemCode", item.ItemCode);
                            cmd.Parameters.AddWithValue("@qty", item.TotalQty);
                            cmd.Parameters.AddWithValue("@uom", item.UoMCode);
                            cmd.Parameters.AddWithValue("@frWhs", payload.WhsCode);
                            cmd.Parameters.AddWithValue("@toWhs", string.Empty);
                            cmd.Parameters.AddWithValue("@remark", payload.Comment);
                            cmd.Parameters.AddWithValue("@eDate", DateTime.Now);

                            int intNum = 0;
                            try
                            {
                                QITcon.Open();
                                intNum = cmd.ExecuteNonQuery();
                                QITcon.Close();
                            }
                            catch (Exception ex1)
                            {
                                this.DeleteDeliveryTransaction(_TransSeq.ToString());
                                return BadRequest(new
                                {
                                    StatusCode = "400",
                                    IsSaved = "N",
                                    StatusMsg = ex1.Message.ToString()
                                });

                            }
                            _Success = _Success + 1;
                            #endregion

                            #region Insert into QR Stock table
                            foreach (var itemDet in item.sdBatchSerial)
                            {
                                QITcon = new SqlConnection(_QIT_connection);
                                _Query = @"
                                INSERT INTO QIT_QRStock_SOToDelivery 
                                (  BranchID, TransId, TransSeq, QRTransSeq, QRCodeID, ItemCode, GateRecNo, Qty, BatchSerialNo, 
                                   FromObjType, ToObjType, FromWhs, ToWhs, FromBinAbsEntry, ToBinAbsEntry, EntryDate
                                ) 
                                VALUES 
                                (  @bID, (SELECT ISNULL(max(TransId),0) + 1 FROM QIT_QRStock_SOToDelivery), @transSeq, @QRTransSeq,
                                   @qr, @itemCode, @gateRecNo, @qty, @batch, @frObj, @toObj, @frWhs, @toWhs, @frBin, @toBin, @eDate
                                )  
                                ";

                                _logger.LogInformation("DeliveryController : QIT Delivery Table Query : {q} ", _Query.ToString());

                                cmd = new SqlCommand(_Query, QITcon);
                                cmd.Parameters.AddWithValue("@bID", payload.BranchID);
                                cmd.Parameters.AddWithValue("@transSeq", _TransSeq);
                                cmd.Parameters.AddWithValue("@QRTransSeq", _QRTransSeq);
                                cmd.Parameters.AddWithValue("@qr", itemDet.DetailQRCodeID.Replace(" ", "~"));
                                cmd.Parameters.AddWithValue("@itemCode", item.ItemCode);
                                cmd.Parameters.AddWithValue("@gateRecNo", itemDet.GateRecNo);
                                cmd.Parameters.AddWithValue("@qty", itemDet.Qty);
                                cmd.Parameters.AddWithValue("@batch", itemDet.BatchSerialNo);
                                cmd.Parameters.AddWithValue("@frObj", "17");
                                cmd.Parameters.AddWithValue("@toObj", "15");
                                cmd.Parameters.AddWithValue("@frWhs", payload.WhsCode);
                                cmd.Parameters.AddWithValue("@toWhs", string.Empty);
                                cmd.Parameters.AddWithValue("@frBin", payload.BinAbsEntry);
                                cmd.Parameters.AddWithValue("@toBin", 0);
                                cmd.Parameters.AddWithValue("@eDate", DateTime.Now);

                                intNum = 0;
                                try
                                {
                                    QITcon.Open();
                                    intNum = cmd.ExecuteNonQuery();
                                    QITcon.Close();
                                }
                                catch (Exception ex1)
                                {
                                    this.DeleteDeliveryTransaction(_TransSeq.ToString());
                                    this.DeleteDeliveryDetails(_TransSeq.ToString());
                                    return BadRequest(new
                                    {
                                        StatusCode = "400",
                                        IsSaved = "N",
                                        StatusMsg = ex1.Message.ToString()
                                    });

                                }
                            }
                            #endregion
                        }


                        int _Line = 0;
                        SAPbobsCOM.Documents oSalesOrder = (SAPbobsCOM.Documents)((WMS_UI_API.Services.SAPConnectionService)_sapConnectionService).oComp.GetBusinessObject(SAPbobsCOM.BoObjectTypes.oOrders);

                        // Retrieve the sales order based on document number or other criteria
                        if (oSalesOrder.GetByKey(payload.SODocEntry))
                        {
                            Documents SalesDelivery = (Documents)((WMS_UI_API.Services.SAPConnectionService)_sapConnectionService).oComp.GetBusinessObject(BoObjectTypes.oDeliveryNotes);

                            SalesDelivery.CardCode = oSalesOrder.CardCode;
                            SalesDelivery.DocDate = DateTime.Now;
                            SalesDelivery.BPL_IDAssignedToInvoice = payload.BranchID;
                            SalesDelivery.Comments = payload.Comment;
                            SalesDelivery.Series = payload.Series;

                            foreach (var item in payload.sdItems)
                            {
                                //SalesDelivery.Lines.ItemCode = item.ItemCode;
                                SalesDelivery.Lines.Quantity = double.Parse(item.TotalQty); // Set the quantity 
                                SalesDelivery.Lines.BaseType = 17;
                                SalesDelivery.Lines.BaseEntry = payload.SODocEntry;
                                SalesDelivery.Lines.BaseLine = item.LineNum;
                                SalesDelivery.Lines.WarehouseCode = payload.WhsCode;
                                SalesDelivery.Lines.TaxCode = oSalesOrder.Lines.TaxCode;

                                if (item.ItemMngBy.ToLower() == "s")
                                {
                                    int i = 0;
                                    foreach (var serial in item.sdBatchSerial)
                                    {
                                        if (!string.IsNullOrEmpty(serial.BatchSerialNo))
                                        {
                                            SalesDelivery.Lines.SerialNumbers.SetCurrentLine(i);
                                            SalesDelivery.Lines.BatchNumbers.BaseLineNumber = _Line;
                                            SalesDelivery.Lines.SerialNumbers.InternalSerialNumber = serial.BatchSerialNo;
                                            SalesDelivery.Lines.SerialNumbers.ManufacturerSerialNumber = serial.BatchSerialNo;
                                            SalesDelivery.Lines.Quantity = Convert.ToDouble(serial.Qty);
                                            SalesDelivery.Lines.SerialNumbers.Add();

                                            if (payload.BinAbsEntry > 0)
                                            {
                                                SalesDelivery.Lines.BinAllocations.BinAbsEntry = payload.BinAbsEntry;
                                                SalesDelivery.Lines.BinAllocations.Quantity = Convert.ToDouble(serial.Qty);
                                                SalesDelivery.Lines.BinAllocations.BaseLineNumber = _Line;
                                                SalesDelivery.Lines.BinAllocations.SerialAndBatchNumbersBaseLine = i;
                                                SalesDelivery.Lines.BinAllocations.Add();
                                            }
                                            i = i + 1;
                                        }
                                    }
                                }
                                else if (item.ItemMngBy.ToLower() == "b")
                                {
                                    int _batchLine = 0;
                                    foreach (var batch in item.sdBatchSerial)
                                    {
                                        if (!string.IsNullOrEmpty(batch.BatchSerialNo))
                                        {
                                            SalesDelivery.Lines.BatchNumbers.BaseLineNumber = _Line;
                                            SalesDelivery.Lines.BatchNumbers.BatchNumber = batch.BatchSerialNo;
                                            SalesDelivery.Lines.BatchNumbers.Quantity = Convert.ToDouble(batch.Qty);
                                            SalesDelivery.Lines.BatchNumbers.Add();

                                            if (payload.BinAbsEntry > 0)
                                            {
                                                SalesDelivery.Lines.BinAllocations.BinAbsEntry = payload.BinAbsEntry;
                                                SalesDelivery.Lines.BinAllocations.Quantity = Convert.ToDouble(batch.Qty);
                                                SalesDelivery.Lines.BinAllocations.BaseLineNumber = _Line;
                                                SalesDelivery.Lines.BinAllocations.SerialAndBatchNumbersBaseLine = _batchLine;
                                                SalesDelivery.Lines.BinAllocations.Add();
                                            }
                                            _batchLine = _batchLine + 1;
                                        }
                                    }
                                }
                                SalesDelivery.Lines.Add();
                                _Line = _Line + 1;
                            }

                            int result = SalesDelivery.Add();
                            if (result != 0)
                            {
                                this.DeleteDeliveryTransaction(_TransSeq.ToString());
                                this.DeleteDeliveryDetails(_TransSeq.ToString());
                                return BadRequest(new
                                {
                                    StatusCode = "400",
                                    IsSaved = "N",
                                    StatusMsg = "Error code: " + result + Environment.NewLine +
                                                "Error message: " + ((WMS_UI_API.Services.SAPConnectionService)_sapConnectionService).oComp.GetLastErrorDescription()
                                });
                            }
                            else
                            {
                                int docEntry = int.Parse(((WMS_UI_API.Services.SAPConnectionService)_sapConnectionService).oComp.GetNewObjectKey());
                                return Ok(new
                                {
                                    StatusCode = "200",
                                    IsSaved = "Y",
                                    DocEntry = docEntry,
                                    StatusMsg = "Sales Delivery done successfully !!!"
                                });
                            }
                        }
                        else
                        {
                            this.DeleteDeliveryTransaction(_TransSeq.ToString());
                            this.DeleteDeliveryDetails(_TransSeq.ToString());
                            return BadRequest(new
                            {
                                StatusCode = "400",
                                IsSaved = "N",
                                StatusMsg = "Sales Order does not exist"
                            });
                        }

                    }
                    else
                    {
                        this.DeleteDeliveryTransaction(_TransSeq.ToString());
                        this.DeleteDeliveryDetails(_TransSeq.ToString());
                        return BadRequest(new
                        {
                            StatusCode = "400",
                            IsSaved = "N",
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
                        IsSaved = "N",
                        TransSeq = 0,
                        StatusMsg = "Details not found"
                    });
                }
            }
            catch (Exception ex)
            {
                this.DeleteDeliveryTransaction(_TransSeq.ToString());
                this.DeleteDeliveryDetails(_TransSeq.ToString());
                _logger.LogError(" Error in DeliveryController : SalesDelivery() :: {Error}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
            }
            finally
            {
               
            }
        }

        private bool DeleteDeliveryTransaction(string p_TransSeq)
        {
            SqlConnection QITcon;
            try
            {
                _logger.LogInformation(" Calling DeliveryController : DeleteDeliveryTransaction() ");

                QITcon = new SqlConnection(_QIT_connection);
                _Query = @" DELETE FROM QIT_Trans_SOToDelivery WHERE TransSeq = @transSeq ";
                _logger.LogInformation(" DeliveryController : DeleteDeliveryTransaction Query : {q} ", _Query.ToString());

                cmd = new SqlCommand(_Query, QITcon);
                cmd.Parameters.AddWithValue("@transSeq", p_TransSeq);
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
                _logger.LogError("Error in DeliveryController : DeleteDeliveryTransaction() :: {Error}", ex.ToString());
                return false;
            }
        }


        private bool DeleteDeliveryDetails(string p_TransSeq)
        {
            SqlConnection QITcon;
            try
            {
                _logger.LogInformation(" Calling DeliveryController : DeleteDeliveryDetails() ");

                QITcon = new SqlConnection(_QIT_connection);
                _Query = @" DELETE FROM QIT_QRStock_SOToDelivery WHERE TransSeq = @transSeq ";
                _logger.LogInformation(" DeliveryController : DeleteDeliveryDetails Query : {q} ", _Query.ToString());

                cmd = new SqlCommand(_Query, QITcon);
                cmd.Parameters.AddWithValue("@transSeq", p_TransSeq);
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
                _logger.LogError("Error in DeliveryController : DeleteDeliveryDetails() :: {Error}", ex.ToString());
                return false;
            }
        }

    }
}
