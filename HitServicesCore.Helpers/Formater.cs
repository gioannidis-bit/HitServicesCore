using System.Globalization;

namespace HitServicesCore.Helpers;

public class Formater
{
	public string Format { get; set; } = "";

	public string CultureInfoDescription { get; set; }

	public CultureInfo CultureInfo
	{
		get
		{
			if (string.IsNullOrEmpty(CultureInfoDescription))
			{
				return CultureInfo.InvariantCulture;
			}
			return CultureInfo.CreateSpecificCulture(CultureInfoDescription);
		}
	}
}
