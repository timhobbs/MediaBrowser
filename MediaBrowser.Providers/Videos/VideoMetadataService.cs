﻿using MediaBrowser.Common.IO;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Providers.Manager;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Providers.Videos
{
    public class VideoMetadataService : MetadataService<Video, ItemLookupInfo>
    {
        private readonly ILibraryManager _libraryManager;

        public VideoMetadataService(IServerConfigurationManager serverConfigurationManager, ILogger logger, IProviderManager providerManager, IProviderRepository providerRepo, IFileSystem fileSystem, ILibraryManager libraryManager)
            : base(serverConfigurationManager, logger, providerManager, providerRepo, fileSystem)
        {
            _libraryManager = libraryManager;
        }

        /// <summary>
        /// Merges the specified source.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="target">The target.</param>
        /// <param name="lockedFields">The locked fields.</param>
        /// <param name="replaceData">if set to <c>true</c> [replace data].</param>
        /// <param name="mergeMetadataSettings">if set to <c>true</c> [merge metadata settings].</param>
        protected override void MergeData(Video source, Video target, List<MetadataFields> lockedFields, bool replaceData, bool mergeMetadataSettings)
        {
            ProviderUtils.MergeBaseItemData(source, target, lockedFields, replaceData, mergeMetadataSettings);
        }

        public override int Order
        {
            get
            {
                // Make sure the type-specific services get picked first
                return 10;
            }
        }
    }
}
