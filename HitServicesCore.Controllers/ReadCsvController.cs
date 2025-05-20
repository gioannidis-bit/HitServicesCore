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

public class ReadCsvController : Controller
{
	private readonly ILogger<ReadCsvController> logger;

	private readonly SystemInfo sysInfo;

	private object lockJsons = new object();

	public ReadCsvController(SystemInfo _sysInfo, ILogger<ReadCsvController> _logger)
	{
		sysInfo = _sysInfo;
		logger = _logger;
	}

	public IActionResult Index()
	{
		base.ViewBag.Title = "Read CSV";
		return View();
	}

	public void UpdateExistingReadFromCsvScript(ISReadFromCsvModel updatedmodel)
	{
		updatedmodel.ClassType = "Job";
		updatedmodel.serviceType = HangFireServiceTypeEnum.ReadFromCsv;
		if (!updatedmodel.serviceVersion.HasValue)
		{
			updatedmodel.serviceVersion = 1L;
		}
		try
		{
			IS_ServicesHelper serviceshelper = new IS_ServicesHelper();
			List<ISReadFromCsvModel> model = serviceshelper.GetReadFromCsvFromJsonFiles();
			model = model.Where((ISReadFromCsvModel x) => x.serviceName != updatedmodel.serviceName).ToList();
			model.Add(updatedmodel);
			serviceshelper.SaveReadFromCsvJsons(model);
		}
		catch (Exception ex)
		{
			logger.LogError("Failed to update existing file with name =" + updatedmodel.serviceName);
			logger.LogError("Error:" + Convert.ToString(ex));
		}
	}

	public void CreateNewReadCsvFile(ISReadFromCsvModel model)
	{
		try
		{
			model.serviceVersion = 1L;
			IS_ServicesHelper serviceshelper = new IS_ServicesHelper();
			List<ISReadFromCsvModel> list = serviceshelper.GetReadFromCsvFromJsonFiles();
			list.Add(model);
			serviceshelper.SaveReadFromCsvJsons(list);
		}
		catch (Exception ex)
		{
			logger.LogError("Failed to write new file with name =" + model.serviceName);
			logger.LogError("Error:" + Convert.ToString(ex));
		}
	}
}
