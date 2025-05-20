using System;
using System.Collections.Generic;
using System.Linq;
using HitHelpersNetCore.Helpers;
using HitHelpersNetCore.Models;
using HitServicesCore.Filters;
using HitServicesCore.Helpers;
using HitServicesCore.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace HitServicesCore.Controllers;

public class InitializeDBController : Controller
{
	private readonly InitializerHelper ihelper;

	private readonly List<PlugInDescriptors> plugins;

	private readonly IApplicationBuilder app;

	private ILogger<InitializeDBController> logger;

	private DIHelper diHelper;

	public InitializeDBController(InitializerHelper _ihelper, List<PlugInDescriptors> _plugins, DIHelper diHelper, ILogger<InitializeDBController> _logger)
	{
		ihelper = _ihelper;
		plugins = _plugins;
		this.diHelper = diHelper;
		logger = _logger;
	}

	[ServiceFilter(typeof(LoginFilter))]
	public IActionResult InitializeDB(string error)
	{
		if (error != null)
		{
			base.ViewBag.error = error;
		}
		List<InitializeDBModel> data = GenerateModel(plugins);
		base.ViewBag.Success = error;
		return View(data);
	}

	public List<InitializeDBModel> GenerateModel(List<PlugInDescriptors> data)
	{
		List<InitializeDBModel> res = new List<InitializeDBModel>();
		foreach (PlugInDescriptors record in data)
		{
			if (record.initialerDescriptor != null)
			{
				res.Add(new InitializeDBModel
				{
					plugInId = record.mainDescriptor.plugIn_Id,
					plugIn_Name = record.mainDescriptor.plugIn_Name,
					dbVersion = record.initialerDescriptor.dbVersion,
					latestUpdate = record.initialerDescriptor.latestUpdate,
					latestUpdateDate = record.initialerDescriptor.latestUpdateDate
				});
			}
		}
		return res;
	}

	[ServiceFilter(typeof(LoginFilter))]
	public IActionResult Initialize(string pluginId)
	{
		string error = null;
		if (pluginId != null)
		{
			logger.LogInformation("Initializing DB of PlugIn with id " + pluginId);
			string dbv1 = plugins.Where((PlugInDescriptors x) => x.mainDescriptor.plugIn_Id == new Guid(pluginId)).FirstOrDefault().initialerDescriptor.dbVersion;
			Version dbVersion = new Version(dbv1);
			string dbv2 = plugins.Where((PlugInDescriptors x) => x.mainDescriptor.plugIn_Id == new Guid(pluginId)).FirstOrDefault().initialerDescriptor.latestUpdate;
			Version currVersion = new Version(dbv2);
			if (currVersion < dbVersion)
			{
				try
				{
					ihelper.RunInitialMethod(new Guid(pluginId), DIHelper.AppBuilder);
				}
				catch (Exception ex)
				{
					error = ex.Message;
					if (ex.InnerException != null)
					{
						error = error + " " + ex.InnerException.Message;
					}
					return RedirectToAction("InitializeDB", "InitializeDB", new { error });
				}
			}
		}
		error = "Initialization complete.";
		return RedirectToAction("InitializeDB", "InitializeDB", new { error });
	}
}
