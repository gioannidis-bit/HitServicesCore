using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using HitHelpersNetCore.Helpers;
using HitHelpersNetCore.Models;
using HitServicesCore.Enums;
using HitServicesCore.Models;
using HitServicesCore.Models.IS_Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace HitServicesCore.Helpers;

public class IS_ServicesHelper
{
	private readonly SystemInfo sysInfo;

	private object lockJsons = new object();

	private readonly List<SchedulerServiceModel> hangfireServices;

	private readonly EncryptionHelper eh;

	private readonly ILogger<IS_ServicesHelper> logger;

	public IS_ServicesHelper()
	{
		if (DIHelper.AppBuilder != null)
		{
			IServiceProvider services = DIHelper.AppBuilder.ApplicationServices;
			logger = services.GetService<ILogger<IS_ServicesHelper>>();
			sysInfo = services.GetService<SystemInfo>();
			hangfireServices = services.GetService<List<SchedulerServiceModel>>();
		}
		eh = new EncryptionHelper();
	}

	private List<HitHelpersNetCore.Models.BaseKeyValueModel> RemoveSpacesFromListKeys(List<HitHelpersNetCore.Models.BaseKeyValueModel> model)
	{
		model.ForEach(delegate(HitHelpersNetCore.Models.BaseKeyValueModel r)
		{
			r.key = r.key.Trim();
		});
		return model;
	}

	private Dictionary<string, string> RemoveSpacesFromKeys(Dictionary<string, string> model)
	{
		return model.ToDictionary((KeyValuePair<string, string> x) => x.Key.Trim(), (KeyValuePair<string, string> x) => x.Value);
	}

	public List<ISRunSqlScriptsModel> GetRunSqlScriptsFromJsonFiles()
	{
		List<ISRunSqlScriptsModel> result = new List<ISRunSqlScriptsModel>();
		try
		{
			string isServicePath = Path.Combine(new string[3] { sysInfo.rootPath, "IS_Services", "SqlScripts" });
			if (!Directory.Exists(isServicePath))
			{
				return result;
			}
			if (isServicePath[isServicePath.Length - 1] != '\\')
			{
				isServicePath += "\\";
			}
			lock (lockJsons)
			{
				List<string> sqlScriptsJsons = Directory.EnumerateFiles(isServicePath, "*.json").ToList();
				foreach (string item in sqlScriptsJsons)
				{
					string sVal = File.ReadAllText(item, Encoding.Default);
					sVal = eh.Decrypt(sVal);
					if (!string.IsNullOrWhiteSpace(sVal))
					{
						result.Add(JsonSerializer.Deserialize<ISRunSqlScriptsModel>(sVal));
					}
				}
			}
		}
		catch (Exception ex)
		{
			logger.LogError(ex.ToString());
		}
		return result;
	}

	public bool SaveRunsSqlScriptsJsons(List<ISRunSqlScriptsModel> model)
	{
		bool result = true;
		string isServicePath = Path.Combine(new string[3] { sysInfo.rootPath, "IS_Services", "SqlScripts" });
		if (!Directory.Exists(isServicePath))
		{
			Directory.CreateDirectory(isServicePath);
		}
		if (isServicePath[isServicePath.Length - 1] != '\\')
		{
			isServicePath += "\\";
		}
		try
		{
			foreach (ISRunSqlScriptsModel item in model)
			{
				_ = item.serviceId;
				if (item.serviceId == Guid.Empty)
				{
					item.serviceId = Guid.NewGuid();
					item.FullClassName = "HitServicesCore.InternalServices.ISRunSqlScriptService";
				}
				string fileName = isServicePath + item.serviceName;
				if (!fileName.EndsWith(".json"))
				{
					fileName += ".json";
				}
				item.serviceType = HangFireServiceTypeEnum.SqlScripts;
				if (item.SqlParameters != null)
				{
					item.SqlParameters = RemoveSpacesFromKeys(item.SqlParameters);
				}
				string savedVal = JsonSerializer.Serialize(item);
				savedVal = eh.Encrypt(savedVal);
				File.WriteAllText(fileName, savedVal);
				if (hangfireServices.Find((SchedulerServiceModel f) => f.serviceId == item.serviceId) == null)
				{
					hangfireServices.Add(new SchedulerServiceModel
					{
						assemblyFileName = "HitServicesCore",
						classFullName = "HitServicesCore.InternalServices.ISRunSqlScriptService",
						description = item.ClassDescription,
						isActive = false,
						schedulerTime = "* * * * *",
						schedulerDescr = "Every minute",
						serviceId = item.serviceId,
						serviceName = item.serviceName,
						serviceType = item.serviceType,
						serviceVersion = ((!item.serviceVersion.HasValue) ? new long?(long.MinValue) : item.serviceVersion).ToString()
					});
				}
			}
		}
		catch (Exception ex)
		{
			result = false;
			logger.LogError(ex.ToString());
		}
		return result;
	}

