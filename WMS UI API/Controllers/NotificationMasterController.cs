using WMS_UI_API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.Data;
using System.Data.SqlClient;
using System.Reflection.PortableExecutable;

using Microsoft.AspNetCore.SignalR;
using WMS_UI_API.Hubs;
using WMS_UI_API.Controllers;
using WMS_UI_API.Models;
using WMS_UI_API.Common;

namespace WMS_UI_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NotificationMasterController : ControllerBase
    {
        private readonly IHubContext<NotificationHub> _hubContext;
        private string _QIT_connection = string.Empty;
        private string _QIT_DB = string.Empty;
        private string _Query = string.Empty;
        private SqlCommand cmd;
        DataSet ds = new DataSet();
        private readonly ILogger<AuthUserController> _logger;
        public IConfiguration Configuration { get; }

        public NotificationMasterController(IConfiguration configuration, ILogger<AuthUserController> AuthLogger, IHubContext<NotificationHub> hubContext)
        {
            _logger = AuthLogger;
            try
            {
                _hubContext = hubContext;
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



        [HttpPost]
        public async Task<ActionResult<IEnumerable<NotificationMasterClass>>> Post(NotificationMasterClass notification)
        {
            string _IsSaved = "N";
            try
            {
                if (notification == null)
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Payload is empty..!!" });
                }
                SqlConnection _QITcon = new SqlConnection(_QIT_connection);
                _QITcon.Open();

                if (notification.Module == string.Empty)
                {
                    _logger.LogError("Error : Notification Module is required..!!");
                    return BadRequest(new { StatusCode = "403", IsSaved = _IsSaved, StatusMsg = "Notification Module is required..!!" });
                }
                List<int> userIds = new List<int>();

                using (SqlCommand cmd = new SqlCommand("QIT_SearchUserByModule", _QITcon))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.Add(new SqlParameter("@searchedText", SqlDbType.NVarChar, 100) { Value = notification.Module });
                    cmd.Parameters.Add(new SqlParameter("@applicationValue", SqlDbType.NVarChar, 100) { Value = true });

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            int userId = reader.GetInt32(0);
                            userIds.Add(userId);
                        }
                    }
                }

                string getUserIDQuery = "SELECT User_Id FROM QIT_User_Master WHERE User_Name = @UserName";

                using (SqlCommand cmd3 = new SqlCommand(getUserIDQuery, _QITcon))
                {
                    cmd3.Parameters.AddWithValue("@UserName", notification.Sender_User_Name);
                    int Sender_User_ID = (int)cmd3.ExecuteScalar();
                    Console.WriteLine("Sender User Name : " + notification.Sender_User_Name + " Sender User ID : " + Sender_User_ID);
                    string addQuery = "INSERT INTO QIT_Notification_Master (Sender_User_Id, Receiver_User_Id, Notification_Text, N_Date_Time, Chk_Status) OUTPUT INSERTED.N_Id VALUES (@Sender_User_Id, @Receiver_User_Id, @Notification_Text, @N_Date_Time, @Chk_Status)";
                    Console.WriteLine("Sender Notification Users ID  : " + userIds);
                    List<Notification_Get_Class> newEntity_notifications = new List<Notification_Get_Class>();
                    foreach (int user_id in userIds)
                    {
                        using (SqlCommand cmd1 = new SqlCommand(addQuery, _QITcon))
                        {
                            cmd1.Parameters.AddWithValue("@Sender_User_Id", Sender_User_ID);
                            cmd1.Parameters.AddWithValue("@Notification_Text", notification.Notification_Text);
                            cmd1.Parameters.AddWithValue("@N_Date_Time", notification.N_Date_Time);
                            cmd1.Parameters.AddWithValue("@Chk_Status", notification.Chk_Status);
                            cmd1.Parameters.AddWithValue("@Receiver_User_Id", user_id);

                            int nId = (int)cmd1.ExecuteScalar();
                            string getUserQuery = "SELECT User_Name FROM QIT_User_Master WHERE User_Id = @UserId";

                            using (SqlCommand cmd2 = new SqlCommand(getUserQuery, _QITcon))
                            {
                                cmd2.Parameters.AddWithValue("@UserId", user_id);

                                string userName = cmd2.ExecuteScalar() as string;
                                var newEntity = new Notification_Get_Class
                                {
                                    N_Id = nId,
                                    Notification_Text = notification.Notification_Text,
                                    Chk_Status = "0",
                                    timeLimit = GetHumanReadableTimeDifference(notification.N_Date_Time)
                                };
                                newEntity_notifications.Add(newEntity);
                                _logger.LogInformation("Notification Added successfully for userName..", userName);
                                await _hubContext.Clients.Group(userName).SendAsync("newEntryAdded", newEntity_notifications);
                            }

                        }
                    }

                }
                _QITcon.Close();
                _logger.LogInformation("Notification Added successfully.. ");

                return Ok(new { StatusCode = "200", IsSaved = _IsSaved, StatusMsg = "Notification Added successfully.." });
            }
            catch (SqlException ex)
            {
                if (ex.Number == 547)
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Invalid user.Please provide a valid user ID." });
                }
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.Message.ToString() });
            }
            catch (Exception ex)
            {
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.Message.ToString() });
            }
        }

        static string GetHumanReadableTimeDifference(DateTime timestamp)
        {
            DateTime currentTime = DateTime.Now;
            TimeSpan timeDifference = currentTime - timestamp;

            if (timeDifference.TotalSeconds < 60)
            {
                return $"{(int)timeDifference.TotalSeconds} seconds ago";
            }
            else if (timeDifference.TotalMinutes < 60)
            {
                return $"{(int)timeDifference.TotalMinutes} minutes ago";
            }
            else if (timeDifference.TotalHours < 24)
            {
                return $"{(int)timeDifference.TotalHours} hours ago";
            }
            else if (timeDifference.TotalDays < 30) // Approximation for a month
            {
                return $"{(int)timeDifference.TotalDays} days ago";
            }
            else if (timeDifference.TotalDays < 365) // Approximation for a year
            {
                int months = (int)(timeDifference.TotalDays / 30);
                return $"{months} {(months == 1 ? "month" : "months")} ago";
            }
            else
            {
                int years = (int)(timeDifference.TotalDays / 365);
                return $"{years} {(years == 1 ? "year" : "years")} ago";
            }

        }

        [HttpGet("GetALlNotification")]
        public async Task<ActionResult<IEnumerable<testclass>>> GetALlNotification(string userName)
        {
            try
            {
                string query = $"SELECT N_Id, Notification_Text, N_Date_Time, Chk_Status FROM QIT_Notification_Master WHERE  Receiver_User_Id = (select User_ID from QIT_User_Master where User_Name='{userName}')ORDER BY N_Date_Time DESC";
                int unread_Cnt = 0;
                List<Notification_Get_Class> notifications = new List<Notification_Get_Class>();
                using (SqlConnection connection = new SqlConnection(_QIT_connection))
                {
                    connection.Open();

                    using (SqlDataAdapter adapter = new SqlDataAdapter(query, connection))
                    {
                        DataTable dt = new DataTable();
                        adapter.Fill(dt);

                        if (dt.Rows.Count > 0)
                        {
                            notifications = dt.AsEnumerable().Select(item => new Notification_Get_Class
                            {
                                N_Id = item.Field<int>("N_Id"),
                                Notification_Text = item.Field<string>("Notification_Text"),
                                timeLimit = GetHumanReadableTimeDifference(item.Field<DateTime>("N_Date_Time")),
                                Chk_Status = item.Field<string>("Chk_Status")
                            }).ToList();
                            unread_Cnt = notifications.Where(item => item.Chk_Status == "0").Count();
                            List<testclass> data = new List<testclass>();
                            data.Add(new testclass { data = notifications, dataCount = unread_Cnt });

                            return data;
                        }
                    }
                }
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError("Error in NotificationMasterController : GetALlNotification() :: {Error}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.Message.ToString() });
            }
           
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Notification_Get_Class>>> Get(int? id)
        {
            try
            {
                if (id == null)
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "User ID is empty..!!" });
                }

                string _query = @"SELECT N_Id, Notification_Text, N_Date_Time, Chk_Status FROM QIT_Notification_Master WHERE Receiver_User_Id = " + id + " order by N_Date_Time desc";

                List<Notification_Get_Class> notifications = new List<Notification_Get_Class>();
                DataTable dtPeriod = new DataTable();
                int unread_Cnt = 0;
                using (SqlConnection _QITcon = new SqlConnection(_QIT_connection))
                {
                    _QITcon.Open();

                    SqlDataAdapter oAdptr = new SqlDataAdapter(_query, _QITcon); // Corrected variable name
                    oAdptr.Fill(dtPeriod);

                    _QITcon.Close();

                    if (dtPeriod.Rows.Count > 0)
                    {
                        //unread_Cnt = dtPeriod.Rows.Count;
                        notifications = dtPeriod.AsEnumerable().Select(item => new Notification_Get_Class
                        {
                            N_Id = item.Field<int>("N_Id"),
                            Notification_Text = item.Field<string>("Notification_Text"),
                            timeLimit = GetHumanReadableTimeDifference(item.Field<DateTime>("N_Date_Time")),
                            Chk_Status = item.Field<string>("Chk_Status")
                        }).ToList();
                        unread_Cnt = notifications.Where(item => item.Chk_Status == "0").Count();
                    }
                    else
                    {
                        return BadRequest(new { StatusCode = "400", errorMessage = "No data found" });
                    }
                }
                var data = new { n_Data = notifications, UnRead = unread_Cnt };

                return Ok(new { StatusCode = "200", data });
            }
            catch (Exception ex)
            {
                _logger.LogError("Error in NotificationMasterController : Get() :: {Error}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.Message.ToString() });
            }
        }

        [HttpPost("updateNotificationStatus")]
        public async Task<ActionResult> updateNotificationStatus(Notification_Update_Status data)
        {
            string _IsSaved = "N";
            try
            {
                if (data == null)
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "payload is empty..!!" });
                }
                SqlConnection _QITcon = new SqlConnection(_QIT_connection);
                _QITcon.Open();
                string _query = @"update QIT_Notification_Master set Chk_Status=1 where N_Id=@NotificationID;";

                using (SqlCommand updateCmd = new SqlCommand(_query, _QITcon))
                {
                    updateCmd.Parameters.AddWithValue("@NotificationID", data.N_Id);
                    updateCmd.ExecuteNonQuery();
                    int updateCount = updateCmd.ExecuteNonQuery();
                    if (updateCount > 0)
                        _IsSaved = "Y";
                }
                if (_IsSaved == "Y")
                {
                    return Ok(new { StatusCode = "200", IsSaved = _IsSaved, StatusMsg = "Status Updated Successfully..!!" });
                }
                else
                {
                    return Ok(new { StatusCode = "404", IsSaved = _IsSaved, StatusMsg = "Updated unsuccessfully..!!" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Error in NotificationMasterController : Get() :: {Error}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.Message.ToString() });
            }
        }


        [HttpPost("readAllNotificationStatus")]
        public async Task<ActionResult> readAllNotificationStatus(Notification_readAll_Status data)
        {
            string _IsSaved = "N";
            try
            {
                if (data == null)
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "payload is empty..!!" });
                }
                SqlConnection _QITcon = new SqlConnection(_QIT_connection);
                _QITcon.Open();
                string _query = @"update QIT_Notification_Master set Chk_Status=1 where Receiver_User_Id = (select User_ID from QIT_User_Master where User_Name=@UserName)";

                using (SqlCommand updateCmd = new SqlCommand(_query, _QITcon))
                {
                    updateCmd.Parameters.AddWithValue("@UserName", data.Username);
                    int updateCount = updateCmd.ExecuteNonQuery();
                    if (updateCount > 0)
                        _IsSaved = "Y";
                }
                if (_IsSaved == "Y")
                {
                    return Ok(new { StatusCode = "200", IsSaved = _IsSaved, StatusMsg = "Status Updated Successfully..!!" });
                }
                else
                {
                    return Ok(new { StatusCode = "404", IsSaved = _IsSaved, StatusMsg = "Updated unsuccessfully..!!" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Error in NotificationMasterController : Get() :: {Error}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.Message.ToString() });
            }
        }

    }
}
