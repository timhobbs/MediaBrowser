﻿using MediaBrowser.Model.Drawing;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Extensions;
using MediaBrowser.Model.MediaInfo;
using System;
using System.Collections.Generic;

namespace MediaBrowser.Model.Dlna
{
    /// <summary>
    /// Class StreamInfo.
    /// </summary>
    public class StreamInfo
    {
        public string ItemId { get; set; }

        public bool IsDirectStream { get; set; }

        public DlnaProfileType MediaType { get; set; }

        public string Container { get; set; }

        public string Protocol { get; set; }

        public long StartPositionTicks { get; set; }

        public string VideoCodec { get; set; }
        public string VideoProfile { get; set; }

        public string AudioCodec { get; set; }

        public int? AudioStreamIndex { get; set; }

        public int? SubtitleStreamIndex { get; set; }

        public int? MaxAudioChannels { get; set; }

        public int? AudioBitrate { get; set; }

        public int? VideoBitrate { get; set; }

        public int? VideoLevel { get; set; }

        public int? MaxWidth { get; set; }
        public int? MaxHeight { get; set; }

        public float? MaxFramerate { get; set; }

        public string DeviceProfileId { get; set; }
        public string DeviceId { get; set; }

        public long? RunTimeTicks { get; set; }

        public TranscodeSeekInfo TranscodeSeekInfo { get; set; }

        public bool EstimateContentLength { get; set; }

        public MediaSourceInfo MediaSource { get; set; }

        public string MediaSourceId
        {
            get
            {
                return MediaSource == null ? null : MediaSource.Id;
            }
        }

        public string ToUrl(string baseUrl)
        {
            return ToDlnaUrl(baseUrl);
        }

        public string ToDlnaUrl(string baseUrl)
        {
            if (string.IsNullOrEmpty(baseUrl))
            {
                throw new ArgumentNullException(baseUrl);
            }

            string dlnaCommand = BuildDlnaParam(this);

            string extension = string.IsNullOrEmpty(Container) ? string.Empty : "." + Container;

            baseUrl = baseUrl.TrimEnd('/');

            if (MediaType == DlnaProfileType.Audio)
            {
                return string.Format("{0}/audio/{1}/stream{2}?{3}", baseUrl, ItemId, extension, dlnaCommand);
            }

            if (StringHelper.EqualsIgnoreCase(Protocol, "hls"))
            {
                return string.Format("{0}/videos/{1}/stream.m3u8?{2}", baseUrl, ItemId, dlnaCommand);
            }

            return string.Format("{0}/videos/{1}/stream{2}?{3}", baseUrl, ItemId, extension, dlnaCommand);
        }

        private static string BuildDlnaParam(StreamInfo item)
        {
            List<string> list = new List<string>
            {
                item.DeviceProfileId ?? string.Empty,
                item.DeviceId ?? string.Empty,
                item.MediaSourceId ?? string.Empty,
                (item.IsDirectStream).ToString().ToLower(),
                item.VideoCodec ?? string.Empty,
                item.AudioCodec ?? string.Empty,
                item.AudioStreamIndex.HasValue ? StringHelper.ToStringCultureInvariant(item.AudioStreamIndex.Value) : string.Empty,
                item.SubtitleStreamIndex.HasValue ? StringHelper.ToStringCultureInvariant(item.SubtitleStreamIndex.Value) : string.Empty,
                item.VideoBitrate.HasValue ? StringHelper.ToStringCultureInvariant(item.VideoBitrate.Value) : string.Empty,
                item.AudioBitrate.HasValue ? StringHelper.ToStringCultureInvariant(item.AudioBitrate.Value) : string.Empty,
                item.MaxAudioChannels.HasValue ? StringHelper.ToStringCultureInvariant(item.MaxAudioChannels.Value) : string.Empty,
                item.MaxFramerate.HasValue ? StringHelper.ToStringCultureInvariant(item.MaxFramerate.Value) : string.Empty,
                item.MaxWidth.HasValue ? StringHelper.ToStringCultureInvariant(item.MaxWidth.Value) : string.Empty,
                item.MaxHeight.HasValue ? StringHelper.ToStringCultureInvariant(item.MaxHeight.Value) : string.Empty,
                StringHelper.ToStringCultureInvariant(item.StartPositionTicks),
                item.VideoLevel.HasValue ? StringHelper.ToStringCultureInvariant(item.VideoLevel.Value) : string.Empty
            };

            return string.Format("Params={0}", string.Join(";", list.ToArray()));
        }

