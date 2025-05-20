using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using HitHelpersNetCore.Helpers;
using HitHelpersNetCore.Models;
using HitServicesCore.Helpers;
using HitServicesCore.Models;
using HitServicesCore.Models.Helpers;
using HitServicesCore.Models.IS_Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace HitServicesCore.Controllers;

[Route("[controller]")]
[ApiController]
public class FetchDataApiController : ControllerBase
{
	private readonly List<PlugInDescriptors> plugins;

	private readonly ManageConfiguration manconf;

	private ILogger<FetchDataApiController> logger;

	private readonly SystemInfo sysInfo;

	private readonly List<SchedulerServiceModel> hangfireServices;

	private readonly HangFire_ManageServices hangfire;

	private readonly SmtpHelper _smhelper;

	private readonly FtpHelper _ftpHelper;

	public FetchDataApiController(ILogger<FetchDataApiController> logger, HangFire_ManageServices _hangfire, List<PlugInDescriptors> _plugins, ManageConfiguration _manconf, SystemInfo _sysInfo, List<SchedulerServiceModel> _hangfireServices, ILogger<FetchDataApiController> _logger, SmtpHelper smhelper, FtpHelper ftpHelper)
	{
		plugins = _plugins;
		manconf = _manconf;
		this.logger = logger;
		hangfire = _hangfire;
		hangfireServices = _hangfireServices;
		hangfireServices = hangfireServices;
		sysInfo = _sysInfo;
		_smhelper = smhelper;
		_ftpHelper = ftpHelper;
	}

	[Route("/FetchDataApi/dataApi/GetPlugins")]
	[HttpGet]
	public async Task<string> GetPlugins(long id)
	{
		List<PlugInDescriptors> plg = plugins;
		List<PluginHelper> mainDesc = new List<PluginHelper>();
		foreach (PlugInDescriptors model in plg)
		{
			PluginHelper tempModel = new PluginHelper();
			tempModel.plugIn_Id = model.mainDescriptor.plugIn_Id;
			tempModel.plugIn_Description = model.mainDescriptor.plugIn_Description;
			tempModel.plugIn_Name = model.mainDescriptor.plugIn_Name;
			tempModel.plugIn_Version = model.mainDescriptor.plugIn_Version;
			tempModel.routing = model.routing;
			mainDesc.Add(tempModel);
		}
		try
		{
			return JsonSerializer.Serialize(mainDesc);
		}
		catch (Exception ex)
		{
			ex.ToString();
			return null;
		}
	}

	[Route("/FetchDataApi/dataApi/FireJobExternally/{guid}")]
	[HttpGet]
	public async Task<bool> FireJobExternally(Guid guid)
	{
		try
		{
			foreach (SchedulerServiceModel service in hangfireServices)
			{
				if (service.serviceId == guid)
				{
					hangfire.FireAndForget(guid);
					break;
				}
			}
		}
		catch (Exception ex)
		{
			ILogger<FetchDataApiController> obj = logger;
			Guid guid2 = guid;
			obj.LogError("Service with ServiceId" + guid2.ToString() + " did not start");
			logger.LogError(Convert.ToString(ex));
			return false;
		}
		return true;
	}

	[Route("/FetchDataApi/dataApi/test")]
	[HttpGet]
	[AllowAnonymous]
	public IActionResult test()
	{
		return Ok("The test is ok");
	}

	[Route("GetSqlScripts")]
	[HttpGet]
	public async Task<List<ISRunSqlScriptsModel>> GetSqlScripts()
	{
		try
		{
			IS_ServicesHelper serviceshelper = new IS_ServicesHelper();
			return serviceshelper.GetRunSqlScriptsFromJsonFiles();
		}
		catch (Exception ex)
		{
			logger.LogError("Failed to get the list of existing Sql Scripts ");
			logger.LogError("Error:" + Convert.ToString(ex));
			return null;
		}
	}

	[Route("/FetchDataApi/dataApi/GetSaveToTable")]
	[HttpGet]
	public async Task<List<ISSaveToTableModel>> GetSaveToTable()
	{
		try
		{
			IS_ServicesHelper serviceshelper = new IS_ServicesHelper();
			return serviceshelper.GetSaveToTableFromJsonFiles();
		}
		catch (Exception ex)
		{
			logger.LogError("Failed to get the list of existing SaveToTable Scripts ");
			logger.LogError("Error:" + Convert.ToString(ex));
			return null;
		}
	}

	[Route("/FetchDataApi/dataApi/GetReadFromCsv")]
	[HttpGet]
	public async Task<List<ISReadFromCsvModel>> GetReadFromCsv()
	{
		try
		{
			IS_ServicesHelper serviceshelper = new IS_ServicesHelper();
			return serviceshelper.GetReadFromCsvFromJsonFiles();
		}
		catch (Exception ex)
		{
			logger.LogError("Failed to get the list of existing ReadFromCsv Scripts ");
			logger.LogError("Error:" + Convert.ToString(ex));
			return null;
		}
	}

