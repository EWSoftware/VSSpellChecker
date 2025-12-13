---
uid: 847a2b53-6583-4198-80ef-0e537346e4a3
alt-uid: Contributing
title: Contributing
keywords: contributing, coding style, submitting changes
---
<!-- Ignore spelling: Allman -->
<autoOutline excludeRelatedTopics="true" lead="none" />

## Issues
If you don't feel like working on the code yourself,
[filing an issue](https://github.com/EWSoftware/VSSpellChecker/issues "VSSpellChecker Issues") and letting me
handle the change is fine.  You don't need to file an issue for trivial changes (i.e. typos, minor fixes, etc.).
Just send a pull request if it's small.

If an issue is complex, adds or removes functionality, changes existing behavior, etc. consider filing an issue
and giving me time to respond before sending a corresponding pull request.  If you want to work on the issue,
just let it be known on the issue thread.  Giving me a chance to review the issue will help save time and
prevents you from wasting time on something that cannot be implemented.  For example, I might let you know why
existing behavior cannot be changed or about particular implementation constraints you need to keep in mind, etc.

## Contributing to the Project
1. Unless it is a trivial change such as a typo or minor fix, make sure that there is a corresponding issue for
   your change first.  If there isn't, create one.
2. Create a fork in GitHub
3. Create a branch off the master branch.  Name it something that makes sense, such as *issue-123* or
   *githubUserID-issue*.  This makes it easy for everyone to figure out what the branch is used for.  It also
   makes it easier to isolate your changes from incoming changes from the origin.
4. Commit your changes and push your changes to GitHub.
5. Create a pull request against the origin's master branch.

## Coding Style
In general, use the Visual Studio defaults.  Take a look at the existing code to see the styles in use. The
*.editorconfig* file defines the preferred style defaults.

1. In source code, use four spaces for indentation, no tabs.
2. For XML, XAML, HTML, and similar file types, use two spaces for indentation and do use tabs.
3. Namespace imports should be specified at the top of the file, outside of the `namespace` declaration, and
   should be sorted alphabetically.  Place `System.` namespaces at the top and blank lines between different top
   level groups.  Remove any unused namespaces to avoid unnecessary clutter.
4. Use Allman style braces where each brace begins on a new line. A single line statement block can go without
   braces but the block must be properly indented on its own line.
5. Do not insert spaces after keywords in control flow statements (use `if(a < b)` instead of `if (a < b)`).
6. Use `camelCase` private members without leading underscores or other such prefixes like `m_`.
7. Always specify the visibility even if it is the default (i.e. `private string foo` not `string foo`).

## See Also
**Other Resources**  
[](@027d2fbc-7bfb-4dc3-b4f5-85f95fcf7629)  
[License Agreement](https://github.com/EWSoftware/VSSpellChecker/blob/master/LICENSE)  
