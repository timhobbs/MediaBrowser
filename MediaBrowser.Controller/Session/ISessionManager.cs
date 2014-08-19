﻿using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Events;
using MediaBrowser.Model.Session;
using MediaBrowser.Model.Users;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Controller.Session
{
    /// <summary>
    /// Interface ISessionManager
    /// </summary>
    public interface ISessionManager
    {
        /// <summary>
        /// Occurs when [playback start].
        /// </summary>
        event EventHandler<PlaybackProgressEventArgs> PlaybackStart;

        /// <summary>
        /// Occurs when [playback progress].
        /// </summary>
        event EventHandler<PlaybackProgressEventArgs> PlaybackProgress;

        /// <summary>
        /// Occurs when [playback stopped].
        /// </summary>
        event EventHandler<PlaybackStopEventArgs> PlaybackStopped;

        /// <summary>
        /// Occurs when [session started].
        /// </summary>
        event EventHandler<SessionEventArgs> SessionStarted;

        /// <summary>
        /// Occurs when [session ended].
        /// </summary>
        event EventHandler<SessionEventArgs> SessionEnded;

        event EventHandler<SessionEventArgs> SessionActivity;
        
        /// <summary>
        /// Occurs when [capabilities changed].
        /// </summary>
        event EventHandler<SessionEventArgs> CapabilitiesChanged;

        /// <summary>
        /// Occurs when [authentication failed].
        /// </summary>
        event EventHandler<GenericEventArgs<AuthenticationRequest>> AuthenticationFailed;

        /// <summary>
        /// Occurs when [authentication succeeded].
        /// </summary>
        event EventHandler<GenericEventArgs<AuthenticationRequest>> AuthenticationSucceeded;
        
        /// <summary>
        /// Gets the sessions.
        /// </summary>
        /// <value>The sessions.</value>
        IEnumerable<SessionInfo> Sessions { get; }

        /// <summary>
        /// Adds the parts.
        /// </summary>
        /// <param name="sessionFactories">The session factories.</param>
        void AddParts(IEnumerable<ISessionControllerFactory> sessionFactories);

        /// <summary>
        /// Logs the user activity.
        /// </summary>
        /// <param name="clientType">Type of the client.</param>
        /// <param name="appVersion">The app version.</param>
        /// <param name="deviceId">The device id.</param>
        /// <param name="deviceName">Name of the device.</param>
        /// <param name="remoteEndPoint">The remote end point.</param>
        /// <param name="user">The user.</param>
        /// <returns>Task.</returns>
        /// <exception cref="System.ArgumentNullException">user</exception>
        Task<SessionInfo> LogSessionActivity(string clientType, string appVersion, string deviceId, string deviceName, string remoteEndPoint, User user);

        /// <summary>
        /// Used to report that playback has started for an item
        /// </summary>
        /// <param name="info">The info.</param>
        /// <returns>Task.</returns>
        Task OnPlaybackStart(PlaybackStartInfo info);

        /// <summary>
        /// Used to report playback progress for an item
        /// </summary>
        /// <param name="info">The info.</param>
        /// <returns>Task.</returns>
        /// <exception cref="System.ArgumentNullException"></exception>
        Task OnPlaybackProgress(PlaybackProgressInfo info);

        /// <summary>
        /// Used to report that playback has ended for an item
        /// </summary>
        /// <param name="info">The info.</param>
        /// <returns>Task.</returns>
        /// <exception cref="System.ArgumentNullException"></exception>
        Task OnPlaybackStopped(PlaybackStopInfo info);

        /// <summary>
        /// Reports the session ended.
        /// </summary>
        /// <param name="sessionId">The session identifier.</param>
        /// <returns>Task.</returns>
        void ReportSessionEnded(string sessionId);

        /// <summary>
        /// Gets the session info dto.
        /// </summary>
        /// <param name="session">The session.</param>
        /// <returns>SessionInfoDto.</returns>
        SessionInfoDto GetSessionInfoDto(SessionInfo session);

        /// <summary>
        /// Sends the general command.
        /// </summary>
        /// <param name="controllingSessionId">The controlling session identifier.</param>
        /// <param name="sessionId">The session identifier.</param>
        /// <param name="command">The command.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        Task SendGeneralCommand(string controllingSessionId, string sessionId, GeneralCommand command, CancellationToken cancellationToken);
        
        /// <summary>
        /// Sends the message command.
        /// </summary>
        /// <param name="controllingSessionId">The controlling session identifier.</param>
        /// <param name="sessionId">The session id.</param>
        /// <param name="command">The command.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        Task SendMessageCommand(string controllingSessionId, string sessionId, MessageCommand command, CancellationToken cancellationToken);

        /// <summary>
        /// Sends the play command.
        /// </summary>
        /// <param name="controllingSessionId">The controlling session identifier.</param>
        /// <param name="sessionId">The session id.</param>
        /// <param name="command">The command.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        Task SendPlayCommand(string controllingSessionId, string sessionId, PlayRequest command, CancellationToken cancellationToken);

        /// <summary>
        /// Sends the browse command.
        /// </summary>
        /// <param name="controllingSessionId">The controlling session identifier.</param>
        /// <param name="sessionId">The session id.</param>
        /// <param name="command">The command.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        Task SendBrowseCommand(string controllingSessionId, string sessionId, BrowseRequest command, CancellationToken cancellationToken);

        /// <summary>
        /// Sends the playstate command.
        /// </summary>
        /// <param name="controllingSessionId">The controlling session identifier.</param>
        /// <param name="sessionId">The session id.</param>
        /// <param name="command">The command.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        Task SendPlaystateCommand(string controllingSessionId, string sessionId, PlaystateRequest command, CancellationToken cancellationToken);

        /// <summary>
        /// Sends the restart required message.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        Task SendRestartRequiredNotification(CancellationToken cancellationToken);

        /// <summary>
        /// Sends the server shutdown notification.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        Task SendServerShutdownNotification(CancellationToken cancellationToken);

        /// <summary>
        /// Sends the server restart notification.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        Task SendServerRestartNotification(CancellationToken cancellationToken);

        /// <summary>
        /// Adds the additional user.
        /// </summary>
        /// <param name="sessionId">The session identifier.</param>
        /// <param name="userId">The user identifier.</param>
        void AddAdditionalUser(string sessionId, Guid userId);

        /// <summary>
        /// Removes the additional user.
        /// </summary>
        /// <param name="sessionId">The session identifier.</param>
        /// <param name="userId">The user identifier.</param>
        void RemoveAdditionalUser(string sessionId, Guid userId);

        /// <summary>
        /// Reports the now viewing item.
        /// </summary>
        /// <param name="sessionId">The session identifier.</param>
        /// <param name="itemId">The item identifier.</param>
        void ReportNowViewingItem(string sessionId, string itemId);

        /// <summary>
        /// Reports the now viewing item.
        /// </summary>
        /// <param name="sessionId">The session identifier.</param>
        /// <param name="item">The item.</param>
        void ReportNowViewingItem(string sessionId, BaseItemInfo item);

        /// <summary>
        /// Authenticates the new session.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="isLocal">if set to <c>true</c> [is local].</param>
        /// <returns>Task{SessionInfo}.</returns>
        Task<AuthenticationResult> AuthenticateNewSession(AuthenticationRequest request, bool isLocal);

        /// <summary>
        /// Reports the capabilities.
        /// </summary>
        /// <param name="sessionId">The session identifier.</param>
        /// <param name="capabilities">The capabilities.</param>
        void ReportCapabilities(string sessionId, SessionCapabilities capabilities);

        /// <summary>
        /// Reports the transcoding information.
        /// </summary>
        /// <param name="deviceId">The device identifier.</param>
        /// <param name="info">The information.</param>
        void ReportTranscodingInfo(string deviceId, TranscodingInfo info);

        /// <summary>
        /// Clears the transcoding information.
        /// </summary>
        /// <param name="deviceId">The device identifier.</param>
        void ClearTranscodingInfo(string deviceId);

        /// <summary>
        /// Gets the session.
        /// </summary>
        /// <param name="deviceId">The device identifier.</param>
        /// <param name="client">The client.</param>
        /// <param name="version">The version.</param>
        /// <returns>SessionInfo.</returns>
        SessionInfo GetSession(string deviceId, string client, string version);

        /// <summary>
        /// Validates the security token.
        /// </summary>
        /// <param name="accessToken">The access token.</param>
        void ValidateSecurityToken(string accessToken);

        /// <summary>
        /// Logouts the specified access token.
        /// </summary>
        /// <param name="accessToken">The access token.</param>
        /// <returns>Task.</returns>
        Task Logout(string accessToken);

        /// <summary>
        /// Revokes the user tokens.
        /// </summary>
        /// <param name="userId">The user identifier.</param>
        /// <returns>Task.</returns>
        Task RevokeUserTokens(string userId);

        /// <summary>
        /// Revokes the token.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns>Task.</returns>
        Task RevokeToken(string id);
    }
}