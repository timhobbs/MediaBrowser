﻿using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using System;
using System.Xml;

namespace MediaBrowser.XbmcMetadata.Parsers
{
    public class SeriesNfoParser : BaseNfoParser<Series>
    {
        public SeriesNfoParser(ILogger logger, IConfigurationManager config) : base(logger, config)
        {
        }

        /// <summary>
        /// Fetches the data from XML node.
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <param name="item">The item.</param>
        protected override void FetchDataFromXmlNode(XmlReader reader, Series item)
        {
            switch (reader.Name)
            {
                case "id":
                    string id = reader.ReadElementContentAsString();
                    if (!string.IsNullOrWhiteSpace(id))
                    {
                        item.SetProviderId(MetadataProviders.Tvdb, id);
                    }
                    break;

                case "airs_dayofweek":
                    {
                        item.AirDays = TVUtils.GetAirDays(reader.ReadElementContentAsString());
                        break;
                    }

                case "airs_time":
                    {
                        var val = reader.ReadElementContentAsString();

                        if (!string.IsNullOrWhiteSpace(val))
                        {
                            item.AirTime = val;
                        }
                        break;
                    }

                case "animeseriesindex":
                    {
                        var number = reader.ReadElementContentAsString();

                        if (!string.IsNullOrWhiteSpace(number))
                        {
                            int num;

                            if (int.TryParse(number, out num))
                            {
                                item.AnimeSeriesIndex = num;
                            }
                        }
                        break;
                    }

                case "status":
                    {
                        var status = reader.ReadElementContentAsString();

                        if (!string.IsNullOrWhiteSpace(status))
                        {
                            SeriesStatus seriesStatus;
                            if (Enum.TryParse(status, true, out seriesStatus))
                            {
                                item.Status = seriesStatus;
                            }
                            else
                            {
                                Logger.Info("Unrecognized series status: " + status);
                            }
                        }

                        break;
                    }

                default:
                    base.FetchDataFromXmlNode(reader, item);
                    break;
            }
        }
    }
}
