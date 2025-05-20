using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using HitHelpersNetCore.Helpers;
using HitHelpersNetCore.Models;
using HitServicesCore.Enums;
using HitServicesCore.Models;
using HitServicesCore.Models.IS_Services;
using HitServicesCore.Models.SharedModels;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using Microsoft.Extensions.Logging;

namespace HitServicesCore.Helpers;

public class PluginsMvcLoader
{
	private readonly ILogger<PluginsMvcLoader> logger;

	private readonly SystemInfo sysInfo;

	private readonly List<PlugInDescriptors> plugInDescriptors;

	private readonly List<MainConfigurationModel> configurations;

	private readonly List<SchedulerServiceModel> hangfireServices;

	private readonly PlugInAnnotationedClassesHelper plgAnnotHelp;

	private readonly LoginsUsers loginsUsers;

	private readonly SmtpHelper smtpConfiguration;

	private static object lockJsons = new object();

	private List<FtpExtModel> ftps;

	private List<SmtpExtModel> smtps;

	public PluginsMvcLoader(SystemInfo _sysInfo, List<PlugInDescriptors> _plugInDescriptors, List<MainConfigurationModel> _configurations, List<SchedulerServiceModel> _hangfireServices, LoginsUsers _loginsUsers, PlugInAnnotationedClassesHelper _plgAnnotHelp, ILogger<PluginsMvcLoader> _logger, SmtpHelper _smtpConfiguration, List<FtpExtModel> _ftps, List<SmtpExtModel> _smtps)
	{
		sysInfo = _sysInfo;
		logger = _logger;
		plugInDescriptors = _plugInDescriptors;
		configurations = _configurations;
		hangfireServices = _hangfireServices;
		loginsUsers = _loginsUsers;
		plgAnnotHelp = _plgAnnotHelp;
		smtpConfiguration = _smtpConfiguration;
		ftps = _ftps;
		smtps = _smtps;
	}

	private bool checkfolderExist(string path, bool create = true, bool delete = false)
	{
		bool res = Directory.Exists(path);
		if (res && delete)
		{
			Directory.Delete(path, recursive: true);
			res = false;
		}
		if (!res && create)
		{
			try
			{
				Directory.CreateDirectory(path);
				return true;
			}
			catch (Exception ex)
			{
				logger.LogError(ex.ToString());
				return false;
			}
		}
		return res;
	}

	public void LoadMvcPlugins(ApplicationPartManager apm)
	{
		InitializeMainConfiguration();
		ConfigureApplicationParts(apm);
		AddConfigurationsToModel();
		CheckServicesForConfiguration();
		AddLogins();
		smtpConfiguration.GetConfig();
		FindInitizlersLatestUpdatedVersion();
	}

	private void ConfigureApplicationParts(ApplicationPartManager apm)
	{
		if (checkfolderExist(sysInfo.pluginPath))
		{
			string[] plugIns = Directory.GetDirectories(sysInfo.pluginPath);
			string[] array = plugIns;
			foreach (string plgPath in array)
			{
				string[] assemblyFiles = Directory.GetFiles(plgPath, "*.dll", SearchOption.AllDirectories);
				string[] array2 = assemblyFiles;
				foreach (string assemblyFile in array2)
				{
					if (assemblyFile.EndsWith(".Views.dll"))
					{
						Assembly assembly = Assembly.LoadFrom(assemblyFile);
						apm.ApplicationParts.Add(new CompiledRazorAssemblyPart(assembly));
						continue;
					}
					string error;
					bool AddToDI = plgAnnotHelp.AddPlugInToPlugInDecriptors(assemblyFile, plugInDescriptors, plgPath, out error);
					if (!string.IsNullOrWhiteSpace(error))
					{
						if (error == "No descriptor for plugin exists")
						{
							logger.LogInformation(error);
						}
						else
						{
							logger.LogError(error);
						}
					}
					if (AddToDI)
					{
						try
						{
							Assembly assembly2 = Assembly.LoadFrom(assemblyFile);
							apm.ApplicationParts.Add(new AssemblyPart(assembly2));
							saveResources(assembly2);
						}
						catch (Exception ex)
						{
							string a = ex.ToString();
						}
					}
				}
			}
		}
		apm.FeatureProviders.Add(new ViewComponentFeatureProvider());
	}

