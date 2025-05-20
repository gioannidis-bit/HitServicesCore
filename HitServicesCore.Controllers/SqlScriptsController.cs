using System;
using System.Collections.Generic;
using System.Linq;
using HitServicesCore.Enums;
using HitServicesCore.Helpers;
using HitServicesCore.Models;
using HitServicesCore.Models.IS_Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace HitServicesCore.Controllers;

public class SqlScriptsController : Controller
{
	private readonly ILogger<SqlScriptsController> logger;

	private readonly SystemInfo sysInfo;

	private object lockJsons = new object();

	private readonly List<SchedulerServiceModel> hangfireServices;

	public SqlScriptsController(SystemInfo _sysInfo, List<SchedulerServiceModel> _hangfireServices, ILogger<SqlScriptsController> _logger)
	{
		sysInfo = _sysInfo;
		hangfireServices = _hangfireServices;
		logger = _logger;
	}

	public IActionResult Index()
	{
		base.ViewBag.Title = "SQL Scripts";
		return View();
	}

	public void UpdateExistingSqlScript(ISRunSqlScriptsModel updatedmodel)
	{
		updatedmodel.ClassType = "Job";
		updatedmodel.serviceType = HangFireServiceTypeEnum.SqlScripts;
		if (!updatedmodel.serviceVersion.HasValue)
		{
			updatedmodel.serviceVersion = 1L;
		}
		try
		{
			IS_ServicesHelper serviceshelper = new IS_ServicesHelper();
			List<ISRunSqlScriptsModel> model = serviceshelper.GetRunSqlScriptsFromJsonFiles();
			model = model.Where((ISRunSqlScriptsModel x) => x.serviceName != updatedmodel.serviceName).ToList();
			model.Add(updatedmodel);
			serviceshelper.SaveRunsSqlScriptsJsons(model);
		}
		catch (Exception ex)
		{
			logger.LogError("Failed to update existing file with name =" + updatedmodel.serviceName);
			logger.LogError("Error:" + Convert.ToString(ex));
		}
	}

	public void CreateNewFile([FromForm] ISRunSqlScriptsModel model)
	{
		try
		{
			model.serviceVersion = 1L;
			IS_ServicesHelper serviceshelper = new IS_ServicesHelper();
			List<ISRunSqlScriptsModel> list = serviceshelper.GetRunSqlScriptsFromJsonFiles();
			list.Add(model);
			serviceshelper.SaveRunsSqlScriptsJsons(list);
		}
		catch (Exception ex)
		{
			logger.LogError("Failed to write new file with name =" + model.serviceName);
			logger.LogError("Error:" + Convert.ToString(ex));
		}
	}
}
