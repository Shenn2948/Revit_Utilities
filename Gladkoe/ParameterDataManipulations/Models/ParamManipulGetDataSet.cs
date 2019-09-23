namespace Gladkoe.ParameterDataManipulations.Models
{
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;

    using Autodesk.Revit.DB;

    using Gladkoe.ParameterDataManipulations.Interfaces;
    using Gladkoe.Utilities;

    public class ParamManipulGetDataSet : IGetDataSetStrategy<string, List<Element>>
    {
        public DataSet GetDataSet(IDictionary<string, List<Element>> data)
        {
            var ds = new DataSet();
            foreach (var element in data)
            {
                ds.Tables.Add(GetTable(element));
            }

            return ds;
        }

        public DataTable GetTable(KeyValuePair<string, List<Element>> element)
        {
            var table = new DataTable { TableName = element.Key };

            foreach (Element item in element.Value)
            {
                DataRow row = table.NewRow();

                foreach (Parameter parameter in item.GetOrderedParameters()
                    .Where(p => (p.Definition.ParameterGroup == BuiltInParameterGroup.PG_ADSK_MODEL_PROPERTIES) && p.IsShared))
                {
                    if (!table.Columns.Contains(parameter.GUID.ToString()))
                    {
                        table.Columns.Add(parameter.GUID.ToString());
                    }

                    row[parameter.GUID.ToString()] = parameter.GetStringParameterValue();
                }

                table.Rows.Add(row);
            }

            return table;
        }
    }
}