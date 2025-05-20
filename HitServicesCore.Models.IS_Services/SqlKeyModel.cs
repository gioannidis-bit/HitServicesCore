using System.Collections.Generic;

namespace HitServicesCore.Models.IS_Services;

public class SqlKeyModel
{
	public string SymmetricKey { get; set; }

	public string Certificate { get; set; }

	public string Password { get; set; }

	public List<string> EncryptedColumns { get; set; }
}