        /// <summary>
        /// Returns the audio stream that will be used
        /// </summary>
        public MediaStream TargetAudioStream
        {
            get
            {
                if (MediaSource != null)
                {
                    if (AudioStreamIndex.HasValue)
                    {
                        foreach (MediaStream i in MediaSource.MediaStreams)
                        {
                            if (i.Index == AudioStreamIndex.Value && i.Type == MediaStreamType.Audio) 
                                return i;
                        }
                        return null;
                    }

                    return MediaSource.DefaultAudioStream;
                }

                return null;
            }
        }

        /// <summary>
        /// Returns the video stream that will be used
        /// </summary>
        public MediaStream TargetVideoStream
        {
            get
            {
                if (MediaSource != null)
                {
                    return MediaSource.VideoStream;
                }

                return null;
            }
        }

        /// <summary>
        /// Predicts the audio sample rate that will be in the output stream
        /// </summary>
        public int? TargetAudioSampleRate
        {
            get
            {
                MediaStream stream = TargetAudioStream;
                return stream == null ? null : stream.SampleRate;
            }
        }

        /// <summary>
        /// Predicts the audio sample rate that will be in the output stream
        /// </summary>
        public int? TargetVideoBitDepth
        {
            get
            {
                MediaStream stream = TargetVideoStream;
                return stream == null || !IsDirectStream ? null : stream.BitDepth;
            }
        }

        /// <summary>
        /// Predicts the audio sample rate that will be in the output stream
        /// </summary>
        public float? TargetFramerate
        {
            get
            {
                MediaStream stream = TargetVideoStream;
                return MaxFramerate.HasValue && !IsDirectStream
                    ? MaxFramerate
                    : stream == null ? null : stream.AverageFrameRate ?? stream.RealFrameRate;
            }
        }

        /// <summary>
        /// Predicts the audio sample rate that will be in the output stream
        /// </summary>
        public double? TargetVideoLevel
        {
            get
            {
                MediaStream stream = TargetVideoStream;
                return VideoLevel.HasValue && !IsDirectStream
                    ? VideoLevel
                    : stream == null ? null : stream.Level;
            }
        }

        /// <summary>
        /// Predicts the audio sample rate that will be in the output stream
        /// </summary>
        public int? TargetPacketLength
        {
            get
            {
                MediaStream stream = TargetVideoStream;
                return !IsDirectStream
                    ? null
                    : stream == null ? null : stream.PacketLength;
            }
        }

        /// <summary>
        /// Predicts the audio sample rate that will be in the output stream
        /// </summary>
        public string TargetVideoProfile
        {
            get
            {
                MediaStream stream = TargetVideoStream;
                return !string.IsNullOrEmpty(VideoProfile) && !IsDirectStream
                    ? VideoProfile
                    : stream == null ? null : stream.Profile;
            }
        }

        /// <summary>
        /// Predicts the audio bitrate that will be in the output stream
        /// </summary>
        public int? TargetAudioBitrate
        {
            get
            {
                MediaStream stream = TargetAudioStream;
                return AudioBitrate.HasValue && !IsDirectStream
                    ? AudioBitrate
                    : stream == null ? null : stream.BitRate;
            }
        }

        /// <summary>
        /// Predicts the audio channels that will be in the output stream
        /// </summary>
        public int? TargetAudioChannels
        {
            get
            {
                MediaStream stream = TargetAudioStream;
                int? streamChannels = stream == null ? null : stream.Channels;

                return MaxAudioChannels.HasValue && !IsDirectStream
                    ? (streamChannels.HasValue ? Math.Min(MaxAudioChannels.Value, streamChannels.Value) : MaxAudioChannels.Value)
                    : stream == null ? null : streamChannels;
            }
        }

