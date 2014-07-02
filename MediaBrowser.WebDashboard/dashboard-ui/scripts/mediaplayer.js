(function (document, setTimeout, clearTimeout, screen, localStorage, $, setInterval, window) {

    function mediaPlayer() {

        var self = this;

        var testableVideoElement = document.createElement('video');
        var currentProgressInterval;
        var canClientSeek;
        var currentPlaylistIndex = 0;
        var getItemFields = "MediaSources,Chapters";

        self.currentMediaElement = null;
        self.currentItem = null;
        self.currentMediaSource = null;

        self.currentDurationTicks = null;
        self.startTimeTicksOffset = null;
        self.playlist = [];
        self.isLocalPlayer = true;
        self.isDefaultPlayer = true;

        self.name = 'Html5 Player';

        self.getTargets = function () {

            var targets = [{
                name: 'My Browser',
                id: ApiClient.deviceId(),
                playerName: self.name,
                playableMediaTypes: ['Audio', 'Video'],
                isLocalPlayer: true,
                supportedCommands: Dashboard.getSupportedRemoteCommands()
            }];

            return targets;
        };

        self.updateCanClientSeek = function (elem) {
            var duration = elem.duration;
            canClientSeek = duration && !isNaN(duration) && duration != Number.POSITIVE_INFINITY && duration != Number.NEGATIVE_INFINITY;
        };

        self.getCurrentTicks = function (mediaElement) {

            var playerTime = Math.floor(10000000 * (mediaElement || self.currentMediaElement).currentTime);

            //if (!self.isCopyingTimestamps) {
            playerTime += self.startTimeTicksOffset;
            //}

            return playerTime;
        };

        self.clearPauseStop = function () {

            if (self.pauseStop) {
                console.log('clearing pause stop timer');
                window.clearTimeout(self.pauseStop);
                self.pauseStop = null;
            }
        };

        self.playNextAfterEnded = function () {

            $(this).off('ended.playnext');

            self.nextTrack();
        };

        self.startProgressInterval = function () {

            clearProgressInterval();

            var intervalTime = ApiClient.isWebSocketOpen() ? 1200 : 20000;

            currentProgressInterval = setInterval(function () {

                if (self.currentMediaElement) {
                    sendProgressUpdate();
                }

            }, intervalTime);
        };

        self.getTranscodingExtension = function () {

            var media = testableVideoElement;

            // safari
            if (media.canPlayType('application/x-mpegURL').replace(/no/, '') ||
                media.canPlayType('application/vnd.apple.mpegURL').replace(/no/, '')) {
                return '.m3u8';
            }

            // Chrome or IE with plugin installed
            if (canPlayWebm()) {
                return '.webm';
            }

            return '.mp4';
        };

        self.changeStream = function (ticks, params) {

            var element = self.currentMediaElement;

            if (canClientSeek && params == null) {

                element.currentTime = ticks / (1000 * 10000);

            } else {

                params = params || {};

                var currentSrc = element.currentSrc;

                var transcodingExtension = self.getTranscodingExtension();
                var isStatic;

                if (self.currentItem.MediaType == "Video") {

                    if (params.AudioStreamIndex != null) {
                        currentSrc = replaceQueryString(currentSrc, 'AudioStreamIndex', params.AudioStreamIndex);
                    }
                    if (params.SubtitleStreamIndex != null) {
                        currentSrc = replaceQueryString(currentSrc, 'SubtitleStreamIndex', (params.SubtitleStreamIndex == -1 ? '' : params.SubtitleStreamIndex));
                    }

                    var maxWidth = params.MaxWidth || getParameterByName('MaxWidth', currentSrc);
                    var audioStreamIndex = params.AudioStreamIndex == null ? getParameterByName('AudioStreamIndex', currentSrc) : params.AudioStreamIndex;
                    var subtitleStreamIndex = self.currentSubtitleStreamIndex;
                    var videoBitrate = parseInt(getParameterByName('VideoBitrate', currentSrc) || '0');
                    var audioBitrate = parseInt(getParameterByName('AudioBitrate', currentSrc) || '0');
                    var bitrate = params.Bitrate || (videoBitrate + audioBitrate);

                    var finalParams = self.getFinalVideoParams(self.currentMediaSource, maxWidth, bitrate, audioStreamIndex, subtitleStreamIndex, transcodingExtension);

                    currentSrc = replaceQueryString(currentSrc, 'MaxWidth', finalParams.maxWidth);
                    currentSrc = replaceQueryString(currentSrc, 'VideoBitrate', finalParams.videoBitrate);

                    currentSrc = replaceQueryString(currentSrc, 'VideoCodec', finalParams.videoCodec);

                    currentSrc = replaceQueryString(currentSrc, 'profile', finalParams.profile || '');
                    currentSrc = replaceQueryString(currentSrc, 'level', finalParams.level || '');

                    if (finalParams.isStatic) {
                        currentSrc = currentSrc.replace('.webm', '.mp4').replace('.m3u8', '.mp4');
                    } else {
                        currentSrc = currentSrc.replace('.mp4', transcodingExtension).replace('.m4v', transcodingExtension);
                    }

                    currentSrc = replaceQueryString(currentSrc, 'AudioBitrate', finalParams.audioBitrate);
                    currentSrc = replaceQueryString(currentSrc, 'Static', finalParams.isStatic);
                    currentSrc = replaceQueryString(currentSrc, 'AudioCodec', finalParams.audioCodec);
                    isStatic = finalParams.isStatic;
                }

                var isSeekableMedia = self.currentMediaSource.RunTimeTicks;
                var isClientSeekable = isStatic || (isSeekableMedia && transcodingExtension == '.m3u8');

                if (isClientSeekable || !ticks || !isSeekableMedia) {
                    currentSrc = replaceQueryString(currentSrc, 'starttimeticks', '');
                }
                else {
                    currentSrc = replaceQueryString(currentSrc, 'starttimeticks', ticks);
                }

                clearProgressInterval();

                $(element).off('ended.playbackstopped').off('ended.playnext').one("play", function () {

                    self.updateCanClientSeek(this);

                    $(this).on('ended.playbackstopped', self.onPlaybackStopped).on('ended.playnext', self.playNextAfterEnded);

                    self.startProgressInterval();
                    sendProgressUpdate();

                });

                if (self.currentItem.MediaType == "Video") {
                    ApiClient.stopActiveEncodings().done(function () {

                        self.startTimeTicksOffset = ticks;
                        element.src = currentSrc;

                    });

                    self.updateTextStreamUrls(ticks || 0);
                } else {
                    self.startTimeTicksOffset = ticks;
                    element.src = currentSrc;
                    element.play();
                }
            }
        };

        self.setCurrentTime = function (ticks, positionSlider, currentTimeElement) {

            // Convert to ticks
            ticks = Math.floor(ticks);

            var timeText = Dashboard.getDisplayTime(ticks);

            if (self.currentDurationTicks) {

                timeText += " / " + Dashboard.getDisplayTime(self.currentDurationTicks);

                if (positionSlider) {

                    var percent = ticks / self.currentDurationTicks;
                    percent *= 100;

                    positionSlider.val(percent).slider('enable').slider('refresh');
                }
            } else {

                if (positionSlider) {

                    positionSlider.slider('disable').slider('refresh');
                }
            }

            if (currentTimeElement) {
                currentTimeElement.html(timeText);
            }

            var state = self.getPlayerStateInternal(self.currentMediaElement, self.currentItem, self.currentMediaSource);

            $(self).trigger('positionchange', [state]);
        };

        var supportsTextTracks;

        self.supportsTextTracks = function () {

            if (supportsTextTracks == null) {
                supportsTextTracks = document.createElement('video').textTracks != null;
            }

            // For now, until ready
            return supportsTextTracks;
        };

        self.canPlayVideoDirect = function (mediaSource, videoStream, audioStream, subtitleStream, maxWidth, bitrate) {

            if (!mediaSource) {
                throw new Error('Null mediaSource');
            }

            if (!videoStream) {
                return false;
            }

            if (mediaSource.VideoType != "VideoFile") {
                console.log('Transcoding because the content is not a video file');
                return false;
            }

            if ((videoStream.Codec || '').toLowerCase().indexOf('h264') == -1) {
                console.log('Transcoding because the content is not h264');
                return false;
            }

            if (audioStream && !canPlayAudioStreamDirect(audioStream)) {
                console.log('Transcoding because the audio cannot be played directly.');
                return false;
            }

            if (subtitleStream && (subtitleStream.IsGraphicalSubtitleStream || !self.supportsTextTracks())) {
                console.log('Transcoding because subtitles are required');
                return false;
            }

            if (!videoStream.Width || videoStream.Width > maxWidth) {
                console.log('Transcoding because resolution is too high');
                return false;
            }

            if (videoStream && videoStream.IsAnamorphic) {
                console.log('Transcoding because video is anamorphic');
                return false;
            }

            if (!mediaSource.Bitrate || mediaSource.Bitrate > bitrate) {
                console.log('Transcoding because bitrate is too high');
                return false;
            }

            var extension = (mediaSource.Container || '').toLowerCase();

            // m4v's and mp4's with high profile failing in chrome
            if (videoStream && videoStream.Profile == 'High') {
                //return false;
            }

            if (extension == 'm4v') {
                return $.browser.chrome;
            }

            return extension == 'mp4';
        };

        self.getFinalVideoParams = function (mediaSource, maxWidth, bitrate, audioStreamIndex, subtitleStreamIndex, transcodingExtension) {

            var mediaStreams = mediaSource.MediaStreams;

            var videoStream = mediaStreams.filter(function (stream) {
                return stream.Type === "Video";
            })[0];

            var audioStream = mediaStreams.filter(function (stream) {
                return stream.Index === audioStreamIndex;
            })[0];

            var subtitleStream = mediaStreams.filter(function (stream) {
                return stream.Index === subtitleStreamIndex;
            })[0];

            var canPlayDirect = self.canPlayVideoDirect(mediaSource, videoStream, audioStream, subtitleStream, maxWidth, bitrate);

            var audioBitrate = bitrate >= 700000 ? 128000 : 64000;

            var videoBitrate = bitrate - audioBitrate;

            var params = {
                isStatic: canPlayDirect,
                maxWidth: maxWidth,
                audioCodec: transcodingExtension == '.webm' ? 'vorbis' : 'aac',
                videoCodec: transcodingExtension == '.webm' ? 'vpx' : 'h264',
                audioBitrate: audioBitrate,
                videoBitrate: videoBitrate
            };

            if (params.videoCodec == 'h264') {
                params.profile = 'baseline';
                params.level = '3';
            }

            return params;
        };

        self.canQueueMediaType = function (mediaType) {

            return self.currentItem && self.currentItem.MediaType == mediaType;
        };

        self.play = function (options) {

            Dashboard.getCurrentUser().done(function (user) {

                if (options.items) {

                    translateItemsForPlayback(options.items).done(function (items) {

                        self.playInternal(items[0], options.startPositionTicks, user);

                        self.playlist = items;
                        currentPlaylistIndex = 0;
                    });

                } else {

                    self.getItemsForPlayback({

                        Ids: options.ids.join(',')

                    }).done(function (result) {

                        translateItemsForPlayback(result.Items).done(function (items) {

                            self.playInternal(items[0], options.startPositionTicks, user);

                            self.playlist = items;
                            currentPlaylistIndex = 0;
                        });

                    });
                }

            });
        };

        self.getBitrateSetting = function () {
            return parseInt(localStorage.getItem('preferredVideoBitrate') || '') || 1500000;
        };

        self.playInternal = function (item, startPosition, user) {

            if (item == null) {
                throw new Error("item cannot be null");
            }

            if (self.isPlaying()) {
                self.stop();
            }

            var mediaElement;

            if (item.MediaType === "Video") {

                self.currentItem = item;
                self.currentMediaSource = getOptimalMediaSource(item.MediaType, item.MediaSources);

                mediaElement = self.playVideo(item, self.currentMediaSource, startPosition);
                self.currentDurationTicks = self.currentMediaSource.RunTimeTicks;

            } else if (item.MediaType === "Audio") {

                self.currentItem = item;
                self.currentMediaSource = getOptimalMediaSource(item.MediaType, item.MediaSources);

                mediaElement = playAudio(item, self.currentMediaSource, startPosition);

                self.currentDurationTicks = self.currentMediaSource.RunTimeTicks;

            } else {
                throw new Error("Unrecognized media type");
            }

            self.currentMediaElement = mediaElement;

            if (item.MediaType === "Video") {

                self.updateNowPlayingInfo(item);
            }
        };

        self.getNowPlayingNameHtml = function (playerState) {

            var nowPlayingItem = playerState.NowPlayingItem;
            var topText = nowPlayingItem.Name;

            if (nowPlayingItem.MediaType == 'Video') {
                if (nowPlayingItem.IndexNumber != null) {
                    topText = nowPlayingItem.IndexNumber + " - " + topText;
                }
                if (nowPlayingItem.ParentIndexNumber != null) {
                    topText = nowPlayingItem.ParentIndexNumber + "." + topText;
                }
            }

            var bottomText = '';

            if (nowPlayingItem.Artists && nowPlayingItem.Artists.length) {
                bottomText = topText;
                topText = nowPlayingItem.Artists[0];
            }
            else if (nowPlayingItem.SeriesName || nowPlayingItem.Album) {
                bottomText = topText;
                topText = nowPlayingItem.SeriesName || nowPlayingItem.Album;
            }
            else if (nowPlayingItem.ProductionYear) {
                bottomText = nowPlayingItem.ProductionYear;
            }

            return bottomText ? topText + '<br/>' + bottomText : topText;
        };

        self.displayContent = function (options) {

            // Handle it the same as a remote control command
            Dashboard.onBrowseCommand({

                ItemName: options.itemName,
                ItemType: options.itemType,
                ItemId: options.itemId,
                Context: options.context

            });
        };

        self.getItemsForPlayback = function (query) {

            var userId = Dashboard.getCurrentUserId();

            query.Limit = query.Limit || 100;
            query.Fields = getItemFields;
            query.ExcludeLocationTypes = "Virtual";

            return ApiClient.getItems(userId, query);
        };

        self.removeFromPlaylist = function (index) {

            self.playlist.remove(index);

        };

        // Gets or sets the current playlist index
        self.currentPlaylistIndex = function (i) {

            if (i == null) {
                return currentPlaylistIndex;
            }

            var newItem = self.playlist[i];

            Dashboard.getCurrentUser().done(function (user) {

                self.playInternal(newItem, 0, user);
                currentPlaylistIndex = i;
            });
        };

        self.nextTrack = function () {

            var newIndex = currentPlaylistIndex + 1;
            var newItem = self.playlist[newIndex];

            if (newItem) {
                Dashboard.getCurrentUser().done(function (user) {

                    self.playInternal(newItem, 0, user);
                    currentPlaylistIndex = newIndex;
                });
            }
        };

        self.previousTrack = function () {
            var newIndex = currentPlaylistIndex - 1;
            if (newIndex >= 0) {
                var newItem = self.playlist[newIndex];

                if (newItem) {
                    Dashboard.getCurrentUser().done(function (user) {

                        self.playInternal(newItem, 0, user);
                        currentPlaylistIndex = newIndex;
                    });
                }
            }
        };

        self.queueItemsNext = function (items) {

            var insertIndex = 1;

            for (var i = 0, length = items.length; i < length; i++) {

                self.playlist.splice(insertIndex, 0, items[i]);

                insertIndex++;
            }
        };

        self.queueItems = function (items) {

            for (var i = 0, length = items.length; i < length; i++) {

                self.playlist.push(items[i]);
            }
        };

        self.queue = function (options) {

            if (!self.currentMediaElement) {
                self.play(options);
                return;
            }

            Dashboard.getCurrentUser().done(function (user) {

                if (options.items) {

                    translateItemsForPlayback(options.items).done(function (items) {

                        self.queueItems(items);
                    });

                } else {

                    self.getItemsForPlayback({

                        Ids: options.ids.join(',')

                    }).done(function (result) {

                        translateItemsForPlayback(result.Items).done(function (items) {

                            self.queueItems(items);
                        });

                    });
                }
            });
        };

        self.queueNext = function (options) {

            if (!self.currentMediaElement) {
                self.play(options);
                return;
            }

            Dashboard.getCurrentUser().done(function (user) {

                if (options.items) {

                    self.queueItemsNext(options.items);

                } else {

                    self.getItemsForPlayback({

                        Ids: options.ids.join(',')

                    }).done(function (result) {

                        options.items = result.Items;

                        self.queueItemsNext(options.items);

                    });
                }

            });
        };

        self.pause = function () {

            self.currentMediaElement.pause();
        };

        self.unpause = function () {
            self.currentMediaElement.play();
        };

        self.seek = function (position) {

            self.changeStream(position);
        };

        self.mute = function () {

            self.setVolume(0);
        };

        self.unMute = function () {

            self.setVolume(self.getSavedVolume() * 100);
        };

        self.toggleMute = function () {

            if (self.currentMediaElement) {

                console.log('MediaPlayer toggling mute');

                if (self.currentMediaElement.volume) {
                    self.mute();
                } else {
                    self.unMute();
                }
            }
        };

        self.volumeDown = function () {

            if (self.currentMediaElement) {
                self.setVolume(Math.max(self.currentMediaElement.volume - .02, 0) * 100);
            }
        };

        self.volumeUp = function () {

            if (self.currentMediaElement) {
                self.setVolume(Math.min(self.currentMediaElement.volume + .02, 1) * 100);
            }
        };

        // Sets volume using a 0-100 scale
        self.setVolume = function (val) {

            if (self.currentMediaElement) {

                console.log('MediaPlayer setting volume to ' + val);
                self.currentMediaElement.volume = val / 100;

                self.onVolumeChanged(self.currentMediaElement);

                self.saveVolume();
            }
        };

        self.saveVolume = function (val) {

            if (val) {
                localStorage.setItem("volume", val);
            }

        };

        self.getSavedVolume = function () {
            return localStorage.getItem("volume") || 0.5;
        };

        self.shuffle = function (id) {

            var userId = Dashboard.getCurrentUserId();

            ApiClient.getItem(userId, id).done(function (item) {

                var query = {
                    UserId: userId,
                    Fields: getItemFields,
                    Limit: 50,
                    Filters: "IsNotFolder",
                    Recursive: true,
                    SortBy: "Random"
                };

                if (item.IsFolder) {
                    query.ParentId = id;

                }
                else if (item.Type == "MusicArtist") {

                    query.MediaTypes = "Audio";
                    query.Artists = item.Name;

                }
                else if (item.Type == "MusicGenre") {

                    query.MediaTypes = "Audio";
                    query.Genres = item.Name;

                } else {
                    return;
                }

                self.getItemsForPlayback(query).done(function (result) {

                    self.play({ items: result.Items });

                });

            });

        };

        self.instantMix = function (id) {

            var userId = Dashboard.getCurrentUserId();

            ApiClient.getItem(userId, id).done(function (item) {

                var promise;

                if (item.Type == "MusicArtist") {

                    promise = ApiClient.getInstantMixFromArtist(name, {
                        UserId: Dashboard.getCurrentUserId(),
                        Fields: getItemFields,
                        Limit: 50
                    });

                }
                else if (item.Type == "MusicGenre") {

                    promise = ApiClient.getInstantMixFromMusicGenre(name, {
                        UserId: Dashboard.getCurrentUserId(),
                        Fields: getItemFields,
                        Limit: 50
                    });

                }
                else if (item.Type == "MusicAlbum") {

                    promise = ApiClient.getInstantMixFromAlbum(id, {
                        UserId: Dashboard.getCurrentUserId(),
                        Fields: getItemFields,
                        Limit: 50
                    });

                }
                else if (item.Type == "Audio") {

                    promise = ApiClient.getInstantMixFromSong(id, {
                        UserId: Dashboard.getCurrentUserId(),
                        Fields: getItemFields,
                        Limit: 50
                    });

                }
                else {
                    return;
                }

                promise.done(function (result) {

                    self.play({ items: result.Items });

                });

            });

        };

        self.stop = function () {

            var elem = self.currentMediaElement;

            if (elem) {

                elem.pause();

                $(elem).off("ended.playnext").one("ended", function () {

                    $(this).off();

                    if (this.tagName.toLowerCase() != 'audio') {
                        $(this).remove();
                    }

                    elem.src = "";
                    self.currentMediaElement = null;
                    self.currentItem = null;
                    self.currentMediaSource = null;

                }).trigger("ended");

            } else {
                self.currentMediaElement = null;
                self.currentItem = null;
                self.currentMediaSource = null;
            }

            if (self.currentItem && self.currentItem.MediaType == "Video") {
                if (self.isFullScreen()) {
                    self.exitFullScreen();
                }
                self.resetEnhancements();
            }
        };

        self.isPlaying = function () {
            return self.currentMediaElement != null;
        };

        self.getPlayerState = function () {

            var deferred = $.Deferred();

            var result = self.getPlayerStateInternal(self.currentMediaElement, self.currentItem, self.currentMediaSource);

            deferred.resolveWith(null, [result]);

            return deferred.promise();
        };

        self.getPlayerStateInternal = function (playerElement, item, mediaSource) {

            var state = {
                PlayState: {}
            };

            if (playerElement) {

                state.PlayState.VolumeLevel = playerElement.volume * 100;
                state.PlayState.IsMuted = playerElement.volume == 0;
                state.PlayState.IsPaused = playerElement.paused;
                state.PlayState.PositionTicks = self.getCurrentTicks(playerElement);

                var currentSrc = playerElement.currentSrc;

                if (currentSrc) {

                    var audioStreamIndex = getParameterByName('AudioStreamIndex', currentSrc);

                    if (audioStreamIndex) {
                        state.PlayState.AudioStreamIndex = parseInt(audioStreamIndex);
                    }
                    state.PlayState.SubtitleStreamIndex = self.currentSubtitleStreamIndex;

                    state.PlayState.PlayMethod = getParameterByName('static', currentSrc) == 'true' ?
                        'DirectStream' :
                        'Transcode';
                }
            }

            if (mediaSource) {

                state.PlayState.MediaSourceId = mediaSource.Id;

                state.NowPlayingItem = {
                    RunTimeTicks: mediaSource.RunTimeTicks
                };

                state.PlayState.CanSeek = mediaSource.RunTimeTicks && mediaSource.RunTimeTicks > 0;
            }

            if (item) {

                state.NowPlayingItem = state.NowPlayingItem || {};
                var nowPlayingItem = state.NowPlayingItem;

                nowPlayingItem.Id = item.Id;
                nowPlayingItem.MediaType = item.MediaType;
                nowPlayingItem.Type = item.Type;
                nowPlayingItem.Name = item.Name;

                nowPlayingItem.IndexNumber = item.IndexNumber;
                nowPlayingItem.IndexNumberEnd = item.IndexNumberEnd;
                nowPlayingItem.ParentIndexNumber = item.ParentIndexNumber;
                nowPlayingItem.ProductionYear = item.ProductionYear;
                nowPlayingItem.PremiereDate = item.PremiereDate;
                nowPlayingItem.SeriesName = item.SeriesName;
                nowPlayingItem.Album = item.Album;
                nowPlayingItem.Artists = item.Artists;

                var imageTags = item.ImageTags || {};

                if (item.SeriesPrimaryImageTag) {

                    nowPlayingItem.PrimaryImageItemId = item.SeriesId;
                    nowPlayingItem.PrimaryImageTag = item.SeriesPrimaryImageTag;
                }
                else if (imageTags.Primary) {

                    nowPlayingItem.PrimaryImageItemId = item.Id;
                    nowPlayingItem.PrimaryImageTag = imageTags.Primary;
                }
                else if (item.AlbumPrimaryImageTag) {

                    nowPlayingItem.PrimaryImageItemId = item.AlbumId;
                    nowPlayingItem.PrimaryImageTag = item.AlbumPrimaryImageTag;
                }
                else if (item.SeriesPrimaryImageTag) {

                    nowPlayingItem.PrimaryImageItemId = item.SeriesId;
                    nowPlayingItem.PrimaryImageTag = item.SeriesPrimaryImageTag;
                }

                if (item.BackdropImageTags && item.BackdropImageTags.length) {

                    nowPlayingItem.BackdropItemId = item.Id;
                    nowPlayingItem.BackdropImageTag = item.BackdropImageTags[0];
                }

                if (imageTags.Thumb) {

                    nowPlayingItem.ThumbItemId = item.Id;
                    nowPlayingItem.ThumbImageTag = imageTags.Thumb;
                }

                if (imageTags.Logo) {

                    nowPlayingItem.LogoItemId = item.Id;
                    nowPlayingItem.LogoImageTag = imageTags.Logo;
                }
                else if (item.ParentLogoImageTag) {

                    nowPlayingItem.LogoItemId = item.ParentLogoItemId;
                    nowPlayingItem.LogoImageTag = item.ParentLogoImageTag;
                }
            }

            return state;
        };

        self.beginPlayerUpdates = function () {
            // Nothing to setup here
        };

        self.endPlayerUpdates = function () {
            // Nothing to setup here
        };

        self.onPlaybackStart = function (playerElement, item, mediaSource) {

            self.updateCanClientSeek(playerElement);

            var state = self.getPlayerStateInternal(playerElement, item, mediaSource);

            $(self).trigger('playbackstart', [state]);

            self.startProgressInterval();
        };

        self.onVolumeChanged = function (playerElement) {

            self.saveVolume(playerElement.volume);

            var state = self.getPlayerStateInternal(playerElement, self.currentItem, self.currentMediaSource);

            $(self).trigger('volumechange', [state]);
        };

        self.onPlaybackStopped = function () {

            $('body').removeClass('bodyWithPopupOpen');

            self.clearPauseStop();

            var playerElement = this;

            $(playerElement).off('.mediaplayerevent').off('ended.playbackstopped');

            clearProgressInterval();

            var item = self.currentItem;
            var mediaSource = self.currentMediaSource;

            if (item.MediaType == "Video") {
                ApiClient.stopActiveEncodings();
                if (self.isFullScreen()) {
                    self.exitFullScreen();
                }
                self.resetEnhancements();
            }

            var state = self.getPlayerStateInternal(playerElement, item, mediaSource);

            $(self).trigger('playbackstop', [state]);
        };

        self.onPlaystateChange = function (playerElement) {

            var state = self.getPlayerStateInternal(playerElement, self.currentItem, self.currentMediaSource);

            $(self).trigger('playstatechange', [state]);
        };

        self.getCurrentTargetInfo = function () {
            return self.getTargets()[0];
        };

        self.canAutoPlayAudio = function () {

            if ($.browser.android || ($.browser.webkit && !$.browser.chrome)) {
                return false;
            }

            return true;
        };

        $(window).on("beforeunload popstate", function () {

            // Try to report playback stopped before the browser closes
            if (self.currentItem && self.currentMediaElement && currentProgressInterval) {

                self.onPlaybackStopped.call(self.currentMediaElement);
            }
        });

        function replaceQueryString(url, param, value) {
            var re = new RegExp("([?|&])" + param + "=.*?(&|$)", "i");
            if (url.match(re))
                return url.replace(re, '$1' + param + "=" + value + '$2');
            else
                return url + '&' + param + "=" + value;
        }

        function translateItemsForPlayback(items) {

            var deferred = $.Deferred();

            var firstItem = items[0];
            var promise;

            if (firstItem.IsFolder) {

                promise = self.getItemsForPlayback({
                    ParentId: firstItem.Id,
                    Filters: "IsNotFolder",
                    Recursive: true,
                    SortBy: "SortName",
                    MediaTypes: "Audio,Video"
                });
            }
            else if (firstItem.Type == "MusicArtist") {

                promise = self.getItemsForPlayback({
                    Artists: firstItem.Name,
                    Filters: "IsNotFolder",
                    Recursive: true,
                    SortBy: "SortName",
                    MediaTypes: "Audio"
                });

            }
            else if (firstItem.Type == "MusicGenre") {

                promise = self.getItemsForPlayback({
                    Genres: firstItem.Name,
                    Filters: "IsNotFolder",
                    Recursive: true,
                    SortBy: "SortName",
                    MediaTypes: "Audio"
                });
            }

            if (promise) {
                promise.done(function (result) {

                    deferred.resolveWith(null, [result.Items]);
                });
            } else {
                deferred.resolveWith(null, [items]);
            }

            return deferred.promise();
        }

        function getOptimalMediaSource(mediaType, versions) {

            var optimalVersion;

            if (mediaType == 'Video') {

                var bitrateSetting = self.getBitrateSetting();

                var maxAllowedWidth = Math.max(screen.height, screen.width);

                optimalVersion = versions.filter(function (v) {

                    var videoStream = v.MediaStreams.filter(function (s) {
                        return s.Type == 'Video';
                    })[0];

                    var audioStream = v.MediaStreams.filter(function (s) {
                        return s.Type == 'Audio';
                    })[0];

                    return self.canPlayVideoDirect(v, videoStream, audioStream, null, maxAllowedWidth, bitrateSetting);

                })[0];
            }

            return optimalVersion || versions[0];
        }

        function sendProgressUpdate() {

            var state = self.getPlayerStateInternal(self.currentMediaElement, self.currentItem, self.currentMediaSource);

            var info = {
                QueueableMediaTypes: state.NowPlayingItem.MediaType,
                ItemId: state.NowPlayingItem.Id,
                NowPlayingItem: state.NowPlayingItem
            };

            info = $.extend(info, state.PlayState);

            ApiClient.reportPlaybackProgress(info);
        }

        function clearProgressInterval() {

            if (currentProgressInterval) {
                clearTimeout(currentProgressInterval);
                currentProgressInterval = null;
            }
        }

        function canPlayWebm() {

            return testableVideoElement.canPlayType('video/webm').replace(/no/, '');
        }
        
        function getAudioElement() {

            var elem = $('.mediaPlayerAudio');

            if (elem.length) {
                return elem;
            }

            var html = '';

            var requiresControls = !self.canAutoPlayAudio();

            if (requiresControls) {
                html += '<div class="mediaPlayerAudioContainer"><div class="mediaPlayerAudioContainerInner">';;
            } else {
                html += '<div class="mediaPlayerAudioContainer" style="display:none;"><div class="mediaPlayerAudioContainerInner">';;
            }

            html += '<audio class="mediaPlayerAudio" controls>';
            html += '</audio></div></div>';

            $(document.body).append(html);

            return $('.mediaPlayerAudio');
        }

        var supportsAac = document.createElement('audio').canPlayType('audio/aac').replace(/no/, '');
        function playAudio(item, mediaSource, startPositionTicks) {

            startPositionTicks = startPositionTicks || 0;

            var baseParams = {
                audioChannels: 2,
                audioBitrate: 128000,
                StartTimeTicks: startPositionTicks,
                mediaSourceId: mediaSource.Id,
                deviceId: ApiClient.deviceId()
            };

            var sourceContainer = (mediaSource.Container || '').toLowerCase();
            var isStatic = false;

            if (sourceContainer == 'mp3' ||
                (sourceContainer == 'aac' && supportsAac)) {

                for (var i = 0, length = mediaSource.MediaStreams.length; i < length; i++) {

                    var stream = mediaSource.MediaStreams[i];

                    if (stream.Type == "Audio") {

                        // Stream statically when possible
                        if (stream.BitRate <= 320000) {
                            isStatic = true;
                        }
                        break;
                    }
                }
            }

            var outputContainer = isStatic ? sourceContainer : 'mp3';
            var audioUrl = ApiClient.getUrl('Audio/' + item.Id + '/stream.' + outputContainer, $.extend({}, baseParams, {
                audioCodec: outputContainer
            }));

            if (isStatic) {
                var seekParam = startPositionTicks ? '#t=' + (startPositionTicks / 10000000) : '';
                audioUrl += "&static=true" + seekParam;
            }

            self.startTimeTicksOffset = isStatic ? 0 : startPositionTicks;

            var initialVolume = self.getSavedVolume();

            return getAudioElement().each(function () {

                this.src = audioUrl;
                this.volume = initialVolume;
                this.play();

            }).on("volumechange.mediaplayerevent", function () {

                self.onVolumeChanged(this);

            }).one("playing.mediaplayerevent", function () {

                $('.mediaPlayerAudioContainer').hide();

                self.onPlaybackStart(this, item, mediaSource);

            }).on("pause.mediaplayerevent", function () {

                self.onPlaystateChange(this);

            }).on("playing.mediaplayerevent", function () {

                self.onPlaystateChange(this);

            }).on("timeupdate.mediaplayerevent", function () {

                self.setCurrentTime(self.getCurrentTicks(this));

            }).on("ended.playbackstopped", self.onPlaybackStopped).on('ended.playnext', self.playNextAfterEnded)[0];
        };

        function canPlayAudioStreamDirect(audioStream) {

            var audioCodec = (audioStream.Codec || '').toLowerCase().replace('-', '');

            if (audioCodec.indexOf('aac') == -1 &&
                audioCodec.indexOf('mp3') == -1 &&
                audioCodec.indexOf('mpeg') == -1) {

                return false;
            }

            if (audioStream.Channels == null) {
                return false;
            }

            // IE won't play at all if more than two channels
            if (audioStream.Channels > 2 && $.browser.msie) {
                return false;
            }

            return true;
        }
    }

    window.MediaPlayer = new mediaPlayer();

    window.MediaController.registerPlayer(window.MediaPlayer);
    window.MediaController.setActivePlayer(window.MediaPlayer);

})(document, setTimeout, clearTimeout, screen, localStorage, $, setInterval, window);