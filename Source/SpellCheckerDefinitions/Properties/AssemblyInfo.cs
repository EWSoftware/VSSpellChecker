//===============================================================================================================
// System  : Visual Studio Spell Checker Definitions
// File    : AssemblyInfo.cs
// Author  : Eric Woodruff  (Eric@EWoodruff.us)
// Updated : 05/20/2013
// Note    : Copyright 2013, Eric Woodruff, All rights reserved
// Compiler: Microsoft Visual C#
//
// Visual Studio spell checker definition attributes.
//
// This code is published under the Microsoft Public License (Ms-PL).  A copy of the license should be
// distributed with the code.  It can also be found at the project website: http://VSSpellChecker.CodePlex.com.
// This notice, the author's name, and all copyright notices must remain intact in all applications,
// documentation, and source files.
//
// Version     Date     Who  Comments
// ==============================================================================================================
// 1.0.0.0  05/20/2013  EFW  Created the code
//===============================================================================================================

using System;
using System.Reflection;
using System.Resources;
using System.Runtime.InteropServices;

// Resources contained within the assembly are English
[assembly: NeutralResourcesLanguageAttribute("en")]

//
// General Information about an assembly is controlled through the following set of attributes. Change these
// attribute values to modify the information associated with an assembly.
//
[assembly: AssemblyProduct("Visual Studio Spell Checker")]
[assembly: AssemblyTitle("Visual Studio Spell Checker Definitions")]
[assembly: AssemblyDescription("This assembly contains interfaces and other supporting classes used to " +
    "implement the Visual Studio spell checker")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("Eric Woodruff")]
[assembly: AssemblyCopyright("Copyright \xA9 2013, Eric Woodruff, All Rights Reserved.\r\n" +
    "Portions Copyright \xA9 2010-2013, Microsoft Corporation, All Rights Reserved.")]
[assembly: AssemblyTrademark("Eric Woodruff, All Rights Reserved")]
[assembly: AssemblyCulture("")]
[assembly: CLSCompliant(true)]
[assembly: ComVisible(false)]

//
// Version information for an assembly consists of the following four values:
//
//      Major Version
//      Minor Version
//      Build Number
//      Revision
//
// You can specify all the values or you can default the Revision and Build Numbers by using the '*' as shown
// below:

[assembly: AssemblyVersion("1.0.0.0")]
