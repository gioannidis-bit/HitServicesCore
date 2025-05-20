using System;
using System.Collections.Generic;
using System.Linq;
using HitHelpersNetCore.Helpers;
using HitServicesCore.Helpers.Common;
using HitServicesCore.Models;
using HitServicesCore.Models.Helpers;
using Microsoft.Extensions.Logging;

namespace HitServicesCore.Helpers;

public class ValidationHelper
{
	private readonly ILogger<ValidationHelper> logger;

	private readonly StringCipher stringCipher;

	private readonly EncryptionHelper encrypt;

	public ValidationHelper(StringCipher stringCipher, ILogger<ValidationHelper> logger)
	{
		this.stringCipher = stringCipher;
		this.logger = logger;
		encrypt = new EncryptionHelper();
		this.stringCipher.Test("h_1.7=$,q+L!c3v($eg#n3r@t()R");
	}

	public bool Validate(ValidationModel model)
	{
		try
		{
			string flName = model.fileName.Replace(".lic", "", StringComparison.OrdinalIgnoreCase);
			string[] LicensesToBeCheck = null;
			LicensesToBeCheck = ((!model.fileRecords.Contains("--------------------")) ? new string[1] { model.fileRecords } : model.fileRecords.Split("--------------------"));
			if (!string.IsNullOrWhiteSpace(model.hitKey))
			{
				if (!model.hitKey.Equals(HitKeyHelper.hitKey, StringComparison.OrdinalIgnoreCase))
				{
					logger.LogInformation("Hit key is invalid");
					return false;
				}
			}
			else
			{
				model.hitKey = HitKeyHelper.hitKey;
			}
			string[] array = LicensesToBeCheck;
			foreach (string item in array)
			{
				string[] lines = item.Split("\n");
				lines = TrimFileRecord(lines);
				if (RemoveLabes(lines[2]) != model.serverId)
				{
					logger.LogInformation("Server Id is invalid");
					return false;
				}
				if (!model.applicationName.Equals(flName, StringComparison.OrdinalIgnoreCase))
				{
					logger.LogInformation("License program and execute program are differnet");
					return false;
				}
				LicenseModel license = new LicenseModel();
				license.CustomerName = lines[0];
				license.ApplicationName = lines[1];
				license.ServerId = lines[2];
				license.ExpirationDate = lines[3];
				license.HitKey = "HIT-KEY : " + model.hitKey;
				bool? isWebPosApiPhoneCenter = model.isWebPosApiPhoneCenter;
				if (isWebPosApiPhoneCenter.HasValue)
				{
					if (isWebPosApiPhoneCenter != true)
					{
						license.storeGuidId = lines[4];
						license.numberOfPos = lines[5];
						license.numberOfPda = lines[6];
						license.numberOfKds = lines[7];
						license.storeSql = encrypt.Decrypt(RemoveLabes(lines[8]));
						license.storeDbName = encrypt.Decrypt(RemoveLabes(lines[9]));
						license.storeUserName = encrypt.Decrypt(RemoveLabes(lines[10]));
						license.storePassword = encrypt.Decrypt(RemoveLabes(lines[11]));
						license.comments = lines[12];
						string odlLicense = lines[13];
						string newLic = CalcData(license, model.fileName);
						if (newLic != odlLicense)
						{
							logger.LogInformation("License is invalid");
							return false;
						}
					}
					else
					{
						if (model.isWebPosApiPhoneCenter == true)
						{
							license.daStores = lines[4];
						}
						license.comments = lines[lines.Count() - 2];
						string odlLicense = lines[lines.Count() - 1];
						string newLic = CalcData(license, model.fileName);
						if (newLic != odlLicense)
						{
							logger.LogInformation("License is invalid");
							return false;
						}
					}
				}
				else
				{
					license.comments = lines[lines.Count() - 2];
					string odlLicense = lines[lines.Count() - 1];
					string newLic = CalcData(license, model.fileName);
					if (newLic != odlLicense)
					{
						logger.LogInformation("License is invalid");
						return false;
					}
				}
			}
		}
		catch (Exception ex)
		{
			logger.LogError(ex.ToString());
			return false;
		}
		return true;
	}

	private string RemoveLabes(string value)
	{
		return value.Substring(value.IndexOf(" : ") + 3);
	}

	private string[] TrimFileRecord(string[] lines)
	{
		List<string> result = new List<string>();
		foreach (string item in lines)
		{
			string tmp = item.Replace("\r", "");
			if (!string.IsNullOrWhiteSpace(tmp.Trim()))
			{
				result.Add(tmp);
			}
		}
		return result.ToArray();
	}

	private string Sq(string value)
	{
		string dr = value.Substring(value.IndexOf(" : ") + 3);
		return encrypt.Decrypt(value);
	}

