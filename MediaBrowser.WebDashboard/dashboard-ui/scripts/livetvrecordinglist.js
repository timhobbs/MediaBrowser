﻿(function ($, document) {

    // The base query options
    var query = {

        UserId: Dashboard.getCurrentUserId(),
        StartIndex: 0
    };

    function reloadItems(page) {

        Dashboard.showLoadingMsg();

        ApiClient.getLiveTvRecordings(query).done(function (result) {

            // Scroll back up so they can see the results from the beginning
            $(document).scrollTop(0);

            var html = '';

            $('.listTopPaging', page).html(LibraryBrowser.getPagingHtml(query, result.TotalRecordCount, true)).trigger('create');

            updateFilterControls();

            var screenWidth = $(window).width();

            html += LibraryBrowser.getPosterViewHtml({

                items: result.Items,
                shape: "auto",
                showTitle: true,
                showParentTitle: true,
                overlayText: screenWidth >= 600,
                coverImage: true

            });

            html += LibraryBrowser.getPagingHtml(query, result.TotalRecordCount);

            $('#items', page).html(html).trigger('create').createPosterItemMenus();

            $('.btnNextPage', page).on('click', function () {
                query.StartIndex += query.Limit;
                reloadItems(page);
            });

            $('.btnPreviousPage', page).on('click', function () {
                query.StartIndex -= query.Limit;
                reloadItems(page);
            });

            $('.selectPageSize', page).on('change', function () {
                query.Limit = parseInt(this.value);
                query.StartIndex = 0;
                reloadItems(page);
            });

            if (getParameterByName('savequery') != 'false') {
                LibraryBrowser.saveQueryValues('episodes', query);
            }

            Dashboard.hideLoadingMsg();
        });
    }

    function updateFilterControls(page) {

    }

    $(document).on('pageinit', "#liveTvRecordingListPage", function () {

        var page = this;


    }).on('pagebeforeshow', "#liveTvRecordingListPage", function () {

        var page = this;
        
        var limit = LibraryBrowser.getDefaultPageSize();

        // If the default page size has changed, the start index will have to be reset
        if (limit != query.Limit) {
            query.Limit = limit;
            query.StartIndex = 0;
        }

        LibraryBrowser.loadSavedQueryValues('episodes', query);

        var groupId = getParameterByName('groupid');
        query.GroupId = groupId;

        reloadItems(page);

        if (query.GroupId) {

            ApiClient.getLiveTvRecordingGroup(query.GroupId).done(function (group) {
                $('.listName', page).html(group.Name);
            });

        } else {
            $('.listName', page).html('All Recordings');
        }

    }).on('pageshow', "#liveTvRecordingListPage", function () {

        updateFilterControls(this);
    });

})(jQuery, document);