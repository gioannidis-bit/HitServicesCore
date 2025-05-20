using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SendInvoiceViaEmail.MainLogic.Flows;

namespace SendInvoiceViaEmail.Controllers;

[Route("api/[controller]")]
[ApiController]
public class SendInvoiceEmailController : ControllerBase
{
	private readonly ILogger<SendInvoiceEmailController> logger;

	private readonly SendInvoiceViaEmailFlow mainFlow;

	private HttpContext httpContext;

	public SendInvoiceEmailController(SendInvoiceViaEmailFlow mainFlow, ILogger<SendInvoiceEmailController> logger)
	{
		httpContext = base.HttpContext;
		this.logger = logger;
		this.mainFlow = mainFlow;
	}

	[HttpPost]
	[Route("SendInvoiceToCustomer")]
	public void SendInvoiceToCustomer()
	{
		try
		{
			if (httpContext == null)
			{
				httpContext = base.HttpContext;
			}
			int num = int.Parse(httpContext.Request.Headers["resNo"]);
			int num2 = int.Parse(httpContext.Request.Headers["winId"]);
			int num3 = int.Parse(httpContext.Request.Headers["fisccode"]);
			mainFlow.InitFlow();
			mainFlow.SendEmailtoCustomer();
		}
		catch (Exception ex)
		{
			logger.LogError(ex.ToString());
		}
	}
}
