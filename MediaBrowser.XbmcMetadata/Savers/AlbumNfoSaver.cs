﻿using MediaBrowser.Common.IO;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml;

namespace MediaBrowser.XbmcMetadata.Savers
{
    public class AlbumNfoSaver : BaseNfoSaver
    {
        public AlbumNfoSaver(IFileSystem fileSystem, IServerConfigurationManager configurationManager, ILibraryManager libraryManager, IUserManager userManager, IUserDataManager userDataManager, ILogger logger) : base(fileSystem, configurationManager, libraryManager, userManager, userDataManager, logger)
        {
        }

        public override string GetSavePath(IHasMetadata item)
        {
            return Path.Combine(item.Path, "album.nfo");
        }

        protected override string GetRootElementName(IHasMetadata item)
        {
            return "album";
        }

        public override bool IsEnabledFor(IHasMetadata item, ItemUpdateType updateType)
        {
            if (!item.SupportsLocalMetadata)
            {
                return false;
            }

            return item is MusicAlbum && updateType >= ItemUpdateType.MetadataDownload;
        }

        protected override void WriteCustomElements(IHasMetadata item, XmlWriter writer)
        {
            var album = (MusicAlbum)item;
            
            var tracks = album.Tracks
                .ToList();

            var artists = tracks
                .SelectMany(i =>
                {
                    var list = new List<string>();

                    if (!string.IsNullOrEmpty(i.AlbumArtist))
                    {
                        list.Add(i.AlbumArtist);
                    }
                    list.AddRange(i.Artists);

                    return list;
                })
                .Distinct(StringComparer.OrdinalIgnoreCase);

            foreach (var artist in artists)
            {
                writer.WriteElementString("artist", artist);
            }

            AddTracks(tracks, writer);
        }        
        
        private static readonly CultureInfo UsCulture = new CultureInfo("en-US");

        private void AddTracks(IEnumerable<Audio> tracks, XmlWriter writer)
        {
            foreach (var track in tracks.OrderBy(i => i.ParentIndexNumber ?? 0).ThenBy(i => i.IndexNumber ?? 0))
            {
                writer.WriteStartElement("track");

                if (track.IndexNumber.HasValue)
                {
                    writer.WriteElementString("position", track.IndexNumber.Value.ToString(UsCulture));
                }

                if (!string.IsNullOrEmpty(track.Name))
                {
                    writer.WriteElementString("title", track.Name);
                }

                if (track.RunTimeTicks.HasValue)
                {
                    var time = TimeSpan.FromTicks(track.RunTimeTicks.Value).ToString(@"mm\:ss");

                    writer.WriteElementString("duration", time);
                }

                writer.WriteEndElement();
            }
        }

        protected override List<string> GetTagsUsed()
        {
            var list = new List<string>
            {
                    "track",
                    "artist"
            };

            return list;
        }
    }
}
