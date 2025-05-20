using System;
using System.Collections.Generic;
using Hangfire;
using HitCustomAnnotations.Classes;
using HitHelpersNetCore.Helpers;
using HitServicesCore.Helpers;
using HitServicesCore.MainLogic.Flows;
using HitServicesCore.Models.IS_Services;

namespace HitServicesCore.InternalServices;

[SchedulerAnnotation("4f83485b-89b6-46c2-8612-d369f6388a52", "readCsvService", "Service to read data from a csv file and import them to a database based on IS_Services\\ReadCsv directory and all jsons included on it", "1.0.1.0")]
public class ISReadCsvService : ServiceExecutions
{
	[AutomaticRetry(Attempts = 1, LogEvents = true, OnAttemptsExceeded = AttemptsExceededAction.Fail)]
	[DisableConcurrentExecution(30)]
	public override void Start(Guid _serviceId)
	{
		IS_ServicesHelper isServicesHlp = new IS_ServicesHelper();
		List<ISReadFromCsvModel> readFromCsvServices = isServicesHlp.GetReadFromCsvFromJsonFiles();
		ISReadFromCsvModel currentService = readFromCsvServices.Find((ISReadFromCsvModel f) => f.serviceId == _serviceId);
		if (currentService != null)
		{
			ReadCsvFlows flow = new ReadCsvFlows(currentService);
			flow.ReadFromCsv();
		}
	}
}
