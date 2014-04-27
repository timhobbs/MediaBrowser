﻿using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Notifications;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Notifications;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Server.Implementations.Notifications
{
    public class NotificationManager : INotificationManager
    {
        private readonly ILogger _logger;
        private readonly IUserManager _userManager;
        private readonly IServerConfigurationManager _config;

        private INotificationService[] _services;
        private INotificationTypeFactory[] _typeFactories;

        public NotificationManager(ILogManager logManager, IUserManager userManager, IServerConfigurationManager config)
        {
            _userManager = userManager;
            _config = config;
            _logger = logManager.GetLogger(GetType().Name);
        }

        public Task SendNotification(NotificationRequest request, CancellationToken cancellationToken)
        {
            var users = request.UserIds.Select(i => _userManager.GetUserById(new Guid(i)));

            var notificationType = request.NotificationType;

            var title = GetTitle(request);

            var tasks = _services.Where(i => IsEnabled(i, notificationType))
                .Select(i => SendNotification(request, i, users, title, cancellationToken));

            return Task.WhenAll(tasks);
        }

        private Task SendNotification(NotificationRequest request,
            INotificationService service,
            IEnumerable<User> users,
            string title,
            CancellationToken cancellationToken)
        {
            users = users.Where(i => IsEnabledForUser(service, i))
                .ToList();

            var tasks = users.Select(i => SendNotification(request, service, title, i, cancellationToken));

            return Task.WhenAll(tasks);

        }

        private async Task SendNotification(NotificationRequest request,
            INotificationService service,
            string title,
            User user,
            CancellationToken cancellationToken)
        {
            var notification = new UserNotification
            {
                Date = request.Date,
                Description = request.Description,
                Level = request.Level,
                Name = title,
                Url = request.Url,
                User = user
            };

            _logger.Debug("Sending notification via {0} to user {1}", service.Name, user.Name);

            try
            {
                await service.SendNotification(notification, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.ErrorException("Error sending notification to {0}", ex, service.Name);
            }
        }

        private string GetTitle(NotificationRequest request)
        {
            var title = request.Name;

            // If empty, grab from options 
            if (string.IsNullOrEmpty(title))
            {
                if (!string.IsNullOrEmpty(request.NotificationType))
                {
                    var options = _config.Configuration.NotificationOptions.GetOptions(request.NotificationType);

                    if (options != null)
                    {
                        title = options.Title;
                    }
                }
            }

            // If still empty, grab default
            if (string.IsNullOrEmpty(title))
            {
                if (!string.IsNullOrEmpty(request.NotificationType))
                {
                    var info = GetNotificationTypes().FirstOrDefault(i => string.Equals(i.Type, request.NotificationType, StringComparison.OrdinalIgnoreCase));

                    if (info != null)
                    {
                        title = info.DefaultTitle;
                    }
                }
            }

            title = title ?? string.Empty;

            foreach (var pair in request.Variables)
            {
                var token = "{" + pair.Key + "}";

                title = title.Replace(token, pair.Value, StringComparison.OrdinalIgnoreCase);
            }

            return title;
        }

        private bool IsEnabledForUser(INotificationService service, User user)
        {
            try
            {
                return service.IsEnabledForUser(user);
            }
            catch (Exception ex)
            {
                _logger.ErrorException("Error in IsEnabledForUser", ex);
                return false;
            }
        }

        private bool IsEnabled(INotificationService service, string notificationType)
        {
            return string.IsNullOrEmpty(notificationType) ||
                _config.Configuration.NotificationOptions.IsServiceEnabled(service.Name, notificationType);
        }

        public void AddParts(IEnumerable<INotificationService> services, IEnumerable<INotificationTypeFactory> notificationTypeFactories)
        {
            _services = services.ToArray();
            _typeFactories = notificationTypeFactories.ToArray();
        }

        public IEnumerable<NotificationTypeInfo> GetNotificationTypes()
        {
            var list = _typeFactories.Select(i =>
            {
                try
                {
                    return i.GetNotificationTypes().ToList();
                }
                catch (Exception ex)
                {
                    _logger.ErrorException("Error in GetNotificationTypes", ex);
                    return new List<NotificationTypeInfo>();
                }

            }).SelectMany(i => i).ToList();

            foreach (var i in list)
            {
                i.Enabled = _config.Configuration.NotificationOptions.IsEnabled(i.Type);
            }

            return list;
        }

        public IEnumerable<NotificationServiceInfo> GetNotificationServices()
        {
            return _services.Select(i => new NotificationServiceInfo
            {
                Name = i.Name,
                Id = i.Name.GetMD5().ToString("N")

            }).OrderBy(i => i.Name);
        }
    }
}
