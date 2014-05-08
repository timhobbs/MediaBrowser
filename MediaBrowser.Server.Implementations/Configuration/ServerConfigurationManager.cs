﻿using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Events;
using MediaBrowser.Common.Implementations.Configuration;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using System;
using System.IO;

namespace MediaBrowser.Server.Implementations.Configuration
{
    /// <summary>
    /// Class ServerConfigurationManager
    /// </summary>
    public class ServerConfigurationManager : BaseConfigurationManager, IServerConfigurationManager
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ServerConfigurationManager" /> class.
        /// </summary>
        /// <param name="applicationPaths">The application paths.</param>
        /// <param name="logManager">The log manager.</param>
        /// <param name="xmlSerializer">The XML serializer.</param>
        public ServerConfigurationManager(IApplicationPaths applicationPaths, ILogManager logManager, IXmlSerializer xmlSerializer)
            : base(applicationPaths, logManager, xmlSerializer)
        {
            UpdateItemsByNamePath();
            UpdateTranscodingTempPath();
            UpdateMetadataPath();
        }

        public event EventHandler<GenericEventArgs<ServerConfiguration>> ConfigurationUpdating;

        /// <summary>
        /// Gets the type of the configuration.
        /// </summary>
        /// <value>The type of the configuration.</value>
        protected override Type ConfigurationType
        {
            get { return typeof(ServerConfiguration); }
        }

        /// <summary>
        /// Gets the application paths.
        /// </summary>
        /// <value>The application paths.</value>
        public IServerApplicationPaths ApplicationPaths
        {
            get { return (IServerApplicationPaths)CommonApplicationPaths; }
        }

        /// <summary>
        /// Gets the configuration.
        /// </summary>
        /// <value>The configuration.</value>
        public ServerConfiguration Configuration
        {
            get { return (ServerConfiguration)CommonConfiguration; }
        }

        /// <summary>
        /// Called when [configuration updated].
        /// </summary>
        protected override void OnConfigurationUpdated()
        {
            UpdateItemsByNamePath();
            UpdateTranscodingTempPath();
            UpdateMetadataPath();

            base.OnConfigurationUpdated();
        }

        /// <summary>
        /// Updates the items by name path.
        /// </summary>
        private void UpdateItemsByNamePath()
        {
            ((ServerApplicationPaths)ApplicationPaths).ItemsByNamePath = string.IsNullOrEmpty(Configuration.ItemsByNamePath) ?
                null :
                Configuration.ItemsByNamePath;
        }

        /// <summary>
        /// Updates the metadata path.
        /// </summary>
        private void UpdateMetadataPath()
        {
            ((ServerApplicationPaths)ApplicationPaths).InternalMetadataPath = string.IsNullOrEmpty(Configuration.MetadataPath) ?
                null :
                Configuration.MetadataPath;
        }

        /// <summary>
        /// Updates the transcoding temporary path.
        /// </summary>
        private void UpdateTranscodingTempPath()
        {
            ((ServerApplicationPaths)ApplicationPaths).TranscodingTempPath = string.IsNullOrEmpty(Configuration.TranscodingTempPath) ?
                null :
                Configuration.TranscodingTempPath;
        }

        /// <summary>
        /// Replaces the configuration.
        /// </summary>
        /// <param name="newConfiguration">The new configuration.</param>
        /// <exception cref="System.IO.DirectoryNotFoundException"></exception>
        public override void ReplaceConfiguration(BaseApplicationConfiguration newConfiguration)
        {
            var newConfig = (ServerConfiguration)newConfiguration;

            ValidateItemByNamePath(newConfig);
            ValidateTranscodingTempPath(newConfig);
            ValidatePathSubstitutions(newConfig);
            ValidateMetadataPath(newConfig);

            EventHelper.FireEventIfNotNull(ConfigurationUpdating, this, new GenericEventArgs<ServerConfiguration> { Argument = newConfig }, Logger);

            base.ReplaceConfiguration(newConfiguration);
        }

        private void ValidatePathSubstitutions(ServerConfiguration newConfig)
        {
            foreach (var map in newConfig.PathSubstitutions)
            {
                if (string.IsNullOrWhiteSpace(map.From) || string.IsNullOrWhiteSpace(map.To))
                {
                    throw new ArgumentException("Invalid path substitution");
                }

                if (!map.From.EndsWith(":\\") && !map.From.EndsWith(":/"))
                {
                    map.From = map.From.TrimEnd('/').TrimEnd('\\');
                }
                if (!map.To.EndsWith(":\\") && !map.To.EndsWith(":/"))
                {
                    map.To = map.To.TrimEnd('/').TrimEnd('\\');
                }

                if (string.IsNullOrWhiteSpace(map.From) || string.IsNullOrWhiteSpace(map.To))
                {
                    throw new ArgumentException("Invalid path substitution");
                }
            }
        }

        /// <summary>
        /// Replaces the item by name path.
        /// </summary>
        /// <param name="newConfig">The new configuration.</param>
        /// <exception cref="System.IO.DirectoryNotFoundException"></exception>
        private void ValidateItemByNamePath(ServerConfiguration newConfig)
        {
            var newPath = newConfig.ItemsByNamePath;

            if (!string.IsNullOrWhiteSpace(newPath)
                && !string.Equals(Configuration.ItemsByNamePath ?? string.Empty, newPath))
            {
                // Validate
                if (!Directory.Exists(newPath))
                {
                    throw new DirectoryNotFoundException(string.Format("{0} does not exist.", newPath));
                }
            }
        }

        /// <summary>
        /// Validates the transcoding temporary path.
        /// </summary>
        /// <param name="newConfig">The new configuration.</param>
        /// <exception cref="DirectoryNotFoundException"></exception>
        private void ValidateTranscodingTempPath(ServerConfiguration newConfig)
        {
            var newPath = newConfig.TranscodingTempPath;

            if (!string.IsNullOrWhiteSpace(newPath)
                && !string.Equals(Configuration.TranscodingTempPath ?? string.Empty, newPath))
            {
                // Validate
                if (!Directory.Exists(newPath))
                {
                    throw new DirectoryNotFoundException(string.Format("{0} does not exist.", newPath));
                }
            }
        }

        /// <summary>
        /// Validates the metadata path.
        /// </summary>
        /// <param name="newConfig">The new configuration.</param>
        /// <exception cref="System.IO.DirectoryNotFoundException"></exception>
        private void ValidateMetadataPath(ServerConfiguration newConfig)
        {
            var newPath = newConfig.MetadataPath;

            if (!string.IsNullOrWhiteSpace(newPath)
                && !string.Equals(Configuration.MetadataPath ?? string.Empty, newPath))
            {
                // Validate
                if (!Directory.Exists(newPath))
                {
                    throw new DirectoryNotFoundException(string.Format("{0} does not exist.", newPath));
                }
            }
        }
    }
}
