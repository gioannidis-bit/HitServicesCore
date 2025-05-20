using System.ComponentModel;

namespace HitServicesCore.Enums;

public enum DBOperationEnum
{
	[Description("Inserts and Updates")]
	InsertsAndUpdates,
	[Description("Inserts only")]
	InsertsOnly,
	[Description("Updates only")]
	UpdatesOnly
}
