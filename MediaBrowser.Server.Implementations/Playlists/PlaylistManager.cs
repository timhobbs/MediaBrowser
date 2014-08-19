﻿using MediaBrowser.Common.IO;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Playlists;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Server.Implementations.Playlists
{
    public class PlaylistManager : IPlaylistManager
    {
        private readonly ILibraryManager _libraryManager;
        private readonly IFileSystem _fileSystem;
        private readonly ILibraryMonitor _iLibraryMonitor;
        private readonly ILogger _logger;
        private readonly IUserManager _userManager;

        public PlaylistManager(ILibraryManager libraryManager, IFileSystem fileSystem, ILibraryMonitor iLibraryMonitor, ILogger logger, IUserManager userManager)
        {
            _libraryManager = libraryManager;
            _fileSystem = fileSystem;
            _iLibraryMonitor = iLibraryMonitor;
            _logger = logger;
            _userManager = userManager;
        }

        public IEnumerable<Playlist> GetPlaylists(string userId)
        {
            var user = _userManager.GetUserById(new Guid(userId));

            return GetPlaylistsFolder(userId).GetChildren(user, true).OfType<Playlist>();
        }

        public async Task<Playlist> CreatePlaylist(PlaylistCreationOptions options)
        {
            var name = options.Name;

            var folderName = _fileSystem.GetValidFilename(name) + " [playlist]";

            var parentFolder = GetPlaylistsFolder(null);

            if (parentFolder == null)
            {
                throw new ArgumentException();
            }

            if (string.IsNullOrWhiteSpace(options.MediaType))
            {
                foreach (var itemId in options.ItemIdList)
                {
                    var item = _libraryManager.GetItemById(itemId);

                    if (item == null)
                    {
                        throw new ArgumentException("No item exists with the supplied Id");
                    }

                    if (!string.IsNullOrWhiteSpace(item.MediaType))
                    {
                        options.MediaType = item.MediaType;
                    }
                    else if (item is MusicArtist || item is MusicAlbum || item is MusicGenre)
                    {
                        options.MediaType = MediaType.Audio;
                    }
                    else if (item is Genre)
                    {
                        options.MediaType = MediaType.Video;
                    }
                    else
                    {
                        var folder = item as Folder;
                        if (folder != null)
                        {
                            options.MediaType = folder.GetRecursiveChildren()
                                .Where(i => !i.IsFolder)
                                .Select(i => i.MediaType)
                                .FirstOrDefault(i => !string.IsNullOrWhiteSpace(i));
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(options.MediaType))
                    {
                        break;
                    }
                }
            }

            if (string.IsNullOrWhiteSpace(options.MediaType))
            {
                throw new ArgumentException("A playlist media type is required.");
            }

            var path = Path.Combine(parentFolder.Path, folderName);
            path = GetTargetPath(path);

            _iLibraryMonitor.ReportFileSystemChangeBeginning(path);

            try
            {
                Directory.CreateDirectory(path);

                var playlist = new Playlist
                {
                    Name = name,
                    Parent = parentFolder,
                    Path = path,
                    OwnerUserId = options.UserId
                };

                playlist.SetMediaType(options.MediaType);

                await parentFolder.AddChild(playlist, CancellationToken.None).ConfigureAwait(false);

                await playlist.RefreshMetadata(new MetadataRefreshOptions { ForceSave = true }, CancellationToken.None)
                    .ConfigureAwait(false);

                if (options.ItemIdList.Count > 0)
                {
                    await AddToPlaylist(playlist.Id.ToString("N"), options.ItemIdList);
                }

                return playlist;
            }
            finally
            {
                // Refresh handled internally
                _iLibraryMonitor.ReportFileSystemChangeComplete(path, false);
            }
        }

        private string GetTargetPath(string path)
        {
            while (Directory.Exists(path))
            {
                path += "1";
            }

            return path;
        }

        private IEnumerable<BaseItem> GetPlaylistItems(IEnumerable<string> itemIds, string playlistMediaType)
        {
            var items = itemIds.Select(i => _libraryManager.GetItemById(i)).Where(i => i != null);

            return Playlist.GetPlaylistItems(playlistMediaType, items, null);
        }

        public async Task AddToPlaylist(string playlistId, IEnumerable<string> itemIds)
        {
            var playlist = _libraryManager.GetItemById(playlistId) as Playlist;

            if (playlist == null)
            {
                throw new ArgumentException("No Playlist exists with the supplied Id");
            }

            var list = new List<LinkedChild>();
            var itemList = new List<BaseItem>();

            var items = GetPlaylistItems(itemIds, playlist.MediaType).ToList();

            foreach (var item in items)
            {
                itemList.Add(item);
                list.Add(LinkedChild.Create(item));
            }

            playlist.LinkedChildren.AddRange(list);

            await playlist.UpdateToRepository(ItemUpdateType.MetadataEdit, CancellationToken.None).ConfigureAwait(false);
            await playlist.RefreshMetadata(new MetadataRefreshOptions{
            
                ForceSave = true

            }, CancellationToken.None).ConfigureAwait(false);
        }

        public async Task RemoveFromPlaylist(string playlistId, IEnumerable<string> entryIds)
        {
            var playlist = _libraryManager.GetItemById(playlistId) as Playlist;

            if (playlist == null)
            {
                throw new ArgumentException("No Playlist exists with the supplied Id");
            }

            var children = playlist.GetManageableItems().ToList();

            var idList = entryIds.ToList();

            var removals = children.Where(i => idList.Contains(i.Item1.Id));

            playlist.LinkedChildren = children.Except(removals)
                .Select(i => i.Item1)
                .ToList();

            await playlist.UpdateToRepository(ItemUpdateType.MetadataEdit, CancellationToken.None).ConfigureAwait(false);
            await playlist.RefreshMetadata(new MetadataRefreshOptions
            {
                ForceSave = true
            }, CancellationToken.None).ConfigureAwait(false);
        }

        public Folder GetPlaylistsFolder(string userId)
        {
            return _libraryManager.RootFolder.Children.OfType<PlaylistsFolder>()
                .FirstOrDefault();
        }
    }
}
