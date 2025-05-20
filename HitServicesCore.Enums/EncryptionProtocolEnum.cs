using System.ComponentModel;

namespace HitServicesCore.Enums;

public enum EncryptionProtocolEnum
{
	[Description("None")]
	None = 0,
	[Description("Ssl2")]
	Ssl2 = 12,
	[Description("Ssl3")]
	Ssl3 = 48,
	[Description("Tls")]
	Tls = 192,
	[Description("Default")]
	Default = 240,
	[Description("Tls11")]
	Tls11 = 768,
	[Description("Tls12")]
	Tls12 = 3072,
	[Description("Tls13")]
	Tls13 = 12288
}
