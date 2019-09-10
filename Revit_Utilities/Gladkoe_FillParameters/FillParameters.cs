namespace Revit_Utilities.Gladkoe_FillParameters
{
    using System.Collections.Generic;
    using System.Linq;

    using Autodesk.Revit.DB;
    using Autodesk.Revit.UI;

    public class FillParameters
    {
        public static void GetElements(Document doc, UIDocument uidoc)
        {
            var elements = new FilteredElementCollector(doc).WhereElementIsNotElementType()
                .WhereElementIsViewIndependent()
                .WherePasses(new ElementMulticategoryFilter(new List<BuiltInCategory> { BuiltInCategory.OST_PipeAccessory, BuiltInCategory.OST_MechanicalEquipment }))
                .OfClass(typeof(FamilyInstance))
                .Cast<FamilyInstance>()
                .Where(
                    e => (e.SuperComponent == null) && (e.MEPModel.ConnectorManager != null) && !e.Symbol.FamilyName.Equals("802_ОпорыКорпусныеПриварные_КП_ОСТ36-146-88(ОбМод)"))
                .SelectMany(e => e.MEPModel.ConnectorManager.Connectors.Cast<Connector>(), (instance, connector) => (instance, connector))
                .SelectMany(e => e.connector.AllRefs.Cast<Connector>(), (tupleInstanceConnector, connector) => (tupleInstanceConnector, connector))
                .SelectMany(e => e.connector.ConnectorManager.Connectors.Cast<Connector>(), (tupleInstanceConnector, connector) => (tupleInstanceConnector, connector))
                .SelectMany(e => e.connector.AllRefs.Cast<Connector>(), (tupleInstanceConnector, connector) => (tupleInstanceConnector, connector))
                .Where(
                    e => (e.connector.Owner.Category.Id.IntegerValue == (int)BuiltInCategory.OST_PipeCurves)
                         || (e.connector.Owner.Category.Id.IntegerValue == (int)BuiltInCategory.OST_PipeFitting))
                .Select(e => e.connector.Owner.Id)
                .ToList();

            var elements2 = new FilteredElementCollector(doc).WhereElementIsNotElementType()
                .WhereElementIsViewIndependent()
                .WherePasses(new ElementMulticategoryFilter(new List<BuiltInCategory> { BuiltInCategory.OST_PipeAccessory, BuiltInCategory.OST_MechanicalEquipment }))
                .OfClass(typeof(FamilyInstance))
                .Cast<FamilyInstance>()
                .Where(
                    e => (e.SuperComponent == null) && (e.MEPModel.ConnectorManager != null) && !e.Symbol.FamilyName.Equals("802_ОпорыКорпусныеПриварные_КП_ОСТ36-146-88(ОбМод)"))
                .SelectMany(e => e.GetSubComponentIds(), (instance, id) => (instance, subComponent: doc.GetElement(id) as FamilyInstance))
                .Where(f => f.subComponent.Symbol.FamilyName.Contains("_Фланец_"))
                .SelectMany(e => e.instance.MEPModel.ConnectorManager.Connectors.Cast<Connector>(), (instance, connector) => (instance, connector))
                .SelectMany(e => e.connector.AllRefs.Cast<Connector>(), (tupleInstanceConnector, connector) => (tupleInstanceConnector, connector))
                .Where(
                    e =>
                    {
                        if (doc.GetElement(e.connector.Owner.Id) is FamilyInstance s)
                        {
                            return !s.Symbol.FamilyName.Equals("801_СварнойШов_ОБЩИЙ");
                        }

                        return false;
                    })
                .Where(
                    e => (e.connector.Owner.Category.Id.IntegerValue == (int)BuiltInCategory.OST_PipeCurves)
                         || (e.connector.Owner.Category.Id.IntegerValue == (int)BuiltInCategory.OST_PipeFitting))
                .Select(e => e.connector.Owner.Id)
                .ToList();
            uidoc.Selection.SetElementIds(elements2);
        }
    }
}