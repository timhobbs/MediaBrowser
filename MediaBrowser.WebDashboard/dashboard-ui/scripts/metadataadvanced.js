﻿var AdvancedMetadataConfigurationPage = {

    onPageShow: function () {

        Dashboard.showLoadingMsg();

        var page = this;

        ApiClient.getServerConfiguration().done(function (configuration) {

            AdvancedMetadataConfigurationPage.load(page, configuration);

        });
    },

    load: function (page, config) {

        $('#chkEnableTmdbPersonUpdates', page).checked(config.EnableTmdbUpdates).checkboxradio("refresh");
        $('#chkEnableTvdbUpdates', page).checked(config.EnableTvDbUpdates).checkboxradio("refresh");
        $('#chkEnableFanartUpdates', page).checked(config.EnableFanArtUpdates).checkboxradio("refresh");

        Dashboard.hideLoadingMsg();
    },

    onSubmit: function () {
        var form = this;

        Dashboard.showLoadingMsg();

        ApiClient.getServerConfiguration().done(function (config) {

            config.EnableTvDbUpdates = $('#chkEnableTvdbUpdates', form).checked();
            config.EnableTmdbUpdates = $('#chkEnableTmdbPersonUpdates', form).checked();
            config.EnableFanArtUpdates = $('#chkEnableFanartUpdates', form).checked();

            ApiClient.updateServerConfiguration(config).done(Dashboard.processServerConfigurationUpdateResult);
        });

        // Disable default form submission
        return false;
    }
};

$(document).on('pageshow', "#advancedMetadataConfigurationPage", AdvancedMetadataConfigurationPage.onPageShow);