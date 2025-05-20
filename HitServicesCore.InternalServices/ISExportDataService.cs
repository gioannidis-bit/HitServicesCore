using System;
using System.Collections.Generic;
using Hangfire;
using HitCustomAnnotations.Classes;
using HitHelpersNetCore.Helpers;
using HitServicesCore.Helpers;
using HitServicesCore.MainLogic.Flows;
using HitServicesCore.Models.IS_Services;

namespace HitServicesCore.InternalServices;

[SchedulerAnnotation("2582590e-7314-4077-bc92-748710db9ef5", "ExportDataService", "Service to export data based on IS_Services\\ExportData directory and all jsons included on it", "1.0.1.0")]
public class ISExportDataService : ServiceExecutions
{
	[AutomaticRetry(Attempts = 1, LogEvents = true, OnAttemptsExceeded = AttemptsExceededAction.Fail)]
	[DisableConcurrentExecution(30)]
	public override void Start(Guid _serviceId)
	{
		IS_ServicesHelper isServicesHlp = new IS_ServicesHelper();
		List<ISExportDataModel> exportDataServices = isServicesHlp.GetExportdataFromJsonFiles();
		ISExportDataModel currentService = exportDataServices.Find((ISExportDataModel f) => f.serviceId == _serviceId);
		if (currentService != null)
		{
			ExportDataFlows flow = new ExportDataFlows(currentService);
			flow.ExportData();
		}
	}
}
