using System;
using System.Collections.Generic;
using System.Linq;

using Autodesk.Revit.DB;
using Autodesk.Revit.UI.Selection;

namespace Revit_Utilities.Utilities
{
    /// <summary>
    /// Allow selection of elements of type T only.
    /// </summary>
    internal class ElementsOfClassSelectionFilter<T> : ISelectionFilter
        where T : Element
    {
        public bool AllowElement(Element elem)
        {
            return elem is T;
        }

        public bool AllowReference(Reference reference, XYZ position)
        {
            return true;
        }
    }
}