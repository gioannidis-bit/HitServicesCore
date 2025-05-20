using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HitServicesCore.Filters;
using HitServicesCore.Helpers;
using HitServicesCore.Models;
using Microsoft.AspNetCore.Mvc;

namespace HitServicesCore.Controllers;

public class ServicesController : Controller
{
	public class ServiceId
	{
		public string serviceId { get; set; }
	}

	private readonly List<SchedulerServiceModel> scheduledTasks;

	private readonly HangFire_ManageServices hangfire;

	public ServicesController(List<SchedulerServiceModel> _scheduledTasks, HangFire_ManageServices _hangfire)
	{
		scheduledTasks = _scheduledTasks;
		hangfire = _hangfire;
	}

	[ServiceFilter(typeof(LoginFilter))]
	public IActionResult Index(string error)
	{
		if (error == null)
		{
			error = "";
		}
		scheduledTasks.OrderBy((SchedulerServiceModel x) => x.serviceName);
		base.ViewBag.ScheduledTasks = scheduledTasks;
		base.ViewBag.error = error;
		return View();
	}

	[ServiceFilter(typeof(LoginFilter))]
	public IActionResult Redirect()
	{
		scheduledTasks.OrderBy((SchedulerServiceModel x) => x.serviceName);
		base.ViewBag.ScheduledTasks = scheduledTasks;
		return View("Index");
	}

	[ServiceFilter(typeof(LoginFilter))]
	[HttpPost]
	public async Task<ActionResult> FireAndForget(ServiceId model)
	{
		if (string.IsNullOrWhiteSpace(model.serviceId) || model.serviceId == "undefined")
		{
			return Ok("OK");
		}
		string res = hangfire.FireAndForget(new Guid(model.serviceId));
		return Ok(res);
	}

	[ServiceFilter(typeof(LoginFilter))]
	public IActionResult ChangeStatusToActive(string serviceId)
	{
		if (string.IsNullOrWhiteSpace(serviceId) || serviceId == "undefined")
		{
			return RedirectToAction("Index", "Services");
		}
		SchedulerServiceModel currentEditedService = scheduledTasks.Where((SchedulerServiceModel x) => x.serviceId == new Guid(serviceId)).FirstOrDefault();
		scheduledTasks.Remove(currentEditedService);
		currentEditedService.isActive = true;
		scheduledTasks.Add(currentEditedService);
		hangfire.SaveSchedulersJobs(scheduledTasks);
		return RedirectToAction("Index", "Services");
	}

	[ServiceFilter(typeof(LoginFilter))]
	public IActionResult ChangeStatusToInActive(string serviceId)
	{
		SchedulerServiceModel currentEditedService = scheduledTasks.Where((SchedulerServiceModel x) => x.serviceId == new Guid(serviceId)).FirstOrDefault();
		scheduledTasks.Remove(currentEditedService);
		scheduledTasks.Add(currentEditedService);
		currentEditedService.isActive = false;
		hangfire.SaveSchedulersJobs(scheduledTasks);
		return RedirectToAction("Index", "Services");
	}
}
