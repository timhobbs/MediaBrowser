﻿(function ($, document) {

    function save(page) {

        Dashboard.showLoadingMsg();

        $.ajax({
            type: "POST",
            url: ApiClient.getUrl("System/Configuration/VideoImageExtraction", { Enabled: $('#chkVideoImages', page).checked() })

        }).done(function () {


            // After saving chapter task, now save server config
            ApiClient.getServerConfiguration().done(function (config) {

                config.ChapterOptions.EnableMovieChapterImageExtraction = $('#chkMovies', page).checked();

                config.EnableUPnP = $('#chkEnableUpnp', page).checked();

                ApiClient.updateServerConfiguration(config).done(function (result) {

                    navigateToNextPage();

                });
            });
        });
    }

    function navigateToNextPage() {

        ApiClient.getSystemInfo().done(function (systemInfo) {

            var os = systemInfo.OperatingSystem.toLowerCase();

            if (os.indexOf('windows') != -1) {
                Dashboard.navigate('wizardservice.html');
            } else {
                Dashboard.navigate('wizardfinish.html');
            }

        });
    }

    $(document).on('pageinit', "#wizardImageSettingsPage", function () {

        var page = this;

        $('#btnNextPage', page).on('click', function () {

            save(page);
        });
    });

})(jQuery, document, window);
