using System;
using HitServicesCore.Enums;

namespace HitServicesCore.Models.IS_Services;

public class ISServiceGeneralModel
{
	public string FullClassName { get; set; }

	public string ClassType { get; set; }

	public string ClassDescription { get; set; }

	public Guid serviceId { get; set; }

	public string serviceName { get; set; }

	public HangFireServiceTypeEnum serviceType { get; set; }

	public string sendEmailTo { get; set; }

	public string emailSubject { get; set; }

	public bool? sendEmailOnSuccess { get; set; }

	public bool? sendEmailOnFailure { get; set; }

	public bool? sendAttachedFiles { get; set; }

	public long? serviceVersion { get; set; }
}
