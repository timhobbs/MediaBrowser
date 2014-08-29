﻿using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Controller.Net;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Controller.Subtitles;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using ServiceStack;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Api.Subtitles
{
    [Route("/Videos/{Id}/Subtitles/{Index}", "DELETE", Summary = "Deletes an external subtitle file")]
    [Authenticated]
    public class DeleteSubtitle
    {
        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <value>The id.</value>
        [ApiMember(Name = "Id", Description = "Item Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "DELETE")]
        public string Id { get; set; }

        [ApiMember(Name = "Index", Description = "The subtitle stream index", IsRequired = true, DataType = "int", ParameterType = "path", Verb = "DELETE")]
        public int Index { get; set; }
    }

    [Route("/Items/{Id}/RemoteSearch/Subtitles/{Language}", "GET")]
    [Authenticated]
    public class SearchRemoteSubtitles : IReturn<List<RemoteSubtitleInfo>>
    {
        [ApiMember(Name = "Id", Description = "Item Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string Id { get; set; }

        [ApiMember(Name = "Language", Description = "Language", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string Language { get; set; }
    }

    [Route("/Items/{Id}/RemoteSearch/Subtitles/Providers", "GET")]
    [Authenticated]
    public class GetSubtitleProviders : IReturn<List<SubtitleProviderInfo>>
    {
        [ApiMember(Name = "Id", Description = "Item Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string Id { get; set; }
    }

    [Route("/Items/{Id}/RemoteSearch/Subtitles/{SubtitleId}", "POST")]
    [Authenticated]
    public class DownloadRemoteSubtitles : IReturnVoid
    {
        [ApiMember(Name = "Id", Description = "Item Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "POST")]
        public string Id { get; set; }

        [ApiMember(Name = "SubtitleId", Description = "SubtitleId", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "POST")]
        public string SubtitleId { get; set; }
    }

    [Route("/Providers/Subtitles/Subtitles/{Id}", "GET")]
    [Authenticated]
    public class GetRemoteSubtitles : IReturnVoid
    {
        [ApiMember(Name = "Id", Description = "Item Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string Id { get; set; }
    }

    [Route("/Videos/{Id}/{MediaSourceId}/Subtitles/{Index}/Stream.{Format}", "GET", Summary = "Gets subtitles in a specified format.")]
    [Route("/Videos/{Id}/{MediaSourceId}/Subtitles/{Index}/{StartPositionTicks}/Stream.{Format}", "GET", Summary = "Gets subtitles in a specified format.")]
    public class GetSubtitle
    {
        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <value>The id.</value>
        [ApiMember(Name = "Id", Description = "Item Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string Id { get; set; }

        [ApiMember(Name = "MediaSourceId", Description = "MediaSourceId", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string MediaSourceId { get; set; }

        [ApiMember(Name = "Index", Description = "The subtitle stream index", IsRequired = true, DataType = "int", ParameterType = "path", Verb = "GET")]
        public int Index { get; set; }

        [ApiMember(Name = "Format", Description = "Format", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string Format { get; set; }

        [ApiMember(Name = "StartPositionTicks", Description = "StartPositionTicks", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public long StartPositionTicks { get; set; }

        [ApiMember(Name = "EndPositionTicks", Description = "EndPositionTicks", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public long? EndPositionTicks { get; set; }
    }

    [Route("/Videos/{Id}/{MediaSourceId}/Subtitles/{Index}/subtitles.m3u8", "GET", Summary = "Gets an HLS subtitle playlist.")]
    public class GetSubtitlePlaylist
    {
        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <value>The id.</value>
        [ApiMember(Name = "Id", Description = "Item Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string Id { get; set; }

        [ApiMember(Name = "MediaSourceId", Description = "MediaSourceId", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string MediaSourceId { get; set; }

        [ApiMember(Name = "Index", Description = "The subtitle stream index", IsRequired = true, DataType = "int", ParameterType = "path", Verb = "GET")]
        public int Index { get; set; }

        [ApiMember(Name = "SegmentLength", Description = "The subtitle srgment length", IsRequired = true, DataType = "int", ParameterType = "path", Verb = "GET")]
        public int SegmentLength { get; set; }
    }

    public class SubtitleService : BaseApiService
    {
        private readonly ILibraryManager _libraryManager;
        private readonly ISubtitleManager _subtitleManager;
        private readonly ISubtitleEncoder _subtitleEncoder;

        public SubtitleService(ILibraryManager libraryManager, ISubtitleManager subtitleManager, ISubtitleEncoder subtitleEncoder)
        {
            _libraryManager = libraryManager;
            _subtitleManager = subtitleManager;
            _subtitleEncoder = subtitleEncoder;
        }

        public object Get(GetSubtitlePlaylist request)
        {
            var item = (Video)_libraryManager.GetItemById(new Guid(request.Id));

            var mediaSource = item.GetMediaSources(false)
                .First(i => string.Equals(i.Id, request.MediaSourceId ?? request.Id));

            var builder = new StringBuilder();

            var runtime = mediaSource.RunTimeTicks ?? -1;

            if (runtime <= 0)
            {
                throw new ArgumentException("HLS Subtitles are not supported for this media.");
            }

            builder.AppendLine("#EXTM3U");
            builder.AppendLine("#EXT-X-TARGETDURATION:" + request.SegmentLength.ToString(CultureInfo.InvariantCulture));
            builder.AppendLine("#EXT-X-VERSION:3");
            builder.AppendLine("#EXT-X-MEDIA-SEQUENCE:0");

            long positionTicks = 0;
            var segmentLengthTicks = TimeSpan.FromSeconds(request.SegmentLength).Ticks;

            while (positionTicks < runtime)
            {
                var remaining = runtime - positionTicks;
                var lengthTicks = Math.Min(remaining, segmentLengthTicks);

                builder.AppendLine("#EXTINF:" + TimeSpan.FromTicks(lengthTicks).TotalSeconds.ToString(CultureInfo.InvariantCulture));

                var endPositionTicks = Math.Min(runtime, positionTicks + segmentLengthTicks);

                var url = string.Format("stream.srt?StartPositionTicks={0}&EndPositionTicks={1}",
                    positionTicks.ToString(CultureInfo.InvariantCulture),
                    endPositionTicks.ToString(CultureInfo.InvariantCulture));

                builder.AppendLine(url);

                positionTicks += segmentLengthTicks;
            }

            builder.AppendLine("#EXT-X-ENDLIST");

            return ResultFactory.GetResult(builder.ToString(), Common.Net.MimeTypes.GetMimeType("playlist.m3u8"), new Dictionary<string, string>());
        }

        public object Get(GetSubtitle request)
        {
            if (string.Equals(request.Format, "js", StringComparison.OrdinalIgnoreCase))
            {
                request.Format = "json";
            }
            if (string.IsNullOrEmpty(request.Format))
            {
                var item = (Video)_libraryManager.GetItemById(new Guid(request.Id));

                var mediaSource = item.GetMediaSources(false)
                    .First(i => string.Equals(i.Id, request.MediaSourceId ?? request.Id));

                var subtitleStream = mediaSource.MediaStreams
                    .First(i => i.Type == MediaStreamType.Subtitle && i.Index == request.Index);

                return ToStaticFileResult(subtitleStream.Path);
            }

            var stream = GetSubtitles(request).Result;

            return ResultFactory.GetResult(stream, Common.Net.MimeTypes.GetMimeType("file." + request.Format));
        }

        private async Task<Stream> GetSubtitles(GetSubtitle request)
        {
            return await _subtitleEncoder.GetSubtitles(request.Id,
                request.MediaSourceId,
                request.Index,
                request.Format,
                request.StartPositionTicks,
                request.EndPositionTicks,
                CancellationToken.None).ConfigureAwait(false);
        }

        public object Get(SearchRemoteSubtitles request)
        {
            var video = (Video)_libraryManager.GetItemById(request.Id);

            var response = _subtitleManager.SearchSubtitles(video, request.Language, CancellationToken.None).Result;

            return ToOptimizedResult(response);
        }

        public void Delete(DeleteSubtitle request)
        {
            var task = _subtitleManager.DeleteSubtitles(request.Id, request.Index);

            Task.WaitAll(task);
        }

        public object Get(GetSubtitleProviders request)
        {
            var result = _subtitleManager.GetProviders(request.Id);

            return ToOptimizedResult(result);
        }

        public object Get(GetRemoteSubtitles request)
        {
            var result = _subtitleManager.GetRemoteSubtitles(request.Id, CancellationToken.None).Result;

            return ResultFactory.GetResult(result.Stream, Common.Net.MimeTypes.GetMimeType("file." + result.Format));
        }

        public void Post(DownloadRemoteSubtitles request)
        {
            var video = (Video)_libraryManager.GetItemById(request.Id);

            Task.Run(async () =>
            {
                try
                {
                    await _subtitleManager.DownloadSubtitles(video, request.SubtitleId, CancellationToken.None)
                        .ConfigureAwait(false);

                    await video.RefreshMetadata(new MetadataRefreshOptions(), CancellationToken.None).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Logger.ErrorException("Error downloading subtitles", ex);
                }

            });
        }
    }
}
