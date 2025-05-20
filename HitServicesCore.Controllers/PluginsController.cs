using System.Collections.Generic;
using HitHelpersNetCore.Models;
using HitServicesCore.Filters;
using HitServicesCore.Models;
using HitServicesCore.Models.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace HitServicesCore.Controllers;

public class PluginsController : Controller
{
	private readonly List<PlugInDescriptors> plugins;

	private readonly LoginsUsers loginsUsers;

	public PluginsController(ILogger<PluginsController> logger, List<PlugInDescriptors> _plugins, LoginsUsers loginsUsers)
	{
		plugins = _plugins;
		this.loginsUsers = loginsUsers;
	}

	[ServiceFilter(typeof(LoginFilter))]
	public IActionResult Index(bool usertype)
	{
		List<PluginHelper> plg = new List<PluginHelper>();
		foreach (PlugInDescriptors item in plugins)
		{
			plg.Add(new PluginHelper
			{
				plugIn_Id = item.mainDescriptor.plugIn_Id,
				plugIn_Description = item.mainDescriptor.plugIn_Description,
				plugIn_Name = item.mainDescriptor.plugIn_Name,
				plugIn_Version = item.mainDescriptor.plugIn_Version,
				routing = item.routing
			});
		}
		return View("Plugins", plg);
	}
}
