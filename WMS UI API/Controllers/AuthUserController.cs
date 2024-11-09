using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Data.SqlClient;
using System.Data;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authorization;
using Newtonsoft.Json;
using WMS_UI_API.Models;
using WMS_UI_API.Common;

namespace WMS_UI_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthUserController : ControllerBase
    {
        private string _QIT_connection = string.Empty;
        private string _QIT_DB = string.Empty;
        private string _Query = string.Empty;
        private SqlCommand cmd;
        DataSet ds = new DataSet();
        private readonly ILogger<AuthUserController> _logger;
        public IConfiguration Configuration { get; }
        public AuthUserController(IConfiguration configuration, ILogger<AuthUserController> AuthLogger)
        {
            _logger = AuthLogger;
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
        private string GenerateJWTToken(string username)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Configuration["Jwt:Key"]));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
            new Claim(JwtRegisteredClaimNames.Sub, username),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };


            var token = new JwtSecurityToken(
                  claims: claims,
                  expires: DateTime.Now.AddDays(1), // Set expiration to 1 day from the current time
                  signingCredentials: credentials
              );


            return new JwtSecurityTokenHandler().WriteToken(token);
        }
        private void SetJWTTokenAsCookie(string token)
        {
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = true, // Set to true if your site uses HTTPS
                SameSite = SameSiteMode.Lax,
            };

            Response.Cookies.Append("jwt", token, cookieOptions);
        }

        [HttpGet("get-jwt-cookie")]
        public IActionResult GetJwtCookie()
        {
            bool tokenbool = Request.Cookies.TryGetValue("jwt", out var token);
            if (token != null)
            {
                return Ok(new { JwtToken = token });
            }

            return NotFound("JWT Token cookie not found");
        }

        [HttpPost("LoginPost")]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<User>>> LoginPost([FromBody] User value)
        {
            try
            {
                if (value == null)
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Payload is empty..!!" });
                }
                else
                {
                    System.Data.DataTable dtData = new System.Data.DataTable();
                    SqlConnection con = new SqlConnection(_QIT_connection);
                    int userId = -1;
                    _Query = "SELECT * FROM QIT_User_Master WHERE User_Name = @uname";
                    con.Open();
                    SqlDataAdapter oAdptr = new SqlDataAdapter(_Query, con);
                    oAdptr.SelectCommand.Parameters.AddWithValue("@uname", value.User_Name);
                    oAdptr.Fill(dtData);
                    con.Close();

                    if (dtData.Rows.Count > 0)
                    {
                        bool passwordMatch = BCrypt.Net.BCrypt.Verify(value.User_Password, dtData.Rows[0]["User_Password"].ToString());
                        if (!passwordMatch)
                        {
                            return BadRequest(new { StatusCode = "400", StatusMsg = "Incorrect password..!!" });
                        }
                        List<getModuleClass> obj = new List<getModuleClass>();
                        DataTable dtPeriod = new DataTable();
                        int UID = (int)dtData.Rows[0]["User_ID"];
                        _Query = @"select Authentication_Rule_Details from QIT_Authentication_Rule where User_ID=" + UID;
                        con.Open();
                        SqlDataAdapter oAdptrr = new SqlDataAdapter(_Query, con);
                        oAdptrr.Fill(dtPeriod);
                        con.Close();
                        if (dtPeriod.Rows.Count > 0)
                        {
                            obj = JsonConvert.DeserializeObject<List<getModuleClass>>(dtPeriod.Rows[0]["Authentication_Rule_Details"].ToString());

                        }

                        DataTable dtPeriod2 = new DataTable();
                        using (SqlDataAdapter warehouseDataAdapter = new SqlDataAdapter("SELECT Warehouse_Code, Warehouse_Name FROM QIT_WarehouseRule_Master WHERE (',' +(SELECT REPLACE(REPLACE(User_details, '[', ''), ']', '') + ',') + ',') LIKE '%,@userId,%'", con))
                        {
                            con.Open();
                            warehouseDataAdapter.Fill(dtPeriod2);
                            con.Close();
                        }


                        List<GetWarehouseForUser> obj2 = dtPeriod2.AsEnumerable()
                            .Select(row => new GetWarehouseForUser
                            {
                                Warehouse_Code = row.Field<string>("Warehouse_Code"),
                                Warehouse_Name = row.Field<string>("Warehouse_Name")
                            })
                            .ToList();

                        string token_jwt = GenerateJWTToken(dtData.Rows[0]["User_Name"].ToString());
                        SetJWTTokenAsCookie(token_jwt);
                        _logger.LogError("User Name : " + dtData.Rows[0]["User_Name"].ToString());
                        return Ok(new { 
                            StatusCode = "200", 
                            Token = token_jwt,
                            UserName = dtData.Rows[0]["User_Name"].ToString(), 
                            IsVendor = dtData.Rows[0]["isVendor"].ToString(),
                            CardCode = dtData.Rows[0]["CardCode"].ToString(),
                            Authentication_Rule = obj,
                            WareHouse_Rule = obj2 });
                    }
                    else
                    {
                        return BadRequest(new { StatusCode = "400", StatusMsg = "Details not found" });
                    }

                }
            }
            catch (Exception ex)
            {
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.Message.ToString() });
            }
        }

        [HttpGet("validate_jwt")]
        [Authorize]
        public async Task<ActionResult> validate_jwt()
        {
            try
            {
                return Ok(new { StatusCode = "200", Message = "valid token" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.Message.ToString() });
            }
        }

        [HttpGet]
        [Authorize]
        public async Task<ActionResult<IEnumerable<User>>> Get(string typeUser)
        {
            try
            {
                List<User> obj = new List<User>();
                DataTable dtPeriod = new DataTable();
                SqlConnection con = new SqlConnection(_QIT_connection);
                if (typeUser == "A")
                {
                    _Query = @" select * from QIT_User_Master";
                }
                else if (typeUser == "S")
                {
                    _Query = @" select * from QIT_User_Master where isVendor='Y'";
                }
                else
                {
                    _Query = @" select * from QIT_User_Master where isVendor='N'";
                }
                con.Open();
                SqlDataAdapter oAdptr = new SqlDataAdapter(_Query, con);
                oAdptr.Fill(dtPeriod);
                con.Close();

                dynamic arData = JsonConvert.SerializeObject(dtPeriod);
                obj = JsonConvert.DeserializeObject<List<User>>(arData.ToString());
                return obj;
            }
            catch (Exception ex)
            {
                _logger.LogError("Error in AuthUserController : Get() :: {Error}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.Message.ToString() });
            }
        }


        [HttpPost("SaveAuthRule")]
        [Authorize]
        public async Task<ActionResult<getDataClass>> SaveAuthRule([FromBody] getDataClass value)
        {
            string _IsSaved = "N";
            try
            {
                getDataClass newObj = new getDataClass();
                SqlConnection _QITcon = new SqlConnection(_QIT_connection);
                _QITcon.Open();
                newObj = value;
                dynamic arData = JsonConvert.SerializeObject(value.moduleCLasses);

                string mergeQuery = @"MERGE INTO QIT_Authentication_Rule AS Target
USING (SELECT @User_ID AS User_ID, @Authentication_Rule_Details AS Authentication_Rule_Details) AS Source
ON Target.User_ID = Source.User_ID
WHEN MATCHED THEN
    UPDATE SET Authentication_Rule_Details = Source.Authentication_Rule_Details
WHEN NOT MATCHED THEN
    INSERT (User_ID, Authentication_Rule_Details)
    VALUES (Source.User_ID, Source.Authentication_Rule_Details);";

                using (SqlCommand cmd = new SqlCommand(mergeQuery, _QITcon))
                {
                    cmd.Parameters.AddWithValue("@User_ID", value.User_ID);
                    cmd.Parameters.AddWithValue("@Authentication_Rule_Details", arData);

                    int insertCount = cmd.ExecuteNonQuery();
                    if (insertCount > 0)
                        _IsSaved = "Y";
                }


                //cmd = new SqlCommand("insert into QIT_Authentication_Rule(User_ID,Authentication_Rule_Details) values(" + value.User_ID + ",'" + arData + "')", _QITcon);
                //int insertCount = cmd.ExecuteNonQuery();
                //if (insertCount > 0)
                //    _IsSaved = "Y";

                _QITcon.Close();
                return Ok(new { StatusCode = "200", IsSaved = _IsSaved, StatusMsg = "Saved Successfully!!!" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = ex.Message.ToString() });
            }
        }

        [HttpPost("Save")]
        public IActionResult SaveUser([FromBody] User payload)
        {
            string _IsSaved = "N";
            try
            {
                _logger.LogInformation(" Calling AuthUserController : SaveUser() ");

                string p_ErrorMsg = string.Empty;

                if (payload != null)
                {
                    if (payload.isVendor.ToString() == "Y" && payload.CardCode.ToString() == "")
                        return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "CardCode not found" });

                    string hashedPassword = BCrypt.Net.BCrypt.HashPassword(payload.User_Password);
                    SqlConnection con = new SqlConnection(_QIT_connection);
                    _Query = @"insert into QIT_User_Master(User_Name, User_Email, User_Password, Mobile_No,Gender,Department,isVendor,CardCode) OUTPUT INSERTED.User_ID 
                           VALUES (@User_Name, @User_Email, @User_Password, @Mobile_No,@Gender, @Department, @isVendor, @CardCode)";

                    cmd = new SqlCommand(_Query, con);
                    cmd.Parameters.AddWithValue("@User_Name", payload.User_Name);
                    cmd.Parameters.AddWithValue("@User_Email", payload.User_Email);
                    cmd.Parameters.AddWithValue("@User_Password", hashedPassword);
                    cmd.Parameters.AddWithValue("@Mobile_No", payload.Mobile_No.ToString());
                    cmd.Parameters.AddWithValue("@Gender", payload.Gender);
                    cmd.Parameters.AddWithValue("@Department", payload.Department);
                    cmd.Parameters.AddWithValue("@isVendor", payload.isVendor);
                    cmd.Parameters.AddWithValue("@CardCode", payload.CardCode);

                    con.Open();
                    //int intNum = cmd.ExecuteNonQuery();
                    int userId = (int)cmd.ExecuteScalar();
                    con.Close();

                    if (userId > 0)
                        _IsSaved = "Y";
                    else
                        _IsSaved = "N";

                    return Ok(new { StatusCode = "200", IsSaved = _IsSaved, User_ID = userId, StatusMsg = "Saved Successfully!!!" });
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = " Details not found" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Error in AuthUserController : SaveUser() :: {Error}", ex.ToString());
                if (ex.Message.ToString().Contains("UQ_User_Name"))
                {
                    return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "User Name already exist" });
                }
                return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = ex.Message.ToString() });
            }
        }
        [HttpPost("SaveWarehouseRule")]
        [Authorize]
        public IActionResult SaveWarehouseRule([FromBody] WarehouseRule payload)
        {
            string _IsSaved = "N";
            try
            {
                _logger.LogInformation("Calling AuthUserController: SaveUser()");

                if (payload != null)
                {
                    string connectionString = _QIT_connection;
                    using (SqlConnection con = new SqlConnection(connectionString))
                    {
                        con.Open();

                        // Check if a record with the given Warehouse_Code exists
                        string checkIfExistsQuery = "SELECT COUNT(*) FROM QIT_WarehouseRule_Master WHERE Warehouse_Code = @Warehouse_Code";
                        SqlCommand checkIfExistsCmd = new SqlCommand(checkIfExistsQuery, con);
                        checkIfExistsCmd.Parameters.AddWithValue("@Warehouse_Code", payload.Warehouse_Code);
                        int count = (int)checkIfExistsCmd.ExecuteScalar();

                        if (count > 0)
                        {
                            // If the record exists, update it
                            string updateQuery = "UPDATE QIT_WarehouseRule_Master " +
                                                 "SET Warehouse_Name = @Warehouse_Name, Warehouse_Location = @Warehouse_Location, " +
                                                 "Warehouse_binActivat = @Warehouse_binActivat, User_details = @User_details " +
                                                 "WHERE Warehouse_Code = @Warehouse_Code";
                            SqlCommand updateCmd = new SqlCommand(updateQuery, con);
                            // Set parameters for the update
                            updateCmd.Parameters.AddWithValue("@Warehouse_Name", payload.Warehouse_Name);
                            updateCmd.Parameters.AddWithValue("@Warehouse_Location", payload.Warehouse_Location);
                            updateCmd.Parameters.AddWithValue("@Warehouse_binActivat", payload.Warehouse_binActivat);
                            updateCmd.Parameters.AddWithValue("@User_details", JsonConvert.SerializeObject(payload.User_Details));
                            updateCmd.Parameters.AddWithValue("@Warehouse_Code", payload.Warehouse_Code);
                            int updateCount = updateCmd.ExecuteNonQuery();
                            if (updateCount > 0)
                                _IsSaved = "Y";
                        }
                        else
                        {
                            // If the record doesn't exist, insert it
                            string insertQuery = "INSERT INTO QIT_WarehouseRule_Master (Warehouse_Code, Warehouse_Name, Warehouse_Location, Warehouse_binActivat, User_details) " +
                                                 "VALUES (@Warehouse_Code, @Warehouse_Name, @Warehouse_Location, @Warehouse_binActivat, @User_details)";
                            SqlCommand insertCmd = new SqlCommand(insertQuery, con);
                            // Set parameters for the insert
                            insertCmd.Parameters.AddWithValue("@Warehouse_Code", payload.Warehouse_Code);
                            insertCmd.Parameters.AddWithValue("@Warehouse_Name", payload.Warehouse_Name);
                            insertCmd.Parameters.AddWithValue("@Warehouse_Location", payload.Warehouse_Location);
                            insertCmd.Parameters.AddWithValue("@Warehouse_binActivat", payload.Warehouse_binActivat);
                            insertCmd.Parameters.AddWithValue("@User_details", JsonConvert.SerializeObject(payload.User_Details));
                            int insertCount = insertCmd.ExecuteNonQuery();
                            if (insertCount > 0)
                                _IsSaved = "Y";
                        }

                        con.Close();

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
                _logger.LogError("Error in AuthUserController : SaveUser() :: {Error}", ex.ToString());
                if (ex.Message.ToString().Contains("UQ_User_Name"))
                {
                    return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "User Name already exists" });
                }
                return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = ex.Message.ToString() });
            }
        }

        [HttpPost("GetWarehouseRule")]
        [Authorize]
        public async Task<ActionResult<IEnumerable<UserBindWithWarehouse>>> GetWarehouseRule([FromBody] GetWarehouseRule payload)
        {
            try
            {
                if (payload.Warehouse_Code == null || payload.Warehouse_Code == "")
                {
                    return BadRequest(new { StatusCode = 400, StatusMsg = "Warehouse code is required" });
                }
                List<UserBindWithWarehouse> obj = new List<UserBindWithWarehouse>();
                DataTable dtW = new DataTable();
                DataTable dtU = new DataTable();
                SqlConnection con = new SqlConnection(_QIT_connection);
                _Query = @"select Warehouse_Code,User_Details from QIT_WarehouseRule_Master where Warehouse_Code=@whsCode";
                con.Open();
                using (SqlCommand cmd = new SqlCommand(_Query, con))
                {
                    cmd.Parameters.AddWithValue("@whsCode", payload.Warehouse_Code); // Add the whsCode parameter

                    SqlDataAdapter oAdptrW = new SqlDataAdapter(cmd);
                    oAdptrW.Fill(dtW);
                    con.Close();

                }
                WarehouseRule rule = new WarehouseRule();
                if (dtW.Rows.Count > 0)
                { 
                    rule.User_Details = JsonConvert.DeserializeObject<List<int>>(dtW.Rows[0]["User_Details"].ToString());
                }
                _Query = @"select * from QIT_User_Master";
                con.Open();
                SqlDataAdapter oAdptr = new SqlDataAdapter(_Query, con);
                oAdptr.Fill(dtU);
                con.Close();

                dynamic arData = JsonConvert.SerializeObject(dtU);
                obj = JsonConvert.DeserializeObject<List<UserBindWithWarehouse>>(arData.ToString());


                if (dtU.Rows.Count > 0)
                {
                    if (dtW.Rows.Count > 0)
                    {
                        foreach (var user in obj)
                        {
                            user.IsBind = rule.User_Details.Contains(user.User_ID);
                        }
                    }
                    return Ok(obj);
                }
                else
                {
                    return BadRequest(new { StatusCode = 400, StatusMsg = "No data found" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Error in AuthUserController : GetWarehouseRule() :: {Error}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.Message.ToString() });
            }
        }

        [HttpGet("GetWarehousebyUser")]
        [Authorize]
        public async Task<ActionResult<IEnumerable<WarehouseRule>>> GetWarehousebyUser()
        {
            try
            {
                string UserId = String.Empty;

                if (HttpContext.Request.Headers.TryGetValue("UserId", out var headerValue))
                {
                    UserId = headerValue.ToString();
                }

                if (UserId == null || string.IsNullOrEmpty(UserId))
                {
                    return BadRequest(new { StatusCode = 400, StatusMsg = "UserId is required" });
                }
                if (UserId.Length <= 0) // file should not be blank
                {
                    return BadRequest(new { StatusCode = 400, StatusMsg = "UserId is required" });
                }
               
                List<WarehouseRule> obj = new List<WarehouseRule>();
                DataTable dtW = new DataTable();
                //DataTable dtU = new DataTable();
                SqlConnection con = new SqlConnection(_QIT_connection);
                _Query = @"SELECT Warehouse_Code, Warehouse_Name FROM QIT_WarehouseRule_Master WHERE (',' +(SELECT REPLACE(REPLACE(User_details, '[', ''), ']', '') + ',') + ',') LIKE '%,@userId,%'";
                con.Open();
                using (SqlCommand cmd = new SqlCommand(_Query, con))
                {
                    cmd.Parameters.AddWithValue("@userId", "%" + UserId + "%"); // Add the whsCode parameter

                    SqlDataAdapter oAdptrW = new SqlDataAdapter(cmd);
                    oAdptrW.Fill(dtW);
                    con.Close();
                }
                
                //WarehouseRule rule = new WarehouseRule();
                //if (dtW.Rows.Count > 0)
                //{
                //    rule.User_Details = JsonConvert.DeserializeObject<List<int>>(dtW.Rows[0]["User_Details"].ToString());
                //}
                //_Query = @"select * from QIT_User_Master";
                //con.Open();
                //SqlDataAdapter oAdptr = new SqlDataAdapter(_Query, con);
                //oAdptr.Fill(dtU);
                //con.Close();

                dynamic arData = JsonConvert.SerializeObject(dtW);
                obj = JsonConvert.DeserializeObject<List<WarehouseRule>>(arData.ToString());

                if (dtW.Rows.Count > 0)
                {
                    return Ok(obj);
                    //if (dtW.Rows.Count > 0)
                    //{
                    //    foreach (var user in obj)
                    //    {
                    //        user.IsBind = rule.User_Details.Contains(user.User_ID);
                    //    }
                    //}
                    //return Ok(obj);
                }
                else
                {
                    return BadRequest(new { StatusCode = 400, StatusMsg = "No data found" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Error in AuthUserController : GetWarehouseRule() :: {Error}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.Message.ToString() });
            }
        }


        [HttpPost("GetAuthRule")]
        [Authorize]
        public async Task<ActionResult<IEnumerable<User>>> GetAuthRule([FromBody] getUserAuthRule user)
        {
            try
            {
                if (user == null)
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "Payload is empty..!!" });
                }
                else
                {
                    System.Data.DataTable dtData = new System.Data.DataTable();
                    SqlConnection con = new SqlConnection(_QIT_connection);
                    int userId = -1;
                    List<getModuleClass> obj = new List<getModuleClass>();
                    DataTable dtPeriod = new DataTable();
                    int UID = user.User_ID;
                    _Query = @"select Authentication_Rule_Details from QIT_Authentication_Rule where User_ID=" + UID;
                    con.Open();
                    SqlDataAdapter oAdptrr = new SqlDataAdapter(_Query, con);
                    oAdptrr.Fill(dtPeriod);
                    con.Close();
                    if (dtPeriod.Rows.Count > 0)
                    {
                        obj = JsonConvert.DeserializeObject<List<getModuleClass>>(dtPeriod.Rows[0]["Authentication_Rule_Details"].ToString());

                        return Ok(new { StatusCode = "200", Authentication_Rule = obj });
                    }
                    else
                    {
                        return BadRequest(new { StatusCode = "400", errorMessage = "No data found" });

                    }


                }
            }
            catch (Exception ex)
            {
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.Message.ToString() });
            }
        }

    }
}
