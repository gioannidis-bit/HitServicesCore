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

public class ReadCsvFlows
{
	private readonly ILogger<ReadCsvFlows> logger;

	private readonly ISReadFromCsvModel settings;

	private readonly SQLFlows scriptFlow;

	private readonly SmtpHelper smtpHelper;

	private readonly EmailHelper emailHelper;

	private readonly SmtpExtModel smtpModel;

	private readonly IMapper mapper;

	private readonly FileHelpers fh;

	private readonly ConvertDynamicHelper dynamicCast;

	private readonly IS_ServicesHelper isServicesHlp;

	public ReadCsvFlows(ISReadFromCsvModel _settings)
	{
		if (DIHelper.AppBuilder != null)
		{
			IServiceProvider services = DIHelper.AppBuilder.ApplicationServices;
			logger = services.GetService<ILogger<ReadCsvFlows>>();
			mapper = services.GetService<IMapper>();
			smtpHelper = services.GetService<SmtpHelper>();
		}
		settings = _settings;
		isServicesHlp = new IS_ServicesHelper();
		fh = new FileHelpers();
		dynamicCast = new ConvertDynamicHelper(mapper);
		scriptFlow = new SQLFlows(null);
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
				model.Body = "[" + DateTime.Now.ToString() + "] Succesfully readed data from csv file " + settings.serviceName;
			}
			else
			{
				model.Body = "[" + DateTime.Now.ToString() + "] Error on reading data from csv file " + settings.serviceName + " \r\n" + sMess;
			}
			model.From = smtpModel.sender;
			model.Subject = "Result After read data from csv file";
			emailHelper.Send(model);
		}
	}

	public void ReadFromCsv()
	{
		try
		{
			List<IDictionary<string, dynamic>> rawData = fh.ReadCsvFile(settings.CsvFilePath, settings.CsvDelimenter, hasHeader: false, mapper, settings.CsvFileHeaders, settings.Encoding, settings.CsvEncoding).ToList();
			string preSqlScript = settings.SqlDestPreScript;
			if (!string.IsNullOrEmpty(settings.SqlDestPreScript))
			{
				scriptFlow.RunScript(preSqlScript, settings.DestinationDB);
			}
			DbTableModel tableInfo = scriptFlow.GetTableInfo(settings.DestinationDB, settings.DestinationDBTableName);
			SaveCsvDataToDB(rawData, tableInfo);
			if (settings != null)
			{
				List<ISReadFromCsvModel> saveSettings = new List<ISReadFromCsvModel>();
				if (settings.serviceVersion == 9223372036854775806L)
				{
					settings.serviceVersion = 0L;
				}
				else
				{
					settings.serviceVersion++;
				}
				saveSettings.Add(settings);
				isServicesHlp.SaveReadFromCsvJsons(saveSettings);
			}
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
	}

	private void SaveDataToDB(dynamic rawData, DbTableModel tableinfo, string conString = null)
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

	private string CreateSqlFromSplittedValues(string[] splValues, int totalFields)
	{
		string result = "";
		int i = 0;
		foreach (string sVal in splValues)
		{
			result = ((!(sVal.ToUpper() == "NULL")) ? (result + "'" + sVal + "'") : (result + sVal));
			i++;
			if (i < totalFields)
			{
				result += ",";
			}
		}
		return result;
	}

	private void SaveCsvDataToDB(dynamic rawData, DbTableModel tableinfo)
	{
		List<IDictionary<string, dynamic>> dictionary = dynamicCast.ToListDictionary(rawData);
		if (dictionary == null)
		{
			return;
		}
		List<DBColumnModel> keyColumns = tableinfo.Columns.Where((DBColumnModel w) => w.PrimaryKey).ToList();
		List<string> executeData = new List<string>();
		List<ImportDataToTableFromCsvModel> insertData = new List<ImportDataToTableFromCsvModel>();
		List<CsvColumnsHeaderModel> columnsData = new List<CsvColumnsHeaderModel>();
		string delim = ((settings.CsvDelimenter.ToLower() == "comma") ? "," : ((settings.CsvDelimenter.ToLower() == "space") ? " " : ((!(settings.CsvDelimenter.ToLower() == "tab")) ? settings.CsvDelimenter : "\t")));
		SqlConstructorHelper sqlConstruct = new SqlConstructorHelper();
		if (settings.CsvFileHeader == true)
		{
			string header = dictionary.First().Values.OfType<string>().ToList().First();
			string[] splHeader = header.Split(delim);
			int idx = 0;
			foreach (IDictionary<string, object> item in dictionary)
			{
				if (idx == 0)
				{
					idx++;
					continue;
				}
				idx++;
				columnsData = new List<CsvColumnsHeaderModel>();
				foreach (dynamic dtValue in item.Values)
				{
					string[] splValues = dtValue.Split(delim);
					for (int i = 0; i < splValues.Count() - 1; i++)
					{
						columnsData.Add(new CsvColumnsHeaderModel
						{
							ColumnName = splHeader[i],
							ColumnValue = splValues[i]
						});
					}
					insertData.Add(new ImportDataToTableFromCsvModel
					{
						RowNo = idx,
						ColumnsData = new LinkedList<CsvColumnsHeaderModel>(columnsData.ToList())
					});
				}
			}
		}
		else
		{
			foreach (IDictionary<string, object> item2 in dictionary)
			{
				string tmpSql = "INSERT INTO " + tableinfo.TableName + " SELECT ";
				foreach (dynamic dtValue2 in item2.Values)
				{
					string[] splValues2 = dtValue2.Split(delim);
					executeData.Add(tmpSql + CreateSqlFromSplittedValues(splValues2, splValues2.Count()));
				}
			}
		}
		int.TryParse(settings.DBTimeout, out var timeOut);
		if (timeOut == 0)
		{
			timeOut = 60;
		}
		scriptFlow.SaveCsvToTable(executeData, insertData, keyColumns, tableinfo.TableName, settings.DestinationDB, settings.DBOperation, settings.DBTransaction, timeOut);
	}
}
