using System.Collections.Generic;
using SendInvoiceViaEmail.DataAccess;
using SendInvoiceViaEmail.LocalModels;

namespace SendInvoiceViaEmail.MainLogic.Tasks;

public class SendInvoiceViaEmailTasks
{
	private readonly SendInvoiceViaEmailDT dt;

	public SendInvoiceViaEmailTasks(string connection, string dbSchema)
	{
		dt = new SendInvoiceViaEmailDT(connection, dbSchema);
	}

	public InvoiceModel GetInvoiceData(int profileId, int invoiceNo, int resNo, out string errorMess)
	{
		return dt.GetInvoiceData(profileId, invoiceNo, resNo, out errorMess);
	}

	public void AddErrorToProtelTable(string errorMess, int mpehotel, int leistacc, int kundennr)
	{
		dt.AddErrorToProtelTable(errorMess, mpehotel, leistacc, kundennr);
	}

	public List<FieldsModel> GetProtelValues(SelectValuesModel model, string sWehere)
	{
		return dt.GetProtelValues(model, sWehere);
	}

	public void AddEmailStatusToDB(SendInvoiceViaEmailDTO model)
	{
		dt.AddEmailStatusToDB(model);
	}
}
