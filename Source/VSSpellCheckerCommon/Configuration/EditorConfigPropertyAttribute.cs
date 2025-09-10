//===============================================================================================================
// System  : Spell Check My Code Package
// File    : EditorConfigPropertyAttribute.cs
// Author  : Eric Woodruff  (Eric@EWoodruff.us)
// Updated : 02/05/2023
// Note    : Copyright 2023, Eric Woodruff, All rights reserved
//
// This file contains an attribute class that is used to associate .editorconfig property names with spell
// checker configuration properties.
//
// This code is published under the Microsoft Public License (Ms-PL).  A copy of the license should be
// distributed with the code and can be found at the project website: https://github.com/EWSoftware/VSSpellChecker
// This notice, the author's name, and all copyright notices must remain intact in all applications,
// documentation, and source files.
//
//    Date     Who  Comments
// ==============================================================================================================
// 02/05/2023  EFW  Created the code
//===============================================================================================================

using System;

namespace VisualStudio.SpellChecker.Common.Configuration
{
    /// <summary>
    /// This attribute is used to associate .editorconfig property names with spell checker configuration
    /// properties.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class EditorConfigPropertyAttribute : Attribute
    {
        #region Properties
        //=====================================================================

        /// <summary>
        /// This read-only property is used to get the .editorconfig file property name
        /// </summary>
        public string PropertyName { get; }

        /// <summary>
        /// This read-only property is used to get whether or not the property can have multiple instances
        /// </summary>
        /// <value>If true, multiple instances of the property may appear with a unique value suffix to order
        /// them.  All values from the properties will be combined in the configuration.  If false, only one
        /// instance of the exact property name will appear.  If multiple copies do appear, the latest one's
        /// value will take precedence.</value>
        public bool CanHaveMultipleInstances { get; }

        #endregion

        #region Constructors
        //=====================================================================

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="propertyName">The .editorconfig file property name</param>
        /// <remarks>This creates a property with a single instance</remarks>
        /// <overloads>There are two overloads for the constructor</overloads>
        public EditorConfigPropertyAttribute(string propertyName) : this(propertyName, false)
        {
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="propertyName">The .editorconfig file property name</param>
        /// <param name="canHaveMultipleInstances">True if the property can have multiple instances, false if not</param>
        public EditorConfigPropertyAttribute(string propertyName, bool canHaveMultipleInstances)
        {
            this.PropertyName = propertyName;
            this.CanHaveMultipleInstances = canHaveMultipleInstances;
        }
        #endregion
    }
}
