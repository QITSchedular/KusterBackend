using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Data.SqlClient;
using WMS_UI_API.Common;
using WMS_UI_API.Models;

namespace WMS_UI_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProjectTransferController : ControllerBase
    {
        private string _ApplicationApiKey = string.Empty;
        private string _connection = string.Empty;
        private string _QIT_connection = string.Empty;
        private string _QIT_DB = string.Empty;
        private string _Query = string.Empty;
        private SqlCommand cmd;

        public IConfiguration Configuration { get; }
        private readonly ILogger<ProjectTransferController> _logger;

        public ProjectTransferController(IConfiguration configuration, ILogger<ProjectTransferController> logger)
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
                _logger.LogError(" Error in ProjectTransferController :: {Error}" + ex.ToString());
            }
        }



        [HttpPost("GetQRDetails")]
        public async Task<ActionResult<IEnumerable<getQRDataOutput>>> GetQRDetails([FromBody] getQRDataInput payload)
        {
            string _IsSaved = "N";
            try
            {
                _logger.LogInformation(" Calling ProjectTransferController : GetQRDetails() ");
                List<getQRDataOutput> obj = new List<getQRDataOutput>();
                System.Data.DataTable dtGateIN = new System.Data.DataTable();
                SqlConnection con = new SqlConnection(_QIT_connection);

                _Query = @" 
                SELECT B.DocEntry PODocEntry, B.DocNum PODocNum, B.GateInNo, A.ItemCode, C.ItemName, A.LineNum, A.QRCodeID, A.BatchSerialNo, A.Qty, B.Project
                FROM QIT_QR_Detail A 
                     inner join QIT_GateIN B ON A.GateInNo = B.GateInNo and A.ItemCode = B.ItemCode and A.LineNum = B.LineNum
                     inner join QIT_Item_Master C ON A.ItemCode = C.ItemCode
                where A.QRCodeID = @dQR and ISNULL(B.BranchId, @bId) = @bId
                ";

                con.Open();
                SqlDataAdapter oAdptr = new SqlDataAdapter(_Query, con);
                oAdptr.SelectCommand.Parameters.AddWithValue("@dQR", payload.QRCodeID.Replace(" ", "~"));
                oAdptr.SelectCommand.Parameters.AddWithValue("@bId", payload.BranchID);
                oAdptr.Fill(dtGateIN);
                con.Close();

                if (dtGateIN.Rows.Count > 0)
                {
                    dynamic arData = JsonConvert.SerializeObject(dtGateIN);
                    obj = JsonConvert.DeserializeObject<List<getQRDataOutput>>(arData.ToString().Replace("~", " "));
                    return obj;
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = " Details not found" });
                }

            }
            catch (Exception ex)
            {
                _logger.LogError(" Error in ProjectTransferController : GetQRDetails() :: {Error}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
            }
        }


        [HttpPut("UpdateProject")]
        public IActionResult UpdateProject([FromBody] updateProjectInput payload)
        {
            string _IsSaved = "N";
            try
            {
                _logger.LogInformation(" Calling ProjectTransferController : UpdateProject() ");

                string p_ErrorMsg = string.Empty;

                if (payload != null)
                {
                    SqlConnection con = new SqlConnection(_QIT_connection);
                    _Query = @" 
                    UPDATE B
                    SET B.Project = @proj
                    FROM QIT_GateIN B
                         INNER JOIN QIT_QR_Detail A ON A.GateInNo = B.GateInNo and A.ItemCode = B.ItemCode and A.LineNum = B.LineNum
                         INNER JOIN QIT_Item_Master C ON A.ItemCode = C.ItemCode
                    where A.QRCodeID = @dQR AND ISNULL(B.BranchId, @bId) = @bId ";
                    _logger.LogInformation(" ProjectTransferController : UpdateProject() Query : {q} ", _Query.ToString());

                    cmd = new SqlCommand(_Query, con);
                    cmd.Parameters.AddWithValue("@proj", payload.Project);
                    cmd.Parameters.AddWithValue("@dQR", payload.QRCodeID.Replace(" ","~"));
                    cmd.Parameters.AddWithValue("@bId", payload.BranchID);

                    con.Open();
                    int intNum = cmd.ExecuteNonQuery();
                    con.Close();

                    if (intNum > 0)
                        _IsSaved = "Y";
                    else
                        _IsSaved = "N";

                    return Ok(new { StatusCode = "200", IsSaved = _IsSaved, StatusMsg = "Updated Successfully!!!" });
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Details not found" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Error in ProjectTransferController : UpdateProject() :: {Error}", ex.ToString());

                if (ex.Message.ToLower().Contains("uc_location"))
                {
                    return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Location already exist" });
                }
                else if (ex.Message.ToLower().Contains("uc_gstin"))
                {
                    return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "GSTIN already exist" });
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = ex.Message.ToString() });
                }
            }
        }

    }
}
