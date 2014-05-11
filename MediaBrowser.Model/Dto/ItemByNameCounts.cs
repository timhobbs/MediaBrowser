﻿using System;

namespace MediaBrowser.Model.Dto
{
    /// <summary>
    /// Class ItemByNameCounts
    /// </summary>
    public class ItemByNameCounts
    {
        public string UserId { get; set; }

        /// <summary>
        /// Gets or sets the total count.
        /// </summary>
        /// <value>The total count.</value>
        public int TotalCount { get; set; }
        /// <summary>
        /// Gets or sets the adult video count.
        /// </summary>
        /// <value>The adult video count.</value>
        public int AdultVideoCount { get; set; }
        /// <summary>
        /// Gets or sets the movie count.
        /// </summary>
        /// <value>The movie count.</value>
        public int MovieCount { get; set; }
        /// <summary>
        /// Gets or sets the series count.
        /// </summary>
        /// <value>The series count.</value>
        public int SeriesCount { get; set; }
        /// <summary>
        /// Gets or sets the episode count.
        /// </summary>
        /// <value>The episode count.</value>
        public int EpisodeCount { get; set; }
        /// <summary>
        /// Gets or sets the game count.
        /// </summary>
        /// <value>The game count.</value>
        public int GameCount { get; set; }
        /// <summary>
        /// Gets or sets the trailer count.
        /// </summary>
        /// <value>The trailer count.</value>
        public int TrailerCount { get; set; }
        /// <summary>
        /// Gets or sets the song count.
        /// </summary>
        /// <value>The song count.</value>
        public int SongCount { get; set; }
        /// <summary>
        /// Gets or sets the album count.
        /// </summary>
        /// <value>The album count.</value>
        public int AlbumCount { get; set; }
        /// <summary>
        /// Gets or sets the music video count.
        /// </summary>
        /// <value>The music video count.</value>
        public int MusicVideoCount { get; set; }
    }
}
