using System;
using System.Collections.Generic;
using Hangfire;
using HitCustomAnnotations.Classes;
using HitHelpersNetCore.Helpers;
using HitServicesCore.Helpers;
using HitServicesCore.MainLogic.Flows;
using HitServicesCore.Models.IS_Services;

namespace HitServicesCore.InternalServices;

[SchedulerAnnotation("6cf39393-9e3d-4ec2-bd92-5be81b2eaadc", "SaveToTableService", "Service to save data from a database to another based on IS_Services\\SaveToTable directory and all jsons included on it", "1.0.1.0")]
public class ISSaveToTableService : ServiceExecutions
{
	[AutomaticRetry(Attempts = 1, LogEvents = true, OnAttemptsExceeded = AttemptsExceededAction.Fail)]
	[DisableConcurrentExecution(30)]
	public override void Start(Guid _serviceId)
	{
		IS_ServicesHelper isServicesHlp = new IS_ServicesHelper();
		List<ISSaveToTableModel> saveToTableServices = isServicesHlp.GetSaveToTableFromJsonFiles();
		ISSaveToTableModel currentService = saveToTableServices.Find((ISSaveToTableModel f) => f.serviceId == _serviceId);
		if (currentService != null)
		{
			SaveDataToDBFlow flow = new SaveDataToDBFlow(currentService);
			flow.SaveDataToDB();
		}
	}
}
