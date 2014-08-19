﻿using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.IO;
using MediaBrowser.Controller.Channels;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Dlna;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Dlna;
using MediaBrowser.Model.Drawing;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.LiveTv;
using MediaBrowser.Model.MediaInfo;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Api.Playback
{
    /// <summary>
    /// Class BaseStreamingService
    /// </summary>
    public abstract class BaseStreamingService : BaseApiService
    {
        /// <summary>
        /// Gets or sets the application paths.
        /// </summary>
        /// <value>The application paths.</value>
        protected IServerConfigurationManager ServerConfigurationManager { get; private set; }

        /// <summary>
        /// Gets or sets the user manager.
        /// </summary>
        /// <value>The user manager.</value>
        protected IUserManager UserManager { get; private set; }

        /// <summary>
        /// Gets or sets the library manager.
        /// </summary>
        /// <value>The library manager.</value>
        protected ILibraryManager LibraryManager { get; private set; }

        /// <summary>
        /// Gets or sets the iso manager.
        /// </summary>
        /// <value>The iso manager.</value>
        protected IIsoManager IsoManager { get; private set; }

        /// <summary>
        /// Gets or sets the media encoder.
        /// </summary>
        /// <value>The media encoder.</value>
        protected IMediaEncoder MediaEncoder { get; private set; }

        protected IFileSystem FileSystem { get; private set; }

        protected ILiveTvManager LiveTvManager { get; private set; }
        protected IDlnaManager DlnaManager { get; private set; }
        protected IChannelManager ChannelManager { get; private set; }
        protected ISubtitleEncoder SubtitleEncoder { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseStreamingService" /> class.
        /// </summary>
        protected BaseStreamingService(IServerConfigurationManager serverConfig, IUserManager userManager, ILibraryManager libraryManager, IIsoManager isoManager, IMediaEncoder mediaEncoder, IFileSystem fileSystem, ILiveTvManager liveTvManager, IDlnaManager dlnaManager, IChannelManager channelManager, ISubtitleEncoder subtitleEncoder)
        {
            SubtitleEncoder = subtitleEncoder;
            ChannelManager = channelManager;
            DlnaManager = dlnaManager;
            LiveTvManager = liveTvManager;
            FileSystem = fileSystem;
            ServerConfigurationManager = serverConfig;
            UserManager = userManager;
            LibraryManager = libraryManager;
            IsoManager = isoManager;
            MediaEncoder = mediaEncoder;
        }

        /// <summary>
        /// Gets the command line arguments.
        /// </summary>
        /// <param name="outputPath">The output path.</param>
        /// <param name="state">The state.</param>
        /// <param name="isEncoding">if set to <c>true</c> [is encoding].</param>
        /// <returns>System.String.</returns>
        protected abstract string GetCommandLineArguments(string outputPath, StreamState state, bool isEncoding);

        /// <summary>
        /// Gets the type of the transcoding job.
        /// </summary>
        /// <value>The type of the transcoding job.</value>
        protected abstract TranscodingJobType TranscodingJobType { get; }

        /// <summary>
        /// Gets the output file extension.
        /// </summary>
        /// <param name="state">The state.</param>
        /// <returns>System.String.</returns>
        protected virtual string GetOutputFileExtension(StreamState state)
        {
            return Path.GetExtension(state.RequestedUrl);
        }

        /// <summary>
        /// Gets the output file path.
        /// </summary>
        /// <param name="state">The state.</param>
        /// <returns>System.String.</returns>
        private string GetOutputFilePath(StreamState state)
        {
            var folder = ServerConfigurationManager.ApplicationPaths.TranscodingTempPath;

            var outputFileExtension = GetOutputFileExtension(state);

            var data = GetCommandLineArguments("dummy\\dummy", state, false);

            data += "-" + (state.Request.DeviceId ?? string.Empty);

            return Path.Combine(folder, data.GetMD5().ToString("N") + (outputFileExtension ?? string.Empty).ToLower());
        }

        protected readonly CultureInfo UsCulture = new CultureInfo("en-US");

        /// <summary>
        /// Gets the fast seek command line parameter.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.String.</returns>
        /// <value>The fast seek command line parameter.</value>
        protected string GetFastSeekCommandLineParameter(StreamRequest request)
        {
            var time = request.StartTimeTicks;

            if (time.HasValue && time.Value > 0)
            {
                return string.Format("-ss {0}", MediaEncoder.GetTimeParameter(time.Value));
            }

            return string.Empty;
        }

        /// <summary>
        /// Gets the map args.
        /// </summary>
        /// <param name="state">The state.</param>
        /// <returns>System.String.</returns>
        protected virtual string GetMapArgs(StreamState state)
        {
            // If we don't have known media info
            // If input is video, use -sn to drop subtitles
            // Otherwise just return empty
            if (state.VideoStream == null && state.AudioStream == null)
            {
                return state.IsInputVideo ? "-sn" : string.Empty;
            }

            // We have media info, but we don't know the stream indexes
            if (state.VideoStream != null && state.VideoStream.Index == -1)
            {
                return "-sn";
            }

            // We have media info, but we don't know the stream indexes
            if (state.AudioStream != null && state.AudioStream.Index == -1)
            {
                return state.IsInputVideo ? "-sn" : string.Empty;
            }

            var args = string.Empty;

            if (state.VideoStream != null)
            {
                args += string.Format("-map 0:{0}", state.VideoStream.Index);
            }
            else
            {
                args += "-map -0:v";
            }

            if (state.AudioStream != null)
            {
                args += string.Format(" -map 0:{0}", state.AudioStream.Index);
            }

            else
            {
                args += " -map -0:a";
            }

            if (state.SubtitleStream == null)
            {
                args += " -map -0:s";
            }

            return args;
        }

        /// <summary>
        /// Determines which stream will be used for playback
        /// </summary>
        /// <param name="allStream">All stream.</param>
        /// <param name="desiredIndex">Index of the desired.</param>
        /// <param name="type">The type.</param>
        /// <param name="returnFirstIfNoIndex">if set to <c>true</c> [return first if no index].</param>
        /// <returns>MediaStream.</returns>
        private MediaStream GetMediaStream(IEnumerable<MediaStream> allStream, int? desiredIndex, MediaStreamType type, bool returnFirstIfNoIndex = true)
        {
            var streams = allStream.Where(s => s.Type == type).OrderBy(i => i.Index).ToList();

            if (desiredIndex.HasValue)
            {
                var stream = streams.FirstOrDefault(s => s.Index == desiredIndex.Value);

                if (stream != null)
                {
                    return stream;
                }
            }

            if (type == MediaStreamType.Video)
            {
                streams = streams.Where(i => !string.Equals(i.Codec, "mjpeg", StringComparison.OrdinalIgnoreCase)).ToList();
            }

            if (returnFirstIfNoIndex && type == MediaStreamType.Audio)
            {
                return streams.FirstOrDefault(i => i.Channels.HasValue && i.Channels.Value > 0) ??
                       streams.FirstOrDefault();
            }

            // Just return the first one
            return returnFirstIfNoIndex ? streams.FirstOrDefault() : null;
        }

        protected EncodingQuality GetQualitySetting()
        {
            var quality = ServerConfigurationManager.Configuration.MediaEncodingQuality;

            if (quality == EncodingQuality.Auto)
            {
                var cpuCount = Environment.ProcessorCount;

                if (cpuCount >= 4)
                {
                    //return EncodingQuality.HighQuality;
                }

                return EncodingQuality.HighSpeed;
            }

            return quality;
        }

        /// <summary>
        /// Gets the number of threads.
        /// </summary>
        /// <returns>System.Int32.</returns>
        /// <exception cref="System.Exception">Unrecognized MediaEncodingQuality value.</exception>
        protected int GetNumberOfThreads(StreamState state, bool isWebm)
        {
            // Use more when this is true. -re will keep cpu usage under control
            if (state.ReadInputAtNativeFramerate)
            {
                if (isWebm)
                {
                    return Math.Max(Environment.ProcessorCount - 1, 2);
                }

                return 0;
            }

            // Webm: http://www.webmproject.org/docs/encoder-parameters/
            // The decoder will usually automatically use an appropriate number of threads according to how many cores are available but it can only use multiple threads 
            // for the coefficient data if the encoder selected --token-parts > 0 at encode time.

            switch (GetQualitySetting())
            {
                case EncodingQuality.HighSpeed:
                    return 2;
                case EncodingQuality.HighQuality:
                    return 2;
                case EncodingQuality.MaxQuality:
                    return isWebm ? Math.Max(Environment.ProcessorCount - 1, 2) : 0;
                default:
                    throw new Exception("Unrecognized MediaEncodingQuality value.");
            }
        }

        /// <summary>
        /// Gets the video bitrate to specify on the command line
        /// </summary>
        /// <param name="state">The state.</param>
        /// <param name="videoCodec">The video codec.</param>
        /// <param name="isHls">if set to <c>true</c> [is HLS].</param>
        /// <returns>System.String.</returns>
        protected string GetVideoQualityParam(StreamState state, string videoCodec, bool isHls)
        {
            var param = string.Empty;

            var isVc1 = state.VideoStream != null &&
                string.Equals(state.VideoStream.Codec, "vc1", StringComparison.OrdinalIgnoreCase);

            var qualitySetting = GetQualitySetting();

            if (string.Equals(videoCodec, "libx264", StringComparison.OrdinalIgnoreCase))
            {
                switch (qualitySetting)
                {
                    case EncodingQuality.HighSpeed:
                        param = "-preset superfast";
                        break;
                    case EncodingQuality.HighQuality:
                        param = "-preset superfast";
                        break;
                    case EncodingQuality.MaxQuality:
                        param = "-preset superfast";
                        break;
                }

                switch (qualitySetting)
                {
                    case EncodingQuality.HighSpeed:
                        param += " -crf 23";
                        break;
                    case EncodingQuality.HighQuality:
                        param += " -crf 20";
                        break;
                    case EncodingQuality.MaxQuality:
                        param += " -crf 18";
                        break;
                }
            }

            // webm
            else if (string.Equals(videoCodec, "libvpx", StringComparison.OrdinalIgnoreCase))
            {
                // Values 0-3, 0 being highest quality but slower
                var profileScore = 0;

                string crf;

                switch (qualitySetting)
                {
                    case EncodingQuality.HighSpeed:
                        crf = "12";
                        profileScore = 2;
                        break;
                    case EncodingQuality.HighQuality:
                        crf = "8";
                        profileScore = 1;
                        break;
                    case EncodingQuality.MaxQuality:
                        crf = "4";
                        break;
                    default:
                        throw new ArgumentException("Unrecognized quality setting");
                }

                if (isVc1)
                {
                    profileScore++;
                    // Max of 2
                    profileScore = Math.Min(profileScore, 2);
                }

                // http://www.webmproject.org/docs/encoder-parameters/
                param = string.Format("-speed 16 -quality good -profile:v {0} -slices 8 -crf {1}",
                    profileScore.ToString(UsCulture),
                    crf);
            }

            else if (string.Equals(videoCodec, "mpeg4", StringComparison.OrdinalIgnoreCase))
            {
                param = "-mbd rd -flags +mv4+aic -trellis 2 -cmp 2 -subcmp 2 -bf 2";
            }

            // asf/wmv
            else if (string.Equals(videoCodec, "wmv2", StringComparison.OrdinalIgnoreCase))
            {
                param = "-qmin 2";
            }

            else if (string.Equals(videoCodec, "msmpeg4", StringComparison.OrdinalIgnoreCase))
            {
                param = "-mbd 2";
            }

            param += GetVideoBitrateParam(state, videoCodec, isHls);

            var framerate = GetFramerateParam(state);
            if (framerate.HasValue)
            {
                param += string.Format(" -r {0}", framerate.Value.ToString(UsCulture));
            }

            if (!string.IsNullOrEmpty(state.OutputVideoSync))
            {
                param += " -vsync " + state.OutputVideoSync;
            }

            if (!string.IsNullOrEmpty(state.VideoRequest.Profile))
            {
                param += " -profile:v " + state.VideoRequest.Profile;
            }

            if (!string.IsNullOrEmpty(state.VideoRequest.Level))
            {
                param += " -level " + state.VideoRequest.Level;
            }

            return param;
        }

        protected string GetAudioFilterParam(StreamState state, bool isHls)
        {
            var volParam = string.Empty;
            var audioSampleRate = string.Empty;

            var channels = state.OutputAudioChannels;

            // Boost volume to 200% when downsampling from 6ch to 2ch
            if (channels.HasValue && channels.Value <= 2)
            {
                if (state.AudioStream != null && state.AudioStream.Channels.HasValue && state.AudioStream.Channels.Value > 5)
                {
                    volParam = ",volume=" + ServerConfigurationManager.Configuration.DownMixAudioBoost.ToString(UsCulture);
                }
            }

            if (state.OutputAudioSampleRate.HasValue)
            {
                audioSampleRate = state.OutputAudioSampleRate.Value + ":";
            }

            var adelay = isHls ? "adelay=1," : string.Empty;

            var pts = string.Empty;

            if (state.SubtitleStream != null && state.SubtitleStream.IsTextSubtitleStream)
            {
                var seconds = TimeSpan.FromTicks(state.Request.StartTimeTicks ?? 0).TotalSeconds;

                pts = string.Format(",asetpts=PTS-{0}/TB", Math.Round(seconds).ToString(UsCulture));
            }

            return string.Format("-af \"{0}aresample={1}async={4}{2}{3}\"",

                adelay,
                audioSampleRate,
                volParam,
                pts,
                state.OutputAudioSync);
        }

        /// <summary>
        /// If we're going to put a fixed size on the command line, this will calculate it
        /// </summary>
        /// <param name="state">The state.</param>
        /// <param name="outputVideoCodec">The output video codec.</param>
        /// <param name="allowTimeStampCopy">if set to <c>true</c> [allow time stamp copy].</param>
        /// <returns>System.String.</returns>
        protected string GetOutputSizeParam(StreamState state,
            string outputVideoCodec,
            bool allowTimeStampCopy = true)
        {
            // http://sonnati.wordpress.com/2012/10/19/ffmpeg-the-swiss-army-knife-of-internet-streaming-part-vi/

            var request = state.VideoRequest;

            var filters = new List<string>();

            if (state.DeInterlace)
            {
                filters.Add("yadif=0:-1:0");
            }

            // If fixed dimensions were supplied
            if (request.Width.HasValue && request.Height.HasValue)
            {
                var widthParam = request.Width.Value.ToString(UsCulture);
                var heightParam = request.Height.Value.ToString(UsCulture);

                filters.Add(string.Format("scale=trunc({0}/2)*2:trunc({1}/2)*2", widthParam, heightParam));
            }

            // If Max dimensions were supplied, for width selects lowest even number between input width and width req size and selects lowest even number from in width*display aspect and requested size
            else if (request.MaxWidth.HasValue && request.MaxHeight.HasValue)
            {
                var maxWidthParam = request.MaxWidth.Value.ToString(UsCulture);
                var maxHeightParam = request.MaxHeight.Value.ToString(UsCulture);

                filters.Add(string.Format("scale=trunc(min(iw\\,{0})/2)*2:trunc(min((iw/dar)\\,{1})/2)*2", maxWidthParam, maxHeightParam));
            }

            // If a fixed width was requested
            else if (request.Width.HasValue)
            {
                var widthParam = request.Width.Value.ToString(UsCulture);

                filters.Add(string.Format("scale={0}:trunc(ow/a/2)*2", widthParam));
            }

            // If a fixed height was requested
            else if (request.Height.HasValue)
            {
                var heightParam = request.Height.Value.ToString(UsCulture);

                filters.Add(string.Format("scale=trunc(oh*a*2)/2:{0}", heightParam));
            }

            // If a max width was requested
            else if (request.MaxWidth.HasValue && (!request.MaxHeight.HasValue || state.VideoStream == null))
            {
                var maxWidthParam = request.MaxWidth.Value.ToString(UsCulture);

                filters.Add(string.Format("scale=min(iw\\,{0}):trunc(ow/dar/2)*2", maxWidthParam));
            }

            // If a max height was requested
            else if (request.MaxHeight.HasValue && (!request.MaxWidth.HasValue || state.VideoStream == null))
            {
                var maxHeightParam = request.MaxHeight.Value.ToString(UsCulture);

                filters.Add(string.Format("scale=trunc(oh*a*2)/2:min(ih\\,{0})", maxHeightParam));
            }

            else if (request.MaxWidth.HasValue ||
                request.MaxHeight.HasValue ||
                request.Width.HasValue ||
                request.Height.HasValue)
            {
                if (state.VideoStream != null)
                {
                    // Need to perform calculations manually

                    // Try to account for bad media info
                    var currentHeight = state.VideoStream.Height ?? request.MaxHeight ?? request.Height ?? 0;
                    var currentWidth = state.VideoStream.Width ?? request.MaxWidth ?? request.Width ?? 0;

                    var outputSize = DrawingUtils.Resize(currentWidth, currentHeight, request.Width, request.Height, request.MaxWidth, request.MaxHeight);

                    var manualWidthParam = outputSize.Width.ToString(UsCulture);
                    var manualHeightParam = outputSize.Height.ToString(UsCulture);

                    filters.Add(string.Format("scale=trunc({0}/2)*2:trunc({1}/2)*2", manualWidthParam, manualHeightParam));
                }
            }

            var output = string.Empty;

            if (state.SubtitleStream != null && state.SubtitleStream.IsTextSubtitleStream)
            {
                var subParam = GetTextSubtitleParam(state);

                filters.Add(subParam);

                if (allowTimeStampCopy)
                {
                    output += " -copyts";
                }
            }

            if (filters.Count > 0)
            {
                output += string.Format(" -vf \"{0}\"", string.Join(",", filters.ToArray()));
            }

            return output;
        }

        /// <summary>
        /// Gets the text subtitle param.
        /// </summary>
        /// <param name="state">The state.</param>
        /// <returns>System.String.</returns>
        protected string GetTextSubtitleParam(StreamState state)
        {
            var seconds = Math.Round(TimeSpan.FromTicks(state.Request.StartTimeTicks ?? 0).TotalSeconds);

            if (state.SubtitleStream.IsExternal)
            {
                var subtitlePath = state.SubtitleStream.Path;

                var charsetParam = string.Empty;

                if (!string.IsNullOrEmpty(state.SubtitleStream.Language))
                {
                    var charenc = SubtitleEncoder.GetSubtitleFileCharacterSet(subtitlePath, state.SubtitleStream.Language);

                    if (!string.IsNullOrEmpty(charenc))
                    {
                        charsetParam = ":charenc=" + charenc;
                    }
                }

                // TODO: Perhaps also use original_size=1920x800 ??
                return string.Format("subtitles=filename='{0}'{1},setpts=PTS -{2}/TB",
                    subtitlePath.Replace('\\', '/').Replace(":/", "\\:/"),
                    charsetParam,
                    seconds.ToString(UsCulture));
            }

            return string.Format("subtitles='{0}:si={1}',setpts=PTS -{2}/TB",
                state.MediaPath.Replace('\\', '/').Replace(":/", "\\:/"),
                state.InternalSubtitleStreamOffset.ToString(UsCulture),
                seconds.ToString(UsCulture));
        }

        /// <summary>
        /// Gets the internal graphical subtitle param.
        /// </summary>
        /// <param name="state">The state.</param>
        /// <param name="outputVideoCodec">The output video codec.</param>
        /// <returns>System.String.</returns>
        protected string GetGraphicalSubtitleParam(StreamState state, string outputVideoCodec)
        {
            var outputSizeParam = string.Empty;

            var request = state.VideoRequest;

            // Add resolution params, if specified
            if (request.Width.HasValue || request.Height.HasValue || request.MaxHeight.HasValue || request.MaxWidth.HasValue)
            {
                outputSizeParam = GetOutputSizeParam(state, outputVideoCodec).TrimEnd('"');
                outputSizeParam = "," + outputSizeParam.Substring(outputSizeParam.IndexOf("scale", StringComparison.OrdinalIgnoreCase));
            }

            var videoSizeParam = string.Empty;

            if (state.VideoStream != null && state.VideoStream.Width.HasValue && state.VideoStream.Height.HasValue)
            {
                videoSizeParam = string.Format(",scale={0}:{1}", state.VideoStream.Width.Value.ToString(UsCulture), state.VideoStream.Height.Value.ToString(UsCulture));
            }

            return string.Format(" -filter_complex \"[0:{0}]format=yuva444p{3},lut=u=128:v=128:y=gammaval(.3)[sub] ; [0:{1}] [sub] overlay{2}\"",
                state.SubtitleStream.Index,
                state.VideoStream.Index,
                outputSizeParam,
                videoSizeParam);
        }

        /// <summary>
        /// Gets the probe size argument.
        /// </summary>
        /// <param name="state">The state.</param>
        /// <returns>System.String.</returns>
        private string GetProbeSizeArgument(StreamState state)
        {
            if (state.PlayableStreamFileNames.Count > 0)
            {
                return MediaEncoder.GetProbeSizeArgument(state.PlayableStreamFileNames.ToArray(), state.InputProtocol);
            }

            return MediaEncoder.GetProbeSizeArgument(new[] { state.MediaPath }, state.InputProtocol);
        }

        /// <summary>
        /// Gets the number of audio channels to specify on the command line
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="audioStream">The audio stream.</param>
        /// <param name="outputAudioCodec">The output audio codec.</param>
        /// <returns>System.Nullable{System.Int32}.</returns>
        private int? GetNumAudioChannelsParam(StreamRequest request, MediaStream audioStream, string outputAudioCodec)
        {
            if (audioStream != null)
            {
                var codec = outputAudioCodec ?? string.Empty;

                if (audioStream.Channels > 2 && codec.IndexOf("wma", StringComparison.OrdinalIgnoreCase) != -1)
                {
                    // wmav2 currently only supports two channel output
                    return 2;
                }
            }

            if (request.MaxAudioChannels.HasValue)
            {
                if (audioStream != null && audioStream.Channels.HasValue)
                {
                    return Math.Min(request.MaxAudioChannels.Value, audioStream.Channels.Value);
                }

                return request.MaxAudioChannels.Value;
            }

            return request.AudioChannels;
        }

        /// <summary>
        /// Determines whether the specified stream is H264.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <returns><c>true</c> if the specified stream is H264; otherwise, <c>false</c>.</returns>
        protected bool IsH264(MediaStream stream)
        {
            return stream.Codec.IndexOf("264", StringComparison.OrdinalIgnoreCase) != -1 ||
                   stream.Codec.IndexOf("avc", StringComparison.OrdinalIgnoreCase) != -1;
        }

        /// <summary>
        /// Gets the name of the output audio codec
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.String.</returns>
        private string GetAudioCodec(StreamRequest request)
        {
            var codec = request.AudioCodec;

            if (string.Equals(codec, "aac", StringComparison.OrdinalIgnoreCase))
            {
                return "aac -strict experimental";
            }
            if (string.Equals(codec, "mp3", StringComparison.OrdinalIgnoreCase))
            {
                return "libmp3lame";
            }
            if (string.Equals(codec, "vorbis", StringComparison.OrdinalIgnoreCase))
            {
                return "libvorbis";
            }
            if (string.Equals(codec, "wma", StringComparison.OrdinalIgnoreCase))
            {
                return "wmav2";
            }

            return codec.ToLower();
        }

        /// <summary>
        /// Gets the name of the output video codec
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.String.</returns>
        private string GetVideoCodec(VideoStreamRequest request)
        {
            var codec = request.VideoCodec;

            if (!string.IsNullOrEmpty(codec))
            {
                if (string.Equals(codec, "h264", StringComparison.OrdinalIgnoreCase))
                {
                    return "libx264";
                }
                if (string.Equals(codec, "vpx", StringComparison.OrdinalIgnoreCase))
                {
                    return "libvpx";
                }
                if (string.Equals(codec, "wmv", StringComparison.OrdinalIgnoreCase))
                {
                    return "wmv2";
                }
                if (string.Equals(codec, "theora", StringComparison.OrdinalIgnoreCase))
                {
                    return "libtheora";
                }

                return codec.ToLower();
            }

            return "copy";
        }

        /// <summary>
        /// Gets the input argument.
        /// </summary>
        /// <param name="state">The state.</param>
        /// <returns>System.String.</returns>
        protected virtual string GetInputArgument(StreamState state)
        {
            var protocol = state.InputProtocol;

            var inputPath = new[] { state.MediaPath };

            if (state.IsInputVideo)
            {
                if (!(state.VideoType == VideoType.Iso && state.IsoMount == null))
                {
                    inputPath = MediaEncoderHelpers.GetInputArgument(state.MediaPath, state.InputProtocol, state.IsoMount, state.PlayableStreamFileNames);
                }
            }

            return MediaEncoder.GetInputArgument(inputPath, protocol);
        }

        private async Task AcquireResources(StreamState state, CancellationTokenSource cancellationTokenSource)
        {
            if (state.VideoType == VideoType.Iso && state.IsoType.HasValue && IsoManager.CanMount(state.MediaPath))
            {
                state.IsoMount = await IsoManager.Mount(state.MediaPath, cancellationTokenSource.Token).ConfigureAwait(false);
            }

            if (string.IsNullOrEmpty(state.MediaPath))
            {
                var checkCodecs = false;

                if (string.Equals(state.ItemType, typeof(LiveTvChannel).Name))
                {
                    var streamInfo = await LiveTvManager.GetChannelStream(state.Request.Id, cancellationTokenSource.Token).ConfigureAwait(false);

                    state.LiveTvStreamId = streamInfo.Id;

                    if (!string.IsNullOrEmpty(streamInfo.Path))
                    {
                        state.MediaPath = streamInfo.Path;
                        state.InputProtocol = MediaProtocol.File;

                        await Task.Delay(1500, cancellationTokenSource.Token).ConfigureAwait(false);
                    }
                    else if (!string.IsNullOrEmpty(streamInfo.Url))
                    {
                        state.MediaPath = streamInfo.Url;
                        state.InputProtocol = MediaProtocol.Http;
                    }

                    AttachMediaStreamInfo(state, streamInfo.MediaStreams, state.VideoRequest, state.RequestedUrl);
                    checkCodecs = true;
                }

                else if (string.Equals(state.ItemType, typeof(LiveTvVideoRecording).Name) ||
                    string.Equals(state.ItemType, typeof(LiveTvAudioRecording).Name))
                {
                    var streamInfo = await LiveTvManager.GetRecordingStream(state.Request.Id, cancellationTokenSource.Token).ConfigureAwait(false);

                    state.LiveTvStreamId = streamInfo.Id;

                    if (!string.IsNullOrEmpty(streamInfo.Path))
                    {
                        state.MediaPath = streamInfo.Path;
                        state.InputProtocol = MediaProtocol.File;

                        await Task.Delay(1500, cancellationTokenSource.Token).ConfigureAwait(false);
                    }
                    else if (!string.IsNullOrEmpty(streamInfo.Url))
                    {
                        state.MediaPath = streamInfo.Url;
                        state.InputProtocol = MediaProtocol.Http;
                    }

                    AttachMediaStreamInfo(state, streamInfo.MediaStreams, state.VideoRequest, state.RequestedUrl);
                    checkCodecs = true;
                }

                var videoRequest = state.VideoRequest;

                if (videoRequest != null && checkCodecs)
                {
                    if (state.VideoStream != null && CanStreamCopyVideo(videoRequest, state.VideoStream))
                    {
                        state.OutputVideoCodec = "copy";
                    }

                    if (state.AudioStream != null && CanStreamCopyAudio(videoRequest, state.AudioStream, state.SupportedAudioCodecs))
                    {
                        state.OutputAudioCodec = "copy";
                    }
                }
            }
        }

        /// <summary>
        /// Starts the FFMPEG.
        /// </summary>
        /// <param name="state">The state.</param>
        /// <param name="outputPath">The output path.</param>
        /// <param name="cancellationTokenSource">The cancellation token source.</param>
        /// <returns>Task.</returns>
        /// <exception cref="System.InvalidOperationException">ffmpeg was not found at  + MediaEncoder.EncoderPath</exception>
        protected async Task StartFfMpeg(StreamState state, string outputPath, CancellationTokenSource cancellationTokenSource)
        {
            if (!File.Exists(MediaEncoder.EncoderPath))
            {
                throw new InvalidOperationException("ffmpeg was not found at " + MediaEncoder.EncoderPath);
            }

            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));

            await AcquireResources(state, cancellationTokenSource).ConfigureAwait(false);

            var commandLineArgs = GetCommandLineArguments(outputPath, state, true);

            if (ServerConfigurationManager.Configuration.EnableDebugEncodingLogging)
            {
                commandLineArgs = "-loglevel debug " + commandLineArgs;
            }

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    CreateNoWindow = true,
                    UseShellExecute = false,

                    // Must consume both stdout and stderr or deadlocks may occur
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    RedirectStandardInput = true,

                    FileName = MediaEncoder.EncoderPath,
                    WorkingDirectory = Path.GetDirectoryName(MediaEncoder.EncoderPath),
                    Arguments = commandLineArgs,

                    WindowStyle = ProcessWindowStyle.Hidden,
                    ErrorDialog = false
                },

                EnableRaisingEvents = true
            };

            ApiEntryPoint.Instance.OnTranscodeBeginning(outputPath,
                TranscodingJobType,
                process,
                state.Request.DeviceId,
                state,
                cancellationTokenSource);

            var commandLineLogMessage = process.StartInfo.FileName + " " + process.StartInfo.Arguments;
            Logger.Info(commandLineLogMessage);

            var logFilePath = Path.Combine(ServerConfigurationManager.ApplicationPaths.LogDirectoryPath, "transcode-" + Guid.NewGuid() + ".txt");
            Directory.CreateDirectory(Path.GetDirectoryName(logFilePath));

            // FFMpeg writes debug/error info to stderr. This is useful when debugging so let's put it in the log directory.
            state.LogFileStream = FileSystem.GetFileStream(logFilePath, FileMode.Create, FileAccess.Write, FileShare.Read, true);

            var commandLineLogMessageBytes = Encoding.UTF8.GetBytes(commandLineLogMessage + Environment.NewLine + Environment.NewLine);
            await state.LogFileStream.WriteAsync(commandLineLogMessageBytes, 0, commandLineLogMessageBytes.Length, cancellationTokenSource.Token).ConfigureAwait(false);

            process.Exited += (sender, args) => OnFfMpegProcessExited(process, state, outputPath);

            try
            {
                process.Start();
            }
            catch (Exception ex)
            {
                Logger.ErrorException("Error starting ffmpeg", ex);

                ApiEntryPoint.Instance.OnTranscodeFailedToStart(outputPath, TranscodingJobType, state);

                throw;
            }

            // MUST read both stdout and stderr asynchronously or a deadlock may occurr
            process.BeginOutputReadLine();

            // Important - don't await the log task or we won't be able to kill ffmpeg when the user stops playback
            StartStreamingLog(state, process.StandardError.BaseStream, state.LogFileStream);

            // Wait for the file to exist before proceeeding
            while (!File.Exists(outputPath))
            {
                await Task.Delay(100, cancellationTokenSource.Token).ConfigureAwait(false);
            }
        }

        private async void StartStreamingLog(StreamState state, Stream source, Stream target)
        {
            try
            {
                using (var reader = new StreamReader(source))
                {
                    while (!reader.EndOfStream)
                    {
                        var line = await reader.ReadLineAsync().ConfigureAwait(false);

                        ParseLogLine(line, state);

                        var bytes = Encoding.UTF8.GetBytes(Environment.NewLine + line);

                        await target.WriteAsync(bytes, 0, bytes.Length).ConfigureAwait(false);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.ErrorException("Error reading ffmpeg log", ex);
            }
        }

        private void ParseLogLine(string line, StreamState state)
        {
            float? framerate = null;
            double? percent = null;

            var parts = line.Split(' ');

            var totalMs = state.RunTimeTicks.HasValue
                ? TimeSpan.FromTicks(state.RunTimeTicks.Value).TotalMilliseconds
                : 0;

            var startMs = state.Request.StartTimeTicks.HasValue
                ? TimeSpan.FromTicks(state.Request.StartTimeTicks.Value).TotalMilliseconds
                : 0;

            for (var i = 0; i < parts.Length; i++)
            {
                var part = parts[i];

                if (string.Equals(part, "fps=", StringComparison.OrdinalIgnoreCase) &&
                    (i + 1 < parts.Length))
                {
                    var rate = parts[i + 1];
                    float val;

                    if (float.TryParse(rate, NumberStyles.Any, UsCulture, out val))
                    {
                        framerate = val;
                    }
                }
                else if (state.RunTimeTicks.HasValue &&
                    part.StartsWith("time=", StringComparison.OrdinalIgnoreCase))
                {
                    var time = part.Split(new[] { '=' }, 2).Last();
                    TimeSpan val;

                    if (TimeSpan.TryParse(time, UsCulture, out val))
                    {
                        var currentMs = startMs + val.TotalMilliseconds;

                        var percentVal = currentMs / totalMs;
                        percent = 100 * percentVal;
                    }
                }
            }

            if (framerate.HasValue || percent.HasValue)
            {
                ApiEntryPoint.Instance.ReportTranscodingProgress(state, framerate, percent);
            }
        }

        private int? GetVideoBitrateParamValue(VideoStreamRequest request, MediaStream videoStream)
        {
            var bitrate = request.VideoBitRate;

            if (videoStream != null)
            {
                var isUpscaling = request.Height.HasValue && videoStream.Height.HasValue &&
                                   request.Height.Value > videoStream.Height.Value;

                if (request.Width.HasValue && videoStream.Width.HasValue &&
                    request.Width.Value > videoStream.Width.Value)
                {
                    isUpscaling = true;
                }

                // Don't allow bitrate increases unless upscaling
                if (!isUpscaling)
                {
                    if (bitrate.HasValue && videoStream.BitRate.HasValue)
                    {
                        bitrate = Math.Min(bitrate.Value, videoStream.BitRate.Value);
                    }
                }
            }

            return bitrate;
        }

        protected string GetVideoBitrateParam(StreamState state, string videoCodec, bool isHls)
        {
            var bitrate = state.OutputVideoBitrate;

            if (bitrate.HasValue)
            {
                var hasFixedResolution = state.VideoRequest.HasFixedResolution;

                if (string.Equals(videoCodec, "libvpx", StringComparison.OrdinalIgnoreCase))
                {
                    if (hasFixedResolution)
                    {
                        return string.Format(" -minrate:v ({0}*.90) -maxrate:v ({0}*1.10) -bufsize:v {0} -b:v {0}", bitrate.Value.ToString(UsCulture));
                    }

                    // With vpx when crf is used, b:v becomes a max rate
                    // https://trac.ffmpeg.org/wiki/vpxEncodingGuide. But higher bitrate source files -b:v causes judder so limite the bitrate but dont allow it to "saturate" the bitrate. So dont contrain it down just up.
                    return string.Format(" -maxrate:v {0} -bufsize:v ({0}*2) -b:v {0}", bitrate.Value.ToString(UsCulture));
                }

                if (string.Equals(videoCodec, "msmpeg4", StringComparison.OrdinalIgnoreCase))
                {
                    return string.Format(" -b:v {0}", bitrate.Value.ToString(UsCulture));
                }

                // H264
                if (hasFixedResolution)
                {
                    if (isHls)
                    {
                        return string.Format(" -b:v {0} -maxrate ({0}*.80) -bufsize {0}", bitrate.Value.ToString(UsCulture));
                    }

                    return string.Format(" -b:v {0}", bitrate.Value.ToString(UsCulture));
                }

                return string.Format(" -maxrate {0} -bufsize {1}",
                    bitrate.Value.ToString(UsCulture),
                    (bitrate.Value * 2).ToString(UsCulture));
            }

            return string.Empty;
        }

        private int? GetAudioBitrateParam(StreamRequest request, MediaStream audioStream)
        {
            if (request.AudioBitRate.HasValue)
            {
                // Make sure we don't request a bitrate higher than the source
                var currentBitrate = audioStream == null ? request.AudioBitRate.Value : audioStream.BitRate ?? request.AudioBitRate.Value;

                return request.AudioBitRate.Value;
                //return Math.Min(currentBitrate, request.AudioBitRate.Value);
            }

            return null;
        }

        /// <summary>
        /// Gets the user agent param.
        /// </summary>
        /// <param name="state">The state.</param>
        /// <returns>System.String.</returns>
        private string GetUserAgentParam(StreamState state)
        {
            string useragent = null;

            state.RemoteHttpHeaders.TryGetValue("User-Agent", out useragent);

            if (!string.IsNullOrWhiteSpace(useragent))
            {
                return "-user-agent \"" + useragent + "\"";
            }

            return string.Empty;
        }

        /// <summary>
        /// Processes the exited.
        /// </summary>
        /// <param name="process">The process.</param>
        /// <param name="state">The state.</param>
        /// <param name="outputPath">The output path.</param>
        private void OnFfMpegProcessExited(Process process, StreamState state, string outputPath)
        {
            var job = ApiEntryPoint.Instance.GetTranscodingJob(outputPath, TranscodingJobType);

            if (job != null)
            {
                job.HasExited = true;
            }

            Logger.Debug("Disposing stream resources");
            state.Dispose();

            try
            {
                Logger.Info("FFMpeg exited with code {0}", process.ExitCode);
            }
            catch
            {
                Logger.Error("FFMpeg exited with an error.");
            }

            // This causes on exited to be called twice:
            //try
            //{
            //    // Dispose the process
            //    process.Dispose();
            //}
            //catch (Exception ex)
            //{
            //    Logger.ErrorException("Error disposing ffmpeg.", ex);
            //}
        }

        protected double? GetFramerateParam(StreamState state)
        {
            if (state.VideoRequest != null)
            {
                if (state.VideoRequest.Framerate.HasValue)
                {
                    return state.VideoRequest.Framerate.Value;
                }

                var maxrate = state.VideoRequest.MaxFramerate ?? 23.97602;

                if (state.VideoStream != null)
                {
                    var contentRate = state.VideoStream.AverageFrameRate ?? state.VideoStream.RealFrameRate;

                    if (contentRate.HasValue && contentRate.Value > maxrate)
                    {
                        return maxrate;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Parses the parameters.
        /// </summary>
        /// <param name="request">The request.</param>
        private void ParseParams(StreamRequest request)
        {
            var vals = request.Params.Split(';');

            var videoRequest = request as VideoStreamRequest;

            for (var i = 0; i < vals.Length; i++)
            {
                var val = vals[i];

                if (string.IsNullOrWhiteSpace(val))
                {
                    continue;
                }

                if (i == 0)
                {
                    request.DeviceProfileId = val;
                }
                else if (i == 1)
                {
                    request.DeviceId = val;
                }
                else if (i == 2)
                {
                    request.MediaSourceId = val;
                }
                else if (i == 3)
                {
                    request.Static = string.Equals("true", val, StringComparison.OrdinalIgnoreCase);
                }
                else if (i == 4)
                {
                    if (videoRequest != null)
                    {
                        videoRequest.VideoCodec = val;
                    }
                }
                else if (i == 5)
                {
                    request.AudioCodec = val;
                }
                else if (i == 6)
                {
                    if (videoRequest != null)
                    {
                        videoRequest.AudioStreamIndex = int.Parse(val, UsCulture);
                    }
                }
                else if (i == 7)
                {
                    if (videoRequest != null)
                    {
                        videoRequest.SubtitleStreamIndex = int.Parse(val, UsCulture);
                    }
                }
                else if (i == 8)
                {
                    if (videoRequest != null)
                    {
                        videoRequest.VideoBitRate = int.Parse(val, UsCulture);
                    }
                }
                else if (i == 9)
                {
                    request.AudioBitRate = int.Parse(val, UsCulture);
                }
                else if (i == 10)
                {
                    request.MaxAudioChannels = int.Parse(val, UsCulture);
                }
                else if (i == 11)
                {
                    if (videoRequest != null)
                    {
                        videoRequest.MaxFramerate = float.Parse(val, UsCulture);
                    }
                }
                else if (i == 12)
                {
                    if (videoRequest != null)
                    {
                        videoRequest.MaxWidth = int.Parse(val, UsCulture);
                    }
                }
                else if (i == 13)
                {
                    if (videoRequest != null)
                    {
                        videoRequest.MaxHeight = int.Parse(val, UsCulture);
                    }
                }
                else if (i == 14)
                {
                    request.StartTimeTicks = long.Parse(val, UsCulture);
                }
                else if (i == 15)
                {
                    if (videoRequest != null)
                    {
                        videoRequest.Level = val;
                    }
                }
            }
        }

        /// <summary>
        /// Parses the dlna headers.
        /// </summary>
        /// <param name="request">The request.</param>
        private void ParseDlnaHeaders(StreamRequest request)
        {
            if (!request.StartTimeTicks.HasValue)
            {
                var timeSeek = GetHeader("TimeSeekRange.dlna.org");

                request.StartTimeTicks = ParseTimeSeekHeader(timeSeek);
            }
        }

        /// <summary>
        /// Parses the time seek header.
        /// </summary>
        private long? ParseTimeSeekHeader(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            if (value.IndexOf("npt=", StringComparison.OrdinalIgnoreCase) != 0)
            {
                throw new ArgumentException("Invalid timeseek header");
            }
            value = value.Substring(4).Split(new[] { '-' }, 2)[0];

            if (value.IndexOf(':') == -1)
            {
                // Parses npt times in the format of '417.33'
                double seconds;
                if (double.TryParse(value, NumberStyles.Any, UsCulture, out seconds))
                {
                    return TimeSpan.FromSeconds(seconds).Ticks;
                }

                throw new ArgumentException("Invalid timeseek header");
            }

            // Parses npt times in the format of '10:19:25.7'
            var tokens = value.Split(new[] { ':' }, 3);
            double secondsSum = 0;
            var timeFactor = 3600;

            foreach (var time in tokens)
            {
                double digit;
                if (double.TryParse(time, NumberStyles.Any, UsCulture, out digit))
                {
                    secondsSum += (digit * timeFactor);
                }
                else
                {
                    throw new ArgumentException("Invalid timeseek header");
                }
                timeFactor /= 60;
            }
            return TimeSpan.FromSeconds(secondsSum).Ticks;
        }

        /// <summary>
        /// Gets the state.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>StreamState.</returns>
        protected async Task<StreamState> GetState(StreamRequest request, CancellationToken cancellationToken)
        {
            ParseDlnaHeaders(request);

            if (!string.IsNullOrWhiteSpace(request.Params))
            {
                ParseParams(request);
            }

            var url = Request.PathInfo;

            if (string.IsNullOrEmpty(request.AudioCodec))
            {
                request.AudioCodec = InferAudioCodec(url);
            }

            var state = new StreamState(LiveTvManager, Logger)
            {
                Request = request,
                RequestedUrl = url
            };

            if (!string.IsNullOrWhiteSpace(request.AudioCodec))
            {
                state.SupportedAudioCodecs = request.AudioCodec.Split(',').Where(i => !string.IsNullOrWhiteSpace(i)).ToList();
                state.Request.AudioCodec = state.SupportedAudioCodecs.FirstOrDefault();
            }

            var item = LibraryManager.GetItemById(request.Id);

            List<MediaStream> mediaStreams = null;

            state.ItemType = item.GetType().Name;

            if (item is ILiveTvRecording)
            {
                var recording = await LiveTvManager.GetInternalRecording(request.Id, cancellationToken).ConfigureAwait(false);

                state.VideoType = VideoType.VideoFile;
                state.IsInputVideo = string.Equals(recording.MediaType, MediaType.Video, StringComparison.OrdinalIgnoreCase);

                var path = recording.RecordingInfo.Path;
                var mediaUrl = recording.RecordingInfo.Url;

                var source = string.IsNullOrEmpty(request.MediaSourceId)
                    ? recording.GetMediaSources(false).First()
                    : recording.GetMediaSources(false).First(i => string.Equals(i.Id, request.MediaSourceId));

                mediaStreams = source.MediaStreams;

                // Just to prevent this from being null and causing other methods to fail
                state.MediaPath = string.Empty;

                if (!string.IsNullOrEmpty(path))
                {
                    state.MediaPath = path;
                    state.InputProtocol = MediaProtocol.File;
                }
                else if (!string.IsNullOrEmpty(mediaUrl))
                {
                    state.MediaPath = mediaUrl;
                    state.InputProtocol = MediaProtocol.Http;
                }
                else
                {
                    // No media info, so this is probably needed
                    state.DeInterlace = true;
                }

                if (recording.RecordingInfo.Status == RecordingStatus.InProgress)
                {
                    state.ReadInputAtNativeFramerate = true;
                }

                state.RunTimeTicks = recording.RunTimeTicks;

                state.OutputAudioSync = "1000";
                state.InputVideoSync = "-1";
                state.InputAudioSync = "1";
                state.InputContainer = recording.Container;
            }
            else if (item is LiveTvChannel)
            {
                var channel = LiveTvManager.GetInternalChannel(request.Id);

                state.VideoType = VideoType.VideoFile;
                state.IsInputVideo = string.Equals(channel.MediaType, MediaType.Video, StringComparison.OrdinalIgnoreCase);
                mediaStreams = new List<MediaStream>();

                state.ReadInputAtNativeFramerate = true;
                state.OutputAudioSync = "1000";
                state.DeInterlace = true;
                state.InputVideoSync = "-1";
                state.InputAudioSync = "1";

                // Just to prevent this from being null and causing other methods to fail
                state.MediaPath = string.Empty;
            }
            else if (item is IChannelMediaItem)
            {
                var mediaSource = await GetChannelMediaInfo(request.Id, request.MediaSourceId, cancellationToken).ConfigureAwait(false);
                state.IsInputVideo = string.Equals(item.MediaType, MediaType.Video, StringComparison.OrdinalIgnoreCase);
                state.InputProtocol = mediaSource.Protocol;
                state.MediaPath = mediaSource.Path;
                state.RunTimeTicks = item.RunTimeTicks;
                state.RemoteHttpHeaders = mediaSource.RequiredHttpHeaders;
                mediaStreams = mediaSource.MediaStreams;
            }
            else
            {
                var hasMediaSources = (IHasMediaSources)item;
                var mediaSource = string.IsNullOrEmpty(request.MediaSourceId)
                    ? hasMediaSources.GetMediaSources(false).First()
                    : hasMediaSources.GetMediaSources(false).First(i => string.Equals(i.Id, request.MediaSourceId));

                mediaStreams = mediaSource.MediaStreams;

                state.MediaPath = mediaSource.Path;
                state.InputProtocol = mediaSource.Protocol;
                state.InputContainer = mediaSource.Container;

                if (item is Video)
                {
                    state.IsInputVideo = true;

                    if (mediaSource.VideoType.HasValue)
                    {
                        state.VideoType = mediaSource.VideoType.Value;
                    }

                    state.IsoType = mediaSource.IsoType;

                    state.PlayableStreamFileNames = mediaSource.PlayableStreamFileNames.ToList();

                    if (mediaSource.Timestamp.HasValue)
                    {
                        state.InputTimestamp = mediaSource.Timestamp.Value;
                    }
                }

                state.RunTimeTicks = mediaSource.RunTimeTicks;
            }

            // If it's a wtv and we don't have media info, we will probably need to deinterlace
            if (string.Equals(state.InputContainer, "wtv", StringComparison.OrdinalIgnoreCase) &&
                mediaStreams.Count == 0)
            {
                state.DeInterlace = true;
            }

            if (state.InputProtocol == MediaProtocol.Rtmp)
            {
                state.ReadInputAtNativeFramerate = true;
            }

            var videoRequest = request as VideoStreamRequest;

            AttachMediaStreamInfo(state, mediaStreams, videoRequest, url);

            state.SegmentLength = state.ReadInputAtNativeFramerate ? 5 : 7;
            state.HlsListSize = state.ReadInputAtNativeFramerate ? 100 : 1440;

            var container = Path.GetExtension(state.RequestedUrl);

            if (string.IsNullOrEmpty(container))
            {
                container = request.Static ? state.InputContainer : Path.GetExtension(GetOutputFilePath(state));
            }

            state.OutputContainer = (container ?? string.Empty).TrimStart('.');

            state.OutputAudioBitrate = GetAudioBitrateParam(state.Request, state.AudioStream);
            state.OutputAudioSampleRate = request.AudioSampleRate;

            state.OutputAudioCodec = GetAudioCodec(state.Request);

            state.OutputAudioChannels = GetNumAudioChannelsParam(state.Request, state.AudioStream, state.OutputAudioCodec);

            if (videoRequest != null)
            {
                state.OutputVideoCodec = GetVideoCodec(videoRequest);
                state.OutputVideoBitrate = GetVideoBitrateParamValue(state.VideoRequest, state.VideoStream);
            }

            ApplyDeviceProfileSettings(state);

            if (videoRequest != null)
            {
                if (state.VideoStream != null && CanStreamCopyVideo(videoRequest, state.VideoStream))
                {
                    state.OutputVideoCodec = "copy";
                }

                if (state.AudioStream != null && CanStreamCopyAudio(request, state.AudioStream, state.SupportedAudioCodecs))
                {
                    state.OutputAudioCodec = "copy";
                }
            }

            state.OutputFilePath = GetOutputFilePath(state);

            return state;
        }

        private void AttachMediaStreamInfo(StreamState state,
            List<MediaStream> mediaStreams,
            VideoStreamRequest videoRequest,
            string requestedUrl)
        {
            if (videoRequest != null)
            {
                if (string.IsNullOrEmpty(videoRequest.VideoCodec))
                {
                    videoRequest.VideoCodec = InferVideoCodec(requestedUrl);
                }

                state.VideoStream = GetMediaStream(mediaStreams, videoRequest.VideoStreamIndex, MediaStreamType.Video);
                state.SubtitleStream = GetMediaStream(mediaStreams, videoRequest.SubtitleStreamIndex, MediaStreamType.Subtitle, false);
                state.AudioStream = GetMediaStream(mediaStreams, videoRequest.AudioStreamIndex, MediaStreamType.Audio);

                if (state.SubtitleStream != null && !state.SubtitleStream.IsExternal)
                {
                    state.InternalSubtitleStreamOffset = mediaStreams.Where(i => i.Type == MediaStreamType.Subtitle && !i.IsExternal).ToList().IndexOf(state.SubtitleStream);
                }

                if (state.VideoStream != null && state.VideoStream.IsInterlaced)
                {
                    state.DeInterlace = true;
                }

                EnforceResolutionLimit(state, videoRequest);
            }
            else
            {
                state.AudioStream = GetMediaStream(mediaStreams, null, MediaStreamType.Audio, true);
            }

            state.AllMediaStreams = mediaStreams;
        }

        private async Task<MediaSourceInfo> GetChannelMediaInfo(string id,
            string mediaSourceId,
            CancellationToken cancellationToken)
        {
            var channelMediaSources = await ChannelManager.GetChannelItemMediaSources(id, cancellationToken)
                .ConfigureAwait(false);

            var list = channelMediaSources.ToList();

            if (!string.IsNullOrWhiteSpace(mediaSourceId))
            {
                var source = list
                    .FirstOrDefault(i => string.Equals(mediaSourceId, i.Id));

                if (source != null)
                {
                    return source;
                }
            }

            return list.First();
        }

        private bool CanStreamCopyVideo(VideoStreamRequest request, MediaStream videoStream)
        {
            if (videoStream.IsInterlaced)
            {
                return false;
            }

            // Can't stream copy if we're burning in subtitles
            if (request.SubtitleStreamIndex.HasValue)
            {
                if (request.SubtitleMethod == SubtitleDeliveryMethod.Encode)
                {
                    return false;
                }
            }

            // Source and target codecs must match
            if (!string.Equals(request.VideoCodec, videoStream.Codec, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            // If client is requesting a specific video profile, it must match the source
            if (!string.IsNullOrEmpty(request.Profile) && !string.Equals(request.Profile, videoStream.Profile, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            // Video width must fall within requested value
            if (request.MaxWidth.HasValue)
            {
                if (!videoStream.Width.HasValue || videoStream.Width.Value > request.MaxWidth.Value)
                {
                    return false;
                }
            }

            // Video height must fall within requested value
            if (request.MaxHeight.HasValue)
            {
                if (!videoStream.Height.HasValue || videoStream.Height.Value > request.MaxHeight.Value)
                {
                    return false;
                }
            }

            // Video framerate must fall within requested value
            var requestedFramerate = request.MaxFramerate ?? request.Framerate;
            if (requestedFramerate.HasValue)
            {
                var videoFrameRate = videoStream.AverageFrameRate ?? videoStream.RealFrameRate;

                if (!videoFrameRate.HasValue || videoFrameRate.Value > requestedFramerate.Value)
                {
                    return false;
                }
            }

            // Video bitrate must fall within requested value
            if (request.VideoBitRate.HasValue)
            {
                if (!videoStream.BitRate.HasValue || videoStream.BitRate.Value > request.VideoBitRate.Value)
                {
                    return false;
                }
            }

            // If a specific level was requested, the source must match or be less than
            if (!string.IsNullOrEmpty(request.Level))
            {
                double requestLevel;

                if (double.TryParse(request.Level, NumberStyles.Any, UsCulture, out requestLevel))
                {
                    if (!videoStream.Level.HasValue)
                    {
                        return false;
                    }

                    if (videoStream.Level.Value > requestLevel)
                    {
                        return false;
                    }
                }
            }

            return request.EnableAutoStreamCopy;
        }

        private bool CanStreamCopyAudio(StreamRequest request, MediaStream audioStream, List<string> supportedAudioCodecs)
        {
            // Source and target codecs must match
            if (string.IsNullOrEmpty(audioStream.Codec) || !supportedAudioCodecs.Contains(audioStream.Codec, StringComparer.OrdinalIgnoreCase))
            {
                return false;
            }

            // Video bitrate must fall within requested value
            if (request.AudioBitRate.HasValue)
            {
                if (!audioStream.BitRate.HasValue || audioStream.BitRate.Value <= 0)
                {
                    return false;
                }
                if (audioStream.BitRate.Value > request.AudioBitRate.Value)
                {
                    return false;
                }
            }

            // Channels must fall within requested value
            var channels = request.AudioChannels ?? request.MaxAudioChannels;
            if (channels.HasValue)
            {
                if (!audioStream.Channels.HasValue || audioStream.Channels.Value <= 0)
                {
                    return false;
                }
                if (audioStream.Channels.Value > channels.Value)
                {
                    return false;
                }
            }

            // Sample rate must fall within requested value
            if (request.AudioSampleRate.HasValue)
            {
                if (!audioStream.SampleRate.HasValue || audioStream.SampleRate.Value <= 0)
                {
                    return false;
                }
                if (audioStream.SampleRate.Value > request.AudioSampleRate.Value)
                {
                    return false;
                }
            }

            return true;
        }

        private void ApplyDeviceProfileSettings(StreamState state)
        {
            var headers = new Dictionary<string, string>();
            foreach (var key in Request.Headers.AllKeys)
            {
                headers[key] = Request.Headers[key];
            }

            state.DeviceProfile = string.IsNullOrWhiteSpace(state.Request.DeviceProfileId) ?
                DlnaManager.GetProfile(headers) :
                DlnaManager.GetProfile(state.Request.DeviceProfileId);

            var profile = state.DeviceProfile;

            if (profile == null)
            {
                // Don't use settings from the default profile. 
                // Only use a specific profile if it was requested.
                return;
            }

            var audioCodec = state.OutputAudioCodec;

            if (string.Equals(audioCodec, "copy", StringComparison.OrdinalIgnoreCase) && state.AudioStream != null)
            {
                audioCodec = state.AudioStream.Codec;
            }

            var videoCodec = state.OutputVideoCodec;

            if (string.Equals(videoCodec, "copy", StringComparison.OrdinalIgnoreCase) && state.VideoStream != null)
            {
                videoCodec = state.VideoStream.Codec;
            }

            var mediaProfile = state.VideoRequest == null ?
                profile.GetAudioMediaProfile(state.OutputContainer, audioCodec, state.OutputAudioChannels, state.OutputAudioBitrate) :
                profile.GetVideoMediaProfile(state.OutputContainer,
                audioCodec,
                videoCodec,
                state.OutputAudioBitrate,
                state.OutputAudioChannels,
                state.OutputWidth,
                state.OutputHeight,
                state.TargetVideoBitDepth,
                state.OutputVideoBitrate,
                state.TargetVideoProfile,
                state.TargetVideoLevel,
                state.TargetFramerate,
                state.TargetPacketLength,
                state.TargetTimestamp,
                state.IsTargetAnamorphic);

            if (mediaProfile != null)
            {
                state.MimeType = mediaProfile.MimeType;
            }

            var transcodingProfile = state.VideoRequest == null ?
                profile.GetAudioTranscodingProfile(state.OutputContainer, audioCodec) :
                profile.GetVideoTranscodingProfile(state.OutputContainer, audioCodec, videoCodec);

            if (transcodingProfile != null)
            {
                state.EstimateContentLength = transcodingProfile.EstimateContentLength;
                state.EnableMpegtsM2TsMode = transcodingProfile.EnableMpegtsM2TsMode;
                state.TranscodeSeekInfo = transcodingProfile.TranscodeSeekInfo;

                if (state.VideoRequest != null && string.IsNullOrWhiteSpace(state.VideoRequest.Profile))
                {
                    state.VideoRequest.Profile = transcodingProfile.VideoProfile;
                }
            }
        }

        /// <summary>
        /// Adds the dlna headers.
        /// </summary>
        /// <param name="state">The state.</param>
        /// <param name="responseHeaders">The response headers.</param>
        /// <param name="isStaticallyStreamed">if set to <c>true</c> [is statically streamed].</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        protected void AddDlnaHeaders(StreamState state, IDictionary<string, string> responseHeaders, bool isStaticallyStreamed)
        {
            var profile = state.DeviceProfile;

            var transferMode = GetHeader("transferMode.dlna.org");
            responseHeaders["transferMode.dlna.org"] = string.IsNullOrEmpty(transferMode) ? "Streaming" : transferMode;
            responseHeaders["realTimeInfo.dlna.org"] = "DLNA.ORG_TLAG=*";

            if (state.RunTimeTicks.HasValue && !isStaticallyStreamed && profile != null)
            {
                AddTimeSeekResponseHeaders(state, responseHeaders);
            }

            if (profile == null)
            {
                profile = DlnaManager.GetDefaultProfile();
            }

            var audioCodec = state.OutputAudioCodec;

            if (string.Equals(audioCodec, "copy", StringComparison.OrdinalIgnoreCase) && state.AudioStream != null)
            {
                audioCodec = state.AudioStream.Codec;
            }

            if (state.VideoRequest == null)
            {
                responseHeaders["contentFeatures.dlna.org"] = new ContentFeatureBuilder(profile)
                    .BuildAudioHeader(
                    state.OutputContainer,
                    audioCodec,
                    state.OutputAudioBitrate,
                    state.OutputAudioSampleRate,
                    state.OutputAudioChannels,
                    isStaticallyStreamed,
                    state.RunTimeTicks,
                    state.TranscodeSeekInfo
                    );
            }
            else
            {
                var videoCodec = state.OutputVideoCodec;

                if (string.Equals(videoCodec, "copy", StringComparison.OrdinalIgnoreCase) && state.VideoStream != null)
                {
                    videoCodec = state.VideoStream.Codec;
                }

                responseHeaders["contentFeatures.dlna.org"] = new ContentFeatureBuilder(profile)
                    .BuildVideoHeader(
                    state.OutputContainer,
                    videoCodec,
                    audioCodec,
                    state.OutputWidth,
                    state.OutputHeight,
                    state.TargetVideoBitDepth,
                    state.OutputVideoBitrate,
                    state.OutputAudioBitrate,
                    state.OutputAudioChannels,
                    state.TargetTimestamp,
                    isStaticallyStreamed,
                    state.RunTimeTicks,
                    state.TargetVideoProfile,
                    state.TargetVideoLevel,
                    state.TargetFramerate,
                    state.TargetPacketLength,
                    state.TranscodeSeekInfo,
                    state.IsTargetAnamorphic

                    ).FirstOrDefault() ?? string.Empty;
            }

            foreach (var item in responseHeaders)
            {
                Request.Response.AddHeader(item.Key, item.Value);
            }
        }

        private void AddTimeSeekResponseHeaders(StreamState state, IDictionary<string, string> responseHeaders)
        {
            var runtimeSeconds = TimeSpan.FromTicks(state.RunTimeTicks.Value).TotalSeconds.ToString(UsCulture);
            var startSeconds = TimeSpan.FromTicks(state.Request.StartTimeTicks ?? 0).TotalSeconds.ToString(UsCulture);

            responseHeaders["TimeSeekRange.dlna.org"] = string.Format("npt={0}-{1}/{1}", startSeconds, runtimeSeconds);
            responseHeaders["X-AvailableSeekRange"] = string.Format("1 npt={0}-{1}", startSeconds, runtimeSeconds);
        }

        /// <summary>
        /// Enforces the resolution limit.
        /// </summary>
        /// <param name="state">The state.</param>
        /// <param name="videoRequest">The video request.</param>
        private void EnforceResolutionLimit(StreamState state, VideoStreamRequest videoRequest)
        {
            // Switch the incoming params to be ceilings rather than fixed values
            videoRequest.MaxWidth = videoRequest.MaxWidth ?? videoRequest.Width;
            videoRequest.MaxHeight = videoRequest.MaxHeight ?? videoRequest.Height;

            videoRequest.Width = null;
            videoRequest.Height = null;
        }

        protected string GetInputModifier(StreamState state, bool genPts = true)
        {
            var inputModifier = string.Empty;

            var probeSize = GetProbeSizeArgument(state);
            inputModifier += " " + probeSize;
            inputModifier = inputModifier.Trim();

            var userAgentParam = GetUserAgentParam(state);

            if (!string.IsNullOrWhiteSpace(userAgentParam))
            {
                inputModifier += " " + userAgentParam;
            }

            inputModifier = inputModifier.Trim();

            inputModifier += " " + GetFastSeekCommandLineParameter(state.Request);
            inputModifier = inputModifier.Trim();

            if (state.VideoRequest != null && genPts)
            {
                inputModifier += " -fflags +genpts";
            }

            if (!string.IsNullOrEmpty(state.InputAudioSync))
            {
                inputModifier += " -async " + state.InputAudioSync;
            }

            if (!string.IsNullOrEmpty(state.InputVideoSync))
            {
                inputModifier += " -vsync " + state.InputVideoSync;
            }

            if (state.ReadInputAtNativeFramerate)
            {
                inputModifier += " -re";
            }

            return inputModifier;
        }

        /// <summary>
        /// Infers the audio codec based on the url
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <returns>System.Nullable{AudioCodecs}.</returns>
        private string InferAudioCodec(string url)
        {
            var ext = Path.GetExtension(url);

            if (string.Equals(ext, ".mp3", StringComparison.OrdinalIgnoreCase))
            {
                return "mp3";
            }
            if (string.Equals(ext, ".aac", StringComparison.OrdinalIgnoreCase))
            {
                return "aac";
            }
            if (string.Equals(ext, ".wma", StringComparison.OrdinalIgnoreCase))
            {
                return "wma";
            }
            if (string.Equals(ext, ".ogg", StringComparison.OrdinalIgnoreCase))
            {
                return "vorbis";
            }
            if (string.Equals(ext, ".oga", StringComparison.OrdinalIgnoreCase))
            {
                return "vorbis";
            }
            if (string.Equals(ext, ".ogv", StringComparison.OrdinalIgnoreCase))
            {
                return "vorbis";
            }
            if (string.Equals(ext, ".webm", StringComparison.OrdinalIgnoreCase))
            {
                return "vorbis";
            }
            if (string.Equals(ext, ".webma", StringComparison.OrdinalIgnoreCase))
            {
                return "vorbis";
            }

            return "copy";
        }

        /// <summary>
        /// Infers the video codec.
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <returns>System.Nullable{VideoCodecs}.</returns>
        private string InferVideoCodec(string url)
        {
            var ext = Path.GetExtension(url);

            if (string.Equals(ext, ".asf", StringComparison.OrdinalIgnoreCase))
            {
                return "wmv";
            }
            if (string.Equals(ext, ".webm", StringComparison.OrdinalIgnoreCase))
            {
                return "vpx";
            }
            if (string.Equals(ext, ".ogg", StringComparison.OrdinalIgnoreCase) || string.Equals(ext, ".ogv", StringComparison.OrdinalIgnoreCase))
            {
                return "theora";
            }
            if (string.Equals(ext, ".m3u8", StringComparison.OrdinalIgnoreCase) || string.Equals(ext, ".ts", StringComparison.OrdinalIgnoreCase))
            {
                return "h264";
            }

            return "copy";
        }
    }
}
