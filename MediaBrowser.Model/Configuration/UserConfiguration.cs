﻿
namespace MediaBrowser.Model.Configuration
{
    /// <summary>
    /// Class UserConfiguration
    /// </summary>
    public class UserConfiguration
    {
        /// <summary>
        /// Gets or sets the max parental rating.
        /// </summary>
        /// <value>The max parental rating.</value>
        public int? MaxParentalRating { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is administrator.
        /// </summary>
        /// <value><c>true</c> if this instance is administrator; otherwise, <c>false</c>.</value>
        public bool IsAdministrator { get; set; }

        /// <summary>
        /// Gets or sets the audio language preference.
        /// </summary>
        /// <value>The audio language preference.</value>
        public string AudioLanguagePreference { get; set; }

        /// <summary>
        /// Gets or sets the subtitle language preference.
        /// </summary>
        /// <value>The subtitle language preference.</value>
        public string SubtitleLanguagePreference { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [use forced subtitles only].
        /// </summary>
        /// <value><c>true</c> if [use forced subtitles only]; otherwise, <c>false</c>.</value>
        public bool UseForcedSubtitlesOnly { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is hidden.
        /// </summary>
        /// <value><c>true</c> if this instance is hidden; otherwise, <c>false</c>.</value>
        public bool IsHidden { get; set; }

        public bool IsDisabled { get; set; }

        public bool DisplayMissingEpisodes { get; set; }
        public bool DisplayUnairedEpisodes { get; set; }
        public bool EnableRemoteControlOfOtherUsers { get; set; }

        public bool EnableLiveTvManagement { get; set; }
        public bool EnableLiveTvAccess { get; set; }

        public bool EnableMediaPlayback { get; set; }
        public bool EnableContentDeletion { get; set; }

        public bool GroupMoviesIntoBoxSets { get; set; }

        public string[] BlockedMediaFolders { get; set; }
        public string[] BlockedChannels { get; set; }

        public UnratedItem[] BlockUnratedItems { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="UserConfiguration" /> class.
        /// </summary>
        public UserConfiguration()
        {
            IsAdministrator = true;

            EnableRemoteControlOfOtherUsers = true;
            EnableLiveTvManagement = true;
            EnableMediaPlayback = true;
            EnableLiveTvAccess = true;
            GroupMoviesIntoBoxSets = true;

            BlockedMediaFolders = new string[] { };
            BlockedChannels = new string[] { };
            BlockUnratedItems = new UnratedItem[] { };
        }
    }
}
