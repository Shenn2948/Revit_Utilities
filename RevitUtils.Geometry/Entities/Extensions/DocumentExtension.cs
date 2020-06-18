using System.Linq;
using Autodesk.Revit.DB;

namespace RevitUtils.Geometry.Entities.Extensions
{
    public static class DocumentExtension
    {
        public static FamilySymbol GetFamilySymbol(this Document doc, string familyName, string name)
        {
            FamilySymbol symbol = new FilteredElementCollector(doc).OfClass(typeof(FamilySymbol))
                                                                   .OfType<FamilySymbol>()
                                                                   .FirstOrDefault(x => x.FamilyName == familyName && x.Name == name);

            if (symbol != null && !symbol.IsActive)
            {
                symbol.Activate();
            }

            return symbol;
        }

    }
}