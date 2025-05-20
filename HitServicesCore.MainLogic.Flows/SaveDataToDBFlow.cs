using System;
using System.Collections.Generic;
using System.Linq;
using AutoMapper;
using HitHelpersNetCore.Classes;
using HitHelpersNetCore.Helpers;
using HitHelpersNetCore.Models;
using HitServicesCore.Helpers;
using HitServicesCore.Models.IS_Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace HitServicesCore.MainLogic.Flows;

public class SaveDataToDBFlow
{
	private readonly ISSaveToTableModel settings;

	private readonly SQLFlows scriptFlow;

	private readonly ILogger<SaveDataToDBFlow> logger;

	private readonly IMapper mapper;

	private readonly ConvertDynamicHelper dynamicCast;

	private readonly IS_ServicesHelper isServicesHlp;

	private readonly SmtpHelper smtpHelper;

	private readonly EmailHelper emailHelper;

	private readonly SmtpExtModel smtpModel;

	public SaveDataToDBFlow(ISSaveToTableModel _settings)
	{
		if (DIHelper.AppBuilder != null)
		{
			IServiceProvider services = DIHelper.AppBuilder.ApplicationServices;
			logger = services.GetService<ILogger<SaveDataToDBFlow>>();
			mapper = services.GetService<IMapper>();
			smtpHelper = services.GetService<SmtpHelper>();
		}
		settings = _settings;
		scriptFlow = new SQLFlows(null);
		dynamicCast = new ConvertDynamicHelper(mapper);
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
				model.Body = "[" + DateTime.Now.ToString() + "] Succesfully executed save data to table " + settings.serviceName;
			}
			else
			{
				model.Body = "[" + DateTime.Now.ToString() + "] Error on execution of save data to table " + settings.serviceName + " \r\n" + sMess;
			}
			model.From = smtpModel.sender;
			model.Subject = "Result After Saved data to db tables";
			emailHelper.Send(model);
		}
	}

	public IEnumerable<dynamic> SaveDataToDB()
	{
		IEnumerable<dynamic> rawData = null;
		Dictionary<string, string> sqlScripts = new Dictionary<string, string>();
		sqlScripts.Add("MAIN", settings.SqlScript);
		if (!string.IsNullOrEmpty(settings.SqlDestPreScript))
		{
			sqlScripts.Add("PRE", settings.SqlDestPreScript);
		}
		else
		{
			sqlScripts.Add("PRE", null);
		}
		try
		{
			string sqlScript = sqlScripts["MAIN"];
			string preSqlScript = sqlScripts["PRE"];
			sqlScript = scriptFlow.PrepareSqlScript(sqlScript, settings.SqlParameters);
			int.TryParse(settings.DBTimeout, out var timeout);
			if (timeout == 0)
			{
				timeout = 60;
			}
			IEnumerable<dynamic> newSqlParameters = null;
			List<IEnumerable<dynamic>> rawDataList = scriptFlow.RunMultySelect(sqlScript, settings.SourceDB, timeout);
			if (rawDataList == null || rawDataList.Count() == 0)
			{
				rawData = new List<object>();
			}
			else
			{
				rawData = rawDataList[0];
				if (rawDataList.Count() >= 2)
				{
					newSqlParameters = rawDataList[1];
				}
			}
			if (!string.IsNullOrEmpty(settings.SqlDestPreScript))
			{
				scriptFlow.RunScript(preSqlScript, settings.DestinationDB);
			}
			DbTableModel tableInfo = scriptFlow.GetTableInfo(settings.DestinationDB, settings.DestinationDBTableName, timeout);
			SaveDataToDB(rawData, tableInfo);
			scriptFlow.UpdateSqlParams(newSqlParameters, settings.SqlParameters);
			List<ISSaveToTableModel> saveSettings = new List<ISSaveToTableModel>();
			if (settings.serviceVersion == 9223372036854775806L)
			{
				settings.serviceVersion = 0L;
			}
			else
			{
				settings.serviceVersion++;
			}
			saveSettings.Add(settings);
			isServicesHlp.SaveSaveToTableJsons(saveSettings);
			if (settings.sendEmailOnSuccess == true)
			{
				SendEmails(succeded: true, "");
			}
		}
		catch (Exception ex)
		{
			if (settings.sendEmailOnFailure == true)
			{
				SendEmails(succeded: false, ex.Message + ((ex.InnerException != null) ? (" InnerException : " + ex.InnerException.Message) : ""));
			}
			logger.LogError(ex.ToString());
		}
		return rawData;
	}

	public void SaveDataToDB(dynamic rawData, DbTableModel tableinfo, string conString = null)
	{
		if (conString == null)
		{
			conString = settings.DestinationDB;
		}
		if (!((rawData == null) ? true : false))
		{
			List<IDictionary<string, dynamic>> dictionary = dynamicCast.ToListDictionary(rawData);
			if (dictionary != null)
			{
				scriptFlow.SaveToTable(dictionary, conString, tableinfo, settings.DBOperation, settings.DBTransaction);
			}
		}
	}
}