	private void AddLogins()
	{
		try
		{
			lock (lockJsons)
			{
				string sFileName = Path.GetFullPath(Path.Combine(new string[3] { sysInfo.rootPath, "Config", "pwss.json" }));
				loginsUsers.logins = FillSubDictionary(sFileName);
			}
		}
		catch (Exception ex)
		{
			logger.LogError(ex.ToString());
		}
	}

	private void FindInitizlersLatestUpdatedVersion()
	{
		try
		{
			lock (lockJsons)
			{
				DateTime dtNow = new DateTime(1900, 1, 1);
				List<Guid> availablePlugins = new List<Guid>();
				List<InitializersLastUpdateModel> initUpdats = new List<InitializersLastUpdateModel>();
				string sFileName = Path.GetFullPath(Path.Combine(new string[3] { sysInfo.rootPath, "Config", "latestUpdates.json" }));
				if (File.Exists(sFileName))
				{
					string sVal = File.ReadAllText(sFileName);
					initUpdats = JsonSerializer.Deserialize<List<InitializersLastUpdateModel>>(sVal);
					foreach (PlugInDescriptors item in plugInDescriptors)
					{
						if (item.initialerDescriptor != null)
						{
							InitializersLastUpdateModel fld = initUpdats.Find((InitializersLastUpdateModel f) => f.plugInId == item.mainDescriptor.plugIn_Id);
							if (fld == null)
							{
								item.initialerDescriptor.latestUpdate = "0.0.0.0";
								item.initialerDescriptor.latestUpdateDate = dtNow;
								initUpdats.Add(new InitializersLastUpdateModel
								{
									plugInId = item.mainDescriptor.plugIn_Id,
									latestUpdate = "0.0.0.0",
									latestUpdateDate = dtNow
								});
							}
							else
							{
								item.initialerDescriptor.latestUpdate = fld.latestUpdate;
								item.initialerDescriptor.latestUpdateDate = fld.latestUpdateDate;
							}
							availablePlugins.Add(item.mainDescriptor.plugIn_Id);
						}
					}
				}
				else
				{
					foreach (PlugInDescriptors item2 in plugInDescriptors)
					{
						if (item2.initialerDescriptor != null)
						{
							item2.initialerDescriptor.latestUpdate = "0.0.0.0";
							item2.initialerDescriptor.latestUpdateDate = dtNow;
							initUpdats.Add(new InitializersLastUpdateModel
							{
								plugInId = item2.mainDescriptor.plugIn_Id,
								latestUpdate = "0.0.0.0",
								latestUpdateDate = dtNow
							});
							availablePlugins.Add(item2.mainDescriptor.plugIn_Id);
						}
					}
				}
				initUpdats.RemoveAll((InitializersLastUpdateModel r) => !availablePlugins.Contains(r.plugInId));
				string newFile = JsonSerializer.Serialize(initUpdats);
				File.WriteAllText(sFileName, newFile);
			}
		}
		catch (Exception ex)
		{
			logger.LogError(ex.ToString());
		}
	}

	private void InitializeMainConfiguration()
	{
		MainConfigHelper configHlp = new MainConfigHelper(configurations, plugInDescriptors, ftps, smtps);
		configHlp.InitializeHitServiceCore();
	}

	private void AddConfigurationsToModel()
	{
		MainConfigHelper configHlp = new MainConfigHelper(configurations, plugInDescriptors, ftps, smtps);
		configHlp.InitializeConfigs();
	}

