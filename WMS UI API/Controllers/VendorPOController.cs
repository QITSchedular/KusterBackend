using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Data.SqlClient;
using WMS_UI_API.Common;

namespace WMS_UI_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class VendorPOController : ControllerBase
    {
        private string _ApplicationApiKey = string.Empty;
        private string _connection = string.Empty;
        private string _QIT_connection = string.Empty;
        private string _QIT_DB = string.Empty;
        private string _Query = string.Empty;
        private SqlCommand cmd;
        public Global objGlobal;

        public IConfiguration Configuration { get; }
        private readonly ILogger<VendorPOController> _logger;

        public VendorPOController(IConfiguration configuration, ILogger<VendorPOController> logger)
        {
            if (objGlobal == null)
                objGlobal = new Global();
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

                //objGlobal.gServer = Configuration["Server"];
                //objGlobal.gSqlVersion = Configuration["SQLVersion"];
                //objGlobal.gCompanyDB = Configuration["CompanyDB"];
                //objGlobal.gLicenseServer = Configuration["LicenseServer"];
                //objGlobal.gSAPUserName = Configuration["SAPUserName"];
                //objGlobal.gSAPPassword = Configuration["SAPPassword"];
                //objGlobal.gDBUserName = Configuration["DBUserName"];
                //objGlobal.gDBPassword = Configuration["DbPassword"];


            }
            catch (Exception ex)
            {
                _logger.LogError(" Error in VendorPOController :: {Error}" + ex.ToString());
            }
        }
    }
}
