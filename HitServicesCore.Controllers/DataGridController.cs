using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using HitHelpersNetCore.Helpers;
using HitHelpersNetCore.Models;
using HitServicesCore.Filters;
using HitServicesCore.Helpers;
using HitServicesCore.Models;
using HitServicesCore.Models.Helpers;
using HitServicesCore.Models.SharedModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace HitServicesCore.Controllers;

public class DataGridController : Controller
{
	public class DictWrapper
	{
		private Dictionary<string, dynamic> config = new Dictionary<string, object>();

		public dynamic this[string someName]
		{
			get
			{
				return config[someName];
			}
			set
			{
				config[someName] = (object)value;
			}
		}

		public static DictWrapper Create(Dictionary<string, dynamic> nestedDict)
		{
			DictWrapper wrapper = new DictWrapper();
			wrapper.config = nestedDict;
			return wrapper;
		}
	}

	private readonly List<PlugInDescriptors> plugins;

	private readonly ManageConfiguration manconf;

	private readonly List<MainConfigurationModel> mainconfModel;

	private static string currentPluginId;

	private readonly LoginsUsers loginsUsers;

	private ILogger<DataGridController> logger;

	public DataGridController(LoginsUsers loginsUsers, ILogger<DataGridController> _logger, ManageConfiguration _manconf, List<PlugInDescriptors> _plugins, List<MainConfigurationModel> _mainconfModel)
	{
		manconf = _manconf;
		plugins = _plugins;
		mainconfModel = _mainconfModel;
		this.loginsUsers = loginsUsers;
		logger = _logger;
	}

	[ServiceFilter(typeof(LoginFilter))]
	public ActionResult Configuration(string pluginId)
	{
		List<string> keys = new List<string>();
		base.ViewBag.isAdmin = false;
		MainConfigurationModel myModel = new MainConfigurationModel();
		List<MainConfigurationModel> configList = manconf.GetConfigs();
		List<PluginHelper> pluginList = new List<PluginHelper>();
		foreach (PlugInDescriptors p in plugins)
		{
			pluginList.Add(new PluginHelper
			{
				plugIn_Id = p.mainDescriptor.plugIn_Id,
				plugIn_Name = p.mainDescriptor.plugIn_Name,
				routing = p.routing
			});
		}
		if (!pluginList.Exists((PluginHelper r) => r.plugIn_Id == default(Guid)))
		{
			pluginList.Insert(0, new PluginHelper
			{
				plugIn_Id = default(Guid),
				plugIn_Name = "Main Configuration"
			});
		}
		base.ViewBag.plugins = pluginList;
		base.ViewBag.ftpList = manconf.getFtps();
		base.ViewBag.smtpList = manconf.getSmtps();
		if (pluginId != null)
		{
			myModel = configList.Where((MainConfigurationModel x) => x.plugInId == new Guid(pluginId)).FirstOrDefault();
			if (myModel != null)
			{
				foreach (KeyValuePair<string, List<DescriptorsModel>> description in myModel.descriptors.descriptions)
				{
					foreach (DescriptorsModel mod in description.Value)
					{
						if (mod.Type.Contains("list,") || mod.Type == "mpehotelKeys")
						{
							keys.Add(mod.Key);
						}
					}
				}
			}
			base.ViewBag.keys = keys;
			if (myModel != null)
			{
				Dictionary<string, dynamic> values = myModel.config.config;
				base.ViewBag.values = (Dictionary<string, object>)values;
			}
			base.ViewBag.MyModel = myModel;
			base.ViewBag.MyListModel = GetListModel(myModel);
			base.ViewBag.plugInId = pluginId;
			currentPluginId = base.ViewBag.plugInId;
			if (!(pluginId != "{00000000-0000-0000-0000-000000000000}") || !(pluginId != "00000000-0000-0000-0000-000000000000"))
			{
				base.ViewBag.plugInname = "Main Configuration";
			}
			else
			{
				base.ViewBag.plugInname = plugins.Where((PlugInDescriptors x) => x.mainDescriptor.plugIn_Id == new Guid(pluginId)).FirstOrDefault().mainDescriptor.plugIn_Name;
			}
			if (loginsUsers.logins["isAdmin"] == true)
			{
				base.ViewBag.isAdmin = true;
			}
			List<PlugInDescriptors> plg = plugins;
			List<MainDescriptorWithAssemplyModel> mainDesc = new List<MainDescriptorWithAssemplyModel>();
			foreach (PlugInDescriptors model in plg)
			{
				mainDesc.Add(model.mainDescriptor);
			}
			base.ViewBag.plgs = mainDesc;
			return View();
		}
		return View();
	}

	public List<DescriptorsListModel> GetListModel(MainConfigurationModel myModel)
	{
		List<DescriptorsListModel> res = new List<DescriptorsListModel>();
		foreach (KeyValuePair<string, List<DescriptorsModel>> item in myModel.descriptors.descriptions)
		{
			res.Add(new DescriptorsListModel
			{
				Section = item.Key,
				Properties = item.Value
			});
		}
		return res;
	}

