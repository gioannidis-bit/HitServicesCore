using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
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

public class ExportDataFlows
{
	private readonly ISExportDataModel settings;

	private readonly SQLFlows scriptFlow;

	private readonly ILogger<ExportDataFlows> logger;

	private readonly IMapper mapper;

	private readonly IS_ServicesHelper isServicesHlp;

	private readonly SmtpHelper smtpHelper;

	private readonly EmailHelper emailHelper;

	private readonly SmtpExtModel smtpModel;

	private readonly ConvertDynamicHelper dynamicCast;

	private ConvertDataHelper convertDataHelper;

	private FileHelpers fileHelpers;

	private List<FtpExtModel> ftps;

	public ExportDataFlows(ISExportDataModel _settings)
	{
		if (DIHelper.AppBuilder != null)
		{
			IServiceProvider services = DIHelper.AppBuilder.ApplicationServices;
			logger = services.GetService<ILogger<ExportDataFlows>>();
			mapper = services.GetService<IMapper>();
			smtpHelper = services.GetService<SmtpHelper>();
		}
		settings = _settings;
		isServicesHlp = new IS_ServicesHelper();
		dynamicCast = new ConvertDynamicHelper(mapper);
		fileHelpers = new FileHelpers();
		scriptFlow = new SQLFlows(null);
		if (smtpHelper != null && smtpHelper._smhelper != null && smtpHelper._smhelper.Count > 0)
		{
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
		ConfigHelper cnfHlp = new ConfigHelper();
		ftps = cnfHlp.GetFtps();
	}

	private void SendEmails(bool succeeded, string sMess, bool sendMessOnSuccess = false, bool sendAttached = false)
	{
		if (smtpHelper == null || emailHelper == null || string.IsNullOrWhiteSpace(settings.sendEmailTo))
		{
			return;
		}
		EmailSendModel model = new EmailSendModel();
		model.To = settings.sendEmailTo.Split(',').ToList();
		if (succeeded)
		{
			model.Body = "[" + DateTime.Now.ToString() + "] Successfully executed export data " + settings.serviceName + (sendMessOnSuccess ? (" \r\n" + sMess) : "");
			if (settings.sendAttachedFiles == true)
			{
				model.AttachedFiles = new List<string>();
				if (!string.IsNullOrWhiteSpace(settings.FilePath) && File.Exists(settings.FilePath))
				{
					model.AttachedFiles.Add(settings.FilePath);
				}
				if (!string.IsNullOrWhiteSpace(settings.CsvFilePath) && File.Exists(settings.CsvFilePath))
				{
					model.AttachedFiles.Add(settings.CsvFilePath);
				}
				if (!string.IsNullOrWhiteSpace(settings.HtmlFilePath) && File.Exists(settings.HtmlFilePath))
				{
					model.AttachedFiles.Add(settings.HtmlFilePath);
				}
				if (!string.IsNullOrWhiteSpace(settings.PdfFilePath) && File.Exists(settings.PdfFilePath))
				{
					model.AttachedFiles.Add(settings.PdfFilePath);
				}
				if (!string.IsNullOrWhiteSpace(settings.JsonFilePath) && File.Exists(settings.JsonFilePath))
				{
					model.AttachedFiles.Add(settings.JsonFilePath);
				}
				if (!string.IsNullOrWhiteSpace(settings.XmlFilePath) && File.Exists(settings.XmlFilePath))
				{
					model.AttachedFiles.Add(settings.XmlFilePath);
				}
			}
		}
		else
		{
			model.Body = "[" + DateTime.Now.ToString() + "] Error on execution of export data " + settings.serviceName + " \r\n" + sMess;
		}
		model.From = smtpModel.sender;
		model.Subject = (string.IsNullOrWhiteSpace(settings.emailSubject) ? "Result After Export Data Execution" : settings.emailSubject);
		emailHelper.Send(model);
	}

	public IEnumerable<dynamic> ExportData()
	{
		try
		{
			string sqlScript = settings.SqlScript;
			sqlScript = scriptFlow.PrepareSqlListScript(sqlScript, settings.SqlParameters);
			IEnumerable<dynamic> newSqlParameters = null;
			int.TryParse(settings.DBTimeout, out var timeOut);
			List<IEnumerable<dynamic>> rawDataList = scriptFlow.RunMultySelect(sqlScript, settings.Custom1DB, timeOut);
			IEnumerable<dynamic> rawData = null;
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
			if (rawData == null || rawData.Count() < 1)
			{
				logger.LogInformation("There are no data to make export files");
				return null;
			}
			ExportData(rawData);
			scriptFlow.UpdateSqlListParams(newSqlParameters, settings.SqlParameters);
			List<ISExportDataModel> saveSettings = new List<ISExportDataModel>();
			if (settings.serviceVersion == 9223372036854775806L)
			{
				settings.serviceVersion = 0L;
			}
			else
			{
				settings.serviceVersion++;
			}
			saveSettings.Add(settings);
			isServicesHlp.SaveExportDataJsons(saveSettings);
			if (settings.sendEmailOnSuccess == true)
			{
				SendEmails(succeeded: true, "");
			}
			if (settings.sendAttachedFiles == true)
			{
				SendEmails(succeeded: true, "", sendMessOnSuccess: false, sendAttached: true);
			}
			return rawData;
		}
		catch (Exception ex)
		{
			if (settings.sendEmailOnSuccess == true)
			{
				SendEmails(succeeded: false, ex.Message + ((ex.InnerException != null) ? (" InnerException : " + ex.InnerException.Message) : ""));
			}
			logger.LogError(ex.ToString());
			return null;
		}
	}

	public string ExportData(dynamic rawData, string extension = "")
	{
		List<IDictionary<string, dynamic>> dictionary = null;
		if (!string.IsNullOrWhiteSpace(settings.FilePath) || (settings.ExportedFileToFTP != null && !string.IsNullOrWhiteSpace(settings.ExportedFileToFTP.FtpFileName)) || !string.IsNullOrEmpty(settings.CsvFilePath) || (settings.CsvFileToFTP != null && !string.IsNullOrWhiteSpace(settings.CsvFileToFTP.FtpFileName)) || !string.IsNullOrEmpty(settings.HtmlFilePath) || (settings.HtmlFileToFTP != null && !string.IsNullOrWhiteSpace(settings.HtmlFileToFTP.FtpFileName)) || !string.IsNullOrEmpty(settings.PdfFilePath) || (settings.PdfFileToFTP != null && !string.IsNullOrWhiteSpace(settings.PdfFileToFTP.FtpFileName)) || !string.IsNullOrEmpty(settings.JsonFilePath) || (settings.JsonFileToFTP != null && !string.IsNullOrWhiteSpace(settings.JsonFileToFTP.FtpFileName)) || !string.IsNullOrEmpty(settings.XmlFilePath) || (settings.XmlFileToFTP != null && !string.IsNullOrWhiteSpace(settings.XmlFileToFTP.FtpFileName)))
		{
			dictionary = dynamicCast.ToListDictionary(rawData);
		}
		if (!string.IsNullOrWhiteSpace(settings.FilePath) || (settings.ExportedFileToFTP != null && !string.IsNullOrWhiteSpace(settings.ExportedFileToFTP.FtpFileName)))
		{
			ToFile(dictionary);
		}
		if (!string.IsNullOrEmpty(settings.CsvFilePath))
		{
			ToCsv(dictionary);
		}
		if (!string.IsNullOrEmpty(settings.HtmlFilePath))
		{
			ToHtml(dictionary);
		}
		if (!string.IsNullOrEmpty(settings.PdfFilePath))
		{
			ToPdf(dictionary);
		}
		if (!string.IsNullOrEmpty(settings.JsonFilePath))
		{
			ToJson(dictionary);
		}
		if (!string.IsNullOrEmpty(settings.XmlFilePath))
		{
			ToXml(dictionary);
		}
		if (!string.IsNullOrEmpty(settings.RestServerUrl))
		{
			if (!(isIEnumerable(rawData) ? true : false))
			{
				return PostToRestServer<object>(rawData, extension);
			}
			return PostToRestServer<List<object>>(ToList(rawData), extension);
		}
		return "";
	}

	public bool isIEnumerable(dynamic rawData)
	{
		if (rawData is IEnumerable)
		{
			return true;
		}
		return false;
	}

	public List<dynamic> ToList(dynamic rawData)
	{
		return (rawData as IEnumerable<object>).ToList();
	}

	public void ToFile(List<IDictionary<string, dynamic>> data)
	{
		convertDataHelper = new ConvertDataHelper(CreateFormaters());
		string fl = convertDataHelper.ToStandarFile(data);
		if (!string.IsNullOrWhiteSpace(settings.FilePath))
		{
			fileHelpers.WriteTextToFile(settings.FilePath, fl, !string.IsNullOrWhiteSpace(settings.EncryptFile), (!string.IsNullOrWhiteSpace(settings.TimeStamp)) ? settings.TimeStamp : "yyyyMMddHHmmss", settings.EncryptFile ?? "", settings.EncryptType.GetValueOrDefault());
		}
		if (settings.ExportedFileToFTP != null && !string.IsNullOrWhiteSpace(settings.ExportedFileToFTP.FtpFileName))
		{
			FtpExtModel ftpextModel = null;
			if (!string.IsNullOrWhiteSpace(settings.ExportedFileToFTP.FTPAlias) && ftps != null && ftps.Count > 0)
			{
				ftpextModel = ftps.Find((FtpExtModel f) => f.alias == settings.ExportedFileToFTP.FTPAlias);
			}
			if (ftpextModel != null)
			{
				int.TryParse(ftpextModel.port, out var ftpPort);
				settings.ExportedFileToFTP.FTPAlias = ftpextModel.alias;
				settings.ExportedFileToFTP.FTPEncryptionMode = ftpextModel.encryptionMode;
				settings.ExportedFileToFTP.FtpPassword = ftpextModel.password;
				settings.ExportedFileToFTP.FtpPort = ftpPort;
				settings.ExportedFileToFTP.FtpServer = ftpextModel.ftp;
				settings.ExportedFileToFTP.FTPSslProtocols = ftpextModel.sslProtocols;
				settings.ExportedFileToFTP.FtpUsername = ftpextModel.username;
			}
			if (ftpextModel.sFtp == true)
			{
				fileHelpers.UploadToSftp(fl, settings.ExportedFileToFTP, (!string.IsNullOrWhiteSpace(settings.TimeStamp)) ? settings.TimeStamp : "yyyyMMddHHmmss", !string.IsNullOrWhiteSpace(settings.EncryptFile), settings.EncryptFile ?? "", settings.EncryptType.GetValueOrDefault());
			}
			else
			{
				fileHelpers.UploadToFTP(fl, settings.ExportedFileToFTP, (!string.IsNullOrWhiteSpace(settings.TimeStamp)) ? settings.TimeStamp : "yyyyMMddHHmmss", !string.IsNullOrWhiteSpace(settings.EncryptFile), settings.EncryptFile ?? "", settings.EncryptType.GetValueOrDefault());
			}
		}
		if (settings.sendEmailOnSuccess == true && !string.IsNullOrWhiteSpace(settings.sendEmailTo))
		{
			SendEmails(succeeded: true, fl, sendMessOnSuccess: true);
		}
	}

	public string PostToRestServer<T>(T data, string extension)
	{
		string ErrorMsg = "";
		int returnCode = 0;
		string result = "";
		WebApiHelper webHelper = new WebApiHelper();
		if (settings.RestServerHttpMethod.ToUpper() == "POST")
		{
			result = webHelper.Post(constractUrl(settings.RestServerUrl, extension), data, out returnCode, out ErrorMsg, settings.RestServerAuthenticationHeader, settings.RestServerCustomHeaders, settings.RestServerMediaType);
			if (returnCode != 200)
			{
				throw new Exception("Http Post Error: " + ErrorMsg + " Code: " + returnCode);
			}
			return result;
		}
		throw new Exception("No Post Http Method has been set.");
	}

	public void ToPdf(List<IDictionary<string, dynamic>> data)
	{
		convertDataHelper = new ConvertDataHelper(CreateFormaters());
		string html = convertDataHelper.ToHtml(data, settings.HtmlHeader == true, settings.PdfTitle, sortColumns: false, settings.Pdfcss);
		html = html.Replace("<style type=\"text/css\">", "");
		html = html.Replace("</style>", "");
		if (settings.PdfFileToFTP != null && !string.IsNullOrWhiteSpace(settings.PdfFileToFTP.FtpFileName))
		{
			FtpExtModel ftpextModel = null;
			if (!string.IsNullOrWhiteSpace(settings.PdfFileToFTP.FTPAlias) && ftps != null && ftps.Count > 0)
			{
				ftpextModel = ftps.Find((FtpExtModel f) => f.alias == settings.PdfFileToFTP.FTPAlias);
			}
			if (ftpextModel != null)
			{
				int.TryParse(ftpextModel.port, out var ftpPort);
				settings.PdfFileToFTP.FTPAlias = ftpextModel.alias;
				settings.PdfFileToFTP.FTPEncryptionMode = ftpextModel.encryptionMode;
				settings.PdfFileToFTP.FtpPassword = ftpextModel.password;
				settings.PdfFileToFTP.FtpPort = ftpPort;
				settings.PdfFileToFTP.FtpServer = ftpextModel.ftp;
				settings.PdfFileToFTP.FTPSslProtocols = ftpextModel.sslProtocols;
				settings.PdfFileToFTP.FtpUsername = ftpextModel.username;
			}
			if (ftpextModel.sFtp == true)
			{
				fileHelpers.UploadToSftp(html, settings.PdfFileToFTP, settings.TimeStamp);
			}
			else
			{
				fileHelpers.UploadToFTP(html, settings.PdfFileToFTP, settings.TimeStamp);
			}
		}
		else
		{
			fileHelpers.WriteHtmlToPdf(settings.PdfFilePath, html, settings.Pdfcss, settings.TimeStamp);
		}
	}

	public void ToXml(List<IDictionary<string, dynamic>> data)
	{
		convertDataHelper = new ConvertDataHelper(CreateFormaters());
		string xml = convertDataHelper.ToXml(data, settings.XmlRootElement, settings.XmlElement, mapper);
		if (settings.XmlFileToFTP != null && !string.IsNullOrWhiteSpace(settings.XmlFileToFTP.FtpFileName))
		{
			FtpExtModel ftpextModel = null;
			if (!string.IsNullOrWhiteSpace(settings.XmlFileToFTP.FTPAlias) && ftps != null && ftps.Count > 0)
			{
				ftpextModel = ftps.Find((FtpExtModel f) => f.alias == settings.XmlFileToFTP.FTPAlias);
			}
			if (ftpextModel != null)
			{
				int.TryParse(ftpextModel.port, out var ftpPort);
				settings.XmlFileToFTP.FTPAlias = ftpextModel.alias;
				settings.XmlFileToFTP.FTPEncryptionMode = ftpextModel.encryptionMode;
				settings.XmlFileToFTP.FtpPassword = ftpextModel.password;
				settings.XmlFileToFTP.FtpPort = ftpPort;
				settings.XmlFileToFTP.FtpServer = ftpextModel.ftp;
				settings.XmlFileToFTP.FTPSslProtocols = ftpextModel.sslProtocols;
				settings.XmlFileToFTP.FtpUsername = ftpextModel.username;
			}
			if (ftpextModel.sFtp == true)
			{
				fileHelpers.UploadToSftp(xml, settings.XmlFileToFTP, settings.TimeStamp);
			}
			else
			{
				fileHelpers.UploadToFTP(xml, settings.XmlFileToFTP, settings.TimeStamp);
			}
		}
		else
		{
			fileHelpers.WriteTextToFile(settings.XmlFilePath, xml, isEncrypted: false, settings.TimeStamp);
		}
	}

	public void ToJson(List<IDictionary<string, dynamic>> data)
	{
		convertDataHelper = new ConvertDataHelper(CreateFormaters());
		string json = convertDataHelper.ToJson(data);
		if (settings.JsonFileToFTP != null && !string.IsNullOrWhiteSpace(settings.JsonFileToFTP.FtpFileName))
		{
			FtpExtModel ftpextModel = null;
			if (!string.IsNullOrWhiteSpace(settings.JsonFileToFTP.FTPAlias) && ftps != null && ftps.Count > 0)
			{
				ftpextModel = ftps.Find((FtpExtModel f) => f.alias == settings.JsonFileToFTP.FTPAlias);
			}
			if (ftpextModel != null)
			{
				int.TryParse(ftpextModel.port, out var ftpPort);
				settings.JsonFileToFTP.FTPAlias = ftpextModel.alias;
				settings.JsonFileToFTP.FTPEncryptionMode = ftpextModel.encryptionMode;
				settings.JsonFileToFTP.FtpPassword = ftpextModel.password;
				settings.JsonFileToFTP.FtpPort = ftpPort;
				settings.JsonFileToFTP.FtpServer = ftpextModel.ftp;
				settings.JsonFileToFTP.FTPSslProtocols = ftpextModel.sslProtocols;
				settings.JsonFileToFTP.FtpUsername = ftpextModel.username;
			}
			if (ftpextModel.sFtp == true)
			{
				fileHelpers.UploadToSftp(json, settings.JsonFileToFTP, settings.TimeStamp);
			}
			else
			{
				fileHelpers.UploadToFTP(json, settings.JsonFileToFTP, settings.TimeStamp);
			}
		}
		else
		{
			fileHelpers.WriteTextToFile(settings.JsonFilePath, json, isEncrypted: false, settings.TimeStamp);
		}
	}

	public void ToCsv(List<IDictionary<string, dynamic>> data)
	{
		FtpExtModel ftpextModel = null;
		if (settings.CsvFileToFTP != null && !string.IsNullOrWhiteSpace(settings.CsvFileToFTP.FTPAlias) && ftps != null && ftps.Count > 0)
		{
			ftpextModel = ftps.Find((FtpExtModel f) => f.alias == settings.CsvFileToFTP.FTPAlias);
		}
		convertDataHelper = new ConvertDataHelper(CreateFormaters());
		string csv = convertDataHelper.ToCsv(data, settings.CsvFileHeader == true, settings.CsvDelimenter);
		if (ftpextModel != null)
		{
			int.TryParse(ftpextModel.port, out var ftpPort);
			string ftpFilePath = Path.GetFullPath(settings.CsvFileToFTP.FtpFileName);
			settings.CsvFileToFTP.FTPAlias = ftpextModel.alias;
			settings.CsvFileToFTP.FTPEncryptionMode = ftpextModel.encryptionMode;
			settings.CsvFileToFTP.FtpPassword = ftpextModel.password;
			settings.CsvFileToFTP.FtpPort = ftpPort;
			settings.CsvFileToFTP.FtpServer = ftpextModel.ftp;
			settings.CsvFileToFTP.FTPSslProtocols = ftpextModel.sslProtocols;
			settings.CsvFileToFTP.FtpUsername = ftpextModel.username;
			if (settings.CsvFileToFTP != null && !string.IsNullOrWhiteSpace(settings.CsvFileToFTP.FtpFileName))
			{
				if (ftpextModel.sFtp == true)
				{
					fileHelpers.UploadToSftp(csv, settings.CsvFileToFTP, settings.TimeStamp, isEncrypted: false, "", 0, (ftpextModel.sFtp == true) ? ftpFilePath : "");
				}
				else
				{
					fileHelpers.UploadToFTP(csv, settings.CsvFileToFTP, settings.TimeStamp, isEncrypted: false, "", 0, (ftpextModel.sFtp == true) ? ftpFilePath : "");
				}
			}
		}
		else
		{
			fileHelpers.WriteTextToFile(settings.CsvFilePath, csv, isEncrypted: false, settings.TimeStamp);
		}
	}

	public void ToHtml(List<IDictionary<string, dynamic>> data)
	{
		convertDataHelper = new ConvertDataHelper(CreateFormaters());
		string fl = convertDataHelper.ToHtml(data, settings.HtmlHeader == true, settings.HtmlTitle, settings.HtmlSortRows == true, settings.Htmlcss);
		if (settings.HtmlFileToFTP != null && !string.IsNullOrWhiteSpace(settings.HtmlFileToFTP.FtpFileName))
		{
			FtpExtModel ftpextModel = null;
			if (!string.IsNullOrWhiteSpace(settings.HtmlFileToFTP.FTPAlias) && ftps != null && ftps.Count > 0)
			{
				ftpextModel = ftps.Find((FtpExtModel f) => f.alias == settings.HtmlFileToFTP.FTPAlias);
			}
			if (ftpextModel != null)
			{
				int.TryParse(ftpextModel.port, out var ftpPort);
				settings.HtmlFileToFTP.FTPAlias = ftpextModel.alias;
				settings.HtmlFileToFTP.FTPEncryptionMode = ftpextModel.encryptionMode;
				settings.HtmlFileToFTP.FtpPassword = ftpextModel.password;
				settings.HtmlFileToFTP.FtpPort = ftpPort;
				settings.HtmlFileToFTP.FtpServer = ftpextModel.ftp;
				settings.HtmlFileToFTP.FTPSslProtocols = ftpextModel.sslProtocols;
				settings.HtmlFileToFTP.FtpUsername = ftpextModel.username;
			}
			if (ftpextModel.sFtp == true)
			{
				fileHelpers.UploadToSftp(fl, settings.HtmlFileToFTP, settings.TimeStamp);
			}
			else
			{
				fileHelpers.UploadToFTP(fl, settings.HtmlFileToFTP, settings.TimeStamp);
			}
		}
		else
		{
			fileHelpers.WriteTextToFile(settings.HtmlFilePath, fl, isEncrypted: false, settings.TimeStamp);
		}
	}

	private string constractUrl(string baseUrl, string extenstion)
	{
		if (extenstion == "")
		{
			return baseUrl;
		}
		return baseUrl + extenstion;
	}

	private Dictionary<string, Formater> CreateFormaters()
	{
		Dictionary<string, Formater> formaters = new Dictionary<string, Formater>();
		if (settings == null || settings.Formater == null)
		{
			return formaters;
		}
		foreach (string key in settings.Formater.Keys)
		{
			Formater f = new Formater();
			f.CultureInfoDescription = settings.CultureInfo;
			f.Format = settings.Formater[key];
			formaters.Add(key, f);
		}
		return formaters;
	}
}
