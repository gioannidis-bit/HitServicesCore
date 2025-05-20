using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.IO;
using System.Reflection;
using System.Text.Json;
using HitCustomAnnotations.Classes;
using HitCustomAnnotations.Interfaces;
using HitHelpersNetCore.Models;
using HitHelpersNetCore.Models.SharedModels;
using HitServicesCore.Enum;
using HitServicesCore.Models;
using HitServicesCore.Models.SharedModels;
using Microsoft.Extensions.DependencyInjection;
using NLog;
using NLog.Web;

namespace HitServicesCore.Helpers;

public class PlugInAnnotationedClassesHelper
{
	private readonly SystemInfo sysInfo;

	private string CurrentPath;

	public PlugInAnnotationedClassesHelper(SystemInfo _sysInfo)
	{
		sysInfo = _sysInfo;
		CurrentPath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
		Directory.SetCurrentDirectory(CurrentPath);
	}

	private bool CheckIfPlugInCanAddedToService(Assembly assembly)
	{
		bool result = false;
		Type[] exportedTypes = assembly.GetExportedTypes();
		foreach (Type item in exportedTypes)
		{
			if (typeof(IMainDescriptor).IsAssignableFrom(item))
			{
				result = true;
				break;
			}
		}
		return result;
	}

	public void AddClassesToDI_OnStartUp(IServiceCollection services)
	{
		List<string> ps = new List<string> { CurrentPath, "Config", "NLog.config" };
		string logpath = Path.GetFullPath(Path.Combine(ps.ToArray()));
		Logger logger = NLogBuilder.ConfigureNLog(logpath).GetCurrentClassLogger();
		if (!checkfolderExist(sysInfo.pluginPath, logger))
		{
			return;
		}
		string[] plugIns = Directory.GetDirectories(sysInfo.pluginPath);
		string[] array = plugIns;
		foreach (string plgPath in array)
		{
			string[] assemblyFiles = Directory.GetFiles(plgPath, "*.dll", SearchOption.AllDirectories);
			string[] array2 = assemblyFiles;
			foreach (string assemblyFile in array2)
			{
				try
				{
					Assembly assembly = Assembly.LoadFrom(assemblyFile);
					if (!CheckIfPlugInCanAddedToService(assembly))
					{
						continue;
					}
					Type[] exportedTypes = assembly.GetExportedTypes();
					foreach (Type item in exportedTypes)
					{
						Attribute tblAttr = item.GetCustomAttribute(typeof(AddClassesToContainer));
						if (tblAttr != null)
						{
							AddClassesToContainer(services, item, tblAttr);
						}
					}
				}
				catch (Exception ex)
				{
					logger.Error(ex.ToString());
				}
			}
		}
	}

	public bool AddPlugInToPlugInDecriptors(string assemblyFile, List<PlugInDescriptors> plgInDescr, string plgPath, out string error)
	{
		bool result = false;
		error = "";
		try
		{
			Assembly assembly = Assembly.LoadFrom(assemblyFile);
			PlugInDescriptors plgModel = null;
			Type[] exportedTypes = assembly.GetExportedTypes();
			foreach (Type item in exportedTypes)
			{
				if (!typeof(IMainDescriptor).IsAssignableFrom(item))
				{
					continue;
				}
				object tmpActiv = Activator.CreateInstance(item);
				object tmpObj = item.GetProperty("plugIn_Id").GetValue(tmpActiv);
				if (tmpObj != null)
				{
					plgModel = plgInDescr.Find((PlugInDescriptors f) => f.mainDescriptor != null && f.mainDescriptor.plugIn_Id == Guid.Parse(tmpObj.ToString()));
					if (plgModel == null)
					{
						plgModel = new PlugInDescriptors();
						plgModel.mainDescriptor = new MainDescriptorWithAssemplyModel();
						plgInDescr.Add(plgModel);
					}
					else if (plgModel.mainDescriptor == null)
					{
						plgModel.mainDescriptor = new MainDescriptorWithAssemplyModel();
					}
				}
				plgModel.mainDescriptor.plugIn_Id = Guid.Parse(tmpObj.ToString());
				tmpObj = item.GetProperty("plugIn_Name").GetValue(tmpActiv);
				plgModel.mainDescriptor.plugIn_Name = tmpObj.ToString();
				tmpObj = item.GetProperty("plugIn_Description").GetValue(tmpActiv);
				if (tmpObj != null)
				{
					plgModel.mainDescriptor.plugIn_Description = tmpObj.ToString();
				}
				plgModel.mainDescriptor.path = plgPath;
				plgModel.mainDescriptor.fileName = item.Name;
				plgModel.mainDescriptor.fullNameSpace = item.FullName;
				FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
				plgModel.mainDescriptor.plugIn_Version = fvi.FileVersion;
				plgModel.mainDescriptor.assembly = assembly;
				string asemblFileName = assemblyFile.Split("\\")[^1];
				asemblFileName = asemblFileName.ToLower().Replace(".dll", "");
				FindAllOtherDescriptors(assembly, plgModel, plgPath, asemblFileName, out error);
				result = true;
				break;
			}
		}
		catch (Exception ex)
		{
			error = ex.ToString();
		}
		return result;
	}