	public string GenerateData(LicenseForCreateModel master)
	{
		string result = "";
		string resModel = "";
		string flName = master.ApplicationName + ".lic";
		LicenseModel model = new LicenseModel();
		model.CustomerName = "CUSTOMER : " + master.CustomerName;
		resModel = resModel + model.CustomerName + "\n";
		model.ApplicationName = "APPLICATION : " + master.ApplicationName;
		resModel = resModel + model.ApplicationName + "\n";
		model.ServerId = "SERVER-ID : " + master.ServerId;
		resModel = resModel + model.ServerId + "\n";
		model.ExpirationDate = "EXPIRATION-DATE : " + master.ExpirationDate.ToString("dd/MM/yyyy");
		resModel = resModel + model.ExpirationDate + "\n";
		model.HitKey = "HIT-KEY : " + master.HitKey;
		model.comments = "COMMENTS : " + master.Comments;
		if (master.IsWebPosApiPhoneCenter.HasValue && master.IsWebPosApiPhoneCenter == true)
		{
			model.daStores = "DA-STORES : " + master.DaStores;
			resModel = resModel + model.daStores + "\n";
		}
		if (master.IsWebPosApiPhoneCenter.HasValue && master.IsWebPosApiPhoneCenter == false && master.StoreModel != null && master.StoreModel.Count > 0)
		{
			string headerModel = resModel;
			resModel = "";
			LicenseStoreModel lstItem = master.StoreModel.Last();
			foreach (LicenseStoreModel item in master.StoreModel)
			{
				string tmpResModel = "";
				model.storeGuidId = "STORE-ID : " + item.StoreGuidId.ToString();
				tmpResModel = tmpResModel + model.storeGuidId + "\n";
				model.numberOfPos = "POS : " + item.NumberOfPos;
				tmpResModel = tmpResModel + model.numberOfPos + "\n";
				model.numberOfPda = "PDA : " + item.NumberOfPda;
				tmpResModel = tmpResModel + model.numberOfPda + "\n";
				model.numberOfKds = "KDS : " + item.NumberOfKds;
				tmpResModel = tmpResModel + model.numberOfKds + "\n";
				model.storeSql = "SQL-SERVER : " + item.StoreSql;
				tmpResModel = tmpResModel + "SQL-SERVER : " + encrypt.Encrypt(item.StoreSql) + "\n";
				model.storeDbName = "SQL-DB : " + item.StoreDbName;
				tmpResModel = tmpResModel + "SQL-DB : " + encrypt.Encrypt(item.StoreDbName) + "\n";
				model.storeUserName = "SQL-User : " + item.StoreUserName;
				tmpResModel = tmpResModel + "SQL-User : " + encrypt.Encrypt(item.StoreUserName) + "\n";
				model.storePassword = "SQL-Password : " + item.StorePassword;
				tmpResModel = tmpResModel + "SQL-Password : " + encrypt.Encrypt(item.StorePassword) + "\n";
				result = CalcData(model, flName);
				resModel += headerModel;
				resModel += tmpResModel;
				resModel = resModel + model.comments + "\n";
				resModel += result;
				if (lstItem != item)
				{
					resModel += "\n--------------------\n";
				}
			}
		}
		else
		{
			result = CalcData(model, flName);
			resModel = resModel + model.comments + "\n";
			resModel += result;
		}
		return resModel;
	}

	private string CalcData(LicenseModel master, string fl)
	{
		string res = master.CustomerName + "\n";
		res = res + master.ApplicationName + "\n";
		res = res + master.ServerId + " \n";
		res = res + master.ExpirationDate + "\n";
		res = res + master.HitKey + "\n";
		if (master.isWebPosApiPhoneCenter.HasValue)
		{
			if (master.isWebPosApiPhoneCenter == true)
			{
				res = res + master.daStores + "\n";
			}
			else
			{
				res = res + master.storeGuidId + "\n";
				res = res + master.numberOfPos + "\n";
				res = res + master.numberOfPda + "\n";
				res = res + master.numberOfKds + "\n";
				res = res + master.storeSql + "\n";
				res = res + master.storeDbName + "\n";
				res = res + master.storeUserName + "\n";
				res = res + master.storePassword + "\n";
			}
		}
		res = res + master.comments + "\n";
		res = res + fl + "\n";
		string first = res.Substring(0, res.Length / 2);
		string second = res.Substring(res.Length / 2, res.Length / 2);
		second = Reverse(second);
		res = first + second;
		return stringCipher.ComputeSha256Hash(res);
	}

	private string Reverse(string s)
	{
		char[] charArray = s.ToCharArray();
		Array.Reverse(charArray);
		return new string(charArray);
	}
}
