using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using HitHelpersNetCore.Helpers;
using HitHelpersNetCore.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace HitServicesCore.Helpers;

public class MainConfigHelper : AbstractConfigurationHelper
{
	public List<MainConfigurationModel> configs;

	private List<FtpExtModel> ftps;

	private List<SmtpExtModel> smtps;

	private List<PlugInDescriptors> plugIns;

	private string rootPath;

	private ILogger<MainConfigHelper> logger;

	public MainConfigHelper(List<MainConfigurationModel> _configs, List<PlugInDescriptors> _plugIns, List<FtpExtModel> _ftps, List<SmtpExtModel> _smtps)
	{
		configs = _configs;
		plugIns = _plugIns;
		ftps = _ftps;
		smtps = _smtps;
		rootPath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
		Directory.SetCurrentDirectory(rootPath);
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
				logger = services.GetService<ILogger<MainConfigHelper>>();
			}
		}
	}

	public void InitializeHitServiceCore()
	{
		CheckLogger();
		try
		{
			configs = new List<MainConfigurationModel>();
			MainConfigurationModel tmpConfig = ReadHitServiceCoreConfig();
			ftps = ReadFtpConfiguration();
			smtps = ReadSmtpConfiguration();
			if (tmpConfig != null)
			{
				configs.Add(tmpConfig);
			}
		}
		catch (Exception ex)
		{
			logger.LogError(ex.ToString());
		}
	}

	public void InitializeConfigs()
	{
		CheckLogger();
		try
		{
			configs = new List<MainConfigurationModel>();
			MainConfigurationModel tmpConfig = ReadHitServiceCoreConfig();
			if (tmpConfig != null)
			{
				configs.Add(tmpConfig);
			}
			if (plugIns == null)
			{
				return;
			}
			foreach (PlugInDescriptors item in plugIns)
			{
				if (item.configClass != null)
				{
					tmpConfig = InitilizeConfiguration(item.mainDescriptor.path, item.mainDescriptor.plugIn_Id, item.configClass.fullClassName);
					if (tmpConfig != null)
					{
						configs.Add(tmpConfig);
					}
				}
			}
		}
		catch (Exception ex)
		{
			logger.LogError(ex.ToString());
		}
	}

	public void SaveConfigChanges(MainConfigurationModel configToSave)
	{
		SaveConfiguration(configToSave);
		MainConfigurationModel fld = configs.Find(delegate(MainConfigurationModel f)
		{
			Guid? plugInId = f.plugInId;
			Guid? plugInId2 = configToSave.plugInId;
			if (plugInId.HasValue != plugInId2.HasValue)
			{
				return false;
			}
			return !plugInId.HasValue || plugInId.GetValueOrDefault() == plugInId2.GetValueOrDefault();
		});
		if (fld == null)
		{
			configs.Add(configToSave);
			fld = configs.Find(delegate(MainConfigurationModel f)
			{
				Guid? plugInId = f.plugInId;
				Guid? plugInId2 = configToSave.plugInId;
				if (plugInId.HasValue != plugInId2.HasValue)
				{
					return false;
				}
				return !plugInId.HasValue || plugInId.GetValueOrDefault() == plugInId2.GetValueOrDefault();
			});
		}
		else
		{
			fld = configToSave;
		}
	}
}