	public List<ISSaveToTableModel> GetSaveToTableFromJsonFiles()
	{
		List<ISSaveToTableModel> result = new List<ISSaveToTableModel>();
		try
		{
			string isServicePath = Path.Combine(new string[3] { sysInfo.rootPath, "IS_Services", "SaveToTable" });
			if (!Directory.Exists(isServicePath))
			{
				return result;
			}
			if (isServicePath[isServicePath.Length - 1] != '\\')
			{
				isServicePath += "\\";
			}
			lock (lockJsons)
			{
				List<string> sqlScriptsJsons = Directory.EnumerateFiles(isServicePath, "*.json").ToList();
				foreach (string item in sqlScriptsJsons)
				{
					string sVal = File.ReadAllText(item, Encoding.Default);
					sVal = eh.Decrypt(sVal);
					if (!string.IsNullOrWhiteSpace(sVal))
					{
						result.Add(JsonSerializer.Deserialize<ISSaveToTableModel>(sVal));
					}
				}
			}
		}
		catch (Exception ex)
		{
			logger.LogError(ex.ToString());
		}
		return result;
	}

	public bool SaveSaveToTableJsons(List<ISSaveToTableModel> model)
	{
		bool result = true;
		string isServicePath = Path.Combine(new string[3] { sysInfo.rootPath, "IS_Services", "SaveToTable" });
		if (!Directory.Exists(isServicePath))
		{
			Directory.CreateDirectory(isServicePath);
		}
		if (isServicePath[isServicePath.Length - 1] != '\\')
		{
			isServicePath += "\\";
		}
		try
		{
			foreach (ISSaveToTableModel item in model)
			{
				_ = item.serviceId;
				if (item.serviceId == Guid.Empty)
				{
					item.serviceId = Guid.NewGuid();
					item.FullClassName = "HitServicesCore.InternalServices.ISSaveToTableService";
				}
				string fileName = isServicePath + item.serviceName;
				if (!fileName.EndsWith(".json"))
				{
					fileName += ".json";
				}
				item.serviceType = HangFireServiceTypeEnum.SaveToTable;
				if (item.SqlParameters != null)
				{
					item.SqlParameters = RemoveSpacesFromKeys(item.SqlParameters);
				}
				string savedVal = JsonSerializer.Serialize(item);
				savedVal = eh.Encrypt(savedVal);
				File.WriteAllText(fileName, savedVal);
				if (hangfireServices.Find((SchedulerServiceModel f) => f.serviceId == item.serviceId) == null)
				{
					hangfireServices.Add(new SchedulerServiceModel
					{
						assemblyFileName = "HitServicesCore",
						classFullName = "HitServicesCore.InternalServices.ISSaveToTableService",
						description = item.ClassDescription,
						isActive = false,
						schedulerTime = "* * * * *",
						schedulerDescr = "Every minute",
						serviceId = item.serviceId,
						serviceName = item.serviceName,
						serviceType = item.serviceType,
						serviceVersion = ((!item.serviceVersion.HasValue) ? new long?(long.MinValue) : item.serviceVersion).ToString()
					});
				}
			}
		}
		catch (Exception ex)
		{
			result = false;
			logger.LogError(ex.ToString());
		}
		return result;
	}

	public List<ISReadFromCsvModel> GetReadFromCsvFromJsonFiles()
	{
		List<ISReadFromCsvModel> result = new List<ISReadFromCsvModel>();
		try
		{
			string isServicePath = Path.Combine(new string[3] { sysInfo.rootPath, "IS_Services", "ReadCsv" });
			if (!Directory.Exists(isServicePath))
			{
				return result;
			}
			if (isServicePath[isServicePath.Length - 1] != '\\')
			{
				isServicePath += "\\";
			}
			lock (lockJsons)
			{
				List<string> sqlScriptsJsons = Directory.EnumerateFiles(isServicePath, "*.json").ToList();
				foreach (string item in sqlScriptsJsons)
				{
					string sVal = File.ReadAllText(item, Encoding.Default);
					sVal = eh.Decrypt(sVal);
					if (!string.IsNullOrWhiteSpace(sVal))
					{
						result.Add(JsonSerializer.Deserialize<ISReadFromCsvModel>(sVal));
					}
				}
			}
		}
		catch (Exception ex)
		{
			logger.LogError(ex.ToString());
		}
		return result;
	}

