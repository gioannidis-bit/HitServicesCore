using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.Json;
using HitHelpersNetCore.Helpers;
using HitHelpersNetCore.Models;
using HitServicesCore.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace HitServicesCore.Helpers;

public class ManageConfiguration
{
	private string CurrentPath;

	private List<MainConfigurationModel> configurations;

	private List<FtpExtModel> ftps;

	private List<SmtpExtModel> smtps;

	private LoginsUsers loginUsers;

	private List<SchedulerServiceModel> scheduledServices;

	private List<PlugInDescriptors> plugIns;

	private object lockJsons = new object();

	private readonly EncryptionHelper eh;

	private ILogger<ManageConfiguration> logger;

	public ManageConfiguration(List<MainConfigurationModel> _configurations, LoginsUsers _loginUsers, List<SchedulerServiceModel> _scheduledServices, List<PlugInDescriptors> _plugIns, List<FtpExtModel> _ftps, List<SmtpExtModel> _smtps)
	{
		CurrentPath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
		Directory.SetCurrentDirectory(CurrentPath);
		configurations = _configurations;
		ftps = _ftps;
		smtps = _smtps;
		loginUsers = _loginUsers;
		scheduledServices = _scheduledServices;
		plugIns = _plugIns;
		eh = new EncryptionHelper();
		CheckLogger();
	}

	private void CheckLogger()
	{
		if (logger == null)
		{
			IApplicationBuilder _app = DIHelper.AppBuilder;
			if (_app != null)
			{
				IServiceProvider services = _app.ApplicationServices;
				logger = services.GetService<ILogger<ManageConfiguration>>();
			}
		}
	}

	public List<FtpExtModel> getFtps()
	{
		ConfigHelperFixingErros fixErrors = new ConfigHelperFixingErros();
		ftps = fixErrors.GetFtps();
		if (ftps != null)
		{
			return ftps;
		}
		return new List<FtpExtModel>();
	}

	public List<SmtpExtModel> getSmtps()
	{
		ConfigHelperFixingErros fixErrors = new ConfigHelperFixingErros();
		smtps = fixErrors.GetSmtps();
		if (smtps != null)
		{
			return smtps;
		}
		return new List<SmtpExtModel>();
	}

	public List<MainConfigurationModel> GetConfigs()
	{
		if (configurations.Count == 0)
		{
			ConfigHelperFixingErros fixErrors = new ConfigHelperFixingErros();
			configurations = fixErrors.GetAllConfigs();
		}
		if (configurations == null)
		{
			configurations = new List<MainConfigurationModel>();
		}
		return configurations;
	}

	public void SaveConfigs(List<MainConfigurationModel> config)
	{
		CheckLogger();
		MainConfigHelper configHlp = new MainConfigHelper(configurations, plugIns, ftps, smtps);
		try
		{
			foreach (MainConfigurationModel item in config)
			{
				configHlp.SaveConfigChanges(item);
			}
		}
		catch (Exception ex)
		{
			logger.LogError(ex.ToString());
		}
	}

	public void SaveLogins(LoginsUsers logins)
	{
		CheckLogger();
		try
		{
			lock (lockJsons)
			{
				string json = JsonSerializer.Serialize(logins);
				string configPath = Path.GetFullPath(Path.Combine(new string[2] { CurrentPath, "pwss.json" }));
				json = eh.Encrypt(json);
				File.WriteAllText(configPath, json, Encoding.Default);
				loginUsers = logins;
			}
		}
		catch (Exception ex)
		{
			logger.LogError(ex.ToString());
		}
	}

	public List<EnumForDisplayModel> GetAllEnums()
	{
		List<EnumForDisplayModel> result = new List<EnumForDisplayModel>();
		foreach (PlugInDescriptors item in plugIns)
		{
			if (item.enumTypes == null)
			{
				continue;
			}
			foreach (Type enumItem in item.enumTypes)
			{
				result.Add(new EnumForDisplayModel
				{
					EnumName = enumItem.Name,
					EnumType = enumItem
				});
			}
		}
		return result;
	}
}
