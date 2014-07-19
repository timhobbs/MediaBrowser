﻿using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MediaBrowser.LocalMetadata.Images
{
    public class EpisodeLocalLocalImageProvider : ILocalImageFileProvider
    {
        public string Name
        {
            get { return "Local Images"; }
        }

        public bool Supports(IHasImages item)
        {
            return item is Episode && item.SupportsLocalMetadata;
        }

        public List<LocalImageInfo> GetImages(IHasImages item, IDirectoryService directoryService)
        {
            var parentPath = Path.GetDirectoryName(item.Path);

            var parentPathFiles = directoryService.GetFileSystemEntries(parentPath);

            var nameWithoutExtension = Path.GetFileNameWithoutExtension(item.Path);

            var files = GetFilesFromParentFolder(nameWithoutExtension, parentPathFiles);

            if (files.Count > 0)
            {
                return files;
            }

            var metadataPath = Path.Combine(parentPath, "metadata");

            if (parentPathFiles.Any(i => string.Equals(i.FullName, metadataPath, StringComparison.OrdinalIgnoreCase)))
            {
                return GetFilesFromParentFolder(nameWithoutExtension, directoryService.GetFiles(metadataPath));
            }

            return new List<LocalImageInfo>();
        }

        private List<LocalImageInfo> GetFilesFromParentFolder(string filenameWithoutExtension, IEnumerable<FileSystemInfo> parentPathFiles)
        {
            var thumbName = filenameWithoutExtension + "-thumb";

            return parentPathFiles
              .Where(i =>
              {
                  if ((i.Attributes & FileAttributes.Directory) == FileAttributes.Directory)
                  {
                      return false;
                  }
                  
                  if (BaseItem.SupportedImageExtensions.Contains(i.Extension, StringComparer.OrdinalIgnoreCase))
                  {
                      var currentNameWithoutExtension = Path.GetFileNameWithoutExtension(i.Name);

                      if (string.Equals(filenameWithoutExtension, currentNameWithoutExtension, StringComparison.OrdinalIgnoreCase))
                      {
                          return true;
                      }

                      if (string.Equals(thumbName, currentNameWithoutExtension, StringComparison.OrdinalIgnoreCase))
                      {
                          return true;
                      }
                  }

                  return false;
              })
              .Select(i => new LocalImageInfo
              {
                  FileInfo = (FileInfo)i,
                  Type = ImageType.Primary
              })
              .ToList();
        }
    }
}
