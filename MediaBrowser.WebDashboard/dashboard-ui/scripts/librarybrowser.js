﻿var LibraryBrowser = (function (window, document, $, screen, localStorage) {

    $(function () {
        $("body").on("create", function () {
            $(".lazy").unveil(200);
        });
    });

    var defaultBackground = "#333";

    return {

        getDefaultPageSize: function () {

            var saved = localStorage.getItem('pagesize_');

            if (saved) {
                return parseInt(saved);
            }

            if (window.location.toString().toLowerCase().indexOf('localhost') != -1) {
                return 100;
            }
            return 20;
        },

        loadSavedQueryValues: function (key, query) {

            var values = localStorage.getItem(key + '_' + Dashboard.getCurrentUserId());

            if (values) {

                values = JSON.parse(values);

                return $.extend(query, values);
            }

            return query;
        },

        saveQueryValues: function (key, query) {

            var values = {};

            if (query.SortBy) {
                values.SortBy = query.SortBy;
            }
            if (query.SortOrder) {
                values.SortOrder = query.SortOrder;
            }

            try {
                localStorage.setItem(key + '_' + Dashboard.getCurrentUserId(), JSON.stringify(values));
            } catch (e) {
                
            }
        },

        saveViewSetting: function (key, value) {

            try {
                localStorage.setItem(key + '_' + Dashboard.getCurrentUserId() + '_view', value);
            } catch (e) {

            }
        },

        getSavedViewSetting: function (key) {

            var deferred = $.Deferred();
            var val = localStorage.getItem(key + '_' + Dashboard.getCurrentUserId() + '_view');

            deferred.resolveWith(null, [val]);
            return deferred.promise();
        },

        getDateParamValue: function (date) {

            function formatDigit(i) {
                return i < 10 ? "0" + i : i;
            }

            var d = date;

            return "" + d.getFullYear() + formatDigit(d.getMonth() + 1) + formatDigit(d.getDate()) + formatDigit(d.getHours()) + formatDigit(d.getMinutes()) + formatDigit(d.getSeconds());
        },

        getItemCountsHtml: function (options, item) {

            var counts = [];

            var childText;

            if (options.context == "movies") {

                if (item.MovieCount) {

                    childText = item.MovieCount == 1 ? "1 Movie" : item.MovieCount + " Movies";

                    counts.push(childText);
                }
                if (item.TrailerCount) {

                    childText = item.TrailerCount == 1 ? "1 Trailer" : item.TrailerCount + " Trailers";

                    counts.push(childText);
                }

            }
            else if (options.context == "tv") {

                if (item.SeriesCount) {

                    childText = item.SeriesCount == 1 ? "1 Show" : item.SeriesCount + " Shows";

                    counts.push(childText);
                }
                if (item.EpisodeCount) {

                    childText = item.EpisodeCount == 1 ? "1 Episode" : item.EpisodeCount + " Episodes";

                    counts.push(childText);
                }

            }
            else if (options.context == "games") {

                if (item.GameCount) {

                    childText = item.GameCount == 1 ? "1 Game" : item.GameCount + " Games";

                    counts.push(childText);
                }
            }
            else if (options.context == "music") {

                if (item.AlbumCount) {

                    childText = item.AlbumCount == 1 ? "1 Album" : item.AlbumCount + " Albums";

                    counts.push(childText);
                }
                if (item.SongCount) {

                    childText = item.SongCount == 1 ? "1 Song" : item.SongCount + " Songs";

                    counts.push(childText);
                }
                if (item.MusicVideoCount) {

                    childText = item.MusicVideoCount == 1 ? "1 Music Video" : item.MusicVideoCount + " Music Videos";

                    counts.push(childText);
                }
            }

            return counts.join(' • ');
        },

        getSongHeaderCellHtml: function (text, cssClass, enableSorting, sortField, selectedSortField, sortDirection) {

            var html = cssClass ? '<th class="' + cssClass + '">' : '<th>';

            if (text && enableSorting) {
                html += '<a class="lnkColumnSort" data-sortfield="' + sortField + '" href="#" style="text-decoration:underline;">';
            }

            html += text;

            if (text && enableSorting) {

                html += '</a>';

                if (sortField == selectedSortField) {

                    if (sortDirection == "Descending") {
                        html += '<span style="font-weight:bold;margin-left:5px;vertical-align:top;font-size:12px;">&darr;</span>';
                    } else {
                        html += '<span style="font-weight:bold;margin-left:5px;vertical-align:top;font-size:12px;">&uarr;</span>';
                    }
                }
            }

            html += '</th>';

            return html;
        },

        getSongTableHtml: function (items, options) {

            options = options || {};

            var html = '';

            var cssClass = "detailTable";

            html += '<div class="detailTableContainer"><table class="' + cssClass + '"><thead>';

            html += '<tr>';

            html += LibraryBrowser.getSongHeaderCellHtml('', '', options.enableColumnSorting);
            html += LibraryBrowser.getSongHeaderCellHtml('Disc', 'desktopColumn', options.enableColumnSorting);
            html += LibraryBrowser.getSongHeaderCellHtml('#', 'desktopColumn', options.enableColumnSorting);
            html += LibraryBrowser.getSongHeaderCellHtml('Track', '', options.enableColumnSorting, 'Name', options.sortBy, options.sortOrder);

            if (options.showAlbum) {
                html += LibraryBrowser.getSongHeaderCellHtml('Album', '', options.enableColumnSorting, 'Album,SortName', options.sortBy, options.sortOrder);
            }
            if (options.showArtist) {
                html += LibraryBrowser.getSongHeaderCellHtml('Artist', 'tabletColumn', options.enableColumnSorting, 'Artist,Album,SortName', options.sortBy, options.sortOrder);
            }
            if (options.showAlbumArtist) {
                html += LibraryBrowser.getSongHeaderCellHtml('Album Artist', 'tabletColumn', options.enableColumnSorting, 'AlbumArtist,Album,SortName', options.sortBy, options.sortOrder);
            }

            html += LibraryBrowser.getSongHeaderCellHtml('Runtime', 'tabletColumn', options.enableColumnSorting, 'Runtime,AlbumArtist,Album,SortName', options.sortBy, options.sortOrder);
            html += LibraryBrowser.getSongHeaderCellHtml('Plays', 'desktopColumn', options.enableColumnSorting, 'PlayCount,AlbumArtist,Album,SortName', options.sortBy, options.sortOrder);

            html += '</tr></thead>';

            html += '<tbody>';

            for (var i = 0, length = items.length; i < length; i++) {

                var item = items[i];

                html += '<tr>';

                html += '<td class="detailTableButtonsCell">';
                html += '<button class="btnPlay" data-icon="play" type="button" data-iconpos="notext" onclick="LibraryBrowser.showPlayMenu(this, \'' + item.Id + '\', \'Audio\', false, \'Audio\');" data-inline="true" title="Play">Play</button>';
                html += '</td>';

                html += '<td class="desktopColumn">' + (item.ParentIndexNumber || "") + '</td>';
                html += '<td class="desktopColumn">' + (item.IndexNumber || "") + '</td>';

                html += '<td><a href="' + LibraryBrowser.getHref(item, "music") + '">' + (item.Name || "") + '</a></td>';

                if (options.showAlbum) {
                    if (item.Album && item.ParentId) {
                        html += '<td><a href="itemdetails.html?id=' + item.ParentId + '">' + item.Album + '</a></td>';
                    } else {
                        html += '<td>' + (item.Album || '') + '</td>';
                    }
                }

                if (options.showArtist) {

                    if (item.Artists && item.Artists.length) {

                        var artistLinksHtml = LibraryBrowser.getArtistLinksHtml(item.Artists);

                        html += '<td class="tabletColumn">' + artistLinksHtml + '</td>';
                    }
                    else {
                        html += '<td class="tabletColumn"></td>';
                    }
                }

                if (options.showAlbumArtist) {

                    if (item.AlbumArtist) {

                        html += '<td class="tabletColumn">' + LibraryBrowser.getArtistLinksHtml([item.AlbumArtist]) + '</td>';

                    } else {
                        html += '<td class="tabletColumn"></td>';
                    }
                }

                var time = Dashboard.getDisplayTime(item.RunTimeTicks || 0);

                html += '<td class="tabletColumn">' + time + '</td>';

                html += '<td class="desktopColumn">' + (item.UserData ? item.UserData.PlayCount : 0) + '</td>';

                html += '</tr>';
            }

            html += '</tbody>';
            html += '</table></div>';

            return html;
        },

        getArtistLinksHtml: function (artists) {

            var html = [];

            for (var i = 0, length = artists.length; i < length; i++) {

                var artist = artists[i];

                html.push('<a href="itembynamedetails.html?context=music&musicartist=' + ApiClient.encodeName(artist) + '">' + artist + '</a>');

            }

            html = html.join(' / ');

            return html;
        },

        showPlayMenu: function (positionTo, itemId, itemType, isFolder, mediaType, resumePositionTicks) {

            if (!resumePositionTicks && mediaType != "Audio" && !isFolder) {
                MediaController.play(itemId);
                return;
            }

            $('.playFlyout').popup("close").remove();

            var html = '<div data-role="popup" class="playFlyout" data-history="false" data-theme="a">';

            html += '<ul data-role="listview" style="min-width: 180px;">';
            html += '<li data-role="list-divider">Play Menu</li>';

            html += '<li><a href="#" onclick="MediaController.play(\'' + itemId + '\');LibraryBrowser.closePlayMenu();">Play</a></li>';

            if (resumePositionTicks) {
                html += '<li><a href="#" onclick="MediaController.play({ids:[\'' + itemId + '\'],startPositionTicks:' + resumePositionTicks + '});LibraryBrowser.closePlayMenu();">Resume</a></li>';
            }

            if (MediaController.canQueueMediaType(mediaType)) {
                html += '<li><a href="#" onclick="MediaController.queue(\'' + itemId + '\');LibraryBrowser.closePlayMenu();">Queue</a></li>';
            }

            if (itemType == "Audio" || itemType == "MusicAlbum" || itemType == "MusicArtist" || itemType == "MusicGenre") {
                html += '<li><a href="#" onclick="MediaController.instantMix(\'' + itemId + '\');LibraryBrowser.closePlayMenu();">Instant Mix</a></li>';
            }

            if (isFolder || itemType == "MusicArtist" || itemType == "MusicGenre") {
                html += '<li><a href="#" onclick="MediaController.shuffle(\'' + itemId + '\');LibraryBrowser.closePlayMenu();">Shuffle</a></li>';
            }

            html += '</ul>';

            html += '</div>';

            $($.mobile.activePage).append(html);

            $('.playFlyout').popup({ positionTo: positionTo || "window" }).trigger('create').popup("open").on("popupafterclose", function () {

                $(this).off("popupafterclose").remove();

            }).parents(".ui-popup-container").css("margin-left", 55);
        },

        closePlayMenu: function () {
            $('.playFlyout').popup("close").remove();
        },

        getHref: function (item, context) {

            var href = LibraryBrowser.getHrefInternal(item);

            if (context) {
                href += href.indexOf('?') == -1 ? "?context=" : "&context=";
                href += context;
            }

            return href;
        },

        getHrefInternal: function (item) {

            if (!item) {
                throw new Error('item cannot be null');
            }

            if (item.url) {
                return item.url;
            }

            // Handle search hints
            var id = item.Id || item.ItemId;

            if (item.CollectionType == 'livetv') {
                return 'livetvsuggested.html';
            }

            if (item.CollectionType == 'channels') {
                return 'channelslatest.html';
            }

            if (item.CollectionType == 'movies') {
                return 'movieslatest.html?topParentId=' + item.Id;
            }

            if (item.CollectionType == 'boxsets') {
                return 'collections.html?topParentId=' + item.Id;
            }

            if (item.CollectionType == 'trailers') {
                return 'movietrailers.html?topParentId=' + item.Id;
            }

            if (item.CollectionType == 'movies') {
                return 'movieslatest.html?topParentId=' + item.Id;
            }

            if (item.CollectionType == 'tvshows') {
                return 'tvrecommended.html?topParentId=' + item.Id;
            }

            if (item.CollectionType == 'music') {
                return 'musicrecommended.html?topParentId=' + item.Id;
            }

            if (item.CollectionType == 'games') {
                return 'gamesrecommended.html?topParentId=' + item.Id;
            }
            if (item.Type == 'CollectionFolder') {
                return 'itemlist.html?topParentId=' + item.Id + '&parentid=' + item.Id;
            }

            if (item.Type == "TvChannel") {
                return "livetvchannel.html?id=" + id;
            }
            if (item.Type == "Channel") {
                return "channelitems.html?id=" + id;
            }
            if (item.Type == "ChannelFolderItem") {
                return "channelitems.html?id=" + item.ChannelId + '&folderId=' + item.Id;
            }
            if (item.Type == "Program") {
                return "livetvprogram.html?id=" + id;
            }
            if (item.Type == "Series") {
                return "itemdetails.html?id=" + id;
            }
            if (item.Type == "Season") {
                return "itemdetails.html?id=" + id;
            }
            if (item.Type == "BoxSet") {
                return "itemdetails.html?id=" + id;
            }
            if (item.Type == "MusicAlbum") {
                return "itemdetails.html?id=" + id;
            }
            if (item.Type == "GameSystem") {
                return "itemdetails.html?id=" + id;
            }
            if (item.Type == "Genre") {
                return "itembynamedetails.html?id=" + id;
            }
            if (item.Type == "MusicGenre") {
                return "itembynamedetails.html?id=" + id;
            }
            if (item.Type == "GameGenre") {
                return "itembynamedetails.html?id=" + id;
            }
            if (item.Type == "Studio") {
                return "itembynamedetails.html?id=" + id;
            }
            if (item.Type == "Person") {
                return "itembynamedetails.html?id=" + id;
            }
            if (item.Type == "Recording") {
                return "livetvrecording.html?id=" + id;
            }

            if (item.Type == "MusicArtist") {
                return "itembynamedetails.html?id=" + id;
            }

            if (item.IsFolder) {
                return id ? "itemlist.html?parentId=" + id : "#";
            }

            return "itemdetails.html?id=" + id;
        },

        getImageUrl: function (item, type, index, options) {

            options = options || {};
            options.type = type;
            options.index = index;

            if (type == 'Backdrop') {
                options.tag = item.BackdropImageTags[index];
            }
            else if (type == 'Screenshot') {
                options.tag = item.ScreenshotImageTags[index];
            }
            else if (type == 'Primary') {
                options.tag = item.PrimaryImageTag || item.ImageTags[type];
            }
            else {
                options.tag = item.ImageTags[type];
            }

            // For search hints
            return ApiClient.getScaledImageUrl(item.Id || item.ItemId, options);

        },

        getPosterViewHtml: function (options) {

            var items = options.items;
            var currentIndexValue;

            options.shape = options.shape || "portrait";

            var html = "";

            var primaryImageAspectRatio;

            if (options.shape == 'auto' || options.shape == 'autosmall') {

                primaryImageAspectRatio = LibraryBrowser.getAveragePrimaryImageAspectRatio(items);

                if (primaryImageAspectRatio && Math.abs(primaryImageAspectRatio - 1.777777778) < .3) {
                    options.shape = options.shape == 'auto' ? 'backdrop' : 'smallBackdrop';
                }
                else if (primaryImageAspectRatio && Math.abs(primaryImageAspectRatio - 1) < .33) {
                    options.coverImage = true;
                    options.shape = 'square';
                }
                else if (primaryImageAspectRatio && Math.abs(primaryImageAspectRatio - 1.3333334) < .01) {
                    options.coverImage = true;
                    options.shape = 'square';
                }
                else if (primaryImageAspectRatio && primaryImageAspectRatio > 1.9) {
                    options.shape = 'banner';
                    options.coverImage = true;
                }
                else if (primaryImageAspectRatio && Math.abs(primaryImageAspectRatio - 0.6666667) < .2) {
                    options.shape = 'portrait';
                }
                else {
                    options.shape = options.defaultShape || 'portrait';
                }
            }

            for (var i = 0, length = items.length; i < length; i++) {

                var item = items[i];

                primaryImageAspectRatio = LibraryBrowser.getAveragePrimaryImageAspectRatio([item]);

                var futureDateText;

                if (item.PremiereDate) {
                    try {

                        futureDateText = LibraryBrowser.getFutureDateText(parseISO8601Date(item.PremiereDate, { toLocal: true }), true);

                    } catch (err) {

                    }
                }

                if (options.showPremiereDateIndex && futureDateText) {

                    var val = futureDateText || "Unknown Date";

                    if (val != currentIndexValue) {

                        html += '<h2 class="timelineHeader detailSectionHeader" style="text-align:center;">' + val + '</h2>';
                        currentIndexValue = val;
                    }
                }
                else if (options.timeline) {
                    var year = item.ProductionYear || "Unknown Year";

                    if (year != currentIndexValue) {

                        html += '<h2 class="timelineHeader detailSectionHeader">' + year + '</h2>';
                        currentIndexValue = year;
                    }
                }

                var imgUrl = null;
                var background = null;
                var width = null;
                var height = null;
                var isGrouped = (item.Type == "Episode" && item.MediaSourceCount > 1);

                var forceName = false;

                var downloadHeight = 576;

                if (options.autoThumb && item.ImageTags && item.ImageTags.Primary && item.PrimaryImageAspectRatio && item.PrimaryImageAspectRatio >= 1.5) {

                    height = 400;
                    width = primaryImageAspectRatio ? Math.round(height * primaryImageAspectRatio) : null;

                    imgUrl = ApiClient.getImageUrl(item.Id, {
                        type: "Primary",
                        height: height,
                        width: width,
                        tag: item.ImageTags.Primary
                    });

                }
                else if (options.autoThumb && item.ImageTags && item.ImageTags.Thumb) {

                    imgUrl = ApiClient.getScaledImageUrl(item.Id, {
                        type: "Thumb",
                        maxWidth: downloadHeight,
                        tag: item.ImageTags.Thumb
                    });

                }
                else if (options.preferBackdrop && item.BackdropImageTags && item.BackdropImageTags.length) {

                    imgUrl = ApiClient.getScaledImageUrl(item.Id, {
                        type: "Backdrop",
                        maxWidth: downloadHeight,
                        tag: item.BackdropImageTags[0]
                    });

                }
                else if (options.preferThumb && item.ImageTags && item.ImageTags.Thumb) {

                    imgUrl = ApiClient.getScaledImageUrl(item.Id, {
                        type: "Thumb",
                        maxWidth: downloadHeight,
                        tag: item.ImageTags.Thumb
                    });

                }
                else if (options.preferBanner && item.ImageTags && item.ImageTags.Banner) {

                    imgUrl = ApiClient.getScaledImageUrl(item.Id, {
                        type: "Banner",
                        maxWidth: 500,
                        tag: item.ImageTags.Banner
                    });

                }
                else if (options.preferThumb && item.SeriesThumbImageTag) {

                    imgUrl = ApiClient.getScaledImageUrl(item.SeriesId, {
                        type: "Thumb",
                        maxWidth: downloadHeight,
                        tag: item.SeriesThumbImageTag
                    });

                }
                else if (options.preferThumb && item.ParentThumbItemId) {

                    imgUrl = ApiClient.getThumbImageUrl(item.ParentThumbItemId, {
                        type: "Thumb",
                        maxWidth: downloadHeight
                    });

                }
                else if (options.preferThumb && item.BackdropImageTags && item.BackdropImageTags.length) {

                    imgUrl = ApiClient.getScaledImageUrl(item.Id, {
                        type: "Backdrop",
                        maxWidth: downloadHeight,
                        tag: item.BackdropImageTags[0]
                    });

                    forceName = true;
                }
                else if (item.ImageTags && item.ImageTags.Primary && !isGrouped) {

                    height = 400;
                    width = primaryImageAspectRatio ? Math.round(height * primaryImageAspectRatio) : null;

                    imgUrl = ApiClient.getImageUrl(item.Id, {
                        type: "Primary",
                        height: height,
                        width: width,
                        tag: item.ImageTags.Primary
                    });

                }
                else if (item.AlbumId && item.AlbumPrimaryImageTag) {

                    height = 220;
                    width = primaryImageAspectRatio ? Math.round(height * primaryImageAspectRatio) : null;

                    imgUrl = ApiClient.getScaledImageUrl(item.AlbumId, {
                        type: "Primary",
                        height: height,
                        width: width,
                        tag: item.AlbumPrimaryImageTag
                    });

                }
                else if (item.BackdropImageTags && item.BackdropImageTags.length) {

                    imgUrl = ApiClient.getScaledImageUrl(item.Id, {
                        type: "Backdrop",
                        maxWidth: downloadHeight,
                        tag: item.BackdropImageTags[0]
                    });

                }
                else if (item.ImageTags && item.ImageTags.Thumb) {

                    imgUrl = ApiClient.getScaledImageUrl(item.Id, {
                        type: "Thumb",
                        maxWidth: downloadHeight,
                        tag: item.ImageTags.Thumb
                    });

                }
                else if (item.SeriesThumbImageTag) {

                    imgUrl = ApiClient.getScaledImageUrl(item.SeriesId, {
                        type: "Thumb",
                        maxWidth: downloadHeight,
                        tag: item.SeriesThumbImageTag
                    });

                }
                else if (item.ParentThumbItemId) {

                    imgUrl = ApiClient.getThumbImageUrl(item, {
                        type: "Thumb",
                        maxWidth: downloadHeight
                    });

                }
                else if (item.ParentBackdropImageTags && item.ParentBackdropImageTags.length) {

                    imgUrl = ApiClient.getImageUrl(item.ParentBackdropItemId, {
                        type: "Backdrop",
                        maxwidth: 576,
                        tag: item.ParentBackdropImageTags[0]
                    });

                }
                else if (item.MediaType == "Audio" || item.Type == "MusicAlbum" || item.Type == "MusicArtist") {

                    if (item.Name && options.showTitle) {
                        imgUrl = 'css/images/items/list/audio.png';
                    }
                    background = defaultBackground;

                }
                else if (item.Type == "Recording" || item.Type == "Program" || item.Type == "TvChannel") {

                    if (item.Name && options.showTitle) {
                        imgUrl = 'css/images/items/list/collection.png';
                    }

                    background = defaultBackground;
                }
                else if (item.MediaType == "Video" || item.Type == "Season" || item.Type == "Series") {

                    if (item.Name && options.showTitle) {
                        imgUrl = 'css/images/items/list/video.png';
                    }
                    background = defaultBackground;
                }
                else if (item.Type == "Person") {

                    if (item.Name && options.showTitle) {
                        imgUrl = 'css/images/items/list/person.png';
                    }
                    background = defaultBackground;
                }
                else {
                    if (item.Name && options.showTitle) {
                        imgUrl = 'css/images/items/list/collection.png';
                    }
                    background = defaultBackground;
                }

                var cssClass = "posterItem";

                if (options.transparent !== false) {
                    cssClass += " transparentPosterItem";
                }

                if (options.borderless) {
                    cssClass += " borderlessPosterItem";
                }

                cssClass += ' ' + options.shape + 'PosterItem';

                var mediaSourceCount = item.MediaSourceCount || 1;

                var hrefItem = item;

                // If the item is grouped we need to fudge a bit
                if (isGrouped) {
                    if (!options.groupType) {
                        if (item.MediaType == "Audio" || item.Type == "MusicAlbum" || item.Type == "MusicArtist") {
                            options.groupType = "MusicAlbum";
                        } else if (item.MediaType == "Video" || item.Type == "Season" || item.Type == "Series") {
                            options.groupType = "Season";
                        }
                    }

                    switch (options.groupType) {
                        case "Season":
                            hrefItem.Type = "Season";
                            hrefItem.Id = hrefItem.SeasonId;
                            break;
                        case "MusicAlbum":
                            hrefItem.Type = "MusicAlbum";
                            hrefItem.Id = hrefItem.AlbumId;
                            break;
                    }
                }

                var href = options.linkItem === false ? '#' : LibraryBrowser.getHref(hrefItem, options.context);

                if (item.UserData) {
                    cssClass += ' posterItemUserData' + item.UserData.Key;
                }

                var itemCommands = [];

                //if (MediaController.canPlay(item)) {
                //    itemCommands.push('playmenu');
                //}

                if (item.Type != "Recording" && item.Type != "Program") {
                    itemCommands.push('edit');
                }

                if (item.LocalTrailerCount) {
                    itemCommands.push('trailer');
                }

                html += '<a data-commands="' + itemCommands.join(',') + '" data-itemid="' + item.Id + '" class="' + cssClass + '" data-mediasourcecount="' + mediaSourceCount + '" href="' + href + '">';

                var style = "";

                if (imgUrl && !options.lazy) {
                    style += 'background-image:url(\'' + imgUrl + '\');';
                }

                if (background) {
                    style += "background-color:" + background + ";";
                }

                var imageCssClass = 'posterItemImage';
                if (options.coverImage) {
                    imageCssClass += " coveredPosterItemImage";
                }

                var dataSrc = "";

                if (options.lazy) {
                    imageCssClass += " lazy";
                    dataSrc = ' data-src="' + imgUrl + '"';
                }

                var progressHtml = options.showProgress === false ? '' : LibraryBrowser.getItemProgressBarHtml(item);

                html += '<div class="' + imageCssClass + '" style="' + style + '"' + dataSrc + '>';

                html += '<div class="posterItemOverlayTarget"></div>';

                if (item.LocationType == "Offline" || item.LocationType == "Virtual") {
                    if (options.showLocationTypeIndicator !== false) {
                        html += LibraryBrowser.getOfflineIndicatorHtml(item);
                    }
                } else if (options.showUnplayedIndicator !== false) {
                    html += LibraryBrowser.getPlayedIndicatorHtml(item);
                }

                if (mediaSourceCount > 1) {
                    html += '<div class="mediaSourceIndicator">' + mediaSourceCount + '</div>';
                }
                if (item.IsUnidentified) {
                    html += '<div class="unidentifiedIndicator"><div class="ui-icon-alert ui-btn-icon-notext"></div></div>';
                }

                if (options.selectionPanel) {
                    var chkItemSelectId = 'chkItemSelect' + i;

                    // Render this pre-enhanced to save on jquery mobile dom manipulation
                    html += '<div class="itemSelectionPanel" onclick="return false;" style="display:none;"><div class="ui-checkbox ui-mini"><label class="ui-btn ui-corner-all ui-btn-inherit ui-btn-icon-left ui-checkbox-off" for="' + chkItemSelectId + '">Select</label><input id="' + chkItemSelectId + '" type="checkbox" class="chkItemSelect" data-enhanced="true" /></div></div>';

                }

                if (!options.overlayText) {

                    if (progressHtml) {
                        html += '<div class="posterItemTextOverlay posterItemProgressContainer">';
                        html += "<div class='posterItemProgress miniPosterItemProgress'>";
                        html += progressHtml;
                        html += "</div>";
                        html += "</div>";
                    }
                }
                html += '</div>';

                var name = LibraryBrowser.getPosterViewDisplayName(item, options.displayAsSpecial);

                if (!imgUrl && !options.showTitle) {
                    html += "<div class='posterItemDefaultText'>";
                    html += htmlEncode(name);
                    html += "</div>";
                }

                var overlayText = options.overlayText || (forceName && !options.showTitle);

                if (overlayText) {
                    html += '<div class="posterItemTextOverlay">';
                }

                cssClass = options.centerText ? "posterItemText posterItemTextCentered" : "posterItemText";

                var lines = [];

                if (options.showParentTitle) {

                    lines.push(item.EpisodeTitle ? item.Name : (item.SeriesName || item.Album || item.AlbumArtist || item.GameSystem || ""));
                }

                if ((options.showTitle && !isGrouped) || forceName) {

                    lines.push(htmlEncode(name));
                }

                if (options.showItemCounts) {

                    var itemCountHtml = LibraryBrowser.getItemCountsHtml(options, item);

                    lines.push(itemCountHtml);
                }

                if (options.showPremiereDate && item.PremiereDate) {

                    try {

                        lines.push(LibraryBrowser.getPremiereDateText(item));

                    } catch (err) {
                        lines.push('');

                    }
                }

                html += LibraryBrowser.getPosterItemTextLines(lines, cssClass, !options.overlayText);

                if (options.overlayText) {

                    if (progressHtml) {
                        html += "<div class='posterItemText posterItemProgress posterItemProgressContainer'>";
                        html += progressHtml || "&nbsp;";
                        html += "</div>";
                    }
                }

                if (overlayText) {
                    html += "</div>";
                }

                html += "</a>";

            }

            return html;
        },

        getPosterItemTextLines: function (lines, cssClass, forceLines) {

            var html = '';

            var valid = 0;
            var i, length;

            for (i = 0, length = lines.length; i < length; i++) {

                var text = lines[i];

                if (text) {
                    html += "<div class='" + cssClass + "'>";
                    html += text;
                    html += "</div>";
                    valid++;
                }
            }

            if (forceLines) {
                while (valid < length) {
                    html += "<div class='" + cssClass + "'>&nbsp;</div>";
                    valid++;
                }
            }

            return html;
        },

        isYesterday: function (date1) {

            var today = new Date();
            today.setDate(today.getDate() - 1);

            return date1.getFullYear() == today.getFullYear() && date1.getDate() == today.getDate();

        },

        isSameDay: function (date1, date2) {

            return date1.getFullYear() == date2.getFullYear() && date1.getDate() == date2.getDate();

        },

        getFutureDateText: function (date, includeDayNamesInFuture) {

            var weekday = [];
            weekday[0] = "Sunday";
            weekday[1] = "Monday";
            weekday[2] = "Tuesday";
            weekday[3] = "Wednesday";
            weekday[4] = "Thursday";
            weekday[5] = "Friday";
            weekday[6] = "Saturday";

            var currentDate = new Date();

            if (LibraryBrowser.isSameDay(date, currentDate)) {
                return "Today";
            }

            if (LibraryBrowser.isYesterday(date)) {
                return "Yesterday";
            }

            var day = weekday[date.getDay()];
            date = date.toLocaleDateString();

            if (date.toLowerCase().indexOf(day.toLowerCase()) == -1) {
                return day + " " + date;
            }

            return date;
        },

        getPremiereDateText: function (item, date) {

            if (!date) {

                var text = '';

                if (item.AirTime) {
                    text += item.AirTime;
                }

                if (item.SeriesStudio) {

                    if (text) {
                        text += " on " + item.SeriesStudio;
                    } else {
                        text += item.SeriesStudio;
                    }
                }

                return text;
            }

            var day = LibraryBrowser.getFutureDateText(date);

            if (item.AirTime) {
                day += " at " + item.AirTime;
            }

            if (item.SeriesStudio) {
                day += " on " + item.SeriesStudio;
            }

            return day;
        },

        getPosterViewDisplayName: function (item, displayAsSpecial, includeParentInfo) {

            var name = item.EpisodeTitle || item.Name;

            if (item.Type == "TvChannel") {

                if (item.Number) {
                    return item.Number + ' ' + name;
                }
                return name;
            }
            if (displayAsSpecial && item.Type == "Episode" && item.ParentIndexNumber == 0) {

                name = "Special - " + name;

            }
            else if (item.Type == "Episode" && item.IndexNumber != null && item.ParentIndexNumber != null) {

                var displayIndexNumber = item.IndexNumber;

                var number = "E" + displayIndexNumber;

                if (includeParentInfo !== false) {
                    number = "S" + item.ParentIndexNumber + ", " + number;
                }

                if (item.IndexNumberEnd) {

                    displayIndexNumber = item.IndexNumberEnd;
                    number += "-" + displayIndexNumber;
                }

                name = number + " - " + name;

            }

            return name;
        },

        getOfflineIndicatorHtml: function (item) {

            if (item.LocationType == "Offline") {
                return '<div class="posterRibbon offlinePosterRibbon">Offline</div>';
            }

            try {

                var date = parseISO8601Date(item.PremiereDate, { toLocal: true });

                if (item.PremiereDate && (new Date().getTime() < date.getTime())) {
                    return '<div class="posterRibbon unairedPosterRibbon">Unaired</div>';
                }
            } catch (err) {

            }

            if (item.IsFolder) {
                return '';
            }
            return '<div class="posterRibbon missingPosterRibbon">Missing</div>';
        },

        getPlayedIndicatorHtml: function (item) {

            if (item.Type == "TvChannel") {
                return '';
            }
            if (item.Type == "Series" || item.Type == "Season" || item.Type == "BoxSet" || item.MediaType == "Video" || item.MediaType == "Game" || item.MediaType == "Book") {
                if (item.RecursiveUnplayedItemCount) {
                    return '<div class="unplayedIndicator">' + item.RecursiveUnplayedItemCount + '</div>';
                }

                if (item.PlayedPercentage == 100 || (item.UserData && item.UserData.Played)) {
                    return '<div class="playedIndicator"><div class="ui-icon-check ui-btn-icon-notext"></div></div>';
                }
            }

            return '';
        },

        getAveragePrimaryImageAspectRatio: function (items) {

            var values = [];

            for (var i = 0, length = items.length; i < length; i++) {

                var ratio = items[i].PrimaryImageAspectRatio || 0;

                if (!ratio) {
                    continue;
                }

                values[values.length] = ratio;
            }

            if (!values.length) {
                return null;
            }

            // Use the median
            values.sort(function (a, b) { return a - b; });

            var half = Math.floor(values.length / 2);

            var result;

            if (values.length % 2)
                result = values[half];
            else
                result = (values[half - 1] + values[half]) / 2.0;

            // If really close to 2:3 (poster image), just return 2:3
            if (Math.abs(0.66666666667 - result) <= .15) {
                return 0.66666666667;
            }

            // If really close to 16:9 (episode image), just return 16:9
            if (Math.abs(1.777777778 - result) <= .15) {
                return 1.777777778;
            }

            // If really close to 1 (square image), just return 1
            if (Math.abs(1 - result) <= .15) {
                return 1;
            }

            // If really close to 4:3 (poster image), just return 2:3
            if (Math.abs(1.33333333333 - result) <= .15) {
                return 1.33333333333;
            }

            return result;
        },

        metroColors: ["#6FBD45", "#4BB3DD", "#4164A5", "#E12026", "#800080", "#E1B222", "#008040", "#0094FF", "#FF00C7", "#FF870F", "#7F0037"],

        getRandomMetroColor: function () {

            var index = Math.floor(Math.random() * (LibraryBrowser.metroColors.length - 1));

            return LibraryBrowser.metroColors[index];
        },

        getMetroColor: function (str) {

            if (str) {
                var character = String(str.substr(0, 1).charCodeAt());
                var sum = 0;
                for (var i = 0; i < character.length; i++) {
                    sum += parseInt(character.charAt(i));
                }
                var index = String(sum).substr(-1);

                return LibraryBrowser.metroColors[index];
            } else {
                return LibraryBrowser.getRandomMetroColor();
            }

        },

        renderName: function (item, nameElem, linkToElement) {

            var name = LibraryBrowser.getPosterViewDisplayName(item, false, false);

            Dashboard.setPageTitle(name);

            if (linkToElement) {
                nameElem.html('<a class="detailPageParentLink" href="' + LibraryBrowser.getHref(item) + '">' + name + '</a>').trigger('create');
            } else {
                nameElem.html(name);
            }
        },

        renderParentName: function (item, parentNameElem) {

            var html = [];

            if (item.AlbumArtist && item.Type == "Audio") {
                html.push('<a class="detailPageParentLink" href="itembynamedetails.html?context=music&musicartist=' + ApiClient.encodeName(item.AlbumArtist) + '">' + item.AlbumArtist + '</a>');
            }
            else if (item.AlbumArtist && item.Type == "MusicAlbum") {
                html.push('<a class="detailPageParentLink" href="itembynamedetails.html?context=music&musicartist=' + ApiClient.encodeName(item.AlbumArtist) + '">' + item.AlbumArtist + '</a>');
            }
            else if (item.Artists && item.Artists.length && item.Type == "MusicVideo") {
                html.push('<a class="detailPageParentLink" href="itembynamedetails.html?context=music&musicartist=' + ApiClient.encodeName(item.Artists[0]) + '">' + item.Artists[0] + '</a>');
            }
            else if (item.SeriesName && item.Type == "Episode") {

                html.push('<a class="detailPageParentLink" href="itemdetails.html?id=' + item.SeriesId + '">' + item.SeriesName + '</a>');
            }

            if (item.SeriesName && item.Type == "Season") {

                html.push('<a class="detailPageParentLink" href="itemdetails.html?id=' + item.SeriesId + '">' + item.SeriesName + '</a>');
            }
            else if (item.ParentIndexNumber != null && item.Type == "Episode") {

                html.push('<a class="detailPageParentLink" href="itemdetails.html?id=' + item.SeasonId + '">Season ' + item.ParentIndexNumber + '</a>');
            }
            else if (item.Album && item.Type == "Audio" && (item.AlbumId || item.ParentId)) {
                html.push('<a class="detailPageParentLink" href="itemdetails.html?id=' + (item.AlbumId || item.ParentId) + '">' + item.Album + '</a>');

            }
            else if (item.Album && item.Type == "MusicVideo" && item.AlbumId) {
                html.push('<a class="detailPageParentLink" href="itemdetails.html?id=' + item.AlbumId + '">' + item.Album + '</a>');

            }
            else if (item.AlbumArtist && item.Type == "MusicAlbum") {

            }
            else if (item.Album) {
                html.push(item.Album);

            }

            if (html.length) {
                parentNameElem.show().html(html.join(' - ')).trigger('create');
            } else {
                parentNameElem.hide();
            }
        },

        renderLinks: function (linksElem, item) {

            var links = [];

            if (item.HomePageUrl) {
                links.push('<a class="textlink" href="' + item.HomePageUrl + '" target="_blank">Website</a>');
            }

            if (item.ExternalUrls) {

                for (var i = 0, length = item.ExternalUrls.length; i < length; i++) {

                    var url = item.ExternalUrls[i];

                    links.push('<a class="textlink" href="' + url.Url + '" target="_blank">' + url.Name + '</a>');
                }
            }

            if (links.length) {

                var html = 'Links:&nbsp;&nbsp;' + links.join('&nbsp;&nbsp;/&nbsp;&nbsp;');

                $(linksElem).html(html).trigger('create');

            } else {
                $(linksElem).hide();
            }
        },

        getPagingHtml: function (query, totalRecordCount, updatePageSizeSetting, pageSizes, showLimit) {

            if (query.Limit && updatePageSizeSetting !== false) {
                try {
                    localStorage.setItem('pagesize_', query.Limit);
                } catch (e) {

                }
            }

            var html = '';

            var recordsEnd = Math.min(query.StartIndex + query.Limit, totalRecordCount);

            // 20 is the minimum page size
            var showControls = totalRecordCount > 20 || query.Limit < totalRecordCount;

            html += '<div class="listPaging">';

            html += '<span style="margin-right: 10px;vertical-align:middle;">';

            var startAtDisplay = totalRecordCount ? query.StartIndex + 1 : 0;
            html += startAtDisplay + '-' + recordsEnd + ' of ' + totalRecordCount;

            html += '</span>';

            if (showControls) {

                html += '<div data-role="controlgroup" data-type="horizontal" style="display:inline-block;">';
                html += '<button data-icon="arrow-l" data-iconpos="notext" data-inline="true" data-mini="true" class="btnPreviousPage" ' + (query.StartIndex ? '' : 'disabled') + '>Previous Page</button>';

                html += '<button data-icon="arrow-r" data-iconpos="notext" data-inline="true" data-mini="true" class="btnNextPage" ' + (query.StartIndex + query.Limit >= totalRecordCount ? 'disabled' : '') + '>Next Page</button>';
                html += '</div>';

                if (showLimit !== false) {
                    var id = "selectPageSize" + new Date().getTime();

                    var options = '';

                    function getOption(val) {

                        if (query.Limit == val) {

                            return '<option value="' + val + '" selected="selected">' + val + '</option>';

                        } else {
                            return '<option value="' + val + '">' + val + '</option>';
                        }
                    }

                    pageSizes = pageSizes || [20, 50, 100, 200, 300, 400, 500];

                    for (var j = 0, length = pageSizes.length; j < length; j++) {
                        options += getOption(pageSizes[j]);
                    }

                    // Add styles to defeat jquery mobile
                    html += '<div class="pageSizeContainer"><label style="font-size:inherit;" class="labelPageSize" for="' + id + '">Limit: </label><select class="selectPageSize" id="' + id + '" data-inline="true" data-mini="true">' + options + '</select></div>';
                }
            }

            html += '</div>';

            return html;
        },

        getRatingHtml: function (item, metascore) {

            var html = "";

            if (item.CommunityRating) {

                html += "<div class='starRating' title='" + item.CommunityRating + "'></div>";
                html += '<div class="starRatingValue">';
                html += item.CommunityRating.toFixed(1);
                html += '</div>';
            }

            if (item.CriticRating != null) {

                if (item.CriticRating >= 60) {
                    html += '<div class="fresh rottentomatoesicon" title="fresh"></div>';
                } else {
                    html += '<div class="rotten rottentomatoesicon" title="rotten"></div>';
                }

                html += '<div class="criticRating">' + item.CriticRating + '%</div>';
            }

            if (item.Metascore && metascore !== false) {

                if (item.Metascore >= 60) {
                    html += '<div class="metascore metascorehigh" title="Metascore">' + item.Metascore + '</div>';
                }
                else if (item.Metascore >= 40) {
                    html += '<div class="metascore metascoremid" title="Metascore">' + item.Metascore + '</div>';
                } else {
                    html += '<div class="metascore metascorelow" title="Metascore">' + item.Metascore + '</div>';
                }
            }

            return html;
        },

        getItemProgressBarHtml: function (item) {


            if (item.Type == "Recording" && item.CompletionPercentage) {

                return '<progress class="itemProgressBar recordingProgressBar" min="0" max="100" value="' + item.CompletionPercentage + '"></progress>';
            }

            if (item.UserData && item.UserData.PlaybackPositionTicks && item.RunTimeTicks) {

                var tooltip = Dashboard.getDisplayTime(item.UserData.PlaybackPositionTicks) + " / " + Dashboard.getDisplayTime(item.RunTimeTicks);

                var pct = (item.UserData.PlaybackPositionTicks / item.RunTimeTicks) * 100;

                if (pct && pct < 100) {

                    return '<progress title="' + tooltip + '" class="itemProgressBar" min="0" max="100" value="' + pct + '"></progress>';
                }
            }

            return null;
        },

        getUserDataIconsHtml: function (item) {

            var html = '';

            var userData = item.UserData || {};

            var itemId = item.Id;
            var type = item.Type;

            if ((item.MediaType || item.IsFolder) && item.Type != "TvChannel" && item.Type != "MusicArtist") {
                if (userData.Played) {
                    html += '<img data-type="' + type + '" data-itemid="' + itemId + '" class="imgUserItemRating imgPlayed" src="css/images/userdata/checkedon.png" alt="Played" title="Played" onclick="LibraryBrowser.markPlayed(this);return false;" />';
                } else {
                    html += '<img data-type="' + type + '" data-itemid="' + itemId + '" class="imgUserItemRating imgPlayedOff" src="css/images/userdata/checkedoff.png" alt="Played" title="Played" onclick="LibraryBrowser.markPlayed(this);return false;" />';
                }
            }

            if (typeof userData.Likes == "undefined") {
                html += '<img onclick="LibraryBrowser.markDislike(this);return false;" data-itemid="' + itemId + '" class="imgUserItemRating imgDislikeOff" src="css/images/userdata/thumbs_down_off.png" alt="Dislike" title="Dislike" />';
                html += '<img onclick="LibraryBrowser.markLike(this);return false;" data-itemid="' + itemId + '" class="imgUserItemRating imgLikeOff" src="css/images/userdata/thumbs_up_off.png" alt="Like" title="Like" />';
            }
            else if (userData.Likes) {
                html += '<img onclick="LibraryBrowser.markDislike(this);return false;" data-itemid="' + itemId + '" class="imgUserItemRating imgDislikeOff" src="css/images/userdata/thumbs_down_off.png" alt="Dislike" title="Dislike" />';
                html += '<img onclick="LibraryBrowser.markLike(this);return false;" data-itemid="' + itemId + '" class="imgUserItemRating imgLike" src="css/images/userdata/thumbs_up_on.png" alt="Like" title="Like" />';
            }
            else {
                html += '<img onclick="LibraryBrowser.markDislike(this);return false;" data-itemid="' + itemId + '" class="imgUserItemRating imgDislike" src="css/images/userdata/thumbs_down_on.png" alt="Dislike" title="Dislike" />';
                html += '<img onclick="LibraryBrowser.markLike(this);return false;" data-itemid="' + itemId + '" class="imgUserItemRating imgLikeOff" src="css/images/userdata/thumbs_up_off.png" alt="Like" title="Like" />';
            }

            if (userData.IsFavorite) {
                html += '<img onclick="LibraryBrowser.markFavorite(this);return false;" data-itemid="' + itemId + '" class="imgUserItemRating imgFavorite" src="css/images/userdata/heart_on.png" alt="Favorite" title="Favorite" />';
            } else {
                html += '<img onclick="LibraryBrowser.markFavorite(this);return false;" data-itemid="' + itemId + '" class="imgUserItemRating imgFavoriteOff" src="css/images/userdata/heart_off.png" alt="Favorite" title="Favorite" />';
            }

            return html;
        },

        markPlayed: function (link) {

            var id = link.getAttribute('data-itemid');

            var $link = $(link);

            var markAsPlayed = $link.hasClass('imgPlayedOff');

            if (markAsPlayed) {
                ApiClient.markPlayed(Dashboard.getCurrentUserId(), id);
            } else {
                ApiClient.markUnplayed(Dashboard.getCurrentUserId(), id);
            }

            if (markAsPlayed) {
                link.src = "css/images/userdata/checkedon.png";
                $link.addClass('imgPlayed').removeClass('imgPlayedOff');
            } else {
                link.src = "css/images/userdata/checkedoff.png";
                $link.addClass('imgPlayedOff').removeClass('imgPlayed');
            }
        },

        markFavorite: function (link) {

            var id = link.getAttribute('data-itemid');

            var $link = $(link);

            var markAsFavorite = $link.hasClass('imgFavoriteOff');

            ApiClient.updateFavoriteStatus(Dashboard.getCurrentUserId(), id, markAsFavorite);

            if (markAsFavorite) {
                link.src = "css/images/userdata/heart_on.png";
                $link.addClass('imgFavorite').removeClass('imgFavoriteOff');
            } else {
                link.src = "css/images/userdata/heart_off.png";
                $link.addClass('imgFavoriteOff').removeClass('imgFavorite');
            }
        },

        markLike: function (link) {

            var id = link.getAttribute('data-itemid');

            var $link = $(link);

            if ($link.hasClass('imgLikeOff')) {

                ApiClient.updateUserItemRating(Dashboard.getCurrentUserId(), id, true);

                link.src = "css/images/userdata/thumbs_up_on.png";
                $link.addClass('imgLike').removeClass('imgLikeOff');

            } else {

                ApiClient.clearUserItemRating(Dashboard.getCurrentUserId(), id);

                link.src = "css/images/userdata/thumbs_up_off.png";
                $link.addClass('imgLikeOff').removeClass('imgLike');
            }

            $link.prev().removeClass('imgDislike').addClass('imgDislikeOff').each(function () {
                this.src = "css/images/userdata/thumbs_down_off.png";
            });
        },

        markDislike: function (link) {

            var id = link.getAttribute('data-itemid');

            var $link = $(link);

            if ($link.hasClass('imgDislikeOff')) {

                ApiClient.updateUserItemRating(Dashboard.getCurrentUserId(), id, false);

                link.src = "css/images/userdata/thumbs_down_on.png";
                $link.addClass('imgDislike').removeClass('imgDislikeOff');

            } else {

                ApiClient.clearUserItemRating(Dashboard.getCurrentUserId(), id);

                link.src = "css/images/userdata/thumbs_down_off.png";
                $link.addClass('imgDislikeOff').removeClass('imgDislike');
            }

            $link.next().removeClass('imgLike').addClass('imgLikeOff').each(function () {
                this.src = "css/images/userdata/thumbs_up_off.png";
            });
        },

        getDetailImageHtml: function (item, href) {

            var imageTags = item.ImageTags || {};

            if (item.PrimaryImageTag) {
                imageTags.Primary = item.PrimaryImageTag;
            }

            var html = '';

            var url;

            var imageHeight = 280;

            if (imageTags.Primary) {

                url = ApiClient.getScaledImageUrl(item.Id, {
                    type: "Primary",
                    maxHeight: imageHeight,
                    tag: item.ImageTags.Primary
                });
            }
            else if (item.BackdropImageTags && item.BackdropImageTags.length) {

                url = ApiClient.getScaledImageUrl(item.Id, {
                    type: "Backdrop",
                    maxHeight: imageHeight,
                    tag: item.BackdropImageTags[0]
                });
            }
            else if (imageTags.Thumb) {

                url = ApiClient.getScaledImageUrl(item.Id, {
                    type: "Thumb",
                    maxHeight: imageHeight,
                    tag: item.ImageTags.Thumb
                });
            }
            else if (imageTags.Disc) {

                url = ApiClient.getScaledImageUrl(item.Id, {
                    type: "Disc",
                    maxHeight: imageHeight,
                    tag: item.ImageTags.Disc
                });
            }
            else if (item.AlbumId && item.AlbumPrimaryImageTag) {

                url = ApiClient.getScaledImageUrl(item.AlbumId, {
                    type: "Primary",
                    maxHeight: imageHeight,
                    tag: item.AlbumPrimaryImageTag
                });

            }
            else if (item.MediaType == "Audio" || item.Type == "MusicAlbum" || item.Type == "MusicGenre") {
                url = "css/images/items/detail/audio.png";
            }
            else if (item.MediaType == "Game" || item.Type == "GameGenre") {
                url = "css/images/items/detail/game.png";
            }
            else if (item.Type == "Person") {
                url = "css/images/items/detail/person.png";
            }
            else if (item.Type == "Genre" || item.Type == "Studio") {
                url = "css/images/items/detail/video.png";
            }
            else if (item.Type == "TvChannel") {
                url = "css/images/items/detail/tv.png";
            }
            else {
                url = "css/images/items/detail/video.png";
            }

            var identifierName = "id";
            var identifierValue = item.Id;

            if (item.Type == "Person" || item.Type == "Genre" || item.Type == "Studio" || item.Type == "MusicArtist" || item.Type == "MusicGenre" || item.Type == "GameGenre") {
                identifierName = item.Type;
                identifierValue = ApiClient.encodeName(item.Name);
            }

            if (!href) {
                href = "itemgallery.html?" + identifierName + "=" + identifierValue;
            }

            var linkToGallery = LibraryBrowser.shouldDisplayGallery(item);

            html += '<div style="position:relative;">';
            if (linkToGallery) {
                html += "<a class='itemDetailGalleryLink' href='" + href + "'>";
            }

            html += "<img class='itemDetailImage' src='" + url + "' />";

            if (linkToGallery) {
                html += "</a>";
            }

            var progressHtml = LibraryBrowser.getItemProgressBarHtml(item);

            if (progressHtml) {
                html += '<div class="detailImageProgressContainer">';
                html += progressHtml;
                html += "</div>";
            }

            html += "</div>";

            return html;
        },

        getMiscInfoHtml: function (item) {

            var miscInfo = [];
            var text, date;

            if (item.Type == "Episode") {

                if (item.PremiereDate) {

                    try {
                        date = parseISO8601Date(item.PremiereDate, { toLocal: true });

                        text = date.toLocaleDateString();
                        miscInfo.push(text);
                    }
                    catch (e) {
                        console.log("Error parsing date: " + item.PremiereDate);
                    }
                }
            }

            if (item.StartDate) {

                try {
                    date = parseISO8601Date(item.StartDate, { toLocal: true });

                    text = date.toLocaleDateString();
                    miscInfo.push(text);

                    if (item.Type != "Recording") {
                        text = LiveTvHelpers.getDisplayTime(date);
                        miscInfo.push(text);
                    }
                }
                catch (e) {
                    console.log("Error parsing date: " + item.PremiereDate);
                }
            }

            if (item.ProductionYear && item.Type == "Series") {

                if (item.Status == "Continuing") {
                    miscInfo.push(item.ProductionYear + "-Present");

                }
                else if (item.ProductionYear) {

                    text = item.ProductionYear;

                    if (item.EndDate) {

                        try {

                            var endYear = parseISO8601Date(item.EndDate, { toLocal: true }).getFullYear();

                            if (endYear != item.ProductionYear) {
                                text += "-" + parseISO8601Date(item.EndDate, { toLocal: true }).getFullYear();
                            }

                        }
                        catch (e) {
                            console.log("Error parsing date: " + item.EndDate);
                        }
                    }

                    miscInfo.push(text);
                }
            }

            if (item.Type != "Series" && item.Type != "Episode") {

                if (item.ProductionYear) {

                    miscInfo.push(item.ProductionYear);
                }
                else if (item.PremiereDate) {

                    try {
                        text = parseISO8601Date(item.PremiereDate, { toLocal: true }).getFullYear();
                        miscInfo.push(text);
                    }
                    catch (e) {
                        console.log("Error parsing date: " + item.PremiereDate);
                    }
                }
            }

            var minutes;

            if (item.RunTimeTicks && item.Type != "Series") {

                if (item.Type == "Audio") {

                    miscInfo.push(Dashboard.getDisplayTime(item.RunTimeTicks));

                } else {
                    minutes = item.RunTimeTicks / 600000000;

                    minutes = minutes || 1;

                    miscInfo.push(Math.round(minutes) + "min");
                }
            }

            if (item.OfficialRating && item.Type !== "Season" && item.Type !== "Episode") {
                miscInfo.push(item.OfficialRating);
            }

            if (item.Video3DFormat) {
                miscInfo.push("3D");
            }

            return miscInfo.join('&nbsp;&nbsp;&nbsp;&nbsp;');
        },

        renderOverview: function (elem, item) {

            var overview = item.Overview || '';

            elem.html(overview).trigger('create');

            $('a', elem).each(function () {
                $(this).attr("target", "_blank");
            });

        },

        renderStudios: function (elem, item, context) {

            if (item.Studios && item.Studios.length && item.Type != "Series") {

                var prefix = item.Studios.length > 1 ? "Studios" : "Studio";
                var html = prefix + ':&nbsp;&nbsp;';

                for (var i = 0, length = item.Studios.length; i < length; i++) {

                    if (i > 0) {
                        html += '&nbsp;&nbsp;/&nbsp;&nbsp;';
                    }

                    html += '<a class="textlink" href="itembynamedetails.html?context=' + context + '&id=' + item.Studios[i].Id + '">' + item.Studios[i].Name + '</a>';
                }

                elem.show().html(html).trigger('create');


            } else {
                elem.hide();
            }
        },

        renderGenres: function (elem, item, context) {

            var html = '';

            var genres = item.Genres || [];

            for (var i = 0, length = genres.length; i < length; i++) {

                if (i > 0) {
                    html += '<span>&nbsp;&nbsp;/&nbsp;&nbsp;</span>';
                }

                var param = item.Type == "Audio" || item.Type == "MusicArtist" || item.Type == "MusicAlbum" ? "musicgenre" : "genre";

                if (item.MediaType == "Game") {
                    param = "gamegenre";
                }

                html += '<a class="textlink" href="itembynamedetails.html?context=' + context + '&' + param + '=' + ApiClient.encodeName(genres[i]) + '">' + genres[i] + '</a>';
            }

            elem.html(html).trigger('create');
        },

        renderPremiereDate: function (elem, item) {
            if (item.PremiereDate) {
                try {

                    var date = parseISO8601Date(item.PremiereDate, { toLocal: true });

                    var text = new Date().getTime() > date.getTime() ? "Premiered" : "Premieres";

                    elem.show().html(text + '&nbsp;&nbsp;' + date.toLocaleDateString());
                } catch (err) {
                    elem.hide();
                }
            } else {
                elem.hide();
            }
        },

        renderBudget: function (elem, item) {
            if (item.Budget) {

                elem.show().html('Budget:&nbsp;&nbsp;$<span>' + item.Budget + '</span>');
            } else {
                elem.hide();
            }
        },

        renderRevenue: function (elem, item) {
            if (item.Revenue) {

                elem.show().html('Revenue:&nbsp;&nbsp;$<span>' + item.Revenue + '</span>');
            } else {
                elem.hide();
            }
        },

        renderAwardSummary: function (elem, item) {
            if (item.AwardSummary) {
                elem.show().html('Awards:&nbsp;&nbsp;' + item.AwardSummary);
            } else {
                elem.hide();
            }
        },

        renderDetailPageBackdrop: function (page, item) {

            var screenWidth = Math.max(screen.height, screen.width);

            var imgUrl;

            if (item.BackdropImageTags && item.BackdropImageTags.length) {

                imgUrl = ApiClient.getScaledImageUrl(item.Id, {
                    type: "Backdrop",
                    index: 0,
                    maxWidth: screenWidth,
                    tag: item.BackdropImageTags[0]
                });

                $('#itemBackdrop', page).removeClass('noBackdrop').css('background-image', 'url("' + imgUrl + '")');

            }
            else if (item.ParentBackdropItemId && item.ParentBackdropImageTags && item.ParentBackdropImageTags.length) {

                imgUrl = ApiClient.getScaledImageUrl(item.ParentBackdropItemId, {
                    type: 'Backdrop',
                    index: 0,
                    tag: item.ParentBackdropImageTags[0],
                    maxWidth: screenWidth
                });

                $('#itemBackdrop', page).removeClass('noBackdrop').css('background-image', 'url("' + imgUrl + '")');

            }
            else {

                $('#itemBackdrop', page).addClass('noBackdrop').css('background-image', 'none');
            }
        },

        shouldDisplayGallery: function (item) {

            var imageTags = item.ImageTags || {};

            if (imageTags.Primary) {

                return true;
            }

            if (imageTags.Banner) {

                return true;
            }

            if (imageTags.Logo) {

                return true;
            }
            if (imageTags.Thumb) {

                return true;
            }
            if (imageTags.Art) {

                return true;

            }
            if (imageTags.Menu) {

                return true;

            }
            if (imageTags.Disc) {

                return true;
            }
            if (imageTags.Box) {

                return true;
            }
            if (imageTags.BoxRear) {

                return true;
            }

            if (item.BackdropImageTags && item.BackdropImageTags.length) {
                return true;

            }

            if (item.ScreenshotImageTags && item.ScreenshotImageTags.length) {
                return true;
            }

            return false;
        },

        getGalleryHtml: function (item) {

            var html = '';
            var i, length;

            var imageTags = item.ImageTags || {};

            if (imageTags.Primary) {

                html += LibraryBrowser.createGalleryImage(item, "Primary", imageTags.Primary);
            }

            if (imageTags.Banner) {

                html += LibraryBrowser.createGalleryImage(item, "Banner", imageTags.Banner);
            }

            if (imageTags.Logo) {

                html += LibraryBrowser.createGalleryImage(item, "Logo", imageTags.Logo);
            }
            if (imageTags.Thumb) {

                html += LibraryBrowser.createGalleryImage(item, "Thumb", imageTags.Thumb);
            }
            if (imageTags.Art) {

                html += LibraryBrowser.createGalleryImage(item, "Art", imageTags.Art);

            }
            if (imageTags.Menu) {

                html += LibraryBrowser.createGalleryImage(item, "Menu", imageTags.Menu);

            }
            if (imageTags.Box) {

                html += LibraryBrowser.createGalleryImage(item, "Box", imageTags.Box);
            }
            if (imageTags.BoxRear) {

                html += LibraryBrowser.createGalleryImage(item, "BoxRear", imageTags.BoxRear);
            }

            if (item.BackdropImageTags) {

                for (i = 0, length = item.BackdropImageTags.length; i < length; i++) {
                    html += LibraryBrowser.createGalleryImage(item, "Backdrop", item.BackdropImageTags[i], i);
                }

            }

            if (item.ScreenshotImageTags) {

                for (i = 0, length = item.ScreenshotImageTags.length; i < length; i++) {
                    html += LibraryBrowser.createGalleryImage(item, "Screenshot", item.ScreenshotImageTags[i], i);
                }
            }
            if (imageTags.Disc) {

                html += LibraryBrowser.createGalleryImage(item, "Disc", imageTags.Disc);
            }

            return html;
        },

        createGalleryImage: function (item, type, tag, index) {

            var screenWidth = Math.max(screen.height, screen.width);

            var html = '';

            if (typeof (index) == "undefined") index = 0;

            html += '<div class="galleryImageContainer">';
            html += '<a href="#pop_' + index + '_' + tag + '" data-rel="popup" data-position-to="window">';

            html += '<img class="galleryImage" src="' + LibraryBrowser.getImageUrl(item, type, index, {
                maxWidth: screenWidth,
                tag: tag
            }) + '" />';
            html += '</div>';

            html += '<div class="galleryPopup" id="pop_' + index + '_' + tag + '" data-role="popup">';
            html += '<a href="#" data-rel="back" data-role="button" data-icon="delete" data-iconpos="notext" class="ui-btn-right">Close</a>';
            html += '<img class="" src="' + LibraryBrowser.getImageUrl(item, type, index, {

                maxWidth: screenWidth,
                tag: tag

            }) + '" />';
            html += '</div>';

            return html;
        },

        groupItmes: function (individualItems, limit) {

            var results = individualItems;
            var group = {};
            var items = [], list = [];
            var cur;

            // Get a list and counts of each
            for (var i = 0, j = results.length; i < j; i++) {
                if (list.length == limit) {
                    break;
                }

                cur = results[i];

                var name;
                switch (cur.Type) {
                    case "Episode":
                        name = cur.SeriesName;
                        break;
                    case "Audio":
                        name = cur.Album;
                        break;
                }

                if (!name) {
                    // Not music/series, so just add it
                    group[cur.Name] = { item: cur, count: 1 };
                    list.push(group[cur.Name]);
                } else if (!(cur.SeriesName in group)) {
                    // Add a new series/album
                    group[name] = { item: cur, count: 1 };
                    list.push(group[name]);
                } else {
                    // Increment counter for item
                    group[name].count += 1;
                }
            }

            // Put the items into the list to pass to the library browser
            for (var i = 0, j = list.length; i < j; i++) {
                var item = list[i].item;
                item.MediaSourceCount = list[i].count;
                items.push(item);
            }

            return items;
        }

    };

})(window, document, jQuery, screen, localStorage);