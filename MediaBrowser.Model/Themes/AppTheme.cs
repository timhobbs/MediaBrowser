﻿using System.Collections.Generic;

namespace MediaBrowser.Model.Themes
{
    public class AppTheme
    {
        public string AppName { get; set; }

        public string Name { get; set; }

        public Dictionary<string, string> Options { get; set; }

        public List<ThemeImage> Images { get; set; }

        public AppTheme()
        {
            Options = new Dictionary<string, string>();

            Images = new List<ThemeImage>();
        }
    }
}
