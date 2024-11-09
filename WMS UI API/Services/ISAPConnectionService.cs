using System.Data.SqlClient;

namespace WMS_UI_API.Services
{
    public interface ISAPConnectionService
    {
        void Initialize();
        void Dispose();
    } 
}
