﻿using MediaBrowser.Controller.Entities;
using System.Collections.Generic;

namespace MediaBrowser.Controller.Providers
{
    /// <summary>
    /// Marker interface
    /// </summary>
    public interface IMetadataProvider
    {
        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>The name.</value>
        string Name { get; }
    }

    public interface IMetadataProvider<TItemType> : IMetadataProvider
           where TItemType : IHasMetadata
    {
    }

    public interface IHasOrder
    {
        int Order { get; }
    }

    public class MetadataResult<T>
    {
        public bool HasMetadata { get; set; }
        public T Item { get; set; }
    }
}
