---
uid: df2a45c1-1996-46f6-9d33-e73f0fa1d88a
alt-uid: ThirdPartyContentTypes
title: Add Spell Checking for Third Party Content Types
keywords: third party content types
---

A NuGet package is available that will allow you to enable spell checking for third-party content
types within Visual Studio (**EWSoftware.VSSpellChecker**).  This topic provides a simple
example of a tagger that enables spell checking for such third-party content types.


> [!NOTE]
> The NuGet package version may not match the version of the latest release of the spell checker
> package.  This is normal.  The NuGet package assembly version only changes when it contains a breaking change
> that will require rebuilding any code that utilizes it.
> 
>

Once you have installed the NuGet package in your project, add a new class file to the project that
implements the content type you want to support.  Replace the new class's content with the following code:


``` cs{title="Example Tagger"}
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;

using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

using VisualStudio.SpellChecker.Definitions;

namespace ExampleTagger.SpellCheckSupport
{
    /// <summary>
    /// Define a class to serve as the tag
    /// </summary>
    class NaturalTextTag : INaturalTextTag
    {
    }

    /// <summary>
    /// Implement the tagger provider
    /// </summary>
    [Export(typeof(ITaggerProvider))]
    [ContentType("MyCustomContentType")] // IMPORTANT: Specify your content type here
    [TagType(typeof(NaturalTextTag))]
    class MyCustomContentTypeNaturalTextTaggerProvider : ITaggerProvider
    {
        /// <inheritdoc />
        public ITagger<T> CreateTagger<T>(ITextBuffer buffer) where T : ITag
        {
            return new MyCustomContentTypeNaturalTextTagger() as ITagger<T>;
        }
    }

    /// <summary>
    /// Implement the tagger
    /// </summary>
    class MyCustomContentTypeNaturalTextTagger : ITagger<NaturalTextTag>
    {
        /// <summary>
        /// This is a simple tagger that returns tags for the normalized snapshot
        /// span collection.
        /// </summary>
        /// <param name="spans"></param>
        /// <returns></returns>
        public IEnumerable<ITagSpan<NaturalTextTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            foreach(var snapshotSpan in spans)
                yield return new TagSpan<NaturalTextTag>(snapshotSpan, new NaturalTextTag());
        }

#pragma warning disable 67
        /// <inheritdoc />
        /// <remarks>Not used by this tagger</remarks>
        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;
#pragma warning restore 67
    }
}
```

Be sure to specify your custom content type in the `ContentType` attribute
on the tagger provider class where indicated.  The tagger above is a basic one that simply returns a tag span for
each normalized snapshot span.  More complex taggers can make use of a classifier to further examine each
snapshot span and determine if the span should be included for spell checking.  See the tagger classes in the
spell checker project for more complex examples.



## See Also


**Other Resources**  
[](@deeba4a0-5a5f-497c-a9c1-7dec64e9c2bf)  
[](@0ff35371-69b5-48dd-a062-037abe2469de)  
