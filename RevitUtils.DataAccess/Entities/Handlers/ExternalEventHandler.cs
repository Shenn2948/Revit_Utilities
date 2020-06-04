using System;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace RevitUtils.DataAccess.Entities.Handlers
{
    public class ExternalEventHandler : IExternalEventHandler
    {
        public Action<Document> Action { get; set; }

        public string TransactionName { get; set; }

        public void Execute(UIApplication app)
        {
            UIDocument uidoc = app.ActiveUIDocument;
            if (null == uidoc)
            {
                return;
            }

            Document doc = uidoc.Document;

            ProceedTransaction(doc);
        }

        public string GetName()
        {
            return "ExternalEventHandler";
        }

        private void ProceedTransaction(Document doc)
        {
            if (TransactionName != null && Action != null)
            {
                using (var tran = new Transaction(doc, TransactionName))
                {
                    tran.Start();

                    Action(doc);

                    tran.Commit();
                }
            }
        }
    }
}