	private void CheckServicesForConfiguration()
	{
		try
		{
			List<Guid> availableServices = new List<Guid>();
			lock (lockJsons)
			{
				string sFileName = Path.GetFullPath(Path.Combine(new string[3] { sysInfo.rootPath, "Config", "scheduler.json" }));
				if (File.Exists(sFileName))
				{
					string sVal = File.ReadAllText(sFileName, Encoding.Default);
					EncryptionHelper eh = new EncryptionHelper();
					sVal = eh.Decrypt(sVal);
					if (!string.IsNullOrWhiteSpace(sVal))
					{
						hangfireServices.AddRange(JsonSerializer.Deserialize<List<SchedulerServiceModel>>(sVal));
					}
					hangfireServices.ForEach(delegate(SchedulerServiceModel f)
					{
						f.assemblyFileName = ((f.serviceType != HangFireServiceTypeEnum.Plugin) ? "HitServicesCore" : f.assemblyFileName);
					});
				}
			}
			GetPluginServices(ref availableServices);
			Get_IS_ExportDataServices(ref availableServices);
			Get_IS_SqlScriptsServices(ref availableServices);
			Get_IS_SaveToTableServices(ref availableServices);
			Get_IS_ReadFromCsvServices(ref availableServices);
			hangfireServices.RemoveAll((SchedulerServiceModel r) => !availableServices.Contains(r.serviceId));
		}
		catch (Exception ex)
		{
			logger.LogError(ex.ToString());
		}
	}

	private void GetPluginServices(ref List<Guid> availableServices)
	{
		int serviceCount = 0;
		lock (lockJsons)
		{
			foreach (PlugInDescriptors item in plugInDescriptors)
			{
				if (item.mainDescriptor.plugIn_Id == Guid.Empty || item.serviceDescriptor == null)
				{
					continue;
				}
				foreach (ServiceDescriptorWithTypeModel service in item.serviceDescriptor)
				{
					Type objectType = Type.GetType(service.fullNameSpace + ", " + service.assemblyFileName);
					if (!(objectType.BaseType.Name == "ServiceExecutions"))
					{
						continue;
					}
					switch (hangfireServices.Where((SchedulerServiceModel w) => w.classFullName == service.fullNameSpace).Count())
					{
					case 1:
					{
						SchedulerServiceModel fld = hangfireServices.Find((SchedulerServiceModel f) => f.classFullName == service.fullNameSpace);
						_ = fld.serviceId;
						if (fld.serviceId == Guid.Empty || fld.serviceId != service.serviceId)
						{
							fld.serviceId = service.serviceId;
						}
						fld.description = "";
						fld = FillEmptyValues(fld, service);
						availableServices.Add(service.serviceId);
						continue;
					}
					case 0:
						hangfireServices.Add(new SchedulerServiceModel
						{
							serviceId = service.serviceId,
							serviceName = service.seriveName,
							classFullName = service.fullNameSpace,
							description = service.serviceDescription,
							isActive = false,
							schedulerTime = "* * * * *",
							schedulerDescr = "Every minute",
							assemblyFileName = service.assemblyFileName,
							serviceVersion = service.serviceVersion,
							serviceType = HangFireServiceTypeEnum.Plugin
						});
						availableServices.Add(service.serviceId);
						continue;
					}
					SchedulerServiceModel fld2 = hangfireServices.Find((SchedulerServiceModel f) => f.classFullName == service.fullNameSpace && f.serviceId == service.serviceId);
					if (fld2 == null)
					{
						fld2 = hangfireServices.Find(delegate(SchedulerServiceModel f)
						{
							if (f.classFullName == service.fullNameSpace)
							{
								_ = f.serviceId;
								return f.serviceId == Guid.Empty;
							}
							return false;
						});
					}
					if (fld2 == null)
					{
						hangfireServices.Add(new SchedulerServiceModel
						{
							serviceId = service.serviceId,
							classFullName = service.fullNameSpace,
							description = service.serviceDescription,
							isActive = false,
							schedulerTime = "* * * * *",
							schedulerDescr = "Every minute",
							assemblyFileName = service.assemblyFileName,
							serviceVersion = service.serviceVersion
						});
					}
					else
					{
						fld2.serviceId = service.serviceId;
						fld2.description = "";
						fld2 = FillEmptyValues(fld2, service);
					}
					availableServices.Add(service.serviceId);
				}
			}
		}
	}

