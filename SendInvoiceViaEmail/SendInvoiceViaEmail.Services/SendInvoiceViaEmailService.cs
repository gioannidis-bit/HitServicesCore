using System;
using HitCustomAnnotations.Classes;
using HitHelpersNetCore.Helpers;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using SendInvoiceViaEmail.MainLogic.Flows;

namespace SendInvoiceViaEmail.Services;

[SchedulerAnnotation("8B5A4C84-2D14-47B7-BB91-41D3630C858D", "SendInvoiceViaEmail", "Send protel invoices to customers using email", "1.0.1.0")]
public class SendInvoiceViaEmailService : ServiceExecutions
{
	public override void Start(Guid _serviceId)
	{
		IApplicationBuilder appBuilder = DIHelper.AppBuilder;
		if (appBuilder == null)
		{
			throw new Exception("General error. Cannot get the Application builder from Hist Services Core");
		}
		IServiceProvider applicationServices = appBuilder.ApplicationServices;
		if (applicationServices == null)
		{
			throw new Exception("General error. Cannot get the Services from Application builder for Hist Services Core");
		}
		SendInvoiceViaEmailFlow service = applicationServices.GetService<SendInvoiceViaEmailFlow>();
		service.InitFlow();
		service.SendEmailtoCustomer();
	}
}
