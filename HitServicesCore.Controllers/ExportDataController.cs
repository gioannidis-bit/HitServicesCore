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

public class ExportDataController : Controller
{
	private readonly ILogger<ExportDataController> logger;

	private readonly SystemInfo sysInfo;

	private object lockJsons = new object();

	public ExportDataController(SystemInfo _sysInfo, ILogger<ExportDataController> _logger)
	{
		sysInfo = _sysInfo;
		logger = _logger;
	}

	public IActionResult Index()
	{
		base.ViewBag.Title = "Export Data";
		return View();
	}

	public IActionResult UpdateExistingExportDataScript(ISExportDataModel updatedmodel)
	{
		updatedmodel.ClassType = "Job";
		updatedmodel.serviceType = HangFireServiceTypeEnum.ExportData;
		if (!updatedmodel.serviceVersion.HasValue)
		{
			updatedmodel.serviceVersion = 1L;
		}
		try
		{
			IS_ServicesHelper serviceshelper = new IS_ServicesHelper();
			List<ISExportDataModel> model = serviceshelper.GetExportdataFromJsonFiles();
			model = model.Where((ISExportDataModel x) => x.serviceName != updatedmodel.serviceName).ToList();
			model.Add(updatedmodel);
			serviceshelper.SaveExportDataJsons(model);
		}
		catch (Exception ex)
		{
			logger.LogError("Failed to update existing file with name =" + updatedmodel.serviceName);
			logger.LogError("Error:" + Convert.ToString(ex));
			return BadRequest();
		}
		return Ok();
	}

	public void CreateNewExportDataFile(ISExportDataModel model)
	{
		try
		{
			model.serviceVersion = 1L;
			IS_ServicesHelper serviceshelper = new IS_ServicesHelper();
			List<ISExportDataModel> list = serviceshelper.GetExportdataFromJsonFiles();
			list.Add(model);
			serviceshelper.SaveExportDataJsons(list);
		}
		catch (Exception ex)
		{
			logger.LogError("Failed to write new file with name =" + model.serviceName);
			logger.LogError("Error:" + Convert.ToString(ex));
		}
	}
}