	[HttpPost]
	public async Task<IActionResult> SaveEditedData(Dictionary<string, string> SaveEditedData)
	{
		logger.LogInformation("Initiating Saving of Data");
		try
		{
			new MainConfigurationModel();
			List<MainConfigurationModel> configList = manconf.GetConfigs();
			if (currentPluginId == null)
			{
				currentPluginId = "{00000000-0000-0000-0000-000000000000}";
			}
			MainConfigurationModel myModel = configList.Where((MainConfigurationModel x) => x.plugInId == new Guid(currentPluginId)).FirstOrDefault();
			if (myModel == null)
			{
				myModel = configList[0];
			}
			List<PlugInDescriptors> _plugins = plugins;
			_plugins.Where((PlugInDescriptors x) => x.mainDescriptor.plugIn_Id == new Guid(currentPluginId)).FirstOrDefault();
			Dictionary<string, dynamic> dic = myModel.config.config;
			DictWrapper.Create(dic);
			foreach (KeyValuePair<string, List<DescriptorsModel>> description in myModel.descriptors.descriptions)
			{
				foreach (DescriptorsModel mod in description.Value)
				{
					if (dic.ContainsKey(mod.Key) && SaveEditedData.ContainsKey(mod.Key))
					{
						switch (mod.Type.ToLower())
						{
						case "list,db":
						{
							string[] lines = SaveEditedData[mod.Key]?.Split(new string[1] { "|" }, StringSplitOptions.None);
							List<string> listofdb = new List<string>(lines.Where((string x) => x != "" && x.Trim().ToLower().StartsWith("server")));
							GenericDBsHelper genericDBsHelper = new GenericDBsHelper();
							listofdb = genericDBsHelper.GetUniqueDbList(listofdb);
							dic[mod.Key] = listofdb;
							break;
						}
						case "list,string":
						{
							string[] linesstring = SaveEditedData[mod.Key]?.Split(new string[1] { "|" }, StringSplitOptions.None);
							List<string> listofstring = new List<string>(linesstring.Where((string x) => x != ""));
							dic[mod.Key] = listofstring;
							break;
						}
						case "list,mpehotel":
						{
							string lsmp = SaveEditedData[mod.Key];
							string[] hotelstrings = lsmp?.Split(new string[1] { "|" }, StringSplitOptions.None);
							List<string> listofmpeconfig = new List<string>(hotelstrings.Where((string x) => x != ""));
							List<HotelConfigModel> hotelModels = new List<HotelConfigModel>();
							if (listofmpeconfig != null && listofmpeconfig.Count > 0)
							{
								foreach (string hotelstring2 in listofmpeconfig)
								{
									HotelConfigModel model2 = new HotelConfigModel();
									string[] temp2 = hotelstring2?.Split(new string[1] { "<" }, StringSplitOptions.None);
									if (temp2 == null || temp2.Length < 2)
									{
										logger.LogError("invalid protel database data: " + lsmp);
										continue;
									}
									string[] descriptionParts2 = temp2[0]?.Split(">");
									if (descriptionParts2 == null || descriptionParts2.Length < 2)
									{
										logger.LogError("invalid protel database data: " + lsmp);
										continue;
									}
									model2.Db = descriptionParts2[0]?.Trim(' ');
									string[] idDBParts2 = descriptionParts2[0]?.Split("-");
									if (idDBParts2 == null || idDBParts2.Length < 3)
									{
										logger.LogError("invalid protel database data: " + lsmp);
										continue;
									}
									model2.mpehotel = idDBParts2[2];
									model2.HotelName = descriptionParts2[1];
									model2.Value = temp2[1]?.Replace("#", "\\");
									hotelModels.Add(model2);
								}
								dic[mod.Key] = hotelModels;
							}
							else
							{
								logger.LogError("invalid protel database data: " + lsmp);
							}
							break;
						}
						case "dictionary":
						{
							string dicModel = SaveEditedData[mod.Key];
							Dictionary<string, string> dicData = JsonSerializer.Deserialize<Dictionary<string, string>>(dicModel);
							dic[mod.Key] = dicData;
							break;
						}
						case "list,smtp":
						case "list,ftp":
						{
							string lsmplk = SaveEditedData[mod.Key];
							string[] hotellkeystrings = lsmplk?.Split(new string[1] { "|" }, StringSplitOptions.None);
							List<string> listofmpekeylist = new List<string>(hotellkeystrings.Where((string x) => x != ""));
							List<HotelConfigModel> hotelkeyslModels = new List<HotelConfigModel>();
							if (listofmpekeylist != null && listofmpekeylist.Count > 0)
							{
								foreach (string hotelstring in listofmpekeylist)
								{
									HotelConfigModel model = new HotelConfigModel();
									string[] temp = hotelstring?.Split(new string[1] { "<" }, StringSplitOptions.None);
									if (temp == null || temp.Length < 2)
									{
										logger.LogError("invalid mpehotel key data: " + lsmplk);
										continue;
									}
									string[] descriptionParts = temp[0]?.Split(">");
									if (descriptionParts == null || descriptionParts.Length < 2)
									{
										logger.LogError("invalid mpehotel key data: " + lsmplk);
										continue;
									}
									model.Db = descriptionParts[0]?.Trim(' ');
									string[] idDBParts = descriptionParts[0]?.Split("-");
									if (idDBParts == null || idDBParts.Length < 3)
									{
										logger.LogError("invalid mpehotel key data: " + lsmplk);
										continue;
									}
									model.mpehotel = idDBParts[2];
									model.HotelName = descriptionParts[1];
									model.Value = temp[1];
									hotelkeyslModels.Add(model);
								}
								dic[mod.Key] = hotelkeyslModels;
							}
							else
							{
								logger.LogError("invalid mpehotel key data: " + lsmplk);
							}
							break;
						}
						case "mpehotelkeys":
						{
							string lsmpk = SaveEditedData[mod.Key];
							string[] hotelkeystrings = lsmpk?.Split(new string[1] { "|" }, StringSplitOptions.None);
							List<string> listofmpekeys = new List<string>(hotelkeystrings.Where((string x) => x != ""));
							List<HotelConfigModel> hotelkeysModels = new List<HotelConfigModel>();
							if (listofmpekeys != null && listofmpekeys.Count > 0)
							{
								foreach (string hotelstring3 in listofmpekeys)
								{
									HotelConfigModel model3 = new HotelConfigModel();
									string[] temp3 = hotelstring3?.Split(new string[1] { "<" }, StringSplitOptions.None);
									if (temp3 == null || temp3.Length < 2)
									{
										logger.LogError("invalid mpehotel key data: " + lsmpk);
										continue;
									}
									string[] descriptionParts3 = temp3[0]?.Split(">");
									if (descriptionParts3 == null || descriptionParts3.Length < 2)
									{
										logger.LogError("invalid mpehotel key data: " + lsmpk);
										continue;
									}
									model3.Db = descriptionParts3[0]?.Trim(' ');
									string[] idDBParts3 = descriptionParts3[0]?.Split("-");
									if (idDBParts3 == null || idDBParts3.Length < 3)
									{
										logger.LogError("invalid mpehotel key data: " + lsmpk);
										continue;
									}
									model3.mpehotel = idDBParts3[2];
									model3.HotelName = descriptionParts3[1];
									model3.Value = temp3[1];
									hotelkeysModels.Add(model3);
								}
								dic[mod.Key] = hotelkeysModels;
							}
							else
							{
								logger.LogError("invalid mpehotel key data: " + lsmpk);
							}
							break;
						}
						case "customdb":
						{
							string dbval = SaveEditedData[mod.Key];
							dic[mod.Key] = dbval;
							break;
						}
						case "maindb":
						{
							string mdb = SaveEditedData[mod.Key];
							dic[mod.Key] = mdb;
							break;
						}
						case "smtp":
						case "ftp":
						{
							string sval2 = SaveEditedData[mod.Key]?.Replace("|", "");
							dic[mod.Key] = sval2;
							break;
						}
						case "encrypted":
						case "string":
						case "text":
						{
							string sval = SaveEditedData[mod.Key];
							dic[mod.Key] = sval;
							break;
						}
						case "bool":
						{
							bool bval = false;
							if (SaveEditedData.ContainsKey(mod.Key))
							{
								bval = bool.Parse(SaveEditedData[mod.Key]);
							}
							dic[mod.Key] = bval;
							break;
						}
						case "int":
						{
							int ival = int.Parse(SaveEditedData[mod.Key]);
							dic[mod.Key] = ival;
							break;
						}
						case "decimal":
						{
							decimal dval = decimal.Parse(SaveEditedData[mod.Key]);
							dic[mod.Key] = dval;
							break;
						}
						case "datetime":
						{
							DateTime daval = DateTime.Parse(SaveEditedData[mod.Key]);
							dic[mod.Key] = daval;
							break;
						}
						}
					}
					else if (dic.ContainsKey(mod.Key))
					{
						dic[mod.Key] = null;
					}
				}
			}
			configList.Remove(myModel);
			myModel.config.config = dic;
			configList.Add(myModel);
			manconf.SaveConfigs(configList);
		}
		catch (Exception ex)
		{
			logger.LogError("Error while Saving Data : " + ex);
			BadRequest();
		}
		return Ok();
	}

	[HttpPost]
	public IActionResult CheckConnections(string constr)
	{
		CheckConnectionsHelper cch = new CheckConnectionsHelper();
		string res = cch.CheckConnection(constr, 15);
		return Ok(res);
	}
}
