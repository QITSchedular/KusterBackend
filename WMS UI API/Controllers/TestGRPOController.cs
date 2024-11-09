//using Microsoft.AspNetCore.Http;
//using Microsoft.AspNetCore.Mvc;
//using System.Data;
//using System.Data.SqlClient;
//using WMS_UI_API.Common;
//using SAPbobsCOM;

//namespace WMS_UI_API.Controllers
//{
//    [Route("api/[controller]")]
//    [ApiController]
//    public class TestGRPOController : ControllerBase
//    {
//        private string _ApplicationApiKey = string.Empty;
//        private string _connection = string.Empty;
//        private string _QIT_connection = string.Empty;
//        private string _QIT_DB = string.Empty;
//        private string _Query = string.Empty;
//        private SqlCommand cmd;
//        public Global objGlobal;

//        public IConfiguration Configuration { get; }
//        private readonly ILogger<TestGRPOController> _logger;

//        public TestGRPOController(IConfiguration configuration, ILogger<TestGRPOController> logger)
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
//                _logger.LogError(" Error in TestGRPOController :: {Error}" + ex.ToString());
//            }
//        }


//        [HttpPost("CreateDraftGRPO")]
//        public async Task<IActionResult> CreateDraftGRPO()
//        {
//            if (objGlobal == null)
//                objGlobal = new Global();

//            string p_ErrorMsg = string.Empty;
//            string _IsSaved = "N";

//            SqlDataAdapter oAdptr;
//            SqlConnection QITcon;
//            SqlConnection SAPcon;

//            try
//            {
//                _logger.LogInformation(" Calling TestGRPOController : CreateDraftGRPO() for GateInNo : " + 648);


//                #region Validate QR Items - must do GRPO of all QRs of GateINNo
//                System.Data.DataTable dtQRData = new System.Data.DataTable();
//                QITcon = new SqlConnection(_QIT_connection);

//                _Query = @" SELECT * FROM QIT_QR_Detail WHERE GateInNo = @gateInNo and ISNULL(BranchID, @bID) = @bID ";
//                _logger.LogInformation(" TestGRPOController for GateInNo : " + 648 + "  Get QR data Query : {q} ", _Query.ToString());
//                QITcon.Open();
//                oAdptr = new SqlDataAdapter(_Query, QITcon);
//                oAdptr.SelectCommand.Parameters.AddWithValue("@gateInNo", 648);
//                oAdptr.SelectCommand.Parameters.AddWithValue("@bID", 1);
//                oAdptr.Fill(dtQRData);
//                QITcon.Close();

//                #endregion

//                #region Get Config
//                System.Data.DataTable dtConfig = new System.Data.DataTable();
//                QITcon = new SqlConnection(_QIT_connection);
//                _Query = @" SELECT * FROM QIT_Config_Master WHERE ISNULL(BranchID, @bId) = @bId ";
//                QITcon.Open();
//                oAdptr = new SqlDataAdapter(_Query, QITcon);
//                oAdptr.SelectCommand.Parameters.AddWithValue("@bId", 1);
//                oAdptr.Fill(dtConfig);
//                QITcon.Close();
//                #endregion

//                var (success, errorMsg) = await objGlobal.ConnectSAP();
//                if (success)
//                {
//                    _logger.LogInformation(" TestGRPOController for GateInNo : " + 648 + "  SAP connected");

//                    if (true)
//                    {
//                        _logger.LogInformation(" Data stored in QIT DB : CreateDraftGRPO() for GateInNo : " + 648);

//                        #region Gate IN Details

//                        System.Data.DataTable dtGateIN = new System.Data.DataTable();
//                        QITcon = new SqlConnection(_QIT_connection);
//                        _Query = @" SELECT * FROM QIT_GateIN WHERE GateInNo = @gNo ";
//                        QITcon.Open();
//                        oAdptr = new SqlDataAdapter(_Query, QITcon);
//                        oAdptr.SelectCommand.Parameters.AddWithValue("@gNo", 648);
//                        oAdptr.Fill(dtGateIN);
//                        QITcon.Close();

//                        #endregion

//                        int _Line = 0;
//                        double dblRate = 0.0063;
//                        #region set GRPO Header level data
//                        Documents grpo = (Documents)objGlobal.oComp.GetBusinessObject(BoObjectTypes.oPurchaseDeliveryNotes);
//                        grpo.DocObjectCode = BoObjectTypes.oPurchaseDeliveryNotes;

//                        grpo.Series = 1281;
//                        grpo.CardCode = "V000151";
//                        grpo.NumAtCard = dtGateIN.Rows[0]["GRPOVendorRefNo"].ToString();
//                        grpo.TaxDate = (DateTime)dtGateIN.Rows[0]["GRPODocDate"]; // GRPODocDate from Gate IN table
//                        grpo.DocDate = DateTime.Now;

//                        grpo.Comments = "KGS PO";
//                        grpo.BPL_IDAssignedToInvoice = 1;

//                        grpo.UserFields.Fields.Item("U_QIT_FromWeb").Value = "Y";
//                        grpo.UserFields.Fields.Item("U_GE").Value = "648";
//                        grpo.UserFields.Fields.Item("U_GEDate").Value = dtGateIN.Rows[0]["RecDate"];
//                        grpo.UserFields.Fields.Item("U_Veh_Number").Value = dtGateIN.Rows[0]["VehicleNo"];
//                        grpo.UserFields.Fields.Item("U_Trans").Value = dtGateIN.Rows[0]["TransporterCode"];
//                        grpo.UserFields.Fields.Item("U_LRNo").Value = dtGateIN.Rows[0]["LRNo"];
//                        grpo.UserFields.Fields.Item("U_LRDate").Value = dtGateIN.Rows[0]["LRDate"].ToString();

