﻿(function ($, document) {

    var view = LibraryBrowser.getDefaultItemsView('Poster', 'List');

    // The base query options
    var query = {

        SortBy: "SeriesSortName,SortName",
        SortOrder: "Ascending",
        IncludeItemTypes: "Episode",
        Recursive: true,
        Fields: "PrimaryImageAspectRatio,SortName",
        StartIndex: 0,
        IsMissing: false,
        IsVirtualUnaired: false
    };

    function getSavedQueryKey() {

        return 'episodes' + (query.ParentId || '');
    }

    function reloadItems(page) {

        Dashboard.showLoadingMsg();

        ApiClient.getItems(Dashboard.getCurrentUserId(), query).done(function (result) {

            // Scroll back up so they can see the results from the beginning
            $(document).scrollTop(0);

            var html = '';

            var pagingHtml = LibraryBrowser.getQueryPagingHtml({
                startIndex: query.StartIndex,
                limit: query.Limit,
                totalRecordCount: result.TotalRecordCount,
                viewButton: true,
                showLimit: false,
                addSelectionButton: true
            });

            $('.listTopPaging', page).html(pagingHtml).trigger('create');

            updateFilterControls();

            var screenWidth = $(window).width();

            if (view == "List") {

                html = LibraryBrowser.getListViewHtml({
                    items: result.Items,
                    context: 'tv',
                    sortBy: query.SortBy
                });
            }
            else if (view == "Poster") {
                html += LibraryBrowser.getPosterViewHtml({
                    items: result.Items,
                    shape: "backdrop",
                    showTitle: true,
                    showParentTitle: true,
                    overlayText: screenWidth >= 600,
                    selectionPanel: true,
                    lazy: true,
                    context: 'tv'
                });
            }

            html += pagingHtml;

            $('.itemsContainer', page).html(html).trigger('create').createPosterItemMenus().trigger('itemsrendered');

            $('.btnNextPage', page).on('click', function () {
                query.StartIndex += query.Limit;
                reloadItems(page);
            });

            $('.btnPreviousPage', page).on('click', function () {
                query.StartIndex -= query.Limit;
                reloadItems(page);
            });

            LibraryBrowser.saveQueryValues(getSavedQueryKey(), query);

            Dashboard.hideLoadingMsg();
        });
    }

    function updateFilterControls(page) {


        // Reset form values using the last used query
        $('.radioSortBy', page).each(function () {

            this.checked = (query.SortBy || '').toLowerCase() == this.getAttribute('data-sortby').toLowerCase();

        }).checkboxradio('refresh');

        $('.radioSortOrder', page).each(function () {

            this.checked = (query.SortOrder || '').toLowerCase() == this.getAttribute('data-sortorder').toLowerCase();

        }).checkboxradio('refresh');

        $('.chkStandardFilter', page).each(function () {

            var filters = "," + (query.Filters || "");
            var filterName = this.getAttribute('data-filter');

            this.checked = filters.toLowerCase().indexOf(',' + filterName.toLowerCase()) != -1;

        }).checkboxradio('refresh');

        $('.chkVideoTypeFilter', page).each(function () {

            var filters = "," + (query.VideoTypes || "");
            var filterName = this.getAttribute('data-filter');

            this.checked = filters.indexOf(',' + filterName) != -1;

        }).checkboxradio('refresh');

        $('#selectView', page).val(view).selectmenu('refresh');

        $('#chkHD', page).checked(query.IsHD == true).checkboxradio('refresh');
        $('#chkSD', page).checked(query.IsHD == false).checkboxradio('refresh');

        $('#chkSubtitle', page).checked(query.HasSubtitles == true).checkboxradio('refresh');
        $('#chkTrailer', page).checked(query.HasTrailer == true).checkboxradio('refresh');
        $('#chkThemeSong', page).checked(query.HasThemeSong == true).checkboxradio('refresh');
        $('#chkThemeVideo', page).checked(query.HasThemeVideo == true).checkboxradio('refresh');
        $('#chkSpecialFeature', page).checked(query.ParentIndexNumber == 0).checkboxradio('refresh');

        $('#chkMissingEpisode', page).checked(query.IsMissing == true).checkboxradio('refresh');
        $('#chkFutureEpisode', page).checked(query.IsUnaired == true).checkboxradio('refresh');

        $('.alphabetPicker', page).alphaValue(query.NameStartsWithOrGreater);
        $('#selectPageSize', page).val(query.Limit).selectmenu('refresh');
    }

    $(document).on('pageinit', "#episodesPage", function () {

        var page = this;

        $('.radioSortBy', this).on('click', function () {
            query.StartIndex = 0;
            query.SortBy = this.getAttribute('data-sortby');
            reloadItems(page);
        });

        $('.radioSortOrder', this).on('click', function () {
            query.StartIndex = 0;
            query.SortOrder = this.getAttribute('data-sortorder');
            reloadItems(page);
        });

        $('.chkStandardFilter', this).on('change', function () {

            var filterName = this.getAttribute('data-filter');
            var filters = query.Filters || "";

            filters = (',' + filters).replace(',' + filterName, '').substring(1);

            if (this.checked) {
                filters = filters ? (filters + ',' + filterName) : filterName;
            }

            query.StartIndex = 0;
            query.Filters = filters;

            reloadItems(page);
        });


        $('.chkVideoTypeFilter', this).on('change', function () {

            var filterName = this.getAttribute('data-filter');
            var filters = query.VideoTypes || "";

            filters = (',' + filters).replace(',' + filterName, '').substring(1);

            if (this.checked) {
                filters = filters ? (filters + ',' + filterName) : filterName;
            }

            query.StartIndex = 0;
            query.VideoTypes = filters;

            reloadItems(page);
        });

        $('#chkSubtitle', this).on('change', function () {

            query.StartIndex = 0;
            query.HasSubtitles = this.checked ? true : null;

            reloadItems(page);
        });

        $('#chkTrailer', this).on('change', function () {

            query.StartIndex = 0;
            query.HasTrailer = this.checked ? true : null;

            reloadItems(page);
        });

        $('#chkThemeSong', this).on('change', function () {

            query.StartIndex = 0;
            query.HasThemeSong = this.checked ? true : null;

            reloadItems(page);
        });

        $('#chkThemeVideo', this).on('change', function () {

            query.StartIndex = 0;
            query.HasThemeVideo = this.checked ? true : null;

            reloadItems(page);
        });

        $('#chkSpecialFeature', this).on('change', function () {

            query.ParentIndexNumber = this.checked ? 0 : null;

            reloadItems(page);
        });

        $('#chkMissingEpisode', this).on('change', function () {

            query.StartIndex = 0;
            query.IsMissing = this.checked ? true : false;

            reloadItems(page);
        });

        $('#chkFutureEpisode', this).on('change', function () {

            query.StartIndex = 0;

            if (this.checked) {
                query.IsUnaired = true;
                query.IsVirtualUnaired = null;
            } else {
                query.IsUnaired = null;
                query.IsVirtualUnaired = false;
            }


            reloadItems(page);
        });

        $('#chkHD', this).on('change', function () {

            query.StartIndex = 0;
            query.IsHD = this.checked ? true : null;

            reloadItems(page);
        });

        $('#chkSD', this).on('change', function () {

            query.StartIndex = 0;
            query.IsHD = this.checked ? false : null;

            reloadItems(page);
        });

        $('.alphabetPicker', this).on('alphaselect', function (e, character) {

            query.NameStartsWithOrGreater = character;
            query.StartIndex = 0;

            reloadItems(page);

        }).on('alphaclear', function (e) {

            query.NameStartsWithOrGreater = '';

            reloadItems(page);
        });

        $('.itemsContainer', page).on('needsrefresh', function () {

            reloadItems(page);

        });

        $('#selectView', this).on('change', function () {

            view = this.value;

            reloadItems(page);

            LibraryBrowser.saveViewSetting(getSavedQueryKey(), view);
        });

        $('#selectPageSize', page).on('change', function () {
            query.Limit = parseInt(this.value);
            query.StartIndex = 0;
            reloadItems(page);
        });

    }).on('pagebeforeshow', "#episodesPage", function () {

        var page = this;
        query.ParentId = LibraryMenu.getTopParentId();

        var limit = LibraryBrowser.getDefaultPageSize();

        // If the default page size has changed, the start index will have to be reset
        if (limit != query.Limit) {
            query.Limit = limit;
            query.StartIndex = 0;
        }

        var viewkey = getSavedQueryKey();

        LibraryBrowser.loadSavedQueryValues(viewkey, query);

        var filters = getParameterByName('filters');
        if (filters) {
            query.Filters = filters;
        }

        var sortby = getParameterByName('sortby');
        if (sortby) {
            query.SortBy = sortby;
        }

        var sortorder = getParameterByName('sortorder');
        if (sortorder) {
            query.SortOrder = sortorder;
        }

        LibraryBrowser.getSavedViewSetting(viewkey).done(function (val) {

            if (val) {
                $('#selectView', page).val(val).selectmenu('refresh').trigger('change');
            } else {
                reloadItems(page);
            }
        });

    }).on('pageshow', "#episodesPage", function () {

        updateFilterControls(this);
    });

})(jQuery, document);