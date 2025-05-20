using HitServicesCore.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;

namespace HitServicesCore.Filters;

public class LoginFilter : IActionFilter, IFilterMetadata
{
	private readonly LoginsUsers loginsUsers;

	public LoginFilter(LoginsUsers _loginsUsers)
	{
		loginsUsers = _loginsUsers;
	}

	public void OnActionExecuted(ActionExecutedContext context)
	{
		dynamic val = loginsUsers.logins["isAdmin"] == true;
		if (!(val ? true : false) && !((val | (loginsUsers.logins["isAdmin"] == false)) ? true : false) && ((loginsUsers.logins["isAdmin"] == null) ? true : false))
		{
			context.Result = new RedirectToRouteResult(new RouteValueDictionary(new
			{
				action = "Index",
				controller = "Login"
			}));
		}
	}

	public void OnActionExecuting(ActionExecutingContext context)
	{
	}
}
