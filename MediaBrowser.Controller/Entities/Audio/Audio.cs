﻿using MediaBrowser.Controller.Persistence;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.MediaInfo;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace MediaBrowser.Controller.Entities.Audio
{
    /// <summary>
    /// Class Audio
    /// </summary>
    public class Audio : BaseItem,
        IHasAlbumArtist,
        IHasArtist,
        IHasMusicGenres,
        IHasLookupInfo<SongInfo>,
        IHasTags,
        IHasMediaSources
    {
        public string FormatName { get; set; }
        public long? Size { get; set; }
        public string Container { get; set; }
        public int? TotalBitrate { get; set; }
        public List<string> Tags { get; set; }

        public Audio()
        {
            Artists = new List<string>();
            Tags = new List<string>();
        }

        /// <summary>
        /// Gets or sets a value indicating whether this instance has embedded image.
        /// </summary>
        /// <value><c>true</c> if this instance has embedded image; otherwise, <c>false</c>.</value>
        public bool HasEmbeddedImage { get; set; }

        /// <summary>
        /// Override this to true if class should be grouped under a container in indicies
        /// The container class should be defined via IndexContainer
        /// </summary>
        /// <value><c>true</c> if [group in index]; otherwise, <c>false</c>.</value>
        [IgnoreDataMember]
        public override bool GroupInIndex
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        /// Override this to return the folder that should be used to construct a container
        /// for this item in an index.  GroupInIndex should be true as well.
        /// </summary>
        /// <value>The index container.</value>
        [IgnoreDataMember]
        public override Folder IndexContainer
        {
            get
            {
                return LatestItemsIndexContainer ?? new MusicAlbum { Name = "Unknown Album" };
            }
        }

        public override Folder LatestItemsIndexContainer
        {
            get
            {
                return Parents.OfType<MusicAlbum>().FirstOrDefault();
            }
        }

        /// <summary>
        /// Gets or sets the artist.
        /// </summary>
        /// <value>The artist.</value>
        public List<string> Artists { get; set; }

        [IgnoreDataMember]
        public List<string> AllArtists
        {
            get
            {
                var list = AlbumArtists;

                list.AddRange(Artists);

                return list;

            }
        }

        [IgnoreDataMember]
        public List<string> AlbumArtists
        {
            get
            {
                var list = new List<string>();

                if (!string.IsNullOrEmpty(AlbumArtist))
                {
                    list.Add(AlbumArtist);
                }

                return list;
            }
            set
            {
                AlbumArtist = value.FirstOrDefault();
            }
        }

        /// <summary>
        /// Gets or sets the album.
        /// </summary>
        /// <value>The album.</value>
        public string Album { get; set; }
        /// <summary>
        /// Gets or sets the album artist.
        /// </summary>
        /// <value>The album artist.</value>
        public string AlbumArtist { get; set; }

        /// <summary>
        /// Gets the type of the media.
        /// </summary>
        /// <value>The type of the media.</value>
        public override string MediaType
        {
            get
            {
                return Model.Entities.MediaType.Audio;
            }
        }

        /// <summary>
        /// Creates the name of the sort.
        /// </summary>
        /// <returns>System.String.</returns>
        protected override string CreateSortName()
        {
            return (ParentIndexNumber != null ? ParentIndexNumber.Value.ToString("0000 - ") : "")
                    + (IndexNumber != null ? IndexNumber.Value.ToString("0000 - ") : "") + Name;
        }

        /// <summary>
        /// Determines whether the specified name has artist.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns><c>true</c> if the specified name has artist; otherwise, <c>false</c>.</returns>
        public bool HasArtist(string name)
        {
            return AllArtists.Contains(name, StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Gets the user data key.
        /// </summary>
        /// <returns>System.String.</returns>
        public override string GetUserDataKey()
        {
            var parent = FindParent<MusicAlbum>();

            if (parent != null)
            {
                var parentKey = parent.GetUserDataKey();

                if (IndexNumber.HasValue)
                {
                    var songKey = (ParentIndexNumber != null ? ParentIndexNumber.Value.ToString("0000 - ") : "")
                                  + (IndexNumber.Value.ToString("0000 - "));

                    return parentKey + songKey;
                }
            }

            return base.GetUserDataKey();
        }

        protected override bool GetBlockUnratedValue(UserConfiguration config)
        {
            return config.BlockUnratedItems.Contains(UnratedItem.Music);
        }

        public SongInfo GetLookupInfo()
        {
            var info = GetItemLookupInfo<SongInfo>();

            info.AlbumArtists = AlbumArtists;
            info.Album = Album;
            info.Artists = Artists;

            return info;
        }

        public virtual IEnumerable<MediaSourceInfo> GetMediaSources(bool enablePathSubstitution)
        {
            var result = new List<MediaSourceInfo>
            {
                GetVersionInfo(this, enablePathSubstitution)
            };

            return result;
        }

        private static MediaSourceInfo GetVersionInfo(Audio i, bool enablePathSubstituion)
        {
            var locationType = i.LocationType;

            var info = new MediaSourceInfo
            {
                Id = i.Id.ToString("N"),
                Protocol = locationType == LocationType.Remote ? MediaProtocol.Http : MediaProtocol.File,
                MediaStreams = ItemRepository.GetMediaStreams(new MediaStreamQuery { ItemId = i.Id }).ToList(),
                Name = i.Name,
                Path = enablePathSubstituion ? GetMappedPath(i.Path, locationType) : i.Path,
                RunTimeTicks = i.RunTimeTicks,
                Container = i.Container,
                Size = i.Size,
                Formats = (i.FormatName ?? string.Empty).Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).ToList()
            };

            if (string.IsNullOrEmpty(info.Container))
            {
                if (!string.IsNullOrWhiteSpace(i.Path) && locationType != LocationType.Remote && locationType != LocationType.Virtual)
                {
                    info.Container = System.IO.Path.GetExtension(i.Path).TrimStart('.');
                }
            }

            var bitrate = i.TotalBitrate ??
                info.MediaStreams.Where(m => m.Type == MediaStreamType.Audio)
                .Select(m => m.BitRate ?? 0)
                .Sum();

            if (bitrate > 0)
            {
                info.Bitrate = bitrate;
            }

            return info;
        }
    }
}
