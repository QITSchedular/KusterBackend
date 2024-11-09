namespace WMS_UI_API.Hubs
{
    using WMS_UI_API.Models;
    using Microsoft.AspNetCore.SignalR;
    using System;
    public class NotificationHub : Hub
    {
        private readonly NotificationService _notificationService;
        private readonly ILogger<NotificationHub> _logger;
        public NotificationHub(NotificationService n, ILogger<NotificationHub> logger)
        {
            try
            {
                _notificationService = n;
                _logger = logger;
            }
            catch (Exception ex)
            {
                _logger.LogError("Error :: {Error}", ex.ToString());
                Console.WriteLine($"Error : {ex.Message}");
            }
           
        }

        public override async Task OnConnectedAsync()
        {
            try
            {
                var userId = Context.GetHttpContext().Request.Query["userId"];
                Console.WriteLine("User Id : " + userId);
                var notifications = await _notificationService.GetNotificationsAsync(userId);
                await Groups.AddToGroupAsync(Context.ConnectionId, userId);
                await Clients.Caller.SendAsync("LoadData", notifications);
                await base.OnConnectedAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError("Error in OnConnectedAsync :: {Error}", ex.ToString());
                Console.WriteLine($"Error in OnConnectedAsync: {ex.Message}");
            }
            
        }
        public void NewEntryAdded(Notification_Get_Class newEntity)
        {
            try
            {
                Clients.All.SendAsync("newEntryAdded", newEntity);
            }
            catch (Exception ex) 
            {
                _logger.LogError("Error in NewEntryAdded :: {Error}", ex.ToString());
                Console.WriteLine($"Error in NewEntryAdded: {ex.Message}");
            }
           
        }
    }
}
