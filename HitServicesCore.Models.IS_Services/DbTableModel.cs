using System.Collections.Generic;

namespace HitServicesCore.Models.IS_Services;

public class DbTableModel
{
	public string TableName { get; set; }

	public List<DBColumnModel> Columns { get; set; }
}
