﻿using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Entities;
using System.IO;

namespace MediaBrowser.Server.Implementations.Collections
{
    public class CollectionsDynamicFolder : IVirtualFolderCreator
    {
        private readonly IApplicationPaths _appPaths;

        public CollectionsDynamicFolder(IApplicationPaths appPaths)
        {
            _appPaths = appPaths;
        }

        public BasePluginFolder GetFolder()
        {
            var path = Path.Combine(_appPaths.DataPath, "collections");

            Directory.CreateDirectory(path);

            return new ManualCollectionsFolder
            {
                Path = path
            };
        }
    }
}
