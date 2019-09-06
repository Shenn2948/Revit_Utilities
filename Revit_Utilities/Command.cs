namespace Revit_Utilities
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Reflection;

    using Autodesk.Revit.Attributes;
    using Autodesk.Revit.DB;
    using Autodesk.Revit.UI;

    using Revit_Utilities.Utilities;

    using Application = Autodesk.Revit.ApplicationServices.Application;

    [Transaction(TransactionMode.Manual)]
    public class Command : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Application app = uiapp.Application;
            Document doc = uidoc.Document;

            var pipeFittingsDictionary2 = new FilteredElementCollector(doc).WhereElementIsNotElementType()
                .WhereElementIsViewIndependent()
                .OfCategory(BuiltInCategory.OST_PipeFitting)
                .OfClass(typeof(FamilyInstance))
                .Cast<FamilyInstance>()
                .Where(i => i.Name.Equals("ГОСТ 10704-91 Трубы стальные электросварные прямошовные") && i.Symbol.FamilyName.Equals("801_СварнойШов_ОБЩИЙ"))
                .SelectMany(i => i.MEPModel.ConnectorManager.Connectors.Cast<Connector>().Select(e => e.AllRefs.Cast<Connector>()).FirstOrDefault())
                .GroupBy(e => e.Owner.Name, e => e)
                .Where(
                    e => e.Key.Contains("Азот") || e.Key.Contains("Вода") || e.Key.Contains("Газ") || e.Key.Contains("Дренаж")
                         || e.Key.Contains("Нефтепродукты") || e.Key.Contains("Пенообразователь") || e.Key.Contains("ХимическиеРеагенты")
                         || e.Key.Contains("Канализация"))
                .ToDictionary(e => e.Key, e => e.ToList());












            using (Transaction tran = new Transaction(doc))
            {
                tran.Start("Change colors");

                foreach (var item in pipeFittingsDictionary2)
                {
                    foreach (Connector connector in item.Value)
                    {
                        foreach (Connector connectorAllRef in connector.AllRefs)
                        {
                            Parameter p = connectorAllRef.Owner.GetOrderedParameters().FirstOrDefault(i => i.Definition.Name.Equals("МатериалФитинга"));

                            if (p != null)
                            {
                                if (item.Key.Contains("Азот"))
                                {
                                    SetValue(p, "0_153_255");
                                }

                                if (item.Key.Contains("Вода"))
                                {
                                    SetValue(p, "0_96_0");
                                }

                                if (item.Key.Contains("Газ"))
                                {
                                    SetValue(p, "255_220_112");
                                }

                                if (item.Key.Contains("Дренаж"))
                                {
                                    SetValue(p, "192_192_192");
                                }

                                if (item.Key.Contains("Нефтепродукты"))
                                {
                                    SetValue(p, "160_80_0");
                                }

                                if (item.Key.Contains("Пенообразователь"))
                                {
                                    SetValue(p, "224_0_0");
                                }

                                if (item.Key.Contains("ХимическиеРеагенты"))
                                {
                                    SetValue(p, "128_96_0");
                                }

                                if (item.Key.Contains("Канализация"))
                                {
                                    SetValue(p, "192_192_192");
                                }
                            }
                        }
                    }
                }

                tran.Commit();
            }

            return Result.Succeeded;
        }

        public static void SetValue(Parameter p, object value)
        {
            try
            {
                if (value is string s1)
                {
                    if (p.SetValueString(s1))
                    {
                        return;
                    }
                }

                switch (p.StorageType)
                {
                    case StorageType.None:
                        break;
                    case StorageType.Double:
                        p.Set(value is string o ? double.Parse(o) : Convert.ToDouble(value));
                        break;
                    case StorageType.Integer:
                        p.Set(value is string v ? int.Parse(v) : Convert.ToInt32(value));
                        break;
                    case StorageType.ElementId:
                        if (value.GetType() == typeof(ElementId))
                        {
                            p.Set(value as ElementId);
                        }
                        else if (value is string s)
                        {
                            p.Set(new ElementId(int.Parse(s)));
                        }
                        else
                        {
                            p.Set(new ElementId(Convert.ToInt32(value)));
                        }

                        break;
                    case StorageType.String:
                        p.Set(value.ToString());
                        break;
                }
            }
            catch
            {
                throw new Exception("Invalid Value Input!");
            }
        }

        private class ConnectorEqualityComparer : IEqualityComparer<KeyValuePair<FamilyInstance, List<Connector>>>
        {
            public bool Equals(KeyValuePair<FamilyInstance, List<Connector>> x, KeyValuePair<FamilyInstance, List<Connector>> y)
            {
                return x.Value.Where((t, i) => t.Owner.Name.Equals(y.Value[i].Owner.Name)).Any();
            }

            public int GetHashCode(KeyValuePair<FamilyInstance, List<Connector>> obj)
            {
                return obj.GetHashCode();
            }
        }
    }
}