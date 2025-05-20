using System;
using HitServicesCore.Helpers;
using HitServicesCore.Models;
using HitServicesCore.Models.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace HitServicesCore.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ValidationController : ControllerBase
{
	private readonly ILogger<ValidationController> logger;

	private readonly ValidationHelper validationHelper;

	public ValidationController(ValidationHelper validationHelper, ILogger<ValidationController> logger)
	{
		this.validationHelper = validationHelper;
		this.logger = logger;
	}

	[HttpPost]
	[Route("Validate")]
	[AllowAnonymous]
	public bool Validate(ValidationModel model)
	{
		try
		{
			return validationHelper.Validate(model);
		}
		catch (Exception ex)
		{
			logger.LogError(ex.ToString());
			return false;
		}
	}

	[HttpPost]
	[Route("Create")]
	public IActionResult Create(LicenseForCreateModel model)
	{
		try
		{
			return Ok(validationHelper.GenerateData(model));
		}
		catch (Exception ex)
		{
			logger.LogError(ex.ToString());
			return BadRequest();
		}
	}
}
