using System;
using System.Collections.Generic;
using System.Linq;
using HitServicesCore.Enums;
using HitServicesCore.Helpers;
using HitServicesCore.Models.IS_Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace HitServicesCore.Controllers;

public class SaveToTableController : Controller
{
	private readonly ILogger<SaveToTableController> logger;

	public SaveToTableController(ILogger<SaveToTableController> _logger)
	{
		logger = _logger;
	}

	public IActionResult Index()
	{
		base.ViewBag.Title = "Save to Table";
		return View();
	}

	public void CreateNewSaveToTableFile(ISSaveToTableModel newmodel)
	{
		try
		{
			newmodel.serviceVersion = 1L;
			IS_ServicesHelper serviceshelper = new IS_ServicesHelper();
			List<ISSaveToTableModel> res = serviceshelper.GetSaveToTableFromJsonFiles();
			res.Add(newmodel);
			serviceshelper.SaveSaveToTableJsons(res);
		}
		catch (Exception ex)
		{
			logger.LogError("Exception new file could not be created");
			logger.LogError("Error:" + Convert.ToString(ex));
		}
	}

	public void UpdateSaveToTableFile(ISSaveToTableModel updatedmodel)
	{
		updatedmodel.ClassType = "Job";
		updatedmodel.serviceType = HangFireServiceTypeEnum.SaveToTable;
		if (!updatedmodel.serviceVersion.HasValue)
		{
			updatedmodel.serviceVersion = 1L;
		}
		try
		{
			IS_ServicesHelper serviceshelper = new IS_ServicesHelper();
			List<ISSaveToTableModel> model = serviceshelper.GetSaveToTableFromJsonFiles();
			model = model.Where((ISSaveToTableModel x) => x.serviceName != updatedmodel.serviceName).ToList();
			model.Add(updatedmodel);
			serviceshelper.SaveSaveToTableJsons(model);
		}
		catch (Exception ex)
		{
			logger.LogError("Failed to update existing file with name =" + updatedmodel.serviceName);
			logger.LogError("Error:" + Convert.ToString(ex));
		}
	}
}
