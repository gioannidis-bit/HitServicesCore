using System;
using System.Collections.Generic;
using System.Linq;
using AutoMapper;
using HitHelpersNetCore.Classes;
using HitHelpersNetCore.Helpers;
using HitHelpersNetCore.Models;
using HitServicesCore.Helpers;
using HitServicesCore.MainLogic.Tasks;
using HitServicesCore.Models.IS_Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace HitServicesCore.MainLogic.Flows;

public class SQLFlows
{
	private readonly ILogger<SQLFlows> logger;

	private readonly ISRunSqlScriptsModel settings;

	private readonly SQLTasks sqlTasks;

	private readonly ConvertDynamicHelper castDynamic;

	private readonly IMapper mapper;

	private readonly IS_ServicesHelper isServicesHlp;

	private readonly SmtpHelper smtpHelper;

	private readonly EmailHelper emailHelper;

	private readonly SmtpExtModel smtpModel;

	public SQLFlows(ISRunSqlScriptsModel _settings)
	{
		if (DIHelper.AppBuilder != null)
		{
			IServiceProvider services = DIHelper.AppBuilder.ApplicationServices;
			logger = services.GetService<ILogger<SQLFlows>>();
			mapper = services.GetService<IMapper>();
			smtpHelper = services.GetService<SmtpHelper>();
		}
		settings = _settings;
		sqlTasks = new SQLTasks(_settings);
		castDynamic = new ConvertDynamicHelper(mapper);
		isServicesHlp = new IS_ServicesHelper();
		if (smtpHelper == null || smtpHelper._smhelper == null || smtpHelper._smhelper.Count <= 0)
		{
			return;
		}
		try
		{
			smtpModel = smtpHelper._smhelper[0];
			if (!string.IsNullOrWhiteSpace(smtpModel.smtp) && !string.IsNullOrWhiteSpace(smtpModel.port) && !string.IsNullOrWhiteSpace(smtpModel.username) && !string.IsNullOrWhiteSpace(smtpModel.password) && !string.IsNullOrWhiteSpace(smtpModel.sender))
			{
				emailHelper = new EmailHelper();
				emailHelper.Init(smtpModel.smtp, Convert.ToInt32(smtpModel.port), Convert.ToBoolean(smtpModel.ssl), smtpModel.username, smtpModel.password);
			}
		}
		catch (Exception ex)
		{
			logger.LogError(ex.ToString());
			throw new Exception(ex.ToString());
		}
	}

	private void SendEmails(bool succeded, string sMess)
	{
		if (smtpHelper != null && emailHelper != null && !string.IsNullOrWhiteSpace(settings.sendEmailTo))
		{
			EmailSendModel model = new EmailSendModel();
			model.To = settings.sendEmailTo.Split(',').ToList();
			if (succeded)
			{
				model.Body = "[" + DateTime.Now.ToString() + "] Succesfully executed sql script " + settings.serviceName;
			}
			else
			{
				model.Body = "[" + DateTime.Now.ToString() + "] Error on execution of sql script " + settings.serviceName + " \r\n" + sMess;
			}
			model.From = smtpModel.sender;
			model.Subject = "Result After Sql Script Execution";
			emailHelper.Send(model);
		}
	}

	public void RunScript(string sqlScript, string dbConnection)
	{
		try
		{
			sqlScript = PrepareSqlScript(sqlScript);
			IEnumerable<dynamic> newSqlParameters = sqlTasks.RunSelect(sqlScript, dbConnection);
			if (settings != null)
			{
				if (settings.SqlParameters != null && settings.SqlParameters.Count > 0)
				{
					UpdateSqlParams(newSqlParameters, settings.SqlParameters);
				}
				List<ISRunSqlScriptsModel> saveSettings = new List<ISRunSqlScriptsModel>();
				if (settings.serviceVersion == 9223372036854775806L)
				{
					settings.serviceVersion = 0L;
				}
				else
				{
					settings.serviceVersion++;
				}
				saveSettings.Add(settings);
				isServicesHlp.SaveRunsSqlScriptsJsons(saveSettings);
				if (settings.sendEmailOnSuccess == true)
				{
					SendEmails(succeded: true, "");
				}
			}
		}
		catch (Exception ex)
		{
			if (settings != null && settings.sendEmailOnFailure == true)
			{
				SendEmails(succeded: false, ex.Message + ((ex.InnerException != null) ? (" InnerException : " + ex.InnerException.Message) : ""));
			}
			logger.LogError(ex.ToString());
			throw;
		}
	}

	public string PrepareSqlScript(string sqlScript)
	{
		try
		{
			if (settings == null)
			{
				return sqlScript;
			}
			if (settings.SqlParameters == null || settings.SqlParameters.Count == 0)
			{
				return sqlScript;
			}
			foreach (string key in settings.SqlParameters.Keys)
			{
				string value = settings.SqlParameters[key].Replace("'", "''");
				sqlScript = sqlScript.Replace(key.Trim(), "'" + value.Trim() + "'");
			}
			return sqlScript;
		}
		catch (Exception ex)
		{
			logger.LogError(ex.ToString());
			throw new Exception(ex.ToString());
		}
	}

