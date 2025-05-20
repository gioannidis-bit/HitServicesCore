using System;
using System.Collections.Generic;

namespace SendInvoiceViaEmail.LocalModels;

public class InvoiceModel
{
	public int kdnr { get; set; }

	public int resno { get; set; }

	public int fisccode { get; set; }

	public int rechnr { get; set; }

	public DateTime issueDate { get; set; }

	public List<string> email { get; set; }

	public int mpehotel { get; set; }

	public string profileName { get; set; }
}
