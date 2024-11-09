using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using SAPbobsCOM;
using System.Data;
using System.Data.SqlClient;
using WMS_UI_API.Common;
using WMS_UI_API.Models;
using WMS_UI_API.Services;
using DataTable = System.Data.DataTable;

namespace WMS_UI_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductionController : ControllerBase
    {
        private string _ApplicationApiKey = string.Empty;
        private string _connection = string.Empty;
        private string _QIT_connection = string.Empty;

        private string _Query = string.Empty;
        private SqlCommand cmd;
        public Global objGlobal;

        public IConfiguration Configuration { get; }
        private readonly ILogger<ProductionController> _logger;
        private readonly ISAPConnectionService _sapConnectionService;

        public ProductionController(IConfiguration configuration, ILogger<ProductionController> logger, ISAPConnectionService sapConnectionService)
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

                Global.QIT_DB = Configuration["QITDB"];
                Global.SAP_DB = Configuration["CompanyDB"];

            }
            catch (Exception ex)
            {
                _logger.LogError(" Error in ProductionController :: {Error}" + ex.ToString());
            }
        }


        #region Production Issue

        [HttpPost("GetProductionOrderList")]
        public async Task<ActionResult<IEnumerable<ProductionOrderList>>> GetProductionOrderList()
        {
            SqlConnection QITcon;
            SqlDataAdapter oAdptr;
            try
            {
                _logger.LogInformation(" Calling ProductionController : GetProductionOrderList() ");
                DataTable dtData = new DataTable();
                QITcon = new SqlConnection(_QIT_connection);

                _Query = @" 
                SELECT T0.[DocEntry], T0.[DocNum], T1.[SeriesName], T0.[Type], T0.[DueDate], T0.[ItemCode], T0.[ProdName], T0.PlannedQty,  
                       T0.[Comments], T0.[StartDate], T0.[Priority] , T1.[Series] 
                FROM  " + Global.SAP_DB + @".dbo.OWOR T0 INNER JOIN " + Global.SAP_DB + @".dbo.NNM1 T1 ON T0.[Series] = T1.[Series]   
                WHERE T0.[Status] = 'R' AND T0.[Type] = 'S' AND   
				      EXISTS 
				      (
					      SELECT U0.[DocEntry] FROM " + Global.SAP_DB + @".dbo.WOR1 U0  
					      WHERE T0.[DocEntry] = U0.[DocEntry] AND U0.[IssueType] = 'M' AND U0.[PlannedQty] > U0.[IssuedQty]  
				      )  
                ORDER BY T0.[DocNum],T0.[DocEntry]
                ";

                _logger.LogInformation(" ProductionController : GetProductionOrderList() Query : {q} ", _Query.ToString());
                QITcon.Open();
                oAdptr = new SqlDataAdapter(_Query, QITcon);
                oAdptr.Fill(dtData);
                QITcon.Close();

                if (dtData.Rows.Count > 0)
                {
                    List<ProductionOrderList> obj = new List<ProductionOrderList>();
                    dynamic arData = JsonConvert.SerializeObject(dtData);
                    obj = JsonConvert.DeserializeObject<List<ProductionOrderList>>(arData.ToString());
                    return obj;
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "No data found" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(" Error in ProductionController : GetProductionOrderList() :: {Error}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
            }
        }


        [HttpPost("GetProductionOrderItemList")]
        public async Task<ActionResult<IEnumerable<ProductionOrderItemList>>> GetProductionOrderItemList(ProOrderItemcls payload)
        {
            SqlConnection QITcon;
            SqlDataAdapter oAdptr;
            try
            {
                _logger.LogInformation(" Calling ProductionController : GetProductionOrderItemList() ");
                System.Data.DataTable dtData = new DataTable();
                QITcon = new SqlConnection(_QIT_connection);

                var proOrderItems = payload.proOrderItem;
                var _DocEntryFilter = string.Join(", ", proOrderItems.Select(item => item.DocEntry));

                if (_DocEntryFilter.Length > 0)
                {
                    _Query = @" 
                    SELECT T0.[DocNum], T1.[LineNum], T1.[ItemCode], T1.[ItemName], T2.[DocItemType], T1.[IssuedQty], T1.[wareHouse], 
                           T1.[DocEntry], T1.[PlannedQty], T0.[Type], T0.[DocEntry], 
	                       T1.[StartDate], T1.[EndDate], T3.[SeqNum], T4.[Code], T3.[Name] 
                    FROM  " + Global.SAP_DB + @".dbo.OWOR T0  
	                      INNER JOIN " + Global.SAP_DB + @".dbo.WOR1 T1 ON T0.[DocEntry] = T1.[DocEntry]   
	                      INNER JOIN " + Global.SAP_DB + @".dbo.B1_DocItemView T2 ON T1.[ItemType] = T2.[DocItemType] AND  
                                     T1.[ItemCode] = T2.[DocItemCode]    
	                      LEFT OUTER JOIN " + Global.SAP_DB + @".dbo.WOR4 T3 ON T1.[StageId] = T3.[StageId] AND  
                                     T1.[DocEntry] = T3.[DocEntry]    
	                      LEFT OUTER JOIN " + Global.SAP_DB + @".dbo.ORST T4 ON T3.[StgEntry] = T4.[AbsEntry]   
                    WHERE T1.[IssueType] = 'M' AND T0.[DocEntry] IN (" + _DocEntryFilter + @" ) AND  
                          (((T0.[Type] = 'S' OR  T0.[Type] = 'P' ) AND  
                          T1.[PlannedQty] > T1.[IssuedQty] ) OR (T0.[Type] = 'D' AND T1.[IssuedQty] > 0.00 )) 
                    ORDER BY T1.[DocEntry],T1.[VisOrder],T1.[LineNum]  
                    ";

                    _logger.LogInformation(" ProductionController : GetProductionOrderItemList() Query : {q} ", _Query.ToString());
                    QITcon.Open();
                    oAdptr = new SqlDataAdapter(_Query, QITcon);
                    oAdptr.Fill(dtData);
                    QITcon.Close();

                    if (dtData.Rows.Count > 0)
                    {
                        List<ProductionOrderItemList> obj = new List<ProductionOrderItemList>();
                        dynamic arData = JsonConvert.SerializeObject(dtData);
                        obj = JsonConvert.DeserializeObject<List<ProductionOrderItemList>>(arData.ToString());
                        return obj;
                    }
                    else
                    {
                        return BadRequest(new { StatusCode = "400", StatusMsg = "No data found" });
                    }
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "No data found" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(" Error in ProductionController : GetProductionOrderItemList() :: {Error}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
            }
        }


        [HttpPost("ValidateItemQR")]
        public async Task<ActionResult<IEnumerable<ValidItemCls>>> ValidateItemQR(ProItemValidateCls payload)
        {
            SqlConnection QITcon;
            SqlConnection SAPcon;
            SqlDataAdapter oAdptr;
            try
            {
                _logger.LogInformation(" Calling ProductionController : ValidateItemQR() ");
                List<ValidItemCls> obj = new List<ValidItemCls>();

                #region Get Item Data from QR
                System.Data.DataTable dtQrData = new DataTable();
                QITcon = new SqlConnection(_QIT_connection);
                _Query = @" SELECT * FROM QIT_QR_Detail WHERE QRCodeID = @qr ";
                _logger.LogInformation(" ProductionController : ValidateItemQR : Item QR Query : {q} ", _Query.ToString());
                QITcon.Open();
                oAdptr = new SqlDataAdapter(_Query, QITcon);
                oAdptr.SelectCommand.Parameters.AddWithValue("@qr", payload.DetailQRCodeID.Replace(" ", "~"));
                oAdptr.Fill(dtQrData);
                QITcon.Close();
                #endregion

                if (dtQrData.Rows.Count > 0) // PO QR
                {
                    System.Data.DataTable dtData = new DataTable();
                    SAPcon = new SqlConnection(_connection);

                    if (dtQrData.Rows[0]["BatchSerialNo"].ToString() == "-")
                    {
                        _Query = @"
                        SELECT -- A.BranchID, A.GateInNo, A.QRCodeID DetailQRCodeID, B.QRCodeID HeaderQRCodeID, A.QRMngBy,
	                            C.DocEntry, C.DocNum, A.GateInNo, A.QRCodeID DetailQRCodeID,
                                D.ItemCode, E.ItemName, D.PlannedQty, D.IssuedQty, '' IssQty,
                                D.UomCode, D.Project, E.ItemMngBy, A.BatchSerialNo, D.LineNum, A.Qty QRQty, D.wareHouse BatchWhsCode,
		                        ISNULL(( SELECT AvailQty FROM 
                                  (
                                        SELECT A.WhsCode, A.POtoGRPOQty, B.InQty, C.OutQty, InQty - OutQty AvailQty 
			                            FROM 
			                            (
				                            SELECT  D.wareHouse WhsCode, sum(Z.Qty) POtoGRPOQty FROM " + Global.QIT_DB + @".dbo.[QIT_QRStock_InvTrans] Z
				                            WHERE Z.QRCodeID = @dQR and Z.FromObjType = '22' and Z.ToObjType = '67'
			                            ) A INNER JOIN 
			                            (
				                            SELECT  D.wareHouse WhsCode, sum(Z.Qty) InQty FROM " + Global.QIT_DB + @".dbo.[QIT_QRStock_InvTrans] Z
				                            WHERE Z.QRCodeID = @dQR and (Z.FromObjType <> '22') AND 
                                                  Z.ToWhs collate SQL_Latin1_General_CP850_CI_AS = D.wareHouse
			                            ) B ON A.WhsCode = B.WhsCode INNER JOIN
			                            (
				                            SELECT  D.wareHouse WhsCode, sum(Z.Qty) OutQty from " + Global.QIT_DB + @".dbo.[QIT_QRStock_InvTrans] Z
				                            WHERE Z.QRCodeID = @dQR and (Z.FromObjType <> '22') and 
                                                  Z.FromWhs collate SQL_Latin1_General_CP850_CI_AS = D.wareHouse
			                            ) as C ON B.WhsCode = C.WhsCode
		                          ) AS C
                                ),0) BatchAvailQty, D.wareHouse ProWhsCode,
                                ISNULL((select sum(IssQty) from " + Global.QIT_DB + @".dbo.QIT_Draft_Issue t 
		                                where t.QRCodeID  = A.QRCodeID and t.BatchSerialNo = A.BatchSerialNo AND 
			                                  t.ItemCode collate SQL_Latin1_General_CP850_CI_AS = D.ItemCode   
			                    ),0) DraftIssueQty, Z.Project QRProject
                        FROM " + Global.QIT_DB + @".dbo.QIT_QR_Detail A  
	                            INNER JOIN " + Global.QIT_DB + @".dbo.QIT_QR_Header B ON A.HeaderSrNo = B.HeaderSrNo
                                INNER JOIN " + Global.QIT_DB + @".dbo.QIT_GateIN Z on Z.GateInNo = A.GateInNo AND Z.ItemCode = A.ItemCode AND A.LineNum = Z.LineNum
	                            INNER JOIN OWOR C ON 1=1 and C.DocEntry = @docEntry
	                            INNER JOIN WOR1 D ON C.DocEntry = D.DocEntry AND D.ItemCode collate SQL_Latin1_General_CP1_CI_AS = A.ItemCode
                                INNER JOIN " + Global.QIT_DB + @".dbo.QIT_Item_Master E on E.ItemCode = A.ItemCode 
                        WHERE A.QRCodeID = @dQR AND A.ItemCode = @itemCode
                        ";
                    }
                    else
                    {
                        _Query = @"
                    SELECT *,
                           ISNULL(( SELECT SUM(ISNULL(Qty,0)) from " + Global.QIT_DB + @".dbo.QIT_Temp_ProIssue 
                                    WHERE ProOrdDocNum = A.DocNum AND ItemCode collate SQL_Latin1_General_CP850_CI_AS = A.ItemCode
                           ),0) ProOrdWiseTempQty,
                           ISNULL(( SELECT SUM(ISNULL(Qty,0)) from " + Global.QIT_DB + @".dbo.QIT_Temp_ProIssue 
                                    WHERE ItemCode collate SQL_Latin1_General_CP850_CI_AS = A.ItemCode
                           ),0) ItemTempQty
                    FROM (
                    SELECT -- A.BranchID, A.GateInNo, A.QRCodeID DetailQRCodeID, B.QRCodeID HeaderQRCodeID, A.QRMngBy,
	                       C.DocEntry, C.DocNum, A.GateInNo, A.QRCodeID DetailQRCodeID,
                           D.ItemCode, E.ItemName, D.PlannedQty, D.IssuedQty, '' IssQty,
                           E.UomCode, D.Project, E.ItemMngBy, A.BatchSerialNo, D.LineNum, A.Qty QRQty,
                           F.WhsCode BatchWhsCode, F.Quantity BatchAvailQty, D.wareHouse ProWhsCode,
                           ISNULL((select sum(IssQty) from " + Global.QIT_DB + @".dbo.QIT_Draft_Issue t 
		                           where t.QRCodeID  = A.QRCodeID and t.BatchSerialNo = A.BatchSerialNo and 
			                             t.ItemCode collate SQL_Latin1_General_CP850_CI_AS = D.ItemCode and 
			                             t.WhsCode collate SQL_Latin1_General_CP850_CI_AS= D.wareHouse and 
			                             t.WhsCode collate SQL_Latin1_General_CP850_CI_AS = F.WhsCode),0) DraftIssueQtyByQR,
                           ISNULL((select sum(IssQty) from " + Global.QIT_DB + @".dbo.QIT_Draft_Issue t 
		                           where t.QRCodeID  = A.QRCodeID and t.BatchSerialNo = A.BatchSerialNo and 
                                         t.ProOrdDocEntry = C.DocEntry AND
			                             t.ItemCode collate SQL_Latin1_General_CP850_CI_AS = D.ItemCode and 
			                             t.WhsCode collate SQL_Latin1_General_CP850_CI_AS= D.wareHouse and 
			                             t.WhsCode collate SQL_Latin1_General_CP850_CI_AS = F.WhsCode),0) DraftIssueQtyByQRPro,
                           Z.Project QRProject,
                           ISNULL(H.ToBinAbsEntry,0) FromBin, (select BinCode from OBIN where AbsEntry = H.ToBinAbsEntry) FromBinCode
                    FROM " + Global.QIT_DB + @".dbo.QIT_QR_Detail A  
	                     INNER JOIN " + Global.QIT_DB + @".dbo.QIT_QR_Header B ON A.HeaderSrNo = B.HeaderSrNo
                         INNER JOIN " + Global.QIT_DB + @".dbo.QIT_GateIN Z on Z.GateInNo = A.GateInNo AND Z.ItemCode = A.ItemCode AND A.LineNum = Z.LineNum
	                     INNER JOIN OWOR C ON 1=1 and C.DocEntry = @docEntry
	                     INNER JOIN WOR1 D ON C.DocEntry = D.DocEntry AND D.ItemCode collate SQL_Latin1_General_CP1_CI_AS = A.ItemCode
                         INNER JOIN " + Global.QIT_DB + @".dbo.QIT_Item_Master E on E.ItemCode = A.ItemCode 
                         INNER JOIN [OBTQ] F ON 1=1   
                         INNER  JOIN [dbo].[OBTN] G ON F.[ItemCode] = G.[ItemCode] AND F.[SysNumber] = G.[SysNumber] 
                         INNER JOIN " + Global.QIT_DB + @".dbo.QIT_QRStock_InvTrans H ON H.QRCodeID = A.QRCodeID and 
                                    H.TransId=(SELECT MAX(A1.TRansId) FROM " + Global.QIT_DB + @".dbo.QIT_QRStock_InvTrans A1 WHERE A1.QRCodeId = @dQR )
                    WHERE A.QRCodeID = @dQR AND F.Quantity <> 0 AND G.DistNumber = @batch AND F.ItemCode = @itemCode

                    UNION

                    SELECT -- A.BranchID, A.GateInNo, A.QRCodeID DetailQRCodeID, B.QRCodeID HeaderQRCodeID, A.QRMngBy,
	                       C.DocEntry, C.DocNum, A.GateInNo, A.QRCodeID DetailQRCodeID,
                           D.ItemCode, E.ItemName, D.PlannedQty, D.IssuedQty, '' IssQty,
                           E.UomCode, D.Project, E.ItemMngBy, A.BatchSerialNo, D.LineNum, A.Qty QRQty,
                           F.WhsCode BatchWhsCode, F.Quantity BatchAvailQty, D.wareHouse ProWhsCode,
                           ISNULL((select sum(IssQty) from " + Global.QIT_DB + @".dbo.QIT_Draft_Issue t 
		                           where t.QRCodeID  = A.QRCodeID and t.BatchSerialNo = A.BatchSerialNo and 
			                             t.ItemCode collate SQL_Latin1_General_CP850_CI_AS = D.ItemCode and 
			                             t.WhsCode collate SQL_Latin1_General_CP850_CI_AS= D.wareHouse and 
			                             t.WhsCode collate SQL_Latin1_General_CP850_CI_AS = F.WhsCode),0) DraftIssueQtyByQR, 
                           ISNULL((select sum(IssQty) from " + Global.QIT_DB + @".dbo.QIT_Draft_Issue t 
		                           where t.QRCodeID  = A.QRCodeID and t.BatchSerialNo = A.BatchSerialNo and 
                                         t.ProOrdDocEntry = C.DocEntry AND
			                             t.ItemCode collate SQL_Latin1_General_CP850_CI_AS = D.ItemCode and 
			                             t.WhsCode collate SQL_Latin1_General_CP850_CI_AS= D.wareHouse and 
			                             t.WhsCode collate SQL_Latin1_General_CP850_CI_AS = F.WhsCode),0) DraftIssueQtyByQRPro,
                           Z.Project QRProject,
                           ISNULL(H.ToBinAbsEntry,0) FromBin, (select BinCode from OBIN where AbsEntry = H.ToBinAbsEntry) FromBinCode
                    FROM " + Global.QIT_DB + @".dbo.QIT_QR_Detail A  
	                     INNER JOIN " + Global.QIT_DB + @".dbo.QIT_QR_Header B ON A.HeaderSrNo = B.HeaderSrNo
                         INNER JOIN " + Global.QIT_DB + @".dbo.QIT_GateIN Z on Z.GateInNo = A.GateInNo AND Z.ItemCode = A.ItemCode AND A.LineNum = Z.LineNum
	                     INNER JOIN OWOR C ON 1=1 and C.DocEntry = @docEntry
	                     INNER JOIN WOR1 D ON C.DocEntry = D.DocEntry AND D.ItemCode collate SQL_Latin1_General_CP1_CI_AS = A.ItemCode
                         INNER JOIN " + Global.QIT_DB + @".dbo.QIT_Item_Master E on E.ItemCode = A.ItemCode 
                         INNER JOIN [OSRQ] F ON 1=1   
                         INNER JOIN [dbo].[OSRN] G ON F.[ItemCode] = G.[ItemCode] AND F.[SysNumber] = G.[SysNumber] 
                         INNER JOIN " + Global.QIT_DB + @".dbo.QIT_QRStock_InvTrans H ON H.QRCodeID = A.QRCodeID and 
                                    H.TransId=(SELECT MAX(A1.TRansId) FROM " + Global.QIT_DB + @".dbo.QIT_QRStock_InvTrans A1 WHERE A1.QRCodeId = @dQR )
                    WHERE A.QRCodeID = @dQR AND F.Quantity <> 0 AND G.DistNumber = @batch AND F.ItemCode = @itemCode
                    ) AS A
                    ";
                    }

                    _logger.LogInformation(" ProductionController : ValidateItemQR() Query : {q} ", _Query.ToString());
                    SAPcon.Open();
                    oAdptr = new SqlDataAdapter(_Query, SAPcon);
                    oAdptr.SelectCommand.Parameters.AddWithValue("@docEntry", payload.ProDocEntry);
                    oAdptr.SelectCommand.Parameters.AddWithValue("@dQR", payload.DetailQRCodeID.Replace(" ", "~"));
                    if (dtQrData.Rows[0]["BatchSerialNo"].ToString() != "-")
                        oAdptr.SelectCommand.Parameters.AddWithValue("@batch", dtQrData.Rows[0]["BatchSerialNo"].ToString());
                    oAdptr.SelectCommand.Parameters.AddWithValue("@itemCode", dtQrData.Rows[0]["ItemCode"].ToString());
                    oAdptr.Fill(dtData);
                    SAPcon.Close();

                    if (dtData.Rows.Count > 0)
                    {
                        var whsCodes = dtData.AsEnumerable().Select(row => row.Field<string>("BatchWhsCode")).Distinct().ToList();

                        var matchingRows = dtData.AsEnumerable()
                                           .Where(row => row.Field<string>("BatchWhsCode") == row.Field<string>("ProWhsCode"))
                                           .ToList();

                        if (matchingRows.Any())
                        {
                            DataTable dtValidItem = dtData.AsEnumerable()
                                            .Where(row => row.Field<string>("BatchWhsCode") == row.Field<string>("ProWhsCode"))
                                            .CopyToDataTable();

                            if (dtValidItem.Rows.Count > 0)
                            {
                                if (dtValidItem.Rows[0]["QRProject"].ToString().ToLower() == "common")
                                {
                                    dynamic arData = JsonConvert.SerializeObject(dtValidItem);
                                    obj = JsonConvert.DeserializeObject<List<ValidItemCls>>(arData.ToString().Replace("~", " "));
                                    return obj;
                                }
                                if (dtValidItem.Rows[0]["QRProject"].ToString().ToLower() == dtValidItem.Rows[0]["Project"].ToString().ToLower())
                                {
                                    dynamic arData = JsonConvert.SerializeObject(dtValidItem);
                                    obj = JsonConvert.DeserializeObject<List<ValidItemCls>>(arData.ToString().Replace("~", " "));
                                    return obj;
                                }
                                else
                                {
                                    return BadRequest(new
                                    {
                                        StatusCode = "400",
                                        StatusMsg = "QR Project : " + dtData.Rows[0]["QRProject"].ToString() + Environment.NewLine +
                                                    "Production Item Project : " + dtData.Rows[0]["Project"].ToString()
                                    });
                                }
                            }
                            else
                            {
                                return BadRequest(new
                                {
                                    StatusCode = "400",
                                    StatusMsg = "Item is not availbale in Production warehouse" + Environment.NewLine +
                                                "Production Warehouse : " + dtData.Rows[0]["ProWhsCode"].ToString() + Environment.NewLine +
                                                "Item QR Warehouse : " + string.Join(",", whsCodes)
                                });
                            }
                        }
                        else
                        {
                            return BadRequest(new
                            {
                                StatusCode = "400",
                                StatusMsg = "Item is not availbale in Production warehouse" + Environment.NewLine +
                                               "Production Warehouse : " + dtData.Rows[0]["ProWhsCode"].ToString() + Environment.NewLine +
                                               "Item QR Warehouse : " + string.Join(",", whsCodes)
                            });
                        }
                    }
                    else
                    {
                        return BadRequest(new { StatusCode = "400", StatusMsg = "No data found" });
                    }
                }
                else
                {
                    #region Get Item Data from QR
                    dtQrData = new DataTable();
                    QITcon = new SqlConnection(_QIT_connection);
                    _Query = @" SELECT * FROM QIT_Production_QR_Detail WHERE QRCodeID = @qr ";
                    _logger.LogInformation(" ProductionController : ValidateItemQR : Product QR Query : {q} ", _Query.ToString());
                    QITcon.Open();
                    oAdptr = new SqlDataAdapter(_Query, QITcon);
                    oAdptr.SelectCommand.Parameters.AddWithValue("@qr", payload.DetailQRCodeID.Replace(" ", "~"));
                    oAdptr.Fill(dtQrData);
                    QITcon.Close();
                    #endregion

                    if (dtQrData.Rows.Count > 0) // PRO QR
                    {
                        System.Data.DataTable dtData = new DataTable();
                        SAPcon = new SqlConnection(_connection);

                        if (dtQrData.Rows[0]["BatchSerialNo"].ToString() == "-")
                        {
                            _Query = @"
                        SELECT -- A.BranchID, A.GateInNo, A.QRCodeID DetailQRCodeID, B.QRCodeID HeaderQRCodeID, A.QRMngBy,
	                            C.DocEntry, C.DocNum, A.GateInNo, A.QRCodeID DetailQRCodeID,
                                D.ItemCode, E.ItemName, D.PlannedQty, D.IssuedQty, '' IssQty,
                                D.UomCode, D.Project, E.ItemMngBy, A.BatchSerialNo, D.LineNum, A.Qty QRQty, D.wareHouse BatchWhsCode,
		                        ISNULL(( SELECT AvailQty FROM 
                                  (
                                        SELECT A.WhsCode, A.POtoGRPOQty, B.InQty, C.OutQty, InQty - OutQty AvailQty 
			                            FROM 
			                            (
				                            SELECT  D.wareHouse WhsCode, sum(Z.Qty) POtoGRPOQty FROM " + Global.QIT_DB + @".dbo.[QIT_QRStock_InvTrans] Z
				                            WHERE Z.QRCodeID = @dQR and Z.FromObjType = '22' and Z.ToObjType = '67'
			                            ) A INNER JOIN 
			                            (
				                            SELECT  D.wareHouse WhsCode, sum(Z.Qty) InQty FROM " + Global.QIT_DB + @".dbo.[QIT_QRStock_InvTrans] Z
				                            WHERE Z.QRCodeID = @dQR and (Z.FromObjType <> '22') AND 
                                                  Z.ToWhs collate SQL_Latin1_General_CP850_CI_AS = D.wareHouse
			                            ) B ON A.WhsCode = B.WhsCode INNER JOIN
			                            (
				                            SELECT  D.wareHouse WhsCode, sum(Z.Qty) OutQty from " + Global.QIT_DB + @".dbo.[QIT_QRStock_InvTrans] Z
				                            WHERE Z.QRCodeID = @dQR and (Z.FromObjType <> '22') and 
                                                  Z.FromWhs collate SQL_Latin1_General_CP850_CI_AS = D.wareHouse
			                            ) as C ON B.WhsCode = C.WhsCode
		                          ) AS C
                                ),0) BatchAvailQty, D.wareHouse ProWhsCode,
                                ISNULL((select sum(IssQty) from " + Global.QIT_DB + @".dbo.QIT_Draft_Issue t 
		                                where t.QRCodeID  = A.QRCodeID and t.BatchSerialNo = A.BatchSerialNo AND 
			                                  t.ItemCode collate SQL_Latin1_General_CP850_CI_AS = D.ItemCode   
			                    ),0) DraftIssueQty, Z.Project QRProject
                        FROM " + Global.QIT_DB + @".dbo.QIT_Production_QR_Detail A  
	                            INNER JOIN " + Global.QIT_DB + @".dbo.QIT_Production_QR_Header B ON A.HeaderSrNo = B.HeaderSrNo
                                INNER JOIN " + Global.QIT_DB + @".dbo.QIT_GateIN Z on Z.GateInNo = A.GateInNo AND Z.ItemCode = A.ItemCode AND A.LineNum = Z.LineNum
	                            INNER JOIN OWOR C ON 1=1 and C.DocEntry = @docEntry
	                            INNER JOIN WOR1 D ON C.DocEntry = D.DocEntry AND D.ItemCode collate SQL_Latin1_General_CP1_CI_AS = A.ItemCode
                                INNER JOIN " + Global.QIT_DB + @".dbo.QIT_Item_Master E on E.ItemCode = A.ItemCode 
                        WHERE A.QRCodeID = @dQR AND A.ItemCode = @itemCode
                        ";
                        }
                        else
                        {
                            _Query = @"
                    SELECT *,
                           ISNULL(( SELECT SUM(ISNULL(Qty,0)) from " + Global.QIT_DB + @".dbo.QIT_Temp_ProIssue 
                                    WHERE ProOrdDocNum = A.DocNum AND ItemCode collate SQL_Latin1_General_CP850_CI_AS = A.ItemCode
                           ),0) ProOrdWiseTempQty,
                           ISNULL(( SELECT SUM(ISNULL(Qty,0)) from " + Global.QIT_DB + @".dbo.QIT_Temp_ProIssue 
                                    WHERE ItemCode collate SQL_Latin1_General_CP850_CI_AS = A.ItemCode
                           ),0) ItemTempQty
                    FROM (
                    SELECT -- A.BranchID, A.GateInNo, A.QRCodeID DetailQRCodeID, B.QRCodeID HeaderQRCodeID, A.QRMngBy,
	                       C.DocEntry, C.DocNum, A.RecNo GateRecNo, A.QRCodeID DetailQRCodeID,
                           D.ItemCode, E.ItemName, D.PlannedQty, D.IssuedQty, '' IssQty,
                           E.UomCode, D.Project, E.ItemMngBy, A.BatchSerialNo, D.LineNum, A.Qty QRQty,
                           F.WhsCode BatchWhsCode, F.Quantity BatchAvailQty, D.wareHouse ProWhsCode,
                           ISNULL((select sum(IssQty) from " + Global.QIT_DB + @".dbo.QIT_Draft_Issue t 
		                           where t.QRCodeID  = A.QRCodeID and t.BatchSerialNo = A.BatchSerialNo and 
			                             t.ItemCode collate SQL_Latin1_General_CP850_CI_AS = D.ItemCode and 
			                             t.WhsCode collate SQL_Latin1_General_CP850_CI_AS= D.wareHouse and 
			                             t.WhsCode collate SQL_Latin1_General_CP850_CI_AS = F.WhsCode),0) DraftIssueQtyByQR,
                           ISNULL((select sum(IssQty) from " + Global.QIT_DB + @".dbo.QIT_Draft_Issue t 
		                           where t.QRCodeID  = A.QRCodeID and t.BatchSerialNo = A.BatchSerialNo and 
                                         t.ProOrdDocEntry = C.DocEntry AND
			                             t.ItemCode collate SQL_Latin1_General_CP850_CI_AS = D.ItemCode and 
			                             t.WhsCode collate SQL_Latin1_General_CP850_CI_AS= D.wareHouse and 
			                             t.WhsCode collate SQL_Latin1_General_CP850_CI_AS = F.WhsCode),0) DraftIssueQtyByQRPro,
                           C.Project QRProject,
                           ISNULL(H.ToBinAbsEntry,0) FromBin, (select BinCode from OBIN where AbsEntry = H.ToBinAbsEntry) FromBinCode
                    FROM " + Global.QIT_DB + @".dbo.QIT_Production_QR_Detail A  
	                     INNER JOIN " + Global.QIT_DB + @".dbo.QIT_Production_QR_Header B ON A.HeaderSrNo = B.HeaderSrNo
	                     INNER JOIN OWOR C ON 1=1 and C.DocEntry = @docEntry
	                     INNER JOIN WOR1 D ON C.DocEntry = D.DocEntry AND D.ItemCode collate SQL_Latin1_General_CP1_CI_AS = A.ItemCode
                         INNER JOIN " + Global.QIT_DB + @".dbo.QIT_Item_Master E on E.ItemCode = A.ItemCode 
                         INNER JOIN [OBTQ] F ON 1=1   
                         INNER  JOIN [dbo].[OBTN] G ON F.[ItemCode] = G.[ItemCode] AND F.[SysNumber] = G.[SysNumber] 
                         INNER JOIN " + Global.QIT_DB + @".dbo.QIT_ProQRStock_InvTrans H ON H.QRCodeID = A.QRCodeID and 
                                    H.TransId=(SELECT MAX(A1.TRansId) FROM " + Global.QIT_DB + @".dbo.QIT_ProQRStock_InvTrans A1 WHERE A1.QRCodeId = @dQR )
                    WHERE A.QRCodeID = @dQR AND F.Quantity <> 0 AND G.DistNumber = @batch AND F.ItemCode = @itemCode

                    UNION

                    SELECT -- A.BranchID, A.GateInNo, A.QRCodeID DetailQRCodeID, B.QRCodeID HeaderQRCodeID, A.QRMngBy,
	                       C.DocEntry, C.DocNum, A.RecNo GateRecNo, A.QRCodeID DetailQRCodeID,
                           D.ItemCode, E.ItemName, D.PlannedQty, D.IssuedQty, '' IssQty,
                           E.UomCode, D.Project, E.ItemMngBy, A.BatchSerialNo, D.LineNum, A.Qty QRQty,
                           F.WhsCode BatchWhsCode, F.Quantity BatchAvailQty, D.wareHouse ProWhsCode,
                           ISNULL((select sum(IssQty) from " + Global.QIT_DB + @".dbo.QIT_Draft_Issue t 
		                           where t.QRCodeID  = A.QRCodeID and t.BatchSerialNo = A.BatchSerialNo and 
			                             t.ItemCode collate SQL_Latin1_General_CP850_CI_AS = D.ItemCode and 
			                             t.WhsCode collate SQL_Latin1_General_CP850_CI_AS= D.wareHouse and 
			                             t.WhsCode collate SQL_Latin1_General_CP850_CI_AS = F.WhsCode),0) DraftIssueQtyByQR, 
                           ISNULL((select sum(IssQty) from " + Global.QIT_DB + @".dbo.QIT_Draft_Issue t 
		                           where t.QRCodeID  = A.QRCodeID and t.BatchSerialNo = A.BatchSerialNo and 
                                         t.ProOrdDocEntry = C.DocEntry AND
			                             t.ItemCode collate SQL_Latin1_General_CP850_CI_AS = D.ItemCode and 
			                             t.WhsCode collate SQL_Latin1_General_CP850_CI_AS= D.wareHouse and 
			                             t.WhsCode collate SQL_Latin1_General_CP850_CI_AS = F.WhsCode),0) DraftIssueQtyByQRPro,
                           C.Project QRProject,
                           ISNULL(H.ToBinAbsEntry,0) FromBin, (select BinCode from OBIN where AbsEntry = H.ToBinAbsEntry) FromBinCode
                    FROM " + Global.QIT_DB + @".dbo.QIT_Production_QR_Detail A  
	                     INNER JOIN " + Global.QIT_DB + @".dbo.QIT_Production_QR_Header B ON A.HeaderSrNo = B.HeaderSrNo
	                     INNER JOIN OWOR C ON 1=1 and C.DocEntry = @docEntry
	                     INNER JOIN WOR1 D ON C.DocEntry = D.DocEntry AND D.ItemCode collate SQL_Latin1_General_CP1_CI_AS = A.ItemCode
                         INNER JOIN " + Global.QIT_DB + @".dbo.QIT_Item_Master E on E.ItemCode = A.ItemCode 
                         INNER JOIN [OSRQ] F ON 1=1   
                         INNER JOIN [dbo].[OSRN] G ON F.[ItemCode] = G.[ItemCode] AND F.[SysNumber] = G.[SysNumber] 
                         INNER JOIN " + Global.QIT_DB + @".dbo.QIT_ProQRStock_InvTrans H ON H.QRCodeID = A.QRCodeID and 
                                    H.TransId=(SELECT MAX(A1.TRansId) FROM " + Global.QIT_DB + @".dbo.QIT_ProQRStock_InvTrans A1 WHERE A1.QRCodeId = @dQR )
                    WHERE A.QRCodeID = @dQR AND F.Quantity <> 0 AND G.DistNumber = @batch AND F.ItemCode = @itemCode
                    ) AS A
                    ";
                        }

                        _logger.LogInformation(" ProductionController : ValidateItemQR() Query : {q} ", _Query.ToString());
                        SAPcon.Open();
                        oAdptr = new SqlDataAdapter(_Query, SAPcon);
                        oAdptr.SelectCommand.Parameters.AddWithValue("@docEntry", payload.ProDocEntry);
                        oAdptr.SelectCommand.Parameters.AddWithValue("@dQR", payload.DetailQRCodeID.Replace(" ", "~"));
                        if (dtQrData.Rows[0]["BatchSerialNo"].ToString() != "-")
                            oAdptr.SelectCommand.Parameters.AddWithValue("@batch", dtQrData.Rows[0]["BatchSerialNo"].ToString());
                        oAdptr.SelectCommand.Parameters.AddWithValue("@itemCode", dtQrData.Rows[0]["ItemCode"].ToString());
                        oAdptr.Fill(dtData);
                        SAPcon.Close();

                        if (dtData.Rows.Count > 0)
                        {
                            var whsCodes = dtData.AsEnumerable().Select(row => row.Field<string>("BatchWhsCode")).Distinct().ToList();

                            var matchingRows = dtData.AsEnumerable()
                                               .Where(row => row.Field<string>("BatchWhsCode") == row.Field<string>("ProWhsCode"))
                                               .ToList();

                            if (matchingRows.Any())
                            {
                                DataTable dtValidItem = dtData.AsEnumerable()
                                                .Where(row => row.Field<string>("BatchWhsCode") == row.Field<string>("ProWhsCode"))
                                                .CopyToDataTable();

                                if (dtValidItem.Rows.Count > 0)
                                {
                                    if (dtValidItem.Rows[0]["QRProject"].ToString().ToLower() == "common")
                                    {
                                        dynamic arData = JsonConvert.SerializeObject(dtValidItem);
                                        obj = JsonConvert.DeserializeObject<List<ValidItemCls>>(arData.ToString().Replace("~", " "));
                                        return obj;
                                    }
                                    if (dtValidItem.Rows[0]["QRProject"].ToString().ToLower() == dtValidItem.Rows[0]["Project"].ToString().ToLower())
                                    {
                                        dynamic arData = JsonConvert.SerializeObject(dtValidItem);
                                        obj = JsonConvert.DeserializeObject<List<ValidItemCls>>(arData.ToString().Replace("~", " "));
                                        return obj;
                                    }
                                    else
                                    {
                                        return BadRequest(new
                                        {
                                            StatusCode = "400",
                                            StatusMsg = "QR Project : " + dtData.Rows[0]["QRProject"].ToString() + Environment.NewLine +
                                                        "Production Item Project : " + dtData.Rows[0]["Project"].ToString()
                                        });
                                    }
                                }
                                else
                                {
                                    return BadRequest(new
                                    {
                                        StatusCode = "400",
                                        StatusMsg = "Item is not availbale in Production warehouse" + Environment.NewLine +
                                                    "Production Warehouse : " + dtData.Rows[0]["ProWhsCode"].ToString() + Environment.NewLine +
                                                    "Item QR Warehouse : " + string.Join(",", whsCodes)
                                    });
                                }
                            }
                            else
                            {
                                return BadRequest(new
                                {
                                    StatusCode = "400",
                                    StatusMsg = "Item is not availbale in Production warehouse" + Environment.NewLine +
                                                   "Production Warehouse : " + dtData.Rows[0]["ProWhsCode"].ToString() + Environment.NewLine +
                                                   "Item QR Warehouse : " + string.Join(",", whsCodes)
                                });
                            }
                        }
                        else
                        {
                            return BadRequest(new { StatusCode = "400", StatusMsg = "No data found" });
                        }
                    }
                    else
                    {
                        #region Get Item Data from QR
                        dtQrData = new DataTable();
                        QITcon = new SqlConnection(_QIT_connection);
                        _Query = @" SELECT * FROM QIT_OpeningStock_QR_Detail WHERE QRCodeID = @qr ";
                        _logger.LogInformation(" ProductionController : ValidateItemQR : OS QR Query : {q} ", _Query.ToString());
                        QITcon.Open();
                        oAdptr = new SqlDataAdapter(_Query, QITcon);
                        oAdptr.SelectCommand.Parameters.AddWithValue("@qr", payload.DetailQRCodeID.Replace(" ", "~"));
                        oAdptr.Fill(dtQrData);
                        QITcon.Close();
                        #endregion

                        if (dtQrData.Rows.Count > 0) // OS QR
                        {
                            System.Data.DataTable dtData = new DataTable();
                            SAPcon = new SqlConnection(_connection);

                            if (dtQrData.Rows[0]["BatchSerialNo"].ToString() == "-")
                            {
                                _Query = @"
                        SELECT -- A.BranchID, A.GateInNo, A.QRCodeID DetailQRCodeID, B.QRCodeID HeaderQRCodeID, A.QRMngBy,
	                            C.DocEntry, C.DocNum, A.GateInNo, A.QRCodeID DetailQRCodeID,
                                D.ItemCode, E.ItemName, D.PlannedQty, D.IssuedQty, '' IssQty,
                                D.UomCode, D.Project, E.ItemMngBy, A.BatchSerialNo, D.LineNum, A.Qty QRQty, D.wareHouse BatchWhsCode,
		                        ISNULL(( SELECT AvailQty FROM 
                                  (
                                        SELECT A.WhsCode, A.POtoGRPOQty, B.InQty, C.OutQty, InQty - OutQty AvailQty 
			                            FROM 
			                            (
				                            SELECT  D.wareHouse WhsCode, sum(Z.Qty) POtoGRPOQty FROM " + Global.QIT_DB + @".dbo.[QIT_QRStock_InvTrans] Z
				                            WHERE Z.QRCodeID = @dQR and Z.FromObjType = '22' and Z.ToObjType = '67'
			                            ) A INNER JOIN 
			                            (
				                            SELECT  D.wareHouse WhsCode, sum(Z.Qty) InQty FROM " + Global.QIT_DB + @".dbo.[QIT_QRStock_InvTrans] Z
				                            WHERE Z.QRCodeID = @dQR and (Z.FromObjType <> '22') AND 
                                                  Z.ToWhs collate SQL_Latin1_General_CP850_CI_AS = D.wareHouse
			                            ) B ON A.WhsCode = B.WhsCode INNER JOIN
			                            (
				                            SELECT  D.wareHouse WhsCode, sum(Z.Qty) OutQty from " + Global.QIT_DB + @".dbo.[QIT_QRStock_InvTrans] Z
				                            WHERE Z.QRCodeID = @dQR and (Z.FromObjType <> '22') and 
                                                  Z.FromWhs collate SQL_Latin1_General_CP850_CI_AS = D.wareHouse
			                            ) as C ON B.WhsCode = C.WhsCode
		                          ) AS C
                                ),0) BatchAvailQty, D.wareHouse ProWhsCode,
                                ISNULL((select sum(IssQty) from " + Global.QIT_DB + @".dbo.QIT_Draft_Issue t 
		                                where t.QRCodeID  = A.QRCodeID and t.BatchSerialNo = A.BatchSerialNo AND 
			                                  t.ItemCode collate SQL_Latin1_General_CP850_CI_AS = D.ItemCode   
			                    ),0) DraftIssueQty, Z.Project QRProject
                        FROM " + Global.QIT_DB + @".dbo.QIT_Production_QR_Detail A  
	                            INNER JOIN " + Global.QIT_DB + @".dbo.QIT_Production_QR_Header B ON A.HeaderSrNo = B.HeaderSrNo
                                INNER JOIN " + Global.QIT_DB + @".dbo.QIT_GateIN Z on Z.GateInNo = A.GateInNo AND Z.ItemCode = A.ItemCode AND A.LineNum = Z.LineNum
	                            INNER JOIN OWOR C ON 1=1 and C.DocEntry = @docEntry
	                            INNER JOIN WOR1 D ON C.DocEntry = D.DocEntry AND D.ItemCode collate SQL_Latin1_General_CP1_CI_AS = A.ItemCode
                                INNER JOIN " + Global.QIT_DB + @".dbo.QIT_Item_Master E on E.ItemCode = A.ItemCode 
                        WHERE A.QRCodeID = @dQR AND A.ItemCode = @itemCode
                        ";
                            }
                            else
                            {
                                _Query = @"
                    SELECT *,
                           ISNULL(( SELECT SUM(ISNULL(Qty,0)) from " + Global.QIT_DB + @".dbo.QIT_Temp_ProIssue 
                                    WHERE ProOrdDocNum = A.DocNum AND ItemCode collate SQL_Latin1_General_CP850_CI_AS = A.ItemCode
                           ),0) ProOrdWiseTempQty,
                           ISNULL(( SELECT SUM(ISNULL(Qty,0)) from " + Global.QIT_DB + @".dbo.QIT_Temp_ProIssue 
                                    WHERE ItemCode collate SQL_Latin1_General_CP850_CI_AS = A.ItemCode
                           ),0) ItemTempQty
                    FROM (
                    SELECT -- A.BranchID, A.GateInNo, A.QRCodeID DetailQRCodeID, B.QRCodeID HeaderQRCodeID, A.QRMngBy,
	                       C.DocEntry, C.DocNum, A.OpeningNo GateRecNo, A.QRCodeID DetailQRCodeID,
                           D.ItemCode, E.ItemName, D.PlannedQty, D.IssuedQty, '' IssQty,
                           E.UomCode, D.Project, E.ItemMngBy, A.BatchSerialNo, D.LineNum, A.Qty QRQty,
                           F.WhsCode BatchWhsCode, F.Quantity BatchAvailQty, D.wareHouse ProWhsCode,
                           ISNULL((select sum(IssQty) from " + Global.QIT_DB + @".dbo.QIT_Draft_Issue t 
		                           where t.QRCodeID  = A.QRCodeID and t.BatchSerialNo = A.BatchSerialNo and 
			                             t.ItemCode collate SQL_Latin1_General_CP850_CI_AS = D.ItemCode and 
			                             t.WhsCode collate SQL_Latin1_General_CP850_CI_AS= D.wareHouse and 
			                             t.WhsCode collate SQL_Latin1_General_CP850_CI_AS = F.WhsCode),0) DraftIssueQtyByQR,
                           ISNULL((select sum(IssQty) from " + Global.QIT_DB + @".dbo.QIT_Draft_Issue t 
		                           where t.QRCodeID  = A.QRCodeID and t.BatchSerialNo = A.BatchSerialNo and 
                                         t.ProOrdDocEntry = C.DocEntry AND
			                             t.ItemCode collate SQL_Latin1_General_CP850_CI_AS = D.ItemCode and 
			                             t.WhsCode collate SQL_Latin1_General_CP850_CI_AS= D.wareHouse and 
			                             t.WhsCode collate SQL_Latin1_General_CP850_CI_AS = F.WhsCode),0) DraftIssueQtyByQRPro,
                           C.Project QRProject,
                           ISNULL(H.ToBinAbsEntry,0) FromBin, (select BinCode from OBIN where AbsEntry = H.ToBinAbsEntry) FromBinCode
                    FROM " + Global.QIT_DB + @".dbo.QIT_OpeningStock_QR_Detail A  
	                     INNER JOIN " + Global.QIT_DB + @".dbo.QIT_OpeningStock_QR_Header B ON A.HeaderSrNo = B.HeaderSrNo
	                     INNER JOIN OWOR C ON 1=1 and C.DocEntry = @docEntry
	                     INNER JOIN WOR1 D ON C.DocEntry = D.DocEntry AND D.ItemCode collate SQL_Latin1_General_CP1_CI_AS = A.ItemCode
                         INNER JOIN " + Global.QIT_DB + @".dbo.QIT_Item_Master E on E.ItemCode = A.ItemCode 
                         INNER JOIN [OBTQ] F ON 1=1   
                         INNER  JOIN [dbo].[OBTN] G ON F.[ItemCode] = G.[ItemCode] AND F.[SysNumber] = G.[SysNumber] 
                         INNER JOIN " + Global.QIT_DB + @".dbo.QIT_OSQRStock_InvTrans H ON H.QRCodeID = A.QRCodeID and 
                                    H.TransId=(SELECT MAX(A1.TRansId) FROM " + Global.QIT_DB + @".dbo.QIT_OSQRStock_InvTrans A1 WHERE A1.QRCodeId = @dQR )
                    WHERE A.QRCodeID = @dQR AND F.Quantity <> 0 AND G.DistNumber = @batch AND F.ItemCode = @itemCode

                    UNION

                    SELECT -- A.BranchID, A.GateInNo, A.QRCodeID DetailQRCodeID, B.QRCodeID HeaderQRCodeID, A.QRMngBy,
	                       C.DocEntry, C.DocNum, A.OpeningNo GateRecNo, A.QRCodeID DetailQRCodeID,
                           D.ItemCode, E.ItemName, D.PlannedQty, D.IssuedQty, '' IssQty,
                           E.UomCode, D.Project, E.ItemMngBy, A.BatchSerialNo, D.LineNum, A.Qty QRQty,
                           F.WhsCode BatchWhsCode, F.Quantity BatchAvailQty, D.wareHouse ProWhsCode,
                           ISNULL((select sum(IssQty) from " + Global.QIT_DB + @".dbo.QIT_Draft_Issue t 
		                           where t.QRCodeID  = A.QRCodeID and t.BatchSerialNo = A.BatchSerialNo and 
			                             t.ItemCode collate SQL_Latin1_General_CP850_CI_AS = D.ItemCode and 
			                             t.WhsCode collate SQL_Latin1_General_CP850_CI_AS= D.wareHouse and 
			                             t.WhsCode collate SQL_Latin1_General_CP850_CI_AS = F.WhsCode),0) DraftIssueQtyByQR, 
                           ISNULL((select sum(IssQty) from " + Global.QIT_DB + @".dbo.QIT_Draft_Issue t 
		                           where t.QRCodeID  = A.QRCodeID and t.BatchSerialNo = A.BatchSerialNo and 
                                         t.ProOrdDocEntry = C.DocEntry AND
			                             t.ItemCode collate SQL_Latin1_General_CP850_CI_AS = D.ItemCode and 
			                             t.WhsCode collate SQL_Latin1_General_CP850_CI_AS= D.wareHouse and 
			                             t.WhsCode collate SQL_Latin1_General_CP850_CI_AS = F.WhsCode),0) DraftIssueQtyByQRPro,
                           C.Project QRProject,
                           ISNULL(H.ToBinAbsEntry,0) FromBin, (select BinCode from OBIN where AbsEntry = H.ToBinAbsEntry) FromBinCode
                    FROM " + Global.QIT_DB + @".dbo.QIT_OpeningStock_QR_Detail A  
	                     INNER JOIN " + Global.QIT_DB + @".dbo.QIT_OpeningStock_QR_Header B ON A.HeaderSrNo = B.HeaderSrNo
	                     INNER JOIN OWOR C ON 1=1 and C.DocEntry = @docEntry
	                     INNER JOIN WOR1 D ON C.DocEntry = D.DocEntry AND D.ItemCode collate SQL_Latin1_General_CP1_CI_AS = A.ItemCode
                         INNER JOIN " + Global.QIT_DB + @".dbo.QIT_Item_Master E on E.ItemCode = A.ItemCode 
                         INNER JOIN [OSRQ] F ON 1=1   
                         INNER JOIN [dbo].[OSRN] G ON F.[ItemCode] = G.[ItemCode] AND F.[SysNumber] = G.[SysNumber] 
                         INNER JOIN " + Global.QIT_DB + @".dbo.QIT_OSQRStock_InvTrans H ON H.QRCodeID = A.QRCodeID and 
                                    H.TransId=(SELECT MAX(A1.TRansId) FROM " + Global.QIT_DB + @".dbo.QIT_OSQRStock_InvTrans A1 WHERE A1.QRCodeId = @dQR )
                    WHERE A.QRCodeID = @dQR AND F.Quantity <> 0 AND G.DistNumber = @batch AND F.ItemCode = @itemCode
                    ) AS A
                    ";
                            }

                            _logger.LogInformation(" ProductionController : ValidateItemQR() Query : {q} ", _Query.ToString());
                            SAPcon.Open();
                            oAdptr = new SqlDataAdapter(_Query, SAPcon);
                            oAdptr.SelectCommand.Parameters.AddWithValue("@docEntry", payload.ProDocEntry);
                            oAdptr.SelectCommand.Parameters.AddWithValue("@dQR", payload.DetailQRCodeID.Replace(" ", "~"));
                            if (dtQrData.Rows[0]["BatchSerialNo"].ToString() != "-")
                                oAdptr.SelectCommand.Parameters.AddWithValue("@batch", dtQrData.Rows[0]["BatchSerialNo"].ToString());
                            oAdptr.SelectCommand.Parameters.AddWithValue("@itemCode", dtQrData.Rows[0]["ItemCode"].ToString());
                            oAdptr.Fill(dtData);
                            SAPcon.Close();

                            if (dtData.Rows.Count > 0)
                            {
                                var whsCodes = dtData.AsEnumerable().Select(row => row.Field<string>("BatchWhsCode")).Distinct().ToList();

                                var matchingRows = dtData.AsEnumerable()
                                                   .Where(row => row.Field<string>("BatchWhsCode") == row.Field<string>("ProWhsCode"))
                                                   .ToList();

                                if (matchingRows.Any())
                                {
                                    DataTable dtValidItem = dtData.AsEnumerable()
                                                    .Where(row => row.Field<string>("BatchWhsCode") == row.Field<string>("ProWhsCode"))
                                                    .CopyToDataTable();

                                    if (dtValidItem.Rows.Count > 0)
                                    {
                                        if (dtValidItem.Rows[0]["QRProject"].ToString().ToLower() == "common")
                                        {
                                            dynamic arData = JsonConvert.SerializeObject(dtValidItem);
                                            obj = JsonConvert.DeserializeObject<List<ValidItemCls>>(arData.ToString().Replace("~", " "));
                                            return obj;
                                        }
                                        if (dtValidItem.Rows[0]["QRProject"].ToString().ToLower() == dtValidItem.Rows[0]["Project"].ToString().ToLower())
                                        {
                                            dynamic arData = JsonConvert.SerializeObject(dtValidItem);
                                            obj = JsonConvert.DeserializeObject<List<ValidItemCls>>(arData.ToString().Replace("~", " "));
                                            return obj;
                                        }
                                        else
                                        {
                                            return BadRequest(new
                                            {
                                                StatusCode = "400",
                                                StatusMsg = "QR Project : " + dtData.Rows[0]["QRProject"].ToString() + Environment.NewLine +
                                                            "Production Item Project : " + dtData.Rows[0]["Project"].ToString()
                                            });
                                        }
                                    }
                                    else
                                    {
                                        return BadRequest(new
                                        {
                                            StatusCode = "400",
                                            StatusMsg = "Item is not availbale in Production warehouse" + Environment.NewLine +
                                                        "Production Warehouse : " + dtData.Rows[0]["ProWhsCode"].ToString() + Environment.NewLine +
                                                        "Item QR Warehouse : " + string.Join(",", whsCodes)
                                        });
                                    }
                                }
                                else
                                {
                                    return BadRequest(new
                                    {
                                        StatusCode = "400",
                                        StatusMsg = "Item is not availbale in Production warehouse" + Environment.NewLine +
                                                       "Production Warehouse : " + dtData.Rows[0]["ProWhsCode"].ToString() + Environment.NewLine +
                                                       "Item QR Warehouse : " + string.Join(",", whsCodes)
                                    });
                                }
                            }
                            else
                            {
                                return BadRequest(new { StatusCode = "400", StatusMsg = "No data found" });
                            }
                        }
                        else
                            return BadRequest(new { StatusCode = "400", StatusMsg = "No such QR exist" });
                    }

                }
            }
            catch (Exception ex)
            {
                _logger.LogError(" Error in ProductionController : ValidateItemQR() :: {Error}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
            }
        }


        [HttpPost("ProductionIssue")]
        public async Task<IActionResult> ProductionIssue(ProductionIssue payload)
        {
            SqlConnection QITcon;
            Object _TransSeq = 0;
            Object _QRTransSeq = 0;
            int _Success = 0;
            int _Fail = 0;

            string p_ErrorMsg = string.Empty;
            try
            {
                _logger.LogInformation(" Calling ProductionController : ProductionIssue() ");

                if (payload != null)
                {
                    if (((WMS_UI_API.Services.SAPConnectionService)_sapConnectionService).oComp.Connected)
                    {
                        //var (success, errorMsg) = await objGlobal.ConnectSAP();
                        if (1 == 1)
                        {
                            int _TotalItemCount = payload.piItems.Count;
                            int _SuccessCount = 0, _DetSuccessCount = 0;
                            int _FailCount = 0, _DetFailCount = 0;

                            #region Get TransSeq No - Pro to Issue
                            QITcon = new SqlConnection(_QIT_connection);
                            _Query = @" SELECT ISNULL(max(TransSeq),0) + 1 FROM QIT_Trans_ProToIssue A  ";
                            _logger.LogInformation(" ProductionController : ProductionIssue GetTransSeqNo Query : {q} ", _Query.ToString());

                            cmd = new SqlCommand(_Query, QITcon);
                            QITcon.Open();
                            _TransSeq = cmd.ExecuteScalar();
                            QITcon.Close();
                            #endregion

                            #region Get QR TransSeq No - Pro to Issue
                            QITcon = new SqlConnection(_QIT_connection);
                            _Query = @" SELECT ISNULL(max(QRTransSeq),0) + 1 FROM QIT_QRStock_ProToIssue A  ";
                            _logger.LogInformation(" ProductionController : ProductionIssue GetQRTransSeqNo(Pro to Issue) Query : {q} ", _Query.ToString());

                            cmd = new SqlCommand(_Query, QITcon);
                            QITcon.Open();
                            _QRTransSeq = cmd.ExecuteScalar();
                            QITcon.Close();
                            #endregion

                            #region Insert into Tables
                            foreach (var item in payload.piItems)
                            {
                                #region Insert in Transaction Table - Pro To Issue
                                QITcon = new SqlConnection(_QIT_connection);

                                _Query = @"
                             INSERT INTO QIT_Trans_ProToIssue
                             (BranchID, TransId, TransSeq, FromObjType, ToObjType,BaseDocEntry, BaseDocNum, 
                              DocEntry, DocNum, ItemCode,  Qty, UoMCode, FromWhs, ToWhs, Remark
                             ) 
                             VALUES 
                             ( @bID, (SELECT ISNULL(max(TransId),0) + 1 FROM QIT_Trans_ProToIssue), @transSeq, '202', '60', @baseDocEntry, @baseDocNum, 
                               @docEntry, @docNum, @itemCode,  @qty, @uom, @fromWhs, @toWhs, @remark 
                             )

                            ";

                                _logger.LogInformation(" ProductionController : Transaction Table Query : {q} ", _Query.ToString());

                                cmd = new SqlCommand(_Query, QITcon);
                                cmd.Parameters.AddWithValue("@bID", payload.BranchID);
                                cmd.Parameters.AddWithValue("@transSeq", _TransSeq);
                                cmd.Parameters.AddWithValue("@baseDocEntry", payload.ProOrdDocEntry);
                                cmd.Parameters.AddWithValue("@baseDocNum", payload.ProOrdDocNum);
                                cmd.Parameters.AddWithValue("@docEntry", 0);
                                cmd.Parameters.AddWithValue("@docNum", 0);
                                cmd.Parameters.AddWithValue("@itemCode", item.ItemCode);
                                cmd.Parameters.AddWithValue("@qty", Convert.ToDouble(item.Qty));
                                cmd.Parameters.AddWithValue("@uom", item.UoMCode);
                                cmd.Parameters.AddWithValue("@fromWhs", item.WhsCode);
                                cmd.Parameters.AddWithValue("@toWhs", item.WhsCode);
                                cmd.Parameters.AddWithValue("@remark", payload.Comment);
                                int intNum = 0;

                                QITcon.Open();
                                intNum = cmd.ExecuteNonQuery();
                                QITcon.Close();


                                if (intNum > 0)
                                    _SuccessCount = _SuccessCount + 1;
                                else
                                    _FailCount = _FailCount + 1;

                                #endregion

                                #region Insert in QR Stock Table - Pro To Issue
                                foreach (var itemDet in item.piBatchSerial)
                                {
                                    foreach (var piIssData in itemDet.piIssData)
                                    {
                                        QITcon = new SqlConnection(_QIT_connection);

                                        _Query = @"
                                    INSERT INTO QIT_QRStock_ProToIssue 
                                    (      BranchID, TransId, TransSeq, QRTransSeq, QRCodeID, IssNo, ItemCode,BatchSerialNo, 
                                           FromObjType, ToObjType, FromWhs, ToWhs, FromBinAbsEntry, ToBinAbsEntry, Qty
                                    ) 
                                    VALUES 
                                    ( @bID, (SELECT ISNULL(max(TransId),0) + 1 FROM QIT_QRStock_ProToIssue), 
                                      @transSeq, @qrtransSeq, @qrCodeID, @issNo, @itemCode, @bsNo, '202', '60', @fromWhs, @toWhs, @fromBin, @toBin, @qty   
                                    ) 
                                    ";

                                        _logger.LogInformation("ProductionController : QR Stock Table Query : {q} ", _Query.ToString());

                                        cmd = new SqlCommand(_Query, QITcon);
                                        cmd.Parameters.AddWithValue("@bID", payload.BranchID);
                                        cmd.Parameters.AddWithValue("@qrtransSeq", _QRTransSeq);
                                        cmd.Parameters.AddWithValue("@transSeq", _TransSeq);
                                        cmd.Parameters.AddWithValue("@qrCodeID", itemDet.DetailQRCodeID.Replace(" ", "~"));
                                        cmd.Parameters.AddWithValue("@issNo", piIssData.DraftIssNo);
                                        cmd.Parameters.AddWithValue("@itemCode", item.ItemCode);
                                        cmd.Parameters.AddWithValue("@bsNo", itemDet.BatchSerialNo);
                                        cmd.Parameters.AddWithValue("@fromWhs", item.WhsCode);
                                        cmd.Parameters.AddWithValue("@toWhs", item.WhsCode);
                                        cmd.Parameters.AddWithValue("@fromBin", itemDet.fromBinAbsEntry);
                                        cmd.Parameters.AddWithValue("@toBin", 0);
                                        cmd.Parameters.AddWithValue("@qty", piIssData.Qty);

                                        intNum = 0;
                                        try
                                        {
                                            QITcon.Open();
                                            intNum = cmd.ExecuteNonQuery();
                                            QITcon.Close();
                                        }
                                        catch (Exception ex1)
                                        {
                                            this.DeleteTransactionProToIssue(_TransSeq.ToString());
                                            this.DeleteQRStockDetProToIssue(_QRTransSeq.ToString());

                                            return BadRequest(new
                                            {
                                                StatusCode = "400",
                                                IsSaved = "N",
                                                TransSeq = _TransSeq,
                                                StatusMsg = ex1.Message
                                            });
                                        }

                                        if (intNum > 0)
                                            _DetSuccessCount = _DetSuccessCount + 1;
                                        else
                                            _DetFailCount = _DetFailCount + 1;
                                    }
                                }

                                #endregion
                            }
                            #endregion


                            if (_TotalItemCount == _SuccessCount)
                            {
                                int _Line = 0;
                                Documents productionIssue = (Documents)((WMS_UI_API.Services.SAPConnectionService)_sapConnectionService).oComp.GetBusinessObject(BoObjectTypes.oInventoryGenExit);

                                productionIssue.DocDate = DateTime.Now;
                                productionIssue.BPL_IDAssignedToInvoice = payload.BranchID;
                                productionIssue.Comments = payload.Comment;
                                productionIssue.Series = payload.Series;
                                productionIssue.UserFields.Fields.Item("U_QIT_FromWeb").Value = "Y";

                                foreach (var item in payload.piItems)
                                {
                                    //productionIssue.Lines.ItemCode = item.ItemCode;
                                    productionIssue.Lines.Quantity = double.Parse(item.Qty); // Set the quantity 
                                    productionIssue.Lines.BaseType = 202;
                                    productionIssue.Lines.BaseEntry = payload.ProOrdDocEntry;
                                    productionIssue.Lines.BaseLine = item.LineNum;
                                    productionIssue.Lines.WarehouseCode = item.WhsCode;

                                    if (item.ItemMngBy.ToLower() == "s")
                                    {
                                        int i = 0;
                                        foreach (var serial in item.piBatchSerial)
                                        {
                                            if (!string.IsNullOrEmpty(serial.BatchSerialNo))
                                            {
                                                productionIssue.Lines.SerialNumbers.SetCurrentLine(i);
                                                productionIssue.Lines.BatchNumbers.BaseLineNumber = _Line;
                                                productionIssue.Lines.SerialNumbers.InternalSerialNumber = serial.BatchSerialNo;
                                                productionIssue.Lines.SerialNumbers.ManufacturerSerialNumber = serial.BatchSerialNo;
                                                productionIssue.Lines.SerialNumbers.Quantity = Convert.ToDouble(serial.Qty);
                                                productionIssue.Lines.SerialNumbers.Add();

                                                if (serial.fromBinAbsEntry > 0)
                                                {
                                                    productionIssue.Lines.BinAllocations.BinAbsEntry = serial.fromBinAbsEntry;
                                                    productionIssue.Lines.BinAllocations.Quantity = Convert.ToDouble(serial.Qty);
                                                    productionIssue.Lines.BinAllocations.BaseLineNumber = _Line;
                                                    productionIssue.Lines.BinAllocations.SerialAndBatchNumbersBaseLine = i;
                                                    productionIssue.Lines.BinAllocations.Add();

                                                }
                                                i = i + 1;
                                            }
                                        }
                                    }
                                    else if (item.ItemMngBy.ToLower() == "b")
                                    {
                                        int _batchLine = 0;
                                        foreach (var batch in item.piBatchSerial)
                                        {
                                            if (!string.IsNullOrEmpty(batch.BatchSerialNo))
                                            {
                                                productionIssue.Lines.BatchNumbers.BaseLineNumber = _Line;
                                                productionIssue.Lines.BatchNumbers.BatchNumber = batch.BatchSerialNo;
                                                productionIssue.Lines.BatchNumbers.Quantity = Convert.ToDouble(batch.Qty);
                                                productionIssue.Lines.BatchNumbers.Add();

                                                if (batch.fromBinAbsEntry > 0)
                                                {
                                                    productionIssue.Lines.BinAllocations.BinAbsEntry = batch.fromBinAbsEntry;
                                                    productionIssue.Lines.BinAllocations.Quantity = Convert.ToDouble(batch.Qty);
                                                    productionIssue.Lines.BinAllocations.BaseLineNumber = _Line;
                                                    productionIssue.Lines.BinAllocations.SerialAndBatchNumbersBaseLine = _batchLine;
                                                    productionIssue.Lines.BinAllocations.Add();
                                                }
                                                _batchLine = _batchLine + 1;
                                            }
                                        }
                                    }
                                    productionIssue.Lines.Add();
                                    _Line = _Line + 1;
                                }

                                int result = productionIssue.Add();
                                if (result != 0)
                                {
                                    this.DeleteTransactionProToIssue(_TransSeq.ToString());
                                    this.DeleteQRStockDetProToIssue(_QRTransSeq.ToString());
                                    return BadRequest(new
                                    {
                                        StatusCode = "400",
                                        IsSaved = "N",
                                        TransSeq = _TransSeq,
                                        StatusMsg = "Error code: " + result + Environment.NewLine +
                                                    "Error message: " + ((WMS_UI_API.Services.SAPConnectionService)_sapConnectionService).oComp.GetLastErrorDescription()
                                    });
                                }
                                else
                                {
                                    int docEntry = int.Parse(((WMS_UI_API.Services.SAPConnectionService)_sapConnectionService).oComp.GetNewObjectKey());

                                    #region Update Transaction Table
                                    QITcon = new SqlConnection(_QIT_connection);
                                    _Query = @" UPDATE QIT_Trans_ProToIssue 
                                            SET DocEntry = @docEntry, 
                                                DocNum = (SELECT docnum FROM " + Global.SAP_DB + @".dbo.OIGE where DocEntry = @docEntry) 
                                            WHERE TransSeq = @code";
                                    _logger.LogInformation(" ProductionController : ProductionIssue() : Update Transaction Table Query : {q} ", _Query.ToString());

                                    cmd = new SqlCommand(_Query, QITcon);
                                    cmd.Parameters.AddWithValue("@docEntry", docEntry);
                                    cmd.Parameters.AddWithValue("@code", _TransSeq);

                                    QITcon.Open();
                                    int intNum = cmd.ExecuteNonQuery();
                                    QITcon.Close();

                                    if (intNum > 0)
                                    {
                                        return Ok(new
                                        {
                                            StatusCode = "200",
                                            IsSaved = "Y",
                                            TransSeq = _TransSeq,
                                            DocEntry = docEntry,
                                            StatusMsg = "Production Issue added successfully !!!"
                                        });
                                    }
                                    else
                                    {
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
                                this.DeleteTransactionProToIssue(_TransSeq.ToString());
                                this.DeleteQRStockDetProToIssue(_QRTransSeq.ToString());
                                return BadRequest(new
                                {
                                    StatusCode = "400",
                                    IsSaved = "N",
                                    TransSeq = _TransSeq,
                                    StatusMsg = "Problem while saving Transaction data"
                                });
                            }
                        }
                        else
                        {
                            this.DeleteTransactionProToIssue(_TransSeq.ToString());
                            this.DeleteQRStockDetProToIssue(_QRTransSeq.ToString());
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
                else
                {
                    this.DeleteTransactionProToIssue(_TransSeq.ToString());
                    this.DeleteQRStockDetProToIssue(_QRTransSeq.ToString());
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
                this.DeleteTransactionProToIssue(_TransSeq.ToString());
                this.DeleteQRStockDetProToIssue(_QRTransSeq.ToString());
                _logger.LogError(" Error in ProductionController : ProductionIssue() :: {Error}", ex.ToString());
                return BadRequest(new
                {
                    StatusCode = "400",
                    IsSaved = "N",
                    StatusMsg = ex.Message.ToString()
                });
            }
            finally
            { 
            }
        }


        [HttpPost("ProductionDraftIssue")]
        public IActionResult ProductionDraftIssue(ProductionDraftIssue payload)
        {
            string _IsSaved = "N";
            Object Value = "";
            SqlConnection QITcon;
            try
            {
                _logger.LogInformation(" Calling ProductionController : ProductionDraftIssue() ");

                if (payload != null)
                {
                    #region Get Iss No
                    QITcon = new SqlConnection(_QIT_connection);
                    DataTable dtData = new DataTable();

                    _Query = @" select ISNULL(max(IssNo),0) + 1 from QIT_Draft_Issue ";
                    _logger.LogInformation(" ProductionController : Get Iss No Query : {q} ", _Query.ToString());

                    cmd = new SqlCommand(_Query, QITcon);
                    QITcon.Open();
                    Value = cmd.ExecuteScalar();
                    QITcon.Close();
                    #endregion

                    int SuccessCount = 0;
                    int FailCount = 0;

                    foreach (var item in payload.piItems)
                    {
                        foreach (var itemDet in item.piBatchSerial)
                        {
                            _Query = @"
                            INSERT INTO QIT_Draft_Issue
                            (IssNo, TransSeq, ProOrdDocEntry, QRCodeID, ItemCode, BatchSerialNo, WhsCode, FromBinAbsEntry, Project, IssQty, Series, Comment) 
                            VALUES ( @issNo, 
                             (select ISNULL(max(TransSeq),0) + 1 from QIT_Draft_Issue WHERE IssNo = @issNo), 
                             @proDocEntry, @qr, @itemCode, @batch,  @whs, @fromBin, @proj, @issQty, @series, @comment ) ";
                            _logger.LogInformation("ProductionController : ProductionDraftIssue : {q} ", _Query.ToString());

                            cmd = new SqlCommand(_Query, QITcon);
                            cmd.Parameters.AddWithValue("@issNo", Value.ToString());
                            cmd.Parameters.AddWithValue("@proDocEntry", payload.ProOrdDocEntry);
                            cmd.Parameters.AddWithValue("@qr", itemDet.DetailQRCodeID.Replace(" ", "~"));
                            cmd.Parameters.AddWithValue("@itemCode", itemDet.ItemCode);
                            cmd.Parameters.AddWithValue("@batch", itemDet.BatchSerialNo);
                            cmd.Parameters.AddWithValue("@Whs", item.WhsCode);
                            cmd.Parameters.AddWithValue("@proj", itemDet.Project);
                            cmd.Parameters.AddWithValue("@fromBin", itemDet.fromBinAbsEntry);
                            cmd.Parameters.AddWithValue("@issQty", itemDet.Qty);
                            cmd.Parameters.AddWithValue("@series", payload.Series);
                            cmd.Parameters.AddWithValue("@comment", payload.Comment);
                            QITcon.Open();
                            int intNum = cmd.ExecuteNonQuery();
                            QITcon.Close();

                            if (intNum > 0)
                                SuccessCount = SuccessCount + 1;
                            else
                                FailCount = FailCount + 1;
                        }
                    }

                    if (FailCount > 0)
                    {
                        this.DeleteDraftIssNo(Value.ToString());
                        return BadRequest(new { StatusCode = "400", IsSaved = "N", StatusMsg = "Fail to add Draft Issue" });
                    }
                    else
                    {
                        return Ok(new { StatusCode = "200", IsSaved = _IsSaved, StatusMsg = "Saved Successfully!!!" });
                    }
                }
                else
                {

                    return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Details not found" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(" Error in ProductionController : ProductionDraftIssue() :: {Error}", ex.ToString());
                this.DeleteDraftIssNo(Value.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
            }
        }


        [HttpPost("GetDraftIssueList")]
        public async Task<ActionResult<IEnumerable<ProductionOrderList>>> GetDraftIssueList()
        {
            SqlConnection SAPcon;
            SqlDataAdapter oAdptr;
            try
            {
                _logger.LogInformation(" Calling ProductionController : GetDraftIssueList() ");
                List<ProductionOrderList> obj = new List<ProductionOrderList>();
                System.Data.DataTable dtData = new DataTable();
                SAPcon = new SqlConnection(_connection);

                _Query = @" 
                SELECT DISTINCT T0.[DocEntry], T0.[DocNum], T2.Series, T1.[SeriesName], T0.[Type], T0.[DueDate], T0.[ItemCode], T0.[ProdName], 
                       T0.[Comments], T0.[StartDate], T0.[Priority] 
                FROM  [dbo].[OWOR] T0  
                INNER JOIN " + Global.QIT_DB + @".dbo.QIT_Draft_Issue T2 ON T0.DocEntry = T2.ProOrdDocEntry
				INNER  JOIN [dbo].[NNM1] T1  ON  T2.[Series] = T1.[Series]   
                ORDER BY T0.[DocNum],T0.[DocEntry]
                ";

                _logger.LogInformation(" ProductionController : GetDraftIssueList() Query : {q} ", _Query.ToString());
                SAPcon.Open();
                oAdptr = new SqlDataAdapter(_Query, SAPcon);
                oAdptr.Fill(dtData);
                SAPcon.Close();

                if (dtData.Rows.Count > 0)
                {
                    dynamic arData = JsonConvert.SerializeObject(dtData);
                    obj = JsonConvert.DeserializeObject<List<ProductionOrderList>>(arData.ToString());
                    return obj;
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "No data found" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(" Error in ProductionController : GetDraftIssueList() :: {Error}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
            }
        }


        [HttpPost("ValidateDraftIssueItemQR")]
        public async Task<ActionResult<IEnumerable<ValidDraftIssueItem>>> ValidateDraftIssueItemQR(ProItemValidateCls payload)
        {
            SqlConnection QITcon;
            SqlDataAdapter oAdptr;
            try
            {
                _logger.LogInformation(" Calling ProductionController : ValidateDraftIssueItemQR() ");
                List<ValidDraftIssueItem> obj = new List<ValidDraftIssueItem>();

                #region Get Item Data from QR
                System.Data.DataTable dtQrData = new DataTable();
                QITcon = new SqlConnection(_QIT_connection);
                _Query = @" 
                SELECT  A.TransId, A.IssNo, A.TransSeq, A.ProOrdDocEntry, B.DocNum, A.Series, A.QRCodeID, A.ItemCode,  
                        C.LineNum, A.BatchSerialNo, A.WhsCode, D.UomCode, ISNULL(A.FromBinAbsEntry,0) FromBinAbsEntry, 
                        A.IssQty, A.Comment, A.EntryDate, D.ItemMngBy, D.QRMngBy, A.Project
                FROM QIT_Draft_Issue A
                     INNER JOIN " + Global.SAP_DB + @".dbo.OWOR B on A.ProOrdDocEntry = B.DocEntry
                     INNER JOIN " + Global.SAP_DB + @".dbo.WOR1 C on B.DocEntry = C.DocEntry and 
                                C.ItemCode collate SQL_Latin1_General_CP1_CI_AS = A.ItemCode
                     INNER JOIN QIT_Item_Master D on A.ItemCode = D.ItemCode
                WHERE A.QRCodeID = @qr and B.DocEntry = @proDocEntry and 
                      concat(A.IssNo, A.QRCodeID, A.ItemCode, A.BatchSerialNo) not in 
				      (select concat(A.IssNo, A.QRCodeID, A.ItemCode, A.BatchSerialNo) from QIT_QRStock_ProToIssue A )";

                _logger.LogInformation(" ProductionController : Item QR Query : {q} ", _Query.ToString());
                QITcon.Open();
                oAdptr = new SqlDataAdapter(_Query, QITcon);
                oAdptr.SelectCommand.Parameters.AddWithValue("@qr", payload.DetailQRCodeID.Replace(" ", "~"));
                oAdptr.SelectCommand.Parameters.AddWithValue("@proDocEntry", payload.ProDocEntry);
                oAdptr.Fill(dtQrData);
                QITcon.Close();
                #endregion

                if (dtQrData.Rows.Count > 0)
                {
                    dynamic arData = JsonConvert.SerializeObject(dtQrData);
                    obj = JsonConvert.DeserializeObject<List<ValidDraftIssueItem>>(arData.ToString().Replace("~", " "));
                    return obj;
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "No such QR exist" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(" Error in ProductionController : ValidateDraftIssueItemQR() :: {Error}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
            }
        }


        [HttpPost("ProIssuedItems")]
        public async Task<ActionResult<IEnumerable<ProIssuedItems>>> ProIssuedItems(int BranchId, int ProOrdDocEntry)
        {
            SqlConnection QITcon;
            SqlDataAdapter oAdptr;
            try
            {
                _logger.LogInformation(" Calling ProductionController : ProIssuedItems() ");
                List<ProIssuedItems> obj = new List<ProIssuedItems>();

                #region Get Item Data
                System.Data.DataTable dtQrData = new DataTable();
                QITcon = new SqlConnection(_QIT_connection);

                _Query = @" SELECT A.TransSeq, A.BaseDocEntry, A.BaseDocNum, A.DocEntry, A.DocNum, A.ItemCode, A.Qty IssuedQty,
	                               B.QRCodeID, B.BatchSerialNo
                            FROM QIT_Trans_ProToIssue A INNER JOIN QIT_QRStock_ProToIssue B ON A.TransSeq = B.TransSeq AND A.ItemCode = B.ItemCode
                            WHERE BaseDocEntry = @docEntry and ISNULL(A.BranchID, @bID) = @bID ";

                _logger.LogInformation(" ProductionController : ProIssuedItems QR Query : {q} ", _Query.ToString());
                QITcon.Open();
                oAdptr = new SqlDataAdapter(_Query, QITcon);
                oAdptr.SelectCommand.Parameters.AddWithValue("@bID", BranchId);
                oAdptr.SelectCommand.Parameters.AddWithValue("@docEntry", ProOrdDocEntry);
                oAdptr.Fill(dtQrData);
                QITcon.Close();
                #endregion

                if (dtQrData.Rows.Count > 0)
                {
                    dynamic arData = JsonConvert.SerializeObject(dtQrData);
                    obj = JsonConvert.DeserializeObject<List<ProIssuedItems>>(arData.ToString().Replace("~", " "));
                    return obj;
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "No data found" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(" Error in ProductionController : ProIssuedItems() :: {Error}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
            }
        }

        #endregion


        #region Production Receipt

        [HttpPost("GetProOrdListReceipt")]
        public async Task<ActionResult<IEnumerable<ProductionOrderReceipt>>> GetProOrdListReceipt()
        {
            SqlConnection QITcon;
            SqlDataAdapter oAdptr;
            try
            {
                _logger.LogInformation(" Calling ProductionController : GetProOrdListReceipt() ");

                System.Data.DataTable dtData = new DataTable();
                QITcon = new SqlConnection(_QIT_connection);

                _Query = @" 
                SELECT B.*, ( SELECT BinActivat FROM " + Global.SAP_DB + @".dbo.OWHS WHERE WhsCode = B.Warehouse) BinActivat,
                       case when C.U_QA in (2) then 'Y' else 'N' end QARequired
                FROM 
                (
                    SELECT A.* FROM 
                    (
	                    SELECT distinct T0.[DocEntry], T0.[DocNum], T1.[SeriesName], T0.[Type], T0.[DueDate], T0.[ItemCode], T0.[ProdName], 
		                       ISNULL(T0.[PlannedQty],0) - ISNULL(( select sum(ReceiptQty) FROM QIT_Draft_Receipt where ProOrdDocEntry = T0.DocEntry ),0) Quantity, 
                               T0.[PlannedQty] , T0.CmpltQty, 
                               T0.Warehouse, T0.Uom UomCode, T0.[Comments], T0.Project,
                               ISNULL(( SELECT SUM(ReceiptQty) FROM QIT_Draft_Receipt WHERE ProOrdDocEntry = T0.DocEntry ),0) ReceiptQty
	                    FROM  " + Global.SAP_DB + @".dbo.OWOR T0  
		                      INNER JOIN " + Global.SAP_DB + @".dbo.NNM1 T1 ON T0.[Series] = T1.[Series]  
                              INNER JOIN QIT_Trans_ProToIssue T2 ON T0.DocEntry = T2.BaseDocEntry
	                    WHERE T0.[Status] = 'R' AND T0.[Type] = 'S' AND T0.[PlannedQty] > T0.[CmpltQty]    
                    ) AS A
                    WHERE concat(A.DocEntry, A.DocNum) not in 
                    (
	                    SELECT DISTINCT concat(po.DocEntry, DocNum)
	                    FROM " + Global.SAP_DB + @".dbo.OWOR po INNER JOIN " + Global.SAP_DB + @".dbo.WOR1 poi ON po.DocEntry = poi.DocEntry
	                    WHERE poi.IssuedQty = 0
                    )
                ) as B INNER JOIN " + Global.SAP_DB + @".dbo.OITM C ON B.ItemCode = C.ItemCode
                -- ORDER BY A.[DocNum],A.[DocEntry]
                ";

                _logger.LogInformation(" ProductionController : GetProOrdListReceipt() Query : {q} ", _Query.ToString());
                QITcon.Open();
                oAdptr = new SqlDataAdapter(_Query, QITcon);
                oAdptr.Fill(dtData);
                QITcon.Close();

                if (dtData.Rows.Count > 0)
                {
                    List<ProductionOrderReceipt> obj = new List<ProductionOrderReceipt>();
                    dynamic arData = JsonConvert.SerializeObject(dtData);
                    obj = JsonConvert.DeserializeObject<List<ProductionOrderReceipt>>(arData.ToString());
                    return obj;
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "No data found" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(" Error in ProductionController : GetProOrdListReceipt() :: {Error}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
            }
            finally
            {
                 
            }
        }


        [HttpPost("ProductionReceipt")]
        public async Task<IActionResult> ProductionReceipt(ProductionReceiptPro payload)
        {
            SqlConnection QITcon;
            Object _TransSeq = 0;
            Object _QRTransSeq = 0;
            Object _QRTransSeqInvTrans = 0;
            int _Success = 0;
            int _Fail = 0;

            string p_ErrorMsg = string.Empty;
            try
            {
                _logger.LogInformation(" Calling ProductionController : ProductionReceipt() ");

                if (payload != null)
                {
                    if (((WMS_UI_API.Services.SAPConnectionService)_sapConnectionService).oComp.Connected)
                    {
                        if (1 == 1)
                        {
                            int _TotalQRCount = payload.recDetails.Count;
                            int _SuccessCount = 0, _DetSuccessCount = 0;
                            int _FailCount = 0, _DetFailCount = 0;

                            #region Get TransSeq No - Pro To Receipt
                            QITcon = new SqlConnection(_QIT_connection);
                            _Query = @" SELECT ISNULL(max(TransSeq),0) + 1 FROM QIT_Trans_ProToReceipt A  ";
                            _logger.LogInformation(" ProductionController : ProductionReceipt GetTransSeqNo Query : {q} ", _Query.ToString());
                            cmd = new SqlCommand(_Query, QITcon);
                            QITcon.Open();
                            _TransSeq = cmd.ExecuteScalar();
                            QITcon.Close();
                            #endregion

                            #region Get QR TransSeq No - Pro to Receipt
                            QITcon = new SqlConnection(_QIT_connection);
                            _Query = @" SELECT ISNULL(max(QRTransSeq),0) + 1 FROM QIT_QRStock_ProToReceipt A  ";
                            _logger.LogInformation(" ProductionController : ProductionReceipt GetQRTransSeqNo(Pro to Receipt) Query : {q} ", _Query.ToString());

                            cmd = new SqlCommand(_Query, QITcon);
                            QITcon.Open();
                            _QRTransSeq = cmd.ExecuteScalar();
                            QITcon.Close();
                            #endregion

                            #region Get QR TransSeq No - Inventory Transfer
                            QITcon = new SqlConnection(_QIT_connection);
                            _Query = @" SELECT ISNULL(max(QRTransSeq),0) + 1 FROM QIT_ProQRStock_InvTrans A  ";
                            _logger.LogInformation(" ProductionController : ProductionReceipt GetQRTransSeqNo(Inv Trans) Query : {q} ", _Query.ToString());

                            cmd = new SqlCommand(_Query, QITcon);
                            QITcon.Open();
                            _QRTransSeqInvTrans = cmd.ExecuteScalar();
                            QITcon.Close();
                            #endregion

                            #region Insert into Tables

                            #region Insert in Transaction Table - Pro To Receipt
                            QITcon = new SqlConnection(_QIT_connection);
                            _Query = @"
                             INSERT INTO QIT_Trans_ProToReceipt
                             (BranchID, TransId, TransSeq, FromObjType, ToObjType,BaseDocEntry, BaseDocNum, 
                              DocEntry, DocNum, ItemCode,  Qty, UoMCode, FromWhs, ToWhs, Remark
                             ) 
                             VALUES 
                             ( @bID, (SELECT ISNULL(max(TransId),0) + 1 FROM QIT_Trans_ProToReceipt), @transSeq, '202', '59', @baseDocEntry, @baseDocNum, 
                               @docEntry, @docNum, @itemCode,  @qty, @uom, @fromWhs, @toWhs, @remark 
                             )

                            ";
                            _logger.LogInformation(" ProductionController : Transaction Table Query : {q} ", _Query.ToString());

                            cmd = new SqlCommand(_Query, QITcon);
                            cmd.Parameters.AddWithValue("@bID", payload.BranchID);
                            cmd.Parameters.AddWithValue("@transSeq", _TransSeq);
                            cmd.Parameters.AddWithValue("@baseDocEntry", payload.ProOrdDocEntry);
                            cmd.Parameters.AddWithValue("@baseDocNum", payload.ProOrdDocNum);
                            cmd.Parameters.AddWithValue("@docEntry", 0);
                            cmd.Parameters.AddWithValue("@docNum", 0);
                            cmd.Parameters.AddWithValue("@itemCode", payload.ItemCode);
                            cmd.Parameters.AddWithValue("@qty", Convert.ToDouble(payload.ReceiptQty));
                            cmd.Parameters.AddWithValue("@uom", payload.UomCode);
                            cmd.Parameters.AddWithValue("@fromWhs", payload.WhsCode);
                            cmd.Parameters.AddWithValue("@toWhs", payload.WhsCode);
                            cmd.Parameters.AddWithValue("@remark", payload.Comment);
                            int intNum = 0;

                            QITcon.Open();
                            intNum = cmd.ExecuteNonQuery();
                            QITcon.Close();


                            if (intNum > 0)
                                _SuccessCount = _SuccessCount + 1;
                            else
                                _FailCount = _FailCount + 1;

                            #endregion

                            foreach (var item in payload.recDetails)
                            {
                                #region Insert in QR Stock Table - Pro To Issue

                                QITcon = new SqlConnection(_QIT_connection);
                                _Query = @"
                                INSERT INTO QIT_QRStock_ProToReceipt
                                (      BranchID, TransId, TransSeq, QRTransSeq, QRCodeID, RecNo, ItemCode, BatchSerialNo, 
                                       FromObjType, ToObjType, FromWhs, ToWhs, FromBinAbsEntry, ToBinAbsEntry, Qty
                                ) 
                                VALUES 
                                ( @bID, (SELECT ISNULL(max(TransId),0) + 1 FROM QIT_QRStock_ProToReceipt), 
                                  @transSeq, @qrtransSeq, @qrCodeID, @RecNo, @itemCode, @bsNo, '202', '59', @fromWhs, @toWhs, @fromBin, @toBin, @qty   
                                ) 

                                INSERT INTO QIT_ProQRStock_InvTrans
                                (   BranchID, TransId,  QRTransSeq, TransSeq, QRCodeID, RecNo, ItemCode,  BatchSerialNo,
                                    FromObjType, ToObjType, FromWhs, ToWhs, FromBinAbsEntry, ToBinAbsEntry, Qty) 
                                VALUES 
                                (   @bID, (SELECT ISNULL(max(TransId),0) + 1 FROM QIT_ProQRStock_InvTrans), 
                                    @qrtransSeqInvTrans, @transSeq, @qrCodeID, @RecNo, @itemCode,  @bsNo, '202', '59', 
                                    @fromWhs, @toWhs, @fromBin, @toBin, @qty   
                                )
                                ";
                                _logger.LogInformation("ProductionController : QR Stock Table Query : {q} ", _Query.ToString());

                                cmd = new SqlCommand(_Query, QITcon);
                                cmd.Parameters.AddWithValue("@bID", payload.BranchID);
                                cmd.Parameters.AddWithValue("@qrtransSeq", _QRTransSeq);
                                cmd.Parameters.AddWithValue("@transSeq", _TransSeq);
                                cmd.Parameters.AddWithValue("@qrtransSeqInvTrans", _QRTransSeqInvTrans);
                                cmd.Parameters.AddWithValue("@qrCodeID", item.DetailQRCodeID.Replace(" ", "~"));
                                cmd.Parameters.AddWithValue("@RecNo", payload.RecNo);
                                cmd.Parameters.AddWithValue("@itemCode", payload.ItemCode);
                                cmd.Parameters.AddWithValue("@bsNo", item.BatchSerialNo);
                                cmd.Parameters.AddWithValue("@fromWhs", payload.WhsCode);
                                cmd.Parameters.AddWithValue("@toWhs", payload.WhsCode);
                                cmd.Parameters.AddWithValue("@fromBin", 0);
                                cmd.Parameters.AddWithValue("@toBin", payload.BinAbsEntry);
                                cmd.Parameters.AddWithValue("@qty", item.Qty);

                                intNum = 0;
                                try
                                {
                                    QITcon.Open();
                                    intNum = cmd.ExecuteNonQuery();
                                    QITcon.Close();
                                }
                                catch (Exception ex1)
                                {
                                    this.DeleteTransactionProToReceipt(_TransSeq.ToString());
                                    this.DeleteQRStockDetProToReceipt(_QRTransSeq.ToString());
                                    this.DeleteQRStockDetProInvTrans(_QRTransSeqInvTrans.ToString());
                                    return BadRequest(new
                                    {
                                        StatusCode = "400",
                                        IsSaved = "N",
                                        TransSeq = _TransSeq,
                                        StatusMsg = ex1.Message
                                    });
                                }


                                if (intNum > 0)
                                    _DetSuccessCount = _DetSuccessCount + 1;
                                else
                                    _DetFailCount = _DetFailCount + 1;


                                #endregion
                            }
                            #endregion

                            if (_TotalQRCount == _DetSuccessCount)
                            {
                                int _Line = 0;
                                Documents productionReceipt = (Documents)((WMS_UI_API.Services.SAPConnectionService)_sapConnectionService).oComp.GetBusinessObject(BoObjectTypes.oInventoryGenEntry);

                                productionReceipt.DocDate = DateTime.Now;
                                productionReceipt.Comments = payload.Comment;
                                productionReceipt.BPL_IDAssignedToInvoice = payload.BranchID;
                                productionReceipt.Series = payload.Series;
                                productionReceipt.UserFields.Fields.Item("U_QIT_FromWeb").Value = "Y";

                                productionReceipt.Lines.Quantity = double.Parse(payload.ReceiptQty);
                                productionReceipt.Lines.BaseType = (int)BoObjectTypes.oProductionOrders;
                                productionReceipt.Lines.BaseEntry = payload.ProOrdDocEntry;
                                productionReceipt.Lines.WarehouseCode = payload.WhsCode;

                                if (payload.ItemMngBy.ToString().ToLower() == "s")
                                {
                                    int i = 0;
                                    foreach (var serial in payload.recDetails)
                                    {
                                        if (!string.IsNullOrEmpty(serial.BatchSerialNo))
                                        {
                                            productionReceipt.Lines.SerialNumbers.SetCurrentLine(i);
                                            productionReceipt.Lines.SerialNumbers.InternalSerialNumber = serial.BatchSerialNo;
                                            productionReceipt.Lines.SerialNumbers.ManufacturerSerialNumber = serial.BatchSerialNo;
                                            productionReceipt.Lines.SerialNumbers.Add();
                                            i = i + 1;
                                        }
                                    }
                                }
                                else if (payload.ItemMngBy.ToString().ToLower() == "b")
                                {
                                    int _batchLine = 0;
                                    foreach (var batch in payload.recDetails)
                                    {
                                        if (!string.IsNullOrEmpty(batch.BatchSerialNo))
                                        {
                                            productionReceipt.Lines.BatchNumbers.BaseLineNumber = _Line;
                                            productionReceipt.Lines.BatchNumbers.BatchNumber = batch.BatchSerialNo;
                                            productionReceipt.Lines.BatchNumbers.Quantity = Convert.ToDouble(batch.Qty);
                                            productionReceipt.Lines.BatchNumbers.Add();

                                            if (payload.BinAbsEntry > 0)
                                            {

                                                productionReceipt.Lines.BinAllocations.BinAbsEntry = payload.BinAbsEntry;
                                                productionReceipt.Lines.BinAllocations.Quantity = Convert.ToDouble(batch.Qty);
                                                productionReceipt.Lines.BinAllocations.BaseLineNumber = _Line;
                                                productionReceipt.Lines.BinAllocations.SerialAndBatchNumbersBaseLine = _batchLine;
                                                productionReceipt.Lines.BinAllocations.Add();
                                            }
                                            _batchLine = _batchLine + 1;
                                        }
                                    }
                                }
                                productionReceipt.Lines.Add();
                                _Line = _Line + 1;

                                int result = productionReceipt.Add();
                                if (result != 0)
                                {
                                    this.DeleteTransactionProToReceipt(_TransSeq.ToString());
                                    this.DeleteQRStockDetProToReceipt(_QRTransSeq.ToString());
                                    this.DeleteGeneratedProDetQR(payload.ProOrdDocEntry, payload.RecNo, payload.ItemCode);
                                    this.DeleteQRStockDetProInvTrans(_QRTransSeqInvTrans.ToString());
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

                                    #region Update Transaction Table
                                    QITcon = new SqlConnection(_QIT_connection);
                                    _Query = @" UPDATE QIT_Trans_ProToReceipt 
                                            SET DocEntry = @docEntry, 
                                                DocNum = (SELECT docnum FROM " + Global.SAP_DB + @".dbo.OIGN where DocEntry = @docEntry) 
                                            WHERE TransSeq = @code";
                                    _logger.LogInformation(" ProductionController : ProductionReceipt() : Update Transaction Table Query : {q} ", _Query.ToString());

                                    cmd = new SqlCommand(_Query, QITcon);
                                    cmd.Parameters.AddWithValue("@docEntry", docEntry);
                                    cmd.Parameters.AddWithValue("@code", _TransSeq);

                                    QITcon.Open();
                                    intNum = cmd.ExecuteNonQuery();
                                    QITcon.Close();

                                    if (intNum > 0)
                                    {
                                        return Ok(new
                                        {
                                            StatusCode = "200",
                                            IsSaved = "Y",
                                            TransSeq = _TransSeq,
                                            DocEntry = docEntry,
                                            StatusMsg = "Production Receipt added successfully !!!"
                                        });
                                    }
                                    else
                                    {
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
                                this.DeleteTransactionProToReceipt(_TransSeq.ToString());
                                this.DeleteQRStockDetProToReceipt(_QRTransSeq.ToString());
                                this.DeleteGeneratedProDetQR(payload.ProOrdDocEntry, payload.RecNo, payload.ItemCode);
                                this.DeleteQRStockDetProInvTrans(_QRTransSeqInvTrans.ToString());
                                return BadRequest(new
                                {
                                    StatusCode = "400",
                                    IsSaved = "N",
                                    StatusMsg = "Problem while saving Receipt data"
                                });
                            }
                        }
                        else
                        {
                            this.DeleteTransactionProToReceipt(_TransSeq.ToString());
                            this.DeleteQRStockDetProToReceipt(_QRTransSeq.ToString());
                            this.DeleteGeneratedProDetQR(payload.ProOrdDocEntry, payload.RecNo, payload.ItemCode);
                            this.DeleteQRStockDetProInvTrans(_QRTransSeqInvTrans.ToString());
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
                            TransSeq = _TransSeq,
                            StatusMsg = "Error code: " + ((WMS_UI_API.Services.SAPConnectionService)_sapConnectionService).oComp.GetLastErrorCode() + Environment.NewLine +
                                           "Error message: " + ((WMS_UI_API.Services.SAPConnectionService)_sapConnectionService).oComp.GetLastErrorDescription()
                        });
                    } 
                }
                else
                {
                    this.DeleteTransactionProToReceipt(_TransSeq.ToString());
                    this.DeleteQRStockDetProToReceipt(_QRTransSeq.ToString());
                    this.DeleteGeneratedProDetQR(payload.ProOrdDocEntry, payload.RecNo, payload.ItemCode);
                    this.DeleteQRStockDetProInvTrans(_QRTransSeqInvTrans.ToString());
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
                this.DeleteTransactionProToReceipt(_TransSeq.ToString());
                this.DeleteQRStockDetProToReceipt(_QRTransSeq.ToString());
                this.DeleteGeneratedProDetQR(payload.ProOrdDocEntry, payload.RecNo, payload.ItemCode);
                this.DeleteQRStockDetProInvTrans(_QRTransSeqInvTrans.ToString());
                _logger.LogError(" Error in ProductionController : ProductionReceipt() :: {Error}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
            }
            finally
            { 
            }
        }


        [HttpPost("GetDraftReceiptList")]
        public async Task<ActionResult<IEnumerable<DraftReceiptList>>> GetDraftReceiptList(int Series)
        {
            SqlConnection QITcon;
            SqlDataAdapter oAdptr;
            try
            {
                _logger.LogInformation(" Calling ProductionController : GetDraftReceiptList() ");

                System.Data.DataTable dtData = new DataTable();
                QITcon = new SqlConnection(_QIT_connection);

                _Query = @" 
                 SELECT DISTINCT B.DocEntry ProOrdDocEntry, B.DocNum ProOrdDocNum, B.PostDate ProOrdDocDate,
		                         B.ItemCode, B.ProdName ItemName, B.PlannedQty,
                                 B.Series, B.Comments 
                 FROM " + Global.SAP_DB + @".dbo.OWOR B  
				 INNER JOIN QIT_Draft_Receipt C on C.ProOrdDocEntry = B.DocEntry
                 WHERE B.Series = @series
                 ";

                _logger.LogInformation(" ProductionController : GetDraftReceiptList() Query : {q} ", _Query.ToString());
                QITcon.Open();
                oAdptr = new SqlDataAdapter(_Query, QITcon);
                oAdptr.SelectCommand.Parameters.AddWithValue("@series", Series);
                oAdptr.Fill(dtData);
                QITcon.Close();

                if (dtData.Rows.Count > 0)
                {
                    List<DraftReceiptList> obj = new List<DraftReceiptList>();
                    dynamic arData = JsonConvert.SerializeObject(dtData);
                    obj = JsonConvert.DeserializeObject<List<DraftReceiptList>>(arData.ToString());
                    return obj;
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "No data found" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(" Error in ProductionController : GetDraftReceiptList() :: {Error}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
            }
        }


        [HttpPost("GetDraftReceiptDetail")]
        public async Task<ActionResult<IEnumerable<DraftReceiptDetail>>> GetDraftReceiptDetail(int ProductionOrderEntry)
        {
            SqlConnection QITcon;
            SqlDataAdapter oAdptr;
            try
            {
                _logger.LogInformation(" Calling ProductionController : GetDraftReceiptDetail() ");

                System.Data.DataTable dtData = new DataTable();
                QITcon = new SqlConnection(_QIT_connection);

                _Query = @" 
                SELECT B.DocEntry ProOrdDocEntry, B.DocNum ProOrdDocNum, B.PostDate ProOrdDocDate, C.Series, 
                       B.ItemCode, B.ProdName ItemName, B.PlannedQty,
		               C.ReceiptQty, B.Comments, C.Comment RecComment, D.ItemMngBy, D.QRMngBy, C.RecNo, 
                       C.WhsCode, C.BinAbsEntry, C.Project, B.Uom UomCode
                FROM " + Global.SAP_DB + @".dbo.OWOR B  
				     INNER JOIN QIT_Draft_Receipt C on C.ProOrdDocEntry = B.DocEntry
                     INNER JOIN QIT_Item_Master D on D.ItemCode collate SQL_Latin1_General_CP850_CI_AS = B.ItemCode
                WHERE B.DocEntry = @docEntry
				ORDER BY C.TransId
                ";

                _logger.LogInformation(" ProductionController : GetDraftReceiptDetail() Query : {q} ", _Query.ToString());
                QITcon.Open();
                oAdptr = new SqlDataAdapter(_Query, QITcon);
                oAdptr.SelectCommand.Parameters.AddWithValue("@docEntry", ProductionOrderEntry);
                oAdptr.Fill(dtData);
                QITcon.Close();

                if (dtData.Rows.Count > 0)
                {
                    List<DraftReceiptDetail> obj = new List<DraftReceiptDetail>();
                    dynamic arData = JsonConvert.SerializeObject(dtData);
                    obj = JsonConvert.DeserializeObject<List<DraftReceiptDetail>>(arData.ToString());
                    return obj;
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "No data found" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(" Error in ProductionController : GetDraftReceiptDetail() :: {Error}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
            }
        }


        [HttpPost("ProductionDraftReceipt")]
        public IActionResult ProductionDraftReceipt(ProductionReceipt payload)
        {
            string _IsSaved = "N";
            int DetSucess = 0;
            int DetFail = 0;
            Object _RecNo = "";

            SqlConnection QITcon;
            SqlDataAdapter oAdptr;

            try
            {
                _logger.LogInformation(" Calling ProductionController : ProductionDraftReceipt() ");

                if (payload != null)
                {

                    #region Get RecNo - Draft Receipt
                    QITcon = new SqlConnection(_QIT_connection);
                    _Query = @" SELECT ISNULL(max(RecNo),0) + 1 FROM QIT_Draft_Receipt A  ";
                    _logger.LogInformation("ProductionController : RecNo Query : {q} ", _Query.ToString());
                    cmd = new SqlCommand(_Query, QITcon);
                    QITcon.Open();
                    _RecNo = cmd.ExecuteScalar();
                    QITcon.Close();
                    #endregion

                    _Query = @"
                    INSERT INTO QIT_Draft_Receipt(RecNo, ProOrdDocEntry, Series, WhsCode, BinAbsEntry, Project, ReceiptQty, Comment) 
                    VALUES ( @recNo, @proDocEntry, @Series, @Whs, @bin, @proj, @recqty, @comment)  ";

                    _logger.LogInformation("ProductionController : ProductionDraftReceipt : {q} ", _Query.ToString());

                    cmd = new SqlCommand(_Query, QITcon);
                    cmd.Parameters.AddWithValue("@recNo", _RecNo);
                    cmd.Parameters.AddWithValue("@proDocEntry", payload.ProOrdDocEntry);
                    cmd.Parameters.AddWithValue("@Series", payload.Series);
                    cmd.Parameters.AddWithValue("@Whs", payload.WhsCode);
                    cmd.Parameters.AddWithValue("@bin", payload.BinAbsEntry);
                    cmd.Parameters.AddWithValue("@proj", payload.Project);
                    cmd.Parameters.AddWithValue("@recqty", payload.ReceiptQty);
                    cmd.Parameters.AddWithValue("@comment", payload.Comment);
                    QITcon.Open();
                    int intNum = cmd.ExecuteNonQuery();
                    QITcon.Close();

                    if (intNum > 0)
                    {
                        _IsSaved = "Y";
                        if (payload.proReworkDet != null)
                        {
                            foreach (var item in payload.proReworkDet)
                            {
                                int intDetIns = 0;

                                _Query = @"
                                INSERT INTO QIT_ProRework_Reason(TransId, ProOrdDocEntry, DraftReceiptNo, DeptId, Hours, Delay) 
                                VALUES ( (select ISNULL(max(TransId),0) + 1 from QIT_ProRework_Reason), @proDocEntry, @dRecNo, @deptId, @hours, @delay)  ";

                                _logger.LogInformation("ProductionController : Pro Rework Entry : {q} ", _Query.ToString());

                                cmd = new SqlCommand(_Query, QITcon);
                                cmd.Parameters.AddWithValue("@proDocEntry", payload.ProOrdDocEntry);
                                cmd.Parameters.AddWithValue("@dRecNo", _RecNo);
                                cmd.Parameters.AddWithValue("@deptId", item.deptId);
                                cmd.Parameters.AddWithValue("@hours", item.hours);
                                cmd.Parameters.AddWithValue("@delay", item.delay);
                                QITcon.Open();
                                intDetIns = cmd.ExecuteNonQuery();
                                QITcon.Close();
                                if (intDetIns > 0)
                                    DetSucess = DetSucess + 1;
                                else
                                    DetFail = DetFail + 1;
                            }
                        }
                    }
                    else
                        _IsSaved = "N";

                    if (_IsSaved == "Y" && (DetFail <= 0))
                        return Ok(new { StatusCode = "200", IsSaved = _IsSaved, StatusMsg = "Saved Successfully!!!" });
                    else
                    {
                        DeleteDraftReceiptData(_RecNo.ToString());
                        return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Problem in saving Draft Receipt data" });
                    }
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Details not found" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(" Error in ProductionController : ProductionDraftReceipt() :: {Error}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
            }
        }

        #endregion


        #region Delete API                          

        private bool DeleteTransactionProToIssue(string _TransSeq)
        {
            SqlConnection QITcon;
            string _IsSaved = "N";
            try
            {
                _logger.LogInformation(" Calling ProductionController : DeleteTransactionProToIssue() ");
                string p_ErrorMsg = string.Empty;

                QITcon = new SqlConnection(_QIT_connection);
                _Query = @" DELETE FROM QIT_Trans_ProToIssue WHERE TransSeq = @transSeq";
                _logger.LogInformation(" ProductionController : DeleteTransactionProToIssue Query : {q} ", _Query.ToString());

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
                _logger.LogError("Error in ProductionController : DeleteTransactionProToIssue() :: {Error}", ex.ToString());
                return false;
            }
        }

        private bool DeleteQRStockDetProToIssue(string _QRTransSeq)
        {
            SqlConnection QITcon;
            string _IsSaved = "N";
            try
            {
                _logger.LogInformation(" Calling ProductionController : DeleteQRStockDetProToIssue() ");
                string p_ErrorMsg = string.Empty;

                QITcon = new SqlConnection(_QIT_connection);
                _Query = @" DELETE FROM QIT_QRStock_ProToIssue WHERE QRTransSeq = @qrtransSeq";
                _logger.LogInformation(" ProductionController : DeleteQRStockDetProToIssue Query : {q} ", _Query.ToString());

                cmd = new SqlCommand(_Query, QITcon);
                cmd.Parameters.AddWithValue("@qrtransSeq", _QRTransSeq);
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
                _logger.LogError("Error in ProductionController : DeleteQRStockDetProToIssue() :: {Error}", ex.ToString());
                return false;
            }
        }

        private bool DeleteTransactionProToReceipt(string _TransSeq)
        {
            SqlConnection QITcon;
            string _IsSaved = "N";
            try
            {
                _logger.LogInformation(" Calling ProductionController : DeleteTransactionProToReceipt() ");
                string p_ErrorMsg = string.Empty;

                QITcon = new SqlConnection(_QIT_connection);
                _Query = @" DELETE FROM QIT_Trans_ProToReceipt WHERE TransSeq = @transSeq";
                _logger.LogInformation(" ProductionController : DeleteTransactionProToReceipt Query : {q} ", _Query.ToString());

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
                _logger.LogError("Error in ProductionController : DeleteTransactionProToReceipt() :: {Error}", ex.ToString());
                return false;
            }
        }

        private bool DeleteQRStockDetProToReceipt(string _QRTransSeq)
        {
            SqlConnection QITcon;
            string _IsSaved = "N";
            try
            {
                _logger.LogInformation(" Calling ProductionController : DeleteQRStockDetProToReceipt() ");
                string p_ErrorMsg = string.Empty;

                QITcon = new SqlConnection(_QIT_connection);
                _Query = @" DELETE FROM QIT_QRStock_ProToReceipt WHERE QRTransSeq = @qrtransSeq";
                _logger.LogInformation(" ProductionController : DeleteQRStockDetProToReceipt Query : {q} ", _Query.ToString());

                cmd = new SqlCommand(_Query, QITcon);
                cmd.Parameters.AddWithValue("@qrtransSeq", _QRTransSeq);
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
                _logger.LogError("Error in ProductionController : DeleteQRStockDetProToReceipt() :: {Error}", ex.ToString());
                return false;
            }
        }

        private bool DeleteQRStockDetProInvTrans(string _QRTransSeq)
        {
            string _IsSaved = "N";
            try
            {
                _logger.LogInformation(" Calling ProductionController : DeleteQRStockDetProInvTrans() ");

                string p_ErrorMsg = string.Empty;

                SqlConnection con = new SqlConnection(_QIT_connection);
                _Query = @" DELETE FROM QIT_ProQRStock_InvTrans WHERE QRTransSeq = @qrtransSeq";
                _logger.LogInformation(" ProductionController : DeleteQRStockDetProInvTrans Query : {q} ", _Query.ToString());

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
                _logger.LogError("Error in ProductionController : DeleteQRStockDetProInvTrans() :: {Error}", ex.ToString());
                return false; 
            }
        }


        private bool DeleteGeneratedProDetQR(int p_ProOrdDocEntry, int p_RecNo, string p_ItemCode)
        {
            SqlConnection QITcon;
            try
            {
                //DeleteGeneratedQR(payload.ProOrdDocEntry, payload.RecNo, payload.ItemCode);
                _logger.LogInformation(" Calling ProductionController : DeleteGeneratedProDetQR() ");
                string p_ErrorMsg = string.Empty;

                QITcon = new SqlConnection(_QIT_connection);
                _Query = @" DELETE A FROM QIT_Production_QR_Detail A inner join QIT_Production_QR_Header B on B.HeaderSrNo = A.HeaderSrNo
                            WHERE A.RecNo = @recNo and B.DocEntry = @proOrdDocEntry and B.ObjType = 202 and A.ItemCode = @itemCode 
                          ";
                _logger.LogInformation(" ProductionController : DeleteGeneratedProDetQR Query : {q} ", _Query.ToString());

                cmd = new SqlCommand(_Query, QITcon);
                cmd.Parameters.AddWithValue("@recNo", p_RecNo);
                cmd.Parameters.AddWithValue("@proOrdDocEntry", p_ProOrdDocEntry);
                cmd.Parameters.AddWithValue("@itemCode", p_ItemCode);
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
                _logger.LogError("Error in ProductionController : DeleteGeneratedProDetQR() :: {Error}", ex.ToString());
                return false;
            }
        }

        private bool DeleteDraftReceiptData(string p_RecNo)
        {
            SqlConnection QITcon;
            try
            {
                _logger.LogInformation(" Calling ProductionController : DeleteDraftReceiptData() ");
                string p_ErrorMsg = string.Empty;

                QITcon = new SqlConnection(_QIT_connection);
                _Query = @" DELETE  FROM QIT_Draft_Receipt WHERE RecNo = @recNo 
                            DELETE FROM QIT_ProRework_Reason where DraftReceiptNo = @recNo
                          ";
                _logger.LogInformation(" ProductionController : DeleteDraftReceiptData Query : {q} ", _Query.ToString());

                cmd = new SqlCommand(_Query, QITcon);
                cmd.Parameters.AddWithValue("@recNo", p_RecNo);
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
                _logger.LogError("Error in ProductionController : DeleteDraftReceiptData() :: {Error}", ex.ToString());
                return false;
            }
        }

        //[HttpDelete("DeleteReceipt")]
        //public IActionResult DeleteReceipt(string TransID)
        //{
        //    string _IsSaved = "N";
        //    try
        //    {
        //        _logger.LogInformation(" Calling ProductionController : DeleteReceipt() ");
        //        string p_ErrorMsg = string.Empty;

        //        SqlConnection con = new SqlConnection(_QIT_connection);
        //        _Query = @" DELETE FROM QIT_Receipt WHERE TransID = @transID";
        //        _logger.LogInformation(" ProductionController : DeleteReceipt Query : {q} ", _Query.ToString());

        //        cmd = new SqlCommand(_Query, con);
        //        cmd.Parameters.AddWithValue("@transID", TransID);
        //        con.Open();
        //        int intNum = cmd.ExecuteNonQuery();
        //        con.Close();

        //        if (intNum > 0)
        //            _IsSaved = "Y";
        //        else
        //            _IsSaved = "N";

        //        return Ok(new { StatusCode = "200", IsSaved = _IsSaved, StatusMsg = "Deleted Successfully!!!" });
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError("Error in ProductionController : DeleteReceipt() :: {Error}", ex.ToString());
        //        return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = ex.ToString() });
        //    }
        //}

        private bool DeleteDraftIssNo(string p_IssNo)
        {
            SqlConnection QITcon;
            try
            {
                _logger.LogInformation(" Calling ProductionController : DeleteDraftIssNo() ");
                string p_ErrorMsg = string.Empty;

                QITcon = new SqlConnection(_QIT_connection);
                _Query = @" DELETE FROM QIT_Draft_Issue  
                            WHERE IssNo = @issNo ";
                _logger.LogInformation(" ProductionController : DeleteDraftIssNo Query : {q} ", _Query.ToString());

                cmd = new SqlCommand(_Query, QITcon);
                cmd.Parameters.AddWithValue("@issNo", p_IssNo);
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
                _logger.LogError("Error in ProductionController : DeleteDraftIssNo() :: {Error}", ex.ToString());
                return false;
            }
        }

        #endregion


        #region Handle Temp Table for Multiple Production Issue

        [HttpPost("SaveTempIssue")]
        public IActionResult SaveTempIssue([FromBody] SaveTempProIssue payload)
        {
            SqlConnection QITcon;
            string _IsSaved = "N";
            try
            {
                _logger.LogInformation(" Calling ProductionController : SaveTempIssue() ");

                string p_ErrorMsg = string.Empty;

                if (payload != null)
                {
                    #region Check Branch
                    if (payload.BranchID == 0)
                    {
                        return BadRequest(new
                        {
                            StatusCode = "400",
                            StatusMsg = "Provide Branch"
                        });
                    }
                    #endregion

                    QITcon = new SqlConnection(_QIT_connection);
                    _Query = @"INSERT INTO QIT_Temp_ProIssue(BranchID, ProOrdDocEntry, ProOrdDocNum, ItemCode, QRCodeID, Qty) 
                           VALUES ( @bID, @docEntry, @docNum, @itemCode, @qr, @qty)";
                    _logger.LogInformation(" ProductionController : SaveTempIssue() Query : {q} ", _Query.ToString());

                    cmd = new SqlCommand(_Query, QITcon);
                    cmd.Parameters.AddWithValue("@bID", payload.BranchID);
                    cmd.Parameters.AddWithValue("@docEntry", payload.ProOrdDocEntry);
                    cmd.Parameters.AddWithValue("@docNum", payload.ProOrdDocNum);
                    cmd.Parameters.AddWithValue("@itemCode", payload.ItemCode);
                    cmd.Parameters.AddWithValue("@qr", payload.DetailQRCodeID.Replace(" ", "~"));
                    cmd.Parameters.AddWithValue("@qty", payload.Qty);

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
                    return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Payload Details not found" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Error in ProductionController : SaveTempIssue() :: {Error}", ex.ToString());


                return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = ex.Message.ToString() });

            }
        }


        [HttpDelete("DeleteTempIssue")]
        public IActionResult DeleteTempIssue(DeleteTempProIssue payload)
        {
            SqlConnection QITcon;
            string _IsSaved = "N";
            try
            {
                _logger.LogInformation(" Calling ProductionController : DeleteTempIssue() ");

                string p_ErrorMsg = string.Empty;

                QITcon = new SqlConnection(_QIT_connection);
                _Query = @" DELETE FROM QIT_Temp_ProIssue WHERE ItemCode = @iCode and QRCodeID = @qr and ProOrdDocEntry = @docEntry and ISNULL(BranchID, @bID) = @bID";
                _logger.LogInformation(" ProductionController : DeleteTempIssue() Query : {q} ", _Query.ToString());

                cmd = new SqlCommand(_Query, QITcon);
                cmd.Parameters.AddWithValue("@iCode", payload.ItemCode);
                cmd.Parameters.AddWithValue("@qr", payload.DetailQRCodeID.Replace(" ", "~"));
                cmd.Parameters.AddWithValue("@docEntry", payload.ProOrdDocEntry);
                cmd.Parameters.AddWithValue("@bID", payload.BranchID);

                QITcon.Open();
                int intNum = cmd.ExecuteNonQuery();
                QITcon.Close();

                if (intNum > 0)
                    _IsSaved = "Y";
                else
                    _IsSaved = "N";

                return Ok(new { StatusCode = "200", IsSaved = _IsSaved, StatusMsg = "Deleted Successfully!!!" });
            }
            catch (Exception ex)
            {
                _logger.LogError("Error in ProductionController : DeleteTempIssue() :: {Error}", ex.ToString());
                return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = ex.ToString() });
            }
        }
        #endregion

    }
}