        /// <summary>
        /// Predicts the audio codec that will be in the output stream
        /// </summary>
        public string TargetAudioCodec
        {
            get
            {
                MediaStream stream = TargetAudioStream;

                return IsDirectStream
                 ? (stream == null ? null : stream.Codec)
                 : AudioCodec;
            }
        }

        /// <summary>
        /// Predicts the audio channels that will be in the output stream
        /// </summary>
        public long? TargetSize
        {
            get
            {
                if (IsDirectStream)
                {
                    return MediaSource.Size;
                }

                if (RunTimeTicks.HasValue)
                {
                    int? totalBitrate = TargetTotalBitrate;

                    double totalSeconds = RunTimeTicks.Value;
                    // Convert to ms
                    totalSeconds /= 10000;
                    // Convert to seconds
                    totalSeconds /= 1000;

                    return totalBitrate.HasValue ?
                        Convert.ToInt64(totalBitrate.Value * totalSeconds) :
                        (long?)null;
                }

                return null;
            }
        }

        public int? TargetVideoBitrate
        {
            get
            {
                MediaStream stream = TargetVideoStream;

                return VideoBitrate.HasValue && !IsDirectStream
                    ? VideoBitrate
                    : stream == null ? null : stream.BitRate;
            }
        }

        public TransportStreamTimestamp TargetTimestamp
        {
            get
            {
                TransportStreamTimestamp defaultValue = StringHelper.EqualsIgnoreCase(Container, "m2ts")
                    ? TransportStreamTimestamp.Valid
                    : TransportStreamTimestamp.None;

                return !IsDirectStream
                    ? defaultValue
                    : MediaSource == null ? defaultValue : MediaSource.Timestamp ?? TransportStreamTimestamp.None;
            }
        }

        public int? TargetTotalBitrate
        {
            get
            {
                return (TargetAudioBitrate ?? 0) + (TargetVideoBitrate ?? 0);
            }
        }

        public bool? IsTargetAnamorphic
        {
            get
            {
                if (IsDirectStream)
                {
                    return TargetVideoStream == null ? null : TargetVideoStream.IsAnamorphic;
                }

                return false;
            }
        }

        public int? TargetWidth
        {
            get
            {
                MediaStream videoStream = TargetVideoStream;

                if (videoStream != null && videoStream.Width.HasValue && videoStream.Height.HasValue)
                {
                    ImageSize size = new ImageSize
                    {
                        Width = videoStream.Width.Value,
                        Height = videoStream.Height.Value
                    };

                    double? maxWidth = MaxWidth.HasValue ? (double)MaxWidth.Value : (double?)null;
                    double? maxHeight = MaxHeight.HasValue ? (double)MaxHeight.Value : (double?)null;

                    ImageSize newSize = DrawingUtils.Resize(size,
                        null,
                        null,
                        maxWidth,
                        maxHeight);

                    return Convert.ToInt32(newSize.Width);
                }

                return MaxWidth;
            }
        }

        public int? TargetHeight
        {
            get
            {
                MediaStream videoStream = TargetVideoStream;

                if (videoStream != null && videoStream.Width.HasValue && videoStream.Height.HasValue)
                {
                    ImageSize size = new ImageSize
                    {
                        Width = videoStream.Width.Value,
                        Height = videoStream.Height.Value
                    };

                    double? maxWidth = MaxWidth.HasValue ? (double)MaxWidth.Value : (double?)null;
                    double? maxHeight = MaxHeight.HasValue ? (double)MaxHeight.Value : (double?)null;

                    ImageSize newSize = DrawingUtils.Resize(size,
                        null,
                        null,
                        maxWidth,
                        maxHeight);

                    return Convert.ToInt32(newSize.Height);
                }

                return MaxHeight;
            }
        }
    }
}