	private void FindAllOtherDescriptors(Assembly assembly, PlugInDescriptors plgInDescr, string sPath, string assemblyFileName, out string error)
	{
		error = "";
		try
		{
			Type[] exportedTypes = assembly.GetExportedTypes();
			foreach (Type item in exportedTypes)
			{
				if (item.GetCustomAttribute(typeof(AddClassesToContainer)) != null)
				{
					Attribute tblAttr = item.GetCustomAttribute(typeof(AddClassesToContainer));
					if (plgInDescr.dIDescriptor == null)
					{
						plgInDescr.dIDescriptor = new List<DIDescriptorWithTypeModel>();
					}
					DIDescriptorWithTypeModel diDescr = new DIDescriptorWithTypeModel();
					diDescr.di_ClassDescription = (tblAttr as AddClassesToContainer).GetDescription();
					diDescr.classType = item;
					diDescr.fileName = item.Name;
					diDescr.path = sPath;
					diDescr.fullNameSpace = item.FullName;
					diDescr.scope = (tblAttr as AddClassesToContainer).GetServiceType();
					plgInDescr.dIDescriptor.Add(diDescr);
					if (item.BaseType.Name == "AbstractConfigurationHelper")
					{
						ConfigDescriptorModel confClass = new ConfigDescriptorModel();
						confClass.confFile = item;
						confClass.fullClassName = item.FullName;
						plgInDescr.configClass = confClass;
					}
				}
				else if (item.GetCustomAttribute(typeof(SchedulerAnnotation)) != null)
				{
					if (item.BaseType.Name == "ServiceExecutions")
					{
						Attribute tblAttr = item.GetCustomAttribute(typeof(SchedulerAnnotation));
						if (plgInDescr.serviceDescriptor == null)
						{
							plgInDescr.serviceDescriptor = new List<ServiceDescriptorWithTypeModel>();
						}
						ServiceDescriptorWithTypeModel servDescr = new ServiceDescriptorWithTypeModel();
						servDescr.classType = item;
						servDescr.fileName = item.Name;
						servDescr.fullNameSpace = item.FullName;
						servDescr.path = sPath;
						servDescr.seriveName = (tblAttr as SchedulerAnnotation).GetServiceName();
						servDescr.serviceDescription = (tblAttr as SchedulerAnnotation).GetDescription();
						servDescr.serviceId = Guid.Parse((tblAttr as SchedulerAnnotation).GetId());
						servDescr.serviceVersion = (tblAttr as SchedulerAnnotation).GetVersion();
						servDescr.assemblyFileName = assemblyFileName;
						plgInDescr.serviceDescriptor.Add(servDescr);
					}
				}
				else if (item.GetCustomAttribute(typeof(RoutingAnnotation)) != null)
				{
					Attribute tblAttr = item.GetCustomAttribute(typeof(RoutingAnnotation));
					if (tblAttr != null)
					{
						if (plgInDescr.routing == null)
						{
							plgInDescr.routing = new Dictionary<string, string>();
						}
						plgInDescr.routing[(tblAttr as RoutingAnnotation).GetRoutingController()] = (tblAttr as RoutingAnnotation).GetRoutingAlias();
					}
				}
				else if (typeof(IInitialerDescriptor).IsAssignableFrom(item))
				{
					object tmpActiv = Activator.CreateInstance(item);
					if (plgInDescr.initialerDescriptor == null)
					{
						plgInDescr.initialerDescriptor = new InitialerDescriptorModel();
					}
					object tmpObj = item.GetProperty("dbVersion").GetValue(tmpActiv);
					if (tmpObj != null)
					{
						plgInDescr.initialerDescriptor.dbVersion = tmpObj.ToString();
					}
					plgInDescr.initialerDescriptor.fullNameSpace = item.FullName;
					plgInDescr.initialerDescriptor.assemblyFileName = assemblyFileName;
					plgInDescr.initialerDescriptor.plugIn_Id = plgInDescr.mainDescriptor.plugIn_Id;
				}
				else if (item.BaseType.Name == "AbstractConfigurationHelper")
				{
					ConfigDescriptorModel confClass2 = new ConfigDescriptorModel();
					confClass2.confFile = item;
					confClass2.fullClassName = item.FullName;
					plgInDescr.configClass = confClass2;
				}
				else if (item.IsEnum)
				{
					if (plgInDescr.enumTypes == null)
					{
						plgInDescr.enumTypes = new List<Type>();
					}
					plgInDescr.enumTypes.Add(item);
				}
			}
		}
		catch (Exception ex)
		{
			error = ex.ToString();
		}
	}

