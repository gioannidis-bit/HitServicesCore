using System.ComponentModel;

namespace HitServicesCore.Enums;

public enum FileEncodingEnum
{
	[Description("UTF7")]
	UTF7,
	[Description("UTF8")]
	UTF8,
	[Description("UTF32")]
	UTF32,
	[Description("ASCII")]
	ASCII,
	[Description("DEFAULT")]
	DEFAULT,
	[Description("Unicode")]
	Unicode,
	[Description("Custom")]
	Custom
}
