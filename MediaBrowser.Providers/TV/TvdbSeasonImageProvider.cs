﻿using MediaBrowser.Common.IO;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace MediaBrowser.Providers.TV
{
    public class TvdbSeasonImageProvider : IRemoteImageProvider, IHasOrder, IHasChangeMonitor
    {
        private static readonly CultureInfo UsCulture = new CultureInfo("en-US");

        private readonly IServerConfigurationManager _config;
        private readonly IHttpClient _httpClient;
        private readonly IFileSystem _fileSystem;

        public TvdbSeasonImageProvider(IServerConfigurationManager config, IHttpClient httpClient, IFileSystem fileSystem)
        {
            _config = config;
            _httpClient = httpClient;
            _fileSystem = fileSystem;
        }

        public string Name
        {
            get { return ProviderName; }
        }

        public static string ProviderName
        {
            get { return "TheTVDB"; }
        }

        public bool Supports(IHasImages item)
        {
            return item is Season;
        }

        public IEnumerable<ImageType> GetSupportedImages(IHasImages item)
        {
            return new List<ImageType>
            {
                ImageType.Primary, 
                ImageType.Banner,
                ImageType.Backdrop
            };
        }

        public async Task<IEnumerable<RemoteImageInfo>> GetImages(IHasImages item, CancellationToken cancellationToken)
        {
            var season = (Season)item;
            var series = season.Series;

            var identity = season.Identities.OfType<SeasonIdentity>().FirstOrDefault(id => id.Type == MetadataProviders.Tvdb.ToString());
            var seriesId = identity != null ? identity.SeriesId : null;

            if (!string.IsNullOrEmpty(seriesId) && season.IndexNumber.HasValue)
            {
                await TvdbSeriesProvider.Current.EnsureSeriesInfo(seriesId, series.GetPreferredMetadataLanguage(), cancellationToken).ConfigureAwait(false);

                // Process images
                var seriesDataPath = TvdbSeriesProvider.GetSeriesDataPath(_config.ApplicationPaths, seriesId);

                var path = Path.Combine(seriesDataPath, "banners.xml");

                try
                {
                    int seasonNumber = AdjustForSeriesOffset(series, identity.SeasonIndex);
                    return GetImages(path, item.GetPreferredMetadataLanguage(), seasonNumber, cancellationToken);
                }
                catch (FileNotFoundException)
                {
                    // No tvdb data yet. Don't blow up
                }
            }

            return new RemoteImageInfo[] { };
        }

        private int AdjustForSeriesOffset(Series series, int seasonNumber)
        {
            var offset = TvdbSeriesProvider.GetSeriesOffset(series.ProviderIds);
            if (offset != null)
                return (int) (seasonNumber + offset);

            return seasonNumber;
        }

        internal static IEnumerable<RemoteImageInfo> GetImages(string xmlPath, string preferredLanguage, int seasonNumber, CancellationToken cancellationToken)
        {
            var settings = new XmlReaderSettings
            {
                CheckCharacters = false,
                IgnoreProcessingInstructions = true,
                IgnoreComments = true,
                ValidationType = ValidationType.None
            };

            var list = new List<RemoteImageInfo>();

            using (var streamReader = new StreamReader(xmlPath, Encoding.UTF8))
            {
                // Use XmlReader for best performance
                using (var reader = XmlReader.Create(streamReader, settings))
                {
                    reader.MoveToContent();

                    // Loop through each element
                    while (reader.Read())
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        if (reader.NodeType == XmlNodeType.Element)
                        {
                            switch (reader.Name)
                            {
                                case "Banner":
                                    {
                                        using (var subtree = reader.ReadSubtree())
                                        {
                                            AddImage(subtree, list, seasonNumber);
                                        }
                                        break;
                                    }
                                default:
                                    reader.Skip();
                                    break;
                            }
                        }
                    }
                }
            }

            var isLanguageEn = string.Equals(preferredLanguage, "en", StringComparison.OrdinalIgnoreCase);

            return list.OrderByDescending(i =>
                {
                    if (string.Equals(preferredLanguage, i.Language, StringComparison.OrdinalIgnoreCase))
                    {
                        return 3;
                    }
                    if (!isLanguageEn)
                    {
                        if (string.Equals("en", i.Language, StringComparison.OrdinalIgnoreCase))
                        {
                            return 2;
                        }
                    }
                    if (string.IsNullOrEmpty(i.Language))
                    {
                        return isLanguageEn ? 3 : 2;
                    }
                    return 0;
                })
                .ThenByDescending(i => i.CommunityRating ?? 0)
                .ThenByDescending(i => i.VoteCount ?? 0)
                .ToList();
        }

        private static void AddImage(XmlReader reader, List<RemoteImageInfo> images, int seasonNumber)
        {
            reader.MoveToContent();

            string bannerType = null;
            string bannerType2 = null;
            string url = null;
            int? bannerSeason = null;
            int? width = null;
            int? height = null;
            string language = null;
            double? rating = null;
            int? voteCount = null;
            string thumbnailUrl = null;

            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    switch (reader.Name)
                    {
                        case "Rating":
                            {
                                var val = reader.ReadElementContentAsString() ?? string.Empty;

                                double rval;

                                if (double.TryParse(val, NumberStyles.Any, UsCulture, out rval))
                                {
                                    rating = rval;
                                }

                                break;
                            }

                        case "RatingCount":
                            {
                                var val = reader.ReadElementContentAsString() ?? string.Empty;

                                int rval;

                                if (int.TryParse(val, NumberStyles.Integer, UsCulture, out rval))
                                {
                                    voteCount = rval;
                                }

                                break;
                            }

                        case "Language":
                            {
                                language = reader.ReadElementContentAsString() ?? string.Empty;
                                break;
                            }

                        case "ThumbnailPath":
                            {
                                thumbnailUrl = reader.ReadElementContentAsString() ?? string.Empty;
                                break;
                            }

                        case "BannerType":
                            {
                                bannerType = reader.ReadElementContentAsString() ?? string.Empty;
                                break;
                            }

                        case "BannerType2":
                            {
                                bannerType2 = reader.ReadElementContentAsString() ?? string.Empty;

                                // Sometimes the resolution is stuffed in here
                                var resolutionParts = bannerType2.Split('x');

                                if (resolutionParts.Length == 2)
                                {
                                    int rval;

                                    if (int.TryParse(resolutionParts[0], NumberStyles.Integer, UsCulture, out rval))
                                    {
                                        width = rval;
                                    }

                                    if (int.TryParse(resolutionParts[1], NumberStyles.Integer, UsCulture, out rval))
                                    {
                                        height = rval;
                                    }

                                }

                                break;
                            }

                        case "BannerPath":
                            {
                                url = reader.ReadElementContentAsString() ?? string.Empty;
                                break;
                            }

                        case "Season":
                            {
                                var val = reader.ReadElementContentAsString();

                                if (!string.IsNullOrWhiteSpace(val))
                                {
                                    bannerSeason = int.Parse(val);
                                }
                                break;
                            }


                        default:
                            reader.Skip();
                            break;
                    }
                }
            }

            if (!string.IsNullOrEmpty(url) && bannerSeason.HasValue && bannerSeason.Value == seasonNumber)
            {
                var imageInfo = new RemoteImageInfo
                {
                    RatingType = RatingType.Score,
                    CommunityRating = rating,
                    VoteCount = voteCount,
                    Url = TVUtils.BannerUrl + url,
                    ProviderName = ProviderName,
                    Language = language,
                    Width = width,
                    Height = height
                };

                if (!string.IsNullOrEmpty(thumbnailUrl))
                {
                    imageInfo.ThumbnailUrl = TVUtils.BannerUrl + thumbnailUrl;
                }

                if (string.Equals(bannerType, "season", StringComparison.OrdinalIgnoreCase))
                {
                    if (string.Equals(bannerType2, "season", StringComparison.OrdinalIgnoreCase))
                    {
                        imageInfo.Type = ImageType.Primary;
                        images.Add(imageInfo);
                    }
                    else if (string.Equals(bannerType2, "seasonwide", StringComparison.OrdinalIgnoreCase))
                    {
                        imageInfo.Type = ImageType.Banner;
                        images.Add(imageInfo);
                    }
                }
                else if (string.Equals(bannerType, "fanart", StringComparison.OrdinalIgnoreCase))
                {
                    imageInfo.Type = ImageType.Backdrop;
                    images.Add(imageInfo);
                }
            }

        }

        public int Order
        {
            get { return 0; }
        }

        public Task<HttpResponseInfo> GetImageResponse(string url, CancellationToken cancellationToken)
        {
            return _httpClient.GetResponse(new HttpRequestOptions
            {
                CancellationToken = cancellationToken,
                Url = url,
                ResourcePool = TvdbSeriesProvider.Current.TvDbResourcePool
            });
        }

        public bool HasChanged(IHasMetadata item, IDirectoryService directoryService, DateTime date)
        {
            if (item.LocationType != LocationType.Virtual)
            {
                // For non-virtual items, only enable if configured
                if (!_config.Configuration.EnableTvDbUpdates)
                {
                    return false;
                }
            }

            var season = (Season)item;
            var series = season.Series;

            if (series == null)
            {
                return false;
            }

            var tvdbId = series.GetProviderId(MetadataProviders.Tvdb);

            if (!String.IsNullOrEmpty(tvdbId))
            {
                // Process images
                var imagesXmlPath = Path.Combine(TvdbSeriesProvider.GetSeriesDataPath(_config.ApplicationPaths, tvdbId), "banners.xml");

                var fileInfo = new FileInfo(imagesXmlPath);

                return fileInfo.Exists && _fileSystem.GetLastWriteTimeUtc(fileInfo) > date;
            }

            return false;
        }
    }
}
