using System.Collections.Generic;

namespace HitServicesCore.Models.IS_Services;

public class ISRunSqlScriptsModel : ISServiceGeneralModel
{
	public string Custom1DB { get; set; } = "Server=server;Database=db;User id=user;Password=password";

	public string DBTimeout { get; set; } = "60";

	public string SqlScript { get; set; }

	public Dictionary<string, string> SqlParameters { get; set; } = new Dictionary<string, string>();
}
