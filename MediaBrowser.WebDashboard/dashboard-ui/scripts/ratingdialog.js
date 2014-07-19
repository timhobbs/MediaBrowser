﻿(function (window, document, $) {

    window.RatingDialog = function (page) {

        var self = this;

        self.show = function (options) {

            options = options || {};

            options.header = options.header || "Rate and Review";

            var html = '<div data-role="popup" id="popupRatingDialog" class="popup" style="min-width:400px;">';

            html += '<div class="ui-bar-a" style="text-align: center; padding: 0 20px;">';
            html += '<h3>' + options.header + '</h3>';
            html += '</div>';

            html += '<div style="padding: 1em;">';
            html += '<form>';

            html += '<div style="margin:0;">';
            html += '<label for="txtRatingDialogRating" >Your Rating:</label>';
            html += '<input id="txtRatingDialogRating" name="rating" type="number" required="required" min=0 max=5 step=1 value=' + options.rating + ' />';
            html += '<label for="txtRatingDialogTitle" >Short Overall Rating Description:</label>';
            html += '<input id="txtRatingDialogTitle" name="title" type="text" maxlength=160 />';
            html += '<label for="txtRatingDialogRecommend" >I recommend this item</label>';
            html += '<input id="txtRatingDialogRecommend" name="recommend" type="checkbox" checked />';
            html += '<label for="txtRatingDialogReview" >Full Review</label>';
            html += '<textarea id="txtRatingDialogReview" name="review" rows=8 style="height:inherit" ></textarea>';
            html += '</div>';


            html += '<p>';
            html += '<button type="submit" data-theme="b" data-icon="check">OK</button>';
            html += '<button type="button" data-icon="delete" onclick="$(this).parents(\'.popup\').popup(\'close\');">Cancel</button>';
            html += '</p>';
            html += '<p id="errorMsg" style="display:none; color:red; font-weight:bold">';
            html += '</p>';
            html += '</form>';
            html += '</div>';
            html += '</div>';

            $(page).append(html);

            var popup = $('#popupRatingDialog').popup().trigger('create').on("popupafteropen", function () {

                $('#txtRatingDialogTitle', this).focus();

            }).popup("open").on("popupafterclose", function () {

                $('form', this).off("submit");

                $(this).off("popupafterclose").remove();

            });

            $('form', popup).on('submit', function () {

                if (options.callback) {
                    var review = {
                        id: options.id,
                        rating: $('#txtRatingDialogRating', this).val(),
                        title: $('#txtRatingDialogTitle', this).val(),
                        recommend: $('#txtRatingDialogRecommend', this).checked(),
                        review: $('#txtRatingDialogReview', this).val(),
                    };

                    if (review.rating < 3) {
                        if (!review.title) {
                            $('#errorMsg', this).html("Please give reason for low rating").show();
                            $('#txtRatingDialogTitle', this).focus();
                            return false;
                        }
                    }

                    if (!review.recommend) {
                        if (!review.title) {
                            $('#errorMsg', this).html("Please give reason for not recommending").show();
                            $('#txtRatingDialogTitle', this).focus();
                            return false;
                        }
                    }

                    options.callback(review);
                } else console.log("No callback function provided");

                return false;
            });

        };

        self.close = function () {
            $('#popupRatingDialog', page).popup("close");
        };
    };

    window.RatingHelpers = {

        ratePackage: function (link) {
            var id = link.getAttribute('data-id');
            var name = link.getAttribute('data-name');
            var rating = link.getAttribute('data-rating');

            var dialog = new RatingDialog($.mobile.activePage);
            dialog.show({
                header: "Rate and review " + name,
                id: id,
                rating: rating,
                callback: function (review) {
                    console.log(review);
                    dialog.close();

                    ApiClient.createPackageReview(review).done(function () {
                        Dashboard.alert({
                            message: "Thank you for your review",
                            title: "Thank You"
                        });
                    });
                }
            });
        },

        getStoreRatingHtml: function (rating, id, name, noLinks) {

            var html = "<div style='margin-left: 5px; margin-right: 5px; display: inline-block; vertical-align:middle;'>";
            if (!rating) rating = 0;

            for (var i = 1; i <= 5; i++) {
                var title = noLinks ? rating + " stars" : "Rate " + i + (i > 1 ? " stars" : " star");

                html += noLinks ? "" : "<span data-id=" + id + " data-name='" + name + "' data-rating=" + i + " onclick='RatingHelpers.ratePackage(this);return false;' >";
                if (rating <= i - 1) {
                    html += "<div class='storeStarRating emptyStarRating' title='" + title + "'></div>";
                } else if (rating < i) {
                    html += "<div class='storeStarRating halfStarRating' title='" + title + "'></div>";
                } else {
                    html += "<div class='storeStarRating' title='" + title + "'></div>";
                }
                html += noLinks ? "" : "</span>";
            }

            html += "</div>";

            return html;
        }
    };

})(window, document, jQuery);