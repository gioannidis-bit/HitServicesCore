using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using HitCustomAnnotations.Classes;
using HitHelpersNetCore.Classes;
using HitHelpersNetCore.Helpers;
using HitHelpersNetCore.Models;
using HitServicesCore.Models.Helpers;
using Microsoft.Extensions.Logging;
using SendInvoiceViaEmail.Classes;
using SendInvoiceViaEmail.LocalModels;
using SendInvoiceViaEmail.MainLogic.Tasks;

namespace SendInvoiceViaEmail.MainLogic.Flows;

[AddClassesToContainer(/*Could not decode attribute arguments.*/)]
public class SendInvoiceViaEmailFlow
{
	private MainConfigurationModel localConfig;

	private List<KeyValueModel> keyValues;

	private SendInvoiceViaEmailTasks tasks;

	private readonly ILogger<SendInvoiceViaEmailFlow> logger;

	private EmailHelper emailHlp;

	private List<HotelConfigModel> subjectPerHotel;

	private List<HotelConfigModel> senderPerHotel;

	private List<HotelConfigModel> smtp;

	private List<SmtpExtModel> smtpList;

	private bool deleteFiles;

	private string[] copyChars;

	private string fileStructure = "";

	private string fileExtension = "";

	private string splitChar = "";

	private string filePath = "";

	private string emailFrom = "";

	private int emailDelay;

	private string backupFolder;

	public SendInvoiceViaEmailFlow(ILogger<SendInvoiceViaEmailFlow> logger)
	{
		this.logger = logger;
	}