	private void Get_IS_ExportDataServices(ref List<Guid> availableServices)
	{
		string isServicePath = Path.Combine(new string[3] { sysInfo.rootPath, "IS_Services", "ExportData" });
		EncryptionHelper eh = new EncryptionHelper();
		if (!Directory.Exists(isServicePath))
		{
			return;
		}
		if (isServicePath[isServicePath.Length - 1] != '\\')
		{
			isServicePath += "\\";
		}
		try
		{
			lock (lockJsons)
			{
				List<string> sqlScriptsJsons = Directory.EnumerateFiles(isServicePath, "*.json").ToList();
				foreach (string item in sqlScriptsJsons)
				{
					string sVal = File.ReadAllText(item, Encoding.Default);
					if (!string.IsNullOrWhiteSpace(sVal))
					{
						sVal = eh.Decrypt(sVal);
						ISExportDataModel scriptModel = JsonSerializer.Deserialize<ISExportDataModel>(sVal);
						if (scriptModel != null)
						{
							ISServiceGeneralModel generalClass = new ISServiceGeneralModel();
							generalClass.ClassDescription = scriptModel.ClassDescription;
							generalClass.ClassType = scriptModel.ClassType;
							generalClass.FullClassName = "HitServicesCore.InternalServices.ISExportDataService";
							generalClass.serviceId = scriptModel.serviceId;
							generalClass.serviceName = scriptModel.serviceName;
							generalClass.serviceType = scriptModel.serviceType;
							generalClass.serviceVersion = ((!scriptModel.serviceVersion.HasValue) ? new long?(long.MinValue) : scriptModel.serviceVersion);
							generalClass.serviceType = HangFireServiceTypeEnum.ExportData;
							AddISServiceToHangFireList(generalClass, "HitServicesCore");
							availableServices.Add(scriptModel.serviceId);
						}
					}
				}
			}
		}
		catch (Exception ex)
		{
			logger.LogError(ex.ToString());
		}
	}

	private void Get_IS_SqlScriptsServices(ref List<Guid> availableServices)
	{
		string isServicePath = Path.Combine(new string[3] { sysInfo.rootPath, "IS_Services", "SqlScripts" });
		EncryptionHelper eh = new EncryptionHelper();
		if (!Directory.Exists(isServicePath))
		{
			return;
		}
		if (isServicePath[isServicePath.Length - 1] != '\\')
		{
			isServicePath += "\\";
		}
		try
		{
			lock (lockJsons)
			{
				List<string> sqlScriptsJsons = Directory.EnumerateFiles(isServicePath, "*.json").ToList();
				foreach (string item in sqlScriptsJsons)
				{
					string sVal = File.ReadAllText(item, Encoding.Default);
					if (!string.IsNullOrWhiteSpace(sVal))
					{
						sVal = eh.Decrypt(sVal);
						ISRunSqlScriptsModel scriptModel = JsonSerializer.Deserialize<ISRunSqlScriptsModel>(sVal);
						if (scriptModel != null)
						{
							ISServiceGeneralModel generalClass = new ISServiceGeneralModel();
							generalClass.ClassDescription = scriptModel.ClassDescription;
							generalClass.ClassType = scriptModel.ClassType;
							generalClass.FullClassName = "HitServicesCore.InternalServices.ISRunSqlScriptService";
							generalClass.serviceId = scriptModel.serviceId;
							generalClass.serviceName = scriptModel.serviceName;
							generalClass.serviceType = scriptModel.serviceType;
							generalClass.serviceVersion = ((!scriptModel.serviceVersion.HasValue) ? new long?(long.MinValue) : scriptModel.serviceVersion);
							generalClass.serviceType = HangFireServiceTypeEnum.SqlScripts;
							AddISServiceToHangFireList(generalClass, "HitServicesCore");
							availableServices.Add(scriptModel.serviceId);
						}
					}
				}
			}
		}
		catch (Exception ex)
		{
			logger.LogError(ex.ToString());
		}
	}

