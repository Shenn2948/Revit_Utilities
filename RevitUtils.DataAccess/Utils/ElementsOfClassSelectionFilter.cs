using Autodesk.Revit.DB;
using Autodesk.Revit.UI.Selection;

namespace RevitUtils.DataAccess.Utils
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