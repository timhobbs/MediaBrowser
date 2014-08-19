﻿(function ($, document, window) {
    
    function updateSeasonPatternHelp(page, value) {
        
        var resultValue = value.replace('%s', '1').replace('%0s', '01').replace('%00s', '001');

        var replacementHtmlResult = Globalize.translate('OrganizePatternResult').replace('{0}', resultValue);

        $('.seasonFolderFieldDescription', page).html(replacementHtmlResult);
    }
    
    function getEpisodeFileName(value, enableMultiEpisode) {
        
        var seriesName = "Series Name";
        var episodeTitle = "Episode Four";

        var result = value.replace('%sn', seriesName)
            .replace('%s.n', seriesName.replace(' ', '.'))
            .replace('%s_n', seriesName.replace(' ', '_'))
            .replace('%s', '1')
            .replace('%0s', '01')
            .replace('%00s', '001')
            .replace('%ext', 'mkv')
            .replace('%en', episodeTitle)
            .replace('%e.n', episodeTitle.replace(' ', '.'))
            .replace('%e_n', episodeTitle.replace(' ', '_'));
        
        if (enableMultiEpisode) {
            result = result
            .replace('%ed', '5')
            .replace('%0ed', '05')
            .replace('%00ed', '005');
        }

        return result
            .replace('%e', '4')
            .replace('%0e', '04')
            .replace('%00e', '004');
    }

    function updateEpisodePatternHelp(page, value) {

        value = getEpisodeFileName(value, false);
        
        var replacementHtmlResult = Globalize.translate('OrganizePatternResult').replace('{0}', value);

        $('.episodePatternDescription', page).html(replacementHtmlResult);
    }

    function updateMultiEpisodePatternHelp(page, value) {

        value = getEpisodeFileName(value, true);

        var replacementHtmlResult = Globalize.translate('OrganizePatternResult').replace('{0}', value);

        $('.multiEpisodePatternDescription', page).html(replacementHtmlResult);
    }

    function loadPage(page, config) {

        var tvOptions = config.TvOptions;

        $('#chkEnableTvSorting', page).checked(tvOptions.IsEnabled).checkboxradio('refresh');
        $('#chkOverwriteExistingEpisodes', page).checked(tvOptions.OverwriteExistingEpisodes).checkboxradio('refresh');
        $('#chkDeleteEmptyFolders', page).checked(tvOptions.DeleteEmptyFolders).checkboxradio('refresh');

        $('#txtMinFileSize', page).val(tvOptions.MinFileSizeMb);
        $('#txtSeasonFolderPattern', page).val(tvOptions.SeasonFolderPattern).trigger('change');
        $('#txtSeasonZeroName', page).val(tvOptions.SeasonZeroFolderName);
        $('#txtWatchFolder', page).val(tvOptions.WatchLocations[0] || '');

        $('#txtEpisodePattern', page).val(tvOptions.EpisodeNamePattern).trigger('change');
        $('#txtMultiEpisodePattern', page).val(tvOptions.MultiEpisodeNamePattern).trigger('change');

        $('#txtDeleteLeftOverFiles', page).val(tvOptions.LeftOverFileExtensionsToDelete.join(';'));

		$('#copyOrMoveFile', page).val(tvOptions.CopyOriginalFile.toString()).selectmenu('refresh');

    }
    
    $(document).on('pageinit', "#libraryFileOrganizerPage", function () {

        var page = this;

        $('#txtSeasonFolderPattern', page).on('change keyup', function () {

            updateSeasonPatternHelp(page, this.value);

        });

        $('#txtEpisodePattern', page).on('change keyup', function () {

            updateEpisodePatternHelp(page, this.value);

        });

        $('#txtMultiEpisodePattern', page).on('change keyup', function () {

            updateMultiEpisodePatternHelp(page, this.value);

        });

        $('#btnSelectWatchFolder', page).on("click.selectDirectory", function () {

            var picker = new DirectoryBrowser(page);

            picker.show({

                callback: function (path) {

                    if (path) {
                        $('#txtWatchFolder', page).val(path);
                    }
                    picker.close();
                },

                header: Globalize.translate('HeaderSelectWatchFolder'),

                instruction: Globalize.translate('HeaderSelectWatchFolderHelp')
            });
        });

    }).on('pageshow', "#libraryFileOrganizerPage", function () {

        var page = this;

        ApiClient.getNamedConfiguration('autoorganize').done(function (config) {
            loadPage(page, config);
        });
    });

    window.LibraryFileOrganizerPage = {
      
        onSubmit: function() {

            var form = this;

            ApiClient.getNamedConfiguration('autoorganize').done(function (config) {
                
                var tvOptions = config.TvOptions;

                tvOptions.IsEnabled = $('#chkEnableTvSorting', form).checked();
                tvOptions.OverwriteExistingEpisodes = $('#chkOverwriteExistingEpisodes', form).checked();
                tvOptions.DeleteEmptyFolders = $('#chkDeleteEmptyFolders', form).checked();

                tvOptions.MinFileSizeMb = $('#txtMinFileSize', form).val();
                tvOptions.SeasonFolderPattern = $('#txtSeasonFolderPattern', form).val();
                tvOptions.SeasonZeroFolderName = $('#txtSeasonZeroName', form).val();

                tvOptions.EpisodeNamePattern = $('#txtEpisodePattern', form).val();
                tvOptions.MultiEpisodeNamePattern = $('#txtMultiEpisodePattern', form).val();

                tvOptions.LeftOverFileExtensionsToDelete = $('#txtDeleteLeftOverFiles', form).val().split(';');

                var watchLocation = $('#txtWatchFolder', form).val();
                tvOptions.WatchLocations = watchLocation ? [watchLocation] : [];

				tvOptions.CopyOriginalFile = $('#copyOrMoveFile', form).val();

				ApiClient.updateNamedConfiguration('autoorganize', config).done(Dashboard.processServerConfigurationUpdateResult);
            });

            return false;
        }

    };

})(jQuery, document, window);