	private void Get_IS_SaveToTableServices(ref List<Guid> availableServices)
	{
		string isServicePath = Path.Combine(new string[3] { sysInfo.rootPath, "IS_Services", "SaveToTable" });
		EncryptionHelper eh = new EncryptionHelper();
		if (!Directory.Exists(isServicePath))
		{
			return;
		}
		if (isServicePath[isServicePath.Length - 1] != '\\')
		{
			isServicePath += "\\";
		}
		try
		{
			lock (lockJsons)
			{
				List<string> saveToTableJsons = Directory.EnumerateFiles(isServicePath, "*.json").ToList();
				foreach (string item in saveToTableJsons)
				{
					string sVal = File.ReadAllText(item, Encoding.Default);
					if (!string.IsNullOrWhiteSpace(sVal))
					{
						sVal = eh.Decrypt(sVal);
						ISSaveToTableModel scriptModel = JsonSerializer.Deserialize<ISSaveToTableModel>(sVal);
						if (scriptModel != null)
						{
							ISServiceGeneralModel generalClass = new ISServiceGeneralModel();
							generalClass.ClassDescription = scriptModel.ClassDescription;
							generalClass.ClassType = scriptModel.ClassType;
							generalClass.FullClassName = "HitServicesCore.InternalServices.ISSaveToTableService";
							generalClass.serviceId = scriptModel.serviceId;
							generalClass.serviceName = scriptModel.serviceName;
							generalClass.serviceType = scriptModel.serviceType;
							generalClass.serviceVersion = ((!scriptModel.serviceVersion.HasValue) ? new long?(long.MinValue) : scriptModel.serviceVersion);
							generalClass.serviceType = HangFireServiceTypeEnum.SaveToTable;
							AddISServiceToHangFireList(generalClass, "HitServicesCore");
							availableServices.Add(scriptModel.serviceId);
						}
					}
				}
			}
		}
		catch (Exception ex)
		{
			logger.LogError(ex.ToString());
		}
	}

	private void Get_IS_ReadFromCsvServices(ref List<Guid> availableServices)
	{
		string isServicePath = Path.Combine(new string[3] { sysInfo.rootPath, "IS_Services", "ReadCsv" });
		EncryptionHelper eh = new EncryptionHelper();
		if (!Directory.Exists(isServicePath))
		{
			return;
		}
		if (isServicePath[isServicePath.Length - 1] != '\\')
		{
			isServicePath += "\\";
		}
		try
		{
			lock (lockJsons)
			{
				List<string> saveToTableJsons = Directory.EnumerateFiles(isServicePath, "*.json").ToList();
				foreach (string item in saveToTableJsons)
				{
					string sVal = File.ReadAllText(item, Encoding.Default);
					if (!string.IsNullOrWhiteSpace(sVal))
					{
						sVal = eh.Decrypt(sVal);
						ISReadFromCsvModel scriptModel = JsonSerializer.Deserialize<ISReadFromCsvModel>(sVal);
						if (scriptModel != null)
						{
							ISServiceGeneralModel generalClass = new ISServiceGeneralModel();
							generalClass.ClassDescription = scriptModel.ClassDescription;
							generalClass.ClassType = scriptModel.ClassType;
							generalClass.FullClassName = "HitServicesCore.InternalServices.ISReadCsvService";
							generalClass.serviceId = scriptModel.serviceId;
							generalClass.serviceName = scriptModel.serviceName;
							generalClass.serviceType = scriptModel.serviceType;
							generalClass.serviceVersion = ((!scriptModel.serviceVersion.HasValue) ? new long?(long.MinValue) : scriptModel.serviceVersion);
							generalClass.serviceType = HangFireServiceTypeEnum.ReadFromCsv;
							AddISServiceToHangFireList(generalClass, "HitServicesCore");
							availableServices.Add(scriptModel.serviceId);
						}
					}
				}
			}
		}
		catch (Exception ex)
		{
			logger.LogError(ex.ToString());
		}
	}

	private void AddISServiceToHangFireList(ISServiceGeneralModel isModel, string fileName)
	{
		switch ((hangfireServices != null) ? ((hangfireServices.Find((SchedulerServiceModel f) => f.serviceId == isModel.serviceId) != null) ? hangfireServices.FindAll((SchedulerServiceModel f) => f.serviceId == isModel.serviceId).Count() : 0) : 0)
		{
		case 0:
			hangfireServices.Add(new SchedulerServiceModel
			{
				assemblyFileName = fileName,
				classFullName = isModel.FullClassName,
				description = isModel.ClassDescription,
				isActive = false,
				schedulerTime = "* * * * *",
				schedulerDescr = "Every minute",
				serviceId = isModel.serviceId,
				serviceName = isModel.serviceName,
				serviceType = isModel.serviceType,
				serviceVersion = isModel.serviceVersion.ToString()
			});
			return;
		case 1:
		{
			SchedulerServiceModel fld = hangfireServices.Find((SchedulerServiceModel f) => f.serviceId == isModel.serviceId);
			fld.description = "";
			fld.assemblyFileName = fileName;
			fld = FillEmptyValuesForISService(fld, isModel, fileName);
			return;
		}
		}
		hangfireServices.RemoveAll((SchedulerServiceModel r) => r.serviceId == isModel.serviceId);
		hangfireServices.Add(new SchedulerServiceModel
		{
			assemblyFileName = fileName,
			classFullName = isModel.FullClassName,
			description = isModel.ClassDescription,
			isActive = false,
			schedulerTime = "* * * * *",
			schedulerDescr = "Every minute",
			serviceId = isModel.serviceId,
			serviceName = isModel.serviceName,
			serviceType = isModel.serviceType,
			serviceVersion = isModel.serviceVersion.ToString()
		});
	}

