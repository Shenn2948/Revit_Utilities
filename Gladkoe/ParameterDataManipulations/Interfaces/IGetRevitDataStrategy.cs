namespace Gladkoe.ParameterDataManipulations.Interfaces
{
    using System.Collections.Generic;

    using Autodesk.Revit.DB;

    public interface IGetRevitDataStrategy
    {
         IEnumerable<Element> GetElements(Document doc);
    }
}