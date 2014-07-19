﻿(function ($, document, Dashboard, LibraryBrowser) {

    function notifications() {

        var self = this;

        self.getNotificationsSummaryPromise = null;

        self.total = 0;

        self.getNotificationsSummary = function () {

            self.getNotificationsSummaryPromise = self.getNotificationsSummaryPromise || ApiClient.getNotificationSummary(Dashboard.getCurrentUserId());

            return self.getNotificationsSummaryPromise;
        };

        self.updateNotificationCount = function () {

            if (!Dashboard.getCurrentUserId()) {
                return;
            }

            self.getNotificationsSummary().done(function (summary) {

                var item = $('.btnNotificationsInner').removeClass('levelNormal').removeClass('levelWarning').removeClass('levelError').html(summary.UnreadCount);

                if (summary.UnreadCount) {
                    item.addClass('level' + summary.MaxUnreadNotificationLevel);
                }
            });
        };

        self.showNotificationsFlyout = function () {

            var html = '<div data-role="panel" data-position="right" data-display="overlay" class="notificationsFlyout" data-position-fixed="true" data-theme="a">';

            html += '<h1 style="margin: .25em 0;">';
            html += '<span style="vertical-align:middle;">' + Globalize.translate('HeaderNotifications') + '</span>';
            html += '<a data-role="button" data-inline="true" data-icon="arrow-r" href="notificationlist.html" data-iconpos="notext" style="vertical-align:middle;margin-left:.5em;">' + Globalize.translate('ButtonViewNotifications') + '</a>';
            html += '</h1>';

            html += '<div>';

            html += '<div class="notificationsFlyoutlist">Loading...';

            html += '</div>';

            html += '<div style="display:none;" class="btnMarkReadContainer"><button class="btnMarkRead" type="button" data-icon="check" data-mini="true">' + Globalize.translate('ButtonMarkTheseRead') + '</button></div>';

            html += '</div>';

            html += '</div>';

            $(document.body).append(html);

            $('.notificationsFlyout').panel({}).panel('option', 'classes.modalOpen', 'notificationsPanelModelOpen ui-panel-dismiss-open').trigger('create').panel("open").on("panelclose", function () {

                $(this).off("panelclose").remove();

            }).on('click', '.btnMarkRead', function () {

                var ids = $('.unreadFlyoutNotification').map(function () {

                    return this.getAttribute('data-notificationid');

                }).get();

                self.markNotificationsRead(ids, function () {

                    $('.notificationsFlyout').panel("close");

                });

            });

            self.isFlyout = true;

            var startIndex = 0;
            var limit = 5;
            var elem = $('.notificationsFlyoutlist');
            var markReadButton = $('.btnMarkReadContainer');

            refreshNotifications(startIndex, limit, elem, markReadButton, false);
        };

        self.markNotificationsRead = function (ids, callback) {

            ApiClient.markNotificationsRead(Dashboard.getCurrentUserId(), ids, true).done(function () {

                self.getNotificationsSummaryPromise = null;

                self.updateNotificationCount();

                callback();

            });

        };

        self.showNotificationsList = function (startIndex, limit, elem, btn) {

            refreshNotifications(startIndex, limit, elem, btn, true);

        };
    }

    function refreshNotifications(startIndex, limit, elem, btn, showPaging) {

        ApiClient.getNotifications(Dashboard.getCurrentUserId(), { StartIndex: startIndex, Limit: limit }).done(function (result) {

            listUnreadNotifications(result.Notifications, result.TotalRecordCount, startIndex, limit, elem, btn, showPaging);

        });
    }

    function listUnreadNotifications(list, totalRecordCount, startIndex, limit, elem, btn, showPaging) {

        if (!totalRecordCount) {
            elem.html('<p style="padding:.5em 1em;">' + Globalize.translate('LabelNoUnreadNotifications') + '</p>');
            btn.hide();
            return;
        }

        Notifications.total = totalRecordCount;

        if (list.filter(function (n) {

            return !n.IsRead;

        }).length) {
            btn.show();
        } else {
            btn.hide();
        }

        var html = '';

        if (totalRecordCount > limit && showPaging === true) {

            var query = { StartIndex: startIndex, Limit: limit };

            html += LibraryBrowser.getPagingHtml(query, totalRecordCount, false, limit, false);
        }

        for (var i = 0, length = list.length; i < length; i++) {

            var notification = list[i];

            html += getNotificationHtml(notification);

        }

        elem.html(html).trigger('create');
    }

    function getNotificationHtml(notification) {

        var html = '';

        var cssClass = notification.IsRead ? "flyoutNotification" : "flyoutNotification unreadFlyoutNotification";

        html += '<div data-notificationid="' + notification.Id + '" class="' + cssClass + '">';

        html += '<div class="notificationImage">';
        html += getImageHtml(notification);
        html += '</div>';

        html += '<div class="notificationContent">';

        html += '<p style="margin: .4em 0 .25em;" class="notificationName">' + notification.Name + '</p>';

        html += '<p class="notificationTime" style="margin: .25em 0;">' + humane_date(notification.Date) + '</p>';

        if (notification.Description) {
            html += '<p style="margin: .25em 0;">' + notification.Description + '</p>';
        }

        if (notification.Url) {
            html += '<p style="margin: .25em 0;"><a href="' + notification.Url + '" target="_blank">' + Globalize.translate('ButtonMoreInformation') + '</a></p>';
        }

        html += '</div>';

        html += '</div>';

        return html;
    }

    function getImageHtml(notification) {

        if (notification.Level == "Error") {

            return '<div class="imgNotification imgNotificationError"><div class="imgNotificationInner imgNotificationIcon"></div></div>';

        }
        if (notification.Level == "Warning") {

            return '<div class="imgNotification imgNotificationWarning"><div class="imgNotificationInner imgNotificationIcon"></div></div>';

        }

        return '<div class="imgNotification imgNotificationNormal"><div class="imgNotificationInner imgNotificationIcon"></div></div>';

    }

    window.Notifications = new notifications();

    $(document).on('headercreated', function (e) {

        $('<a class="headerButton headerButtonRight btnNotifications" href="#" title="Notifications"><div class="btnNotificationsInner">0</div></a>').insertAfter($('.headerUserButton')).on('click', Notifications.showNotificationsFlyout);

        Notifications.updateNotificationCount();
    });

    $(ApiClient).on("websocketmessage", function (e, msg) {


        if (msg.MessageType === "NotificationUpdated" || msg.MessageType === "NotificationAdded" || msg.MessageType === "NotificationsMarkedRead") {

            Notifications.getNotificationsSummaryPromise = null;

            Notifications.updateNotificationCount();
        }

    });


})(jQuery, document, Dashboard, LibraryBrowser);