	private SchedulerServiceModel FillEmptyValues(SchedulerServiceModel model, ServiceDescriptorWithTypeModel pluginModel)
	{
		if (string.IsNullOrWhiteSpace(model.assemblyFileName))
		{
			model.assemblyFileName = pluginModel.assemblyFileName;
		}
		if (string.IsNullOrWhiteSpace(model.classFullName))
		{
			model.classFullName = pluginModel.fullNameSpace;
		}
		if (string.IsNullOrWhiteSpace(model.description))
		{
			model.description = pluginModel.serviceDescription;
		}
		if (string.IsNullOrWhiteSpace(model.serviceVersion))
		{
			model.serviceVersion = pluginModel.serviceVersion;
		}
		if (string.IsNullOrWhiteSpace(model.schedulerDescr))
		{
			model.schedulerDescr = ParseCron(model.schedulerTime);
		}
		if (string.IsNullOrWhiteSpace(model.serviceName))
		{
			model.serviceName = pluginModel.seriveName;
		}
		return model;
	}

	private SchedulerServiceModel FillEmptyValuesForISService(SchedulerServiceModel model, ISServiceGeneralModel isModel, string fileName)
	{
		if (string.IsNullOrWhiteSpace(model.assemblyFileName))
		{
			model.assemblyFileName = fileName;
		}
		if (string.IsNullOrWhiteSpace(model.classFullName))
		{
			model.classFullName = isModel.FullClassName;
		}
		if (string.IsNullOrWhiteSpace(model.description))
		{
			model.description = isModel.ClassDescription;
		}
		if (string.IsNullOrWhiteSpace(model.serviceVersion))
		{
			model.serviceVersion = ((!isModel.serviceVersion.HasValue) ? new long?(long.MinValue) : isModel.serviceVersion).ToString();
		}
		if (string.IsNullOrWhiteSpace(model.schedulerDescr))
		{
			model.schedulerDescr = ParseCron(model.schedulerTime);
		}
		if (string.IsNullOrWhiteSpace(model.serviceName))
		{
			model.serviceName = isModel.serviceName;
		}
		return model;
	}

	private string ParseCron(string cron)
	{
		string[] cronParce = cron.Split(" ");
		string minuteState = "";
		string hourState = "";
		string daysState = "";
		string monthsState = "";
		string weekdayState = "";
		if (cronParce[0] == "*")
		{
			minuteState = "At every minute";
		}
		if (cronParce[0] == "*/2")
		{
			minuteState = "At every 2nd minute";
		}
		if (cronParce[0] == "1-59/2")
		{
			minuteState = "At every 2nd minute from 1 through 59";
		}
		if (cronParce[1] == "*")
		{
			hourState = "past every hour";
		}
		if (cronParce[1] == "*/2")
		{
			hourState = "past every two hours";
		}
		if (cronParce[1] == "1-23/2")
		{
			hourState = "past every 2nd hour from 1 through 23";
		}
		if (cronParce[2] == "*")
		{
			daysState = "on every day";
		}
		if (cronParce[2] == "*/2")
		{
			daysState = "on 2nd day";
		}
		if (cronParce[2] == "1-31/2")
		{
			daysState = "on every 2nd day-of-month from 1 through 31";
		}
		if (cronParce[3] == "*")
		{
			monthsState = "in every month";
		}
		if (cronParce[3] == "*/2")
		{
			monthsState = "in every 2nd month";
		}
		if (cronParce[3] == "1-12/2")
		{
			monthsState = "in every 2nd month from January through December";
		}
		if (cronParce[4] == "*")
		{
			weekdayState = "on every weekday";
		}
		if (cronParce[4] == "*/2")
		{
			weekdayState = "on every 2nd weekday";
		}
		if (cronParce[4] == "0-6/2")
		{
			weekdayState = "on every 2nd day-of-week from Monday through Sunday";
		}
		return ((!string.IsNullOrWhiteSpace(minuteState)) ? (minuteState + ", ") : "") + ((!string.IsNullOrWhiteSpace(hourState)) ? (hourState + ", ") : "") + ((!string.IsNullOrWhiteSpace(daysState)) ? (daysState + ", ") : "") + ((!string.IsNullOrWhiteSpace(monthsState)) ? (monthsState + ", ") : "") + ((!string.IsNullOrWhiteSpace(weekdayState)) ? weekdayState : "");
	}

