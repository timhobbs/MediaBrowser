﻿using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Querying;
using ServiceStack;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MediaBrowser.Api.UserLibrary
{
    /// <summary>
    /// Class GetYears
    /// </summary>
    [Route("/Years", "GET", Summary = "Gets all years from a given item, folder, or the entire library")]
    public class GetYears : GetItemsByName
    {
    }

    /// <summary>
    /// Class GetYear
    /// </summary>
    [Route("/Years/{Year}", "GET", Summary = "Gets a year")]
    public class GetYear : IReturn<BaseItemDto>
    {
        /// <summary>
        /// Gets or sets the year.
        /// </summary>
        /// <value>The year.</value>
        [ApiMember(Name = "Year", Description = "The year", IsRequired = true, DataType = "int", ParameterType = "path", Verb = "GET")]
        public int Year { get; set; }

        /// <summary>
        /// Gets or sets the user id.
        /// </summary>
        /// <value>The user id.</value>
        [ApiMember(Name = "UserId", Description = "Optional. Filter by user id, and attach user data", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public Guid? UserId { get; set; }
    }

    /// <summary>
    /// Class YearsService
    /// </summary>
    public class YearsService : BaseItemsByNameService<Year>
    {
        public YearsService(IUserManager userManager, ILibraryManager libraryManager, IUserDataManager userDataRepository, IItemRepository itemRepo, IDtoService dtoService)
            : base(userManager, libraryManager, userDataRepository, itemRepo, dtoService)
        {
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public object Get(GetYear request)
        {
            var result = GetItem(request);

            return ToOptimizedSerializedResultUsingCache(result);
        }

        /// <summary>
        /// Gets the item.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>Task{BaseItemDto}.</returns>
        private BaseItemDto GetItem(GetYear request)
        {
            var item = LibraryManager.GetYear(request.Year);

            // Get everything
            var fields = Enum.GetNames(typeof(ItemFields)).Select(i => (ItemFields)Enum.Parse(typeof(ItemFields), i, true));

            if (request.UserId.HasValue)
            {
                var user = UserManager.GetUserById(request.UserId.Value);

                return DtoService.GetBaseItemDto(item, fields.ToList(), user);
            }

            return DtoService.GetBaseItemDto(item, fields.ToList());
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public object Get(GetYears request)
        {
            var result = GetResult(request);

            return ToOptimizedSerializedResultUsingCache(result);
        }

        /// <summary>
        /// Gets all items.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="items">The items.</param>
        /// <returns>IEnumerable{Tuple{System.StringFunc{System.Int32}}}.</returns>
        protected override IEnumerable<Year> GetAllItems(GetItemsByName request, IEnumerable<BaseItem> items)
        {
            var itemsList = items.Where(i => i.ProductionYear != null).ToList();

            return itemsList
                .Select(i => i.ProductionYear ?? 0)
                .Where(i => i > 0)
                .Distinct()
                .Select(year => LibraryManager.GetYear(year));
        }
    }
}
