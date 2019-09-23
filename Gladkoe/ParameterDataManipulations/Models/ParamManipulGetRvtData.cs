namespace Gladkoe.ParameterDataManipulations.Models
{
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;

    using Autodesk.Revit.DB;

    using Gladkoe.ParameterDataManipulations.Interfaces;
    using Gladkoe.Utilities;

    public class ParamManipulGetRvtData : IGetRevitDataStrategy
    {
        public IEnumerable<Element> GetElements(Document doc)
        {
            return new FilteredElementCollector(doc).WhereElementIsNotElementType()
                .WhereElementIsViewIndependent()
                .WherePasses(
                    new ElementMulticategoryFilter(
                        new List<BuiltInCategory>
                        {
                            BuiltInCategory.OST_PipeAccessory,
                            BuiltInCategory.OST_PipeCurves,
                            BuiltInCategory.OST_MechanicalEquipment,
                            BuiltInCategory.OST_PipeFitting,
                            BuiltInCategory.OST_FlexPipeCurves,
                            BuiltInCategory.OST_PlumbingFixtures
                        }))
                .Where(e => e.Category != null)
                .Where(
                    delegate (Element e)
                    {
                        Parameter volume = e.get_Parameter(BuiltInParameter.HOST_VOLUME_COMPUTED);

                        if ((e is FamilyInstance fs && (fs.SuperComponent != null)) || ((volume != null) && !volume.HasValue))
                        {
                            return false;
                        }

                        return true;
                    });
        }
    }
}