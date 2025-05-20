using System.Collections.Generic;

namespace HitServicesCore.Models.IS_Services;

public class ISSaveToTableModel : ISServiceGeneralModel
{
	public string SourceDB { get; set; } = "Server=server;Database=db;User id=user;Password=password";

	public string DestinationDB { get; set; } = "Server=server;Database=db;User id=user;Password=password";

	public string DestinationDBTableName { get; set; }

	public int DBOperation { get; set; }

	public bool DBTransaction { get; set; }

	public string DBTimeout { get; set; } = "60";

	public string SqlScript { get; set; }

	public Dictionary<string, string> SqlParameters { get; set; } = new Dictionary<string, string>();

	public string SqlDestPreScript { get; set; }
}
