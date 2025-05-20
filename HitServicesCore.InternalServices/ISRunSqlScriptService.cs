using System;
using System.Collections.Generic;
using Hangfire;
using HitCustomAnnotations.Classes;
using HitHelpersNetCore.Helpers;
using HitServicesCore.Helpers;
using HitServicesCore.MainLogic.Flows;
using HitServicesCore.Models.IS_Services;
using Microsoft.AspNetCore.Builder;

namespace HitServicesCore.InternalServices;

[SchedulerAnnotation("8e477a21-8853-47e1-be86-9b6de10e9718", "RunSqlScriptService", "Service to execute sql scripts based on IS_Services\\SqlScripts directory and all jsons included on it", "1.0.1.0")]
public class ISRunSqlScriptService : ServiceExecutions
{
	[AutomaticRetry(Attempts = 1, LogEvents = true, OnAttemptsExceeded = AttemptsExceededAction.Fail)]
	[DisableConcurrentExecution(30)]
	public override void Start(Guid _serviceId)
	{
		IApplicationBuilder _app = DIHelper.AppBuilder;
		IServiceProvider services = _app.ApplicationServices;
		IS_ServicesHelper isServicesHlp = new IS_ServicesHelper();
		List<ISRunSqlScriptsModel> runSqlServices = isServicesHlp.GetRunSqlScriptsFromJsonFiles();
		ISRunSqlScriptsModel currentService = runSqlServices.Find((ISRunSqlScriptsModel f) => f.serviceId == _serviceId);
		if (currentService != null)
		{
			SQLFlows sqlFlow = new SQLFlows(currentService);
			sqlFlow.RunScript(currentService.SqlScript, currentService.Custom1DB);
		}
	}
}
