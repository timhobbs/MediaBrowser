﻿using MediaBrowser.Common.Implementations.Networking;
using MediaBrowser.Common.Net;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Net;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace MediaBrowser.ServerApplication.Networking
{
    /// <summary>
    /// Class NetUtils
    /// </summary>
    public class NetworkManager : BaseNetworkManager, INetworkManager
    {
        public NetworkManager(ILogger logger)
            : base(logger)
        {
        }

        /// <summary>
        /// Gets the network shares.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns>IEnumerable{NetworkShare}.</returns>
        public IEnumerable<NetworkShare> GetNetworkShares(string path)
        {
			return new List<NetworkShare> ();
        }

        /// <summary>
        /// Gets a list of network devices
        /// </summary>
        /// PC's in the Domain</returns>
		public IEnumerable<FileSystemEntryInfo> GetNetworkDevices()
        {
			return new List<FileSystemEntryInfo> ();
        }
    }
}
