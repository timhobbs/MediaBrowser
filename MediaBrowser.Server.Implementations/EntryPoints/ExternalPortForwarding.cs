﻿using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Model.Logging;
using Mono.Nat;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

namespace MediaBrowser.Server.Implementations.EntryPoints
{
    public class ExternalPortForwarding : IServerEntryPoint
    {
        private readonly IServerApplicationHost _appHost;
        private readonly ILogger _logger;
        private readonly IServerConfigurationManager _config;

        private bool _isStarted;

        private Timer _timer;

        public ExternalPortForwarding(ILogManager logmanager, IServerApplicationHost appHost, IServerConfigurationManager config)
        {
            _logger = logmanager.GetLogger("PortMapper");
            _appHost = appHost;
            _config = config;

            _config.ConfigurationUpdated += _config_ConfigurationUpdated;
        }

        void _config_ConfigurationUpdated(object sender, EventArgs e)
        {
            var enable = _config.Configuration.EnableUPnP;

            if (enable && !_isStarted)
            {
                Reload();
            }
            else if (!enable && _isStarted)
            {
                DisposeNat();
            }
        }

        public void Run()
        {
            //NatUtility.Logger = new LogWriter(_logger);

            Reload();
        }

        private void Reload()
        {
            if (_config.Configuration.EnableUPnP)
            {
                _logger.Debug("Starting NAT discovery");

                NatUtility.DeviceFound += NatUtility_DeviceFound;

                // Mono.Nat does never rise this event. The event is there however it is useless. 
                // You could remove it with no risk. 
                NatUtility.DeviceLost += NatUtility_DeviceLost;


                // it is hard to say what one should do when an unhandled exception is raised
                // because there isn't anything one can do about it. Probably save a log or ignored it.
                NatUtility.UnhandledException += NatUtility_UnhandledException;
                NatUtility.StartDiscovery();

                _isStarted = true;

                _timer = new Timer(s => _createdRules = new List<string>(), null, TimeSpan.FromMinutes(10), TimeSpan.FromMinutes(10));
            }
        }

        void NatUtility_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            //var ex = e.ExceptionObject as Exception;

            //if (ex == null)
            //{
            //    _logger.Error("Unidentified error reported by Mono.Nat");
            //}
            //else
            //{
            //    // Seeing some blank exceptions coming through here
            //    _logger.ErrorException("Error reported by Mono.Nat: ", ex);
            //}
        }

        void NatUtility_DeviceFound(object sender, DeviceEventArgs e)
        {
            try
            {
                var device = e.Device;
                _logger.Debug("NAT device found: {0}", device.LocalAddress.ToString());

                CreateRules(device);
            }
            catch (Exception)
            {
                // I think it could be a good idea to log the exception because 
                //   you are using permanent portmapping here (never expire) and that means that next time
                //   CreatePortMap is invoked it can fails with a 718-ConflictInMappingEntry or not. That depends
                //   on the router's upnp implementation (specs says it should fail however some routers don't do it)
                //   It also can fail with others like 727-ExternalPortOnlySupportsWildcard, 728-NoPortMapsAvailable
                // and those errors (upnp errors) could be useful for diagnosting.  

                //_logger.ErrorException("Error creating port forwarding rules", ex);
            }
        }

        private List<string> _createdRules = new List<string>();
        private void CreateRules(INatDevice device)
        {
            // On some systems the device discovered event seems to fire repeatedly
            // This check will help ensure we're not trying to port map the same device over and over

            var address = device.LocalAddress.ToString();

            if (!_createdRules.Contains(address))
            {
                _createdRules.Add(address);

                var info = _appHost.GetSystemInfo();

                CreatePortMap(device, info.HttpServerPortNumber);
            }
        }

        private void CreatePortMap(INatDevice device, int port)
        {
            _logger.Debug("Creating port map on port {0}", port);

            device.CreatePortMap(new Mapping(Protocol.Tcp, port, port)
            {
                Description = "Media Browser Server"
            });
        }

        // As I said before, this method will be never invoked. You can remove it.
        void NatUtility_DeviceLost(object sender, DeviceEventArgs e)
        {
            var device = e.Device;
            _logger.Debug("NAT device lost: {0}", device.LocalAddress.ToString());
        }

        public void Dispose()
        {
            DisposeNat();
        }

        private void DisposeNat()
        {
            _logger.Debug("Stopping NAT discovery");

            if (_timer != null)
            {
                _timer.Dispose();
                _timer = null;
            }

            try
            {
                // This is not a significant improvement
                NatUtility.StopDiscovery();
                NatUtility.DeviceFound -= NatUtility_DeviceFound;
                NatUtility.DeviceLost -= NatUtility_DeviceLost;
                NatUtility.UnhandledException -= NatUtility_UnhandledException;
            }
            // Statements in try-block will no fail because StopDiscovery is a one-line 
            // method that was no chances to fail.
            //		public static void StopDiscovery ()
            //      {
            //          searching.Reset();
            //      }
            // IMO you could remove the catch-block
            catch (Exception ex)
            {
                _logger.ErrorException("Error stopping NAT Discovery", ex);
            }
            finally
            {
                _isStarted = false;
            }
        }

        private class LogWriter : TextWriter
        {
            private readonly ILogger _logger;

            public LogWriter(ILogger logger)
            {
                _logger = logger;
            }

            public override Encoding Encoding
            {
                get { return Encoding.UTF8; }
            }

            public override void WriteLine(string format, params object[] arg)
            {
                _logger.Debug(format, arg);
            }

            public override void WriteLine(string value)
            {
                _logger.Debug(value);
            }
        }
    }
}
