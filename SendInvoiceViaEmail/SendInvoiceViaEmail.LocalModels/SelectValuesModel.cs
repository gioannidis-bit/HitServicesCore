using System.Collections.Generic;

namespace SendInvoiceViaEmail.LocalModels;

public class SelectValuesModel
{
	public string tableName { get; set; }

	public List<FieldsModel> fields { get; set; }
}
