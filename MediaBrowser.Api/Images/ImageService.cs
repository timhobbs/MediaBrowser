﻿using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.IO;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Net;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Drawing;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using ServiceStack;
using ServiceStack.Text.Controller;
using ServiceStack.Web;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Api.Images
{
    /// <summary>
    /// Class GetItemImage
    /// </summary>
    [Route("/Items/{Id}/Images", "GET")]
    [Api(Description = "Gets information about an item's images")]
    [Authenticated]
    public class GetItemImageInfos : IReturn<List<ImageInfo>>
    {
        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <value>The id.</value>
        [ApiMember(Name = "Id", Description = "Item Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string Id { get; set; }
    }

    [Route("/Items/{Id}/Images/{Type}", "GET")]
    [Route("/Items/{Id}/Images/{Type}/{Index}", "GET")]
    [Route("/Items/{Id}/Images/{Type}/{Index}/{Tag}/{Format}/{MaxWidth}/{MaxHeight}/{PercentPlayed}", "GET")]
    [Route("/Items/{Id}/Images/{Type}/{Index}/{Tag}/{Format}/{MaxWidth}/{MaxHeight}/{PercentPlayed}", "HEAD")]
    [Api(Description = "Gets an item image")]
    public class GetItemImage : ImageRequest
    {
        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <value>The id.</value>
        [ApiMember(Name = "Id", Description = "Item Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string Id { get; set; }
    }

    /// <summary>
    /// Class UpdateItemImageIndex
    /// </summary>
    [Route("/Items/{Id}/Images/{Type}/{Index}/Index", "POST")]
    [Api(Description = "Updates the index for an item image")]
    [Authenticated]
    public class UpdateItemImageIndex : IReturnVoid
    {
        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <value>The id.</value>
        [ApiMember(Name = "Id", Description = "Item Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the type of the image.
        /// </summary>
        /// <value>The type of the image.</value>
        [ApiMember(Name = "Type", Description = "Image Type", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "POST")]
        public ImageType Type { get; set; }

        /// <summary>
        /// Gets or sets the index.
        /// </summary>
        /// <value>The index.</value>
        [ApiMember(Name = "Index", Description = "Image Index", IsRequired = false, DataType = "int", ParameterType = "query", Verb = "POST")]
        public int Index { get; set; }

        /// <summary>
        /// Gets or sets the new index.
        /// </summary>
        /// <value>The new index.</value>
        [ApiMember(Name = "NewIndex", Description = "The new image index", IsRequired = true, DataType = "string", ParameterType = "query", Verb = "GET")]
        public int NewIndex { get; set; }
    }

    /// <summary>
    /// Class GetPersonImage
    /// </summary>
    [Route("/Artists/{Name}/Images/{Type}", "GET")]
    [Route("/Artists/{Name}/Images/{Type}/{Index}", "GET")]
    [Route("/Genres/{Name}/Images/{Type}", "GET")]
    [Route("/Genres/{Name}/Images/{Type}/{Index}", "GET")]
    [Route("/GameGenres/{Name}/Images/{Type}", "GET")]
    [Route("/GameGenres/{Name}/Images/{Type}/{Index}", "GET")]
    [Route("/MusicGenres/{Name}/Images/{Type}", "GET")]
    [Route("/MusicGenres/{Name}/Images/{Type}/{Index}", "GET")]
    [Route("/Persons/{Name}/Images/{Type}", "GET")]
    [Route("/Persons/{Name}/Images/{Type}/{Index}", "GET")]
    [Route("/Studios/{Name}/Images/{Type}", "GET")]
    [Route("/Studios/{Name}/Images/{Type}/{Index}", "GET")]
    [Route("/Years/{Year}/Images/{Type}", "GET")]
    [Route("/Years/{Year}/Images/{Type}/{Index}", "GET")]
    [Api(Description = "Gets an item by name image")]
    public class GetItemByNameImage : ImageRequest
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        [ApiMember(Name = "Name", Description = "Item name", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string Name { get; set; }
    }

    /// <summary>
    /// Class GetUserImage
    /// </summary>
    [Route("/Users/{Id}/Images/{Type}", "GET")]
    [Route("/Users/{Id}/Images/{Type}/{Index}", "GET")]
    [Api(Description = "Gets a user image")]
    public class GetUserImage : ImageRequest
    {
        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <value>The id.</value>
        [ApiMember(Name = "Id", Description = "User Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public Guid Id { get; set; }
    }

    /// <summary>
    /// Class DeleteItemImage
    /// </summary>
    [Route("/Items/{Id}/Images/{Type}", "DELETE")]
    [Route("/Items/{Id}/Images/{Type}/{Index}", "DELETE")]
    [Api(Description = "Deletes an item image")]
    [Authenticated]
    public class DeleteItemImage : DeleteImageRequest, IReturnVoid
    {
        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <value>The id.</value>
        [ApiMember(Name = "Id", Description = "Item Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "DELETE")]
        public Guid Id { get; set; }
    }

    /// <summary>
    /// Class DeleteUserImage
    /// </summary>
    [Route("/Users/{Id}/Images/{Type}", "DELETE")]
    [Route("/Users/{Id}/Images/{Type}/{Index}", "DELETE")]
    [Api(Description = "Deletes a user image")]
    [Authenticated]
    public class DeleteUserImage : DeleteImageRequest, IReturnVoid
    {
        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <value>The id.</value>
        [ApiMember(Name = "Id", Description = "User Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "DELETE")]
        public Guid Id { get; set; }
    }

    /// <summary>
    /// Class PostUserImage
    /// </summary>
    [Route("/Users/{Id}/Images/{Type}", "POST")]
    [Route("/Users/{Id}/Images/{Type}/{Index}", "POST")]
    [Api(Description = "Posts a user image")]
    [Authenticated]
    public class PostUserImage : DeleteImageRequest, IRequiresRequestStream, IReturnVoid
    {
        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <value>The id.</value>
        [ApiMember(Name = "Id", Description = "User Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "POST")]
        public Guid Id { get; set; }

        /// <summary>
        /// The raw Http Request Input Stream
        /// </summary>
        /// <value>The request stream.</value>
        public Stream RequestStream { get; set; }
    }

    /// <summary>
    /// Class PostItemImage
    /// </summary>
    [Route("/Items/{Id}/Images/{Type}", "POST")]
    [Route("/Items/{Id}/Images/{Type}/{Index}", "POST")]
    [Api(Description = "Posts an item image")]
    [Authenticated]
    public class PostItemImage : DeleteImageRequest, IRequiresRequestStream, IReturnVoid
    {
        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <value>The id.</value>
        [ApiMember(Name = "Id", Description = "Item Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "POST")]
        public string Id { get; set; }

        /// <summary>
        /// The raw Http Request Input Stream
        /// </summary>
        /// <value>The request stream.</value>
        public Stream RequestStream { get; set; }
    }

    /// <summary>
    /// Class ImageService
    /// </summary>
    public class ImageService : BaseApiService
    {
        private readonly IUserManager _userManager;

        private readonly ILibraryManager _libraryManager;

        private readonly IProviderManager _providerManager;

        private readonly IItemRepository _itemRepo;
        private readonly IImageProcessor _imageProcessor;
        private readonly IFileSystem _fileSystem;

        /// <summary>
        /// Initializes a new instance of the <see cref="ImageService" /> class.
        /// </summary>
        public ImageService(IUserManager userManager, ILibraryManager libraryManager, IProviderManager providerManager, IItemRepository itemRepo, IImageProcessor imageProcessor, IFileSystem fileSystem)
        {
            _userManager = userManager;
            _libraryManager = libraryManager;
            _providerManager = providerManager;
            _itemRepo = itemRepo;
            _imageProcessor = imageProcessor;
            _fileSystem = fileSystem;
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public object Get(GetItemImageInfos request)
        {
            var item = _libraryManager.GetItemById(request.Id);

            var result = GetItemImageInfos(item);

            return ToOptimizedSerializedResultUsingCache(result);
        }

        /// <summary>
        /// Gets the item image infos.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>Task{List{ImageInfo}}.</returns>
        public List<ImageInfo> GetItemImageInfos(BaseItem item)
        {
            var list = new List<ImageInfo>();

            foreach (var image in item.ImageInfos.Where(i => !item.AllowsMultipleImages(i.Type)))
            {
                var info = GetImageInfo(item, image, null);

                if (info != null)
                {
                    list.Add(info);
                }
            }

            foreach (var imageType in item.ImageInfos.Select(i => i.Type).Distinct().Where(item.AllowsMultipleImages))
            {
                var index = 0;

                // Prevent implicitly captured closure
                var currentImageType = imageType;

                foreach (var image in item.ImageInfos.Where(i => i.Type == currentImageType))
                {
                    var info = GetImageInfo(item, image, index);

                    if (info != null)
                    {
                        list.Add(info);
                    }

                    index++;
                }
            }

            var video = item as Video;

            if (video != null)
            {
                var index = 0;

                foreach (var chapter in _itemRepo.GetChapters(video.Id))
                {
                    if (!string.IsNullOrEmpty(chapter.ImagePath))
                    {
                        var image = chapter.ImagePath;

                        var info = GetImageInfo(item, new ItemImageInfo
                        {
                            Path = image,
                            Type = ImageType.Chapter,
                            DateModified = _fileSystem.GetLastWriteTimeUtc(image)

                        }, index);

                        if (info != null)
                        {
                            list.Add(info);
                        }
                    }

                    index++;
                }
            }

            return list;
        }

        private ImageInfo GetImageInfo(IHasImages item, ItemImageInfo info, int? imageIndex)
        {
            try
            {
                var fileInfo = new FileInfo(info.Path);

                var size = _imageProcessor.GetImageSize(info.Path);

                return new ImageInfo
                {
                    Path = info.Path,
                    ImageIndex = imageIndex,
                    ImageType = info.Type,
                    ImageTag = _imageProcessor.GetImageCacheTag(item, info),
                    Size = fileInfo.Length,
                    Width = Convert.ToInt32(size.Width),
                    Height = Convert.ToInt32(size.Height)
                };
            }
            catch (Exception ex)
            {
                Logger.ErrorException("Error getting image information for {0}", ex, info.Path);

                return null;
            }
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public object Get(GetItemImage request)
        {
            var item = string.IsNullOrEmpty(request.Id) ?
                _libraryManager.RootFolder :
                _libraryManager.GetItemById(request.Id);

            return GetImage(request, item, false);
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public object Head(GetItemImage request)
        {
            var item = string.IsNullOrEmpty(request.Id) ?
                _libraryManager.RootFolder :
                _libraryManager.GetItemById(request.Id);

            return GetImage(request, item, true);
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public object Get(GetUserImage request)
        {
            var item = _userManager.Users.First(i => i.Id == request.Id);

            return GetImage(request, item, false);
        }

        public object Get(GetItemByNameImage request)
        {
            var pathInfo = PathInfo.Parse(Request.PathInfo);
            var type = pathInfo.GetArgumentValue<string>(0);

            var item = GetItemByName(request.Name, type, _libraryManager);

            return GetImage(request, item, false);
        }

        /// <summary>
        /// Posts the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        public void Post(PostUserImage request)
        {
            var pathInfo = PathInfo.Parse(Request.PathInfo);
            var id = new Guid(pathInfo.GetArgumentValue<string>(1));

            request.Type = (ImageType)Enum.Parse(typeof(ImageType), pathInfo.GetArgumentValue<string>(3), true);

            var item = _userManager.Users.First(i => i.Id == id);

            var task = PostImage(item, request.RequestStream, request.Type, Request.ContentType);

            Task.WaitAll(task);
        }

        /// <summary>
        /// Posts the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        public void Post(PostItemImage request)
        {
            var pathInfo = PathInfo.Parse(Request.PathInfo);
            var id = new Guid(pathInfo.GetArgumentValue<string>(1));

            request.Type = (ImageType)Enum.Parse(typeof(ImageType), pathInfo.GetArgumentValue<string>(3), true);

            var item = _libraryManager.GetItemById(id);

            var task = PostImage(item, request.RequestStream, request.Type, Request.ContentType);

            Task.WaitAll(task);
        }

        /// <summary>
        /// Deletes the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        public void Delete(DeleteUserImage request)
        {
            var item = _userManager.Users.First(i => i.Id == request.Id);

            var task = item.DeleteImage(request.Type, request.Index ?? 0);

            Task.WaitAll(task);
        }

        /// <summary>
        /// Deletes the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        public void Delete(DeleteItemImage request)
        {
            var item = _libraryManager.GetItemById(request.Id);

            var task = item.DeleteImage(request.Type, request.Index ?? 0);

            Task.WaitAll(task);
        }

        /// <summary>
        /// Posts the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        public void Post(UpdateItemImageIndex request)
        {
            var item = _libraryManager.GetItemById(request.Id);

            var task = UpdateItemIndex(item, request.Type, request.Index, request.NewIndex);

            Task.WaitAll(task);
        }

        /// <summary>
        /// Updates the index of the item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="type">The type.</param>
        /// <param name="currentIndex">Index of the current.</param>
        /// <param name="newIndex">The new index.</param>
        /// <returns>Task.</returns>
        /// <exception cref="System.ArgumentException">The change index operation is only applicable to backdrops and screenshots</exception>
        private Task UpdateItemIndex(IHasImages item, ImageType type, int currentIndex, int newIndex)
        {
            return item.SwapImages(type, currentIndex, newIndex);
        }

        /// <summary>
        /// Gets the image.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="item">The item.</param>
        /// <param name="isHeadRequest">if set to <c>true</c> [is head request].</param>
        /// <returns>System.Object.</returns>
        /// <exception cref="ResourceNotFoundException"></exception>
        public object GetImage(ImageRequest request, IHasImages item, bool isHeadRequest)
        {
            var imageInfo = GetImageInfo(request, item);

            if (imageInfo == null)
            {
                throw new ResourceNotFoundException(string.Format("{0} does not have an image of type {1}", item.Name, request.Type));
            }

            var supportedImageEnhancers = request.EnableImageEnhancers ? _imageProcessor.ImageEnhancers.Where(i =>
            {
                try
                {
                    return i.Supports(item, request.Type);
                }
                catch (Exception ex)
                {
                    Logger.ErrorException("Error in image enhancer: {0}", ex, i.GetType().Name);

                    return false;
                }

            }).ToList() : new List<IImageEnhancer>();

            var contentType = GetMimeType(request.Format, imageInfo.Path);

            var cacheGuid = new Guid(_imageProcessor.GetImageCacheTag(item, imageInfo, supportedImageEnhancers));

            TimeSpan? cacheDuration = null;

            if (!string.IsNullOrEmpty(request.Tag) && cacheGuid == new Guid(request.Tag))
            {
                cacheDuration = TimeSpan.FromDays(365);
            }

            var responseHeaders = new Dictionary<string, string>
            {
                {"transferMode.dlna.org", "Interactive"},
                {"realTimeInfo.dlna.org", "DLNA.ORG_TLAG=*"}
            };

            return GetImageResult(item,
                request,
                imageInfo,
                supportedImageEnhancers,
                contentType,
                cacheDuration,
                responseHeaders,
                isHeadRequest)
                .Result;
        }

        private async Task<object> GetImageResult(IHasImages item,
            ImageRequest request,
            ItemImageInfo image,
            List<IImageEnhancer> enhancers,
            string contentType,
            TimeSpan? cacheDuration,
            IDictionary<string, string> headers,
            bool isHeadRequest)
        {
            var cropwhitespace = request.Type == ImageType.Logo || request.Type == ImageType.Art;

            if (request.CropWhitespace.HasValue)
            {
                cropwhitespace = request.CropWhitespace.Value;
            }

            var options = new ImageProcessingOptions
            {
                CropWhiteSpace = cropwhitespace,
                Enhancers = enhancers,
                Height = request.Height,
                ImageIndex = request.Index ?? 0,
                Image = image,
                Item = item,
                MaxHeight = request.MaxHeight,
                MaxWidth = request.MaxWidth,
                Quality = request.Quality,
                Width = request.Width,
                OutputFormat = request.Format,
                AddPlayedIndicator = request.AddPlayedIndicator,
                PercentPlayed = request.PercentPlayed ?? 0,
                UnplayedCount = request.UnplayedCount,
                BackgroundColor = request.BackgroundColor
            };

            var file = await _imageProcessor.ProcessImage(options).ConfigureAwait(false);

            return ResultFactory.GetStaticFileResult(Request, new StaticFileResultOptions
            {
                CacheDuration = cacheDuration,
                ResponseHeaders = headers,
                ContentType = contentType,
                IsHeadRequest = isHeadRequest,
                Path = file
            });
        }

        private string GetMimeType(ImageOutputFormat format, string path)
        {
            if (format == ImageOutputFormat.Bmp)
            {
                return Common.Net.MimeTypes.GetMimeType("i.bmp");
            }
            if (format == ImageOutputFormat.Gif)
            {
                return Common.Net.MimeTypes.GetMimeType("i.gif");
            }
            if (format == ImageOutputFormat.Jpg)
            {
                return Common.Net.MimeTypes.GetMimeType("i.jpg");
            }
            if (format == ImageOutputFormat.Png)
            {
                return Common.Net.MimeTypes.GetMimeType("i.png");
            }
            if (format == ImageOutputFormat.Webp)
            {
                return Common.Net.MimeTypes.GetMimeType("i.webp");
            }

            return Common.Net.MimeTypes.GetMimeType(path);
        }

        /// <summary>
        /// Gets the image path.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="item">The item.</param>
        /// <returns>System.String.</returns>
        private ItemImageInfo GetImageInfo(ImageRequest request, IHasImages item)
        {
            var index = request.Index ?? 0;

            return item.GetImageInfo(request.Type, index);
        }

        /// <summary>
        /// Posts the image.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <param name="inputStream">The input stream.</param>
        /// <param name="imageType">Type of the image.</param>
        /// <param name="mimeType">Type of the MIME.</param>
        /// <returns>Task.</returns>
        public async Task PostImage(BaseItem entity, Stream inputStream, ImageType imageType, string mimeType)
        {
            using (var reader = new StreamReader(inputStream))
            {
                var text = await reader.ReadToEndAsync().ConfigureAwait(false);

                var bytes = Convert.FromBase64String(text);

                var memoryStream = new MemoryStream(bytes)
                {
                    Position = 0
                };

                // Handle image/png; charset=utf-8
                mimeType = mimeType.Split(';').FirstOrDefault();

                await _providerManager.SaveImage(entity, memoryStream, mimeType, imageType, null, CancellationToken.None).ConfigureAwait(false);

                await entity.UpdateToRepository(ItemUpdateType.ImageUpdate, CancellationToken.None).ConfigureAwait(false);
            }
        }
    }
}
