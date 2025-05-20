using System.ComponentModel;

namespace HitServicesCore.Enums;

public enum HttpMediaTypeEnum
{
	[Description("application/json")]
	json,
	[Description("application/xml")]
	xml,
	[Description("application/text")]
	text
}
