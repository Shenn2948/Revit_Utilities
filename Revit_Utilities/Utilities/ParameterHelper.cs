namespace Revit_Utilities.Utilities
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Autodesk.Revit.DB;

    public static class ParameterHelper
    {
        public static string GetStringParameterValue(this Parameter param)
        {
            string s;
            switch (param.StorageType)
            {
                case StorageType.Double:
                    s = param.HasValue ? param.AsValueString() : string.Empty;
                    break;

                case StorageType.Integer:
                    s = param.HasValue ? param.AsInteger().ToString() : string.Empty;
                    break;

                case StorageType.String:
                    s = param.HasValue ? param.AsString() : string.Empty;
                    break;

                case StorageType.ElementId:
                    s = param.HasValue ? param.AsElementId().IntegerValue.ToString() : string.Empty;
                    break;

                case StorageType.None:
                    s = "?NONE?";
                    break;

                default:
                    s = "?ELSE?";
                    break;
            }

            return s;
        }

        public static void SetObjectParameterValue(this Parameter param, object value)
        {
            switch (param.StorageType)
            {
                case StorageType.Double:
                    param.SetValueString(value.ToString());
                    break;

                case StorageType.Integer:
                    break;

                case StorageType.String:
                    break;

                case StorageType.ElementId:
                    break;

                case StorageType.None:
                    // s = "?NONE?";
                    break;

                default:
                    // s = "?ELSE?";
                    break;
            }
        }

        public static Dictionary<TKey, List<TValue>> ToDictionary<TKey, TValue>(this IEnumerable<IGrouping<TKey, TValue>> groupings)
        {
            return groupings.ToDictionary(group => group.Key, group => group.ToList());
        }
    }
}