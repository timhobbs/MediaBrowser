﻿using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Model.Session;
using MediaBrowser.Model.System;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Server.Implementations.Session
{
    public class HttpSessionController : ISessionController, IDisposable
    {
        private readonly IHttpClient _httpClient;
        private readonly IJsonSerializer _json;
        private readonly ISessionManager _sessionManager;

        public SessionInfo Session { get; private set; }

        private readonly string _postUrl;

        private Timer _pingTimer;

        public HttpSessionController(IHttpClient httpClient,
            IJsonSerializer json,
            SessionInfo session,
            string postUrl, ISessionManager sessionManager)
        {
            _httpClient = httpClient;
            _json = json;
            Session = session;
            _postUrl = postUrl;
            _sessionManager = sessionManager;

            _pingTimer = new Timer(PingTimerCallback, null, Timeout.Infinite, Timeout.Infinite);

            ResetPingTimer();
        }

        public bool IsSessionActive
        {
            get
            {
                return (DateTime.UtcNow - Session.LastActivityDate).TotalMinutes <= 10;
            }
        }

        public bool SupportsMediaControl
        {
            get { return true; }
        }

        private async void PingTimerCallback(object state)
        {
            try
            {
                await SendMessage("Ping", CancellationToken.None).ConfigureAwait(false);
            }
            catch
            {
                ReportSessionEnded();
            }
        }

        private void ReportSessionEnded()
        {
            try
            {
                _sessionManager.ReportSessionEnded(Session.Id);
            }
            catch (Exception ex)
            {
            }
        }

        private void ResetPingTimer()
        {
            var period = TimeSpan.FromSeconds(60);

            _pingTimer.Change(period, period);
        }

        private Task SendMessage(string name, CancellationToken cancellationToken)
        {
            return SendMessage(name, new Dictionary<string, string>(), cancellationToken);
        }

        private async Task SendMessage(string name, 
            Dictionary<string, string> args, 
            CancellationToken cancellationToken)
        {
            var url = _postUrl + "/" + name + ToQueryString(args);

            await _httpClient.Post(new HttpRequestOptions
            {
                Url = url,
                CancellationToken = cancellationToken

            }).ConfigureAwait(false);

            ResetPingTimer();
        }

        public Task SendSessionEndedNotification(SessionInfoDto sessionInfo, CancellationToken cancellationToken)
        {
            return Task.FromResult(true);
        }

        public Task SendPlaybackStartNotification(SessionInfoDto sessionInfo, CancellationToken cancellationToken)
        {
            return Task.FromResult(true);
        }

        public Task SendPlaybackStoppedNotification(SessionInfoDto sessionInfo, CancellationToken cancellationToken)
        {
            return Task.FromResult(true);
        }

        public Task SendPlayCommand(PlayRequest command, CancellationToken cancellationToken)
        {
            return Task.FromResult(true);
            //return SendMessage(new WebSocketMessage<PlayRequest>
            //{
            //    MessageType = "Play",
            //    Data = command

            //}, cancellationToken);
        }

        public Task SendPlaystateCommand(PlaystateRequest command, CancellationToken cancellationToken)
        {
            var args = new Dictionary<string, string>();

            if (command.Command == PlaystateCommand.Seek)
            {

            }

            return SendMessage(command.Command.ToString(), cancellationToken);
        }

        public Task SendLibraryUpdateInfo(LibraryUpdateInfo info, CancellationToken cancellationToken)
        {
            return Task.FromResult(true);
        }

        public Task SendRestartRequiredNotification(SystemInfo info, CancellationToken cancellationToken)
        {
            return SendMessage("RestartRequired", cancellationToken);
        }

        public Task SendUserDataChangeInfo(UserDataChangeInfo info, CancellationToken cancellationToken)
        {
            return Task.FromResult(true);
        }

        public Task SendServerShutdownNotification(CancellationToken cancellationToken)
        {
            return SendMessage("ServerShuttingDown", cancellationToken);
        }

        public Task SendServerRestartNotification(CancellationToken cancellationToken)
        {
            return SendMessage("ServerRestarting", cancellationToken);
        }

        public Task SendGeneralCommand(GeneralCommand command, CancellationToken cancellationToken)
        {
            return SendMessage(command.Name, command.Arguments, cancellationToken);
        }

        private string ToQueryString(Dictionary<string, string> nvc)
        {
            var array = (from item in nvc
                         select string.Format("{0}={1}", WebUtility.UrlEncode(item.Key), WebUtility.UrlEncode(item.Value)))
                .ToArray();

            var args = string.Join("&", array);

            if (string.IsNullOrEmpty(args))
            {
                return args;
            }

            return "?" + args;
        }

        public void Dispose()
        {
            DisposePingTimer();
        }

        private void DisposePingTimer()
        {
            if (_pingTimer != null)
            {
                _pingTimer.Dispose();
                _pingTimer = null;
            }
        }
    }
}