	public void InitFlow()
	{
		//IL_0a8e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a98: Expected O, but got Unknown
		SendInvoiceViaEmailConfig sendInvoiceViaEmailConfig = new SendInvoiceViaEmailConfig();
		localConfig = ((AbstractConfigurationHelper)sendInvoiceViaEmailConfig).ReadConfiguration();
		try
		{
			string directoryName = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
			List<string> list = new List<string> { directoryName, "Config", "keyValues.json" };
			string fullPath = Path.GetFullPath(Path.Combine(list.ToArray()));
			if (File.Exists(fullPath))
			{
				keyValues = JsonSerializer.Deserialize<List<KeyValueModel>>(File.ReadAllText(fullPath));
			}
		}
		catch
		{
			keyValues = new List<KeyValueModel>();
		}
		MainConfigurationModel obj2 = localConfig;
		object obj3;
		if (obj2 == null)
		{
			obj3 = null;
		}
		else
		{
			MainConfiguration config = obj2.config;
			obj3 = ((config != null) ? config.config : null);
		}
		if (obj3 == null || !localConfig.config.config.ContainsKey("protelDBKey"))
		{
			logger.LogError("No value for protel database has been set on configuration");
			throw new Exception("No value for protel database has been set on configuration");
		}
		List<HotelConfigModel> list2 = (dynamic)localConfig.config.config["protelDBKey"];
		if (list2 == null || list2.Count < 1)
		{
			logger.LogError("No value for protel database has been set on configuration");
			throw new Exception("No value for protel database has been set on configuration");
		}
		string text = "proteluser";
		MainConfigurationModel obj4 = localConfig;
		object obj5;
		if (obj4 == null)
		{
			obj5 = null;
		}
		else
		{
			MainConfiguration config2 = obj4.config;
			obj5 = ((config2 != null) ? config2.config : null);
		}
		if (obj5 != null && localConfig.config.config.ContainsKey("protelDBSchema"))
		{
			text = (dynamic)localConfig.config.config["protelDBSchema"];
		}
		text = (string.IsNullOrWhiteSpace(text) ? "proteluser" : text);
		if (!localConfig.config.config.ContainsKey("invoicePath"))
		{
			logger.LogError("There is no path for saved pdf files");
			throw new Exception("There is no path for saved pdf files");
		}
		filePath = (dynamic)localConfig.config.config["invoicePath"];
		if (!filePath.EndsWith('\\'))
		{
			filePath += "\\";
		}
		if (!localConfig.config.config.ContainsKey("fileStructure"))
		{
			logger.LogError("There is no file structure");
			throw new Exception("There is no file structure");
		}
		fileStructure = (dynamic)localConfig.config.config["fileStructure"];
		if (!localConfig.config.config.ContainsKey("fileExtension"))
		{
			logger.LogError("There is no file extension. Set to default extension (pdf)");
			fileExtension = "pdf";
		}
		else
		{
			fileExtension = (dynamic)localConfig.config.config["fileExtension"];
		}
		if (string.IsNullOrWhiteSpace(fileExtension))
		{
			fileExtension = "pdf";
		}
		if (!fileExtension.StartsWith("."))
		{
			fileExtension = "." + fileExtension;
		}
		if (!localConfig.config.config.ContainsKey("smtpSettings"))
		{
			logger.LogError("There is no smtp to send email");
			throw new Exception("There is no smtp to send email");
		}
		smtp = (dynamic)localConfig.config.config["smtpSettings"];
		if (smtp == null || smtp.Count < 1)
		{
			logger.LogError("There is no smtp to send email");
			throw new Exception("There is no smtp to send email");
		}
		smtpList = ((AbstractConfigurationHelper)sendInvoiceViaEmailConfig).GetSmtps();
		if (smtpList == null || smtpList.Count < 1)
		{
			logger.LogError("No available smpts");
			throw new Exception("No available smpts");
		}
		foreach (HotelConfigModel item in smtp)
		{
			if (smtpList.Find((SmtpExtModel f) => f.alias == item.Value) == null)
			{
				logger.LogError("Smtp " + item.Value + " not exists");
				throw new Exception("Smtp " + item.Value + " not exists");
			}
		}
		if (!localConfig.config.config.ContainsKey("emailSubject"))
		{
			logger.LogError("There is no hotel initialized");
			throw new Exception("There is no hotel initialized");
		}
		subjectPerHotel = (dynamic)localConfig.config.config["emailSubject"];
		if (subjectPerHotel == null)
		{
			logger.LogError("There is no hotel initialized");
			throw new Exception("There is no hotel initialized");
		}
		if (!localConfig.config.config.ContainsKey("emailFrom"))
		{
			logger.LogError("There is no sender email");
			throw new Exception("There is no sender email");
		}
		senderPerHotel = (dynamic)localConfig.config.config["emailFrom"];
		if (senderPerHotel == null)
		{
			logger.LogError("There is no sender email");
			throw new Exception("There is no sender email");
		}
		splitChar = " ";
		if (localConfig.config.config.ContainsKey("splitChar"))
		{
			splitChar = (dynamic)localConfig.config.config["splitChar"];
		}
		if (localConfig.config.config.ContainsKey("emailDelay"))
		{
			emailDelay = (dynamic)localConfig.config.config["emailDelay"];
		}
		if (emailDelay < 1)
		{
			emailDelay = 15;
		}
		deleteFiles = true;
		if (localConfig.config.config.ContainsKey("deleteFiles"))
		{
			try
			{
				deleteFiles = (dynamic)localConfig.config.config["deleteFiles"];
			}
			catch
			{
			}
		}
		if (!deleteFiles)
		{
			backupFolder = "Backup";
			List<string> list3 = new List<string> { filePath, backupFolder };
			backupFolder = Path.GetFullPath(Path.Combine(list3.ToArray()));
			if (!backupFolder.EndsWith('\\'))
			{
				backupFolder += "\\";
			}
			if (!Directory.Exists(backupFolder))
			{
				Directory.CreateDirectory(backupFolder);
			}
		}
		if (localConfig.config.config.ContainsKey("copyCharsForFiles"))
		{
			try
			{
				string text2 = (dynamic)localConfig.config.config["copyCharsForFiles"];
				copyChars = text2.Split(",", StringSplitOptions.RemoveEmptyEntries);
			}
			catch
			{
			}
		}
		if (copyChars == null || copyChars.Length < 1)
		{
			copyChars = new string[4];
			copyChars[0] = "(";
			copyChars[0] = ")";
			copyChars[0] = "copy";
			copyChars[0] = "αντ";
		}
		emailHlp = new EmailHelper();
		tasks = new SendInvoiceViaEmailTasks(list2[0].Value, text);
	}

