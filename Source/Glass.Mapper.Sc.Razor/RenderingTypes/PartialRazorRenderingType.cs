/*
   Copyright 2012 Michael Edwards
 
   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use this file except in compliance with the License.
   You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.
 
*/ 
//-CRE-
 using System;
using System.Collections.Specialized;
using System.Web.UI;
using Glass.Mapper.Sc.Razor.Web.Ui;

namespace Glass.Mapper.Sc.Razor.RenderingTypes
{
    /// <summary>
    /// Class TypedRazorRenderingType
    /// </summary>
    public class PartialRazorRenderingType : AbstractCachingRenderingType
    {
        static Func<string, Type> _typeLoader = typeName =>
        {
            var strongGeneric = typeof(PartialControl<>);
            var modelType = Type.GetType(typeName);

            if (modelType == null) throw new NullReferenceException("Could not find type {0}.".Formatted(typeName));

            return strongGeneric.MakeGenericType(modelType);
        };

        /// <summary>
        /// Gets the control.
        /// </summary>
        /// <param name="parameters">The parameters.</param>
        /// <param name="assert">if set to <c>true</c> this instance performs asserts.</param>
        /// <returns>The created control.</returns>
        public override Control GetControl(NameValueCollection parameters, bool assert)
        {
            string view = parameters["Name"];
            string type = parameters["Type"];
            string assembly = parameters["assembly"];
            string contextName = parameters["ContextName"];

            return CreateControl(view, type, assembly, contextName);
        }

        /// <summary>
        /// Creates the control.
        /// </summary>
        /// <param name="view">The view.</param>
        /// <param name="type">The type.</param>
        /// <param name="assembly">The assembly.</param>
        /// <param name="contextName">Name of the context.</param>
        /// <returns>Sitecore.Web.UI.WebControl.</returns>
        public static global::Sitecore.Web.UI.WebControl CreateControl(string view, string type, string assembly, string contextName)
        {
            string typeName = "{0}, {1}".Formatted(type, assembly);

            var viewType = GetControlType(typeName, _typeLoader);

            IRazorControl control = global::Sitecore.Reflection.ReflectionUtil.CreateObject(viewType) as IRazorControl;
            control.View = ViewManager.GetRazorView(view); ;
            control.ContextName = contextName;
            return control as global::Sitecore.Web.UI.WebControl;
        }
    }

}
