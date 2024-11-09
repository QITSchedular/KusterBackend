using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.Extensions.Options;
using System.Data.SqlClient;

using SAPbobsCOM;

namespace WMS_UI_API.Services
{
    public class SAPConnectionService : ISAPConnectionService
    {
        private readonly SAPConnectionSettings _settings;
        public Company oComp = new();

        public SAPConnectionService(IOptions<SAPConnectionSettings> settings)
        {
            _settings = settings.Value;
        }

        public void Initialize()
        {
            oComp.Server = _settings.Server;

            if (_settings.SQLVersion == "2008")
                oComp.DbServerType = BoDataServerTypes.dst_MSSQL2008;
            else if (_settings.SQLVersion == "2012")
                oComp.DbServerType = BoDataServerTypes.dst_MSSQL2012;
            else if (_settings.SQLVersion == "2014")
                oComp.DbServerType = BoDataServerTypes.dst_MSSQL2014;
            else if (_settings.SQLVersion == "2016")
                oComp.DbServerType = BoDataServerTypes.dst_MSSQL2016;
            else if (_settings.SQLVersion == "2017")
                oComp.DbServerType = BoDataServerTypes.dst_MSSQL2017;
           

            oComp.CompanyDB = _settings.CompanyDB;
            oComp.LicenseServer = _settings.LicenseServer;
            oComp.UserName = _settings.SAPUserName;
            oComp.Password = _settings.SAPPassword;
            oComp.UseTrusted = false;
            oComp.DbUserName = _settings.DBUserName;
            oComp.DbPassword = _settings.DbPassword;

            if (oComp.Connect() == 0)
                Console.WriteLine("SAP Connection done.");
            else
                Console.WriteLine("SAP Connection failed.");
           

            //// Initialize SAP connection with settings
            //_sapConnection = new SAPConnectionSettings
            //{
            //    Server = _settings.Server,
            //    SQLVersion = _settings.SQLVersion,
            //    CompanyDB = _settings.CompanyDB,
            //    LicenseServer = _settings.LicenseServer,
            //    SAPUserName = _settings.SAPUserName,
            //    SAPPassword = _settings.SAPPassword,
            //    DBUserName = _settings.DBUserName,
            //    DbPassword = _settings.DbPassword
            //};
            // Connect to SAP...
            Console.WriteLine("SAP Connection initialized.");
        }

        public void Dispose()
        {
            //// Initialize SAP connection with settings
            //this.Dispose();
            //// Connect to SAP...
            //Console.WriteLine("SAP Connection disposing.");
        }


    }
}
