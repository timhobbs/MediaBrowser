﻿using System;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.IO;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Net;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Serialization;
using ServiceStack;
using ServiceStack.Text.Controller;
using ServiceStack.Web;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MediaBrowser.Api
{
    /// <summary>
    /// Class GetConfiguration
    /// </summary>
    [Route("/System/Configuration", "GET", Summary = "Gets application configuration")]
    [Authenticated]
    public class GetConfiguration : IReturn<ServerConfiguration>
    {

    }

    [Route("/System/Configuration/{Key}", "GET", Summary = "Gets a named configuration")]
    [Authenticated]
    public class GetNamedConfiguration
    {
        [ApiMember(Name = "Key", Description = "Key", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string Key { get; set; }
    }
    
    /// <summary>
    /// Class UpdateConfiguration
    /// </summary>
    [Route("/System/Configuration", "POST", Summary = "Updates application configuration")]
    [Authenticated]
    public class UpdateConfiguration : ServerConfiguration, IReturnVoid
    {
    }

    [Route("/System/Configuration/{Key}", "POST", Summary = "Updates named configuration")]
    [Authenticated]
    public class UpdateNamedConfiguration : IReturnVoid, IRequiresRequestStream
    {
        [ApiMember(Name = "Key", Description = "Key", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string Key { get; set; }

        public Stream RequestStream { get; set; }
    }
    
    [Route("/System/Configuration/MetadataOptions/Default", "GET", Summary = "Gets a default MetadataOptions object")]
    [Authenticated]
    public class GetDefaultMetadataOptions : IReturn<MetadataOptions>
    {

    }

    [Route("/System/Configuration/MetadataPlugins", "GET", Summary = "Gets all available metadata plugins")]
    [Authenticated]
    public class GetMetadataPlugins : IReturn<List<MetadataPluginSummary>>
    {

    }

    [Route("/System/Configuration/MetadataPlugins/Autoset", "POST")]
    [Authenticated]
    public class AutoSetMetadataOptions : IReturnVoid
    {

    }

    public class ConfigurationService : BaseApiService
    {
        /// <summary>
        /// The _json serializer
        /// </summary>
        private readonly IJsonSerializer _jsonSerializer;

        /// <summary>
        /// The _configuration manager
        /// </summary>
        private readonly IServerConfigurationManager _configurationManager;

        private readonly IFileSystem _fileSystem;
        private readonly IProviderManager _providerManager;
        private readonly ILibraryManager _libraryManager;

        public ConfigurationService(IJsonSerializer jsonSerializer, IServerConfigurationManager configurationManager, IFileSystem fileSystem, IProviderManager providerManager, ILibraryManager libraryManager)
        {
            _jsonSerializer = jsonSerializer;
            _configurationManager = configurationManager;
            _fileSystem = fileSystem;
            _providerManager = providerManager;
            _libraryManager = libraryManager;
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public object Get(GetConfiguration request)
        {
            var configPath = _configurationManager.ApplicationPaths.SystemConfigurationFilePath;

            var dateModified = _fileSystem.GetLastWriteTimeUtc(configPath);

            var cacheKey = (configPath + dateModified.Ticks).GetMD5();

            return ToOptimizedResultUsingCache(cacheKey, dateModified, null, () => _configurationManager.Configuration);
        }

        public object Get(GetNamedConfiguration request)
        {
            var result = _configurationManager.GetConfiguration(request.Key);

            return ToOptimizedResult(result);
        }

        public void Post(AutoSetMetadataOptions request)
        {
            var service = AutoDetectMetadataService();

            Logger.Info("Setting preferred metadata format to " + service);

            _configurationManager.SetPreferredMetadataService(service);
            _configurationManager.SaveConfiguration();
        }

        private string AutoDetectMetadataService()
        {
            const string xbmc = "Xbmc Nfo";
            const string mb = "Media Browser Xml";

            var paths = _libraryManager.GetDefaultVirtualFolders()
                .SelectMany(i => i.Locations)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Select(i => new DirectoryInfo(i))
                .ToList();

            if (paths.Select(i => i.EnumerateFiles("*.xml", SearchOption.AllDirectories))
                .SelectMany(i => i)
                .Any())
            {
                return xbmc;
            }

            if (paths.Select(i => i.EnumerateFiles("*.xml", SearchOption.AllDirectories))
                .SelectMany(i => i)
                .Any(i => string.Equals(i.Name, "series.xml", StringComparison.OrdinalIgnoreCase) || string.Equals(i.Name, "movie.xml", StringComparison.OrdinalIgnoreCase)))
            {
                return mb;
            }
            
            return xbmc;
        }

        /// <summary>
        /// Posts the specified configuraiton.
        /// </summary>
        /// <param name="request">The request.</param>
        public void Post(UpdateConfiguration request)
        {
            // Silly, but we need to serialize and deserialize or the XmlSerializer will write the xml with an element name of UpdateConfiguration
            var json = _jsonSerializer.SerializeToString(request);

            var config = _jsonSerializer.DeserializeFromString<ServerConfiguration>(json);

            _configurationManager.ReplaceConfiguration(config);
        }

        public void Post(UpdateNamedConfiguration request)
        {
            var pathInfo = PathInfo.Parse(Request.PathInfo);
            var key = pathInfo.GetArgumentValue<string>(2);

            var configurationType = _configurationManager.GetConfigurationType(key);
            var configuration = _jsonSerializer.DeserializeFromStream(request.RequestStream, configurationType);
            
            _configurationManager.SaveConfiguration(key, configuration);
        }

        public object Get(GetDefaultMetadataOptions request)
        {
            return ToOptimizedSerializedResultUsingCache(new MetadataOptions());
        }

        public object Get(GetMetadataPlugins request)
        {
            return ToOptimizedSerializedResultUsingCache(_providerManager.GetAllMetadataPlugins().ToList());
        }
    }
}
