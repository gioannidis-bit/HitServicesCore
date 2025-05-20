using System;
using System.Collections.Generic;

namespace HitServicesCore.Models.Helpers;

public class LicenseForCreateModel
{
	public string CustomerName { get; set; }

	public string HitKey { get; set; }

	public string ApplicationName { get; set; }

	public string ServerId { get; set; }

	public DateTime ExpirationDate { get; set; }

	public bool? IsWebPosApiPhoneCenter { get; set; }

	public int? DaStores { get; set; }

	public List<LicenseStoreModel> StoreModel { get; set; }

	public string Comments { get; set; }
}
