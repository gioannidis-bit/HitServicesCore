using System.Collections.Generic;
using HitHelpersNetCore.Classes;
using HitHelpersNetCore.Helpers;
using HitHelpersNetCore.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace HitServicesCore.Controllers;

public class FTPController : Controller
{
	private readonly FtpHelper _ftpHelper;

	private ILogger<FTPController> _logger;

	private readonly EmailHelper ehelper;

	public FTPController(FtpHelper ftphelper, ILogger<FTPController> logger, EmailHelper ehelp)
	{
		_ftpHelper = ftphelper;
		_logger = logger;
		ehelper = ehelp;
	}

	public IActionResult Configuration()
	{
		return View();
	}

	[HttpGet]
	public List<FtpExtModel> GetConfigurations()
	{
		_ftpHelper.GetConfig();
		return _ftpHelper._ftpHelper;
	}
}
