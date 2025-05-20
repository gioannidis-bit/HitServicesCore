namespace HitServicesCore.Models;

public class ValidationModel
{
	public string hitKey { get; set; }

	public string fileName { get; set; }

	public string fileRecords { get; set; }

	public string serverId { get; set; }

	public string applicationName { get; set; }

	public bool? isWebPosApiPhoneCenter { get; set; }
}
