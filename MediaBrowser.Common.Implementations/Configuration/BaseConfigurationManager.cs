﻿using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Events;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace MediaBrowser.Common.Implementations.Configuration
{
    /// <summary>
    /// Class BaseConfigurationManager
    /// </summary>
    public abstract class BaseConfigurationManager : IConfigurationManager
    {
        /// <summary>
        /// Gets the type of the configuration.
        /// </summary>
        /// <value>The type of the configuration.</value>
        protected abstract Type ConfigurationType { get; }

        /// <summary>
        /// Occurs when [configuration updated].
        /// </summary>
        public event EventHandler<EventArgs> ConfigurationUpdated;

        /// <summary>
        /// Occurs when [configuration updating].
        /// </summary>
        public event EventHandler<ConfigurationUpdateEventArgs> NamedConfigurationUpdating;
        
        /// <summary>
        /// Occurs when [named configuration updated].
        /// </summary>
        public event EventHandler<ConfigurationUpdateEventArgs> NamedConfigurationUpdated;

        /// <summary>
        /// Gets the logger.
        /// </summary>
        /// <value>The logger.</value>
        protected ILogger Logger { get; private set; }
        /// <summary>
        /// Gets the XML serializer.
        /// </summary>
        /// <value>The XML serializer.</value>
        protected IXmlSerializer XmlSerializer { get; private set; }

        /// <summary>
        /// Gets or sets the application paths.
        /// </summary>
        /// <value>The application paths.</value>
        public IApplicationPaths CommonApplicationPaths { get; private set; }

        /// <summary>
        /// The _configuration loaded
        /// </summary>
        private bool _configurationLoaded;
        /// <summary>
        /// The _configuration sync lock
        /// </summary>
        private object _configurationSyncLock = new object();
        /// <summary>
        /// The _configuration
        /// </summary>
        private BaseApplicationConfiguration _configuration;
        /// <summary>
        /// Gets the system configuration
        /// </summary>
        /// <value>The configuration.</value>
        public BaseApplicationConfiguration CommonConfiguration
        {
            get
            {
                // Lazy load
                LazyInitializer.EnsureInitialized(ref _configuration, ref _configurationLoaded, ref _configurationSyncLock, () => (BaseApplicationConfiguration)ConfigurationHelper.GetXmlConfiguration(ConfigurationType, CommonApplicationPaths.SystemConfigurationFilePath, XmlSerializer));
                return _configuration;
            }
            protected set
            {
                _configuration = value;

                _configurationLoaded = value != null;
            }
        }

        private ConfigurationStore[] _configurationStores = {};
        private IConfigurationFactory[] _configurationFactories;

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseConfigurationManager" /> class.
        /// </summary>
        /// <param name="applicationPaths">The application paths.</param>
        /// <param name="logManager">The log manager.</param>
        /// <param name="xmlSerializer">The XML serializer.</param>
        protected BaseConfigurationManager(IApplicationPaths applicationPaths, ILogManager logManager, IXmlSerializer xmlSerializer)
        {
            CommonApplicationPaths = applicationPaths;
            XmlSerializer = xmlSerializer;
            Logger = logManager.GetLogger(GetType().Name);

            UpdateCachePath();
        }

        public void AddParts(IEnumerable<IConfigurationFactory> factories)
        {
            _configurationFactories = factories.ToArray();

            _configurationStores = _configurationFactories
                .SelectMany(i => i.GetConfigurations())
                .ToArray();
        }

        /// <summary>
        /// Saves the configuration.
        /// </summary>
        public void SaveConfiguration()
        {
            var path = CommonApplicationPaths.SystemConfigurationFilePath;

            Directory.CreateDirectory(Path.GetDirectoryName(path));

            lock (_configurationSyncLock)
            {
                XmlSerializer.SerializeToFile(CommonConfiguration, path);
            }

            OnConfigurationUpdated();
        }

        /// <summary>
        /// Called when [configuration updated].
        /// </summary>
        protected virtual void OnConfigurationUpdated()
        {
            UpdateCachePath();

            EventHelper.QueueEventIfNotNull(ConfigurationUpdated, this, EventArgs.Empty, Logger);
        }

        /// <summary>
        /// Replaces the configuration.
        /// </summary>
        /// <param name="newConfiguration">The new configuration.</param>
        /// <exception cref="System.ArgumentNullException">newConfiguration</exception>
        public virtual void ReplaceConfiguration(BaseApplicationConfiguration newConfiguration)
        {
            if (newConfiguration == null)
            {
                throw new ArgumentNullException("newConfiguration");
            }

            ValidateCachePath(newConfiguration);

            CommonConfiguration = newConfiguration;
            SaveConfiguration();
        }

        /// <summary>
        /// Updates the items by name path.
        /// </summary>
        private void UpdateCachePath()
        {
            ((BaseApplicationPaths)CommonApplicationPaths).CachePath = string.IsNullOrEmpty(CommonConfiguration.CachePath) ?
                null :
                CommonConfiguration.CachePath;
        }

        /// <summary>
        /// Replaces the cache path.
        /// </summary>
        /// <param name="newConfig">The new configuration.</param>
        /// <exception cref="System.IO.DirectoryNotFoundException"></exception>
        private void ValidateCachePath(BaseApplicationConfiguration newConfig)
        {
            var newPath = newConfig.CachePath;

            if (!string.IsNullOrWhiteSpace(newPath)
                && !string.Equals(CommonConfiguration.CachePath ?? string.Empty, newPath))
            {
                // Validate
                if (!Directory.Exists(newPath))
                {
                    throw new DirectoryNotFoundException(string.Format("{0} does not exist.", newPath));
                }
            }
        }

        private readonly ConcurrentDictionary<string, object> _configurations = new ConcurrentDictionary<string, object>();

        private string GetConfigurationFile(string key)
        {
            return Path.Combine(CommonApplicationPaths.ConfigurationDirectoryPath, key.ToLower() + ".xml");
        }

        public object GetConfiguration(string key)
        {
            return _configurations.GetOrAdd(key, k =>
            {
                var file = GetConfigurationFile(key);

                var configurationType = _configurationStores
                    .First(i => string.Equals(i.Key, key, StringComparison.OrdinalIgnoreCase))
                    .ConfigurationType;

                lock (_configurationSyncLock)
                {
                    return ConfigurationHelper.GetXmlConfiguration(configurationType, file, XmlSerializer);
                }
            });
        }

        public void SaveConfiguration(string key, object configuration)
        {
            var configurationType = GetConfigurationType(key);

            if (configuration.GetType() != configurationType)
            {
                throw new ArgumentException("Expected configuration type is " + configurationType.Name);
            }

            EventHelper.FireEventIfNotNull(NamedConfigurationUpdating, this, new ConfigurationUpdateEventArgs
            {
                Key = key,
                NewConfiguration = configuration

            }, Logger);
            
            _configurations.AddOrUpdate(key, configuration, (k, v) => configuration);

            var path = GetConfigurationFile(key);
            Directory.CreateDirectory(Path.GetDirectoryName(path));

            lock (_configurationSyncLock)
            {
                XmlSerializer.SerializeToFile(configuration, path);
            }

            EventHelper.FireEventIfNotNull(NamedConfigurationUpdated, this, new ConfigurationUpdateEventArgs
            {
                Key = key,
                NewConfiguration = configuration

            }, Logger);
        }

        public Type GetConfigurationType(string key)
        {
            return _configurationStores
                .First(i => string.Equals(i.Key, key, StringComparison.OrdinalIgnoreCase))
                .ConfigurationType;
        }
    }
}
