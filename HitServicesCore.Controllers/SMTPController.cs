using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HitHelpersNetCore.Classes;
using HitHelpersNetCore.Helpers;
using HitHelpersNetCore.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace HitServicesCore.Controllers;

public class SMTPController : Controller
{
	private readonly SmtpHelper _smhelper;

	private ILogger<SMTPController> _logger;

	private readonly EmailHelper ehelper;

	public SMTPController(SmtpHelper smhelper, ILogger<SMTPController> logger, EmailHelper ehelp)
	{
		_smhelper = smhelper;
		_logger = logger;
		ehelper = ehelp;
	}

	public IActionResult Configuration()
	{
		return View();
	}

	[HttpGet]
	public List<SmtpExtModel> GetConfigurations()
	{
		_smhelper.GetConfig();
		return _smhelper._smhelper;
	}

	[HttpPost]
	public async Task<IActionResult> TestEmail(TestEmail model)
	{
		_logger.LogInformation("Sending smtp email test");
		string err = "";
		try
		{
			ehelper.Init(model.smtp, Convert.ToInt32(model.port), model.ssl == "1", model.username, model.password);
			EmailSendModel email = new EmailSendModel();
			List<string> emailList = new List<string>();
			email.Subject = " Hit Services Core Email Test Subject";
			email.Body = " Hit Services Core Email Test Body";
			emailList.Add(model.testemail);
			email.From = model.sender;
			email.To = emailList;
			ehelper.Send(email);
		}
		catch (Exception ex)
		{
			_logger.LogError("Error sending test email from selected SMTP: " + Convert.ToString(ex.Message));
			_ = "Error sending test email from selected SMTP: " + Convert.ToString(ex.Message);
			throw;
		}
		return Ok(err);
	}
}
