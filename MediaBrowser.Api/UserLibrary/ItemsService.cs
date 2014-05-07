﻿using MediaBrowser.Controller.Collections;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Localization;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Querying;
using ServiceStack;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace MediaBrowser.Api.UserLibrary
{
    /// <summary>
    /// Class GetItems
    /// </summary>
    [Route("/Items", "GET", Summary = "Gets items based on a query.")]
    [Route("/Users/{UserId}/Items", "GET", Summary = "Gets items based on a query.")]
    public class GetItems : BaseItemsRequest, IReturn<ItemsResult>
    {
        /// <summary>
        /// Gets or sets the user id.
        /// </summary>
        /// <value>The user id.</value>
        [ApiMember(Name = "UserId", Description = "User Id", IsRequired = false, DataType = "string", ParameterType = "path", Verb = "GET")]
        public Guid? UserId { get; set; }

        /// <summary>
        /// Limit results to items containing a specific person
        /// </summary>
        /// <value>The person.</value>
        [ApiMember(Name = "Person", Description = "Optional. If specified, results will be filtered to include only those containing the specified person.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string Person { get; set; }

        /// <summary>
        /// If the Person filter is used, this can also be used to restrict to a specific person type
        /// </summary>
        /// <value>The type of the person.</value>
        [ApiMember(Name = "PersonTypes", Description = "Optional. If specified, along with Person, results will be filtered to include only those containing the specified person and PersonType. Allows multiple, comma-delimited", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string PersonTypes { get; set; }

        /// <summary>
        /// Limit results to items containing specific genres
        /// </summary>
        /// <value>The genres.</value>
        [ApiMember(Name = "Genres", Description = "Optional. If specified, results will be filtered based on genre. This allows multiple, pipe delimeted.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET", AllowMultiple = true)]
        public string Genres { get; set; }

        [ApiMember(Name = "AllGenres", Description = "Optional. If specified, results will be filtered based on genre. This allows multiple, pipe delimeted.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET", AllowMultiple = true)]
        public string AllGenres { get; set; }

        /// <summary>
        /// Limit results to items containing specific studios
        /// </summary>
        /// <value>The studios.</value>
        [ApiMember(Name = "Studios", Description = "Optional. If specified, results will be filtered based on studio. This allows multiple, pipe delimeted.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET", AllowMultiple = true)]
        public string Studios { get; set; }

        /// <summary>
        /// Gets or sets the studios.
        /// </summary>
        /// <value>The studios.</value>
        [ApiMember(Name = "Artists", Description = "Optional. If specified, results will be filtered based on artist. This allows multiple, pipe delimeted.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET", AllowMultiple = true)]
        public string Artists { get; set; }

        [ApiMember(Name = "Albums", Description = "Optional. If specified, results will be filtered based on album. This allows multiple, pipe delimeted.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET", AllowMultiple = true)]
        public string Albums { get; set; }

        /// <summary>
        /// Limit results to items containing specific years
        /// </summary>
        /// <value>The years.</value>
        [ApiMember(Name = "Years", Description = "Optional. If specified, results will be filtered based on production year. This allows multiple, comma delimeted.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET", AllowMultiple = true)]
        public string Years { get; set; }

        /// <summary>
        /// Gets or sets the item ids.
        /// </summary>
        /// <value>The item ids.</value>
        [ApiMember(Name = "Ids", Description = "Optional. If specific items are needed, specify a list of item id's to retrieve. This allows multiple, comma delimited.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET", AllowMultiple = true)]
        public string Ids { get; set; }

        /// <summary>
        /// Gets or sets the video types.
        /// </summary>
        /// <value>The video types.</value>
        [ApiMember(Name = "VideoTypes", Description = "Optional filter by VideoType (videofile, dvd, bluray, iso). Allows multiple, comma delimeted.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET", AllowMultiple = true)]
        public string VideoTypes { get; set; }

        /// <summary>
        /// Gets or sets the video formats.
        /// </summary>
        /// <value>The video formats.</value>
        [ApiMember(Name = "Is3D", Description = "Optional filter by items that are 3D, or not.", IsRequired = false, DataType = "bool", ParameterType = "query", Verb = "GET")]
        public bool? Is3D { get; set; }

        /// <summary>
        /// Gets or sets the series status.
        /// </summary>
        /// <value>The series status.</value>
        [ApiMember(Name = "SeriesStatus", Description = "Optional filter by Series Status. Allows multiple, comma delimeted.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET", AllowMultiple = true)]
        public string SeriesStatus { get; set; }

        [ApiMember(Name = "NameStartsWithOrGreater", Description = "Optional filter by items whose name is sorted equally or greater than a given input string.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string NameStartsWithOrGreater { get; set; }

        [ApiMember(Name = "NameStartsWith", Description = "Optional filter by items whose name is sorted equally than a given input string.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string NameStartsWith { get; set; }

        [ApiMember(Name = "NameLessThan", Description = "Optional filter by items whose name is equally or lesser than a given input string.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string NameLessThan { get; set; }

        [ApiMember(Name = "AlbumArtistStartsWithOrGreater", Description = "Optional filter by items whose album artist is sorted equally or greater than a given input string.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string AlbumArtistStartsWithOrGreater { get; set; }

        /// <summary>
        /// Gets or sets the air days.
        /// </summary>
        /// <value>The air days.</value>
        [ApiMember(Name = "AirDays", Description = "Optional filter by Series Air Days. Allows multiple, comma delimeted.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET", AllowMultiple = true)]
        public string AirDays { get; set; }

        /// <summary>
        /// Gets or sets the min offical rating.
        /// </summary>
        /// <value>The min offical rating.</value>
        [ApiMember(Name = "MinOfficialRating", Description = "Optional filter by minimum official rating (PG, PG-13, TV-MA, etc).", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string MinOfficialRating { get; set; }

        /// <summary>
        /// Gets or sets the max offical rating.
        /// </summary>
        /// <value>The max offical rating.</value>
        [ApiMember(Name = "MaxOfficialRating", Description = "Optional filter by maximum official rating (PG, PG-13, TV-MA, etc).", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string MaxOfficialRating { get; set; }

        [ApiMember(Name = "HasThemeSong", Description = "Optional filter by items with theme songs.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public bool? HasThemeSong { get; set; }

        [ApiMember(Name = "HasThemeVideo", Description = "Optional filter by items with theme videos.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public bool? HasThemeVideo { get; set; }

        [ApiMember(Name = "HasSubtitles", Description = "Optional filter by items with subtitles.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public bool? HasSubtitles { get; set; }

        [ApiMember(Name = "HasSpecialFeature", Description = "Optional filter by items with special features.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public bool? HasSpecialFeature { get; set; }

        [ApiMember(Name = "HasTrailer", Description = "Optional filter by items with trailers.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public bool? HasTrailer { get; set; }

        [ApiMember(Name = "AdjacentTo", Description = "Optional. Return items that are siblings of a supplied item.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string AdjacentTo { get; set; }

        [ApiMember(Name = "MinIndexNumber", Description = "Optional filter by minimum index number.", IsRequired = false, DataType = "int", ParameterType = "query", Verb = "GET")]
        public int? MinIndexNumber { get; set; }

        [ApiMember(Name = "MinPlayers", Description = "Optional filter by minimum number of game players.", IsRequired = false, DataType = "int", ParameterType = "query", Verb = "GET")]
        public int? MinPlayers { get; set; }

        [ApiMember(Name = "MaxPlayers", Description = "Optional filter by maximum number of game players.", IsRequired = false, DataType = "int", ParameterType = "query", Verb = "GET")]
        public int? MaxPlayers { get; set; }

        [ApiMember(Name = "ParentIndexNumber", Description = "Optional filter by parent index number.", IsRequired = false, DataType = "int", ParameterType = "query", Verb = "GET")]
        public int? ParentIndexNumber { get; set; }

        [ApiMember(Name = "HasParentalRating", Description = "Optional filter by items that have or do not have a parental rating", IsRequired = false, DataType = "bool", ParameterType = "query", Verb = "GET")]
        public bool? HasParentalRating { get; set; }

        [ApiMember(Name = "IsHD", Description = "Optional filter by items that are HD or not.", IsRequired = false, DataType = "bool", ParameterType = "query", Verb = "GET")]
        public bool? IsHD { get; set; }

        [ApiMember(Name = "LocationTypes", Description = "Optional. If specified, results will be filtered based on LocationType. This allows multiple, comma delimeted.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET", AllowMultiple = true)]
        public string LocationTypes { get; set; }

        [ApiMember(Name = "ExcludeLocationTypes", Description = "Optional. If specified, results will be filtered based on LocationType. This allows multiple, comma delimeted.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET", AllowMultiple = true)]
        public string ExcludeLocationTypes { get; set; }

        public bool IncludeIndexContainers { get; set; }

        [ApiMember(Name = "IsMissing", Description = "Optional filter by items that are missing episodes or not.", IsRequired = false, DataType = "bool", ParameterType = "query", Verb = "GET")]
        public bool? IsMissing { get; set; }

        [ApiMember(Name = "IsUnaired", Description = "Optional filter by items that are unaired episodes or not.", IsRequired = false, DataType = "bool", ParameterType = "query", Verb = "GET")]
        public bool? IsUnaired { get; set; }

        [ApiMember(Name = "IsVirtualUnaired", Description = "Optional filter by items that are virtual unaired episodes or not.", IsRequired = false, DataType = "bool", ParameterType = "query", Verb = "GET")]
        public bool? IsVirtualUnaired { get; set; }

        [ApiMember(Name = "MinCommunityRating", Description = "Optional filter by minimum community rating.", IsRequired = false, DataType = "int", ParameterType = "query", Verb = "GET")]
        public double? MinCommunityRating { get; set; }

        [ApiMember(Name = "MinCriticRating", Description = "Optional filter by minimum critic rating.", IsRequired = false, DataType = "int", ParameterType = "query", Verb = "GET")]
        public double? MinCriticRating { get; set; }

        [ApiMember(Name = "AiredDuringSeason", Description = "Gets all episodes that aired during a season, including specials.", IsRequired = false, DataType = "int", ParameterType = "query", Verb = "GET")]
        public int? AiredDuringSeason { get; set; }

        [ApiMember(Name = "MinPremiereDate", Description = "Optional. The minimum premiere date. Format = ISO", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "POST")]
        public string MinPremiereDate { get; set; }

        [ApiMember(Name = "MaxPremiereDate", Description = "Optional. The maximum premiere date. Format = ISO", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "POST")]
        public string MaxPremiereDate { get; set; }

        [ApiMember(Name = "HasOverview", Description = "Optional filter by items that have an overview or not.", IsRequired = false, DataType = "bool", ParameterType = "query", Verb = "GET")]
        public bool? HasOverview { get; set; }

        [ApiMember(Name = "HasImdbId", Description = "Optional filter by items that have an imdb id or not.", IsRequired = false, DataType = "bool", ParameterType = "query", Verb = "GET")]
        public bool? HasImdbId { get; set; }

        [ApiMember(Name = "HasTmdbId", Description = "Optional filter by items that have a tmdb id or not.", IsRequired = false, DataType = "bool", ParameterType = "query", Verb = "GET")]
        public bool? HasTmdbId { get; set; }

        [ApiMember(Name = "HasTvdbId", Description = "Optional filter by items that have a tvdb id or not.", IsRequired = false, DataType = "bool", ParameterType = "query", Verb = "GET")]
        public bool? HasTvdbId { get; set; }

        [ApiMember(Name = "IsYearMismatched", Description = "Optional filter by items that are potentially misidentified.", IsRequired = false, DataType = "bool", ParameterType = "query", Verb = "GET")]
        public bool? IsYearMismatched { get; set; }

        [ApiMember(Name = "IsInBoxSet", Description = "Optional filter by items that are in boxsets, or not.", IsRequired = false, DataType = "bool", ParameterType = "query", Verb = "GET")]
        public bool? IsInBoxSet { get; set; }

        [ApiMember(Name = "IsLocked", Description = "Optional filter by items that are locked.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public bool? IsLocked { get; set; }

        [ApiMember(Name = "IsUnidentified", Description = "Optional filter by items that are unidentified by internet metadata providers.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public bool? IsUnidentified { get; set; }

        [ApiMember(Name = "IsPlaceHolder", Description = "Optional filter by items that are placeholders", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public bool? IsPlaceHolder { get; set; }

        [ApiMember(Name = "HasOfficialRating", Description = "Optional filter by items that have official ratings", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public bool? HasOfficialRating { get; set; }

        [ApiMember(Name = "CollapseBoxSetItems", Description = "Whether or not to hide items behind their boxsets.", IsRequired = false, DataType = "bool", ParameterType = "query", Verb = "GET")]
        public bool? CollapseBoxSetItems { get; set; }
    }

    /// <summary>
    /// Class ItemsService
    /// </summary>
    public class ItemsService : BaseApiService
    {
        /// <summary>
        /// The _user manager
        /// </summary>
        private readonly IUserManager _userManager;
        private readonly IUserDataManager _userDataRepository;

        /// <summary>
        /// The _library manager
        /// </summary>
        private readonly ILibraryManager _libraryManager;
        private readonly ILocalizationManager _localization;

        private readonly IDtoService _dtoService;
        private readonly ICollectionManager _collectionManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="ItemsService" /> class.
        /// </summary>
        /// <param name="userManager">The user manager.</param>
        /// <param name="libraryManager">The library manager.</param>
        /// <param name="userDataRepository">The user data repository.</param>
        /// <param name="localization">The localization.</param>
        /// <param name="dtoService">The dto service.</param>
        /// <param name="collectionManager">The collection manager.</param>
        public ItemsService(IUserManager userManager, ILibraryManager libraryManager, IUserDataManager userDataRepository, ILocalizationManager localization, IDtoService dtoService, ICollectionManager collectionManager)
        {
            _userManager = userManager;
            _libraryManager = libraryManager;
            _userDataRepository = userDataRepository;
            _localization = localization;
            _dtoService = dtoService;
            _collectionManager = collectionManager;
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public object Get(GetItems request)
        {
            var result = GetItems(request);

            return ToOptimizedSerializedResultUsingCache(result);
        }

        /// <summary>
        /// Gets the items.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>Task{ItemsResult}.</returns>
        private ItemsResult GetItems(GetItems request)
        {
            var parentItem = string.IsNullOrEmpty(request.ParentId) ? null : _libraryManager.GetItemById(request.ParentId);
            var user = request.UserId.HasValue ? _userManager.GetUserById(request.UserId.Value) : null;

            var items = GetItemsToSerialize(request, user, parentItem);

            items = items.AsParallel();

            items = ApplyAdditionalFilters(request, items, user);

            // Apply filters
            // Run them starting with the ones that are likely to reduce the list the most
            foreach (var filter in request.GetFilters().OrderByDescending(f => (int)f))
            {
                items = ApplyFilter(items, filter, user, _userDataRepository);
            }

            items = FilterVirtualEpisodes(request, items, user);

            items = items.AsEnumerable();

            if (CollapseBoxSetItems(request, parentItem, user))
            {
                items = _collectionManager.CollapseItemsWithinBoxSets(items, user);
            }

            items = ApplySortOrder(request, items, user, _libraryManager);

            // This must be the last filter
            if (!string.IsNullOrEmpty(request.AdjacentTo))
            {
                items = FilterForAdjacency(items, request.AdjacentTo);
            }

            var itemsArray = items.ToList();

            var pagedItems = ApplyPaging(request, itemsArray);

            var fields = request.GetItemFields().ToList();

            var returnItems = pagedItems.Select(i => _dtoService.GetBaseItemDto(i, fields, user)).ToArray();

            return new ItemsResult
            {
                TotalRecordCount = itemsArray.Count,
                Items = returnItems
            };
        }

        private bool CollapseBoxSetItems(GetItems request, BaseItem parentItem, User user)
        {
            // Could end up stuck in a loop like this
            if (parentItem is BoxSet)
            {
                return false;
            }

            var param = request.CollapseBoxSetItems;

            if (!param.HasValue)
            {
                if (user != null && !user.Configuration.GroupMoviesIntoBoxSets)
                {
                    return false;
                }

                if (!string.IsNullOrWhiteSpace(request.IncludeItemTypes) &&
                    request.IncludeItemTypes.Split(',').Contains("Movie", StringComparer.OrdinalIgnoreCase))
                {
                    param = true;
                }
            }

            return param.HasValue && param.Value && AllowBoxSetCollapsing(request);
        }

        /// <summary>
        /// Gets the items to serialize.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="user">The user.</param>
        /// <param name="parentItem">The parent item.</param>
        /// <returns>IEnumerable{BaseItem}.</returns>
        /// <exception cref="System.InvalidOperationException"></exception>
        private IEnumerable<BaseItem> GetItemsToSerialize(GetItems request, User user, BaseItem parentItem)
        {
            var item = string.IsNullOrEmpty(request.ParentId) ?
                user == null ? _libraryManager.RootFolder : user.RootFolder :
                parentItem;

            // Default list type = children
            IEnumerable<BaseItem> items;

            if (!string.IsNullOrEmpty(request.Ids))
            {
                var idList = request.Ids.Split(',').ToList();

                items = idList.Select(i => _libraryManager.GetItemById(i));
            }

            else if (request.Recursive)
            {
                items = user == null ?
                    ((Folder)item).RecursiveChildren :
                    ((Folder)item).GetRecursiveChildren(user);

                items = _libraryManager.ReplaceVideosWithPrimaryVersions(items);
            }
            else
            {
                items = user == null ?
                  ((Folder)item).Children :
                  ((Folder)item).GetChildren(user, true);

                items = _libraryManager.ReplaceVideosWithPrimaryVersions(items);
            }

            if (request.IncludeIndexContainers)
            {
                var list = items.ToList();

                var containers = list.Select(i => i.IndexContainer)
                    .Where(i => i != null);

                list.AddRange(containers);

                return list.Distinct();
            }

            return items;
        }

        /// <summary>
        /// Applies sort order
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="items">The items.</param>
        /// <param name="user">The user.</param>
        /// <param name="libraryManager">The library manager.</param>
        /// <returns>IEnumerable{BaseItem}.</returns>
        internal static IEnumerable<BaseItem> ApplySortOrder(BaseItemsRequest request, IEnumerable<BaseItem> items, User user, ILibraryManager libraryManager)
        {
            var orderBy = request.GetOrderBy().ToList();

            return orderBy.Count == 0 ? items : libraryManager.Sort(items, user, orderBy, request.SortOrder ?? SortOrder.Ascending);
        }

        /// <summary>
        /// Applies filtering
        /// </summary>
        /// <param name="items">The items.</param>
        /// <param name="filter">The filter.</param>
        /// <param name="user">The user.</param>
        /// <param name="repository">The repository.</param>
        /// <returns>IEnumerable{BaseItem}.</returns>
        internal static IEnumerable<BaseItem> ApplyFilter(IEnumerable<BaseItem> items, ItemFilter filter, User user, IUserDataManager repository)
        {
            // Avoid implicitly captured closure
            var currentUser = user;

            switch (filter)
            {
                case ItemFilter.IsFavoriteOrLikes:
                    return items.Where(item =>
                    {
                        var userdata = repository.GetUserData(user.Id, item.GetUserDataKey());

                        if (userdata == null)
                        {
                            return false;
                        }

                        var likes = userdata.Likes ?? false;
                        var favorite = userdata.IsFavorite;

                        return likes || favorite;
                    });

                case ItemFilter.Likes:
                    return items.Where(item =>
                    {
                        var userdata = repository.GetUserData(user.Id, item.GetUserDataKey());

                        return userdata != null && userdata.Likes.HasValue && userdata.Likes.Value;
                    });

                case ItemFilter.Dislikes:
                    return items.Where(item =>
                    {
                        var userdata = repository.GetUserData(user.Id, item.GetUserDataKey());

                        return userdata != null && userdata.Likes.HasValue && !userdata.Likes.Value;
                    });

                case ItemFilter.IsFavorite:
                    return items.Where(item =>
                    {
                        var userdata = repository.GetUserData(user.Id, item.GetUserDataKey());

                        return userdata != null && userdata.IsFavorite;
                    });

                case ItemFilter.IsResumable:
                    return items.Where(item =>
                    {
                        var userdata = repository.GetUserData(user.Id, item.GetUserDataKey());

                        return userdata != null && userdata.PlaybackPositionTicks > 0;
                    });

                case ItemFilter.IsPlayed:
                    return items.Where(item => item.IsPlayed(currentUser));

                case ItemFilter.IsUnplayed:
                    return items.Where(item => item.IsUnplayed(currentUser));

                case ItemFilter.IsFolder:
                    return items.Where(item => item.IsFolder);

                case ItemFilter.IsNotFolder:
                    return items.Where(item => !item.IsFolder);

                case ItemFilter.IsRecentlyAdded:
                    return items.Where(item => (DateTime.UtcNow - item.DateCreated).TotalDays <= 10);
            }

            return items;
        }

        private IEnumerable<BaseItem> FilterVirtualEpisodes(GetItems request, IEnumerable<BaseItem> items, User user)
        {
            items = FilterVirtualSeasons(request, items, user);

            if (request.IsMissing.HasValue)
            {
                var val = request.IsMissing.Value;
                items = items.Where(i =>
                {
                    var e = i as Episode;
                    if (e != null)
                    {
                        return e.IsMissingEpisode == val;
                    }
                    return true;
                });
            }

            if (request.IsUnaired.HasValue)
            {
                var val = request.IsUnaired.Value;
                items = items.Where(i =>
                {
                    var e = i as Episode;
                    if (e != null)
                    {
                        return e.IsUnaired == val;
                    }
                    return true;
                });
            }

            if (request.IsVirtualUnaired.HasValue)
            {
                var val = request.IsVirtualUnaired.Value;
                items = items.Where(i =>
                {
                    var e = i as Episode;
                    if (e != null)
                    {
                        return e.IsVirtualUnaired == val;
                    }
                    return true;
                });
            }

            return items;
        }

        private IEnumerable<BaseItem> FilterVirtualSeasons(GetItems request, IEnumerable<BaseItem> items, User user)
        {
            if (request.IsMissing.HasValue && request.IsVirtualUnaired.HasValue)
            {
                var isMissing = request.IsMissing.Value;
                var isVirtualUnaired = request.IsVirtualUnaired.Value;

                if (!isMissing && !isVirtualUnaired)
                {
                    return items.Where(i =>
                    {
                        var e = i as Season;
                        if (e != null)
                        {
                            return !e.IsMissingOrVirtualUnaired;
                        }
                        return true;
                    });
                }
            }

            if (request.IsMissing.HasValue)
            {
                var val = request.IsMissing.Value;
                items = items.Where(i =>
                {
                    var e = i as Season;
                    if (e != null)
                    {
                        return e.IsMissingSeason == val;
                    }
                    return true;
                });
            }

            if (request.IsUnaired.HasValue)
            {
                var val = request.IsUnaired.Value;
                items = items.Where(i =>
                {
                    var e = i as Season;
                    if (e != null)
                    {
                        return e.IsUnaired == val;
                    }
                    return true;
                });
            }

            if (request.IsVirtualUnaired.HasValue)
            {
                var val = request.IsVirtualUnaired.Value;
                items = items.Where(i =>
                {
                    var e = i as Season;
                    if (e != null)
                    {
                        return e.IsVirtualUnaired == val;
                    }
                    return true;
                });
            }

            return items;
        }

        /// <summary>
        /// Applies the additional filters.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="items">The items.</param>
        /// <returns>IEnumerable{BaseItem}.</returns>
        private IEnumerable<BaseItem> ApplyAdditionalFilters(GetItems request, IEnumerable<BaseItem> items, User user)
        {
            if (request.MinCommunityRating.HasValue)
            {
                var val = request.MinCommunityRating.Value;

                items = items.Where(i => i.CommunityRating.HasValue && i.CommunityRating >= val);
            }

            if (request.MinCriticRating.HasValue)
            {
                var val = request.MinCriticRating.Value;

                items = items.Where(i =>
                {
                    var hasCriticRating = i as IHasCriticRating;

                    if (hasCriticRating != null)
                    {
                        return hasCriticRating.CriticRating.HasValue && hasCriticRating.CriticRating >= val;
                    }

                    return false;
                });
            }

            // Artists
            if (!string.IsNullOrEmpty(request.Artists))
            {
                var artists = request.Artists.Split('|');

                items = items.Where(i =>
                {
                    var audio = i as IHasArtist;

                    return audio != null && artists.Any(audio.HasArtist);
                });
            }

            // Albums
            if (!string.IsNullOrEmpty(request.Albums))
            {
                var albums = request.Albums.Split('|');

                items = items.Where(i =>
                {
                    var audio = i as Audio;

                    if (audio != null)
                    {
                        return albums.Any(a => string.Equals(a, audio.Album, StringComparison.OrdinalIgnoreCase));
                    }

                    var album = i as MusicAlbum;

                    if (album != null)
                    {
                        return albums.Any(a => string.Equals(a, album.Name, StringComparison.OrdinalIgnoreCase));
                    }

                    var musicVideo = i as MusicVideo;

                    if (musicVideo != null)
                    {
                        return albums.Any(a => string.Equals(a, musicVideo.Album, StringComparison.OrdinalIgnoreCase));
                    }

                    return false;
                });
            }

            // Min index number
            if (request.MinIndexNumber.HasValue)
            {
                items = items.Where(i => i.IndexNumber.HasValue && i.IndexNumber.Value >= request.MinIndexNumber.Value);
            }

            // Min official rating
            if (!string.IsNullOrEmpty(request.MinOfficialRating))
            {
                var level = _localization.GetRatingLevel(request.MinOfficialRating);

                if (level.HasValue)
                {
                    items = items.Where(i =>
                    {
                        var rating = i.CustomRating;

                        if (string.IsNullOrEmpty(rating))
                        {
                            rating = i.OfficialRating;
                        }

                        if (string.IsNullOrEmpty(rating))
                        {
                            return true;
                        }

                        var itemLevel = _localization.GetRatingLevel(rating);

                        return !itemLevel.HasValue || itemLevel.Value >= level.Value;
                    });
                }
            }

            // Max official rating
            if (!string.IsNullOrEmpty(request.MaxOfficialRating))
            {
                var level = _localization.GetRatingLevel(request.MaxOfficialRating);

                if (level.HasValue)
                {
                    items = items.Where(i =>
                    {
                        var rating = i.CustomRating;

                        if (string.IsNullOrEmpty(rating))
                        {
                            rating = i.OfficialRating;
                        }

                        if (string.IsNullOrEmpty(rating))
                        {
                            return true;
                        }

                        var itemLevel = _localization.GetRatingLevel(rating);

                        return !itemLevel.HasValue || itemLevel.Value <= level.Value;
                    });
                }
            }

            // Exclude item types
            if (!string.IsNullOrEmpty(request.ExcludeItemTypes))
            {
                var vals = request.ExcludeItemTypes.Split(',');
                items = items.Where(f => !vals.Contains(f.GetType().Name, StringComparer.OrdinalIgnoreCase));
            }

            // Include item types
            if (!string.IsNullOrEmpty(request.IncludeItemTypes))
            {
                var vals = request.IncludeItemTypes.Split(',');
                items = items.Where(f => vals.Contains(f.GetType().Name, StringComparer.OrdinalIgnoreCase));
            }

            // LocationTypes
            if (!string.IsNullOrEmpty(request.LocationTypes))
            {
                var vals = request.LocationTypes.Split(',');
                items = items.Where(f => vals.Contains(f.LocationType.ToString(), StringComparer.OrdinalIgnoreCase));
            }

            // ExcludeLocationTypes
            if (!string.IsNullOrEmpty(request.ExcludeLocationTypes))
            {
                var vals = request.ExcludeLocationTypes.Split(',');
                items = items.Where(f => !vals.Contains(f.LocationType.ToString(), StringComparer.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrEmpty(request.NameStartsWithOrGreater))
            {
                items = items.Where(i => string.Compare(request.NameStartsWithOrGreater, i.SortName, StringComparison.CurrentCultureIgnoreCase) < 1);
            }
            if (!string.IsNullOrEmpty(request.NameStartsWith))
            {
                items = items.Where(i => string.Compare(request.NameStartsWith, i.SortName.Substring(0, 1), StringComparison.CurrentCultureIgnoreCase) == 0);
            }

            if (!string.IsNullOrEmpty(request.NameLessThan))
            {
                items = items.Where(i => string.Compare(request.NameLessThan, i.SortName, StringComparison.CurrentCultureIgnoreCase) == 1);
            }

            if (!string.IsNullOrEmpty(request.AlbumArtistStartsWithOrGreater))
            {
                items = items.OfType<IHasAlbumArtist>()
                    .Where(i => string.Compare(request.AlbumArtistStartsWithOrGreater, i.AlbumArtist, StringComparison.CurrentCultureIgnoreCase) < 1)
                    .Cast<BaseItem>();
            }

            // Filter by Series Status
            if (!string.IsNullOrEmpty(request.SeriesStatus))
            {
                var vals = request.SeriesStatus.Split(',');

                items = items.OfType<Series>().Where(i => i.Status.HasValue && vals.Contains(i.Status.Value.ToString(), StringComparer.OrdinalIgnoreCase));
            }

            // Filter by Series AirDays
            if (!string.IsNullOrEmpty(request.AirDays))
            {
                var days = request.AirDays.Split(',').Select(d => (DayOfWeek)Enum.Parse(typeof(DayOfWeek), d, true));

                items = items.OfType<Series>().Where(i => i.AirDays != null && days.Any(d => i.AirDays.Contains(d)));
            }

            // Filter by Video3DFormat
            if (request.Is3D.HasValue)
            {
                items = items.OfType<Video>().Where(i => request.Is3D.Value == i.Video3DFormat.HasValue);
            }

            // Filter by VideoType
            if (!string.IsNullOrEmpty(request.VideoTypes))
            {
                var types = request.VideoTypes.Split(',');

                items = items.OfType<Video>().Where(i => types.Contains(i.VideoType.ToString(), StringComparer.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrEmpty(request.MediaTypes))
            {
                var types = request.MediaTypes.Split(',');

                items = items.Where(i => !string.IsNullOrEmpty(i.MediaType) && types.Contains(i.MediaType, StringComparer.OrdinalIgnoreCase));
            }

            var imageTypes = request.GetImageTypes().ToList();
            if (imageTypes.Count > 0)
            {
                items = items.Where(item => imageTypes.Any(imageType => HasImage(item, imageType)));
            }

            // Apply genre filter
            if (!string.IsNullOrEmpty(request.Genres))
            {
                var vals = request.Genres.Split('|');
                items = items.Where(f => vals.Any(v => f.Genres.Contains(v, StringComparer.OrdinalIgnoreCase)));
            }

            // Apply genre filter
            if (!string.IsNullOrEmpty(request.AllGenres))
            {
                var vals = request.AllGenres.Split('|');
                items = items.Where(f => vals.All(v => f.Genres.Contains(v, StringComparer.OrdinalIgnoreCase)));
            }

            // Apply studio filter
            if (!string.IsNullOrEmpty(request.Studios))
            {
                var vals = request.Studios.Split('|');
                items = items.Where(f => vals.Any(v => f.Studios.Contains(v, StringComparer.OrdinalIgnoreCase)));
            }

            // Apply year filter
            if (!string.IsNullOrEmpty(request.Years))
            {
                var vals = request.Years.Split(',').Select(int.Parse).ToList();
                items = items.Where(f => f.ProductionYear.HasValue && vals.Contains(f.ProductionYear.Value));
            }

            // Apply person filter
            if (!string.IsNullOrEmpty(request.Person))
            {
                var personTypes = request.PersonTypes;

                if (string.IsNullOrEmpty(personTypes))
                {
                    items = items.Where(item => item.People.Any(p => string.Equals(p.Name, request.Person, StringComparison.OrdinalIgnoreCase)));
                }
                else
                {
                    var types = personTypes.Split(',');

                    items = items.Where(item =>
                            item.People != null &&
                            item.People.Any(p =>
                                p.Name.Equals(request.Person, StringComparison.OrdinalIgnoreCase) && (types.Contains(p.Type, StringComparer.OrdinalIgnoreCase) || types.Contains(p.Role, StringComparer.OrdinalIgnoreCase))));
                }
            }

            if (request.HasTrailer.HasValue)
            {
                var val = request.HasTrailer.Value;
                items = items.Where(i =>
                {
                    var trailerCount = 0;

                    var hasTrailers = i as IHasTrailers;
                    if (hasTrailers != null)
                    {
                        trailerCount = hasTrailers.LocalTrailerIds.Count;
                    }

                    return val ? trailerCount > 0 : trailerCount == 0;
                });
            }

            if (request.HasThemeSong.HasValue)
            {
                var filterValue = request.HasThemeSong.Value;

                items = items.Where(i =>
                {
                    var themeCount = 0;
                    var iHasThemeMedia = i as IHasThemeMedia;

                    if (iHasThemeMedia != null)
                    {
                        themeCount = iHasThemeMedia.ThemeSongIds.Count;
                    }
                    return filterValue ? themeCount > 0 : themeCount == 0;
                });
            }

            if (request.HasThemeVideo.HasValue)
            {
                var filterValue = request.HasThemeVideo.Value;

                items = items.Where(i =>
                {
                    var themeCount = 0;
                    var iHasThemeMedia = i as IHasThemeMedia;

                    if (iHasThemeMedia != null)
                    {
                        themeCount = iHasThemeMedia.ThemeVideoIds.Count;
                    }
                    return filterValue ? themeCount > 0 : themeCount == 0;
                });
            }

            if (request.MinPlayers.HasValue)
            {
                var filterValue = request.MinPlayers.Value;

                items = items.Where(i =>
                {
                    var game = i as Game;

                    if (game != null)
                    {
                        var players = game.PlayersSupported ?? 1;

                        return players >= filterValue;
                    }

                    return false;
                });
            }

            if (request.MaxPlayers.HasValue)
            {
                var filterValue = request.MaxPlayers.Value;

                items = items.Where(i =>
                {
                    var game = i as Game;

                    if (game != null)
                    {
                        var players = game.PlayersSupported ?? 1;

                        return players <= filterValue;
                    }

                    return false;
                });
            }

            if (request.HasSpecialFeature.HasValue)
            {
                var filterValue = request.HasSpecialFeature.Value;

                items = items.Where(i =>
                {
                    var movie = i as Movie;

                    if (movie != null)
                    {
                        return filterValue
                                   ? movie.SpecialFeatureIds.Count > 0
                                   : movie.SpecialFeatureIds.Count == 0;
                    }

                    var series = i as Series;

                    if (series != null)
                    {
                        return filterValue
                                   ? series.SpecialFeatureIds.Count > 0
                                   : series.SpecialFeatureIds.Count == 0;
                    }

                    return false;
                });
            }

            if (request.HasSubtitles.HasValue)
            {
                var val = request.HasSubtitles.Value;

                items = items.OfType<Video>().Where(i => val == i.HasSubtitles);
            }

            if (request.HasParentalRating.HasValue)
            {
                var val = request.HasParentalRating.Value;

                items = items.Where(i =>
                {
                    var rating = i.CustomRating;

                    if (string.IsNullOrEmpty(rating))
                    {
                        rating = i.OfficialRating;
                    }

                    if (val)
                    {
                        return !string.IsNullOrEmpty(rating);
                    }

                    return string.IsNullOrEmpty(rating);
                });
            }

            if (request.IsHD.HasValue)
            {
                var val = request.IsHD.Value;
                items = items.OfType<Video>().Where(i => i.IsHD == val);
            }

            if (request.IsInBoxSet.HasValue)
            {
                var val = request.IsHD.Value;
                items = items.Where(i => i.Parents.OfType<BoxSet>().Any() == val);
            }

            if (request.IsPlayed.HasValue)
            {
                var val = request.IsPlayed.Value;
                items = items.Where(i => i.IsPlayed(user) == val);
            }

            if (request.IsUnidentified.HasValue)
            {
                var val = request.IsUnidentified.Value;
                items = items.Where(i => i.IsUnidentified == val);
            }

            if (request.IsLocked.HasValue)
            {
                var val = request.IsLocked.Value;
                items = items.Where(i => i.IsLocked == val);
            }

            if (request.ParentIndexNumber.HasValue)
            {
                var filterValue = request.ParentIndexNumber.Value;

                items = items.Where(i =>
                {
                    var episode = i as Episode;

                    if (episode != null)
                    {
                        return episode.ParentIndexNumber.HasValue && episode.ParentIndexNumber.Value == filterValue;
                    }

                    var song = i as Audio;

                    if (song != null)
                    {
                        return song.ParentIndexNumber.HasValue && song.ParentIndexNumber.Value == filterValue;
                    }

                    return true;
                });
            }

            if (request.AiredDuringSeason.HasValue)
            {
                items = Series.FilterEpisodesBySeason(items.OfType<Episode>(), request.AiredDuringSeason.Value, true);
            }

            if (!string.IsNullOrEmpty(request.MinPremiereDate))
            {
                var date = DateTime.Parse(request.MinPremiereDate, null, DateTimeStyles.RoundtripKind).ToUniversalTime();

                items = items.Where(i => i.PremiereDate.HasValue && i.PremiereDate.Value >= date);
            }

            if (!string.IsNullOrEmpty(request.MaxPremiereDate))
            {
                var date = DateTime.Parse(request.MaxPremiereDate, null, DateTimeStyles.RoundtripKind).ToUniversalTime();

                items = items.Where(i => i.PremiereDate.HasValue && i.PremiereDate.Value <= date);
            }

            if (request.HasOverview.HasValue)
            {
                var filterValue = request.HasOverview.Value;

                items = items.Where(i =>
                {
                    var hasValue = !string.IsNullOrEmpty(i.Overview);

                    return hasValue == filterValue;
                });
            }

            if (request.HasImdbId.HasValue)
            {
                var filterValue = request.HasImdbId.Value;

                items = items.Where(i =>
                {
                    var hasValue = !string.IsNullOrEmpty(i.GetProviderId(MetadataProviders.Imdb));

                    return hasValue == filterValue;
                });
            }

            if (request.HasTmdbId.HasValue)
            {
                var filterValue = request.HasTmdbId.Value;

                items = items.Where(i =>
                {
                    var hasValue = !string.IsNullOrEmpty(i.GetProviderId(MetadataProviders.Tmdb));

                    return hasValue == filterValue;
                });
            }

            if (request.HasTvdbId.HasValue)
            {
                var filterValue = request.HasTvdbId.Value;

                items = items.Where(i =>
                {
                    var hasValue = !string.IsNullOrEmpty(i.GetProviderId(MetadataProviders.Tvdb));

                    return hasValue == filterValue;
                });
            }

            if (request.IsYearMismatched.HasValue)
            {
                var filterValue = request.IsYearMismatched.Value;

                items = items.Where(i => IsYearMismatched(i) == filterValue);
            }

            if (request.HasOfficialRating.HasValue)
            {
                var filterValue = request.HasOfficialRating.Value;

                items = items.Where(i =>
                {
                    var hasValue = !string.IsNullOrEmpty(i.OfficialRating);

                    return hasValue == filterValue;
                });
            }

            if (request.IsPlaceHolder.HasValue)
            {
                var filterValue = request.IsPlaceHolder.Value;

                items = items.Where(i =>
                {
                    var isPlaceHolder = false;

                    var hasPlaceHolder = i as ISupportsPlaceHolders;

                    if (hasPlaceHolder != null)
                    {
                        isPlaceHolder = hasPlaceHolder.IsPlaceHolder;
                    }

                    return isPlaceHolder == filterValue;
                });
            }

            return items;
        }

        private bool IsYearMismatched(BaseItem item)
        {
            if (item.ProductionYear.HasValue)
            {
                var path = item.Path;

                if (!string.IsNullOrEmpty(path))
                {
                    int? yearInName;
                    string name;
                    NameParser.ParseName(Path.GetFileName(path), out name, out yearInName);

                    // Go up a level if we didn't get a year
                    if (!yearInName.HasValue)
                    {
                        NameParser.ParseName(Path.GetFileName(Path.GetDirectoryName(path)), out name, out yearInName);
                    }

                    if (yearInName.HasValue)
                    {
                        return yearInName.Value != item.ProductionYear.Value;
                    }
                }
            }

            return false;
        }

        private bool AllowBoxSetCollapsing(GetItems request)
        {
            if (!string.IsNullOrWhiteSpace(request.Filters))
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(request.AllGenres))
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(request.Genres))
            {
                return false;
            }

            if (request.HasImdbId.HasValue)
            {
                return false;
            }

            if (request.HasOfficialRating.HasValue)
            {
                return false;
            }

            if (request.HasOverview.HasValue)
            {
                return false;
            }

            if (request.HasParentalRating.HasValue)
            {
                return false;
            }

            if (request.HasSpecialFeature.HasValue)
            {
                return false;
            }

            if (request.HasSubtitles.HasValue)
            {
                return false;
            }

            if (request.HasThemeSong.HasValue)
            {
                return false;
            }

            if (request.HasThemeVideo.HasValue)
            {
                return false;
            }

            if (request.HasTmdbId.HasValue)
            {
                return false;
            }

            if (request.HasTrailer.HasValue)
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(request.Ids))
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(request.ImageTypes))
            {
                return false;
            }

            if (request.Is3D.HasValue)
            {
                return false;
            }

            if (request.IsHD.HasValue)
            {
                return false;
            }

            if (request.IsInBoxSet.HasValue)
            {
                return false;
            }

            if (request.IsLocked.HasValue)
            {
                return false;
            }

            if (request.IsPlaceHolder.HasValue)
            {
                return false;
            }

            if (request.IsPlayed.HasValue)
            {
                return false;
            }

            if (request.IsUnidentified.HasValue)
            {
                return false;
            }

            if (request.IsYearMismatched.HasValue)
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(request.Person))
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(request.Studios))
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(request.VideoTypes))
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(request.Years))
            {
                return false;
            }

            return true;
        }

        internal static IEnumerable<BaseItem> FilterForAdjacency(IEnumerable<BaseItem> items, string adjacentToId)
        {
            var list = items.ToList();

            var adjacentToIdGuid = new Guid(adjacentToId);
            var adjacentToItem = list.FirstOrDefault(i => i.Id == adjacentToIdGuid);

            var index = list.IndexOf(adjacentToItem);

            var previousId = Guid.Empty;
            var nextId = Guid.Empty;

            if (index > 0)
            {
                previousId = list[index - 1].Id;
            }

            if (index < list.Count - 1)
            {
                nextId = list[index + 1].Id;
            }

            return list.Where(i => i.Id == previousId || i.Id == nextId);
        }

        /// <summary>
        /// Determines whether the specified item has image.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="imageType">Type of the image.</param>
        /// <returns><c>true</c> if the specified item has image; otherwise, <c>false</c>.</returns>
        internal static bool HasImage(BaseItem item, ImageType imageType)
        {
            return item.HasImage(imageType);
        }

        /// <summary>
        /// Applies the paging.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="items">The items.</param>
        /// <returns>IEnumerable{BaseItem}.</returns>
        private IEnumerable<BaseItem> ApplyPaging(GetItems request, IEnumerable<BaseItem> items)
        {
            // Start at
            if (request.StartIndex.HasValue)
            {
                items = items.Skip(request.StartIndex.Value);
            }

            // Return limit
            if (request.Limit.HasValue)
            {
                items = items.Take(request.Limit.Value);
            }

            return items;
        }
    }

    /// <summary>
    /// Class DateCreatedComparer
    /// </summary>
    public class DateCreatedComparer : IComparer<BaseItem>
    {
        /// <summary>
        /// Compares the specified x.
        /// </summary>
        /// <param name="x">The x.</param>
        /// <param name="y">The y.</param>
        /// <returns>System.Int32.</returns>
        public int Compare(BaseItem x, BaseItem y)
        {
            return x.DateCreated.CompareTo(y.DateCreated);
        }
    }
}
