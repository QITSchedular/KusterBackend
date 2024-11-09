using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using SAPbobsCOM;
using SAPbouiCOM;
using System.Data.SqlClient;
using WMS_UI_API.Common;
using WMS_UI_API.Models;
using WMS_UI_API.Services;

namespace WMS_UI_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class InventoryTransferController : ControllerBase
    {
        private string _ApplicationApiKey = string.Empty;
        private string _connection = string.Empty;
        private string _QIT_connection = string.Empty;
        private string _QIT_DB = string.Empty;
        private string _Query = string.Empty;
        private SqlCommand cmd;
        public Global objGlobal;

        public IConfiguration Configuration { get; }
        private readonly ILogger<InventoryTransferController> _logger;
        private readonly ISAPConnectionService _sapConnectionService;

        public InventoryTransferController(IConfiguration configuration, ILogger<InventoryTransferController> logger, ISAPConnectionService sapConnectionService)
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
                _logger.LogError("Error in InventoryTransferController :: {Error}" + ex.ToString());
            }
        }


        [HttpGet("GetWhsStockByQR")]
        public async Task<ActionResult<IEnumerable<AvailableQRWhs>>> GetWhsStockByQR(int BranchId, string QRCodeID)
        {
            SqlConnection QITcon;
            SqlDataAdapter oAdptr;
            try
            {
                _logger.LogInformation(" Calling InventoryTransferController : GetWhsStockByQR() ");

                List<AvailableQRWhs> obj = new List<AvailableQRWhs>();
                System.Data.DataTable dtQRData = new System.Data.DataTable();
                System.Data.DataTable dtData = new System.Data.DataTable();
                QITcon = new SqlConnection(_QIT_connection);

                _Query = @"
                SELECT A.QRCodeID, A.BatchSerialNo, A.ItemCode, A.LineNum FROM QIT_QR_Detail A 
                WHERE ISNULL(A.BranchID, @bID) = @bID and A.QRCodeId = @dQR 

                UNION

                SELECT A.QRCodeID, A.BatchSerialNo, A.ItemCode, 0 LineNum FROM QIT_Production_QR_Detail A 
                WHERE ISNULL(A.BranchID, @bID) = @bID and A.QRCodeId = @dQR 

                UNION

                SELECT A.QRCodeID, A.BatchSerialNo, A.ItemCode, DocLineNum LineNum FROM QIT_OpeningStock_QR_Detail A 
                WHERE ISNULL(A.BranchID, @bID) = @bID and A.QRCodeId = @dQR 

                ";
                _logger.LogInformation(" InventoryTransferController : Query : {q} ", _Query.ToString());
                QITcon.Open();
                oAdptr = new SqlDataAdapter(_Query, QITcon);
                oAdptr.SelectCommand.Parameters.AddWithValue("@bID", BranchId);
                oAdptr.SelectCommand.Parameters.AddWithValue("@dQR", QRCodeID.Replace(" ", "~"));

                oAdptr.Fill(dtQRData);
                QITcon.Close();

                if (dtQRData.Rows.Count <= 0)
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "No such QR exist" });
                }

                #region Query
                _Query = @"  
                SELECT A.WhsCode, A.WhsName, (SELECT BinActivat FROM " + Global.SAP_DB + @".dbo.OWHS where WhsCode = A.WhsCode) BinActivat,
	                   A.AbsEntry BinAbsEntry,(SELECT BinCode FROM " + Global.SAP_DB + @".dbo.OBIN where AbsEntry = A.AbsEntry) BinCode, A.Stock 
                FROM 
                (
                    SELECT C.WhsCode, C.WhsName, B.AbsEntry, SUM(A.Stock) Stock FROM 
                    (   
                        SELECT Z.ToWhs WhsCode, Z.ToBinAbsEntry Bin, ISNULL(sum(Z.Qty),0) * 1 Stock FROM [QIT_QRStock_InvTrans] Z
                        WHERE  Z.QRCodeID = @dQR and Z.FromObjType = '22' and Z.ToObjType = '67' and ISNULL(Z.BranchID, @bID) = @bID
                        GROUP BY Z.ToWhs, Z.ToBinAbsEntry	
      
                        UNION 
	     
                        SELECT Z.ToWhs WhsCode, Z.ToBinAbsEntry Bin, ISNULL(sum(Z.Qty),0) * 1 Stock FROM [QIT_QRStock_InvTrans] Z
                        WHERE  Z.QRCodeID = @dQR and (Z.FromObjType <> '22') and ISNULL(Z.BranchID, @bID) = @bID
                        GROUP BY Z.ToWhs, Z.ToBinAbsEntry

                        UNION 

                        SELECT Z.FromWhs WhsCode, Z.FromBinAbsEntry Bin, ISNULL(sum(Z.Qty),0) * -1 Stock FROM [QIT_QRStock_InvTrans] Z
                        WHERE Z.QRCodeID = @dQR and (Z.FromObjType <> '22') and ISNULL(Z.BranchID, @bID) = @bID
                        GROUP BY Z.FromWhs, Z.FromBinAbsEntry

                        UNION 

                        SELECT Z.FromWhs WhsCode, Z.FromBinAbsEntry Bin, ISNULL(sum(Z.Qty),0) * -1 Stock FROM QIT_QRStock_ProToIssue Z
                        WHERE Z.QRCodeID = @dQR and ISNULL(Z.BranchID, @bID) = @bID
                        GROUP BY Z.FromWhs, Z.FromBinAbsEntry

                        UNION

                        SELECT Z.FromWhs WhsCode, Z.FromBinAbsEntry Bin, ISNULL(sum(Z.Qty),0) * -1 Stock FROM QIT_QRStock_SOToDelivery Z
                        WHERE Z.QRCodeID = @dQR and ISNULL(Z.BranchID, @bID) = @bID
                        GROUP BY Z.FromWhs, Z.FromBinAbsEntry

                        UNION

                        SELECT Z.ToWhs WhsCode, Z.ToBinAbsEntry Bin, ISNULL(sum(Z.Qty),0) * 1 Stock FROM QIT_QRStock_ProToReceipt Z
                             WHERE Z.QRCodeID = @dQR and ISNULL(Z.BranchID, @bID) = @bID
                        GROUP BY Z.ToWhs, Z.ToBinAbsEntry

                        UNION

                        SELECT Z.ToWhs WhsCode, Z.ToBinAbsEntry Bin, ISNULL(sum(Z.Qty),0) * 1 Stock FROM [QIT_ProQRStock_InvTrans] Z
                        WHERE  Z.QRCodeID = @dQR and Z.FromObjType = '202' and Z.ToObjType = '59' and ISNULL(Z.BranchID, @bID) = @bID
                        GROUP BY Z.ToWhs, Z.ToBinAbsEntry	
      
                        UNION 
	     
                        SELECT Z.ToWhs WhsCode, Z.ToBinAbsEntry Bin, ISNULL(sum(Z.Qty),0) * 1 Stock FROM [QIT_ProQRStock_InvTrans] Z
                        WHERE  Z.QRCodeID = @dQR and (Z.FromObjType <> '202') and ISNULL(Z.BranchID, @bID) = @bID
                        GROUP BY Z.ToWhs, Z.ToBinAbsEntry
 
                        UNION 

                        SELECT Z.FromWhs WhsCode, Z.FromBinAbsEntry Bin, ISNULL(sum(Z.Qty),0) * -1 Stock FROM [QIT_ProQRStock_InvTrans] Z
                        WHERE Z.QRCodeID = @dQR and (Z.FromObjType <> '202') and ISNULL(Z.BranchID, @bID) = @bID
                        GROUP BY Z.FromWhs, Z.FromBinAbsEntry

                        UNION

                        SELECT Z.ToWhs WhsCode, Z.ToBinAbsEntry Bin, ISNULL(sum(Z.Qty),0) * 1 Stock FROM [QIT_OSQRStock_InvTrans] Z
                        WHERE  Z.QRCodeID = @dQR and Z.FromObjType = '310000001' and Z.ToObjType = '310000001' and ISNULL(Z.BranchID, @bID) = @bID
                        GROUP BY Z.ToWhs, Z.ToBinAbsEntry	
      
                        UNION 
	      
                        SELECT Z.ToWhs WhsCode, Z.ToBinAbsEntry Bin, ISNULL(sum(Z.Qty),0) * 1 Stock FROM [QIT_OSQRStock_InvTrans] Z
                        WHERE  Z.QRCodeID = @dQR and (Z.FromObjType <> '310000001') and ISNULL(Z.BranchID, @bID) = @bID
                        GROUP BY Z.ToWhs, Z.ToBinAbsEntry

                        UNION 

                        SELECT Z.FromWhs WhsCode, Z.FromBinAbsEntry Bin, ISNULL(sum(Z.Qty),0) * -1 Stock FROM [QIT_OSQRStock_InvTrans] Z
                        WHERE Z.QRCodeID = @dQR and (Z.FromObjType <> '310000001') and ISNULL(Z.BranchID, @bID) = @bID
                        GROUP BY Z.FromWhs, Z.FromBinAbsEntry

                    ) as A
                    INNER JOIN " + Global.SAP_DB + @".dbo.OWHS C ON C.WhsCode collate SQL_Latin1_General_CP1_CI_AS = A.WhsCode
                    LEFT JOIN " + Global.SAP_DB + @".dbo.OBIN B ON A.Bin = B.AbsEntry 
                    GROUP BY C.WhsCode, C.WhsName, B.AbsEntry
                    HAVING SUM(A.Stock) > 0
                ) as A
                ";
                #endregion

                _logger.LogInformation(" InventoryTransferController : GetWhsStockByQR() Query : {q} ", _Query.ToString());
                QITcon.Open();
                oAdptr = new SqlDataAdapter(_Query, QITcon);
                oAdptr.SelectCommand.Parameters.AddWithValue("@bID", BranchId);
                oAdptr.SelectCommand.Parameters.AddWithValue("@dQR", QRCodeID.Replace(" ", "~"));
                oAdptr.Fill(dtData);
                QITcon.Close();

                if (dtData.Rows.Count <= 0)
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Stock not available for QR : " + QRCodeID.Replace("~", " ") });
                }
                else
                {
                    dynamic arData = JsonConvert.SerializeObject(dtData);
                    obj = JsonConvert.DeserializeObject<List<AvailableQRWhs>>(arData.ToString().Replace("~", " "));
                    return obj;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(" Error in InventoryTransferController : GetWhsStockByQR() :: {Error}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
            }
        }


        [HttpPost("ItemDataInWhs")]
        public async Task<ActionResult<IEnumerable<ValidItemData>>> ItemDataInWhs(ValidateItemInIT payload)
        {
            SqlConnection QITcon;
            SqlDataAdapter oAdptr;
            try
            {
                _logger.LogInformation(" Calling InventoryTransferController : ItemDataInWhs() ");
                string _ToBinFilter = string.Empty;
                string _FromBinFilter = string.Empty;

                if (payload.FromBinAbsEntry > 0)
                {
                    _FromBinFilter += " and FromBinAbsEntry = @fromBin ";
                    _ToBinFilter += " and ToBinAbsEntry = @fromBin ";
                }

                List<ValidItemData> obj = new List<ValidItemData>();
                System.Data.DataTable dtData = new System.Data.DataTable();
                QITcon = new SqlConnection(_QIT_connection);

                if (payload.DetailQRCodeID.Replace(" ", "~").Contains("~PO~"))
                {
                    #region Query
                    _Query = @"  
                    SELECT A.BranchID, A.QRCodeID DetailQRCodeID, A.ItemCode, B.ItemName, A.BatchSerialNo, WhsQ,
                           @frwhs Whs, A.Q Qty, A.Q TransQty, F.CardCode, F.CardName, G.GateInNo, I.Project, G.UoMCode, H.ItemMngBy,  
                           ( SELECT sum(OnHand) FROM " + Global.SAP_DB + @".dbo.OITW 
                             WHERE ItemCode collate SQL_Latin1_General_CP1_CI_AS = A.ItemCode
                           ) ItemStock,
                           ( SELECT sum(OnHand) FROM " + Global.SAP_DB + @".dbo.OITW 
                             WHERE ItemCode collate SQL_Latin1_General_CP1_CI_AS = A.ItemCode and WhsCode = @frwhs
                           ) ItemWhsStock
                           
                    FROM 
                    (	
                        SELECT A.BranchID, A.TransId, A.QRTransSeq, A.TransSeq, A.QRCodeID, A.ItemCode, A.BatchSerialNo, 
                               A.FromObjType, A.ToBinAbsEntry, A.FromBinAbsEntry,
                               A.ToObjType, A.FromWhs, A.ToWhs, CASE WHEN A.Q = 0 then A.Qty else A.Q end Q, A.Q WhsQ
                        FROM 
                        (
                            SELECT TOP 1 *, 
                                   ( SELECT ISNULL( ISNULL(AA.A,0) + ISNULL(BB.B,0) - ISNULL(CC.C,0) ,0)
                                     FROM 
                                     (
                                        SELECT ISNULL(sum(qty),0) A FROM QIT_QRStock_InvTrans A
                                        WHERE A.QRCodeID = @dQR AND ISNULL(A.BranchID, @bID) = @bID and 
                                              A.FromObjType = '22' and A.ToObjType = '67' and 
                                              A.ToWhs = @frwhs " + _ToBinFilter + @"
                                     ) AA LEFT JOIN
                                     (
                                        SELECT ISNULL(sum(qty),0) B FROM QIT_QRStock_InvTrans A
                                        WHERE A.QRCodeID = @dQR AND ISNULL(A.BranchID, @bID) = @bID and 
                                              A.FromObjType <> '22' and A.ToWhs = @frwhs " + _ToBinFilter + @"
                                     ) BB ON 1 = 1 LEFT JOIN
                                     (
		                                SELECT ISNULL(sum(qty),0) C FROM QIT_QRStock_InvTrans A
                                        WHERE A.QRCodeID = @dQR AND ISNULL(A.BranchID, @bID) = @bID and
		                                      A.FromObjType <> '22' and A.FromWhs = @frwhs " + _FromBinFilter + @"
                                     ) CC ON 1 = 1
                                   ) Q	
                            FROM QIT_QRStock_InvTrans A
                            WHERE A.QRCodeID = @dQR AND ISNULL(A.BranchID, @bID) = @bID
                            ORDER BY TransId desc
                        ) A
                    ) as A
                    INNER JOIN " + Global.SAP_DB + @".dbo.OITM B ON A.ItemCode collate SQL_Latin1_General_CP850_CI_AS= B.ItemCode
                    INNER JOIN " + Global.SAP_DB + @".dbo.OITW C ON C.ItemCode = B.ItemCode and 
                               C.WhsCode collate SQL_Latin1_General_CP850_CI_AS = @frwhs AND 
                               C.OnHand > 0 AND A.Q <= C.OnHand
                    INNER JOIN QIT_QR_Detail D ON A.QRCodeID = D.QRCodeID
                    INNER JOIN QIT_QR_Header E ON E.HeaderSrNo = D.HeaderSrNo
                    INNER JOIN QIT_GateIN G ON G.DocEntry = E.DocEntry and G.ObjType = E.ObjType and 
                               G.GateInNo = D.GateInNo and G.ItemCode = D.ItemCode
                    INNER JOIN " + Global.SAP_DB + @".dbo.OPOR F on F.DocEntry = E.DocEntry and F.DocNum = E.DocNum and 
                               F.ObjType collate SQL_Latin1_General_CP1_CI_AS = E.ObjType
                    INNER JOIN " + Global.SAP_DB + @".dbo.POR1 I ON F.DocEntry = I.DocEntry AND 
				               I.ItemCode collate SQL_Latin1_General_CP1_CI_AS = D.ItemCode and D.LineNum = I.LineNum
                    INNER JOIN QIT_Item_Master H on H.ItemCode = A.ItemCode
                    FOR BROWSE
                    ";
                    #endregion
                }
                if (payload.DetailQRCodeID.Replace(" ", "~").Contains("~PRO~"))
                {
                    #region Query
                    _Query = @"  
                    SELECT A.BranchID, A.QRCodeID DetailQRCodeID, A.ItemCode, B.ItemName, A.BatchSerialNo, WhsQ,
                           @frwhs Whs, A.Q Qty, A.Q TransQty, F.CardCode, '' CardName , G.RecNo GateInNo, F.Project, H.UoMCode, I.ItemMngBy,
                           ( SELECT sum(OnHand) FROM " + Global.SAP_DB + @".dbo.OITW 
                             WHERE ItemCode collate SQL_Latin1_General_CP1_CI_AS = A.ItemCode
                           ) ItemStock,
	                       ( SELECT sum(OnHand) FROM " + Global.SAP_DB + @".dbo.OITW 
                             WHERE ItemCode collate SQL_Latin1_General_CP1_CI_AS = A.ItemCode and WhsCode =  @frwhs
                           ) ItemWhsStock
                    FROM 
                    (	
                        SELECT A.BranchID, A.TransId, A.QRTransSeq, A.TransSeq, A.QRCodeID, A.ItemCode, A.BatchSerialNo, A.FromObjType,
                               A.ToBinAbsEntry, A.FromBinAbsEntry,
                               A.ToObjType, A.FromWhs, A.ToWhs, case when A.Q = 0 then A.Qty else A.Q end Q, A.Q WhsQ
                        FROM 
                        (
	                        SELECT TOP 1 *, 
                                   ( SELECT ISNULL(ISNULL(AA.A,0) + ISNULL(BB.B,0) - ISNULL(CC.C,0) ,0)
                                     FROM 
                                     (
                                        SELECT ISNULL(sum(qty),0) A FROM QIT_ProQRStock_InvTrans A
                                        WHERE A.QRCodeID = @dQR AND ISNULL(A.BranchID, @bID) = @bID and
		                                      A.FromObjType = '202' and A.ToObjType = '59' and A.ToWhs = @frwhs " + _ToBinFilter + @"
                                     ) AA LEFT JOIN
                                     (
                                        SELECT ISNULL(sum(qty),0) B FROM QIT_ProQRStock_InvTrans A
                                        WHERE A.QRCodeID = @dQR AND ISNULL(A.BranchID, @bID) = @bID and
		                                      A.FromObjType <> '202' and A.ToWhs = @frwhs " + _ToBinFilter + @"
                                     ) BB ON 1 = 1 LEFT JOIN
                                     (
		                                SELECT ISNULL(sum(qty),0) C FROM QIT_ProQRStock_InvTrans A
                                        WHERE A.QRCodeID = @dQR AND ISNULL(A.BranchID, @bID) = @bID and
		                                      A.FromObjType <> '202' and A.FromWhs = @frwhs " + _FromBinFilter + @"
                                     ) CC ON 1 = 1
                                   ) Q		
                            FROM QIT_ProQRStock_InvTrans A
	                        WHERE A.QRCodeID = @dQR AND ISNULL(A.BranchID, @bID) = @bID
	                        ORDER BY TransId desc
	                    ) A
                    ) as A
                    INNER JOIN " + Global.SAP_DB + @".dbo.OITM B ON A.ItemCode collate SQL_Latin1_General_CP850_CI_AS= B.ItemCode
                    INNER JOIN " + Global.SAP_DB + @".dbo.OITW C ON C.ItemCode = B.ItemCode and 
                               C.WhsCode collate SQL_Latin1_General_CP850_CI_AS = @frwhs AND 
                               C.OnHand > 0 AND A.Q <= C.OnHand
                    INNER JOIN QIT_Production_QR_Detail D ON A.QRCodeID = D.QRCodeID
                    INNER JOIN QIT_Production_QR_Header E ON E.HeaderSrNo = D.HeaderSrNo
                    INNER JOIN QIT_QRStock_ProToReceipt G ON  G.RecNo = D.RecNo and 
                               G.ItemCode = D.ItemCode AND G.QRCodeID = A.QRCodeID
				    INNER JOIN QIT_Trans_ProToReceipt H ON H.TransSeq = G.TransSeq
                    INNER JOIN " + Global.SAP_DB + @".dbo.OWOR F on F.DocEntry = E.DocEntry and 
                               F.DocNum = E.DocNum and F.ObjType collate SQL_Latin1_General_CP1_CI_AS = E.ObjType
                    INNER JOIN QIT_Item_Master I on I.ItemCode collate SQL_Latin1_General_CP1_CI_AS = A.ItemCode
                    FOR BROWSE
                    ";
                    #endregion
                }
                if (payload.DetailQRCodeID.Replace(" ", "~").Contains("~OS~"))
                {
                    #region Query
                    _Query = @"  
                    SELECT A.BranchID, A.QRCodeID DetailQRCodeID, A.ItemCode, B.ItemName, A.BatchSerialNo, WhsQ,
                           @frwhs Whs, A.Q Qty, A.Q TransQty, '' CardCode, '' CardName , G.OpeningNo GateInNo, G.Project, I.UomCode, I.ItemMngBy,
                           ( SELECT sum(OnHand) FROM " + Global.SAP_DB + @".dbo.OITW 
                             WHERE ItemCode collate SQL_Latin1_General_CP1_CI_AS = A.ItemCode
                           ) ItemStock,
                           ( SELECT sum(OnHand) FROM " + Global.SAP_DB + @".dbo.OITW 
                             WHERE ItemCode collate SQL_Latin1_General_CP1_CI_AS = A.ItemCode and WhsCode =  @frwhs
                           ) ItemWhsStock
                    FROM 
                    (	
                        SELECT A.BranchID, A.TransId, A.QRTransSeq, A.TransSeq, A.QRCodeID, A.ItemCode, A.BatchSerialNo, A.FromObjType,
                               A.ToBinAbsEntry, A.FromBinAbsEntry,
                               A.ToObjType, A.FromWhs, A.ToWhs, case when A.Q = 0 then A.Qty else A.Q end Q, A.Q WhsQ
                        FROM 
                        (
                            SELECT TOP 1 *, 
                                   ( SELECT ISNULL(ISNULL(AA.A,0) + ISNULL(BB.B,0) - ISNULL(CC.C,0) ,0)
                                     FROM 
                                     (
                                        SELECT ISNULL(sum(qty),0) A FROM QIT_OSQRStock_InvTrans A
                                        WHERE A.QRCodeID = @dQR AND ISNULL(A.BranchID, @bID) = @bID and
                                              A.FromObjType = '310000001' and A.ToObjType = '310000001' and A.ToWhs = @frwhs 
                                     ) AA LEFT JOIN
                                     (
                                        SELECT ISNULL(sum(qty),0) B FROM QIT_OSQRStock_InvTrans A
                                        WHERE A.QRCodeID = @dQR AND ISNULL(A.BranchID, @bID) = @bID and
                                              A.FromObjType <> '310000001' and A.ToWhs = @frwhs 
                                     ) BB ON 1 = 1 LEFT JOIN
                                     (
                                        SELECT ISNULL(sum(qty),0) C FROM QIT_OSQRStock_InvTrans A
                                        WHERE A.QRCodeID = @dQR AND ISNULL(A.BranchID, @bID) = @bID and
                                              A.FromObjType <> '310000001' and A.FromWhs = @frwhs 
                                     ) CC ON 1 = 1
                                   ) Q		
                            FROM QIT_OSQRStock_InvTrans A
                            WHERE A.QRCodeID = @dQR AND ISNULL(A.BranchID, @bID) = @bID
                            ORDER BY TransId desc
                        ) A
                    ) as A
                    INNER JOIN " + Global.SAP_DB + @".dbo.OITM B ON A.ItemCode collate SQL_Latin1_General_CP850_CI_AS= B.ItemCode
                    INNER JOIN " + Global.SAP_DB + @".dbo.OITW C ON C.ItemCode = B.ItemCode and 
                               C.WhsCode collate SQL_Latin1_General_CP850_CI_AS = @frwhs AND 
                               C.OnHand > 0 AND A.Q <= C.OnHand
                    INNER JOIN QIT_OpeningStock_QR_Detail D ON A.QRCodeID = D.QRCodeID
                    INNER JOIN QIT_OpeningStock_QR_Header E ON E.HeaderSrNo = D.HeaderSrNo
                    INNER JOIN QIT_OpeningStock G ON  G.OpeningNo = D.OpeningNo and G.ItemCode = D.ItemCode    
                    INNER JOIN QIT_Item_Master I on I.ItemCode collate SQL_Latin1_General_CP1_CI_AS = A.ItemCode
                    FOR BROWSE
                    ";
                    #endregion
                }

                _logger.LogInformation(" InventoryTransferController : ItemDataInWhs() Query : {q} ", _Query.ToString());
                QITcon.Open();
                oAdptr = new SqlDataAdapter(_Query, QITcon);
                oAdptr.SelectCommand.Parameters.AddWithValue("@bID", payload.BranchID);
                oAdptr.SelectCommand.Parameters.AddWithValue("@dQR", payload.DetailQRCodeID.Replace(" ", "~"));
                oAdptr.SelectCommand.Parameters.AddWithValue("@frwhs", payload.FromWhs);
                if (payload.FromBinAbsEntry > 0)
                {
                    oAdptr.SelectCommand.Parameters.AddWithValue("@fromBin", payload.FromBinAbsEntry);
                }
                oAdptr.Fill(dtData);
                QITcon.Close();

                if (dtData.Rows.Count > 0)
                {
                    if (double.Parse(dtData.Rows[0]["WhsQ"].ToString()) > 0)
                    {
                        dynamic arData = JsonConvert.SerializeObject(dtData);
                        obj = JsonConvert.DeserializeObject<List<ValidItemData>>(arData.ToString().Replace("~", " "));
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
                _logger.LogError(" Error in InventoryTransferController : ItemDataInWhs() :: {Error}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
            }
        }


        [HttpGet("GetWarehouse")]
        public async Task<ActionResult<IEnumerable<Warehouse>>> GetWarehouse()
        {
            try
            {
                _logger.LogInformation(" Calling InventoryTransferController : GetWarehouse() ");
                List<Warehouse> obj = new List<Warehouse>();
                System.Data.DataTable dtData = new System.Data.DataTable();
                SqlConnection con = new SqlConnection(_connection);


                _Query = @" 
                SELECT T0.WhsCode, T0.WhsName, T0.BinActivat 
                FROM [dbo].[OWHS] T0 
                WHERE T0.[DropShip] = 'N' AND T0.[Inactive] = 'N' ORDER BY T0.[WhsCode]";

                _logger.LogInformation(" InventoryTransferController : GetWarehouse() Query : {q} ", _Query.ToString());
                con.Open();
                SqlDataAdapter oAdptr = new SqlDataAdapter(_Query, con);
                oAdptr.Fill(dtData);
                con.Close();

                if (dtData.Rows.Count > 0)
                {
                    dynamic arData = JsonConvert.SerializeObject(dtData);
                    obj = JsonConvert.DeserializeObject<List<Warehouse>>(arData.ToString());
                    return obj;
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "No data found" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(" Error in InventoryTransferController : GetWarehouse() :: {Error}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
            }
        }


        //[HttpPost("InventoryTransfer")]
        //public async Task<IActionResult> InventoryTransfer([FromBody] IT payload)
        //{
        //    if (objGlobal == null)
        //        objGlobal = new Global();
        //    string p_ErrorMsg = string.Empty;
        //    string _IsSaved = "N";
        //    Object _QRTransSeqInvTrans = 0;

        //    bool _IsContinue = true;
        //    try
        //    {
        //        _logger.LogInformation(" Calling InventoryTransferController : InventoryTransfer() ");
        //        string _FromObjType = string.Empty;
        //        string _ToObjType = string.Empty;

        //        if (payload != null)
        //        {
        //            //if (objGlobal.ConnectSAP(out p_ErrorMsg))
        //            var (success, errorMsg) = await objGlobal.ConnectSAP();
        //            if (success)
        //            {
        //                if (!_IsContinue)
        //                {
        //                    DeleteQRStockITDet(_QRTransSeqInvTrans.ToString());
        //                    return BadRequest(new
        //                    {
        //                        StatusCode = "400",
        //                        IsSaved = _IsSaved,
        //                        TransSeq = _QRTransSeqInvTrans,
        //                        StatusMsg = "Error code: " + objGlobal.oComp.GetLastErrorCode() + Environment.NewLine +
        //                                        "Error message: " + objGlobal.oComp.GetLastErrorDescription()
        //                    });

        //                }

        //                if (payload.FromObjType == null)
        //                    _FromObjType = "67";
        //                else
        //                {
        //                    if (payload.FromObjType == string.Empty || payload.FromObjType.ToLower() == "string")
        //                        _FromObjType = "67";
        //                    else
        //                        _FromObjType = payload.FromObjType;
        //                }

        //                if (payload.ToObjType == null)
        //                    _ToObjType = "67";
        //                else
        //                {
        //                    if (payload.ToObjType == string.Empty || payload.ToObjType.ToLower() == "string")
        //                        _ToObjType = "67";
        //                    else
        //                        _ToObjType = payload.ToObjType;
        //                }


        //                DateTime _docDate = DateTime.Today;
        //                int _TotalItemCount = payload.itDetails.Count;
        //                int _SuccessCount = 0;
        //                int _FailCount = 0;

        //                #region Get QR TransSeq No - Inventory Transfer
        //                SqlConnection con = new SqlConnection(_QIT_connection);
        //                _Query = @" SELECT ISNULL(max(QRTransSeq),0) + 1 FROM QIT_QRStock_InvTrans A  ";
        //                _logger.LogInformation(" InventoryTransferController : GetQRTransSeqNo(Inv Trans) Query : {q} ", _Query.ToString());

        //                cmd = new SqlCommand(_Query, con);
        //                con.Open();
        //                _QRTransSeqInvTrans = cmd.ExecuteScalar();
        //                con.Close();
        //                #endregion

        //                #region Insert in QR Stock Table
        //                foreach (var itemDet in payload.itDetails)
        //                {
        //                    int _DetTotalCount = itemDet.itQRDetails.Count;
        //                    int _DetSuccessCount = 0;
        //                    int _DetFailCount = 0;
        //                    foreach (var itemQR in itemDet.itQRDetails)
        //                    {
        //                        con = new SqlConnection(_QIT_connection);
        //                        _Query = @"INSERT INTO QIT_QRStock_InvTrans (BranchID, TransId, TransSeq, QRTransSeq, QRCodeID, GateInNo, ItemCode,BatchSerialNo, FromObjType, ToObjType, FromWhs, ToWhs, Qty, FromBinAbsEntry, ToBinAbsEntry) 
        //                 VALUES ( @bID,  (SELECT ISNULL(max(TransId),0) + 1 FROM QIT_QRStock_InvTrans), 
        //                           @transSeq, @qrtransSeq, @qrCodeID, @gateInNo, @itemCode, @bsNo, @frObjType, @toObjType, @fromWhs, @toWhs, @qty, @fromBin, @toBin   
        //                 )";
        //                        _logger.LogInformation("InventoryTransferController : QR Stock Table Query : {q} ", _Query.ToString());

        //                        cmd = new SqlCommand(_Query, con);
        //                        cmd.Parameters.AddWithValue("@bID", payload.BranchID);
        //                        cmd.Parameters.AddWithValue("@qrtransSeq", _QRTransSeqInvTrans);
        //                        cmd.Parameters.AddWithValue("@transSeq", 0);
        //                        cmd.Parameters.AddWithValue("@qrCodeID", itemQR.DetailQRCodeID.Replace(" ", "~"));
        //                        cmd.Parameters.AddWithValue("@gateInNo", itemQR.GateInNo);
        //                        cmd.Parameters.AddWithValue("@itemCode", itemDet.ItemCode);
        //                        cmd.Parameters.AddWithValue("@bsNo", itemQR.BatchSerialNo);
        //                        cmd.Parameters.AddWithValue("@frObjType", _FromObjType);
        //                        cmd.Parameters.AddWithValue("@toObjType", _ToObjType);
        //                        cmd.Parameters.AddWithValue("@fromWhs", itemQR.FromWhsCode);
        //                        cmd.Parameters.AddWithValue("@toWhs", payload.ToWhsCode);
        //                        cmd.Parameters.AddWithValue("@fromBin", itemQR.FromBinAbsEntry);
        //                        cmd.Parameters.AddWithValue("@toBin", payload.ToBinAbsEntry);
        //                        cmd.Parameters.AddWithValue("@qty", itemQR.Qty);

        //                        con.Open();
        //                        int intNum = cmd.ExecuteNonQuery();
        //                        con.Close();

        //                        if (intNum > 0)
        //                            _DetSuccessCount = _DetSuccessCount + 1;
        //                        else
        //                            _DetFailCount = _DetFailCount + 1;
        //                    }

        //                    if (_DetTotalCount == _DetSuccessCount)
        //                        _SuccessCount = _SuccessCount + 1;
        //                    else
        //                        _FailCount = _FailCount + 1;
        //                }

        //                #endregion

        //                if (_TotalItemCount == _SuccessCount)
        //                {
        //                    #region Get BinCode of ToBin
        //                    //System.Data.DataTable dtBinData = new System.Data.DataTable();
        //                    //SqlConnection _conSAP = new SqlConnection(_connection);
        //                    //_Query = @" SELECT T0.* FROM OBIN T0 WHERE T0.AbsEntry = @absEntry ";
        //                    //_conSAP.Open();
        //                    //SqlDataAdapter oAdptr = new SqlDataAdapter(_Query, _conSAP);
        //                    //oAdptr.SelectCommand.Parameters.AddWithValue("@docEntry", payload.ToBinAbsEntry);
        //                    //oAdptr.Fill(dtBinData);
        //                    //_conSAP.Close();
        //                    #endregion

        //                    int _Line = 0;

        //                    StockTransfer oStockTransfer = (StockTransfer)objGlobal.oComp.GetBusinessObject(BoObjectTypes.oStockTransfer);

        //                    oStockTransfer.DocObjectCode = BoObjectTypes.oStockTransfer;
        //                    oStockTransfer.Series = payload.Series;

        //                    if (payload.FromIT == "N")
        //                        oStockTransfer.CardCode = payload.CardCode;

        //                    oStockTransfer.Comments = payload.Comments;
        //                    oStockTransfer.DocDate = _docDate;
        //                    //oStockTransfer.DueDate = payload.DueDate;
        //                    // oStockTransfer.FromWarehouse = payload.FromWhsCode;
        //                    oStockTransfer.ToWarehouse = payload.ToWhsCode;

        //                    oStockTransfer.UserFields.Fields.Item("U_QIT_FromWeb").Value = "Y";

        //                    foreach (var item in payload.itDetails)
        //                    {
        //                        oStockTransfer.Lines.ItemCode = item.ItemCode;

        //                        oStockTransfer.Lines.WarehouseCode = payload.ToWhsCode;
        //                        oStockTransfer.Lines.Quantity = item.TotalItemQty;
        //                        oStockTransfer.Lines.ProjectCode = item.Project;
        //                        oStockTransfer.Lines.UserFields.Fields.Item("U_reason").Value = item.Reason;

        //                        if (item.ItemMngBy.ToLower() == "s")
        //                        {
        //                            int i = 0;
        //                            foreach (var serial in item.itQRDetails)
        //                            {
        //                                if (!string.IsNullOrEmpty(serial.BatchSerialNo))
        //                                {
        //                                    oStockTransfer.Lines.FromWarehouseCode = serial.FromWhsCode;
        //                                    oStockTransfer.Lines.SerialNumbers.SetCurrentLine(i);
        //                                    oStockTransfer.Lines.BatchNumbers.BaseLineNumber = _Line;
        //                                    oStockTransfer.Lines.SerialNumbers.InternalSerialNumber = serial.BatchSerialNo;
        //                                    oStockTransfer.Lines.SerialNumbers.ManufacturerSerialNumber = serial.BatchSerialNo;
        //                                    oStockTransfer.Lines.SerialNumbers.Quantity = Convert.ToDouble(serial.Qty);
        //                                    oStockTransfer.Lines.SerialNumbers.Add();

        //                                    if (serial.FromBinAbsEntry > 0) // Enter in this code block only when From Whs has Bin Allocation
        //                                    {
        //                                        oStockTransfer.Lines.BinAllocations.BinActionType = BinActionTypeEnum.batFromWarehouse;
        //                                        oStockTransfer.Lines.BinAllocations.BinAbsEntry = serial.FromBinAbsEntry;
        //                                        oStockTransfer.Lines.BinAllocations.Quantity = Convert.ToDouble(serial.Qty);
        //                                        oStockTransfer.Lines.BinAllocations.BaseLineNumber = _Line;
        //                                        oStockTransfer.Lines.BinAllocations.SerialAndBatchNumbersBaseLine = i;
        //                                        oStockTransfer.Lines.BinAllocations.Add();
        //                                    }
        //                                    if (payload.ToBinAbsEntry > 0) // Enter in this code block only when To Whs has Bin Allocation
        //                                    {
        //                                        oStockTransfer.Lines.BinAllocations.BinActionType = BinActionTypeEnum.batToWarehouse;
        //                                        oStockTransfer.Lines.BinAllocations.BinAbsEntry = payload.ToBinAbsEntry;
        //                                        oStockTransfer.Lines.BinAllocations.Quantity = Convert.ToDouble(serial.Qty);
        //                                        oStockTransfer.Lines.BinAllocations.BaseLineNumber = _Line;
        //                                        oStockTransfer.Lines.BinAllocations.SerialAndBatchNumbersBaseLine = i;
        //                                        oStockTransfer.Lines.BinAllocations.Add();
        //                                    }

        //                                    i = i + 1;
        //                                }
        //                            }
        //                        }
        //                        else if (item.ItemMngBy.ToLower() == "b")
        //                        {
        //                            int _batchLine = 0;
        //                            foreach (var batch in item.itQRDetails)
        //                            {
        //                                if (!string.IsNullOrEmpty(batch.BatchSerialNo))
        //                                {
        //                                    oStockTransfer.Lines.FromWarehouseCode = batch.FromWhsCode;
        //                                    oStockTransfer.Lines.BatchNumbers.BaseLineNumber = _Line;
        //                                    oStockTransfer.Lines.BatchNumbers.BatchNumber = batch.BatchSerialNo;
        //                                    oStockTransfer.Lines.BatchNumbers.Quantity = Convert.ToDouble(batch.Qty);
        //                                    oStockTransfer.Lines.BatchNumbers.Add();

        //                                    if (batch.FromBinAbsEntry > 0) // Enter in this code block only when From Whs has Bin Allocation
        //                                    {
        //                                        oStockTransfer.Lines.BinAllocations.BinActionType = BinActionTypeEnum.batFromWarehouse;
        //                                        oStockTransfer.Lines.BinAllocations.BinAbsEntry = batch.FromBinAbsEntry;
        //                                        oStockTransfer.Lines.BinAllocations.Quantity = Convert.ToDouble(batch.Qty);
        //                                        oStockTransfer.Lines.BinAllocations.BaseLineNumber = _Line;
        //                                        oStockTransfer.Lines.BinAllocations.SerialAndBatchNumbersBaseLine = _batchLine;
        //                                        oStockTransfer.Lines.BinAllocations.Add();
        //                                    }
        //                                    if (payload.ToBinAbsEntry > 0) // Enter in this code block only when To Whs has Bin Allocation
        //                                    {
        //                                        oStockTransfer.Lines.BinAllocations.BinActionType = BinActionTypeEnum.batToWarehouse;
        //                                        oStockTransfer.Lines.BinAllocations.BinAbsEntry = payload.ToBinAbsEntry;
        //                                        oStockTransfer.Lines.BinAllocations.Quantity = Convert.ToDouble(batch.Qty);
        //                                        oStockTransfer.Lines.BinAllocations.BaseLineNumber = _Line;
        //                                        oStockTransfer.Lines.BinAllocations.SerialAndBatchNumbersBaseLine = _batchLine;
        //                                        oStockTransfer.Lines.BinAllocations.Add();
        //                                    }

        //                                    _batchLine = _batchLine + 1;
        //                                }
        //                            }
        //                        }
        //                        oStockTransfer.Lines.Add();
        //                        _Line = _Line + 1;
        //                    }

        //                    int addResult = oStockTransfer.Add();

        //                    if (addResult != 0)
        //                    {
        //                        DeleteQRStockITDet(_QRTransSeqInvTrans.ToString());
        //                        return BadRequest(new
        //                        {
        //                            StatusCode = "400",
        //                            IsSaved = _IsSaved,
        //                            TransSeq = _QRTransSeqInvTrans,
        //                            StatusMsg = "Error code: " + addResult + Environment.NewLine +
        //                                               "Error message: " + objGlobal.oComp.GetLastErrorDescription()
        //                        });
        //                    }
        //                    else
        //                    {
        //                        objGlobal.oComp.Disconnect();
        //                        return Ok(new { StatusCode = "200", IsSaved = "Y", StatusMsg = "Saved Successfully" });
        //                    }
        //                }
        //                else
        //                {
        //                    DeleteQRStockITDet(_QRTransSeqInvTrans.ToString());
        //                    return BadRequest(new
        //                    {
        //                        StatusCode = "400",
        //                        IsSaved = "N",
        //                        TransSeq = _QRTransSeqInvTrans,
        //                        StatusMsg = "Problem while saving QR Stock data"
        //                    });
        //                }
        //            }
        //            else
        //            {

        //                DeleteQRStockITDet(_QRTransSeqInvTrans.ToString());
        //                return BadRequest(new
        //                {
        //                    StatusCode = "400",
        //                    IsSaved = _IsSaved,
        //                    TransSeq = _QRTransSeqInvTrans,
        //                    StatusMsg = "Error code: " + objGlobal.oComp.GetLastErrorCode() + Environment.NewLine +
        //                                    "Error message: " + objGlobal.oComp.GetLastErrorDescription()
        //                });

        //                _IsContinue = false;
        //            }
        //        }
        //        else
        //        {
        //            DeleteQRStockITDet(_QRTransSeqInvTrans.ToString());
        //            return BadRequest(new
        //            {
        //                StatusCode = "400",
        //                IsSaved = _IsSaved,
        //                TransSeq = 0,
        //                StatusMsg = "Details not found"
        //            });
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        DeleteQRStockITDet(_QRTransSeqInvTrans.ToString());
        //        _logger.LogError("Error in InventoryTransferController : InventoryTransfer() :: {Error}", ex.ToString());
        //        return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = ex.Message.ToString() });
        //    }
        //    finally
        //    {
        //        objGlobal.oComp.Disconnect();
        //    }
        //}

        private bool DeleteQRStockITDet(string _QRTransSeq)
        {
            string _IsSaved = "N";
            try
            {
                _logger.LogInformation(" Calling InventoryTransferController : DeleteQRStockITDet() ");

                string p_ErrorMsg = string.Empty;

                SqlConnection con = new SqlConnection(_QIT_connection);
                _Query = @" DELETE FROM QIT_QRStock_InvTrans WHERE QRTransSeq = @qrtransSeq";
                _logger.LogInformation(" InventoryTransferController : DeleteQRStockITDet Query : {q} ", _Query.ToString());

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
                _logger.LogError("Error in InventoryTransferController : DeleteQRStockITDet() :: {Error}", ex.ToString());
                return false;
            }
        }

        private bool DeleteProQRStockITDet(string _ProQRTransSeq)
        {
            string _IsSaved = "N";
            try
            {
                _logger.LogInformation(" Calling InventoryTransferController : DeleteProQRStockITDet() ");

                string p_ErrorMsg = string.Empty;

                SqlConnection con = new SqlConnection(_QIT_connection);
                _Query = @" DELETE FROM QIT_ProQRStock_InvTrans WHERE QRTransSeq = @qrtransSeq";
                _logger.LogInformation(" InventoryTransferController : DeleteProQRStockITDet Query : {q} ", _Query.ToString());

                cmd = new SqlCommand(_Query, con);
                cmd.Parameters.AddWithValue("@qrtransSeq", _ProQRTransSeq);
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
                _logger.LogError("Error in InventoryTransferController : DeleteProQRStockITDet() :: {Error}", ex.ToString());
                return false;
            }
        }


        [HttpPost("InventoryTransferNewFlow")]
        public async Task<IActionResult> InventoryTransferNewFlow([FromBody] ITNewFlow payload)
        {
            if (objGlobal == null)
                objGlobal = new Global();
            string p_ErrorMsg = string.Empty;
            string _IsSaved = "N";
            Object _QRTransSeqInvTrans = 0;
            Object _ProQRTransSeqInvTrans = 0;
            Object _OSQRTransSeqInvTrans = 0;

            SqlConnection QITcon;

            try
            {
                _logger.LogInformation(" Calling InventoryTransferController : InventoryTransfer() ");
                string _FromObjType = string.Empty;
                string _ToObjType = string.Empty;

                if (payload != null)
                {
                    if (((WMS_UI_API.Services.SAPConnectionService)_sapConnectionService).oComp.Connected)
                    {
                        if (1 == 1)
                        {
                            #region Set Object Type
                            if (payload.FromObjType == null)
                                _FromObjType = "67";
                            else
                            {
                                if (payload.FromObjType == string.Empty || payload.FromObjType.ToLower() == "string")
                                    _FromObjType = "67";
                                else
                                    _FromObjType = payload.FromObjType;
                            }

                            if (payload.ToObjType == null)
                                _ToObjType = "67";
                            else
                            {
                                if (payload.ToObjType == string.Empty || payload.ToObjType.ToLower() == "string")
                                    _ToObjType = "67";
                                else
                                    _ToObjType = payload.ToObjType;
                            }
                            #endregion

                            DateTime _docDate = DateTime.Today;
                            int _TotalItemCount = payload.itDetails.Count;
                            int _SuccessCount = 0;
                            int _FailCount = 0;

                            #region Get QR TransSeq No - Inventory Transfer
                            QITcon = new SqlConnection(_QIT_connection);
                            _Query = @" SELECT ISNULL(max(QRTransSeq),0) + 1 FROM QIT_QRStock_InvTrans A  ";
                            _logger.LogInformation(" InventoryTransferController : GetQRTransSeqNo(Inv Trans) Query : {q} ", _Query.ToString());

                            cmd = new SqlCommand(_Query, QITcon);
                            QITcon.Open();
                            _QRTransSeqInvTrans = cmd.ExecuteScalar();
                            QITcon.Close();
                            #endregion

                            #region Get QR TransSeq No - Inventory Transfer
                            QITcon = new SqlConnection(_QIT_connection);
                            _Query = @" SELECT ISNULL(max(QRTransSeq),0) + 1 FROM QIT_ProQRStock_InvTrans A  ";
                            _logger.LogInformation(" InventoryTransferController : GetQRTransSeqNo(Inv Trans) Query : {q} ", _Query.ToString());

                            cmd = new SqlCommand(_Query, QITcon);
                            QITcon.Open();
                            _ProQRTransSeqInvTrans = cmd.ExecuteScalar();
                            QITcon.Close();
                            #endregion

                            #region Get QR TransSeq No - Inventory Transfer
                            QITcon = new SqlConnection(_QIT_connection);
                            _Query = @" SELECT ISNULL(max(QRTransSeq),0) + 1 FROM QIT_OSQRStock_InvTrans A  ";
                            _logger.LogInformation(" InventoryTransferController : GetQRTransSeqNo(Inv Trans) Query : {q} ", _Query.ToString());

                            cmd = new SqlCommand(_Query, QITcon);
                            QITcon.Open();
                            _OSQRTransSeqInvTrans = cmd.ExecuteScalar();
                            QITcon.Close();
                            #endregion

                            #region Insert in QR Stock Table
                            foreach (var itemDet in payload.itDetails)
                            {
                                int _DetTotalCount = itemDet.itQRDetails.Count;
                                int _DetSuccessCount = 0;
                                int _DetFailCount = 0;
                                foreach (var itemQR in itemDet.itQRDetails)
                                {
                                    QITcon = new SqlConnection(_QIT_connection);

                                    if (itemQR.DetailQRCodeID.Replace(" ", "~").ToUpper().Contains("~PO~"))
                                    {
                                        _Query = @"
                                    INSERT INTO QIT_QRStock_InvTrans 
                                    (  BranchID, TransId, TransSeq, QRTransSeq, QRCodeID, GateInNo, ItemCode,BatchSerialNo, 
                                       FromObjType, ToObjType, 
                                       FromWhs, ToWhs, Qty, FromBinAbsEntry, ToBinAbsEntry
                                    ) 
                                    VALUES 
                                    (  @bID,  (SELECT ISNULL(max(TransId),0) + 1 FROM QIT_QRStock_InvTrans), 
                                       @transSeq, @qrtransSeq, @qrCodeID, @gateInNo, @itemCode, @bsNo, @frObjType, @toObjType, 
                                       @fromWhs, @toWhs, @qty, @fromBin, @toBin   
                                    )";
                                        _logger.LogInformation("InventoryTransferController : QR Stock Table Query : {q} ", _Query.ToString());

                                        cmd = new SqlCommand(_Query, QITcon);
                                        cmd.Parameters.AddWithValue("@bID", payload.BranchID);
                                        cmd.Parameters.AddWithValue("@qrtransSeq", _QRTransSeqInvTrans);
                                        cmd.Parameters.AddWithValue("@transSeq", 0);
                                        cmd.Parameters.AddWithValue("@qrCodeID", itemQR.DetailQRCodeID.Replace(" ", "~"));
                                        cmd.Parameters.AddWithValue("@gateInNo", itemQR.GateRecNo);
                                        cmd.Parameters.AddWithValue("@itemCode", itemDet.ItemCode);
                                        cmd.Parameters.AddWithValue("@bsNo", itemQR.BatchSerialNo);
                                        cmd.Parameters.AddWithValue("@frObjType", _FromObjType);
                                        cmd.Parameters.AddWithValue("@toObjType", _ToObjType);
                                        cmd.Parameters.AddWithValue("@fromWhs", itemQR.FromWhsCode);
                                        cmd.Parameters.AddWithValue("@toWhs", itemQR.ToWhsCode);
                                        cmd.Parameters.AddWithValue("@fromBin", itemQR.FromBinAbsEntry);
                                        cmd.Parameters.AddWithValue("@toBin", itemQR.ToBinAbsEntry);
                                        cmd.Parameters.AddWithValue("@qty", itemQR.Qty);
                                    }
                                    else if (itemQR.DetailQRCodeID.Replace(" ", "~").ToUpper().Contains("~PRO~"))
                                    {
                                        _Query = @"
                                    INSERT INTO QIT_ProQRStock_InvTrans 
                                    ( BranchID, TransId, TransSeq, QRTransSeq, QRCodeID, RecNo, ItemCode, BatchSerialNo, FromObjType, ToObjType, FromWhs, ToWhs, Qty, FromBinAbsEntry, ToBinAbsEntry
                                    ) 
                                    VALUES 
                                    ( @bID, (SELECT ISNULL(max(TransId),0) + 1 FROM QIT_ProQRStock_InvTrans), 
                                      @transSeq, @qrtransSeq, @qrCodeID, @recNo, @itemCode, @bsNo, @frObjType, @toObjType, 
                                      @fromWhs, @toWhs, @qty, @fromBin, @toBin   
                                    )";
                                        _logger.LogInformation("InventoryTransferController : QR Stock Table Query : {q} ", _Query.ToString());

                                        cmd = new SqlCommand(_Query, QITcon);
                                        cmd.Parameters.AddWithValue("@bID", payload.BranchID);
                                        cmd.Parameters.AddWithValue("@qrtransSeq", _ProQRTransSeqInvTrans);
                                        cmd.Parameters.AddWithValue("@transSeq", 0);
                                        cmd.Parameters.AddWithValue("@qrCodeID", itemQR.DetailQRCodeID.Replace(" ", "~"));
                                        cmd.Parameters.AddWithValue("@recNo", itemQR.GateRecNo);
                                        cmd.Parameters.AddWithValue("@itemCode", itemDet.ItemCode);
                                        cmd.Parameters.AddWithValue("@bsNo", itemQR.BatchSerialNo);
                                        cmd.Parameters.AddWithValue("@frObjType", _FromObjType);
                                        cmd.Parameters.AddWithValue("@toObjType", _ToObjType);
                                        cmd.Parameters.AddWithValue("@fromWhs", itemQR.FromWhsCode);
                                        cmd.Parameters.AddWithValue("@toWhs", itemQR.ToWhsCode);
                                        cmd.Parameters.AddWithValue("@fromBin", itemQR.FromBinAbsEntry);
                                        cmd.Parameters.AddWithValue("@toBin", itemQR.ToBinAbsEntry);
                                        cmd.Parameters.AddWithValue("@qty", itemQR.Qty);
                                    }
                                    else if (itemQR.DetailQRCodeID.Replace(" ", "~").ToUpper().Contains("~OS~"))
                                    {
                                        _Query = @"
                                    INSERT INTO QIT_OSQRStock_InvTrans 
                                    ( BranchID, TransId, TransSeq, QRTransSeq, QRCodeID, OpeningNo, ItemCode, BatchSerialNo,   
                                      FromObjType, ToObjType, FromWhs, ToWhs, Qty, FromBinAbsEntry, ToBinAbsEntry
                                    ) 
                                    VALUES 
                                    ( @bID, (SELECT ISNULL(max(TransId),0) + 1 FROM QIT_OSQRStock_InvTrans), 
                                      @transSeq, @qrtransSeq, @qrCodeID, @oNo, @itemCode, @bsNo, 
                                      @frObjType, @toObjType, @fromWhs, @toWhs, @qty, @fromBin, @toBin   
                                    )";
                                        _logger.LogInformation("InventoryTransferController : QR Stock Table Query : {q} ", _Query.ToString());

                                        cmd = new SqlCommand(_Query, QITcon);
                                        cmd.Parameters.AddWithValue("@bID", payload.BranchID);
                                        cmd.Parameters.AddWithValue("@qrtransSeq", _OSQRTransSeqInvTrans);
                                        cmd.Parameters.AddWithValue("@transSeq", 0);
                                        cmd.Parameters.AddWithValue("@qrCodeID", itemQR.DetailQRCodeID.Replace(" ", "~"));
                                        cmd.Parameters.AddWithValue("@oNo", itemQR.GateRecNo);
                                        cmd.Parameters.AddWithValue("@itemCode", itemDet.ItemCode);
                                        cmd.Parameters.AddWithValue("@bsNo", itemQR.BatchSerialNo);
                                        cmd.Parameters.AddWithValue("@frObjType", _FromObjType);
                                        cmd.Parameters.AddWithValue("@toObjType", _ToObjType);
                                        cmd.Parameters.AddWithValue("@fromWhs", itemQR.FromWhsCode);
                                        cmd.Parameters.AddWithValue("@toWhs", itemQR.ToWhsCode);
                                        cmd.Parameters.AddWithValue("@fromBin", itemQR.FromBinAbsEntry);
                                        cmd.Parameters.AddWithValue("@toBin", itemQR.ToBinAbsEntry);
                                        cmd.Parameters.AddWithValue("@qty", itemQR.Qty);
                                    }

                                    QITcon.Open();
                                    int intNum = cmd.ExecuteNonQuery();
                                    QITcon.Close();

                                    if (intNum > 0)
                                        _DetSuccessCount = _DetSuccessCount + 1;
                                    else
                                        _DetFailCount = _DetFailCount + 1;
                                }

                                if (_DetTotalCount == _DetSuccessCount)
                                    _SuccessCount = _SuccessCount + 1;
                                else
                                    _FailCount = _FailCount + 1;
                            }

                            #endregion

                            if (_TotalItemCount == _SuccessCount)
                            {

                                int _Line = 0;

                                StockTransfer oStockTransfer = (StockTransfer)((WMS_UI_API.Services.SAPConnectionService)_sapConnectionService).oComp.GetBusinessObject(BoObjectTypes.oStockTransfer);

                                oStockTransfer.DocObjectCode = BoObjectTypes.oStockTransfer;
                                oStockTransfer.Series = payload.Series;

                                if (payload.FromIT == "N")
                                    oStockTransfer.CardCode = payload.CardCode;

                                oStockTransfer.Comments = payload.Comments;
                                oStockTransfer.DocDate = _docDate;

                                //oStockTransfer.ToWarehouse = payload.ToWhsCode;

                                oStockTransfer.UserFields.Fields.Item("U_QIT_FromWeb").Value = "Y";

                                foreach (var item in payload.itDetails)
                                {
                                    oStockTransfer.Lines.ItemCode = item.ItemCode;
                                    oStockTransfer.Lines.Quantity = item.TotalItemQty;
                                    oStockTransfer.Lines.ProjectCode = item.Project;
                                    oStockTransfer.Lines.UserFields.Fields.Item("U_reason").Value = item.Reason;

                                    if (item.ItemMngBy.ToLower() == "s")
                                    {
                                        int i = 0;
                                        foreach (var serial in item.itQRDetails)
                                        {
                                            oStockTransfer.Lines.WarehouseCode = serial.ToWhsCode;
                                            if (!string.IsNullOrEmpty(serial.BatchSerialNo))
                                            {
                                                oStockTransfer.Lines.FromWarehouseCode = serial.FromWhsCode;
                                                oStockTransfer.Lines.SerialNumbers.SetCurrentLine(i);
                                                oStockTransfer.Lines.BatchNumbers.BaseLineNumber = _Line;
                                                oStockTransfer.Lines.SerialNumbers.InternalSerialNumber = serial.BatchSerialNo;
                                                oStockTransfer.Lines.SerialNumbers.ManufacturerSerialNumber = serial.BatchSerialNo;
                                                oStockTransfer.Lines.SerialNumbers.Quantity = Convert.ToDouble(serial.Qty);
                                                oStockTransfer.Lines.SerialNumbers.Add();

                                                if (serial.FromBinAbsEntry > 0) // Enter in this code block only when From Whs has Bin Allocation
                                                {
                                                    oStockTransfer.Lines.BinAllocations.BinActionType = BinActionTypeEnum.batFromWarehouse;
                                                    oStockTransfer.Lines.BinAllocations.BinAbsEntry = serial.FromBinAbsEntry;
                                                    oStockTransfer.Lines.BinAllocations.Quantity = Convert.ToDouble(serial.Qty);
                                                    oStockTransfer.Lines.BinAllocations.BaseLineNumber = _Line;
                                                    oStockTransfer.Lines.BinAllocations.SerialAndBatchNumbersBaseLine = i;
                                                    oStockTransfer.Lines.BinAllocations.Add();
                                                }
                                                if (serial.ToBinAbsEntry > 0) // Enter in this code block only when To Whs has Bin Allocation
                                                {
                                                    oStockTransfer.Lines.BinAllocations.BinActionType = BinActionTypeEnum.batToWarehouse;
                                                    oStockTransfer.Lines.BinAllocations.BinAbsEntry = serial.ToBinAbsEntry;
                                                    oStockTransfer.Lines.BinAllocations.Quantity = Convert.ToDouble(serial.Qty);
                                                    oStockTransfer.Lines.BinAllocations.BaseLineNumber = _Line;
                                                    oStockTransfer.Lines.BinAllocations.SerialAndBatchNumbersBaseLine = i;
                                                    oStockTransfer.Lines.BinAllocations.Add();
                                                }

                                                i = i + 1;
                                            }
                                        }
                                    }
                                    else if (item.ItemMngBy.ToLower() == "b")
                                    {
                                        int _batchLine = 0;
                                        foreach (var batch in item.itQRDetails)
                                        {
                                            oStockTransfer.Lines.WarehouseCode = batch.ToWhsCode;
                                            if (!string.IsNullOrEmpty(batch.BatchSerialNo))
                                            {
                                                oStockTransfer.Lines.FromWarehouseCode = batch.FromWhsCode;
                                                oStockTransfer.Lines.BatchNumbers.BaseLineNumber = _Line;
                                                oStockTransfer.Lines.BatchNumbers.BatchNumber = batch.BatchSerialNo;
                                                oStockTransfer.Lines.BatchNumbers.Quantity = Convert.ToDouble(batch.Qty);
                                                oStockTransfer.Lines.BatchNumbers.Add();

                                                if (batch.FromBinAbsEntry > 0) // Enter in this code block only when From Whs has Bin Allocation
                                                {
                                                    oStockTransfer.Lines.BinAllocations.BinActionType = BinActionTypeEnum.batFromWarehouse;
                                                    oStockTransfer.Lines.BinAllocations.BinAbsEntry = batch.FromBinAbsEntry;
                                                    oStockTransfer.Lines.BinAllocations.Quantity = Convert.ToDouble(batch.Qty);
                                                    oStockTransfer.Lines.BinAllocations.BaseLineNumber = _Line;
                                                    oStockTransfer.Lines.BinAllocations.SerialAndBatchNumbersBaseLine = _batchLine;
                                                    oStockTransfer.Lines.BinAllocations.Add();
                                                }
                                                if (batch.ToBinAbsEntry > 0) // Enter in this code block only when To Whs has Bin Allocation
                                                {
                                                    oStockTransfer.Lines.BinAllocations.BinActionType = BinActionTypeEnum.batToWarehouse;
                                                    oStockTransfer.Lines.BinAllocations.BinAbsEntry = batch.ToBinAbsEntry;
                                                    oStockTransfer.Lines.BinAllocations.Quantity = Convert.ToDouble(batch.Qty);
                                                    oStockTransfer.Lines.BinAllocations.BaseLineNumber = _Line;
                                                    oStockTransfer.Lines.BinAllocations.SerialAndBatchNumbersBaseLine = _batchLine;
                                                    oStockTransfer.Lines.BinAllocations.Add();
                                                }

                                                _batchLine = _batchLine + 1;
                                            }
                                        }
                                    }
                                    oStockTransfer.Lines.Add();
                                    _Line = _Line + 1;
                                }

                                int addResult = oStockTransfer.Add();

                                if (addResult != 0)
                                {
                                    DeleteQRStockITDet(_QRTransSeqInvTrans.ToString());
                                    DeleteProQRStockITDet(_ProQRTransSeqInvTrans.ToString());
                                    return BadRequest(new
                                    {
                                        StatusCode = "400",
                                        IsSaved = _IsSaved,
                                        TransSeq = _QRTransSeqInvTrans,
                                        StatusMsg = "Error code: " + addResult + Environment.NewLine +
                                                           "Error message: " + ((WMS_UI_API.Services.SAPConnectionService)_sapConnectionService).oComp.GetLastErrorDescription()
                                    });
                                }
                                else
                                {
                                    return Ok(new { StatusCode = "200", IsSaved = "Y", StatusMsg = "Saved Successfully" });
                                }
                            }
                            else
                            {
                                DeleteQRStockITDet(_QRTransSeqInvTrans.ToString());
                                DeleteProQRStockITDet(_ProQRTransSeqInvTrans.ToString());
                                return BadRequest(new
                                {
                                    StatusCode = "400",
                                    IsSaved = "N",
                                    TransSeq = _QRTransSeqInvTrans,
                                    StatusMsg = "Problem while saving QR Stock data"
                                });
                            }
                        }
                        else
                        {

                            DeleteQRStockITDet(_QRTransSeqInvTrans.ToString());
                            DeleteProQRStockITDet(_ProQRTransSeqInvTrans.ToString());
                            return BadRequest(new
                            {
                                StatusCode = "400",
                                IsSaved = _IsSaved,
                                TransSeq = _QRTransSeqInvTrans,
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
                            StatusMsg = "Error code: " + ((WMS_UI_API.Services.SAPConnectionService)_sapConnectionService).oComp.GetLastErrorCode() + Environment.NewLine +
                                           "Error message: " + ((WMS_UI_API.Services.SAPConnectionService)_sapConnectionService).oComp.GetLastErrorDescription()
                        });
                    }
                    
                }
                else
                {
                    DeleteQRStockITDet(_QRTransSeqInvTrans.ToString());
                    DeleteProQRStockITDet(_ProQRTransSeqInvTrans.ToString());
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
                DeleteQRStockITDet(_QRTransSeqInvTrans.ToString());
                DeleteProQRStockITDet(_ProQRTransSeqInvTrans.ToString());
                _logger.LogError("Error in InventoryTransferController : InventoryTransfer() :: {Error}", ex.ToString());
                return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = ex.Message.ToString() });
            }
            finally
            {
                
            }
        }

    }
}
