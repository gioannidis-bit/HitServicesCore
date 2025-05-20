using System.Collections.Generic;

namespace HitServicesCore.Models.IS_Services;

public class ImportDataToTableFromCsvModel
{
	public int RowNo { get; set; }

	public LinkedList<CsvColumnsHeaderModel> ColumnsData { get; set; }
}
