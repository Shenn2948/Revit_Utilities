using Autodesk.Revit.DB;
using Autodesk.Revit.UI.Selection;

namespace RevitUtils.Geometry.Entities.Selection
{
    public class WallSelectionFilter : ISelectionFilter
    {
        public bool AllowElement(Element elem)
        {
            return elem is Wall;
        }

        public bool AllowReference(Reference reference, XYZ position)
        {
            return false;
        }
    }
}