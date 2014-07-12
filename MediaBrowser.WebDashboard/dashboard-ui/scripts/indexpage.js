﻿(function ($, document, apiClient) {

    function getUserViews(userId) {

        var deferred = $.Deferred();

        ApiClient.getUserViews(userId).done(function (result) {

            var items = result.Items;

            deferred.resolveWith(null, [items]);
        });

        return deferred.promise();
    }

    function createMediaLinks(options) {

        var html = "";

        var items = options.items;

        // "My Library" backgrounds
        for (var i = 0, length = items.length; i < length; i++) {

            var item = items[i];

            var imgUrl;

            switch (item.CollectionType) {
                case "movies":
                    imgUrl = "css/images/items/folders/movies.png";
                    break;
                case "music":
                    imgUrl = "css/images/items/folders/music.png";
                    break;
                case "photos":
                    imgUrl = "css/images/items/folders/photos.png";
                    break;
                case "livetv":
                case "tvshows":
                    imgUrl = "css/images/items/folders/tv.png";
                    break;
                case "games":
                    imgUrl = "css/images/items/folders/games.png";
                    break;
                case "trailers":
                    imgUrl = "css/images/items/folders/movies.png";
                    break;
                case "adultvideos":
                case "homevideos":
                    imgUrl = "css/images/items/folders/homevideos.png";
                    break;
                case "musicvideos":
                    imgUrl = "css/images/items/folders/musicvideos.png";
                    break;
                case "books":
                    imgUrl = "css/images/items/folders/books.png";
                    break;
                case "channels":
                    imgUrl = "css/images/items/folders/channels.png";
                    break;
                case "boxsets":
                default:
                    imgUrl = "css/images/items/folders/folder.png";
                    break;
            }

            var cssClass = "posterItem";
            cssClass += ' ' + options.shape + 'PosterItem';

            if (item.CollectionType) {
                cssClass += ' ' + item.CollectionType + 'PosterItem';
            }

            var href = item.url || LibraryBrowser.getHref(item, options.context);

            html += '<a data-itemid="' + item.Id + '" class="' + cssClass + '" href="' + href + '">';

            var style = "";

            if (imgUrl) {
                style += 'background-image:url(\'' + imgUrl + '\');';
            }

            var imageCssClass = 'posterItemImage';

            html += '<div class="' + imageCssClass + '" style="' + style + '">';
            html += '</div>';

            html += "<div class='posterItemDefaultText posterItemText'>";
            html += item.Name;
            html += "</div>";

            html += "</a>";
        }

        return html;
    }

    function loadlibraryButtons(elem, userId, index) {

        getUserViews(userId).done(function (items) {

            var html = '<br/>';

            if (index) {
                html += '<h1 class="listHeader">' + Globalize.translate('HeaderMyViews') + '</h1>';
            }
            html += '<div>';
            html += createMediaLinks({
                items: items,
                shape: 'myLibrary',
                showTitle: true,
                centerText: true

            });
            html += '</div>';

            $(elem).html(html);

            handleLibraryLinkNavigations(elem);
        });
    }

    function loadRecentlyAdded(elem, userId) {

        var screenWidth = $(window).width();
        
        var options = {

            Limit: screenWidth >= 2400 ? 30 : (screenWidth >= 1920 ? 15 : (screenWidth >= 1440 ? 12 : (screenWidth >= 800 ? 9 : 8))),
            Fields: "PrimaryImageAspectRatio",
            IsPlayed: false,
            IsFolder: false
        };

        ApiClient.getJSON(ApiClient.getUrl('Users/' + userId + '/Items/Latest', options)).done(function (items) {

            var html = '';

            if (items.length) {
                html += '<h1 class="listHeader">' + Globalize.translate('HeaderLatestMedia') + '</h1>';
                html += '<div>';
                html += LibraryBrowser.getPosterViewHtml({
                    items: items,
                    preferThumb: true,
                    shape: 'backdrop',
                    context: 'home',
                    showUnplayedIndicator: false,
                    showChildCountIndicator: true,
                    lazy: true
                });
                html += '</div>';
            }

            $(elem).html(html).trigger('create').createPosterItemMenus();
        });
    }

    function loadLatestChannelMedia(elem, userId) {

        var screenWidth = $(window).width();

        var options = {

            Limit: screenWidth >= 2400 ? 10 : (screenWidth >= 1920 ? 10 : (screenWidth >= 1440 ? 8 : (screenWidth >= 800 ? 7 : 6))),
            Fields: "PrimaryImageAspectRatio",
            Filters: "IsUnplayed",
            UserId: userId
        };

        ApiClient.getJSON(ApiClient.getUrl("Channels/Items/Latest", options)).done(function (result) {

            var html = '';

            if (result.Items.length) {
                html += '<h1 class="listHeader">' + Globalize.translate('HeaderLatestChannelMedia') + '</h1>';
                html += '<div>';
                html += LibraryBrowser.getPosterViewHtml({
                    items: result.Items,
                    preferThumb: true,
                    shape: 'autosmall',
                    showTitle: true,
                    centerText: true,
                    lazy: true
                });
                html += '</div>';
            }

            $(elem).html(html).trigger('create').createPosterItemMenus();
        });
    }

    function loadLibraryTiles(elem, userId, shape, index) {

        getUserViews(userId).done(function (items) {

            var html = '';

            if (items.length) {

                var cssClass = index !== 0 ? 'listHeader' : 'listHeader firstListHeader';

                html += '<div>';
                html += '<h1 style="display:inline-block; vertical-align:middle;" class="' + cssClass + '">' + Globalize.translate('HeaderMyViews') + '</h1>';
                html += '<a href="mypreferencesdisplay.html" data-role="button" data-icon="edit" data-mini="true" data-inline="true" data-iconpos="notext" class="sectionHeaderButton">d</a>';
                html += '</div>';

                html += '<div>';
                html += LibraryBrowser.getPosterViewHtml({
                    items: items,
                    shape: shape,
                    showTitle: true,
                    centerText: true,
                    lazy: true,
                    autoThumb: true,
                    context: 'home'
                });
                html += '</div>';
            }


            $(elem).html(html).trigger('create').createPosterItemMenus();

            handleLibraryLinkNavigations(elem);
        });
    }

    function loadLibraryFolders(elem, userId, shape, index) {

        ApiClient.getItems(userId, {

            SortBy: "SortName"

        }).done(function (result) {

            var html = '';
            var items = result.Items;

            for (var i = 0, length = items.length; i < length; i++) {
                items[i].url = 'itemlist.html?parentid=' + items[i].Id;
            }

            if (items.length) {

                html += '<h1 class="listHeader">' + Globalize.translate('HeaderLibraryFolders') + '</h1>';

                html += '<div>';
                html += LibraryBrowser.getPosterViewHtml({
                    items: items,
                    shape: shape,
                    showTitle: true,
                    centerText: true,
                    lazy: true
                });
                html += '</div>';
            }

            $(elem).html(html).trigger('create').createPosterItemMenus();

            handleLibraryLinkNavigations(elem);
        });
    }

    function loadResume(elem, userId) {

        var screenWidth = $(window).width();

        var options = {

            SortBy: "DatePlayed",
            SortOrder: "Descending",
            MediaTypes: "Video",
            Filters: "IsResumable",
            Limit: screenWidth >= 1920 ? 10 : (screenWidth >= 1440 ? 8 : 6),
            Recursive: true,
            Fields: "PrimaryImageAspectRatio",
            CollapseBoxSetItems: false,
            ExcludeLocationTypes: "Virtual"
        };

        ApiClient.getItems(userId, options).done(function (result) {

            var html = '';

            if (result.Items.length) {
                html += '<h1 class="listHeader">' + Globalize.translate('HeaderResume') + '</h1>';
                html += '<div>';
                html += LibraryBrowser.getPosterViewHtml({
                    items: result.Items,
                    preferBackdrop: true,
                    shape: 'backdrop',
                    overlayText: screenWidth >= 600,
                    showTitle: true,
                    showParentTitle: true,
                    context: 'home',
                    lazy: true
                });
                html += '</div>';
            }

            $(elem).html(html).trigger('create').createPosterItemMenus();
        });
    }

    function handleLibraryLinkNavigations(elem) {

        $('a.posterItem', elem).on('click', function () {

            var text = $('.posterItemText', this).html();

            LibraryMenu.setText(text);
        });
    }

    function loadLatestChannelItems(elem, userId, options) {

        options = $.extend(options || {}, {

            UserId: userId,
            SupportsLatestItems: true
        });

        ApiClient.getJSON(ApiClient.getUrl("Channels", options)).done(function (result) {

            var channels = result.Items;

            var channelsHtml = channels.map(function (c) {

                return '<div id="channel' + c.Id + '"></div>';

            }).join('');

            $(elem).html(channelsHtml);

            for (var i = 0, length = channels.length; i < length; i++) {

                var channel = channels[i];

                loadLatestChannelItemsFromChannel(elem, channel, i);
            }

        });
    }

    function loadLatestChannelItemsFromChannel(page, channel, index) {

        var screenWidth = $(window).width();

        var options = {

            Limit: screenWidth >= 1920 ? 7 : (screenWidth >= 1440 ? 5 : (screenWidth >= 800 ? 6 : 6)),
            Fields: "PrimaryImageAspectRatio",
            Filters: "IsUnplayed",
            UserId: Dashboard.getCurrentUserId(),
            ChannelIds: channel.Id
        };

        ApiClient.getJSON(ApiClient.getUrl("Channels/Items/Latest", options)).done(function (result) {

            var html = '';

            if (result.Items.length) {

                var cssClass = index !== 0 ? 'listHeader' : 'listHeader firstListHeader';

                html += '<div>';
                var text = Globalize.translate('HeaderLatestFromChannel').replace('{0}', channel.Name);
                html += '<h1 style="display:inline-block; vertical-align:middle;" class="' + cssClass + '">' + text + '</h1>';
                html += '<a href="channelitems.html?context=channels&id=' + channel.Id + '" data-role="button" data-icon="arrow-r" data-mini="true" data-inline="true" data-iconpos="notext" class="sectionHeaderButton">d</a>';
                html += '</div>';
            }
            html += LibraryBrowser.getPosterViewHtml({
                items: result.Items,
                shape: 'autosmall',
                defaultShape: 'square',
                showTitle: true,
                centerText: true,
                context: 'channels',
                lazy: true
            });

            $('#channel' + channel.Id + '', page).html(html).trigger('create').createPosterItemMenus();
        });
    }

    function loadLatestLiveTvRecordings(elem, userId, index) {

        ApiClient.getLiveTvRecordings({

            userId: userId,
            limit: 9,
            IsInProgress: false

        }).done(function (result) {

            var html = '';

            if (result.Items.length) {

                var cssClass = index !== 0 ? 'listHeader' : 'listHeader firstListHeader';

                html += '<div>';
                html += '<h1 style="display:inline-block; vertical-align:middle;" class="' + cssClass + '">' + Globalize.translate('HeaderLatestTvRecordings') + '</h1>';
                html += '<a href="livetvrecordings.html?context=livetv" data-role="button" data-icon="arrow-r" data-mini="true" data-inline="true" data-iconpos="notext" class="sectionHeaderButton">d</a>';
                html += '</div>';
            }

            var screenWidth = $(window).width();

            html += LibraryBrowser.getPosterViewHtml({
                items: result.Items,
                shape: "autosmall",
                showTitle: true,
                showParentTitle: true,
                overlayText: screenWidth >= 600,
                coverImage: true,
                lazy: true
            });

            elem.html(html).trigger('create').createPosterItemMenus();

        });
    }

    window.Sections = {
        loadRecentlyAdded: loadRecentlyAdded,
        loadLatestChannelMedia: loadLatestChannelMedia,
        loadLibraryTiles: loadLibraryTiles,
        loadLibraryFolders: loadLibraryFolders,
        loadResume: loadResume,
        loadLatestChannelItems: loadLatestChannelItems,
        loadLatestLiveTvRecordings: loadLatestLiveTvRecordings,
        loadlibraryButtons: loadlibraryButtons
    };

})(jQuery, document, ApiClient);