	private void DeleteFile(string fullfileName)
	{
		if (!deleteFiles)
		{
			try
			{
				File.Copy(fullfileName, Path.Combine(backupFolder, Path.GetFileName(fullfileName)), overwrite: true);
			}
			catch
			{
			}
		}
		try
		{
			if (File.Exists(fullfileName))
			{
				File.Delete(fullfileName);
			}
		}
		catch (Exception ex)
		{
			logger.LogError(ex.ToString());
		}
	}

	public void SendEmailtoCustomer()
	{
		//IL_0968: Unknown result type (might be due to invalid IL or missing references)
		//IL_096f: Expected O, but got Unknown
		string text = string.Empty;
		try
		{
			List<string> list = new List<string>();
			string errorMess = "";
			SendInvoiceViaEmailDTO sendInvoiceViaEmailDTO = null;
			if (!Directory.Exists(filePath))
			{
				sendInvoiceViaEmailDTO = new SendInvoiceViaEmailDTO();
				errorMess = "Directory " + filePath + " not exists.";
				logger.LogError(errorMess);
				tasks.AddErrorToProtelTable(errorMess, 0, 0, 0);
				sendInvoiceViaEmailDTO.ProfileId = -1;
				sendInvoiceViaEmailDTO.ProfileName = "";
				sendInvoiceViaEmailDTO.InvoiceNo = -1;
				sendInvoiceViaEmailDTO.Mpehotel = 0;
				sendInvoiceViaEmailDTO.EmailTo = "";
				sendInvoiceViaEmailDTO.ErrorMessage = errorMess;
				sendInvoiceViaEmailDTO.IssueDate = new DateTime(1900, 1, 1);
				sendInvoiceViaEmailDTO.StatusCode = "Fail";
				tasks.AddEmailStatusToDB(sendInvoiceViaEmailDTO);
				throw new Exception(errorMess);
			}
			int result = 0;
			int result2 = 0;
			int result3 = 0;
			int result4 = 0;
			text = DateTime.Now.Hour.ToString() + DateTime.Now.Minute + DateTime.Now.Second;
			List<string> list2 = new List<string> { filePath, text };
			text = Path.GetFullPath(Path.Combine(list2.ToArray()));
			if (!text.EndsWith('\\'))
			{
				text += "\\";
			}
			if (!Directory.Exists(text))
			{
				Directory.CreateDirectory(text);
			}
			string[] files = Directory.GetFiles(filePath, "*" + fileExtension, SearchOption.TopDirectoryOnly);
			if (files == null || files.Count() < 1)
			{
				return;
			}
			string[] array = files;
			foreach (string text2 in array)
			{
				string fileName = Path.GetFileName(text2);
				File.Move(text2, text + fileName);
			}
			string[] array2 = fileStructure.Split(splitChar, StringSplitOptions.RemoveEmptyEntries);
			string text3 = "";
			string text4 = "";
			files = Directory.GetFiles(text, "*" + fileExtension, SearchOption.TopDirectoryOnly);
			if (files == null || files.Count() < 1)
			{
				return;
			}
			string[] array3 = files;
			string pdfName;
			InvoiceModel invoiceData;
			foreach (string text5 in array3)
			{
				pdfName = Path.GetFileName(text5);
				if (copyChars.Any((string a) => pdfName.Contains(a, StringComparison.CurrentCultureIgnoreCase)))
				{
					DeleteFile(text5);
					continue;
				}
				string[] array4 = pdfName.Replace(fileExtension, "").Split(splitChar, StringSplitOptions.RemoveEmptyEntries);
				try
				{
					for (int num = 0; num < array2.Count(); num++)
					{
						if (array2[num].StartsWith("%"))
						{
							string text6 = "";
							text6 = ((!array4[num].Contains("(")) ? array4[num] : array4[num].Substring(0, array4[num].IndexOf("(")).Trim());
							if (string.Equals("%kdnr", array2[num]))
							{
								int.TryParse(text6, out result2);
							}
							else if (string.Equals("%leistacc", array2[num]))
							{
								int.TryParse(text6, out result);
							}
							else if (string.Equals("%rechnr", array2[num]))
							{
								int.TryParse(text6, out result3);
							}
							else if (string.Equals("%fisccode", array2[num]))
							{
								int.TryParse(text6, out result4);
							}
						}
					}
				}
				catch (Exception ex)
				{
					logger.LogError(ex.ToString());
					DeleteFile(text5);
					continue;
				}
				sendInvoiceViaEmailDTO = new SendInvoiceViaEmailDTO();
				sendInvoiceViaEmailDTO.ReservationId = result;
				sendInvoiceViaEmailDTO.InvoiceTypeId = 0;
				invoiceData = tasks.GetInvoiceData(result2, result3, result, out errorMess);
				if (!string.IsNullOrWhiteSpace(errorMess))
				{
					logger.LogError(errorMess);
					tasks.AddErrorToProtelTable(errorMess, (invoiceData == null) ? 1 : invoiceData.mpehotel, result, result2);
					sendInvoiceViaEmailDTO.ProfileId = result2;
					sendInvoiceViaEmailDTO.ReservationId = result;
					sendInvoiceViaEmailDTO.ProfileName = ((invoiceData == null) ? "" : (invoiceData.profileName ?? ""));
					sendInvoiceViaEmailDTO.InvoiceNo = result3;
					sendInvoiceViaEmailDTO.InvoiceTypeId = ((invoiceData != null) ? invoiceData.fisccode : 0);
					sendInvoiceViaEmailDTO.Mpehotel = ((invoiceData != null) ? invoiceData.mpehotel : 0);
					sendInvoiceViaEmailDTO.EmailTo = "";
					sendInvoiceViaEmailDTO.ErrorMessage = errorMess;
					sendInvoiceViaEmailDTO.IssueDate = ((invoiceData == null) ? new DateTime(1900, 1, 1) : invoiceData.issueDate);
					sendInvoiceViaEmailDTO.StatusCode = "Fail";
					tasks.AddEmailStatusToDB(sendInvoiceViaEmailDTO);
					DeleteFile(text5);
					continue;
				}
				if (invoiceData.mpehotel < 1)
				{
					DeleteFile(text5);
					continue;
				}
				if (invoiceData.email == null || invoiceData.email.Count < 1)
				{
					errorMess = "There is no email attached to profile id " + invoiceData.kdnr + " (" + invoiceData.profileName + ")";
					logger.LogError(errorMess);
					try
					{
						tasks.AddErrorToProtelTable(errorMess, (invoiceData == null) ? 1 : invoiceData.mpehotel, result, (invoiceData != null) ? invoiceData.kdnr : 0);
						sendInvoiceViaEmailDTO.ProfileId = result2;
						sendInvoiceViaEmailDTO.ProfileName = invoiceData.profileName;
						sendInvoiceViaEmailDTO.InvoiceNo = result3;
						sendInvoiceViaEmailDTO.InvoiceTypeId = invoiceData.fisccode;
						sendInvoiceViaEmailDTO.Mpehotel = invoiceData.mpehotel;
						sendInvoiceViaEmailDTO.EmailTo = "";
						sendInvoiceViaEmailDTO.ErrorMessage = errorMess;
						sendInvoiceViaEmailDTO.IssueDate = new DateTime(1900, 1, 1);
						sendInvoiceViaEmailDTO.StatusCode = "Fail";
						tasks.AddEmailStatusToDB(sendInvoiceViaEmailDTO);
						DeleteFile(text5);
					}
					catch (Exception ex2)
					{
						logger.LogError(ex2.ToString());
					}
					continue;
				}
				HotelConfigModel fldSmtp = smtp.Find((HotelConfigModel f) => f.mpehotel == invoiceData.mpehotel.ToString());
				if (fldSmtp == null)
				{
					logger.LogInformation("Smtps count " + JsonSerializer.Serialize(smtp));
					logger.LogInformation("Mpehotel from Reservation : " + invoiceData.mpehotel);
					logger.LogError("Smtp for mpehotel " + invoiceData.mpehotel + " not initialized");
					continue;
				}
				SmtpExtModel val = smtpList.Find((SmtpExtModel f) => f.alias == fldSmtp.Value);
				emailHlp.Init(val.smtp, int.Parse(val.port), val.ssl.GetValueOrDefault() != 0, val.username, val.password);
				HotelConfigModel val2 = subjectPerHotel.Find((HotelConfigModel f) => f.mpehotel == invoiceData.mpehotel.ToString());
				if (val2 == null)
				{
					DeleteFile(text5);
					continue;
				}
				text3 = val2.Value;
				val2 = senderPerHotel.Find((HotelConfigModel f) => f.mpehotel == invoiceData.mpehotel.ToString());
				if (val2 == null)
				{
					DeleteFile(text5);
					continue;
				}
				emailFrom = val2.Value;
				if (localConfig.config.config.ContainsKey("emailBody"))
				{
					text4 = (dynamic)localConfig.config.config["emailBody"];
				}
				if (text4 == null)
				{
					text4 = "";
				}
				if (text3 == null)
				{
					text3 = "";
				}
				List<FieldsModel> list3 = ProtelValuesPerTableAndKey(invoiceData.kdnr, result, invoiceData.mpehotel, result4, invoiceData.rechnr);
				foreach (FieldsModel item in list3)
				{
					text3 = text3.Replace(item.keyValue, item.protelValue);
					text4 = text4.Replace(item.keyValue, item.protelValue);
				}
				EmailSendModel val3 = new EmailSendModel();
				val3.Body = text4;
				val3.From = (string.IsNullOrWhiteSpace(emailFrom) ? val.sender : emailFrom);
				val3.Subject = text3;
				val3.To = new List<string>();
				val3.To.AddRange(invoiceData.email);
				val3.AttachedFiles = new List<string> { text5 };
				try
				{
					emailHlp.Send(val3);
				}
				catch (Exception ex3)
				{
					logger.LogError(ex3.ToString());
					try
					{
						list.Add("Cannot send file " + text5 + " to emails : " + string.Join("-", val3.To) + " \r\n" + ex3.ToString());
						sendInvoiceViaEmailDTO.ProfileId = invoiceData.kdnr;
						sendInvoiceViaEmailDTO.ProfileName = invoiceData.profileName;
						sendInvoiceViaEmailDTO.InvoiceNo = invoiceData.rechnr;
						sendInvoiceViaEmailDTO.InvoiceTypeId = invoiceData.fisccode;
						sendInvoiceViaEmailDTO.Mpehotel = invoiceData.mpehotel;
						sendInvoiceViaEmailDTO.EmailTo = string.Join("-", val3.To);
						sendInvoiceViaEmailDTO.ErrorMessage = "Cannot send file " + text5 + " \r\n" + ex3.Message + ((ex3.InnerException != null && !string.IsNullOrWhiteSpace(ex3.InnerException.Message)) ? (" (" + ex3.InnerException.Message + ")") : "");
						sendInvoiceViaEmailDTO.IssueDate = invoiceData.issueDate;
						sendInvoiceViaEmailDTO.StatusCode = "Fail";
						tasks.AddEmailStatusToDB(sendInvoiceViaEmailDTO);
					}
					catch (Exception ex4)
					{
						logger.LogError(ex4.ToString());
					}
					continue;
				}
				DeleteFile(text5);
				sendInvoiceViaEmailDTO.ProfileId = invoiceData.kdnr;
				sendInvoiceViaEmailDTO.ProfileName = invoiceData.profileName;
				sendInvoiceViaEmailDTO.InvoiceNo = invoiceData.rechnr;
				sendInvoiceViaEmailDTO.InvoiceTypeId = invoiceData.fisccode;
				sendInvoiceViaEmailDTO.Mpehotel = invoiceData.mpehotel;
				sendInvoiceViaEmailDTO.EmailTo = string.Join(";", invoiceData.email);
				sendInvoiceViaEmailDTO.ErrorMessage = "";
				sendInvoiceViaEmailDTO.IssueDate = invoiceData.issueDate;
				sendInvoiceViaEmailDTO.StatusCode = "Success";
				tasks.AddEmailStatusToDB(sendInvoiceViaEmailDTO);
				DateTime dateTime = DateTime.Now.AddSeconds(emailDelay);
				while (dateTime > DateTime.Now)
				{
				}
			}
			if (list == null || list.Count <= 0)
			{
				return;
			}
			errorMess = "";
			foreach (string item2 in list)
			{
				errorMess = errorMess + item2 + " \r\n";
			}
			logger.LogError(errorMess);
			throw new Exception(errorMess);
		}
		catch (Exception ex5)
		{
			logger.LogError(ex5.ToString());
			throw new Exception(ex5.ToString());
		}
		finally
		{
			string[] files2 = Directory.GetFiles(text);
			string[] array5 = files2;
			foreach (string fullfileName in array5)
			{
				try
				{
					DeleteFile(fullfileName);
				}
				catch
				{
				}
			}
			try
			{
				Directory.Delete(text);
			}
			catch
			{
			}
		}
	}

