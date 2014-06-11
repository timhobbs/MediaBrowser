﻿using MediaBrowser.Model.Entities;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Controller.MediaEncoding
{
    /// <summary>
    /// Interface IMediaEncoder
    /// </summary>
    public interface IMediaEncoder
    {
        /// <summary>
        /// Gets the encoder path.
        /// </summary>
        /// <value>The encoder path.</value>
        string EncoderPath { get; }

        /// <summary>
        /// Gets the version.
        /// </summary>
        /// <value>The version.</value>
        string Version { get; }

        /// <summary>
        /// Extracts the audio image.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{Stream}.</returns>
        Task<Stream> ExtractAudioImage(string path, CancellationToken cancellationToken);

        /// <summary>
        /// Extracts the video image.
        /// </summary>
        /// <param name="inputFiles">The input files.</param>
        /// <param name="type">The type.</param>
        /// <param name="threedFormat">The threed format.</param>
        /// <param name="offset">The offset.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{Stream}.</returns>
        Task<Stream> ExtractVideoImage(string[] inputFiles, InputType type, Video3DFormat? threedFormat, TimeSpan? offset, CancellationToken cancellationToken);
        
        /// <summary>
        /// Extracts the text subtitle.
        /// </summary>
        /// <param name="inputFiles">The input files.</param>
        /// <param name="type">The type.</param>
        /// <param name="subtitleStreamIndex">Index of the subtitle stream.</param>
        /// <param name="copySubtitleStream">if set to true, copy stream instead of converting.</param>
        /// <param name="outputPath">The output path.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        Task ExtractTextSubtitle(string[] inputFiles, InputType type, int subtitleStreamIndex, bool copySubtitleStream, string outputPath, CancellationToken cancellationToken);

        /// <summary>
        /// Gets the subtitle language encoding parameter.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="language">The language.</param>
        /// <returns>System.String.</returns>
        string GetSubtitleLanguageEncodingParam(string path, string language);

        /// <summary>
        /// Gets the media info.
        /// </summary>
        /// <param name="inputFiles">The input files.</param>
        /// <param name="type">The type.</param>
        /// <param name="isAudio">if set to <c>true</c> [is audio].</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        Task<InternalMediaInfoResult> GetMediaInfo(string[] inputFiles, InputType type, bool isAudio, CancellationToken cancellationToken);

        /// <summary>
        /// Gets the probe size argument.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>System.String.</returns>
        string GetProbeSizeArgument(InputType type);

        /// <summary>
        /// Gets the input argument.
        /// </summary>
        /// <param name="inputFiles">The input files.</param>
        /// <param name="type">The type.</param>
        /// <returns>System.String.</returns>
        string GetInputArgument(string[] inputFiles, InputType type);
    }

    /// <summary>
    /// Enum InputType
    /// </summary>
    public enum InputType
    {
        /// <summary>
        /// The file
        /// </summary>
        File,
        /// <summary>
        /// The bluray
        /// </summary>
        Bluray,
        /// <summary>
        /// The DVD
        /// </summary>
        Dvd,
        /// <summary>
        /// The URL
        /// </summary>
        Url
    }
}
