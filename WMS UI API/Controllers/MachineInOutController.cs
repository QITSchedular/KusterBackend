using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Data.SqlClient;
using System.Data;
using WMS_UI_API.Common;
using Newtonsoft.Json;
using WMS_UI_API.Models;

namespace WMS_UI_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MachineInOutController : ControllerBase
    {
        private string _QIT_connection = string.Empty;
        private string _QIT_DB = string.Empty;
        private string _Query = string.Empty;
        private SqlCommand cmd;
        DataSet ds = new DataSet();
        private readonly ILogger<MachineInOutController> _logger;
        public IConfiguration Configuration { get; }
        public MachineInOutController(IConfiguration configuration, ILogger<MachineInOutController> MachineInOutLogger)
        {
            _logger = MachineInOutLogger;
            try
            {
                Configuration = configuration;

                _QIT_connection = Configuration["connectApp:QITConnString"];
                _QIT_DB = Configuration["QITDB"];
                Global.QIT_DB = _QIT_DB;
                Global.SAP_DB = Configuration["CompanyDB"];

            }
            catch (Exception e)
            {
                _logger.LogError("Error : " + e.ToString());
            }
        }


        [HttpPost("MachineLogIn")]
        public IActionResult MachineLogIn(MachineIn payload)
        {
            string _IsSaved = "N";
            string _msg = "Details not found";
            int _StatusCode = 400;
            try
            {
                SqlConnection con = new SqlConnection(_QIT_connection);
                _logger.LogInformation(" Calling MachineInOutController : MachineLogIn() ");
                string _whereModule = string.Empty;

                if (payload != null)
                {
                    if (payload.MachineId == string.Empty || payload.MachineId == null)
                    {
                        return BadRequest(new { StatusCode = "400", StatusMsg = "Provide Machine Id" });
                    }

                    if (payload.UserName == string.Empty || payload.UserName == null)
                    {
                        return BadRequest(new { StatusCode = "400", StatusMsg = "Provide User Name" });
                    }

                    if (payload.ProcessID <= 0 || payload.ProcessID == null)
                    {
                        return BadRequest(new { StatusCode = "400", StatusMsg = "Provide Process Id" });
                    }

                    if (payload.ShiftID <= 0 || payload.ShiftID == null)
                    {
                        return BadRequest(new { StatusCode = "400", StatusMsg = "Provide Shift Id" });
                    }

                    if (payload.WhsCode == string.Empty || payload.WhsCode == null)
                    {
                        return BadRequest(new { StatusCode = "400", StatusMsg = "Provide Warehouse Code" });
                    }

                    _Query = @"SELECT COUNT(*) FROM QIT_Machine_Master WHERE MachineID=@MachineId";
                    if (con.State == ConnectionState.Closed)
                        con.Open();
                    SqlCommand cmd = new SqlCommand(_Query, con);
                    cmd.Parameters.AddWithValue("@MachineId", payload.MachineId);

                    Object Value1 = cmd.ExecuteScalar();
                    con.Close();
                    if (Int32.Parse(Value1.ToString()) == 0)
                    {
                        return BadRequest(new { StatusCode = "400", StatusMsg = "Machine not found...!!" });
                    }


                    _Query = @"SELECT COUNT(*) FROM QIT_Process_Master WHERE ID=@ProcessId";
                    if (con.State == ConnectionState.Closed)
                        con.Open();
                    cmd = new SqlCommand(_Query, con);
                    cmd.Parameters.AddWithValue("@ProcessId", payload.ProcessID);

                    Object Value2 = cmd.ExecuteScalar();
                    con.Close();
                    if (Int32.Parse(Value2.ToString()) == 0)
                    {
                        return BadRequest(new { StatusCode = "400", StatusMsg = "Process not found...!!" });
                    }


                    _Query = @"SELECT COUNT(*) FROM QIT_Shift_Master WHERE ID=@ShiftId";
                    if (con.State == ConnectionState.Closed)
                        con.Open();
                    cmd = new SqlCommand(_Query, con);
                    cmd.Parameters.AddWithValue("@ShiftId", payload.ShiftID);

                    Object Value3 = cmd.ExecuteScalar();
                    con.Close();
                    if (Int32.Parse(Value3.ToString()) == 0)
                    {
                        return BadRequest(new { StatusCode = "400", StatusMsg = "Shift not found...!!" });
                    }


                    _Query = @"SELECT User_ID FROM QIT_User_Master WHERE User_Name=@UserName";
                    if (con.State == ConnectionState.Closed)
                        con.Open();
                    cmd = new SqlCommand(_Query, con);
                    cmd.Parameters.AddWithValue("@UserName", payload.UserName);
                    int userId = 0;

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            //return reader["MachineId"].ToString();
                            // Step 2: Increment the MachineId
                            userId = (int.Parse(reader["User_ID"].ToString()));
                        }
                    }

                    if (userId > 0)
                    {
                        _Query = @"SELECT COUNT(*) FROM QIT_MachineInOut WHERE UserID=@UserID AND CONVERT(DATE, InTime) = CONVERT(DATE, GETDATE()) AND (UpdateDate IS NULL)";
                        if (con.State == ConnectionState.Closed)
                            con.Open();
                        cmd = new SqlCommand(_Query, con);
                        cmd.Parameters.AddWithValue("@UserID", userId);

                        Object Value = cmd.ExecuteScalar();
                        con.Close();
                        if (Int32.Parse(Value.ToString()) > 0)
                        {
                            return BadRequest(new { StatusCode = "400", StatusMsg = "Already logged in...!!" });
                        }
                        else
                        {
                            _Query = @"INSERT INTO [dbo].[QIT_MachineInOut]
                                       ([MachineID]
                                       ,[UserID]
                                       ,[ProcessID]
                                       ,[ShiftID]
                                       ,[WhsCode])
                                 VALUES
                                       (@MachineID
                                       ,@UserID
                                       ,@ProcessID
                                       ,@ShiftID
                                       ,@WhsCode)";
                            _logger.LogInformation(" MachineInOutController : MachineLogIn() Query : {q} ", _Query.ToString());

                            cmd = new SqlCommand(_Query, con);
                            cmd.Parameters.AddWithValue("@MachineID", payload.MachineId);
                            cmd.Parameters.AddWithValue("@userId", userId);
                            cmd.Parameters.AddWithValue("@ProcessID", payload.ProcessID);
                            cmd.Parameters.AddWithValue("@ShiftID", payload.ShiftID);
                            cmd.Parameters.AddWithValue("@WhsCode", payload.WhsCode);
                            con.Open();
                            int intNum = cmd.ExecuteNonQuery();
                            con.Close();

                            if (intNum > 0)
                            {
                                _IsSaved = "Y";
                                _StatusCode = 200;
                                _msg = "Logged In Successfully!!!";
                            }
                            else
                                _IsSaved = "N";

                            return Ok(new { StatusCode = _StatusCode, IsSaved = _IsSaved, StatusMsg = _msg });
                        }
                    }
                    else
                    {
                        return BadRequest(new { StatusCode = "400", StatusMsg = "User not found...!!" });
                    }

                }
                else
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Details not found...!!" });
                }


            }
            catch (Exception ex)
            {
                _logger.LogError(" Error in MachineInOutController : MachineLogIn() :: {Error}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
            }
        }

        [HttpPost("MachineLogOut")]
        public IActionResult MachineLogOut(MachineOut payload)
        {
            string _IsSaved = "N";
            string _msg = "Details not found";
            int _StatusCode = 400;
            try
            {
                SqlConnection con = new SqlConnection(_QIT_connection);
                _logger.LogInformation(" Calling MachineInOutController : MachineLogOut() ");
                string _whereModule = string.Empty;

                if (payload != null)
                {
                    if (payload.UserName == string.Empty || payload.UserName == null)
                    {
                        return BadRequest(new { StatusCode = "400", StatusMsg = "Provide User Name" });
                    }

                    _Query = @"SELECT Id,UserID FROM QIT_MachineInOut WHERE UserID=(SELECT User_ID FROM QIT_User_Master WHERE User_Name=@UserName) AND CONVERT(DATE, InTime) = CONVERT(DATE, GETDATE()) AND (UpdateDate IS NULL)";
                    if (con.State == ConnectionState.Closed)
                        con.Open();
                    SqlCommand cmd = new SqlCommand(_Query, con);
                    cmd.Parameters.AddWithValue("@UserName", payload.UserName);
                    int loginId = 0;
                    int userId = 0;

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            //return reader["MachineId"].ToString();
                            // Step 2: Increment the MachineId
                            loginId = (int.Parse(reader["Id"].ToString()));
                            userId = (int.Parse(reader["UserID"].ToString()));
                        }
                    }

                    if (loginId > 0 & userId > 0)
                    {
                        _Query = @"UPDATE [dbo].[QIT_MachineInOut]
                                   SET OutTime = GETDATE()
                                      ,UpdateDate = GETDATE()
                                   WHERE UserID= @UserID AND CONVERT(DATE, InTime) = CONVERT(DATE, GETDATE()) AND (UpdateDate IS NULL)";
                        _logger.LogInformation(" MachineInOutController : MachineLogIn() Query : {q} ", _Query.ToString());

                        cmd = new SqlCommand(_Query, con);
                        cmd.Parameters.AddWithValue("@UserID", userId);

                        int intNum = cmd.ExecuteNonQuery();
                        con.Close();

                        if (intNum > 0)
                        {
                            _IsSaved = "Y";
                            _StatusCode = 200;
                            _msg = "Logged Out Successfully!!!";
                        }
                        else
                            _IsSaved = "N";

                        return Ok(new { StatusCode = _StatusCode, IsSaved = _IsSaved, StatusMsg = _msg });
                    }
                    else
                    {
                        return BadRequest(new { StatusCode = "400", StatusMsg = "Details not found...!!" });
                    }

                }
                else
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Details not found...!!" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(" Error in MachineInOutController : MachineLogIn() :: {Error}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
            }
        }

        [HttpPost("ChkMcLogInStat")]
        public async Task<ActionResult<IEnumerable<UserMachineStat>>> ChkMcLogInStat(MachineOut payload)
        {
            try
            {
                List<UserMachineStat> obj = new List<UserMachineStat>();
                SqlConnection con = new SqlConnection(_QIT_connection);
                _logger.LogInformation(" Calling MachineInOutController : ChkMcLogInStat() ");
                DataTable dtPeriod = new DataTable();
                string _whereModule = string.Empty;

                if (payload != null)
                {
                    if (payload.UserName == string.Empty || payload.UserName == null)
                    {
                        return BadRequest(new { StatusCode = "400", StatusMsg = "Provide User Name" });
                    }

                    _Query = @"SELECT D.MachineName,B.MachineID,B.UserID,B.WhsCode,B.InTime,B.OutTime,A.Name ProcessName,C.Name ShiftName FROM QIT_MachineInOut B
                               INNER JOIN QIT_Process_Master A ON A.ID=B.ProcessID
                               INNER JOIN QIT_Shift_Master C ON C.Id=B.ShiftID
                               INNER JOIN QIT_Machine_Master D ON D.MachineID=B.MachineID
                               WHERE B.UserID=(SELECT User_ID FROM QIT_User_Master WHERE User_Name=@UserName) AND CONVERT(DATE, B.InTime) = CONVERT(DATE, GETDATE()) AND (B.UpdateDate IS NULL)";
                    //_Query = @"SELECT * FROM QIT_MachineInOut WHERE UserID=@UserId AND CONVERT(DATE, InTime) = CONVERT(DATE, GETDATE()) AND (UpdateDate IS NULL)";
                    con.Open();
                    SqlDataAdapter oAdptr = new SqlDataAdapter(_Query, con);
                    oAdptr.SelectCommand.Parameters.AddWithValue("@UserName", payload.UserName);
                    dtPeriod = new DataTable();
                    oAdptr.Fill(dtPeriod);
                    con.Close();

                    if (dtPeriod.Rows.Count > 0)
                    {
                        dynamic arData = JsonConvert.SerializeObject(dtPeriod);
                        obj = JsonConvert.DeserializeObject<List<UserMachineStat>>(arData.ToString());
                        return obj;
                    }
                    else
                    {
                        return BadRequest(new { StatusCode = "400", StatusMsg = "No data...!!" });
                    }

                }
                else
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Details not found...!!" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(" Error in MachineInOutController : MachineLogIn() :: {Error}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
            }
        }


        [HttpPost("Machine_Management")]
        public async Task<ActionResult<IEnumerable<MachineReport>>> Machine_Management(MachineDetails payload)
        {
            try
            {
                List<MachineReport> obj = new List<MachineReport>();
                SqlConnection con = new SqlConnection(_QIT_connection);
                _logger.LogInformation(" Calling MachineInOutController : Machine_Management() ");
                DataTable dtPeriod = new DataTable();
                string _whereModule = "";

                if (payload != null)
                {
                    if (payload.FromDate == string.Empty || payload.FromDate.ToLower() == "string")
                    {
                        return BadRequest(new { StatusCode = "400", StatusMsg = "Provide FromDate" });
                    }

                    if (payload.ToDate == string.Empty || payload.ToDate.ToLower() == "string")
                    {
                        return BadRequest(new { StatusCode = "400", StatusMsg = "Provide ToDate" });
                    }

                    if (payload.MachineId != string.Empty && payload.MachineId != null)
                    {
                        _whereModule += " AND A.MachineID=@MachineId";
                    }

                    if (payload.Status != string.Empty && payload.Status != null)
                    {
                        if (payload.Status.ToLower() == "in")
                            _whereModule += " AND (A.OutTime IS  NULL)";
                        else if (payload.Status.ToLower() == "out")
                            _whereModule += " AND (A.OutTime IS NOT NULL)";
                        else
                            return BadRequest(new { StatusCode = "400", StatusMsg = "Provide Valid Status" });
                    }

                    if (payload.WhsCode != string.Empty && payload.WhsCode != null)
                    {
                        _whereModule += " AND A.WhsCode=@WhsCode";
                    }

                    if (payload.UserId != null && payload.UserId > 0)
                    {
                        _whereModule += " AND A.UserID=@userId";
                    }


                    _Query = @"SELECT A.MachineId,B.MachineSrNo,B.MachineName,C.User_Name,D.Name ProcessName,E.Name ShiftName,A.WhsCode,A.InTime LoggedInTime,A.OutTime LoggedOutTime,
                            CASE 
	                            WHEN A.OutTime IS NOT NULL THEN 
		                            CONVERT(VARCHAR, DATEADD(MINUTE, DATEDIFF(MINUTE, A.InTime, A.OutTime), 0), 108)
	                            ELSE 
		                            'Active' 
                            END AS TotalTime
                            FROM QIT_MachineInOut A 
                            INNER JOIN QIT_Machine_Master B ON B.MachineID=A.MachineID 
                            INNER JOIN QIT_User_Master C ON C.User_ID=A.UserID
                            INNER JOIN QIT_Process_Master D ON D.ID=A.ProcessID
                            INNER JOIN QIT_Shift_Master E ON E.Id=A.ShiftID
                            WHERE A.EntryDate>=@frmDate AND A.EntryDate<=@endDate " + _whereModule;
                    con.Open();
                    SqlDataAdapter oAdptr = new SqlDataAdapter(_Query, con);
                    oAdptr.SelectCommand.Parameters.AddWithValue("@frmDate", payload.FromDate);
                    oAdptr.SelectCommand.Parameters.AddWithValue("@endDate", payload.ToDate);
                    if (payload.MachineId != string.Empty && payload.MachineId != null)
                    {
                        oAdptr.SelectCommand.Parameters.AddWithValue("@MachineId", payload.MachineId);
                    }

                    if (payload.WhsCode != string.Empty && payload.WhsCode != null)
                    {
                        oAdptr.SelectCommand.Parameters.AddWithValue("@WhsCode", payload.WhsCode);
                    }

                    if (payload.UserId != null && payload.UserId > 0)
                    {
                        oAdptr.SelectCommand.Parameters.AddWithValue("@userId", payload.UserId);
                    }
                    dtPeriod = new DataTable();
                    oAdptr.Fill(dtPeriod);
                    con.Close();

                    if (dtPeriod.Rows.Count > 0)
                    {
                        dynamic arData = JsonConvert.SerializeObject(dtPeriod);
                        obj = JsonConvert.DeserializeObject<List<MachineReport>>(arData.ToString());
                        return obj;
                    }
                    else
                    {
                        return BadRequest(new { StatusCode = "400", StatusMsg = "No data...!!" });
                    }

                }
                else
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Details not found...!!" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(" Error in MachineInOutController : Machine_Management() :: {Error}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
            }
        }

    }
}
