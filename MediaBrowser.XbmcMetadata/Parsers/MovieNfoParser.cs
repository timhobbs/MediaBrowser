﻿using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using System.Collections.Generic;
using System.Threading;
using System.Xml;

namespace MediaBrowser.XbmcMetadata.Parsers
{
    class MovieNfoParser : BaseNfoParser<Video>
    {
        private List<ChapterInfo> _chaptersFound;

        public MovieNfoParser(ILogger logger, IConfigurationManager config) : base(logger, config)
        {
        }

        public void Fetch(Video item, 
            List<ChapterInfo> chapters, 
            string metadataFile, 
            CancellationToken cancellationToken)
        {
            _chaptersFound = chapters;

            Fetch(item, metadataFile, cancellationToken);
        }

        /// <summary>
        /// Fetches the data from XML node.
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <param name="item">The item.</param>
        protected override void FetchDataFromXmlNode(XmlReader reader, Video item)
        {
            switch (reader.Name)
            {
                case "id":
                    var id = reader.ReadElementContentAsString();
                    if (!string.IsNullOrWhiteSpace(id))
                    {
                        item.SetProviderId(MetadataProviders.Imdb, id);
                    }
                    break;

                case "set":
                    {
                        var val = reader.ReadElementContentAsString();
                        var movie = item as Movie;

                        if (!string.IsNullOrWhiteSpace(val) && movie != null)
                        {
                            movie.TmdbCollectionName = val;
                        }

                        break;
                    }

                case "artist":
                    {
                        var val = reader.ReadElementContentAsString();
                        var movie = item as MusicVideo;

                        if (!string.IsNullOrWhiteSpace(val) && movie != null)
                        {
                            movie.Artist = val;
                        }

                        break;
                    }

                case "album":
                    {
                        var val = reader.ReadElementContentAsString();
                        var movie = item as MusicVideo;

                        if (!string.IsNullOrWhiteSpace(val) && movie != null)
                        {
                            movie.Album = val;
                        }

                        break;
                    }

                //case "chapter":

                //    _chaptersFound.AddRange(FetchChaptersFromXmlNode(item, reader.ReadSubtree()));
                //    break;

                default:
                    base.FetchDataFromXmlNode(reader, item);
                    break;
            }
        }
    }
}
