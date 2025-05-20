using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.Json;
using Hangfire;
using Hangfire.Common;
using HitHelpersNetCore.Helpers;
using HitServicesCore.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace HitServicesCore.Helpers;

public class HangFire_ManageServices
{
	private List<SchedulerServiceModel> hangFireServices;

	private IRecurringJobManager hangFire;

	private IBackgroundJobClient jobClient;

	private string CurrentPath;

	private readonly EncryptionHelper eh;

	private object lockJsons = new object();

	private ILogger<HangFire_ManageServices> logger;

	public HangFire_ManageServices(List<SchedulerServiceModel> _hangFireServices, IRecurringJobManager _hangFire, IBackgroundJobClient _jobClient)
	{
		hangFireServices = _hangFireServices;
		hangFire = _hangFire;
		jobClient = _jobClient;
		CurrentPath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
		Directory.SetCurrentDirectory(CurrentPath);
		eh = new EncryptionHelper();
		CheckLogger();
	}

	private void CheckLogger()
	{
		if (logger == null)
		{
			IApplicationBuilder _app = DIHelper.AppBuilder;
			if (_app != null)
			{
				IServiceProvider services = _app.ApplicationServices;
				logger = services.GetService<ILogger<HangFire_ManageServices>>();
			}
		}
	}

	public void AddServicesToHangFire()
	{
		try
		{
			CheckLogger();
			foreach (SchedulerServiceModel item in hangFireServices)
			{
				if (item.isActive)
				{
					Type LoadType = Type.GetType(item.classFullName + ", " + item.assemblyFileName);
					if (LoadType == null)
					{
						logger.LogError(">>>>>>> Class :" + item.description + " not found !!!");
					}
					object instance = Activator.CreateInstance(LoadType);
					MethodInfo method = LoadType.GetMethod("Start");
					Job hfjob = new Job(LoadType, method, item.serviceId);
					hangFire.AddOrUpdate(item.serviceName + " (" + item.serviceId.ToString() + ")", hfjob, item.schedulerTime, TimeZoneInfo.Local);
				}
			}
		}
		catch (Exception ex)
		{
			logger.LogError(ex.ToString());
		}
	}

	public void LoadServices()
	{
		AddServicesToHangFire();
	}

	private void ReloadHangFireServices()
	{
		foreach (SchedulerServiceModel item in hangFireServices)
		{
			if (!item.isActive)
			{
				hangFire.RemoveIfExists(item.serviceName + " (" + item.serviceId.ToString() + ")");
			}
		}
		AddServicesToHangFire();
	}

	public bool SaveSchedulersJobs(List<SchedulerServiceModel> jobs)
	{
		bool result = true;
		CheckLogger();
		try
		{
			lock (lockJsons)
			{
				string sFileName = Path.GetFullPath(Path.Combine(new string[3] { CurrentPath, "Config", "scheduler.json" }));
				if (!File.Exists(sFileName))
				{
					File.WriteAllText(sFileName, " ");
				}
				string sVal = JsonSerializer.Serialize(jobs);
				sVal = eh.Encrypt(sVal);
				File.WriteAllText(sFileName, sVal);
				foreach (SchedulerServiceModel item in jobs)
				{
					SchedulerServiceModel tmp = FillEmptyValues(item);
					SchedulerServiceModel fld = hangFireServices.Find((SchedulerServiceModel f) => f.serviceId == item.serviceId);
					if (fld == null)
					{
						hangFireServices.Add(tmp);
					}
					else
					{
						fld = tmp;
					}
				}
				ReloadHangFireServices();
			}
		}
		catch (Exception ex)
		{
			logger.LogError(ex.ToString());
			result = false;
		}
		return result;
	}

	private SchedulerServiceModel FillEmptyValues(SchedulerServiceModel model)
	{
		SchedulerServiceModel fld = hangFireServices.Find((SchedulerServiceModel f) => f.serviceId == model.serviceId);
		if (fld != null)
		{
			if (string.IsNullOrWhiteSpace(model.assemblyFileName))
			{
				model.assemblyFileName = fld.assemblyFileName;
			}
			if (string.IsNullOrWhiteSpace(model.classFullName))
			{
				model.classFullName = fld.classFullName;
			}
			if (string.IsNullOrWhiteSpace(model.description))
			{
				model.description = fld.description;
			}
			if (string.IsNullOrWhiteSpace(model.serviceVersion))
			{
				model.serviceVersion = fld.serviceVersion;
			}
			if (string.IsNullOrWhiteSpace(model.serviceName))
			{
				model.serviceName = fld.serviceName;
			}
		}
		return model;
	}

	public string FireAndForget(Guid serviceId)
	{
		try
		{
			CheckLogger();
			SchedulerServiceModel fld = hangFireServices.Find((SchedulerServiceModel f) => f.serviceId == serviceId);
			if (fld == null)
			{
				ILogger<HangFire_ManageServices> obj = logger;
				Guid guid = serviceId;
				obj.LogInformation("Service with Id " + guid.ToString(), ToString() + " not found");
				guid = serviceId;
				return "Service with Id " + guid.ToString() + " not found";
			}
			Type LoadType = Type.GetType(fld.classFullName + ", " + fld.assemblyFileName);
			if (LoadType == null)
			{
				logger.LogError(">>>>>>> Class :" + fld.description + " not found !!!");
				return "Error on Fire And Forget  >>>>>>> Class :" + fld.description + " not found !!!";
			}
			ServiceExecutions instance = (ServiceExecutions)Activator.CreateInstance(LoadType);
			logger.LogInformation("Executing (Fire-and-Forget) Job: " + fld.ToString() + "...");
			BackgroundJob.Enqueue(() => instance.Start(fld.serviceId));
			return "OK";
		}
		catch (Exception ex)
		{
			logger.LogError("Error on Fire And Forget  " + Convert.ToString(ex));
			return "Error on Fire And Forget  " + Convert.ToString(ex);
		}
	}
}
