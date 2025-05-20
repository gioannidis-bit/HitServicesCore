using System;

namespace HitServicesCore.Models.Helpers;

public class LicenseStoreModel
{
	public Guid StoreGuidId { get; set; }

	public int NumberOfPos { get; set; }

	public int NumberOfPda { get; set; }

	public int NumberOfKds { get; set; }

	public string StoreSql { get; set; }

	public string StoreDbName { get; set; }

	public string StoreUserName { get; set; }

	public string StorePassword { get; set; }
}
