﻿using System;
using System.Collections.Generic;

namespace MediaBrowser.Model.Session
{
    public class GeneralCommand
    {
        public string Name { get; set; }

        public string ControllingUserId { get; set; }

        public Dictionary<string, string> Arguments { get; set; }

        public GeneralCommand()
        {
            Arguments = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
    }

    /// <summary>
    /// This exists simply to identify a set of known commands.
    /// </summary>
    public enum GeneralCommandType
    {
        MoveUp = 0,
        MoveDown = 1,
        MoveLeft = 2,
        MoveRight = 3,
        PageUp = 4,
        PageDown = 5,
        PreviousLetter = 6,
        NextLetter = 7,
        ToggleOsd = 8,
        ToggleContextMenu = 9,
        Select = 10,
        Back = 11,
        TakeScreenshot = 12,
        SendKey = 13,
        SendString = 14,
        GoHome = 15,
        GoToSettings = 16,
        VolumeUp = 17,
        VolumeDown = 18,
        Mute = 19,
        Unmute = 20,
        ToggleMute = 21,
        SetVolume = 22,
        SetAudioStreamIndex = 23,
        SetSubtitleStreamIndex = 24,
        ToggleFullscreen = 25,
        DisplayContent = 26,
        GoToSearch = 27
    }
}