	public void CheckIfNullValuesExistsOnConfiguration(Dictionary<string, dynamic> config, Dictionary<string, List<DescriptorsModel>> descriptor)
	{
		foreach (KeyValuePair<string, List<DescriptorsModel>> item2 in descriptor)
		{
			List<DescriptorsModel> dsp = item2.Value;
			foreach (DescriptorsModel dspVal in dsp)
			{
				bool flag = !config.ContainsKey(dspVal.Key);
				if (flag || ((flag | string.IsNullOrEmpty(config[dspVal.Key]?.ToString())) ? true : false))
				{
					switch (dspVal.Type.ToLower())
					{
					case "string":
						config[dspVal.Key] = dspVal.DefaultValue;
						break;
					case "bool":
						config[dspVal.Key] = bool.Parse(dspVal.DefaultValue);
						break;
					case "int":
						config[dspVal.Key] = int.Parse(dspVal.DefaultValue);
						break;
					case "decimal":
						config[dspVal.Key] = decimal.Parse(dspVal.DefaultValue);
						break;
					case "datetime":
						config[dspVal.Key] = DateTime.Parse(dspVal.DefaultValue);
						break;
					case "int64":
						config[dspVal.Key] = long.Parse(dspVal.DefaultValue);
						break;
					case "int32":
						config[dspVal.Key] = int.Parse(dspVal.DefaultValue);
						break;
					case "float":
						config[dspVal.Key] = float.Parse(dspVal.DefaultValue);
						break;
					case "double":
						config[dspVal.Key] = double.Parse(dspVal.DefaultValue);
						break;
					default:
						config[dspVal.Key] = dspVal.DefaultValue;
						break;
					}
				}
			}
		}
	}

	private void AddClassesToContainer(IServiceCollection services, Type classToAdd, Attribute tblAttr)
	{
		switch ((tblAttr as AddClassesToContainer).GetServiceType())
		{
		case ServicesAddTypeEnum.addScoped:
			services.AddScoped(classToAdd);
			break;
		case ServicesAddTypeEnum.addSingleton:
			services.AddSingleton(classToAdd);
			break;
		case ServicesAddTypeEnum.addTransient:
			services.AddTransient(classToAdd);
			break;
		}
	}

	private string ReturnDynamicObjectFromString(string value)
	{
		Dictionary<string, object> objDiction = new Dictionary<string, object>();
		string[] objProperties = value.Split(',');
		string[] array = objProperties;
		foreach (string item in array)
		{
			string[] objPropValues = item.Split('=');
			objDiction.Add(objPropValues[0], objPropValues[1]);
		}
		return JsonSerializer.Serialize(DictionaryToObject(objDiction));
	}

	private dynamic DictionaryToObject(IDictionary<string, object> source)
	{
		ExpandoObject expandoObj = new ExpandoObject();
		ICollection<KeyValuePair<string, object>> expandoObjCollection = expandoObj;
		foreach (KeyValuePair<string, object> keyValuePair in source)
		{
			expandoObjCollection.Add(keyValuePair);
		}
		return expandoObj;
	}

	private bool checkfolderExist(string path, Logger logger, bool create = true, bool delete = false)
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
				logger.Error(ex.ToString());
				return false;
			}
		}
		return res;
	}
}