	public string PrepareSqlListScript(string sqlScript, List<BaseKeyValueModel> SqlParameters)
	{
		try
		{
			if (SqlParameters == null || SqlParameters.Count == 0)
			{
				return sqlScript;
			}
			foreach (BaseKeyValueModel row in SqlParameters)
			{
				string value = row.value.Replace("'", "''");
				sqlScript = sqlScript.Replace(row.key, "'" + value + "'");
			}
			return sqlScript;
		}
		catch (Exception ex)
		{
			logger.LogError(ex.ToString());
			throw new Exception(ex.ToString());
		}
	}

	public string PrepareSqlScript(string sqlScript, Dictionary<string, string> SqlParameters)
	{
		try
		{
			if (SqlParameters == null || SqlParameters.Count == 0)
			{
				return sqlScript;
			}
			foreach (string key in SqlParameters.Keys)
			{
				string value = SqlParameters[key].Replace("'", "''");
				sqlScript = sqlScript.Replace(key, "'" + value + "'");
			}
			return sqlScript;
		}
		catch (Exception ex)
		{
			logger.LogError(ex.ToString());
			throw new Exception(ex.ToString());
		}
	}

	public void UpdateSqlListParams(IEnumerable<dynamic> newSqlParameters, List<BaseKeyValueModel> SqlParameters)
	{
		if (newSqlParameters == null)
		{
			return;
		}
		try
		{
			IDictionary<string, dynamic> sqlParamsDict = castDynamic.ToListDictionary(newSqlParameters).FirstOrDefault();
			if (sqlParamsDict == null)
			{
				return;
			}
			foreach (string key in sqlParamsDict.Keys)
			{
				BaseKeyValueModel item = SqlParameters.Find((BaseKeyValueModel r) => r.key == "@" + key.Trim());
				if (item != null)
				{
					item.value = ConvertDynamicValueToString(sqlParamsDict[key]);
					continue;
				}
				item = SqlParameters.Find((BaseKeyValueModel r) => r.key == key.Trim());
				if (item != null)
				{
					item.value = ConvertDynamicValueToString(sqlParamsDict[key]);
				}
			}
		}
		catch (Exception ex)
		{
			logger.LogError(ex.ToString());
			throw new Exception(ex.ToString());
		}
	}

	public void UpdateSqlParams(IEnumerable<dynamic> newSqlParameters, Dictionary<string, string> SqlParameters)
	{
		if (newSqlParameters == null)
		{
			return;
		}
		try
		{
			IDictionary<string, dynamic> sqlParamsDict = castDynamic.ToListDictionary(newSqlParameters).FirstOrDefault();
			if (sqlParamsDict == null)
			{
				return;
			}
			foreach (string key in sqlParamsDict.Keys)
			{
				if (SqlParameters.ContainsKey("@" + key.Trim()))
				{
					SqlParameters["@" + key] = ConvertDynamicValueToString(sqlParamsDict[key]);
				}
				else if (SqlParameters.ContainsKey(key))
				{
					SqlParameters[key] = ConvertDynamicValueToString(sqlParamsDict[key]);
				}
			}
		}
		catch (Exception ex)
		{
			logger.LogError(ex.ToString());
			throw new Exception(ex.ToString());
		}
	}

	private string ConvertDynamicValueToString(dynamic value)
	{
		try
		{
			string type = value.GetType().Name;
			if (type.ToLower().Contains("date"))
			{
				return value.ToString("yyyy-MM-dd");
			}
			return value.ToString();
		}
		catch (Exception ex)
		{
			logger.LogError(ex.ToString());
			throw;
		}
	}

	public List<IEnumerable<dynamic>> RunMultySelect(string sqlScript, string conString, int timeout = 0)
	{
		try
		{
			return sqlTasks.RunMultySelect(sqlScript, conString, timeout);
		}
		catch (Exception ex)
		{
			logger.LogError(ex.ToString());
			throw;
		}
	}

	public DbTableModel GetTableInfo(string constr, string tableName, int timeout = 60)
	{
		try
		{
			return sqlTasks.GetTableInfo(constr, tableName, timeout);
		}
		catch (Exception ex)
		{
			logger.LogError(ex.ToString());
			throw;
		}
	}

	public void SaveToTable(List<IDictionary<string, dynamic>> data, string constr, DbTableModel tableinfo, int operation, bool useTransaction, int timeout = 60)
	{
		try
		{
			sqlTasks.SaveToTable(data, constr, tableinfo, operation, useTransaction, timeout);
		}
		catch (Exception ex)
		{
			logger.LogError(ex.ToString());
			throw;
		}
	}

	public void SaveCsvToTable(List<string> executeData, List<ImportDataToTableFromCsvModel> command, List<DBColumnModel> keyColumns, string tableName, string constr, int operation, bool useTransaction, int timeout = 60)
	{
		try
		{
			sqlTasks.SaveCsvToTable(executeData, command, keyColumns, tableName, constr, operation, useTransaction, timeout);
		}
		catch (Exception ex)
		{
			logger.LogError(ex.ToString());
			throw;
		}
	}
}
