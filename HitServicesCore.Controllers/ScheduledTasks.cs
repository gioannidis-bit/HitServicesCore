using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HitServicesCore.Filters;
using HitServicesCore.Helpers;
using HitServicesCore.Models;
using Microsoft.AspNetCore.Mvc;
using NCrontab;

namespace HitServicesCore.Controllers;

public class ScheduledTasks : Controller
{
	public class schedulerHelper
	{
		public string stars { get; set; }

		public string starsDesc { get; set; }
	}

	private readonly List<SchedulerServiceModel> scheduledTasks;

	private readonly HangFire_ManageServices hangfire;

	private static string currentServiceId;

	public ScheduledTasks(List<SchedulerServiceModel> _scheduledTasks, HangFire_ManageServices _hangfire)
	{
		scheduledTasks = _scheduledTasks;
		hangfire = _hangfire;
	}

	[ServiceFilter(typeof(LoginFilter))]
	public IActionResult Index(string serviceId)
	{
		if (string.IsNullOrWhiteSpace(serviceId) || serviceId == "undefined")
		{
			return View("~/Views/Services/Index.cshtml");
		}
		SchedulerServiceModel model = scheduledTasks.Where((SchedulerServiceModel x) => x.serviceId == new Guid(serviceId)).FirstOrDefault();
		if (model == null)
		{
			return View("~/Views/Services/Index.cshtml");
		}
		base.ViewBag.schedulerDescr = model.schedulerDescr;
		if (CrontabSchedule.TryParse(model.schedulerTime) == null)
		{
			model.schedulerTime = "* * * * *";
		}
		base.ViewBag.occurences = ParseCron(model.schedulerTime);
		if (serviceId == null)
		{
			currentServiceId = Convert.ToString(scheduledTasks[0].serviceId);
		}
		else
		{
			currentServiceId = serviceId;
		}
		base.ViewBag.ScheduledJob = model.serviceName;
		base.ViewBag.ScheduledTime = model.schedulerTime;
		string str = Convert.ToString(base.ViewBag.ScheduledTime);
		string[] currentTime = str.Split((char[]?)null);
		List<string> manualTime = new List<string>();
		string[] array = currentTime;
		foreach (string s in array)
		{
			if (s.Contains("*/"))
			{
				manualTime.Add(s.Substring(2));
			}
			else
			{
				manualTime.Add(s);
			}
		}
		base.ViewBag.currentTime = currentTime;
		base.ViewBag.manualTime = manualTime;
		base.ViewBag.ScheduledTasks = scheduledTasks;
		return View("ScheduledTasks");
	}

	[HttpPost]
	public async Task<IActionResult> ScheduleJob(schedulerHelper obj)
	{
		SchedulerServiceModel currentEditedService = scheduledTasks.Where((SchedulerServiceModel x) => x.serviceId == new Guid(currentServiceId)).FirstOrDefault();
		scheduledTasks.Remove(currentEditedService);
		currentEditedService.schedulerTime = obj.stars;
		currentEditedService.schedulerDescr = obj.starsDesc;
		scheduledTasks.Add(currentEditedService);
		hangfire.SaveSchedulersJobs(scheduledTasks);
		return Ok();
	}

	private object ParseCron(string cron)
	{
		CrontabSchedule s = null;
		try
		{
			s = CrontabSchedule.Parse(cron);
		}
		catch (Exception)
		{
		}
		DateTime start = DateTime.Now;
		DateTime end = start.AddMonths(25);
		return (from x in s.GetNextOccurrences(start, end).Take(500)
			select x.ToString("ddd, dd MMM yyyy  HH:mm")).ToList();
	}
}
