using System;
using System.Collections.Generic;
using System.Linq;

using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace Revit_Utilities.Gladkoe
{
    using System.Text;

    using Autodesk.Revit.UI.Selection;

    public class FillLevelMarkParameter
    {
        public static void FillParams(Document doc, UIDocument uidoc)
        {
            try
            {
                FillParametersAction(doc, uidoc);
            }
            catch (Exception e)
            {
                TaskDialog.Show("Fill parameters", e.Message);
            }
        }

        private static void FillParametersAction(Document doc, UIDocument uidoc)
        {
            Reference pickedObj = uidoc.Selection.PickObject(ObjectType.Element, "Select element");
            ElementId elementId = pickedObj.ElementId;

            using (Transaction tx = new Transaction(doc))
            {
                StringBuilder sb = new StringBuilder();
                tx.Start("GetInfo");

                Element e = doc.GetElement(pickedObj.ElementId);

                sb.Append("\n" + e.Name);
                TaskDialog.Show("Info", sb.ToString());

                tx.Commit()
            }
        }
    }
}