	private List<FieldsModel> ProtelValuesPerTableAndKey(int profileId, int leistacc, int mpehotel, int fisccode, int rechnr)
	{
		List<FieldsModel> list = new List<FieldsModel>();
		List<SelectValuesModel> list2 = new List<SelectValuesModel>();
		foreach (KeyValueModel item in keyValues)
		{
			SelectValuesModel selectValuesModel = list2.Find((SelectValuesModel f) => f.tableName == item.tableName);
			if (selectValuesModel == null)
			{
				list2.Add(new SelectValuesModel
				{
					tableName = item.tableName,
					fields = new List<FieldsModel>()
				});
			}
			selectValuesModel = list2.Find((SelectValuesModel f) => f.tableName == item.tableName);
			FieldsModel fieldsModel = selectValuesModel.fields.Find((FieldsModel f) => f.keyValue == item.keyName);
			if (fieldsModel == null)
			{
				selectValuesModel.fields.Add(new FieldsModel
				{
					keyValue = item.keyName,
					protelValue = item.fieldName
				});
			}
		}
		if (list2.Count > 0)
		{
			string text = "";
			foreach (SelectValuesModel item2 in list2)
			{
				if (string.Equals(item2.tableName, "kunden", StringComparison.OrdinalIgnoreCase))
				{
					text = " kdnr = " + profileId;
				}
				else if (string.Equals(item2.tableName, "buch", StringComparison.OrdinalIgnoreCase))
				{
					text = " leistacc = " + leistacc + " AND (globbnr < 1 OR (globbnr > 0 AND umzdurch = 1)) ";
				}
				else if (string.Equals(item2.tableName, "lizenz", StringComparison.OrdinalIgnoreCase) || string.Equals(item2.tableName, "datum", StringComparison.OrdinalIgnoreCase))
				{
					text = " mpehotel = " + mpehotel;
				}
				else if (string.Equals(item2.tableName, "fiscalcd", StringComparison.OrdinalIgnoreCase))
				{
					text = " ref = " + fisccode;
				}
				else if (string.Equals(item2.tableName, "rechhist", StringComparison.OrdinalIgnoreCase))
				{
					text = " fisccode = " + fisccode + " AND rechnr = " + rechnr;
				}
				else if (string.Equals(item2.tableName, "SendInvoiceViaEmailParameters", StringComparison.OrdinalIgnoreCase))
				{
					text = " mpehotel = " + mpehotel;
				}
				if (string.IsNullOrWhiteSpace(text))
				{
					text = " 1 = 1 ";
				}
				list.AddRange(tasks.GetProtelValues(item2, text));
			}
		}
		return list;
	}
}