	private static Dictionary<string, dynamic> FillSubDictionary(string jsonFile)
	{
		EncryptionHelper eh = new EncryptionHelper();
		if (!File.Exists(jsonFile))
		{
			LoginsUsers logins = new LoginsUsers();
			logins.logins = new Dictionary<string, object>
			{
				{ "Admin_Username", "hitadmin" },
				{ "Admin_Password", "h1ts@" },
				{ "User_Username", "hituser" },
				{ "User_Password", "4502" }
			};
			try
			{
				lock (lockJsons)
				{
					string json = eh.Encrypt(JsonSerializer.Serialize(logins.logins));
					File.WriteAllText(jsonFile, json, Encoding.Default);
				}
			}
			catch
			{
			}
		}
		string rawData = File.ReadAllText(jsonFile, Encoding.Default);
		rawData = eh.Decrypt(rawData);
		return JsonSerializer.Deserialize<Dictionary<string, object>>(rawData);
	}

	private void saveResources(Assembly assembly)
	{
		string[] manifestResourceNames = assembly.GetManifestResourceNames();
		string[] array = manifestResourceNames;
		foreach (string recource in array)
		{
			try
			{
				FileInfo fi = setFileInfo(recource);
				if (fi != null)
				{
					string ext = fi.Extension.Replace(".", "");
					Stream recourceStream = assembly.GetManifestResourceStream(recource);
					switch (ext)
					{
					case "jpg":
					case "png":
					case "mp4":
					case "ico":
					case "gif":
					case "spg":
						saveBinaryResource(fi, recourceStream);
						break;
					default:
						saveTextResource(fi, recourceStream);
						break;
					}
				}
			}
			catch (Exception ex)
			{
				logger.LogError(ex.ToString());
			}
		}
	}

	private FileInfo setFileInfo(string key)
	{
		List<string> parts = key.Split(".").ToList();
		string path = sysInfo.pluginFilePath;
		for (int i = 0; i < parts.Count() - 2; i++)
		{
			path = Path.Combine(path, parts[i]);
		}
		if (checkfolderExist(path))
		{
			string ext = parts[parts.Count() - 1].ToLower();
			path = Path.Combine(path, parts[parts.Count() - 2]);
			path = path + "." + ext;
			return new FileInfo(path);
		}
		return null;
	}

	private void saveTextResource(FileInfo fi, Stream recourceStream)
	{
		try
		{
			lock (lockJsons)
			{
				string content = string.Empty;
				using (StreamReader reader = new StreamReader(recourceStream, Encoding.Default))
				{
					content += reader.ReadToEnd();
				}
				File.WriteAllText(fi.FullName, content);
			}
		}
		catch (Exception)
		{
		}
	}

	private void saveBinaryResource(FileInfo fi, Stream recourceStream)
	{
		BinaryReader br = new BinaryReader(recourceStream);
		FileStream fs = new FileStream(fi.FullName, FileMode.Create);
		BinaryWriter bw = new BinaryWriter(fs);
		byte[] ba = new byte[recourceStream.Length];
		recourceStream.Read(ba, 0, ba.Length);
		bw.Write(ba);
		br.Close();
		bw.Close();
		recourceStream.Close();
	}
}
