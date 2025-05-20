using System;
using HitServicesCore.Enums;

namespace HitServicesCore.Models;

public class SchedulerServiceModel
{
	public Guid serviceId { get; set; }

	public string serviceName { get; set; }

	public string classFullName { get; set; }

	public string assemblyFileName { get; set; }

	public string description { get; set; }

	public bool isActive { get; set; }

	public string schedulerTime { get; set; }

	public string schedulerDescr { get; set; }

	public string serviceVersion { get; set; }

	public HangFireServiceTypeEnum serviceType { get; set; }
}
