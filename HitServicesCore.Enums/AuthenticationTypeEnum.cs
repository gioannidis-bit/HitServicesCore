using System.ComponentModel;

namespace HitServicesCore.Enums;

public enum AuthenticationTypeEnum
{
	[Description("Basic")]
	Basic,
	[Description("OAuth2")]
	OAuth2,
	[Description("Anonymous")]
	Anonymous
}
