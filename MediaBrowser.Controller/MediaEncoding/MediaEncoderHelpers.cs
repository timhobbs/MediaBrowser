﻿using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.MediaInfo;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace MediaBrowser.Controller.MediaEncoding
{
    /// <summary>
    /// Class MediaEncoderHelpers
    /// </summary>
    public static class MediaEncoderHelpers
    {
        /// <summary>
        /// Gets the input argument.
        /// </summary>
        /// <param name="videoPath">The video path.</param>
        /// <param name="protocol">The protocol.</param>
        /// <param name="isoMount">The iso mount.</param>
        /// <param name="playableStreamFileNames">The playable stream file names.</param>
        /// <returns>System.String[][].</returns>
        public static string[] GetInputArgument(string videoPath, MediaProtocol protocol, IIsoMount isoMount, List<string> playableStreamFileNames)
        {
            if (playableStreamFileNames.Count > 0)
            {
                if (isoMount == null)
                {
                    return GetPlayableStreamFiles(videoPath, playableStreamFileNames).ToArray();
                }
                return GetPlayableStreamFiles(isoMount.MountedPath, playableStreamFileNames).ToArray();
            }

            return new[] {videoPath};
        }

        public static List<string> GetPlayableStreamFiles(string rootPath, IEnumerable<string> filenames)
        {
            var allFiles = Directory
                .EnumerateFiles(rootPath, "*", SearchOption.AllDirectories)
                .ToList();

            return filenames.Select(name => allFiles.FirstOrDefault(f => string.Equals(Path.GetFileName(f), name, StringComparison.OrdinalIgnoreCase)))
                .Where(f => !string.IsNullOrEmpty(f))
                .ToList();
        }

        public static MediaInfo GetMediaInfo(InternalMediaInfoResult data)
        {
            var internalStreams = data.streams ?? new MediaStreamInfo[] { };

            var info = new MediaInfo
            {
                MediaStreams = internalStreams.Select(s => GetMediaStream(s, data.format))
                    .Where(i => i != null)
                    .ToList()
            };

            if (data.format != null)
            {
                info.Format = data.format.format_name;

                if (!string.IsNullOrEmpty(data.format.bit_rate))
                {
                    info.TotalBitrate = int.Parse(data.format.bit_rate, UsCulture);
                }
            }

            return info;
        }

        private static readonly CultureInfo UsCulture = new CultureInfo("en-US");

        /// <summary>
        /// Converts ffprobe stream info to our MediaStream class
        /// </summary>
        /// <param name="streamInfo">The stream info.</param>
        /// <param name="formatInfo">The format info.</param>
        /// <returns>MediaStream.</returns>
        private static MediaStream GetMediaStream(MediaStreamInfo streamInfo, MediaFormatInfo formatInfo)
        {
            var stream = new MediaStream
            {
                Codec = streamInfo.codec_name,
                Profile = streamInfo.profile,
                Level = streamInfo.level,
                Index = streamInfo.index,
                PixelFormat = streamInfo.pix_fmt
            };

            if (streamInfo.tags != null)
            {
                stream.Language = GetDictionaryValue(streamInfo.tags, "language");
            }

            if (string.Equals(streamInfo.codec_type, "audio", StringComparison.OrdinalIgnoreCase))
            {
                stream.Type = MediaStreamType.Audio;

                stream.Channels = streamInfo.channels;

                if (!string.IsNullOrEmpty(streamInfo.sample_rate))
                {
                    stream.SampleRate = int.Parse(streamInfo.sample_rate, UsCulture);
                }

                stream.ChannelLayout = ParseChannelLayout(streamInfo.channel_layout);
            }
            else if (string.Equals(streamInfo.codec_type, "subtitle", StringComparison.OrdinalIgnoreCase))
            {
                stream.Type = MediaStreamType.Subtitle;
            }
            else if (string.Equals(streamInfo.codec_type, "video", StringComparison.OrdinalIgnoreCase))
            {
                stream.Type = (streamInfo.codec_name ?? string.Empty).IndexOf("mjpeg", StringComparison.OrdinalIgnoreCase) != -1
                    ? MediaStreamType.EmbeddedImage
                    : MediaStreamType.Video;

                stream.Width = streamInfo.width;
                stream.Height = streamInfo.height;
                stream.AspectRatio = GetAspectRatio(streamInfo);

                stream.AverageFrameRate = GetFrameRate(streamInfo.avg_frame_rate);
                stream.RealFrameRate = GetFrameRate(streamInfo.r_frame_rate);

                stream.BitDepth = GetBitDepth(stream.PixelFormat);

                //stream.IsAnamorphic = string.Equals(streamInfo.sample_aspect_ratio, "0:1", StringComparison.OrdinalIgnoreCase) ||
                //    string.Equals(stream.AspectRatio, "2.35:1", StringComparison.OrdinalIgnoreCase) ||
                //    string.Equals(stream.AspectRatio, "2.40:1", StringComparison.OrdinalIgnoreCase);

                stream.IsAnamorphic = string.Equals(streamInfo.sample_aspect_ratio, "0:1", StringComparison.OrdinalIgnoreCase);
            }
            else
            {
                return null;
            }

            // Get stream bitrate
            var bitrate = 0;

            if (!string.IsNullOrEmpty(streamInfo.bit_rate))
            {
                bitrate = int.Parse(streamInfo.bit_rate, UsCulture);
            }
            else if (formatInfo != null && !string.IsNullOrEmpty(formatInfo.bit_rate) && stream.Type == MediaStreamType.Video)
            {
                // If the stream info doesn't have a bitrate get the value from the media format info
                bitrate = int.Parse(formatInfo.bit_rate, UsCulture);
            }

            if (bitrate > 0)
            {
                stream.BitRate = bitrate;
            }

            if (streamInfo.disposition != null)
            {
                var isDefault = GetDictionaryValue(streamInfo.disposition, "default");
                var isForced = GetDictionaryValue(streamInfo.disposition, "forced");

                stream.IsDefault = string.Equals(isDefault, "1", StringComparison.OrdinalIgnoreCase);

                stream.IsForced = string.Equals(isForced, "1", StringComparison.OrdinalIgnoreCase);
            }

            return stream;
        }

        private static int? GetBitDepth(string pixelFormat)
        {
            var eightBit = new List<string>
            {
                "yuv420p",
                "yuv411p",
                "yuvj420p",
                "uyyvyy411",
                "nv12",
                "nv21",
                "rgb444le",
                "rgb444be",
                "bgr444le",
                "bgr444be",
                "yuvj411p"            
            };

            if (!string.IsNullOrEmpty(pixelFormat))
            {
                if (eightBit.Contains(pixelFormat, StringComparer.OrdinalIgnoreCase))
                {
                    return 8;
                }
            }

            return null;
        }

        /// <summary>
        /// Gets a string from an FFProbeResult tags dictionary
        /// </summary>
        /// <param name="tags">The tags.</param>
        /// <param name="key">The key.</param>
        /// <returns>System.String.</returns>
        private static string GetDictionaryValue(Dictionary<string, string> tags, string key)
        {
            if (tags == null)
            {
                return null;
            }

            string val;

            tags.TryGetValue(key, out val);
            return val;
        }

        private static string ParseChannelLayout(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return input;
            }

            return input.Split('(').FirstOrDefault();
        }

        private static string GetAspectRatio(MediaStreamInfo info)
        {
            var original = info.display_aspect_ratio;

            int height;
            int width;

            var parts = (original ?? string.Empty).Split(':');
            if (!(parts.Length == 2 &&
                int.TryParse(parts[0], NumberStyles.Any, UsCulture, out width) &&
                int.TryParse(parts[1], NumberStyles.Any, UsCulture, out height) &&
                width > 0 &&
                height > 0))
            {
                width = info.width;
                height = info.height;
            }

            if (width > 0 && height > 0)
            {
                double ratio = width;
                ratio /= height;

                if (IsClose(ratio, 1.777777778, .03))
                {
                    return "16:9";
                }

                if (IsClose(ratio, 1.3333333333, .05))
                {
                    return "4:3";
                }

                if (IsClose(ratio, 1.41))
                {
                    return "1.41:1";
                }

                if (IsClose(ratio, 1.5))
                {
                    return "1.5:1";
                }

                if (IsClose(ratio, 1.6))
                {
                    return "1.6:1";
                }

                if (IsClose(ratio, 1.66666666667))
                {
                    return "5:3";
                }

                if (IsClose(ratio, 1.85, .02))
                {
                    return "1.85:1";
                }

                if (IsClose(ratio, 2.35, .025))
                {
                    return "2.35:1";
                }

                if (IsClose(ratio, 2.4, .025))
                {
                    return "2.40:1";
                }
            }

            return original;
        }

        private static bool IsClose(double d1, double d2, double variance = .005)
        {
            return Math.Abs(d1 - d2) <= variance;
        }

        /// <summary>
        /// Gets a frame rate from a string value in ffprobe output
        /// This could be a number or in the format of 2997/125.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>System.Nullable{System.Single}.</returns>
        private static float? GetFrameRate(string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                var parts = value.Split('/');

                float result;

                if (parts.Length == 2)
                {
                    result = float.Parse(parts[0], UsCulture) / float.Parse(parts[1], UsCulture);
                }
                else
                {
                    result = float.Parse(parts[0], UsCulture);
                }

                return float.IsNaN(result) ? (float?)null : result;
            }

            return null;
        }

    }
}
