using System.ComponentModel;

namespace HitServicesCore.Enums;

public enum EncryptionModeEnum
{
	[Description("None")]
	None,
	[Description("Implicit")]
	Implicit,
	[Description("Explicit")]
	Explicit
}
