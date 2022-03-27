﻿using Append.Blazor.Notifications;
using WanderLost.Shared.Data;

namespace WanderLost.Client
{
    public class ClientNotificationService
    {
        public bool NotificationsAvailable { get; private set; } = false;

        private readonly INotificationService _notifications;
        private ClientData _userSettings = new();
        public ClientNotificationService(INotificationService notif)
        {
            _notifications = notif;
        }

        /// <summary>
        /// Apply usersettings for ignored merchants, loot, etc.
        /// </summary>
        /// <param name="userSettings"></param>
        /// <returns></returns>
        public void Init(ClientData? userSettings)
        {
            if (userSettings != null)
            {
                _userSettings = userSettings;
                NotificationsAvailable = userSettings.NotificationsEnabled;
            }
        }
        /// <summary>
        /// Check if user has granted permission for Browser-Notifications.
        /// </summary>
        /// <returns></returns>
        public async Task<bool> IsPermissionGrantedByUser()
        {
            if (await _notifications.IsSupportedByBrowserAsync())
            {
                if (_notifications.PermissionStatus == PermissionType.Granted)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Check if browser supports notifications.
        /// </summary>
        /// <returns></returns>
        public ValueTask<bool> IsSupportedByBrowser()
        {
            return _notifications.IsSupportedByBrowserAsync();
        }

        /// <summary>
        /// Request permission to send notifications from user.
        /// </summary>
        /// <returns></returns>
        public async ValueTask<bool> RequestPermission()
        {
            return await _notifications.RequestPermissionAsync() is PermissionType answer && answer == PermissionType.Granted;
        }

        /// <summary>
        /// Disable notifications manually, browser settings are unaffected.
        /// </summary>
        public void DisableNotifications()
        {
            NotificationsAvailable = false;
        }

        /// <summary>
        /// Disable notifications manually, browser settings are unaffected.
        /// </summary>
        public void EnableNotifications()
        {
            NotificationsAvailable = true;
        }

        private bool IsAllowedForMerchantFoundNotifications(ActiveMerchantGroup merchantGroup)
        {
            if (merchantGroup.ActiveMerchants.Count == 0) return false;

            //Check _userSettings if merchants in merchantGroup are allowed for notifications.
            if (_userSettings.NotifyingMerchants != null)
            {
                if (!_userSettings.NotifyingMerchants.Any(allowedMerch => allowedMerch.Name == merchantGroup.MerchantName)) return false;
                if (!_userSettings.NotifyingMerchants.Where(allowedMerch => allowedMerch.Name == merchantGroup.MerchantName)
                                                        .Any(x => x.Zones.Any(allowedZone => merchantGroup.ActiveMerchants.Any(actMerch => actMerch.Zone == allowedZone)))) return false;
                if (!_userSettings.NotifyingMerchants.Where(allowedMerch => allowedMerch.Name == merchantGroup.MerchantName)
                                                        .Any(x => x.Cards.Any(allowedCard => merchantGroup.ActiveMerchants.Any(actMerch => actMerch.Card.Name == allowedCard.Name)))) return false;
            }
            return true;
        }

        /// <summary>
        /// Request a "merchant found" Browser-Notification for the given merchantGroup, rules from usersettings are applied; the request can be denied.
        /// </summary>
        /// <param name="merchantGroup"></param>
        /// <returns></returns>
        public ValueTask RequestMerchantFoundNotification(ActiveMerchantGroup merchantGroup)
        {
            if (!NotificationsAvailable) return ValueTask.CompletedTask;
            if (merchantGroup == null) return ValueTask.CompletedTask;
            if (!IsAllowedForMerchantFoundNotifications(merchantGroup)) return ValueTask.CompletedTask;

            return ForceMerchantFoundNotification(merchantGroup);
        }
        /// <summary>
        /// Force a "merchant found" Browser-Notification for the given merchantGroup, rules from usersettings are NOT applied.
        /// </summary>
        /// <param name="merchantGroup"></param>
        /// <returns></returns>
        public ValueTask ForceMerchantFoundNotification(ActiveMerchantGroup merchantGroup)
        {
            string body = "";
            if (merchantGroup.ActiveMerchants.Count > 1)
            {
                body += "Conflicting merchant data, click for more information.";
            }
            else
            {
                body += $"Location: {merchantGroup.ActiveMerchants[0].Zone}\n";
                body += $"Card: {merchantGroup.ActiveMerchants[0].Card.Name}\n";
                body += $"Rapport: {merchantGroup.ActiveMerchants[0].RapportRarity?.ToString() ?? "_unknown"}\n";
            }

            return _notifications.CreateAsync($"Wandering Merchant \"{merchantGroup.MerchantName}\" found", new NotificationOptions { Body = body, Renotify = true, Tag = $"found_{merchantGroup.MerchantName}", Icon = "images/notifications/ExclamationMark.png" });
        }
        /// <summary>
        /// Request a "merchant appeared" Browser-Notification for the given merchantGroup, rules from usersettings are applied; the request can be denied.
        /// </summary>
        /// <param name="merchantGroup"></param>
        /// <returns></returns>
        public ValueTask RequestMerchantSpawnNotification(ActiveMerchantGroup merchantGroup)
        {
            if (!NotificationsAvailable) return ValueTask.CompletedTask;
            if (merchantGroup == null) return ValueTask.CompletedTask;
            if (!_userSettings.NotifyMerchantAppearance) return ValueTask.CompletedTask;
            if (_userSettings.NotifyingMerchants != null)
            {
                if (!_userSettings.NotifyingMerchants.Any(allowedMerch => allowedMerch.Name == merchantGroup.MerchantName)) return ValueTask.CompletedTask;
            }

            string body = $"Wandering Merchant \"{merchantGroup.MerchantName}\" is waiting for you somewhere.";
            return _notifications.CreateAsync($"Wandering Merchant \"{merchantGroup.MerchantName}\" appeared", new NotificationOptions { Body = body, Renotify = true, Tag = "spawn_merchant", Icon = "images/notifications/QuestionMark.png" });
        }

        /// <summary>
        /// Force a "merchant appeared" Browser-Notification for the given merchantGroup, rules from usersettings are NOT applied.
        /// </summary>
        /// <param name="merchantGroup"></param>
        /// <returns></returns>
        public ValueTask ForceMerchantSpawnNotification(ActiveMerchantGroup merchantGroup)
        {
            string body = $"Wandering Merchant \"{merchantGroup.MerchantName}\" is waiting for you somewhere.";
            return _notifications.CreateAsync($"Wandering Merchant \"{merchantGroup.MerchantName}\" appeared", new NotificationOptions { Body = body, Renotify = true, Tag = "spawn_merchant", Icon = "images/notifications/QuestionMark.png" });
        }
    }
}
