namespace HitServicesCore.Models.Helpers;

public class LicenseModel
{
	public string CustomerName { get; set; }

	public string HitKey { get; set; }

	public string ApplicationName { get; set; }

	public string ServerId { get; set; }

	public string ExpirationDate { get; set; }

	public bool? isWebPosApiPhoneCenter { get; set; }

	public string daStores { get; set; }

	public string storeGuidId { get; set; }

	public string numberOfPos { get; set; }

	public string numberOfPda { get; set; }

	public string numberOfKds { get; set; }

	public string storeSql { get; set; }

	public string storeDbName { get; set; }

	public string storeUserName { get; set; }

	public string storePassword { get; set; }

	public string comments { get; set; }
}
