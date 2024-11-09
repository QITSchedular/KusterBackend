using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Data;
using System.Data.SqlClient;
using WMS_UI_API.Common;
using WMS_UI_API.Models;

namespace WMS_UI_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ConfigsController : ControllerBase
    {
        private string _ApplicationApiKey = string.Empty;
        private string _connection = string.Empty;
        private string _QIT_connection = string.Empty; 
        private string _Query = string.Empty;
        private SqlCommand cmd;

        public IConfiguration Configuration { get; }
        private readonly ILogger<ConfigsController> _logger;


        public ConfigsController(IConfiguration configuration, ILogger<ConfigsController> logger)
        {
            _logger = logger;
            try
            {
                Configuration = configuration;
                _ApplicationApiKey = Configuration["connectApp:ServiceApiKey"];
                _connection = Configuration["connectApp:ConnString"];
                _QIT_connection = Configuration["connectApp:QITConnString"];
                
                Global.QIT_DB = Configuration["QITDB"];
                Global.SAP_DB = Configuration["CompanyDB"];
            }
            catch (Exception ex)
            {
                _logger.LogError(" Error in ConfigsController :: {Error}" + ex.ToString());
            }
        }


        [HttpPost("Save")]
        public IActionResult SaveConfig([FromBody] SaveConfig payload)
        {
            string _IsSaved = "N";
            SqlConnection QITcon;
            SqlDataAdapter oAdptr;
            try
            {
                _logger.LogInformation(" Calling ConfigsController : SaveConfig() ");

                if (payload != null)
                {
                    #region Check for Attachment Path
                    if (payload.AtcPath.ToString().ToLower().Trim() == "string")
                        return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Provide Attachment Path" });
                    if (payload.AtcPath.ToString().Trim() == String.Empty)
                        return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Provide Attachment Path" });
                    #endregion

                    #region Check for Generation Method
                    if (payload.GenMethod.ToString().ToLower().Trim() == "string")
                        return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Provide Generation Method" });
                    if (payload.GenMethod.ToString().Trim() == String.Empty)
                        return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Provide Generation Method" });
                    if (payload.GenMethod.ToString().ToUpper() != "A" && payload.GenMethod.ToString().ToUpper() != "M")
                        return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "GenMethod Values : A:Auto / M:Manual " });
                    #endregion

                    #region Check for QR Manage By
                    if (payload.QRMngBy.ToString().ToLower().Trim() == "string")
                        return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Provide QR Manage By" });
                    if (payload.QRMngBy.ToString().Trim() == String.Empty)
                        return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Provide QR Manage By" });
                    if (payload.QRMngBy.ToString().ToUpper() != "N" && payload.QRMngBy.ToString().ToUpper() != "B" && payload.QRMngBy.ToString().ToUpper() != "S")
                        return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "QR Manage By Values : N:None / B:Batch / S:Serial Numbers " });
                    #endregion

                    #region Check for QR Generation Method
                    if (payload.QRGenMethod.ToString().ToLower().Trim() == "string")
                        return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Provide QR Generation Method" });
                    if (payload.QRGenMethod.ToString().Trim() == String.Empty)
                        return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Provide QR Generation Method" });
                    if (payload.QRGenMethod.ToString().ToUpper() != "T" && payload.QRGenMethod.ToString().ToUpper() != "M")
                        return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "QRGenMethod Values : T:Transaction Wise QR / M:Master Wise QR " });
                    #endregion

                    #region Check for Remark
                    if (payload.Remark.ToString().ToLower().Trim() == "string")
                        return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Provide Remark Value" });
                    if (payload.Remark.ToString().Trim() == String.Empty)
                        return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Provide Remark Value" });
                    if (payload.Remark.ToString().ToUpper() != "Y" && payload.Remark.ToString().ToUpper() != "N")
                        return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Remark Values : Y:Yes / N:No " });
                    #endregion

                    #region Check for QC Required
                    if (payload.QCRequired.ToString().ToLower().Trim() == "string")
                        return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Provide QC Required or not" });
                    if (payload.QCRequired.ToString().Trim() == String.Empty)
                        return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Provide QC Required or not" });
                    if (payload.QCRequired.ToString().ToUpper() != "N" && payload.QCRequired.ToString().ToUpper() != "Y")
                        return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "QC Required By Values : Y:Yes / N:No" });
                    #endregion

                    #region Check for Issue Series
                    if (payload.IssueSeriesName.ToString().ToLower().Trim() == "string")
                        return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Provide Issue Series" });
                    if (payload.IssueSeriesName.ToString().Trim() == String.Empty)
                        return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Provide Issue Series" });

                    #region Check Issue Series Existence

                    QITcon = new SqlConnection(_QIT_connection);
                    _Query = @" 
                    SELECT COUNT(*) FROM " + Global.SAP_DB + @".dbo.NNM1 
                    WHERE SeriesName = @sName and Series = @series and ObjectCode = '60' and Locked = 'N' ";
                    _logger.LogInformation(" ConfigsController : Issue Series Existence Query : {q} ", _Query.ToString());

                    cmd = new SqlCommand(_Query, QITcon);
                    cmd.Parameters.AddWithValue("@sName", payload.IssueSeriesName);
                    cmd.Parameters.AddWithValue("@series", payload.IssueSeries);
                    QITcon.Open();
                    Object Value = cmd.ExecuteScalar();
                    QITcon.Close();
                    if (Int32.Parse(Value.ToString()) == 1)
                    { }
                    else
                    {
                        return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Issue Series does not exist" });
                    }  
                    #endregion

                    #endregion

                    #region Check for Receipt Series
                    Value = 0;
                    if (payload.ReceiptSeriesName.ToString().ToLower().Trim() == "string")
                        return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Provide Receipt Series" });
                    if (payload.ReceiptSeriesName.ToString().Trim() == String.Empty)
                        return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Provide Receipt Series" });

                    #region Check Receipt Series Existence
                    QITcon = new SqlConnection(_QIT_connection);

                    _Query = @" 
                    SELECT COUNT(*) FROM " + Global.SAP_DB + @".dbo.NNM1 
                    WHERE SeriesName = @sName and Series = @series and ObjectCode = '59' and Locked = 'N' ";
                    _logger.LogInformation(" ConfigsController : Receipt Series Existence Query : {q} ", _Query.ToString());

                    cmd = new SqlCommand(_Query, QITcon);
                    cmd.Parameters.AddWithValue("@sName", payload.ReceiptSeriesName);
                    cmd.Parameters.AddWithValue("@series", payload.ReceiptSeries);
                    QITcon.Open();
                    Value = cmd.ExecuteScalar();
                    QITcon.Close();
                    if (Int32.Parse(Value.ToString()) == 1)
                    { }
                    else
                    {
                        return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Receipt Series does not exist" });
                    }

                    #endregion

                    #endregion

                    #region Check for Production Rework
                    if (payload.IsProRework.ToString().ToLower().Trim() == "string")
                        return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Provide IsProRework Value" });
                    if (payload.IsProRework.ToString().Trim() == String.Empty)
                        return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Provide IsProRework Value" });
                    if (payload.IsProRework.ToString().ToUpper() != "Y" && payload.IsProRework.ToString().ToUpper() != "N")
                        return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "IsProRework Values : Y:Yes / N:No " });
                    #endregion

                    #region Check for Delivery Series
                    Value = 0;
                    if (payload.DeliverySeriesName.ToString().ToLower().Trim() == "string")
                        return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Provide Delivery Series" });
                    if (payload.DeliverySeriesName.ToString().Trim() == String.Empty)
                        return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Provide Delivery Series" });

                    #region Check Delivery Series Existence
                    QITcon = new SqlConnection(_QIT_connection);

                    _Query = @" 
                    SELECT count(*) from " + Global.SAP_DB + @".dbo.NNM1 
                    WHERE SeriesName = @sName and Series = @series and ObjectCode = '15' and Locked = 'N' ";
                    _logger.LogInformation(" ConfigsController : Delivery Series Existence Query : {q} ", _Query.ToString());

                    cmd = new SqlCommand(_Query, QITcon);
                    cmd.Parameters.AddWithValue("@sName", payload.DeliverySeriesName);
                    cmd.Parameters.AddWithValue("@series", payload.DeliverySeries);
                    QITcon.Open();
                    Value = cmd.ExecuteScalar();
                    QITcon.Close();
                    if (Int32.Parse(Value.ToString()) == 1)
                    { }
                    else
                    {
                        return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Delivery Series does not exist" });
                    }
                    #endregion

                    #endregion

                    #region Check for GRPO Series
                    Value = 0;
                    if (payload.GRPOSeriesName.ToString().ToLower().Trim() == "string")
                        return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Provide GRPO Series" });
                    if (payload.GRPOSeriesName.ToString().Trim() == String.Empty)
                        return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Provide GRPO Series" });

                    #region Check GRPO Series Existence
                    QITcon = new SqlConnection(_QIT_connection);

                    _Query = @" 
                    SELECT count(*) FROM " + Global.SAP_DB + @".dbo.NNM1  
                    WHERE SeriesName = @sName and Series = @series and ObjectCode = '20' and Locked = 'N' ";
                    _logger.LogInformation(" ConfigsController : GRPO Series Existence Query : {q} ", _Query.ToString());

                    cmd = new SqlCommand(_Query, QITcon);
                    cmd.Parameters.AddWithValue("@sName", payload.GRPOSeriesName);
                    cmd.Parameters.AddWithValue("@series", payload.GRPOSeries);
                    QITcon.Open();
                    Value = cmd.ExecuteScalar();
                    QITcon.Close();
                    if (Int32.Parse(Value.ToString()) == 1)
                    { }
                    else
                    {
                        return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "GRPO Series does not exist" });
                    }

                    #endregion

                    #endregion

                    #region Check for IT Series
                    Value = 0;
                    if (payload.ITSeriesName.ToString().ToLower().Trim() == "string")
                        return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Provide IT Series" });
                    if (payload.ITSeriesName.ToString().Trim() == String.Empty)
                        return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Provide IT Series" });

                    #region Check IT Series Existence
                    QITcon = new SqlConnection(_QIT_connection);

                    _Query = @" 
                    SELECT COUNT(*) FROM " + Global.SAP_DB + @".dbo.NNM1  
                    WHERE SeriesName = @sName and Series = @series and ObjectCode = '67' and Locked = 'N' ";
                    _logger.LogInformation(" ConfigsController : IT Series Existence Query : {q} ", _Query.ToString());

                    cmd = new SqlCommand(_Query, QITcon);
                    cmd.Parameters.AddWithValue("@sName", payload.ITSeriesName);
                    cmd.Parameters.AddWithValue("@series", payload.ITSeries);
                    QITcon.Open();
                    Value = cmd.ExecuteScalar();
                    QITcon.Close();
                    if (Int32.Parse(Value.ToString()) == 1)
                    { }
                    else
                    {
                        return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "IT Series does not exist" });
                    }
                    #endregion

                    #endregion

                    #region Check for QC IT Series
                    Value = 0;
                    if (payload.QCITSeriesName.ToString().ToLower().Trim() == "string")
                        return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Provide QC IT Series" });
                    if (payload.QCITSeriesName.ToString().Trim() == String.Empty)
                        return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Provide QC IT Series" });

                    #region Check QC IT Series Existence
                    QITcon = new SqlConnection(_QIT_connection);

                    _Query = @" 
                    SELECT count(*) from " + Global.SAP_DB + @".dbo.NNM1  
                    WHERE SeriesName = @sName and Series = @series and ObjectCode = '67' and Locked = 'N' ";
                    _logger.LogInformation(" ConfigsController : QC IT Series Existence Query : {q} ", _Query.ToString());

                    cmd = new SqlCommand(_Query, QITcon);
                    cmd.Parameters.AddWithValue("@sName", payload.QCITSeriesName);
                    cmd.Parameters.AddWithValue("@series", payload.QCITSeries);
                    QITcon.Open();
                    Value = cmd.ExecuteScalar();
                    QITcon.Close();
                    if (Int32.Parse(Value.ToString()) == 1)
                    { }
                    else
                    {
                        return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "QC IT Series does not exist" });
                    }
                    #endregion

                    #endregion

                    #region Check for Incoming QC Warehouse
                    if (payload.QCRequired.ToString().ToLower().Trim() == "y")
                    {
                        if (payload.IncomingQCWhs.ToString().Trim().Length <= 0 || payload.IncomingQCWhs.ToString().ToLower().Trim() == "string")
                            return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Provide Incoming QC Warehouse" });

                        #region Check Incoming QC Existence
                        DataTable dtWhs = new DataTable();
                        QITcon = new SqlConnection(_QIT_connection);

                        _Query = @" 
                        SELECT WhsCode, WhsName, BinActivat 
                        FROM " + Global.SAP_DB + @".dbo.OWHS where WhsCode = @inComingQCWhs and Locked = 'N' ";
                        _logger.LogInformation(" ConfigsController : Incoming QC Warehouse Existence Query : {q} ", _Query.ToString());

                        oAdptr = new SqlDataAdapter(_Query, QITcon);
                        oAdptr.SelectCommand.Parameters.AddWithValue("@inComingQCWhs", payload.IncomingQCWhs);
                        oAdptr.Fill(dtWhs);
                        QITcon.Close();

                        if (dtWhs.Rows.Count > 0)
                        {
                            if (dtWhs.Rows[0]["BinActivat"].ToString().ToLower() == "y" && payload.IncomingQCBinAbsEntry <= 0)
                            {
                                return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Provide Bin Location for Incoming QC Warehouse" });
                            }
                            else if (dtWhs.Rows[0]["BinActivat"].ToString().ToLower() == "y")
                            {
                                #region Check for Valid Bin Location
                                Value = 0;
                                QITcon = new SqlConnection(_QIT_connection);

                                _Query = @" 
                                SELECT COUNT(*) from " + Global.SAP_DB + @".dbo.OBIN 
                                WHERE WhsCode = @inComingQCWhs and AbsEntry = @absEntry ";
                                _logger.LogInformation(" ConfigsController : Incoming QC Bin Location Existence Query : {q} ", _Query.ToString());

                                cmd = new SqlCommand(_Query, QITcon);
                                cmd.Parameters.AddWithValue("@inComingQCWhs", payload.IncomingQCWhs);
                                cmd.Parameters.AddWithValue("@absEntry", payload.IncomingQCBinAbsEntry);
                                QITcon.Open();
                                Value = cmd.ExecuteScalar();
                                QITcon.Close();
                                if (Int32.Parse(Value.ToString()) == 1)
                                { }
                                else
                                {
                                    return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Incoming QC Bin Location does not exist" });
                                }
                                # endregion
                            }
                        }
                        else
                        {
                            return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Incoming QC Warehouse does not exist" });
                        }

                        #endregion
                    }


                    #endregion

                    #region Check for InProcess QC Warehouse
                    if (payload.QCRequired.ToString().ToLower().Trim() == "y")
                    {
                        if (payload.InProcessQCWhs.ToString().Trim().Length <= 0 || payload.InProcessQCWhs.ToString().ToLower().Trim() == "string")
                            return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Provide InProcess QC Warehouse" });

                        #region Check InProcess QC Existence
                        DataTable dtWhs = new DataTable();
                        QITcon = new SqlConnection(_QIT_connection);

                        _Query = @" 
                        SELECT WhsCode, WhsName, BinActivat 
                        FROM " + Global.SAP_DB + @".dbo.OWHS where WhsCode = @inProcessQCWhs and Locked = 'N' ";
                        _logger.LogInformation(" ConfigsController : InProcess QC Warehouse Existence Query : {q} ", _Query.ToString());

                        oAdptr = new SqlDataAdapter(_Query, QITcon);
                        oAdptr.SelectCommand.Parameters.AddWithValue("@inProcessQCWhs", payload.InProcessQCWhs);
                        oAdptr.Fill(dtWhs);
                        QITcon.Close();

                        if (dtWhs.Rows.Count > 0)
                        {
                            if (dtWhs.Rows[0]["BinActivat"].ToString().ToLower() == "y" && payload.InProcessQCBinAbsEntry <= 0)
                            {
                                return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Provide Bin Location for InProcess QC Warehouse" });
                            }
                            else if (dtWhs.Rows[0]["BinActivat"].ToString().ToLower() == "y")
                            {
                                #region Check for Valid Bin Location
                                Value = 0;
                                QITcon = new SqlConnection(_QIT_connection);

                                _Query = @" 
                                SELECT COUNT(*) FROM " + Global.SAP_DB + @".dbo.OBIN 
                                WHERE WhsCode = @inProcessQCWhs and AbsEntry = @absEntry ";
                                _logger.LogInformation(" ConfigsController : InProcess QC Bin Location Existence Query : {q} ", _Query.ToString());

                                cmd = new SqlCommand(_Query, QITcon);
                                cmd.Parameters.AddWithValue("@inProcessQCWhs", payload.InProcessQCWhs);
                                cmd.Parameters.AddWithValue("@absEntry", payload.InProcessQCBinAbsEntry);
                                QITcon.Open();
                                Value = cmd.ExecuteScalar();
                                QITcon.Close();
                                if (Int32.Parse(Value.ToString()) == 1)
                                { }
                                else
                                {
                                    return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "InProcess QC Bin Location does not exist" });
                                }
                                # endregion
                            }
                        }
                        else
                        {
                            return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "InProcess QC Warehouse does not exist" });
                        }

                        #endregion
                    }


                    #endregion

                    #region Check for Approve QC Warehouse
                    if (payload.QCRequired.ToString().ToLower().Trim() == "y")
                    {
                        if (payload.ApprovedWhs.ToString().Trim().Length <= 0 || payload.ApprovedWhs.ToString().ToLower().Trim() == "string")
                            return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Provide Approved QC Warehouse" });

                        #region Check Approve QC Existence
                        DataTable dtWhs = new DataTable();
                        QITcon = new SqlConnection(_QIT_connection);

                        _Query = @" 
                        SELECT WhsCode, WhsName, BinActivat 
                        FROM " + Global.SAP_DB + @".dbo.OWHS where WhsCode = @approveQCWhs and Locked = 'N' ";
                        _logger.LogInformation(" ConfigsController : Approve QC Warehouse Existence Query : {q} ", _Query.ToString());

                        oAdptr = new SqlDataAdapter(_Query, QITcon);
                        oAdptr.SelectCommand.Parameters.AddWithValue("@approveQCWhs", payload.ApprovedWhs);
                        oAdptr.Fill(dtWhs);
                        QITcon.Close();

                        if (dtWhs.Rows.Count > 0)
                        {
                            if (dtWhs.Rows[0]["BinActivat"].ToString().ToLower() == "y" && payload.ApproveQCBinAbsEntry <= 0)
                            {
                                return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Provide Bin Location for Approve QC Warehouse" });
                            }
                            else if (dtWhs.Rows[0]["BinActivat"].ToString().ToLower() == "y")
                            {
                                #region Check for Valid Bin Location
                                Value = 0;
                                QITcon = new SqlConnection(_QIT_connection);

                                _Query = @" 
                                SELECT COUNT(*) FROM " + Global.SAP_DB + @".dbo.OBIN 
                                WHERE WhsCode = @approveQCWhs and AbsEntry = @absEntry ";
                                _logger.LogInformation(" ConfigsController : Approve QC Bin Location Existence Query : {q} ", _Query.ToString());

                                cmd = new SqlCommand(_Query, QITcon);
                                cmd.Parameters.AddWithValue("@approveQCWhs", payload.ApprovedWhs);
                                cmd.Parameters.AddWithValue("@absEntry", payload.ApproveQCBinAbsEntry);
                                QITcon.Open();
                                Value = cmd.ExecuteScalar();
                                QITcon.Close();
                                if (Int32.Parse(Value.ToString()) == 1)
                                { }
                                else
                                {
                                    return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Approve QC Bin Location does not exist" });
                                }
                                # endregion
                            }
                        }
                        else
                        {
                            return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Approve QC Warehouse does not exist" });
                        }

                        #endregion
                    }


                    #endregion

                    #region Check for Reject QC Warehouse
                    if (payload.QCRequired.ToString().ToLower().Trim() == "y")
                    {
                        if (payload.RejectedWhs.ToString().Trim().Length <= 0 || payload.RejectedWhs.ToString().ToLower().Trim() == "string")
                            return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Provide Reject QC Warehouse" });

                        #region Check Reject QC Existence
                        DataTable dtWhs = new DataTable();
                        QITcon = new SqlConnection(_QIT_connection);

                        _Query = @" 
                        SELECT WhsCode, WhsName, BinActivat 
                        FROM " + Global.SAP_DB + @".dbo.OWHS where WhsCode = @rejectQCWhs and Locked = 'N' ";
                        _logger.LogInformation(" ConfigsController : Reject QC Warehouse Existence Query : {q} ", _Query.ToString());

                        oAdptr = new SqlDataAdapter(_Query, QITcon);
                        oAdptr.SelectCommand.Parameters.AddWithValue("@rejectQCWhs", payload.RejectedWhs);
                        oAdptr.Fill(dtWhs);
                        QITcon.Close();

                        if (dtWhs.Rows.Count > 0)
                        {
                            if (dtWhs.Rows[0]["BinActivat"].ToString().ToLower() == "y" && payload.RejectQCBinAbsEntry <= 0)
                            {
                                return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Provide Bin Location for Reject QC Warehouse" });
                            }
                            else if (dtWhs.Rows[0]["BinActivat"].ToString().ToLower() == "y")
                            {
                                #region Check for Valid Bin Location
                                Value = 0;
                                QITcon = new SqlConnection(_QIT_connection);

                                _Query = @" 
                                SELECT COUNT(*) from " + Global.SAP_DB + @".dbo.OBIN 
                                where WhsCode = @rejectQCWhs and AbsEntry = @absEntry ";
                                _logger.LogInformation(" ConfigsController : Reject QC Bin Location Existence Query : {q} ", _Query.ToString());

                                cmd = new SqlCommand(_Query, QITcon);
                                cmd.Parameters.AddWithValue("@rejectQCWhs", payload.RejectedWhs);
                                cmd.Parameters.AddWithValue("@absEntry", payload.RejectQCBinAbsEntry);
                                QITcon.Open();
                                Value = cmd.ExecuteScalar();
                                QITcon.Close();
                                if (Int32.Parse(Value.ToString()) == 1)
                                { }
                                else
                                {
                                    return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Reject QC Bin Location does not exist" });
                                }
                                # endregion
                            }
                        }
                        else
                        {
                            return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Reject QC Warehouse does not exist" });
                        }

                        #endregion
                    }
                    #endregion

                    #region Non-QC Warehouse

                    if (payload.NonQCWhs.ToString().Trim().Length <= 0 || payload.NonQCWhs.ToString().ToLower().Trim() == "string")
                        return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Provide Non-QC Warehouse" });

                    #region Check Non-QC Existence
                    DataTable dtNonQCWhs = new DataTable();
                    QITcon = new SqlConnection(_QIT_connection);

                    _Query = @" 
                    SELECT WhsCode, WhsName, BinActivat 
                    FROM " + Global.SAP_DB + @".dbo.OWHS where WhsCode = @nonQCWhs and Locked = 'N' ";
                    _logger.LogInformation(" ConfigsController : Non-QC Warehouse Existence Query : {q} ", _Query.ToString());

                    oAdptr = new SqlDataAdapter(_Query, QITcon);
                    oAdptr.SelectCommand.Parameters.AddWithValue("@nonQCWhs", payload.NonQCWhs);
                    oAdptr.Fill(dtNonQCWhs);
                    QITcon.Close();

                    if (dtNonQCWhs.Rows.Count > 0)
                    {
                        if (dtNonQCWhs.Rows[0]["BinActivat"].ToString().ToLower() == "y" && payload.NonQCBinAbsEntry <= 0)
                        {
                            return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Provide Bin Location for Non-QC Warehouse" });
                        }
                        else if (dtNonQCWhs.Rows[0]["BinActivat"].ToString().ToLower() == "y")
                        {
                            #region Check for Valid Bin Location
                            Value = 0;
                            QITcon = new SqlConnection(_QIT_connection);

                            _Query = @" 
                            select COUNT(*) from " + Global.SAP_DB + @".dbo.OBIN 
                            where WhsCode = @nonQCWhs and AbsEntry = @absEntry ";
                            _logger.LogInformation(" ConfigsController : Non-QC Bin Location Existence Query : {q} ", _Query.ToString());

                            cmd = new SqlCommand(_Query, QITcon);
                            cmd.Parameters.AddWithValue("@nonQCWhs", payload.NonQCWhs);
                            cmd.Parameters.AddWithValue("@absEntry", payload.NonQCBinAbsEntry);
                            QITcon.Open();
                            Value = cmd.ExecuteScalar();
                            QITcon.Close();
                            if (Int32.Parse(Value.ToString()) == 1)
                            { }
                            else
                            {
                                return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Non-QC Bin Location does not exist" });
                            }
                            #endregion
                        }
                    }
                    else
                    {
                        return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Non-QC Warehouse does not exist" });
                    }

                    #endregion


                    #endregion

                    #region Check for Delivery Warehouse

                    if (payload.DeliveryWhs.ToString().Trim().Length <= 0 || payload.DeliveryWhs.ToString().ToLower().Trim() == "string")
                        return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Provide Delivery Warehouse" });

                    #region Check Delivery Existence
                    DataTable dtDeliveryWhs = new DataTable();
                    QITcon = new SqlConnection(_QIT_connection);

                    _Query = @" 
                    SELECT WhsCode, WhsName, BinActivat 
                    FROM " + Global.SAP_DB + @".dbo.OWHS where WhsCode = @deliveryWhs and Locked = 'N' ";
                    _logger.LogInformation(" ConfigsController : Delivery Warehouse Existence Query : {q} ", _Query.ToString());

                    oAdptr = new SqlDataAdapter(_Query, QITcon);
                    oAdptr.SelectCommand.Parameters.AddWithValue("@deliveryWhs", payload.DeliveryWhs);
                    oAdptr.Fill(dtDeliveryWhs);
                    QITcon.Close();

                    if (dtDeliveryWhs.Rows.Count > 0)
                    {
                        if (dtDeliveryWhs.Rows[0]["BinActivat"].ToString().ToLower() == "y" && payload.DeliveryBinAbsEntry <= 0)
                        {
                            return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Provide Bin Location for Delivery Warehouse" });
                        }
                        else if (dtDeliveryWhs.Rows[0]["BinActivat"].ToString().ToLower() == "y")
                        {
                            #region Check for Valid Bin Location
                            Value = 0;
                            QITcon = new SqlConnection(_QIT_connection);

                            _Query = @" 
                            SELECT COUNT(*) from " + Global.SAP_DB + @".dbo.OBIN 
                            WHERE WhsCode = @deliveryWhs and AbsEntry = @absEntry ";
                            _logger.LogInformation(" ConfigsController : Delivery Bin Location Existence Query : {q} ", _Query.ToString());

                            cmd = new SqlCommand(_Query, QITcon);
                            cmd.Parameters.AddWithValue("@deliveryWhs", payload.DeliveryWhs);
                            cmd.Parameters.AddWithValue("@absEntry", payload.DeliveryBinAbsEntry);
                            QITcon.Open();
                            Value = cmd.ExecuteScalar();
                            QITcon.Close();
                            if (Int32.Parse(Value.ToString()) == 1)
                            { }
                            else
                            {
                                return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Delivery Bin Location does not exist" });
                            }
                            #endregion
                        }
                    }
                    else
                    {
                        return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Delivery Warehouse does not exist" });
                    }

                    #endregion



                    #endregion

                    #region Check for GRPO Delivery Date
                    if (payload.GRPODeliveryDate.ToString().ToLower().Trim() == "string")
                        return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Provide GRPO Delivery Date" });
                    if (payload.GRPODeliveryDate.ToString().Trim() == String.Empty)
                        return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Provide GRPO Delivery Date" });
                    if (payload.GRPODeliveryDate.ToString().ToUpper() != "P" &&
                        payload.GRPODeliveryDate.ToString().ToUpper() != "C" &&
                        payload.GRPODeliveryDate.ToString().ToUpper() != "G"
                        )
                        return BadRequest(new
                        {
                            StatusCode = "400",
                            IsSaved = _IsSaved,
                            StatusMsg = "GRPO Delivery Date Values : P:PO Delivery Date / C:Current Date / G:GateIn Date"
                        });
                    #endregion

                    #region Check for Pick is Active
                    if (payload.IsPickActive.ToString().ToLower().Trim() == "string")
                        return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Provide Pick is Active or not" });
                    if (payload.IsPickActive.ToString().Trim() == String.Empty)
                        return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Provide Pick is Active or not" });
                    if (payload.IsPickActive.ToString().ToUpper() != "N" && payload.QCRequired.ToString().ToUpper() != "Y")
                        return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "IsPickActive Values : Y:Yes / N:No" });
                    #endregion

                    #region Check for Pick Warehouse
                    if (payload.IsPickActive.ToString().ToUpper() == "Y")
                    {
                        if (payload.PickWhs.ToString().Trim().Length <= 0 || payload.PickWhs.ToString().ToLower().Trim() == "string")
                            return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Provide Pick Warehouse" });

                        #region Check Pick Existence
                        DataTable dtPickWhs = new DataTable();
                        QITcon = new SqlConnection(_QIT_connection);

                        _Query = @" 
                    SELECT WhsCode, WhsName, BinActivat 
                    FROM " + Global.SAP_DB + @".dbo.OWHS where WhsCode = @pickWhs and Locked = 'N' ";
                        _logger.LogInformation(" ConfigsController : Pick Warehouse Existence Query : {q} ", _Query.ToString());

                        oAdptr = new SqlDataAdapter(_Query, QITcon);
                        oAdptr.SelectCommand.Parameters.AddWithValue("@pickWhs", payload.PickWhs);
                        oAdptr.Fill(dtPickWhs);
                        QITcon.Close();

                        if (dtPickWhs.Rows.Count > 0)
                        {
                            if (dtPickWhs.Rows[0]["BinActivat"].ToString().ToLower() == "y" && payload.PickBinAbsEntry <= 0)
                            {
                                return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Provide Bin Location for Pick Warehouse" });
                            }
                            else if (dtPickWhs.Rows[0]["BinActivat"].ToString().ToLower() == "y")
                            {
                                #region Check for Valid Bin Location
                                Value = 0;
                                QITcon = new SqlConnection(_QIT_connection);

                                _Query = @" select COUNT(*) from " + Global.SAP_DB + @".dbo.OBIN where WhsCode = @pickWhs and AbsEntry = @absEntry ";
                                _logger.LogInformation(" ConfigsController : Pick Bin Location Existence Query : {q} ", _Query.ToString());

                                cmd = new SqlCommand(_Query, QITcon);
                                cmd.Parameters.AddWithValue("@pickWhs", payload.PickWhs);
                                cmd.Parameters.AddWithValue("@absEntry", payload.PickBinAbsEntry);
                                QITcon.Open();
                                Value = cmd.ExecuteScalar();
                                QITcon.Close();
                                if (Int32.Parse(Value.ToString()) == 1)
                                { }
                                else
                                {
                                    return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Pick Bin Location does not exist" });
                                }
                                #endregion
                            }
                        }
                        else
                        {
                            return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Pick Warehouse does not exist" });
                        }

                        #endregion

                    }

                    #endregion

                    QITcon = new SqlConnection(_QIT_connection);
                    _Query = @"DELETE FROM QIT_Config_Master where BranchID = @bID
                               INSERT INTO QIT_Config_Master
                               ( BranchID, Atcpath, GenMethod, QRMngBy, QRGenMethod,Remark, Indicator, QCRequired, 
                                 IncomingQCWhs, InProcessQCWhs, ApprovedWhs, RejectedWhs, NonQCWhs, DeliveryWhs,
                                 IncomingBin, InProcessBin, ApproveBin, RejectBin, NonQCBin, DeliveryBin,
                                 IssueSeries, ReceiptSeries, DeliverySeries, GRPOSeries, ITSeries, QCITSeries,
                                 OSSeries,
                                 IsProRework, GRPODeliveryDate, IsPickActive, PickWhs, PickBin 
                               ) 
                               VALUES
                               ( @bID, @atcPath, @genMethod, @qrMngBy, @qrGenMethod, @rem, @indicator, @qc, 
                                 @incoming, @inProcess, @ApproveWhs, @RejWhs, @nonQCWhs, @deliveryWhs,
                                 @inComingBin, @inProcessBin, @approveBin, @rejectBin, @nonQCBin, @deliveryBin,
                                 @issueSeries, @receiptSeries, @deliverySeries, @grpoSeries, @itSeries, @qcITSeries, 
                                 @osSeries,
                                 @isProRework, @gDate, @isPickActive, @pickWhs, @pickBin) ";
                    _logger.LogInformation(" ConfigsController : SaveConfig() Query : {q} ", _Query.ToString());

                    cmd = new SqlCommand(_Query, QITcon);
                    cmd.Parameters.AddWithValue("@atcPath", payload.AtcPath);
                    cmd.Parameters.AddWithValue("@genMethod", payload.GenMethod.ToUpper());
                    cmd.Parameters.AddWithValue("@qrMngBy", payload.QRMngBy.ToUpper());
                    cmd.Parameters.AddWithValue("@qrGenMethod", payload.QRGenMethod.ToUpper());
                    cmd.Parameters.AddWithValue("@rem", payload.Remark.ToUpper());
                    cmd.Parameters.AddWithValue("@bID", payload.BranchID);
                    cmd.Parameters.AddWithValue("@indicator", payload.Indicator);
                    cmd.Parameters.AddWithValue("@qc", payload.QCRequired);
                    cmd.Parameters.AddWithValue("@incoming", payload.IncomingQCWhs);
                    cmd.Parameters.AddWithValue("@inProcess", payload.InProcessQCWhs);
                    cmd.Parameters.AddWithValue("@ApproveWhs", payload.ApprovedWhs);
                    cmd.Parameters.AddWithValue("@RejWhs", payload.RejectedWhs);
                    cmd.Parameters.AddWithValue("@nonQCWhs", payload.NonQCWhs);
                    cmd.Parameters.AddWithValue("@deliveryWhs", payload.DeliveryWhs);

                    cmd.Parameters.AddWithValue("@inComingBin", payload.IncomingQCBinAbsEntry);
                    cmd.Parameters.AddWithValue("@inProcessBin", payload.InProcessQCBinAbsEntry);
                    cmd.Parameters.AddWithValue("@approveBin", payload.ApproveQCBinAbsEntry);
                    cmd.Parameters.AddWithValue("@rejectBin", payload.RejectQCBinAbsEntry);
                    cmd.Parameters.AddWithValue("@nonQCBin", payload.NonQCBinAbsEntry);
                    cmd.Parameters.AddWithValue("@deliveryBin", payload.DeliveryBinAbsEntry);

                    cmd.Parameters.AddWithValue("@issueSeries", payload.IssueSeries);
                    cmd.Parameters.AddWithValue("@receiptSeries", payload.ReceiptSeries);
                    cmd.Parameters.AddWithValue("@deliverySeries", payload.DeliverySeries);
                    cmd.Parameters.AddWithValue("@grpoSeries", payload.GRPOSeries);
                    cmd.Parameters.AddWithValue("@itSeries", payload.ITSeries);
                    cmd.Parameters.AddWithValue("@qcITSeries", payload.QCITSeries);
                    cmd.Parameters.AddWithValue("@osSeries", payload.OSSeries);

                    cmd.Parameters.AddWithValue("@isProRework", payload.IsProRework);
                    cmd.Parameters.AddWithValue("@gDate", payload.GRPODeliveryDate);

                    cmd.Parameters.AddWithValue("@isPickActive", payload.IsPickActive);
                    cmd.Parameters.AddWithValue("@pickWhs", payload.PickWhs);
                    cmd.Parameters.AddWithValue("@pickBin", payload.PickBinAbsEntry);


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
                    return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = "Details not found" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Error in ConfigsController : SaveConfig() :: {Error}", ex.ToString());
                return BadRequest(new { StatusCode = "400", IsSaved = _IsSaved, StatusMsg = ex.Message.ToString() });
            }
        }


        [HttpGet("Get")]
        public async Task<ActionResult<IEnumerable<GetConfig>>> GetConfig(int BranchID)
        {
            SqlConnection QITcon;
            SqlDataAdapter oAdptr;
            try
            {
                _logger.LogInformation(" Calling ConfigsController : Get Config() ");
                List<GetConfig> obj = new List<GetConfig>();
                System.Data.DataTable dtData = new System.Data.DataTable();
                QITcon = new SqlConnection(_QIT_connection);

                _Query = @" 
                SELECT B.BranchID, B.AtcPath, B.GenMethod, B.QRMngBy, B.QRGenMethod, B.BatchType, B.Indicator, B.QCRequired, B.Remark, 
                       B.IncomingQCWhs, ISNULL(B.IncomingBin,0) IncomingBin, F.BinCode IncomingBinCode, 
                       B.InProcessQCWhs, ISNULL(B.InProcessBin, 0) InProcessBin, G.BinCode InProcessBinCode,
                       B.ApprovedWhs, ISNULL(B.ApproveBin,0) ApproveBin, I.BinCode ApproveBinCode, 
                       B.RejectedWhs, ISNULL(B.RejectBin, 0) RejectBin, J.BinCode RejectBinCode, 
                       B.NonQCWhs, ISNULL(B.NonQCBin,0) NonQCBin, H.BinCode NonQCBinCode,
                       B.DeliveryWhs, ISNULL(B.DeliveryBin, 0) DeliveryBin, N.BinCode DeliveryBinCode,                        
                       B.IssueSeries, C.SeriesName IssueSeriesName, B.ReceiptSeries,  D.SeriesName ReceiptSeriesName, 
                       B.DeliverySeries, E.SeriesName DeliverySeriesName, B.GRPOSeries, K.SeriesName GRPOSeriesName, 
                       B.ITSeries, L.SeriesName ITSeriesName, B.QCITSeries, M.SeriesName QCITSeriesName, 
                       B.OSSeries, P.SeriesName OSSeriesName,
                       B.IsProRework, B.GRPODeliveryDate,
                       B.IsPickActive, B.PickWhs, ISNULL(B.PickBin, 0) PickBin, O.BinCode PickBinCode
                FROM [dbo].[QIT_Config_Master] B 
                    LEFT JOIN " + Global.SAP_DB + @".dbo.NNM1 C ON B.IssueSeries = C.Series 
                    LEFT JOIN " + Global.SAP_DB + @".dbo.NNM1 D ON B.ReceiptSeries = D.Series 
                    LEFT JOIN " + Global.SAP_DB + @".dbo.NNM1 E ON B.DeliverySeries = E.Series 
                    LEFT JOIN " + Global.SAP_DB + @".dbo.OBIN F ON F.AbsEntry = B.IncomingBin 
                    LEFT JOIN " + Global.SAP_DB + @".dbo.OBIN G ON G.AbsEntry = B.InProcessBin 
                    LEFT JOIN " + Global.SAP_DB + @".dbo.OBIN H ON H.AbsEntry = B.NonQCBin 
                    LEFT JOIN " + Global.SAP_DB + @".dbo.OBIN I ON I.AbsEntry = B.ApproveBin 
                    LEFT JOIN " + Global.SAP_DB + @".dbo.OBIN J ON J.AbsEntry = B.RejectBin 
                    LEFT JOIN " + Global.SAP_DB + @".dbo.NNM1 K ON B.GRPOSeries = K.Series 
                    LEFT JOIN " + Global.SAP_DB + @".dbo.NNM1 L ON B.ITSeries = L.Series 
                    LEFT JOIN " + Global.SAP_DB + @".dbo.NNM1 M ON B.QCITSeries = M.Series 
                    LEFT JOIN " + Global.SAP_DB + @".dbo.OBIN N ON N.AbsEntry = B.DeliveryBin 
                    LEFT JOIN " + Global.SAP_DB + @".dbo.OBIN O ON O.AbsEntry = B.PickBin
                    LEFT JOIN " + Global.SAP_DB + @".dbo.NNM1 P ON B.OSSeries = P.Series 
                WHERE B.BranchID = @bId ";
                _logger.LogInformation(" ConfigsController : GetItemSubGroup() Query : {q} ", _Query.ToString());
                QITcon.Open();
                oAdptr = new SqlDataAdapter(_Query, QITcon);
                oAdptr.SelectCommand.Parameters.AddWithValue("@bId", BranchID);
                oAdptr.Fill(dtData);
                QITcon.Close();

                if (dtData.Rows.Count > 0)
                {
                    dynamic arData = JsonConvert.SerializeObject(dtData);
                    obj = JsonConvert.DeserializeObject<List<GetConfig>>(arData.ToString());
                    return obj;
                }
                else
                {
                    return BadRequest(new { StatusCode = "400", StatusMsg = "No data found" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(" Error in ConfigsController : GetConfig() :: {Error}", ex.ToString());
                return BadRequest(new { StatusCode = "400", StatusMsg = ex.ToString() });
            }
        }
   
    }
}