	public bool SaveReadFromCsvJsons(List<ISReadFromCsvModel> model)
	{
		bool result = true;
		string isServicePath = Path.Combine(new string[3] { sysInfo.rootPath, "IS_Services", "ReadCsv" });
		if (!Directory.Exists(isServicePath))
		{
			Directory.CreateDirectory(isServicePath);
		}
		if (isServicePath[isServicePath.Length - 1] != '\\')
		{
			isServicePath += "\\";
		}
		try
		{
			foreach (ISReadFromCsvModel item in model)
			{
				_ = item.serviceId;
				if (item.serviceId == Guid.Empty)
				{
					item.serviceId = Guid.NewGuid();
					item.FullClassName = "HitServicesCore.InternalServices.ISReadCsvService";
				}
				string fileName = isServicePath + item.serviceName;
				if (!fileName.EndsWith(".json"))
				{
					fileName += ".json";
				}
				item.serviceType = HangFireServiceTypeEnum.ReadFromCsv;
				string savedVal = JsonSerializer.Serialize(item);
				savedVal = eh.Encrypt(savedVal);
				File.WriteAllText(fileName, savedVal);
				if (hangfireServices.Find((SchedulerServiceModel f) => f.serviceId == item.serviceId) == null)
				{
					hangfireServices.Add(new SchedulerServiceModel
					{
						assemblyFileName = "HitServicesCore",
						classFullName = "HitServicesCore.InternalServices.ISReadCsvService",
						description = item.ClassDescription,
						isActive = false,
						schedulerTime = "* * * * *",
						schedulerDescr = "Every minute",
						serviceId = item.serviceId,
						serviceName = item.serviceName,
						serviceType = item.serviceType,
						serviceVersion = ((!item.serviceVersion.HasValue) ? new long?(long.MinValue) : item.serviceVersion).ToString()
					});
				}
			}
		}
		catch (Exception ex)
		{
			result = false;
			logger.LogError(ex.ToString());
		}
		return result;
	}

	public List<ISExportDataModel> GetExportdataFromJsonFiles()
	{
		List<ISExportDataModel> result = new List<ISExportDataModel>();
		try
		{
			string isServicePath = Path.Combine(new string[3] { sysInfo.rootPath, "IS_Services", "ExportData" });
			if (!Directory.Exists(isServicePath))
			{
				return result;
			}
			if (isServicePath[isServicePath.Length - 1] != '\\')
			{
				isServicePath += "\\";
			}
			lock (lockJsons)
			{
				List<string> sqlScriptsJsons = Directory.EnumerateFiles(isServicePath, "*.json").ToList();
				foreach (string item in sqlScriptsJsons)
				{
					string sVal = File.ReadAllText(item, Encoding.Default);
					sVal = eh.Decrypt(sVal);
					if (!string.IsNullOrWhiteSpace(sVal))
					{
						result.Add(JsonSerializer.Deserialize<ISExportDataModel>(sVal));
					}
				}
			}
		}
		catch (Exception ex)
		{
			logger.LogError(ex.ToString());
		}
		return result;
	}

	public bool SaveExportDataJsons(List<ISExportDataModel> model)
	{
		bool result = true;
		string isServicePath = Path.Combine(new string[3] { sysInfo.rootPath, "IS_Services", "ExportData" });
		if (!Directory.Exists(isServicePath))
		{
			Directory.CreateDirectory(isServicePath);
		}
		if (isServicePath[isServicePath.Length - 1] != '\\')
		{
			isServicePath += "\\";
		}
		try
		{
			foreach (ISExportDataModel item in model)
			{
				_ = item.serviceId;
				if (item.serviceId == Guid.Empty)
				{
					item.serviceId = Guid.NewGuid();
					item.FullClassName = "HitServicesCore.InternalServices.ISExportDataService";
				}
				string fileName = isServicePath + item.serviceName;
				if (!fileName.EndsWith(".json"))
				{
					fileName += ".json";
				}
				item.serviceType = HangFireServiceTypeEnum.ExportData;
				if (item.SqlParameters != null)
				{
					item.SqlParameters = RemoveSpacesFromListKeys(item.SqlParameters);
				}
				string savedVal = JsonSerializer.Serialize(item);
				savedVal = eh.Encrypt(savedVal);
				File.WriteAllText(fileName, savedVal);
				if (hangfireServices.Find((SchedulerServiceModel f) => f.serviceId == item.serviceId) == null)
				{
					hangfireServices.Add(new SchedulerServiceModel
					{
						assemblyFileName = "HitServicesCore",
						classFullName = "HitServicesCore.InternalServices.ISExportDataService",
						description = item.ClassDescription,
						isActive = false,
						schedulerTime = "* * * * *",
						schedulerDescr = "Every minute",
						serviceId = item.serviceId,
						serviceName = item.serviceName,
						serviceType = item.serviceType,
						serviceVersion = ((!item.serviceVersion.HasValue) ? new long?(long.MinValue) : item.serviceVersion).ToString()
					});
				}
			}
		}
		catch (Exception ex)
		{
			result = false;
			logger.LogError(ex.ToString());
		}
		return result;
	}
}
