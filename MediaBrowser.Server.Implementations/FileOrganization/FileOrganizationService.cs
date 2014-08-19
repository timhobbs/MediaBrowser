﻿using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.IO;
using MediaBrowser.Common.ScheduledTasks;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.FileOrganization;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.FileOrganization;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Querying;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Server.Implementations.FileOrganization
{
    public class FileOrganizationService : IFileOrganizationService
    {
        private readonly ITaskManager _taskManager;
        private readonly IFileOrganizationRepository _repo;
        private readonly ILogger _logger;
        private readonly ILibraryMonitor _libraryMonitor;
        private readonly ILibraryManager _libraryManager;
        private readonly IServerConfigurationManager _config;
        private readonly IFileSystem _fileSystem;
        private readonly IProviderManager _providerManager;

        public FileOrganizationService(ITaskManager taskManager, IFileOrganizationRepository repo, ILogger logger, ILibraryMonitor libraryMonitor, ILibraryManager libraryManager, IServerConfigurationManager config, IFileSystem fileSystem, IProviderManager providerManager)
        {
            _taskManager = taskManager;
            _repo = repo;
            _logger = logger;
            _libraryMonitor = libraryMonitor;
            _libraryManager = libraryManager;
            _config = config;
            _fileSystem = fileSystem;
            _providerManager = providerManager;
        }

        public void BeginProcessNewFiles()
        {
            _taskManager.CancelIfRunningAndQueue<OrganizerScheduledTask>();
        }

        public Task SaveResult(FileOrganizationResult result, CancellationToken cancellationToken)
        {
            if (result == null || string.IsNullOrEmpty(result.OriginalPath))
            {
                throw new ArgumentNullException("result");
            }

            result.Id = result.OriginalPath.GetMD5().ToString("N");

            return _repo.SaveResult(result, cancellationToken);
        }

        public QueryResult<FileOrganizationResult> GetResults(FileOrganizationResultQuery query)
        {
            return _repo.GetResults(query);
        }

        public FileOrganizationResult GetResult(string id)
        {
            return _repo.GetResult(id);
        }

        public FileOrganizationResult GetResultBySourcePath(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException("path");
            }
            
            var id = path.GetMD5().ToString("N");

            return GetResult(id);
        }

        public Task DeleteOriginalFile(string resultId)
        {
            var result = _repo.GetResult(resultId);

            _logger.Info("Requested to delete {0}", result.OriginalPath);
            try
            {
                File.Delete(result.OriginalPath);
            }
            catch (Exception ex)
            {
                _logger.ErrorException("Error deleting {0}", ex, result.OriginalPath);
            }

            return _repo.Delete(resultId);
        }

        private TvFileOrganizationOptions GetTvOptions()
        {
            return _config.GetAutoOrganizeOptions().TvOptions;
        }

        public async Task PerformOrganization(string resultId)
        {
            var result = _repo.GetResult(resultId);

            if (string.IsNullOrEmpty(result.TargetPath))
            {
                throw new ArgumentException("No target path available.");
            }

            var organizer = new EpisodeFileOrganizer(this, _config, _fileSystem, _logger, _libraryManager,
                _libraryMonitor, _providerManager);

            await organizer.OrganizeEpisodeFile(result.OriginalPath, GetTvOptions(), true, CancellationToken.None)
                    .ConfigureAwait(false);
        }

        public Task ClearLog()
        {
            return _repo.DeleteAll();
        }

        public async Task PerformEpisodeOrganization(EpisodeFileOrganizationRequest request)
        {
            var organizer = new EpisodeFileOrganizer(this, _config, _fileSystem, _logger, _libraryManager,
                _libraryMonitor, _providerManager);

            await organizer.OrganizeWithCorrection(request, GetTvOptions(), CancellationToken.None).ConfigureAwait(false);
        }
    }
}
