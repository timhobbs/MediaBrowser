﻿using MediaBrowser.Common.Progress;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Querying;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Controller.Entities.Movies
{
    /// <summary>
    /// Class BoxSet
    /// </summary>
    public class BoxSet : Folder, IHasTrailers, IHasKeywords, IHasPreferredMetadataLanguage, IHasDisplayOrder, IHasLookupInfo<BoxSetInfo>, IMetadataContainer
    {
        public BoxSet()
        {
            RemoteTrailers = new List<MediaUrl>();
            LocalTrailerIds = new List<Guid>();

            DisplayOrder = ItemSortBy.PremiereDate;
            Keywords = new List<string>();
        }

        protected override bool FilterLinkedChildrenPerUser
        {
            get
            {
                return true;
            }
        }

        public List<Guid> LocalTrailerIds { get; set; }

        /// <summary>
        /// Gets or sets the remote trailers.
        /// </summary>
        /// <value>The remote trailers.</value>
        public List<MediaUrl> RemoteTrailers { get; set; }

        /// <summary>
        /// Gets or sets the tags.
        /// </summary>
        /// <value>The tags.</value>
        public List<string> Keywords { get; set; }

        public string PreferredMetadataLanguage { get; set; }

        /// <summary>
        /// Gets or sets the preferred metadata country code.
        /// </summary>
        /// <value>The preferred metadata country code.</value>
        public string PreferredMetadataCountryCode { get; set; }

        /// <summary>
        /// Gets or sets the display order.
        /// </summary>
        /// <value>The display order.</value>
        public string DisplayOrder { get; set; }

        protected override bool GetBlockUnratedValue(UserConfiguration config)
        {
            return config.BlockUnratedItems.Contains(UnratedItem.Movie);
        }

        [IgnoreDataMember]
        public override bool IsPreSorted
        {
            get
            {
                return true;
            }
        }

        public override IEnumerable<BaseItem> GetChildren(User user, bool includeLinkedChildren)
        {
            var children = base.GetChildren(user, includeLinkedChildren);

            if (string.Equals(DisplayOrder, ItemSortBy.SortName, StringComparison.OrdinalIgnoreCase))
            {
                // Sort by name
                return LibraryManager.Sort(children, user, new[] { ItemSortBy.SortName }, SortOrder.Ascending);
            }

            if (string.Equals(DisplayOrder, ItemSortBy.PremiereDate, StringComparison.OrdinalIgnoreCase))
            {
                // Sort by release date
                return LibraryManager.Sort(children, user, new[] { ItemSortBy.ProductionYear, ItemSortBy.PremiereDate, ItemSortBy.SortName }, SortOrder.Ascending);
            }

            // Default sorting
            return LibraryManager.Sort(children, user, new[] { ItemSortBy.ProductionYear, ItemSortBy.PremiereDate, ItemSortBy.SortName }, SortOrder.Ascending);
        }

        public BoxSetInfo GetLookupInfo()
        {
            return GetItemLookupInfo<BoxSetInfo>();
        }

        public async Task RefreshAllMetadata(MetadataRefreshOptions refreshOptions, IProgress<double> progress, CancellationToken cancellationToken)
        {
            // Refresh bottom up, children first, then the boxset
            // By then hopefully the  movies within will have Tmdb collection values
            var items = RecursiveChildren.ToList();

            var totalItems = items.Count;
            var percentages = new Dictionary<Guid, double>(totalItems);

            // Refresh songs
            foreach (var item in items)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var innerProgress = new ActionableProgress<double>();

                // Avoid implicitly captured closure
                var currentChild = item;
                innerProgress.RegisterAction(p =>
                {
                    lock (percentages)
                    {
                        percentages[currentChild.Id] = p / 100;

                        var percent = percentages.Values.Sum();
                        percent /= totalItems;
                        percent *= 100;
                        progress.Report(percent);
                    }
                });

                // Avoid implicitly captured closure
                await RefreshItem(item, refreshOptions, innerProgress, cancellationToken).ConfigureAwait(false);
            }

            // Refresh current item
            await RefreshMetadata(refreshOptions, cancellationToken).ConfigureAwait(false);

            progress.Report(100);
        }

        private async Task RefreshItem(BaseItem item, MetadataRefreshOptions refreshOptions, IProgress<double> progress, CancellationToken cancellationToken)
        {
            await item.RefreshMetadata(refreshOptions, cancellationToken).ConfigureAwait(false);

            progress.Report(100);
        }
    }
}
