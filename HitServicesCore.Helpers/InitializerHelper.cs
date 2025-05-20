using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.Json;
using HitHelpersNetCore.Models;
using HitServicesCore.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Logging;

namespace HitServicesCore.Helpers;

public class InitializerHelper
{
	private readonly ILogger<InitializerHelper> logger;

	public readonly List<PlugInDescriptors> plugins;

	private readonly SystemInfo sysInfo;

	private object lockJsons = new object();

	public InitializerHelper(List<PlugInDescriptors> _plugins, SystemInfo _sysInfo, ILogger<InitializerHelper> _logger)
	{
		plugins = _plugins;
		sysInfo = _sysInfo;
		logger = _logger;
	}

	public bool RunInitialMethod(Guid plugInId, IApplicationBuilder _app)
	{
		bool result = true;
		try
		{
			PlugInDescriptors fld = plugins.Find((PlugInDescriptors f) => f.mainDescriptor.plugIn_Id == plugInId);
			if (fld == null)
			{
				logger.LogInformation("PlugIn Id " + plugInId.ToString() + " not exists");
				return true;
			}
			if (fld.initialerDescriptor == null)
			{
				logger.LogInformation("PlugIn [" + fld.mainDescriptor.plugIn_Description + "] not having initialazer method");
				return true;
			}
			if (fld.initialerDescriptor.latestUpdate == fld.initialerDescriptor.dbVersion)
			{
				logger.LogInformation("PlugIn [" + fld.mainDescriptor.plugIn_Description + "] is updated to latest version");
				return true;
			}
			Type t = Type.GetType(fld.initialerDescriptor.fullNameSpace + ", " + fld.initialerDescriptor.assemblyFileName);
			if (t == null)
			{
				logger.LogInformation("Cannot create instance for plugIn [" + fld.mainDescriptor.plugIn_Description + "]");
				return false;
			}
			object[] obj = new object[2]
			{
				fld.initialerDescriptor.latestUpdate,
				_app
			};
			object instance = Activator.CreateInstance(t);
			MethodInfo method = t.GetMethod("Start");
			object res = method.Invoke(instance, obj);
			fld.initialerDescriptor.latestUpdate = fld.initialerDescriptor.dbVersion;
			fld.initialerDescriptor.latestUpdateDate = DateTime.UtcNow.Date;
			List<InitializersLastUpdateModel> initUpdats = new List<InitializersLastUpdateModel>();
			foreach (PlugInDescriptors item in plugins)
			{
				if (item.initialerDescriptor != null)
				{
					InitializersLastUpdateModel tmp = new InitializersLastUpdateModel();
					tmp.latestUpdate = item.initialerDescriptor.latestUpdate;
					tmp.latestUpdateDate = item.initialerDescriptor.latestUpdateDate;
					tmp.plugInId = item.mainDescriptor.plugIn_Id;
					initUpdats.Add(tmp);
				}
			}
			if (initUpdats.Count > 0)
			{
				SaveUpdateChangesToFile(initUpdats);
			}
		}
		catch (Exception ex)
		{
			logger.LogError(ex.ToString());
			throw;
		}
		return result;
	}

	private void SaveUpdateChangesToFile(List<InitializersLastUpdateModel> initUpdats)
	{
		try
		{
			lock (lockJsons)
			{
				string sFileName = Path.GetFullPath(Path.Combine(new string[3] { sysInfo.rootPath, "Config", "latestUpdates.json" }));
				string sVal = JsonSerializer.Serialize(initUpdats);
				File.WriteAllText(sFileName, sVal);
			}
		}
		catch
		{
		}
	}
}
