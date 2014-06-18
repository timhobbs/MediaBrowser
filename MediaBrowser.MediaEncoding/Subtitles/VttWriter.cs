﻿using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace MediaBrowser.MediaEncoding.Subtitles
{
    public class VttWriter : ISubtitleWriter
    {
        public void Write(SubtitleTrackInfo info, Stream stream, CancellationToken cancellationToken)
        {
            using (var writer = new StreamWriter(stream, Encoding.UTF8, 1024, true))
            {
                writer.WriteLine("WEBVTT");
                writer.WriteLine(string.Empty);
                foreach (var trackEvent in info.TrackEvents)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    writer.WriteLine(@"{0:hh\:mm\:ss\.fff} --> {1:hh\:mm\:ss\.fff}", TimeSpan.FromTicks(trackEvent.StartPositionTicks), TimeSpan.FromTicks(trackEvent.EndPositionTicks));

                    var text = trackEvent.Text;
                    text = Regex.Replace(text, @"\\N", "\r\n", RegexOptions.IgnoreCase);

                    writer.WriteLine(text);
                    writer.WriteLine(string.Empty);
                }
            }
        }
    }
}