//                        #endregion


//                        SAPbobsCOM.Documents oPurchaseOrder = (SAPbobsCOM.Documents)objGlobal.oComp.GetBusinessObject(SAPbobsCOM.BoObjectTypes.oPurchaseOrders);

//                        if (oPurchaseOrder.GetByKey(68335))
//                        {
//                            DateTime dtDeliveryDate;
//                            if (dtConfig.Rows[0]["GRPODeliveryDate"].ToString().ToUpper() == "P")
//                                dtDeliveryDate = oPurchaseOrder.DocDueDate;
//                            else if (dtConfig.Rows[0]["GRPODeliveryDate"].ToString().ToUpper() == "G")
//                                dtDeliveryDate = (DateTime)dtGateIN.Rows[0]["RecDate"];
//                            else
//                                dtDeliveryDate = DateTime.Now;

//                            grpo.DocDueDate = dtDeliveryDate; // DateTime.Now;
                                                         
//                            //grpo.Lines.ItemCode = "100100210001";
//                            grpo.Lines.Quantity = 5;
//                            //grpo.Lines.Quantity = dblRate * 5;
//                            grpo.Lines.Price = 5000;
//                            grpo.Lines.TaxCode = "CS18T1";
//                            grpo.Lines.BaseType = 22;
//                            grpo.Lines.BaseEntry = 68335;
//                            grpo.Lines.BaseLine = 0;
//                            grpo.Lines.WarehouseCode = "VD-QA";

//                            grpo.Lines.MeasureUnit = "KGS"; 
                            
                              
//                            int _batchLine = 0;

//                            grpo.Lines.BatchNumbers.BaseLineNumber = _Line;
//                            grpo.Lines.BatchNumbers.BatchNumber = "2402000220BPO";
//                            //grpo.Lines.BatchNumbers.Quantity = Convert.ToDouble(batch.Qty);
//                            grpo.Lines.BatchNumbers.Quantity = dblRate * 5;
                            

//                            grpo.Lines.BatchNumbers.Add();

//                            _batchLine = _batchLine + 1;


//                            grpo.Lines.Add();
//                            _Line = _Line + 1;

//                        }
//                        else
//                        {

//                            return BadRequest(new
//                            {
//                                StatusCode = "400",
//                                IsSaved = "N",

//                                StatusMsg = "No such PO exist"
//                            });
//                        }

//                        int addResult = grpo.Add();
//                        // Check if the addition was successful
//                        if (addResult != 0)
//                        { 
//                            string msg;
//                            msg = "Error code: " + objGlobal.oComp.GetLastErrorCode() + Environment.NewLine +
//                                  "Error message: " + objGlobal.oComp.GetLastErrorDescription();
//                            _logger.LogInformation(" Calling TestGRPOController : Error " + msg);
//                            return BadRequest(new
//                            {
//                                StatusCode = "400",
//                                IsSaved = _IsSaved,
                                
//                                StatusMsg = "Error code: " + addResult + Environment.NewLine +
//                                            "Error message: " + objGlobal.oComp.GetLastErrorDescription()
//                            });
//                        }
//                        else
//                        {
//                            int docEntry = int.Parse(objGlobal.oComp.GetNewObjectKey());

//                            return Ok(new
//                            {
//                                StatusCode = "200",
//                                IsSaved = "Y", 
//                                DocEntry = docEntry, 
//                                StatusMsg = "GRPO added successfully !!!"
//                            });

//                        }
//                    }
//                    else
//                    {

//                        return BadRequest(new
//                        {
//                            StatusCode = "400",
//                            IsSaved = "N",

//                            StatusMsg = "Problem in saving data in Transaction table"
//                        });
//                    }
//                }
//                else
//                {
//                    string msg;
//                    msg = "Error code: " + objGlobal.oComp.GetLastErrorCode() + Environment.NewLine +
//                          "Error message: " + objGlobal.oComp.GetLastErrorDescription();
//                    return BadRequest(new
//                    {
//                        StatusCode = "400",
//                        IsSaved = _IsSaved,

//                        StatusMsg = "Error code: " + objGlobal.oComp.GetLastErrorCode() + Environment.NewLine +
//                                    "Error message: " + objGlobal.oComp.GetLastErrorDescription()
//                    });
//                }

//            }
//            catch (Exception ex)
//            {
//                _logger.LogError("Error in TestGRPOController : CreateDraftGRPO() :: {Error}", ex.ToString());
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


//        public DataTable ConvertListToDataTable<T>(List<T> dataList)
//        {
//            DataTable dataTable = new DataTable();
//            try
//            {
//                if (dataList.Count > 0)
//                {
//                    // Get the properties of the DetailData class
//                    var properties = typeof(T).GetProperties();

//                    // Create columns in the DataTable based on the properties
//                    foreach (var property in properties)
//                    {
//                        dataTable.Columns.Add(property.Name, property.PropertyType);
//                    }

//                    // Add rows to the DataTable based on the objects in the list
//                    foreach (var item in dataList)
//                    {
//                        DataRow row = dataTable.NewRow();

//                        foreach (var property in properties)
//                        {
//                            row[property.Name] = property.GetValue(item);
//                        }

//                        dataTable.Rows.Add(row);
//                    }
//                }
//                return dataTable;
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError("Error in TestGRPOController : ConvertListToDataTable() :: {Error}", ex.ToString());
//                return dataTable;
//            }
//        }


//    }
//}
