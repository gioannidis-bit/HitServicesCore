using System;

namespace HitServicesCore.Models;

public class InitializersLastUpdateModel
{
	public Guid plugInId { get; set; }

	public string latestUpdate { get; set; }

	public DateTime latestUpdateDate { get; set; }
}
