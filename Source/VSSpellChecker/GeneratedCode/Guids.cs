using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VisualStudio.SpellChecker
{
    static partial class GuidList
    {
        public const string guidVSSpellCheckerPkgString = "86b8a6ea-6a96-4e31-b31d-943e86581421";
        public const string guidCommandSetString = "34482677-bc69-4bd3-8b8b-1ecd347f609d";

        public static readonly Guid guidVSSpellCheckerCmdSet = new Guid(guidCommandSetString);
    };
}
