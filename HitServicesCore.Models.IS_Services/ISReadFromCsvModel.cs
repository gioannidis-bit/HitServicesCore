using System.Collections.Generic;
using System.Text;

namespace HitServicesCore.Models.IS_Services;

public class ISReadFromCsvModel : ISServiceGeneralModel
{
	public string DestinationDB { get; set; } = "Server=server;Database=db;User id=user;Password=password";

	public string DestinationDBTableName { get; set; }

	public int DBOperation { get; set; }

	public bool DBTransaction { get; set; }

	public string DBTimeout { get; set; } = "60";

	public string SqlDestPreScript { get; set; }

	public string CsvFilePath { get; set; }

	public bool? CsvFileHeader { get; set; } = true;

	public List<string> CsvFileHeaders { get; set; }

	public string CsvDelimenter { get; set; } = ";";

	public string CsvEncoding { get; set; } = "UTF8";

	public Encoding Encoding { get; set; }
}
