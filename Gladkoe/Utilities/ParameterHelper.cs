namespace Gladkoe.Utilities
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
                    if (param.AsInteger() == 0)
                    {
                        s = string.Empty;
                        break;
                    }

                    s = param.HasValue ? param.AsValueString() : string.Empty;
                    break;

                case StorageType.Integer:
                    if (param.AsInteger() == 0)
                    {
                        s = string.Empty;
                        break;
                    }

                    s = param.HasValue ? param.AsInteger().ToString() : string.Empty;
                    break;

                case StorageType.String:
                    s = param.HasValue ? param.AsString() : string.Empty;
                    break;

                case StorageType.ElementId:
                    if (param.AsElementId() == null)
                    {
                        s = null;

                        break;
                    }

                    s = param.HasValue ? param.AsValueString() : string.Empty;
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
                    if (string.IsNullOrEmpty(value.ToString()))
                    {
                        break;
                    }

                    param.Set(Convert.ToDouble(value));
                    break;

                case StorageType.Integer:
                    if (string.IsNullOrEmpty(value.ToString()))
                    {
                        break;
                    }

                    param.Set(Convert.ToInt32(value));
                    break;

                case StorageType.String:
                    if (string.IsNullOrEmpty(value.ToString()))
                    {
                        break;
                    }

                    param.Set(value.ToString());
                    break;

                case StorageType.ElementId:
                    if (string.IsNullOrEmpty(value.ToString()))
                    {
                        break;
                    }

                    param.Set((ElementId)default);
                    break;

                case StorageType.None:
                    break;

                default:
                    break;
            }
        }

        public static Dictionary<TKey, List<TValue>> ToDictionary<TKey, TValue>(this IEnumerable<IGrouping<TKey, TValue>> groupings)
        {
            return groupings.ToDictionary(group => group.Key, group => group.ToList());
        }

        public static int ToInt32(this string s)
        {
            return Convert.ToInt32(s);
        }
    }
}