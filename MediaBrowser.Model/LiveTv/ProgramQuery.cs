﻿using System;

namespace MediaBrowser.Model.LiveTv
{
    /// <summary>
    /// Class ProgramQuery.
    /// </summary>
    public class ProgramQuery
    {
        /// <summary>
        /// Gets or sets the channel identifier.
        /// </summary>
        /// <value>The channel identifier.</value>
        public string[] ChannelIdList { get; set; }

        /// <summary>
        /// Gets or sets the user identifier.
        /// </summary>
        /// <value>The user identifier.</value>
        public string UserId { get; set; }

        public DateTime? MinStartDate { get; set; }

        public DateTime? MaxStartDate { get; set; }

        public DateTime? MinEndDate { get; set; }

        public DateTime? MaxEndDate { get; set; }
        
        public ProgramQuery()
        {
            ChannelIdList = new string[] { };
        }
    }
}
