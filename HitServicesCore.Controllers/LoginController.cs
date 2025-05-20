using HitServicesCore.Filters;
using HitServicesCore.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace HitServicesCore.Controllers;

public class LoginController : Controller
{
	public class mainLoginsHelper
	{
		private string user { get; set; }

		private string pass { get; set; }
	}

	private readonly LoginsUsers loginsUsers;

	public LoginController(ILogger<LoginController> logger, LoginsUsers _loginsUsers)
	{
		loginsUsers = _loginsUsers;
	}

	[ServiceFilter(typeof(LoginFilter))]
	public IActionResult Logout(bool logout)
	{
		if (logout)
		{
			loginsUsers.logins["isAdmin"] = null;
		}
		base.ViewBag.isValid = false;
		return View("Login");
	}

	public IActionResult Index(bool error)
	{
		base.ViewBag.Version = GetType().Assembly.GetName().Version.ToString();
		base.ViewBag.isValid = error;
		return View("Login");
	}

	public IActionResult Validation(string username, string password)
	{
		if (username == null || password == null)
		{
			return RedirectToAction("Index", "Login", new
			{
				error = true
			});
		}
		bool isValid = false;
		if (username.Equals(loginsUsers.logins["Admin_Username"].ToString()) && password.Equals(loginsUsers.logins["Admin_Password"].ToString()))
		{
			loginsUsers.logins["isAdmin"] = true;
			isValid = true;
		}
		if (username.Equals(loginsUsers.logins["User_Username"].ToString()) && password.Equals(loginsUsers.logins["User_Password"].ToString()))
		{
			loginsUsers.logins["isAdmin"] = false;
			isValid = true;
		}
		if (isValid)
		{
			return RedirectToAction("Index", "Plugins");
		}
		return RedirectToAction("Index", "Login", new
		{
			error = true
		});
	}
}
