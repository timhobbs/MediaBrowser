﻿using MediaBrowser.Common.IO;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Library;
using MediaBrowser.XbmcMetadata.Configuration;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml;

namespace MediaBrowser.XbmcMetadata.Savers
{
    public class ArtistNfoSaver : BaseNfoSaver
    {
        public ArtistNfoSaver(IFileSystem fileSystem, IServerConfigurationManager configurationManager, ILibraryManager libraryManager, IUserManager userManager, IUserDataManager userDataManager)
            : base(fileSystem, configurationManager, libraryManager, userManager, userDataManager)
        {
        }

        public override string GetSavePath(IHasMetadata item)
        {
            return Path.Combine(item.Path, "artist.nfo");
        }

        protected override string GetRootElementName(IHasMetadata item)
        {
            return "artist";
        }

        public override bool IsEnabledFor(IHasMetadata item, ItemUpdateType updateType)
        {
            if (!item.SupportsLocalMetadata)
            {
                return false;
            }

            return item is MusicArtist && updateType >= ItemUpdateType.MetadataDownload;
        }

        protected override void WriteCustomElements(IHasMetadata item, XmlWriter writer)
        {
            var artist = (MusicArtist)item;

            if (artist.EndDate.HasValue)
            {
                var formatString = ConfigurationManager.GetNfoConfiguration().ReleaseDateFormat;

                writer.WriteElementString("disbanded", artist.EndDate.Value.ToString(formatString));
            }
            
            var albums = artist
                .RecursiveChildren
                .OfType<MusicAlbum>()
                .ToList();

            AddAlbums(albums, writer);
        }

        private static readonly CultureInfo UsCulture = new CultureInfo("en-US");
        
        private void AddAlbums(IEnumerable<MusicAlbum> albums, XmlWriter writer)
        {
            foreach (var album in albums)
            {
                writer.WriteStartElement("album");

                if (!string.IsNullOrEmpty(album.Name))
                {
                    writer.WriteElementString("title", album.Name);
                }

                if (album.ProductionYear.HasValue)
                {
                    writer.WriteElementString("year", album.ProductionYear.Value.ToString(UsCulture));
                }

                writer.WriteEndElement();
            }
        }

        protected override List<string> GetTagsUsed()
        {
            var list = new List<string>
            {
                    "album",
                    "disbanded"
            };

            return list;
        }
    }
}