	[Route("/FetchDataApi/dataApi/GetExportData")]
	[HttpGet]
	public async Task<List<ISExportDataModel>> GetExportData()
	{
		try
		{
			IS_ServicesHelper serviceshelper = new IS_ServicesHelper();
			return serviceshelper.GetExportdataFromJsonFiles();
		}
		catch (Exception ex)
		{
			logger.LogError("Failed to get the list of existing ExportData Scripts ");
			logger.LogError("Error:" + Convert.ToString(ex));
			return null;
		}
	}

	[Route("Get")]
	[HttpGet]
	public async Task<string> GetMappedConfigurationData(long id)
	{
		List<ConfigurationHelper> MappedConfigList = new List<ConfigurationHelper>();
		List<MainConfigurationModel> configList = manconf.GetConfigs();
		foreach (MainConfigurationModel model in configList)
		{
			if (model.descriptors.descriptions.Count == 0)
			{
				continue;
			}
			string pluginid = Convert.ToString(model.plugInId);
			foreach (KeyValuePair<string, List<DescriptorsModel>> descriptor in model.descriptors.descriptions)
			{
				_ = descriptor.Key;
				foreach (DescriptorsModel item in descriptor.Value)
				{
					_ = item;
					ConfigurationHelper mapper = new ConfigurationHelper();
					mapper.PlugInId = pluginid;
					foreach (KeyValuePair<string, object> item2 in model.config.config)
					{
						_ = item2;
					}
				}
			}
		}
		try
		{
			return JsonSerializer.Serialize(MappedConfigList);
		}
		catch (Exception ex)
		{
			ex.ToString();
			return null;
		}
	}

	[Route("Post")]
	[HttpPost]
	public IActionResult Post(ConfigurationHelper data)
	{
		List<MainConfigurationModel> configList = manconf.GetConfigs();
		MainConfigurationModel tempmodel = configList.Where((MainConfigurationModel x) => x.plugInId == new Guid(data.PlugInId)).FirstOrDefault();
		configList.Remove(tempmodel);
		Dictionary<string, dynamic> dic = tempmodel.config.config;
		try
		{
			foreach (KeyValuePair<string, List<DescriptorsModel>> description in tempmodel.descriptors.descriptions)
			{
				foreach (DescriptorsModel mod in description.Value)
				{
				}
			}
		}
		catch (Exception ex)
		{
			logger.LogError(ex.ToString());
		}
		configList.Remove(tempmodel);
		tempmodel.config.config = dic;
		configList.Add(tempmodel);
		return Ok();
	}

	[HttpPut]
	public IActionResult Put(string key, string values)
	{
		return Ok();
	}

	[Route("/FetchDataApi/GetSmtpConfigurations")]
	[HttpGet]
	public List<SmtpExtModel> GetSmtpConfigurations()
	{
		_smhelper.GetConfig();
		return _smhelper._smhelper;
	}

	[Route("/FetchDataApi/SaveSmtpConfiguration")]
	[HttpPost]
	public IActionResult SaveSmtpConfiguration(List<SmtpExtModel> model)
	{
		logger.LogInformation("Saving smtp configuration");
		try
		{
			_smhelper.SaveSmtps(model);
			return Ok("Configuration saved");
		}
		catch (Exception ex)
		{
			logger.LogError("Error saving smtp configuration: " + Convert.ToString(ex));
			return Ok("Error saving smtp configuration");
		}
	}

	[Route("/FetchDataApi/DeleteSmtpConfiguration")]
	[HttpDelete]
	public IActionResult DeleteSmtpConfiguration(string alias)
	{
		try
		{
			_smhelper.GetConfig();
			_smhelper.SaveSmtps(_smhelper._smhelper.Where((SmtpExtModel r) => r.alias != alias).ToList());
		}
		catch (Exception ex)
		{
			return BadRequest(ex.ToString());
		}
		return Ok();
	}

	[Route("/FetchDataApi/GetFtpConfigurations")]
	[HttpGet]
	public List<FtpExtModel> GetFtpConfigurations()
	{
		_ftpHelper.GetConfig();
		return _ftpHelper._ftpHelper;
	}

	[Route("/FetchDataApi/SaveFtpConfiguration")]
	[HttpPost]
	public IActionResult SaveFtpConfiguration(List<FtpExtModel> model)
	{
		logger.LogInformation("Saving ftp configuration");
		try
		{
			_ftpHelper.SaveConfig(model);
			return Ok("Configuration saved");
		}
		catch (Exception ex)
		{
			logger.LogError("Error saving ftp configuration: " + Convert.ToString(ex));
			return Ok("Error saving ftp configuration");
		}
	}

	[Route("/FetchDataApi/DeleteFtpConfiguration")]
	[HttpDelete]
	public IActionResult DeleteFtpConfiguration(string alias)
	{
		try
		{
			_ftpHelper.GetConfig();
			_ftpHelper.SaveConfig(_ftpHelper._ftpHelper.Where((FtpExtModel r) => r.alias != alias).ToList());
		}
		catch (Exception ex)
		{
			return BadRequest(ex.ToString());
		}
		return Ok();
	}
}
