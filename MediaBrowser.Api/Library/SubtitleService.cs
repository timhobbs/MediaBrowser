﻿using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Controller.Subtitles;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using ServiceStack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Api.Library
{
    [Route("/Videos/{Id}/{MediaSourceId}/Subtitles/{Index}/Stream.{Format}", "GET", Summary = "Gets subtitles in a specified format (vtt).")]
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
    }

    [Route("/Videos/{Id}/Subtitles/{Index}", "DELETE", Summary = "Deletes an external subtitle file")]
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
    public class SearchRemoteSubtitles : IReturn<List<RemoteSubtitleInfo>>
    {
        [ApiMember(Name = "Id", Description = "Item Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string Id { get; set; }

        [ApiMember(Name = "Language", Description = "Language", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string Language { get; set; }
    }

    [Route("/Items/{Id}/RemoteSearch/Subtitles/Providers", "GET")]
    public class GetSubtitleProviders : IReturn<List<SubtitleProviderInfo>>
    {
        [ApiMember(Name = "Id", Description = "Item Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string Id { get; set; }
    }

    [Route("/Items/{Id}/RemoteSearch/Subtitles/{SubtitleId}", "POST")]
    public class DownloadRemoteSubtitles : IReturnVoid
    {
        [ApiMember(Name = "Id", Description = "Item Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "POST")]
        public string Id { get; set; }

        [ApiMember(Name = "SubtitleId", Description = "SubtitleId", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "POST")]
        public string SubtitleId { get; set; }
    }

    [Route("/Providers/Subtitles/Subtitles/{Id}", "GET")]
    public class GetRemoteSubtitles : IReturnVoid
    {
        [ApiMember(Name = "Id", Description = "Item Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string Id { get; set; }
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

        public object Get(SearchRemoteSubtitles request)
        {
            var video = (Video)_libraryManager.GetItemById(request.Id);

            var response = _subtitleManager.SearchSubtitles(video, request.Language, CancellationToken.None).Result;

            return ToOptimizedResult(response);
        }
        public object Get(GetSubtitle request)
        {
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
                CancellationToken.None).ConfigureAwait(false);
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
