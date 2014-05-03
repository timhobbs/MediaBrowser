﻿using MediaBrowser.Model.Entities;
using MediaBrowser.Model.MediaInfo;
using System.Collections.Generic;

namespace MediaBrowser.Model.Dto
{
    public class MediaSourceInfo
    {
        public string Id { get; set; }

        public string Path { get; set; }

        public string Container { get; set; }
        public long? Size { get; set; }

        public LocationType LocationType { get; set; }
        
        public string Name { get; set; }
        
        public long? RunTimeTicks { get; set; }

        public VideoType? VideoType { get; set; }

        public IsoType? IsoType { get; set; }

        public Video3DFormat? Video3DFormat { get; set; }
        
        public List<MediaStream> MediaStreams { get; set; }

        public List<string> Formats { get; set; }
        
        public int? Bitrate { get; set; }

        public TransportStreamTimestamp? Timestamp { get; set; }
        
        public MediaSourceInfo()
        {
            Formats = new List<string>();
            MediaStreams = new List<MediaStream>();
        }
    }
}
