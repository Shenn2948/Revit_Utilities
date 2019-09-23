namespace Gladkoe.ParameterDataManipulations.Interfaces
{
    using System.Collections.Generic;
    using System.Data;

    using Autodesk.Revit.DB;

    public interface IGetDataSetStrategy<TKey, TValue>
    {
        DataSet GetDataSet(IDictionary<TKey, TValue> data);

        DataTable GetTable(KeyValuePair<TKey, TValue> element);
    }
}