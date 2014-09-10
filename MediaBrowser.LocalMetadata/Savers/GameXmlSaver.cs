﻿using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security;
using System.Text;
using System.Threading;

namespace MediaBrowser.LocalMetadata.Savers
{
    /// <summary>
    /// Saves game.xml for games
    /// </summary>
    public class GameXmlSaver : IMetadataFileSaver
    {
        public string Name
        {
            get
            {
                return "Media Browser Xml";
            }
        }

        private readonly IServerConfigurationManager _config;

        public GameXmlSaver(IServerConfigurationManager config)
        {
            _config = config;
        }
        
        /// <summary>
        /// Determines whether [is enabled for] [the specified item].
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="updateType">Type of the update.</param>
        /// <returns><c>true</c> if [is enabled for] [the specified item]; otherwise, <c>false</c>.</returns>
        public bool IsEnabledFor(IHasMetadata item, ItemUpdateType updateType)
        {
            if (!item.SupportsLocalMetadata)
            {
                return false;
            }

            return item is Game && updateType >= ItemUpdateType.MetadataDownload;
        }

        private static readonly CultureInfo UsCulture = new CultureInfo("en-US");

        /// <summary>
        /// Saves the specified item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        public void Save(IHasMetadata item, CancellationToken cancellationToken)
        {
            var builder = new StringBuilder();

            builder.Append("<Item>");

            var game = (Game)item;

            if (game.PlayersSupported.HasValue)
            {
                builder.Append("<Players>" + SecurityElement.Escape(game.PlayersSupported.Value.ToString(UsCulture)) + "</Players>");
            }

            if (!string.IsNullOrEmpty(game.GameSystem))
            {
                builder.Append("<GameSystem>" + SecurityElement.Escape(game.GameSystem) + "</GameSystem>");
            }

            var val = game.GetProviderId(MetadataProviders.NesBox);

            if (!string.IsNullOrEmpty(val))
            {
                builder.Append("<NesBox>" + SecurityElement.Escape(val) + "</NesBox>");
            }

            val = game.GetProviderId(MetadataProviders.NesBoxRom);

            if (!string.IsNullOrEmpty(val))
            {
                builder.Append("<NesBoxRom>" + SecurityElement.Escape(val) + "</NesBoxRom>");
            }

            XmlSaverHelpers.AddCommonNodes(game, builder);

            builder.Append("</Item>");

            var xmlFilePath = GetSavePath(item);

            XmlSaverHelpers.Save(builder, xmlFilePath, new List<string>
                {
                    "Players",
                    "GameSystem",
                    "NesBox",
                    "NesBoxRom"
                }, _config);
        }

        public string GetSavePath(IHasMetadata item)
        {
            return GetGameSavePath((Game)item);
        }

        public static string GetGameSavePath(Game item)
        {
            if (item.IsInMixedFolder)
            {
                return Path.ChangeExtension(item.Path, ".xml");
            }

            return Path.Combine(item.ContainingFolderPath, "game.xml");
        }
    }
}