(function ($, document, apiClient) {

    function getDefaultSection(index) {

        switch (index) {

            case 0:
                return 'smalllibrarytiles';
            case 1:
                return 'resume';
            case 2:
                return '';
            case 3:
                return '';
            default:
                return '';
        }

    }

    function loadSection(page, userId, displayPreferences, index) {

        var section = displayPreferences.CustomPrefs['home' + index] || getDefaultSection(index);

        var elem = $('.section' + index, page);

        if (section == 'latestmedia') {
            Sections.loadRecentlyAdded(elem, userId);
        }
        else if (section == 'librarytiles') {
            Sections.loadLibraryTiles(elem, userId, 'backdrop', index);
        }
        else if (section == 'smalllibrarytiles') {
            Sections.loadLibraryTiles(elem, userId, 'smallBackdrop', index);
        }
        else if (section == 'librarybuttons') {
            Sections.loadlibraryButtons(elem, userId, index);
        }
        else if (section == 'resume') {
            Sections.loadResume(elem, userId);
        }

        else if (section == 'latesttvrecordings') {
            Sections.loadLatestLiveTvRecordings(elem, userId);
        }

        else if (section == 'folders') {
            Sections.loadLibraryFolders(elem, userId, 'smallBackdrop', index);

        } else if (section == 'latestchannelmedia') {
            Sections.loadLatestChannelMedia(elem, userId);

        } else {

            elem.empty();
        }
    }

    function loadSections(page, userId, displayPreferences) {

        var i, length;
        var sectionCount = 4;

        var elem = $('.sections', page);

        if (!elem.html().length) {
            var html = '';
            for (i = 0, length = sectionCount; i < length; i++) {

                html += '<div class="homePageSection section' + i + '"></div>';
            }

            elem.html(html);
        }

        for (i = 0, length = sectionCount; i < length; i++) {

            loadSection(page, userId, displayPreferences, i);
        }
    }

    var homePageDismissValue = '2';

    function dismissWelcome(page, userId) {

        ApiClient.getDisplayPreferences('home', userId, 'webclient').done(function (result) {

            result.CustomPrefs.homePageWelcomeDismissed = homePageDismissValue;
            ApiClient.updateDisplayPreferences('home', result, userId, 'webclient').done(function () {

                $('.welcomeMessage', page).hide();

            });
        });
    }

    $(document).on('pageinit', "#indexPage", function () {

        var page = this;

        var userId = Dashboard.getCurrentUserId();

        $('.btnDismissWelcome', page).on('click', function () {
            dismissWelcome(page, userId);
        });

    }).on('pagebeforeshow', "#indexPage", function () {

        var page = this;

        var userId = Dashboard.getCurrentUserId();

        ApiClient.getDisplayPreferences('home', userId, 'webclient').done(function (result) {

            if (result.CustomPrefs.homePageWelcomeDismissed == homePageDismissValue) {
                $('.welcomeMessage', page).hide();
            } else {
                $('.welcomeMessage', page).show();
            }

            loadSections(page, userId, result);
        });

    });

})(jQuery, document, ApiClient);