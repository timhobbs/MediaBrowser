﻿using MediaBrowser.Common.IO;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Resolvers;
using System;
using System.IO;

namespace MediaBrowser.Server.Implementations.Library.Resolvers
{
    /// <summary>
    /// Class LocalTrailerResolver
    /// </summary>
    public class LocalTrailerResolver : BaseVideoResolver<Trailer>
    {
        private readonly IFileSystem _fileSystem;

        public LocalTrailerResolver(IFileSystem fileSystem)
        {
            _fileSystem = fileSystem;
        }

        /// <summary>
        /// Resolves the specified args.
        /// </summary>
        /// <param name="args">The args.</param>
        /// <returns>Trailer.</returns>
        protected override Trailer Resolve(ItemResolveArgs args)
        {
            // Trailers are not Children, therefore this can never happen
            if (args.Parent != null)
            {
                return null;
            }

            // If the file is within a trailers folder, see if the VideoResolver returns something
            if (!args.IsDirectory)
            {
                if (string.Equals(Path.GetFileName(Path.GetDirectoryName(args.Path)), BaseItem.TrailerFolderName, StringComparison.OrdinalIgnoreCase))
                {
                    return base.Resolve(args);
                }

                // Support xbmc local trailer convention, but only when looking for local trailers (hence the parent == null check)
                if (args.Parent == null && _fileSystem.GetFileNameWithoutExtension(args.Path).EndsWith(BaseItem.XbmcTrailerFileSuffix, StringComparison.OrdinalIgnoreCase))
                {
                    return base.Resolve(args);
                }
            }

            return null;
        }
    }
}
