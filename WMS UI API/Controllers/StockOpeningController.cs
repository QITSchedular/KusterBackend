using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using System.Data;
using System.Data.SqlClient;
using WMS_UI_API.Models;
using WMS_UI_API.Common;
using Newtonsoft.Json;

namespace WMS_UI_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StockOpeningController : ControllerBase
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
        private readonly ILogger<StockOpeningController> _logger;


        public StockOpeningController(IConfiguration configuration, ILogger<StockOpeningController> logger)
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
                _logger.LogError(" Error in StockOpeningController :: {Error}" + ex.ToString());
            }
        }


        #region Opening Stock

        [HttpGet("Warehouse")]
        public async Task<ActionResult<IEnumerable<OS_Warehouse>>> GetWarehouse(int BranchID)
        {

            string _IsSaved = "N";
            try
            {
                _logger.LogInformation(" Calling StockOpeningController : GetWarehouse() ");

                System.Data.DataTable dtWarehouse = new System.Data.DataTable();
                QITcon = new SqlConnection(_QIT_connection);
               
                _Query = @"    
                SELECT A.WhsCode, B.WhsName, B.BinActivat BinActivate
                FROM
                (
                    SELECT DISTINCT WhsCode FROM " + Global.SAP_DB + @".dbo.OITW WHERE OnHand > 0
                ) as A INNER JOIN " + Global.SAP_DB + @".dbo.OWHS B ON A.WhsCode = B.WhsCode and ISNULL(B.BPLid, @bid) = @bid
                ORDER BY A.WhsCode
                FOR BROWSE
                ";
              
                _logger.LogInformation(" StockOpeningController : GetWarehouse() Query : {q} ", _Query.ToString());
                QITcon.Open();
                oAdptr = new SqlDataAdapter(_Query, QITcon);
                oAdptr.SelectCommand.Parameters.AddWithValue("@bid", BranchID);
                oAdptr.Fill(dtWarehouse);
                QITcon.Close();

                if (dtWarehouse.Rows.Count > 0)
                {
                    List<OS_Warehouse> obj = new List<OS_Warehouse>();
                    dynamic arData = JsonConvert.SerializeObject(dtWarehouse);
                    obj = JsonConvert.DeserializeObject<List<OS_Warehouse>>(arData.ToString());
                    return obj;
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "No data found" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(" Error in StockOpeningController : GetWarehouse() :: {Error}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
            }
        }


        [HttpGet("BinLocation")]
        public async Task<ActionResult<IEnumerable<OS_BinLocation>>> GetBinLocation(int BranchID, string WhsCode)
        {
            string _IsSaved = "N";
            try
            {
                _logger.LogInformation(" Calling StockOpeningController : GetBinLocation() ");

                System.Data.DataTable dtBin = new System.Data.DataTable();
                QITcon = new SqlConnection(_QIT_connection);

                //_Query = @" 
                //SELECT A.BinAbs AbsEntry, B.BinCode 
                //FROM 
                //(
                //     SELECT DISTINCT BinAbs FROM " + Global.SAP_DB + @".dbo.OIBQ WHERE WhsCode = @whsCode and OnHandQty > 0
                //) as A
                //INNER JOIN " + Global.SAP_DB + @".dbo.OBIN B ON A.BinAbs = B.AbsEntry and B.WhsCode = @whsCode
                //-- INNER JOIN " + Global.SAP_DB + @".dbo.OWHS C ON C.WhsCode = B.WhsCode and ISNULL(C.BPLid, @bid) = @bid
                //FOR BROWSE
                //";


                _Query = @" 
                SELECT DISTINCT T1.AbsEntry, T1.BinCode 
                FROM " + Global.SAP_DB + @".dbo.OITW T0
	                INNER JOIN " + Global.SAP_DB + @".dbo.OBIN T1 ON T0.WhsCode = T1.WhsCode
	                INNER JOIN " + Global.SAP_DB + @".dbo.OIBQ T2 ON T2.WhsCode = T0.WhsCode AND 
                               T1.AbsEntry = T2.BinAbs AND T0.ItemCode = T2.ItemCode
	                INNER JOIN " + Global.SAP_DB + @".dbo.OBBQ T3 ON T3.ItemCode = T0.ItemCode AND 
                               T3.BinAbs = T1.AbsEntry AND T3.WhsCode = T2.WhsCode
	                INNER JOIN " + Global.SAP_DB + @".dbo.OBTN T4 ON T4.AbsEntry = T3.SnBMDAbs
	                INNER JOIN " + Global.SAP_DB + @".dbo.OITM T5 ON T5.ItemCode = T0.ItemCode
                WHERE T2.OnHandQty > 0 AND T3.OnHandQty > 0 AND T2.OnHandQty > 0 and T0.WhsCode= @whsCode 
                      --- and T0.ItemCode = '130010490041' and T4.U_Project = 'Common' -- and T1.BinCode='VD-SUB-UFLEX LIMITED-B-06'
                GROUP BY T1.AbsEntry, T1.BinCode 
                FOR BROWSE
                ";

                _logger.LogInformation(" StockOpeningController : GetBinLocation() Query : {q} ", _Query.ToString());
                QITcon.Open();
                oAdptr = new SqlDataAdapter(_Query, QITcon);
                oAdptr.SelectCommand.Parameters.AddWithValue("@whsCode", WhsCode);
                //oAdptr.SelectCommand.Parameters.AddWithValue("@bid", BranchID);
                oAdptr.Fill(dtBin);
                QITcon.Close();

                if (dtBin.Rows.Count > 0)
                {
                    List<OS_BinLocation> obj = new List<OS_BinLocation>();
                    dynamic arData = JsonConvert.SerializeObject(dtBin);
                    obj = JsonConvert.DeserializeObject<List<OS_BinLocation>>(arData.ToString());
                    return obj;
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "No data found" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(" Error in StockOpeningController : GetBinLocation() :: {Error}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
            }
        }


        [HttpGet("Project")]
        public async Task<ActionResult<IEnumerable<OS_Project>>> GetProject(string WhsCode, int? BinAbsEntry)
        {
            string _IsSaved = "N";
            try
            {
                _logger.LogInformation(" Calling StockOpeningController : GetProject() ");

                System.Data.DataTable dtBin = new System.Data.DataTable();
                QITcon = new SqlConnection(_QIT_connection);

                //_Query = @" 
                //SELECT B.PrjCode, B.PrjName FROM
                //(
                // SELECT DISTINCT MIN(T0.[U_Project]) AS 'Project'
                // FROM  " + Global.SAP_DB + @".dbo.[OBTN] T0  
                //    INNER JOIN " + Global.SAP_DB + @".dbo.[OITM] T1 ON T1.[ItemCode] = T0.[ItemCode]   
                //    INNER JOIN " + Global.SAP_DB + @".dbo.[ITL1] T2 ON T2.[ItemCode] = T0.[ItemCode] AND T2.[SysNumber] = T0.[SysNumber]   
                //    INNER JOIN " + Global.SAP_DB + @".dbo.[OITL] T3 ON T3.[LogEntry] = T2.[LogEntry] AND T3.[ManagedBy] = 10000044    
                //    LEFT OUTER JOIN " + Global.SAP_DB + @".dbo.[OBTQ] T4 ON T4.[ItemCode] = T0.[ItemCode] AND 
                //               T4.[SysNumber] = T0.[SysNumber] AND T4.[WhsCode] = T3.[LocCode]    
                //    LEFT OUTER JOIN " + Global.SAP_DB + @".dbo.[OBTW] T5 ON T5.[ItemCode] = T0.[ItemCode] AND 
                //               T5.[SysNumber] = T0.[SysNumber] AND T5.[WhsCode] = T3.[LocCode]    
                //    LEFT OUTER JOIN " + Global.SAP_DB + @".dbo.[OCRD] T6 ON T6.[CardCode] = T3.[CardCode]    
                //    LEFT OUTER JOIN " + Global.SAP_DB + @".dbo.[TCN1] T7 ON T7.[AbsEntry] = T0.[TrackingNt] AND 
                //               T7.[LineNum] = T0.[TrackiNtLn]    
                //    LEFT OUTER JOIN " + Global.SAP_DB + @".dbo.[OTCN] T8 ON T8.[AbsEntry] = T7.[AbsEntry]  
                //    LEFT OUTER JOIN " + Global.SAP_DB + @".dbo.[OWHS] T9 ON T9.WhsCode = T4.WhsCode
                //    LEFT OUTER JOIN " + Global.SAP_DB + @".dbo.[OBIN] T10 ON T10.WhsCode = T9.WhsCode and T10.WhsCode = T4.WhsCode 
                // WHERE T1.[InvntItem] = 'Y' AND T1.[ManBtchNum] = 'Y' AND T4.WhsCode = @whsCode -- AND T10.AbsEntry =  37158 
                // GROUP BY T0.[AbsEntry], T4.[WhsCode] 
                //) AS A INNER JOIN " + Global.SAP_DB + @".dbo.OPRJ B ON B.PrjCode = A.Project
                //";

                if (BinAbsEntry <= 0)
                {
                    _Query = @"
                    SELECT A.PrjCode ProjectCode, B.PrjName ProjectName 
                    FROM 
                    (
                        SELECT DISTINCT PrjCode
                        FROM " + Global.SAP_DB + @".dbo.OINM
                        WHERE Warehouse = @whsCode 
                        GROUP BY PrjCode
                        having ( sum(InQty) - sum(OutQty) ) > 0
                    ) as A INNER JOIN " + Global.SAP_DB + @".dbo.OPRJ B ON A.PrjCode = B.PrjCode
                    FOR BROWSE
                    ";
                }
                else
                {
                    _Query = @"
                    SELECT B.PrjCode ProjectCode, B.PrjName ProjectName 
                    FROM 
                    (
                        SELECT DISTINCT T4.U_Project
                        FROM " + Global.SAP_DB + @".dbo.OITW T0
	                        INNER JOIN " + Global.SAP_DB + @".dbo.OBIN T1 ON T0.WhsCode = T1.WhsCode
	                        INNER JOIN " + Global.SAP_DB + @".dbo.OIBQ T2 ON T2.WhsCode = T0.WhsCode AND 
                                       T1.AbsEntry = T2.BinAbs AND T0.ItemCode = T2.ItemCode
	                        INNER JOIN " + Global.SAP_DB + @".dbo.OBBQ T3 ON T3.ItemCode = T0.ItemCode AND 
                                       T3.BinAbs = T1.AbsEntry AND T3.WhsCode = T2.WhsCode
	                        INNER JOIN " + Global.SAP_DB + @".dbo.OBTN T4 ON T4.AbsEntry = T3.SnBMDAbs
	                        INNER JOIN " + Global.SAP_DB + @".dbo.OITM T5 ON T5.ItemCode = T0.ItemCode
                        WHERE T2.OnHandQty > 0 AND T3.OnHandQty > 0 AND T2.OnHandQty > 0 AND
	                          T0.WhsCode = @whsCode and T1.AbsEntry = @bin and T4.U_Project is not null
                        GROUP BY T4.U_Project
                    ) AS A INNER JOIN " + Global.SAP_DB + @".dbo.OPRJ B on A.U_Project = B.PrjCode
                    FOR BROWSE
                    ";
                }

                _logger.LogInformation(" StockOpeningController : GetProject() Query : {q} ", _Query.ToString());
                QITcon.Open();
                oAdptr = new SqlDataAdapter(_Query, QITcon);
                oAdptr.SelectCommand.Parameters.AddWithValue("@whsCode", WhsCode);
                oAdptr.SelectCommand.Parameters.AddWithValue("@bin", BinAbsEntry);
                oAdptr.Fill(dtBin);
                QITcon.Close();

                if (dtBin.Rows.Count > 0)
                {
                    List<OS_Project> obj = new List<OS_Project>();
                    dynamic arData = JsonConvert.SerializeObject(dtBin);
                    obj = JsonConvert.DeserializeObject<List<OS_Project>>(arData.ToString());
                    return obj;
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "No data found" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(" Error in StockOpeningController : GetProject() :: {Error}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
            }
        }


        [HttpGet("ItemData")]
        public async Task<ActionResult<IEnumerable<OS_ItemData>>> GetItemData(int BranchID, string WhsCode, int AbsEntry, string ProjectCode)
        {
            string _IsSaved = "N";
            try
            {
                _logger.LogInformation(" Calling StockOpeningController : GetItemData() ");

                #region Validation
                if (BranchID <= 0)
                {
                    return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Provide Branch" });
                }

                if (WhsCode.Trim().Length <= 0)
                {
                    return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Provide Warehouse" });
                }

                #endregion

                System.Data.DataTable dtItemData = new System.Data.DataTable();
                QITcon = new SqlConnection(_QIT_connection);

                if (AbsEntry > 0)
                {
                    #region Old Query
                    //_Query = @"                
                    //WITH _BinWiseData AS 
                    //(
                    //    SELECT A.ItemCode , PrjCode, SUM(A.InQty) - SUM(A.OutQty) AS NetQty
                    //    FROM " + Global.SAP_DB + @".dbo.B1_OinmWithBinTransfer A
                    //         INNER JOIN " + Global.SAP_DB + @".dbo.OBTL B ON A.InvPLMessageID = B.MessageID
                    //         INNER JOIN " + Global.SAP_DB + @".dbo.OBIN C ON C.AbsEntry = B.BinAbs
                    //    WHERE /* A.ItemCode = '130040600064' AND */ A.Warehouse = @whsCode AND A.PrjCode = @proj AND C.AbsEntry = @bin
                    //    GROUP BY A.ItemCode, A.PrjCode
                    //    HAVING (SUM(A.InQty) - SUM(A.OutQty)) > 0
                    //)
                    //, _BatchWiseData AS 
                    //(
                    //    SELECT DISTINCT B.BinCode, D.DistNumber, D.LotNumber, A.ItemCode, B.WhsCode, A.OnHandQty
                    //    FROM " + Global.SAP_DB + @".dbo.OBBQ A
                    //         LEFT JOIN " + Global.SAP_DB + @".dbo.OBIN B ON B.WhsCode = A.WhsCode AND B.AbsEntry = A.BinAbs
                    //         LEFT JOIN " + Global.SAP_DB + @".dbo.OIBT C ON A.ItemCode = C.ItemCode AND A.WhsCode = C.WhsCode
                    //         LEFT JOIN " + Global.SAP_DB + @".dbo.OBTN D ON A.SnBMDAbs = D.AbsEntry
                    //    WHERE /*A.ItemCode = '130040600064' AND */ B.WhsCode = @whsCode AND D.LotNumber = @proj AND 
                    //          B.AbsEntry = @bin AND A.OnHandQty > 0 
                    //)
 
                    //SELECT WhsCode, BinCode Bin, ItemCode, ItemName, SAPStock, QITStock, 
                    //       SAPStock - QITStock EligibleStock, null OpeningStock, DistNumber 
                    //FROM
                    //(
                    //    SELECT BB.WhsCode WhsCode, BB.BinCode Bin, BB.ItemCode, CC.ItemName, BB.DistNumber,   
                    //           AA.PrjCode, AA.NetQty, BB.BinCode, BB.OnHandQty SAPStock,
	                   // (
                    //        SELECT ISNULL(SUM(ISNULL(stock,0)),0)  
                    //        FROM 
                    //        (
                    //            SELECT CASE WHEN A.POtoGRPO > 0 THEN 
                    //                        (POtoGRPO + InQty - OutQty - IssueQty - DeliverQty + ReceiptQty + OpeningQty)
                    //                    ELSE 
                    //                        (InQty - OutQty - IssueQty - DeliverQty + ReceiptQty + OpeningQty)
                    //                    END Stock
                    //            FROM 
                    //            (
                    //                SELECT A.WhsCode, A.WhsName,
                    //                (
                    //                    ISNULL((	
                    //                       SELECT sum(Z.Qty) POtoGRPOQty FROM [QIT_QRStock_InvTrans] Z 
                    //                       INNER JOIN QIT_GateIN Z1 on Z.GateInNo = Z1.GateInNo and Z.ItemCode = Z1.ItemCode
                    //                       WHERE Z.ItemCode collate SQL_Latin1_General_CP850_CI_AS = BB.ItemCode and 
                    //                             Z.FromObjType = '22' and Z.ToObjType = '67' and  
                    //                             Z.ToWhs = A.WhsCode and ISNULL(Z.BranchID, @bId) = @bId and 
                    //                             Z1.Project collate SQL_Latin1_General_CP850_CI_AS = AA.PrjCode AND
								            //     Z.BatchSerialNo collate SQL_Latin1_General_CP850_CI_AS= BB.DistNumber
                    //                     ),0)
                    //                ) POtoGRPO,
                    //                (
                    //                    ISNULL((
                    //                    SELECT sum(Z.Qty) InQty FROM [QIT_QRStock_InvTrans] Z 
                    //                    INNER JOIN QIT_GateIN Z1 on Z.GateInNo = Z1.GateInNo and Z.ItemCode = Z1.ItemCode
                    //                    WHERE Z.ItemCode collate SQL_Latin1_General_CP850_CI_AS  = BB.ItemCode and 
                    //                            (Z.FromObjType <> '22') AND 
                    //                            Z.ToWhs collate SQL_Latin1_General_CP850_CI_AS = A.WhsCode and 
                    //                            ISNULL(Z.BranchID, @bId) = @bId and 
                    //                            Z1.Project collate SQL_Latin1_General_CP850_CI_AS = AA.PrjCode AND
								            //    Z.BatchSerialNo collate SQL_Latin1_General_CP850_CI_AS = BB.DistNumber
                    //                    ),0) 
                    //                ) InQty,
                    //                (
                    //                    ISNULL((
                    //                    SELECT sum(Z.Qty) OutQty FROM [QIT_QRStock_InvTrans] Z 
                    //                    INNER JOIN QIT_GateIN Z1 on Z.GateInNo = Z1.GateInNo and Z.ItemCode = Z1.ItemCode
                    //                    WHERE Z.ItemCode collate SQL_Latin1_General_CP850_CI_AS  = BB.ItemCode and 
                    //                            (Z.FromObjType <> '22') and
                    //                            Z.FromWhs collate SQL_Latin1_General_CP850_CI_AS = A.WhsCode and 
                    //                            ISNULL(Z.BranchID, @bId) = @bId and 
                    //                            Z1.Project collate SQL_Latin1_General_CP850_CI_AS = AA.PrjCode AND
								            //    Z.BatchSerialNo collate SQL_Latin1_General_CP850_CI_AS = BB.DistNumber

                    //                    ),0)
                    //                ) OutQty,
                    //                (
                    //                  ISNULL((
                    //                    SELECT sum(Z.Qty) IssueQty FROM QIT_QRStock_ProToIssue Z 
				                //                    INNER JOIN QIT_QR_Detail Z1 on Z.QRCodeID = Z1.QRCodeID
				                //                    INNER JOIN QIT_GateIN Z2 on Z1.GateInNo = Z2.GateInNo and Z.ItemCode = Z2.ItemCode
                    //                    WHERE Z.ItemCode collate SQL_Latin1_General_CP850_CI_AS  = BB.ItemCode and  
                    //                          Z.FromWhs collate SQL_Latin1_General_CP850_CI_AS = A.WhsCode and 
                    //                          ISNULL(Z.BranchID, @bId) = @bId and 
                    //                          Z2.Project collate SQL_Latin1_General_CP850_CI_AS = AA.PrjCode and
								            //  Z.BatchSerialNo collate SQL_Latin1_General_CP850_CI_AS = BB.DistNumber
                    //                  ),0)
                    //                ) IssueQty,
                    //                (
                    //                  ISNULL((
                    //                    SELECT sum(Z.Qty) DeliverQty FROM QIT_QRStock_SOToDelivery Z
				                //                    inner join QIT_QR_Detail Z1 on Z.QRCodeID = Z1.QRCodeID
				                //                    inner join QIT_GateIN Z2 on Z1.GateInNo = Z2.GateInNo and Z.ItemCode = Z2.ItemCode
                    //                    WHERE Z.ItemCode collate SQL_Latin1_General_CP850_CI_AS  = BB.ItemCode and  
                    //                       Z.FromWhs collate SQL_Latin1_General_CP850_CI_AS = A.WhsCode and 
                    //                       ISNULL(Z.BranchID, @bId) = @bId and 
                    //                       Z2.Project collate SQL_Latin1_General_CP850_CI_AS = AA.PrjCode and
							             //  Z.BatchSerialNo collate SQL_Latin1_General_CP850_CI_AS= BB.DistNumber
                    //                  ),0)
                    //                ) DeliverQty,
                    //                (
                    //                  ISNULL((
                    //                    SELECT sum(Z.Qty) ReceiptQty FROM QIT_QRStock_ProToReceipt Z
                    //                    INNER JOIN QIT_QR_Detail Z1 on Z.QRCodeID = Z1.QRCodeID
                    //                    INNER JOIN QIT_GateIN Z2 on Z1.GateInNo = Z2.GateInNo and Z.ItemCode = Z2.ItemCode
                    //                    WHERE Z.ItemCode collate SQL_Latin1_General_CP850_CI_AS  = BB.ItemCode and 
                    //                          Z.ToWhs collate SQL_Latin1_General_CP850_CI_AS = A.WhsCode and 
                    //                          ISNULL(Z.BranchID, @bId) = @bId and 
                    //                         Z2.Project collate SQL_Latin1_General_CP850_CI_AS = AA.PrjCode and
						 		           //  Z.BatchSerialNo collate SQL_Latin1_General_CP850_CI_AS= BB.DistNumber
                    //                  ),0)
                    //                ) ReceiptQty,
                    //                (
                    //                    ISNULL((
                    //                    SELECT sum(Z.OpeningQty) OpeningQty FROM QIT_OpeningStock Z 
                    //                    WHERE Z.ItemCode collate SQL_Latin1_General_CP850_CI_AS  = BB.ItemCode and 
                    //                            Z.WhsCode collate SQL_Latin1_General_CP850_CI_AS = A.WhsCode and 
                    //                            ISNULL(Z.BranchID, @bId) = @bId and 
                    //                            Z.Project collate SQL_Latin1_General_CP850_CI_AS = AA.PrjCode and
								            //    Z.BatchSerialNo collate SQL_Latin1_General_CP850_CI_AS = BB.DistNumber
                    //                    ),0)
                    //                ) OpeningQty
                    //                FROM QIT_Warehouse_Master AS A
                    //                WHERE Locked = 'N' and A.WhsCode collate SQL_Latin1_General_CP850_CI_AS = BB.WhsCode
				                //) as A  
                    //        ) as A 
                    //        WHERE A.Stock > 0   
                    //    ) QITStock
                    //    FROM _BinWiseData AA
                    //         INNER JOIN _BatchWiseData BB ON AA.ItemCode = BB.ItemCode AND AA.PrjCode = BB.LotNumber 
                    //         INNER JOIN " + Global.SAP_DB + @".dbo.OITM CC ON  BB.ItemCode = CC.ItemCode
                    //) AS A
                    //WHERE A.QITStock <= A.SAPStock
                    //FOR BROWSE
                    //";
                    #endregion

                    _Query = @" 
                    SELECT @whsCode WhsCode, (select Z.BinCode from " + Global.SAP_DB + @".dbo.OBIN Z where Z.AbsEntry = @bin) Bin, 
                           ItemCode, ItemName, SAPStock, QITStock, 
                           SAPStock - QITStock EligibleStock, null OpeningStock, DistNumber
                    FROM 
                    ( 
                        SELECT EE.DistNumber, AA.ItemCode, FF.ItemName, SUM(CC.OnHandQty) SAPStock,
                        (
                            SELECT ISNULL(SUM(ISNULL(stock,0)),0)  FROM 
                            (
                                SELECT CASE WHEN A.POtoGRPO > 0 THEN 
                                            (POtoGRPO + InQty - OutQty - IssueQty - DeliverQty + ReceiptQty + OpeningQty)
                                       ELSE 
                                            (InQty - OutQty - IssueQty - DeliverQty + ReceiptQty + OpeningQty)
                                       END Stock
                                FROM 
                                (
                                    SELECT A.WhsCode, A.WhsName,
                                           (
                                             ISNULL((	
                                               SELECT sum(Z.Qty) POtoGRPOQty FROM [QIT_QRStock_InvTrans] Z 
                                               INNER JOIN QIT_GateIN Z1 on Z.GateInNo = Z1.GateInNo and Z.ItemCode = Z1.ItemCode
                                               WHERE Z.ItemCode collate SQL_Latin1_General_CP850_CI_AS = AA.ItemCode and 
                                                     Z.FromObjType = '22' and Z.ToObjType = '67' and  
                                                     Z.ToWhs = A.WhsCode and ISNULL(Z.BranchID, @bId) = @bId and 
                                                     Z1.Project collate SQL_Latin1_General_CP850_CI_AS = EE.U_Project
                                             ),0)
                                           ) POtoGRPO,
                                           (
                                             ISNULL((
                                               SELECT sum(Z.Qty) InQty FROM [QIT_QRStock_InvTrans] Z 
                                               INNER JOIN QIT_GateIN Z1 on Z.GateInNo = Z1.GateInNo and Z.ItemCode = Z1.ItemCode
                                               WHERE Z.ItemCode collate SQL_Latin1_General_CP850_CI_AS  = AA.ItemCode and 
                                                     (Z.FromObjType <> '22') AND 
                                                     Z.ToWhs collate SQL_Latin1_General_CP850_CI_AS = A.WhsCode and 
                                                     ISNULL(Z.BranchID, @bId) = @bId and 
                                                     Z1.Project collate SQL_Latin1_General_CP850_CI_AS = EE.U_Project
                                             ),0) 
                                           ) InQty,
                                           (
                                             ISNULL((
                                               SELECT sum(Z.Qty) OutQty FROM [QIT_QRStock_InvTrans] Z 
                                               INNER JOIN QIT_GateIN Z1 on Z.GateInNo = Z1.GateInNo and Z.ItemCode = Z1.ItemCode
                                               WHERE Z.ItemCode collate SQL_Latin1_General_CP850_CI_AS  = AA.ItemCode and 
                                                     (Z.FromObjType <> '22') and
                                                     Z.FromWhs collate SQL_Latin1_General_CP850_CI_AS = A.WhsCode and 
                                                     ISNULL(Z.BranchID, @bId) = @bId and 
                                                     Z1.Project collate SQL_Latin1_General_CP850_CI_AS = EE.U_Project

                                             ),0)
                                           ) OutQty,
                                           (
                                             ISNULL((
                                               SELECT sum(Z.Qty) IssueQty FROM QIT_QRStock_ProToIssue Z 
				                                           INNER JOIN QIT_QR_Detail Z1 on Z.QRCodeID = Z1.QRCodeID
				                                           INNER JOIN QIT_GateIN Z2 on Z1.GateInNo = Z2.GateInNo and Z.ItemCode = Z2.ItemCode
                                               WHERE Z.ItemCode collate SQL_Latin1_General_CP850_CI_AS  = AA.ItemCode and  
                                                     Z.FromWhs collate SQL_Latin1_General_CP850_CI_AS = A.WhsCode and 
                                                     ISNULL(Z.BranchID, @bId) = @bId and 
                                                     Z2.Project collate SQL_Latin1_General_CP850_CI_AS = EE.U_Project
                                             ),0)
                                           ) IssueQty,
                                           (
                                             ISNULL((
                                               SELECT sum(Z.Qty) DeliverQty FROM QIT_QRStock_SOToDelivery Z
				                                           inner join QIT_QR_Detail Z1 on Z.QRCodeID = Z1.QRCodeID
				                                           inner join QIT_GateIN Z2 on Z1.GateInNo = Z2.GateInNo and Z.ItemCode = Z2.ItemCode
                                               WHERE Z.ItemCode collate SQL_Latin1_General_CP850_CI_AS  = AA.ItemCode and  
                                                  Z.FromWhs collate SQL_Latin1_General_CP850_CI_AS = A.WhsCode and 
                                                  ISNULL(Z.BranchID, @bId) = @bId and 
                                                  Z2.Project collate SQL_Latin1_General_CP850_CI_AS = EE.U_Project
                                             ),0)
                                           ) DeliverQty,
                                           (
                                             ISNULL((
                                               SELECT sum(Z.Qty) ReceiptQty FROM QIT_QRStock_ProToReceipt Z
                                               INNER JOIN QIT_QR_Detail Z1 on Z.QRCodeID = Z1.QRCodeID
                                               INNER JOIN QIT_GateIN Z2 on Z1.GateInNo = Z2.GateInNo and Z.ItemCode = Z2.ItemCode
                                               WHERE Z.ItemCode collate SQL_Latin1_General_CP850_CI_AS  = AA.ItemCode and 
                                                     Z.ToWhs collate SQL_Latin1_General_CP850_CI_AS = A.WhsCode and 
                                                     ISNULL(Z.BranchID, @bId) = @bId and 
                                                     Z2.Project collate SQL_Latin1_General_CP850_CI_AS = EE.U_Project
                                             ),0)
                                           ) ReceiptQty,
                                           (
                                             ISNULL((
                                               SELECT sum(Z.OpeningQty) OpeningQty FROM QIT_OpeningStock Z 
                                               WHERE Z.ItemCode collate SQL_Latin1_General_CP850_CI_AS  = AA.ItemCode and 
                                                     Z.WhsCode collate SQL_Latin1_General_CP850_CI_AS = A.WhsCode and 
                                                     ISNULL(Z.BranchID, @bId) = @bId and 
                                                     Z.Project collate SQL_Latin1_General_CP850_CI_AS = EE.U_Project
                                             ),0)
                                           ) OpeningQty
                                    FROM QIT_Warehouse_Master AS A
                                    WHERE Locked = 'N' and A.WhsCode collate SQL_Latin1_General_CP850_CI_AS = AA.WhsCode
				
                                ) as A  
                            ) as A 
                            WHERE A.Stock > 0   
                        ) QITStock
                        FROM " + Global.SAP_DB + @".dbo.OITW AA
	                    INNER JOIN " + Global.SAP_DB + @".dbo.OBIN BB ON AA.WhsCode = BB.WhsCode
	                    INNER JOIN " + Global.SAP_DB + @".dbo.OIBQ CC ON CC.WhsCode = AA.WhsCode AND 
                                   BB.AbsEntry = CC.BinAbs AND AA.ItemCode = CC.ItemCode
	                    INNER JOIN " + Global.SAP_DB + @".dbo.OBBQ DD ON DD.ItemCode = AA.ItemCode AND 
                                   DD.BinAbs = BB.AbsEntry AND DD.WhsCode = CC.WhsCode
	                    INNER JOIN " + Global.SAP_DB + @".dbo.OBTN EE ON EE.AbsEntry = DD.SnBMDAbs
	                    INNER JOIN " + Global.SAP_DB + @".dbo.OITM FF ON FF.ItemCode = AA.ItemCode
                        WHERE CC.OnHandQty > 0 AND DD.OnHandQty > 0 AND CC.OnHandQty > 0 and 
                              AA.WhsCode = @whsCode and BB.AbsEntry = @bin and EE.U_Project = @proj
                        GROUP BY AA.WhsCode, EE.DistNumber, AA.ItemCode , FF.ItemName, EE.U_Project
                    ) as A  
                    WHERE A.QITStock <= A.SAPStock
                    FOR BROWSE
                    ";
                }
                else
                {
                    _Query = @"                
                    SELECT @whsCode WhsCode, '-' Bin, ItemCode, ItemName, SAPStock, QITStock, 
                           SAPStock - QITStock EligibleStock, null OpeningStock, DistNumber
                    FROM 
                    ( 
                        SELECT EE.DistNumber, AA.ItemCode , BB.ItemName, sum(InQty) - sum(OutQty) SAPStock,
                        (
                            SELECT ISNULL(SUM(ISNULL(stock,0)),0)  FROM 
                            (
                                SELECT CASE WHEN A.POtoGRPO > 0 THEN 
                                            (POtoGRPO + InQty - OutQty - IssueQty - DeliverQty + ReceiptQty + OpeningQty)
                                       ELSE 
                                            (InQty - OutQty - IssueQty - DeliverQty + ReceiptQty + OpeningQty)
                                       END Stock
                                FROM 
                                (
                                    SELECT A.WhsCode, A.WhsName,
                                           (
                                             ISNULL((	
                                               SELECT sum(Z.Qty) POtoGRPOQty FROM [QIT_QRStock_InvTrans] Z 
                                               INNER JOIN QIT_GateIN Z1 on Z.GateInNo = Z1.GateInNo and Z.ItemCode = Z1.ItemCode
                                               WHERE Z.ItemCode collate SQL_Latin1_General_CP850_CI_AS = AA.ItemCode and 
                                                     Z.FromObjType = '22' and Z.ToObjType = '67' and  
                                                     Z.ToWhs = A.WhsCode and ISNULL(Z.BranchID, @bId) = @bId and 
                                                     Z1.Project collate SQL_Latin1_General_CP850_CI_AS = CC.PrjCode
                                             ),0)
                                           ) POtoGRPO,
                                           (
                                             ISNULL((
                                               SELECT sum(Z.Qty) InQty FROM [QIT_QRStock_InvTrans] Z 
                                               INNER JOIN QIT_GateIN Z1 on Z.GateInNo = Z1.GateInNo and Z.ItemCode = Z1.ItemCode
                                               WHERE Z.ItemCode collate SQL_Latin1_General_CP850_CI_AS  = AA.ItemCode and 
                                                     (Z.FromObjType <> '22') AND 
                                                     Z.ToWhs collate SQL_Latin1_General_CP850_CI_AS = A.WhsCode and 
                                                     ISNULL(Z.BranchID, @bId) = @bId and 
                                                     Z1.Project collate SQL_Latin1_General_CP850_CI_AS = CC.PrjCode
                                             ),0) 
                                           ) InQty,
                                           (
                                             ISNULL((
                                               SELECT sum(Z.Qty) OutQty FROM [QIT_QRStock_InvTrans] Z 
                                               INNER JOIN QIT_GateIN Z1 on Z.GateInNo = Z1.GateInNo and Z.ItemCode = Z1.ItemCode
                                               WHERE Z.ItemCode collate SQL_Latin1_General_CP850_CI_AS  = AA.ItemCode and 
                                                     (Z.FromObjType <> '22') and
                                                     Z.FromWhs collate SQL_Latin1_General_CP850_CI_AS = A.WhsCode and 
                                                     ISNULL(Z.BranchID, @bId) = @bId and 
                                                     Z1.Project collate SQL_Latin1_General_CP850_CI_AS = CC.PrjCode

                                             ),0)
                                           ) OutQty,
                                           (
                                             ISNULL((
                                               SELECT sum(Z.Qty) IssueQty FROM QIT_QRStock_ProToIssue Z 
						                       INNER JOIN QIT_QR_Detail Z1 on Z.QRCodeID = Z1.QRCodeID
						                       INNER JOIN QIT_GateIN Z2 on Z1.GateInNo = Z2.GateInNo and Z.ItemCode = Z2.ItemCode
                                               WHERE Z.ItemCode collate SQL_Latin1_General_CP850_CI_AS  = AA.ItemCode and  
                                                     Z.FromWhs collate SQL_Latin1_General_CP850_CI_AS = A.WhsCode and 
                                                     ISNULL(Z.BranchID, @bId) = @bId and 
                                                     Z2.Project collate SQL_Latin1_General_CP850_CI_AS = CC.PrjCode
                                             ),0)
                                           ) IssueQty,
                                           (
                                             ISNULL((
                                               SELECT sum(Z.Qty) DeliverQty FROM QIT_QRStock_SOToDelivery Z
						                       inner join QIT_QR_Detail Z1 on Z.QRCodeID = Z1.QRCodeID
						                       inner join QIT_GateIN Z2 on Z1.GateInNo = Z2.GateInNo and Z.ItemCode = Z2.ItemCode
                                               WHERE Z.ItemCode collate SQL_Latin1_General_CP850_CI_AS  = AA.ItemCode and  
                                                  Z.FromWhs collate SQL_Latin1_General_CP850_CI_AS = A.WhsCode and 
                                                  ISNULL(Z.BranchID, @bId) = @bId and 
                                                  Z2.Project collate SQL_Latin1_General_CP850_CI_AS = CC.PrjCode
                                             ),0)
                                           ) DeliverQty,
                                           (
                                             ISNULL((
                                               SELECT sum(Z.Qty) ReceiptQty FROM QIT_QRStock_ProToReceipt Z
					                           INNER JOIN QIT_QR_Detail Z1 on Z.QRCodeID = Z1.QRCodeID
					                           INNER JOIN QIT_GateIN Z2 on Z1.GateInNo = Z2.GateInNo and Z.ItemCode = Z2.ItemCode
                                               WHERE Z.ItemCode collate SQL_Latin1_General_CP850_CI_AS  = AA.ItemCode and 
                                                     Z.ToWhs collate SQL_Latin1_General_CP850_CI_AS = A.WhsCode and 
                                                     ISNULL(Z.BranchID, @bId) = @bId and 
                                                     Z2.Project collate SQL_Latin1_General_CP850_CI_AS = CC.PrjCode
                                             ),0)
                                           ) ReceiptQty,
                                           (
                                             ISNULL((
                                               SELECT sum(Z.OpeningQty) OpeningQty FROM QIT_OpeningStock Z 
                                               WHERE Z.ItemCode collate SQL_Latin1_General_CP850_CI_AS  = AA.ItemCode and 
                                                     Z.WhsCode collate SQL_Latin1_General_CP850_CI_AS = A.WhsCode and 
                                                     ISNULL(Z.BranchID, @bId) = @bId and 
                                                     Z.Project collate SQL_Latin1_General_CP850_CI_AS = CC.PrjCode
                                             ),0)
                                           ) OpeningQty
                                    FROM QIT_Warehouse_Master AS A
                                    WHERE Locked = 'N' and A.WhsCode collate SQL_Latin1_General_CP850_CI_AS = AA.WhsCode
				
                                ) as A  
                            ) as A 
                            WHERE A.Stock > 0   
                        ) QITStock
                        FROM " + Global.SAP_DB + @".dbo.OITW AA 
	                    INNER JOIN " + Global.SAP_DB + @".dbo.OITM BB ON  AA.ItemCode = BB.ItemCode
	                    INNER JOIN " + Global.SAP_DB + @".dbo.OINM CC ON CC.ItemCode = BB.ItemCode and CC.Warehouse = AA.WhsCode 
                        INNER JOIN " + Global.SAP_DB + @".dbo.OBTQ DD ON DD.ItemCode = CC.ItemCode and 
                                   DD.WhsCode = CC.Warehouse and DD.Quantity > 0
	                    INNER JOIN " + Global.SAP_DB + @".dbo.OBTN EE ON EE.ItemCode = DD.ItemCode and EE.SysNumber = DD.SysNumber and 
                                   EE.AbsEntry = DD.MdAbsEntry
                        WHERE AA.WhsCode = @whsCode and AA.OnHand > 0 and CC.PrjCode = @proj
	                    GROUP BY AA.WhsCode, CC.PrjCode, AA.ItemCode, BB.ItemName, EE.DistNumber
	                    HAVING ( sum(InQty) - sum(OutQty) ) > 0
                    ) as A  
                    WHERE A.QITStock <= A.SAPStock
                    FOR BROWSE
                    ";
                }

                _logger.LogInformation(" StockOpeningController : GetItemData() Query : {q} ", _Query.ToString());
                QITcon.Open();
                oAdptr = new SqlDataAdapter(_Query, QITcon);
                oAdptr.SelectCommand.Parameters.AddWithValue("@bId", BranchID);
                oAdptr.SelectCommand.Parameters.AddWithValue("@whsCode", WhsCode);
                oAdptr.SelectCommand.Parameters.AddWithValue("@bin", AbsEntry);
                oAdptr.SelectCommand.Parameters.AddWithValue("@proj", ProjectCode);
                oAdptr.Fill(dtItemData);
                QITcon.Close();

                if (dtItemData.Rows.Count > 0)
                {
                    List<OS_ItemData> obj = new List<OS_ItemData>();
                    dynamic arData = JsonConvert.SerializeObject(dtItemData);
                    obj = JsonConvert.DeserializeObject<List<OS_ItemData>>(arData.ToString());
                    return obj;
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "No data found" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(" Error in StockOpeningController : GetItemData() :: {Error}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
            }
        }


        [HttpPost("SaveOpeningStock")]
        public IActionResult SaveOpeningStock(OS_Save payload)
        {
            string _IsSaved = "N";
            object _OpeningNo = 0;

            try
            {
                _logger.LogInformation(" Calling StockOpeningController : SaveOpeningStock() ");

                int _totalOpening = 0;
                int _SaveOpening = 0;
                int _DocLineNum = 1;

                if (payload != null)
                {
                    _totalOpening = payload.OpeningDetails.Count();

                    #region Get Opening Id
                    QITcon = new SqlConnection(_QIT_connection);
                    _Query = @" SELECT ISNULL(max(OpeningNo),0) + 1 FROM QIT_OpeningStock A  ";
                    _logger.LogInformation(" StockOpeningController : OpeningNo Query : {q} ", _Query.ToString());

                    cmd = new SqlCommand(_Query, QITcon);
                    QITcon.Open();
                    _OpeningNo = cmd.ExecuteScalar();
                    QITcon.Close();
                    #endregion

                    foreach (var itemDet in payload.OpeningDetails)
                    {
                        QITcon = new SqlConnection(_QIT_connection);
                        _Query = @" INSERT INTO QIT_OpeningStock
                                    (
                                        SrNo, BranchID, OpeningNo, Series, ItemCode, DocLineNum, OpeningQty, 
                                        WhsCode, BinAbsEntry, Project, BatchSerialNo
                                    )
                                    VALUES( (select ISNULL(max(SrNo),0) + 1 from QIT_OpeningStock), @bID, @oId, 
                                            @series, @iCode, @docLine, @Qty, @whs, @bin, @proj, @batchSerial)
                                 ";
                        _logger.LogInformation(" StockOpeningController : SaveOpeningStock() Query : {q} ", _Query.ToString());

                        cmd = new SqlCommand(_Query, QITcon);
                        cmd.Parameters.AddWithValue("@bID", payload.BranchId);
                        cmd.Parameters.AddWithValue("@oId", _OpeningNo);
                        cmd.Parameters.AddWithValue("@series", payload.Series);
                        cmd.Parameters.AddWithValue("@iCode", itemDet.ItemCode);
                        cmd.Parameters.AddWithValue("@docLine", _DocLineNum);
                        cmd.Parameters.AddWithValue("@Qty", itemDet.OpeningQty);
                        cmd.Parameters.AddWithValue("@whs", payload.WhsCode);
                        cmd.Parameters.AddWithValue("@bin", payload.BinAbsEntry);
                        cmd.Parameters.AddWithValue("@proj", payload.Project);
                        cmd.Parameters.AddWithValue("@batchSerial", itemDet.DistNumber);

                        QITcon.Open();
                        int intNum = cmd.ExecuteNonQuery();
                        QITcon.Close();

                        if (intNum > 0)
                        {
                            _IsSaved = "Y";
                            _SaveOpening = _SaveOpening + 1;
                        }
                        else
                        {
                            _IsSaved = "N";
                            bool bln = DeleteOpening((int)_OpeningNo);
                            return BadRequest(new
                            {
                                StatusCode = "400",
                                IsSaved = "N",
                                StatusMsg = "Problem in saving Opening Stock for :" + Environment.NewLine +
                                            "ItemCode : " + itemDet.ItemCode
                            });
                        }
                        _DocLineNum = _DocLineNum + 1;
                    }

                    if (_totalOpening == _SaveOpening && _SaveOpening > 0)
                    {
                        return Ok(new { 
                            StatusCode = "200", 
                            IsSaved = _IsSaved, 
                            OpeningNo = _OpeningNo,
                            StatusMsg = "Saved Successfully!!!" 
                        });
                    }
                    else
                    {
                        return BadRequest(new { StatusCode = "400", IsSaved = "N", StatusMsg = "Problem in saving Opening Stock" });
                    }
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = " Details not found" });
                }
            }
            catch (Exception ex)
            {
                bool bln = DeleteOpening((int)_OpeningNo);
                _logger.LogError(" Error in StockOpeningController : SaveOpeningStock() :: {Error}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
            }
        }


        private bool DeleteOpening(int p_OpeningNo)
        {
            try
            {
                _logger.LogInformation(" Calling StockOpeningController : DeleteOpening() ");
                string p_ErrorMsg = string.Empty;

                QITcon = new SqlConnection(_QIT_connection);
                _Query = @" DELETE FROM QIT_OpeningStock WHERE OpeningNo = @oId ";
                _logger.LogInformation(" StockOpeningController : DeleteOpening Query : {q} ", _Query.ToString());

                cmd = new SqlCommand(_Query, QITcon);
                cmd.Parameters.AddWithValue("@oId", p_OpeningNo);
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
                _logger.LogError("Error in StockOpeningController : DeleteOpening() :: {Error}", ex.ToString());
                return false;
            }
        }

        #endregion


        #region Generate and Print QR

        [HttpGet("GetOpeningNo")]
        public async Task<ActionResult<IEnumerable<OpeningNoList>>> GetOpeningNo(int BranchID)
        {
            string _IsSaved = "N";
            try
            {
                _logger.LogInformation(" Calling StockOpeningController : GetOpeningNo() ");

                if (BranchID <= 0)
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Provide BranchID" });
                }

                System.Data.DataTable dtData = new System.Data.DataTable();
                QITcon = new SqlConnection(_QIT_connection);
                _Query = @" 
                SELECT DISTINCT A.OpeningNo OpeningNo
                FROM QIT_OpeningStock A
                WHERE ISNULL(BranchID, @bid) = @bid AND A.Canceled = 'N' FOR BROWSE
                ";

                _logger.LogInformation(" StockOpeningController : GetOpeningNo() Query : {q} ", _Query.ToString());
                QITcon.Open();
                oAdptr = new SqlDataAdapter(_Query, QITcon);
                oAdptr.SelectCommand.Parameters.AddWithValue("@bid", BranchID);
                oAdptr.Fill(dtData);
                QITcon.Close();

                if (dtData.Rows.Count > 0)
                {
                    List<OpeningNoList> obj = new List<OpeningNoList>();
                    dynamic arData = JsonConvert.SerializeObject(dtData);
                    obj = JsonConvert.DeserializeObject<List<OpeningNoList>>(arData.ToString());
                    return obj;
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "No data found" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(" Error in StockOpeningController : GetOpeningNo() :: {Error}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
            }
        }


        [HttpGet("GetOpeningStockData")]
        public async Task<ActionResult<IEnumerable<OS_OpeningStockData>>> GetOpeningStockData(int BranchID, int OpeningNo)
        {
            string _IsSaved = "N";
            try
            {
                _logger.LogInformation(" Calling StockOpeningController : GetOpeningStockData() ");

                if (BranchID <= 0)
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Provide BranchID" });
                }

                if (OpeningNo <= 0)
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Provide Opening No" });
                }


                System.Data.DataTable dtData = new System.Data.DataTable();
                QITcon = new SqlConnection(_QIT_connection);
                _Query = @" 
                SELECT A.OpeningNo OpeningNo, A.ObjType, A.Series, B.SeriesName, A.ItemCode, 
                       (select ItemName FROM " + Global.SAP_DB + @".dbo.OITM WHERE ItemCode collate SQL_Latin1_General_CP1_CI_AS = A.ItemCode) ItemName, A.DocLineNum,
                       A.OpeningQty Qty, A.OpeningDate, A.WhsCode, A.Project, A.BinAbsEntry, 
                       (select BinCode FROM " + Global.SAP_DB + @".dbo.OBIN WHERE AbsEntry = A.BinAbsEntry) BinCode,
                       A.Canceled, A.BatchSerialNo, C.ItemMngBy, C.QRMngBy
                FROM QIT_OpeningStock A 
                     INNER JOIN " + Global.SAP_DB + @".dbo.NNM1 B ON A.Series = B.Series
                     INNER JOIN QIT_ITEM_Master C ON C.ItemCode = A.ItemCode
                WHERE ISNULL(A.BranchID, @bid) = @bid AND OpeningNo = @OpeningNo and A.Canceled = 'N'
                FOR BROWSE
                ";

                _logger.LogInformation(" StockOpeningController : GetOpeningStockData() Query : {q} ", _Query.ToString());
                QITcon.Open();
                oAdptr = new SqlDataAdapter(_Query, QITcon);
                oAdptr.SelectCommand.Parameters.AddWithValue("@bid", BranchID);
                oAdptr.SelectCommand.Parameters.AddWithValue("@OpeningNo", OpeningNo);
                oAdptr.Fill(dtData);
                QITcon.Close();

                if (dtData.Rows.Count > 0)
                {
                    List<OS_OpeningStockData> obj = new List<OS_OpeningStockData>();
                    dynamic arData = JsonConvert.SerializeObject(dtData);
                    obj = JsonConvert.DeserializeObject<List<OS_OpeningStockData>>(arData.ToString());
                    return obj;
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "No data found" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(" Error in StockOpeningController : GetOpeningStockData() :: {Error}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
            }
        }


        [HttpPost("IsHeaderQRExist")]
        public ActionResult<bool> IsHeaderQRExist(CheckHeaderOpening payload)
        {
            try
            {
                System.Data.DataTable dtData = new System.Data.DataTable();
                _logger.LogInformation(" Calling StockOpeningController : IsHeaderQRExist() ");

                QITcon = new SqlConnection(_QIT_connection);

                _Query = @" SELECT * FROM QIT_OpeningStock_QR_Header 
                            WHERE BranchID = @bId AND 
                                  OpeningNo = @oNo AND 
                                  Series = @series AND
                                  ObjType = @objType FOR BROWSE
                         ";
                _logger.LogInformation(" StockOpeningController : IsHeaderQRExist() Query : {q} ", _Query.ToString());
                oAdptr = new SqlDataAdapter(_Query, QITcon);

                oAdptr.SelectCommand.Parameters.AddWithValue("@bId", payload.BranchID);
                oAdptr.SelectCommand.Parameters.AddWithValue("@oNo", payload.OpeningNo);
                oAdptr.SelectCommand.Parameters.AddWithValue("@series", payload.Series);
                oAdptr.SelectCommand.Parameters.AddWithValue("@objType", payload.ObjType);
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
                _logger.LogError(" Error in StockOpeningController : IsHeaderQRExist() :: {Error}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
            }
        }


        [HttpPost("GetHeaderQR")]
        public ActionResult<string> GetHeaderQR(CheckHeaderOpening payload)
        {
            try
            {
                _logger.LogInformation(" Calling StockOpeningController : GetHeaderQR() ");

                QITcon = new SqlConnection(_QIT_connection);

                string _strDay = DateTime.Now.Day.ToString("D2");
                string _strMonth = DateTime.Now.Month.ToString("D2");
                string _strYear = DateTime.Now.Year.ToString();

                string _qr = ConvertInQRString(_strYear) + ConvertInQRString(_strMonth) + ConvertInQRString(_strDay) + "~" +
                             ConvertInQRString(_strMonth) + "~";

                _Query = @" SELECT RIGHT('00000' + CONVERT(VARCHAR,ISNULL(MAX(INC_NO),0) + 1), 6) 
                            FROM QIT_OpeningStock_QR_Header 
                            WHERE YEAR(EntryDate) = @year AND 
							      FORMAT(MONTH(EntryDate), '00') = @month -- AND FORMAT(Day(EntryDate), '00') = @day 
                          ";
                _logger.LogInformation(" StockOpeningController : GetHeaderQR() Query : {q} ", _Query.ToString());

                cmd = new SqlCommand(_Query, QITcon);
                cmd.Parameters.AddWithValue("@year", _strYear);
                cmd.Parameters.AddWithValue("@month", _strMonth);
                //cmd.Parameters.AddWithValue("@day", _strDay);
                QITcon.Open();
                Object Value = cmd.ExecuteScalar();
                QITcon.Close();

                _qr = _qr + Value.ToString() + "~" + "OS";
                return Ok(new { StatusCode = "200", QRCode = _qr.Replace("~", " "), IncNo = Value.ToString() });
                //return _qr;
            }
            catch (Exception ex)
            {
                _logger.LogError(" Error in StockOpeningController : GetHeaderQR() :: {Error}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
            }
        }


        [HttpPost("SaveHeaderQR")]
        public IActionResult SaveHeaderQR(SaveHeaderOpeningQR payload)
        {
            string _IsSaved = "N";
            try
            {
                _logger.LogInformation(" Calling StockOpeningController : SaveHeaderQR() ");

                if (payload != null)
                {
                    if (payload.QRCodeID.Replace(" ", "~").Split('~')[2].ToString() != payload.IncNo)
                    {
                        return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Inc No must be " + payload.QRCodeID.Replace(" ", "~").Split('~')[2].ToString() });
                    }

                    QITcon = new SqlConnection(_QIT_connection);
                    _Query = @" INSERT INTO QIT_OpeningStock_QR_Header
                                (HeaderSrNo, BranchID, OpeningNo, Series, QRCodeID, ObjType, Inc_No, EntryDate)
                                VALUES
                                ( (select ISNULL(max(HeaderSrNo),0) + 1 FROM QIT_OpeningStock_QR_Header), 
                                  @bId, @oNo, @series, @qr, @objType, @incNo, @entryDate)
                              ";
                    _logger.LogInformation(" StockOpeningController : SaveHeaderQR() Query : {q} ", _Query.ToString());

                    cmd = new SqlCommand(_Query, QITcon);
                    cmd.Parameters.AddWithValue("@bID", payload.BranchID);
                    cmd.Parameters.AddWithValue("@oNo", payload.OpeningNo);
                    cmd.Parameters.AddWithValue("@series", payload.Series);
                    cmd.Parameters.AddWithValue("@qr", payload.QRCodeID.Replace(" ", "~"));
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
                _logger.LogError(" Error in StockOpeningController : SaveHeaderQR() :: {Error}", ex.ToString());
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


        [HttpPost("IsDetailQRExist")]
        public ActionResult<bool> IsDetailQRExist(CheckDetailOpening payload)
        {
            try
            {
                System.Data.DataTable dtData = new System.Data.DataTable();
                _logger.LogInformation(" Calling StockOpeningController : IsDetailQRExist() ");

                QITcon = new SqlConnection(_QIT_connection);

                _Query = @" SELECT A.* FROM QIT_OpeningStock_QR_Detail A 
                            INNER JOIN QIT_OpeningStock_QR_Header B on A.HeaderSrNo = B.HeaderSrNo
                            WHERE B.BranchID = @bId AND  
                                  B.Series = @series AND
                                  B.ObjType = @objType AND A.ItemCode = @iCode AND A.OpeningNo = @oNo AND A.DocLineNum = @docLine
                         ";
                _logger.LogInformation(" StockOpeningController : IsDetailQRExist() Query : {q} ", _Query.ToString());
                oAdptr = new SqlDataAdapter(_Query, QITcon);

                oAdptr.SelectCommand.Parameters.AddWithValue("@bId", payload.BranchID);
                oAdptr.SelectCommand.Parameters.AddWithValue("@series", payload.Series);
                oAdptr.SelectCommand.Parameters.AddWithValue("@objType", payload.ObjType);
                oAdptr.SelectCommand.Parameters.AddWithValue("@iCode", payload.ItemCode);
                oAdptr.SelectCommand.Parameters.AddWithValue("@docLine", payload.DocLineNum);
                oAdptr.SelectCommand.Parameters.AddWithValue("@oNo", payload.OpeningNo);
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
                _logger.LogError(" Error in StockOpeningController : IsDetailQRExist() :: {Error}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
            }
        }


        [HttpGet("GetDetailQR")]
        public ActionResult<string> GetDetailQR(string HeaderQR)
        {
            try
            {
                _logger.LogInformation(" Calling StockOpeningController : GetDetailQR() ");
                QITcon = new SqlConnection(_QIT_connection);
                DataTable dtData = new DataTable();

                _Query = @" SELECT * FROM QIT_OpeningStock_QR_Header WHERE QRCodeID = @headerQR ";
                _logger.LogInformation(" StockOpeningController : Check for Header QR Query : {q} ", _Query.ToString());

                oAdptr = new SqlDataAdapter(_Query, QITcon);
                oAdptr.SelectCommand.Parameters.AddWithValue("@headerQR", HeaderQR.Replace(" ", "~"));
                QITcon.Open();
                oAdptr.Fill(dtData);
                QITcon.Close();

                if (dtData.Rows.Count > 0)
                {
                    _Query = @" SELECT RIGHT('00000' + CONVERT(VARCHAR,ISNULL(MAX(A.Inc_No),0) + 1), 6) 
                                FROM QIT_OpeningStock_QR_Detail A INNER JOIN QIT_OpeningStock_QR_Header B on A.HeaderSrNo = B.HeaderSrNo
                                WHERE B.QRCodeID = @headerQR FOR BROWSE
                              ";
                    _logger.LogInformation(" StockOpeningController : GetDetailQR() Query : {q} ", _Query.ToString());

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
                _logger.LogError(" Error in StockOpeningController : GetDetailQR() :: {Error}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
            }
        }


        [HttpPost("SaveDetailQR")]
        public IActionResult SaveDetailQR(SaveDetailOpeningQR payload)
        {
            string _IsSaved = "N";
            Object _QRTransSeqInvTrans = 0;
            try
            {
                _logger.LogInformation(" Calling StockOpeningController : SaveDetailQR() ");

                if (payload != null)
                {
                    #region Opening No

                    QITcon = new SqlConnection(_QIT_connection);
                    DataTable dtOpeningNo = new DataTable();

                    _Query = @" SELECT * FROM QIT_OpeningStock WHERE OpeningNo = @oNo ";
                    _logger.LogInformation(" StockOpeningController : Opening No () Query : {q} ", _Query.ToString());

                    oAdptr = new SqlDataAdapter(_Query, QITcon);
                    oAdptr.SelectCommand.Parameters.AddWithValue("@oNo", payload.OpeningNo);
                    QITcon.Open();
                    oAdptr.Fill(dtOpeningNo);
                    QITcon.Close();

                    #endregion

                    if (dtOpeningNo.Rows.Count <= 0)
                        return BadRequest(new { StatusCode = "400", IsExist = "N", StatusMsg = "No such Opening No exists" });

                    QITcon = new SqlConnection(_QIT_connection);
                    DataTable dtData = new DataTable();

                    _Query = @" SELECT * FROM QIT_OpeningStock_QR_Header WHERE QRCodeID = @headerQR ";
                    _logger.LogInformation(" StockOpeningController : DetailIncNo() Query : {q} ", _Query.ToString());

                    oAdptr = new SqlDataAdapter(_Query, QITcon);
                    oAdptr.SelectCommand.Parameters.AddWithValue("@headerQR", payload.HeaderQRCodeID.Replace(" ", "~"));
                    QITcon.Open();
                    oAdptr.Fill(dtData);
                    QITcon.Close();

                    if (dtData.Rows.Count > 0)
                    {
                        _Query = @" 
                        INSERT INTO QIT_OpeningStock_QR_Detail 
                        (   DetailSrNo, HeaderSrNo, OpeningNo, BranchID, QRCodeID, Inc_No, ItemCode, DocLineNum, QRMngBy,  
                            BatchSerialNo, Qty, Remark, EntryDate 
                        )  
                        VALUES  
                        (  
                            ( SELECT ISNULL(max(DetailSrNo),0) + 1 from QIT_OpeningStock_QR_Detail),  
                            @hSrNo, @oNo,  @bId, @qr, @incNo, @iCode, @docLine, @qrMngBy,  
                            @batchSerial, @qty, @remark, @entryDate  
                        )


                        INSERT INTO QIT_OSQRStock_InvTrans
                        (   BranchID, TransId,  QRTransSeq, TransSeq, QRCodeID, OpeningNo, ItemCode,  BatchSerialNo,
                            FromObjType, ToObjType, FromWhs, ToWhs, FromBinAbsEntry, ToBinAbsEntry, Qty
                        ) 
                        VALUES 
                        (   @bId, (SELECT ISNULL(max(TransId),0) + 1 FROM QIT_OSQRStock_InvTrans), 
                            (SELECT ISNULL(max(QRTransSeq),0) + 1 FROM QIT_OSQRStock_InvTrans), 
                            (SELECT ISNULL(max(TransSeq),0) + 1 FROM QIT_OSQRStock_InvTrans), 
                            @qr, @oNo, @iCode,  @batchSerial, '310000001', '310000001', 
                            @fromWhs, @toWhs, @fromBin, @toBin, @qty   
                        )"
                        ;

                        _logger.LogInformation(" StockOpeningController : SaveDetailQR() Query : {q} ", _Query.ToString());

                        cmd = new SqlCommand(_Query, QITcon);
                        cmd.Parameters.AddWithValue("@hSrNo", dtData.Rows[0]["HeaderSrNo"]);
                        cmd.Parameters.AddWithValue("@oNo", payload.OpeningNo);
                        cmd.Parameters.AddWithValue("@bId", payload.BranchID);
                        cmd.Parameters.AddWithValue("@qr", payload.DetailQRCodeID.Replace(" ", "~"));
                        cmd.Parameters.AddWithValue("@incNo", payload.IncNo);
                        cmd.Parameters.AddWithValue("@iCode", payload.ItemCode);
                        cmd.Parameters.AddWithValue("@docLine", payload.DocLineNum);
                        cmd.Parameters.AddWithValue("@qrMngBy", payload.QRMngBy);
                        cmd.Parameters.AddWithValue("@batchSerial", payload.BatchSerialNo);
                        cmd.Parameters.AddWithValue("@qty", payload.Qty);
                        cmd.Parameters.AddWithValue("@remark", payload.Remark);
                        cmd.Parameters.AddWithValue("@fromWhs", dtOpeningNo.Rows[0]["WhsCode"].ToString());
                        cmd.Parameters.AddWithValue("@toWhs", dtOpeningNo.Rows[0]["WhsCode"].ToString());
                        cmd.Parameters.AddWithValue("@fromBin", dtOpeningNo.Rows[0]["BinAbsEntry"]);
                        cmd.Parameters.AddWithValue("@toBin", dtOpeningNo.Rows[0]["BinAbsEntry"]);
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
                        return BadRequest(new { StatusCode = "400", IsExist = "N", StatusMsg = "Header QR does not exist" });
                    }
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = " Details not found" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(" Error in StockOpeningController : SaveDetailQR() :: {Error}", ex.ToString());
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
        public async Task<ActionResult<IEnumerable<GetDetailDataQR>>> DetailDataQR(CheckDetailOpening payload)
        {
            try
            {
                _logger.LogInformation(" Calling StockOpeningController : DetailDataQR() ");

                DataTable dtData = new DataTable();
                QITcon = new SqlConnection(_QIT_connection);

                _Query = @" 
                SELECT  A.BranchID BranchID, B.QRCodeID HeaderQRCodeID, A.QRCodeID DetailQRCodeID,  A.OpeningNo, C.Project,
                        A.Inc_No IncNo, A.ItemCode ItemCode, A.DocLineNum, A.QRMngBy QRMngBy, A.Qty Qty, A.Remark, A.BatchSerialNo
                FROM QIT_OpeningStock_QR_Detail A 
                     INNER JOIN QIT_OpeningStock_QR_Header B on A.HeaderSrNo = B.HeaderSrNo
                     INNER JOIN QIT_OpeningStock C ON C.OpeningNo = A.OpeningNo and C.ItemCode = A.ItemCode and C.DocLineNum = A.DocLineNum
                WHERE ISNULL(A.BranchID, @bId) = @bId AND B.Series = @series and B.ObjType = @objType and 
                      A.ItemCode = @iCode and A.DocLineNum = @docLine and A.OpeningNo = @oNo FOR BROWSE ";
                _logger.LogInformation(" StockOpeningController : DetailDataQR() Query : {q} ", _Query.ToString());
                QITcon.Open();
                oAdptr = new SqlDataAdapter(_Query, QITcon);
                oAdptr.SelectCommand.Parameters.AddWithValue("@bId", payload.BranchID);
                oAdptr.SelectCommand.Parameters.AddWithValue("@series", payload.Series);
                oAdptr.SelectCommand.Parameters.AddWithValue("@objType", payload.ObjType);
                oAdptr.SelectCommand.Parameters.AddWithValue("@iCode", payload.ItemCode);
                oAdptr.SelectCommand.Parameters.AddWithValue("@docLine", payload.DocLineNum);
                oAdptr.SelectCommand.Parameters.AddWithValue("@oNo", payload.OpeningNo);

                oAdptr.Fill(dtData);
                QITcon.Close();

                if (dtData.Rows.Count > 0)
                {
                    List<GetDetailDataQR> obj = new List<GetDetailDataQR>();
                    dynamic arData = JsonConvert.SerializeObject(dtData);
                    obj = JsonConvert.DeserializeObject<List<GetDetailDataQR>>(arData.ToString().Replace("~", " "));
                    return obj;
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "No data found" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(" Error in StockOpeningController : DetailDataQR() :: {Error}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
            }
        }


        [HttpPost("GetAllDetailDataQR")]
        public async Task<ActionResult<IEnumerable<SaveDetailOpeningQR>>> GetAllDetailDataQR(CheckDetailOpening payload)
        {
            try
            {
                _logger.LogInformation(" Calling StockOpeningController : GetAllDetailDataQR() ");

                DataTable dtData = new DataTable();
                QITcon = new SqlConnection(_QIT_connection);

                _Query = @" 
                SELECT  A.BranchID BranchID, B.QRCodeID HeaderQRCodeID, A.QRCodeID DetailQRCodeID, 
                        A.Inc_No IncNo, A.ItemCode ItemCode, A.DocLineNum, A.QRMngBy QRMngBy, A.Qty Qty, A.Remark, A.BatchSerialNo
                FROM QIT_OpeningStock_QR_Detail A 
                     INNER JOIN QIT_OpeningStock_QR_Header B on A.HeaderSrNo = B.HeaderSrNo 
                WHERE ISNULL(A.BranchID, @bId) = @bId AND B.Series = @series and B.ObjType = @objType and A.OpeningNo = @oNo FOR BROWSE ";
                _logger.LogInformation(" StockOpeningController : GetAllDetailDataQR() Query : {q} ", _Query.ToString());
                QITcon.Open();
                oAdptr = new SqlDataAdapter(_Query, QITcon);
                oAdptr.SelectCommand.Parameters.AddWithValue("@bId", payload.BranchID);
                oAdptr.SelectCommand.Parameters.AddWithValue("@series", payload.Series);
                oAdptr.SelectCommand.Parameters.AddWithValue("@objType", payload.ObjType); 
                oAdptr.SelectCommand.Parameters.AddWithValue("@oNo", payload.OpeningNo);

                oAdptr.Fill(dtData);
                QITcon.Close();

                if (dtData.Rows.Count > 0)
                {
                    List<SaveDetailOpeningQR> obj = new List<SaveDetailOpeningQR>();
                    dynamic arData = JsonConvert.SerializeObject(dtData);
                    obj = JsonConvert.DeserializeObject<List<SaveDetailOpeningQR>>(arData.ToString().Replace("~", " "));
                    return obj;
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "No data found" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(" Error in StockOpeningController : GetAllDetailDataQR() :: {Error}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
            }
        }

        #endregion


        #region Method

        private string ConvertInQRString(string p_str)
        {
            try
            {
                _logger.LogInformation(" Calling StockOpeningController : ConvertInQRString() ");
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
                _logger.LogError("Error in StockOpeningController : ConvertInQRString() :: {Error}", ex.ToString());
                return String.Empty;
            }
        }

        #endregion

    }
}
