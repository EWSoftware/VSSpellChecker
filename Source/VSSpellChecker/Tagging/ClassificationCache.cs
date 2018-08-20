//===============================================================================================================
// System  : Visual Studio Spell Checker Package
// File    : ClassificationCache.cs
// Author  : Eric Woodruff  (Eric@EWoodruff.us)
// Updated : 08/17/2018
// Note    : Copyright 2018, Eric Woodruff, All rights reserved
// Compiler: Microsoft Visual C#
//
// This file contains a class used to cache content type classifications so that they can be tracked and omitted
// if not wanted.
//
// This code is published under the Microsoft Public License (Ms-PL).  A copy of the license should be
// distributed with the code and can be found at the project website: https://github.com/EWSoftware/VSSpellChecker
// This notice, the author's name, and all copyright notices must remain intact in all applications,
// documentation, and source files.
//
//    Date     Who  Comments
//===============================================================================================================
// 08/15/2018  EFW  Created the code
//===============================================================================================================

using System.Collections.Concurrent;
using System.Collections.Generic;

namespace VisualStudio.SpellChecker.Tagging
{
    /// <summary>
    /// This is used to cache content type classifications
    /// </summary>
    /// <remarks>It is used by the configuration file editor to allow exclusion of classifications for any
    /// content type handled by the generic comment text tagger or others.</remarks>
    internal class ClassificationCache
    {
        #region Private data members
        //=====================================================================

        // Thread-safe dictionaries are used to ensure there are no issues if accessed from background tasks
        private static readonly ConcurrentDictionary<string, ClassificationCache> contentTypes =
            new ConcurrentDictionary<string, ClassificationCache>();

        private readonly ConcurrentDictionary<string, byte> contentClassifications;

        #endregion

        #region Properties
        //=====================================================================

        /// <summary>
        /// This is used to indicate whether or not classification caching is enabled
        /// </summary>
        /// <remarks>This only needs to be enabled when you want to obtain a set of content types and their
        /// classifications that are spell checked.  This can be done from the configuration file editor.</remarks>
        public static bool CachingEnabled { get; set; }

        /// <summary>
        /// This read-only property returns an enumerable list of the cached content types
        /// </summary>
        public static IEnumerable<string> ContentTypes
        {
            get { return contentTypes.Keys; }
        }

        /// <summary>
        /// This read-only property returns an enumerable list of the cached classifications
        /// </summary>
        public IEnumerable<string> Classifications
        {
            get { return contentClassifications.Keys; }
        }
        #endregion

        #region Constructor
        //=====================================================================

        /// <summary>
        /// Constructor
        /// </summary>
        private ClassificationCache()
        {
            contentClassifications = new ConcurrentDictionary<string, byte>();
        }
        #endregion

        #region Methods
        //=====================================================================

        /// <summary>
        /// This is used to obtain the classification cache for the given content type
        /// </summary>
        /// <param name="contentType">The content type for which to obtain a cache</param>
        /// <returns>The classification cache for the given content type</returns>
        public static ClassificationCache CacheFor(string contentType)
        {
            return contentTypes.GetOrAdd(contentType, (key) => new ClassificationCache());
        }

        /// <summary>
        /// This is used to add a classification type to the cache
        /// </summary>
        /// <param name="classification">The classification type to add</param>
        /// <remarks>If <see cref="CachingEnabled"/> is false, the classification will not be added</remarks>
        public void Add(string classification)
        {
            if(CachingEnabled)
                contentClassifications.TryAdd(classification, 1);
        }
        #endregion
    }
}
