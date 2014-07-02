﻿using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Net;
using MediaBrowser.Controller.Session;
using ServiceStack.Web;

namespace MediaBrowser.Server.Implementations.HttpServer.Security
{
    public class SessionContext : ISessionContext
    {
        private readonly IUserManager _userManager;
        private readonly ISessionManager _sessionManager;
        private readonly IAuthorizationContext _authContext;

        public SessionContext(IUserManager userManager, IAuthorizationContext authContext, ISessionManager sessionManager)
        {
            _userManager = userManager;
            _authContext = authContext;
            _sessionManager = sessionManager;
        }

        public SessionInfo GetSession(IRequest requestContext)
        {
            var authorization = _authContext.GetAuthorizationInfo(requestContext);

            return _sessionManager.GetSession(authorization.DeviceId, authorization.Client, authorization.Version);
        }

        public User GetUser(IRequest requestContext)
        {
            var session = GetSession(requestContext);

            return session == null || !session.UserId.HasValue ? null : _userManager.GetUserById(session.UserId.Value);
        }
    }
}
