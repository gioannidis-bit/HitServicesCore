using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using AutoMapper;
using Hangfire;
using Hangfire.MemoryStorage;
using HitHelpersNetCore.Classes;
using HitHelpersNetCore.Helpers;
using HitHelpersNetCore.Models;
using HitServicesCore.Filters;
using HitServicesCore.Helpers;
using HitServicesCore.Helpers.Common;
using HitServicesCore.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using NLog;
using NLog.Web;

namespace HitServicesCore;

public class Startup
{
	public IConfiguration Configuration { get; }

	public Startup(IConfiguration configuration)
	{
		Configuration = configuration;
	}

	public void ConfigureServices(IServiceCollection services)
	{
		string CurrentPath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
		List<string> ps = new List<string> { CurrentPath, "Config", "NLog.config" };
		string logpath = Path.GetFullPath(Path.Combine(ps.ToArray()));
		Logger logger = NLogBuilder.ConfigureNLog(logpath).GetCurrentClassLogger();
		try
		{
			services.AddRazorPages().AddNewtonsoftJson(delegate(MvcNewtonsoftJsonOptions options)
			{
				options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
			}).AddJsonOptions(delegate(JsonOptions options)
			{
				options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
				options.JsonSerializerOptions.PropertyNamingPolicy = null;
				options.JsonSerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
				options.JsonSerializerOptions.IgnoreNullValues = true;
			});
			services.AddMvc(delegate(MvcOptions options)
			{
				options.EnableEndpointRouting = false;
				options.OutputFormatters.Add(new XmlSerializerOutputFormatter());
			}).SetCompatibilityVersion(CompatibilityVersion.Latest).AddNewtonsoftJson(delegate(MvcNewtonsoftJsonOptions options)
			{
				options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
			})
				.AddJsonOptions(delegate(JsonOptions o)
				{
					o.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
					o.JsonSerializerOptions.PropertyNamingPolicy = null;
					o.JsonSerializerOptions.DictionaryKeyPolicy = null;
					o.JsonSerializerOptions.IgnoreNullValues = true;
				})
				.AddXmlSerializerFormatters()
				.AddXmlDataContractSerializerFormatters();
			services.AddControllers().AddNewtonsoftJson(delegate(MvcNewtonsoftJsonOptions options)
			{
				options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
			}).AddJsonOptions(delegate(JsonOptions o)
			{
				o.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
				o.JsonSerializerOptions.PropertyNamingPolicy = null;
				o.JsonSerializerOptions.DictionaryKeyPolicy = null;
				o.JsonSerializerOptions.IgnoreNullValues = true;
			});
			services.AddControllersWithViews().AddRazorRuntimeCompilation().AddNewtonsoftJson(delegate(MvcNewtonsoftJsonOptions options)
			{
				options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
			});
			services.AddSingleton<StringCipher>();
			services.AddSingleton<ValidationHelper>();
			HitKeyHelper.InitialzeKey(EncryptHitKeys());
			services.AddSingleton<HitKeyHelper>();
			services.AddSingleton<DIHelper>();
			SmtpExtModel mod = new SmtpExtModel();
			SystemInfo sysInf = InitializeSystemInfo();
			services.AddSingleton(sysInf);
			services.AddSingleton<List<PlugInDescriptors>>();
			services.AddSingleton<List<MainConfigurationModel>>();
			services.AddSingleton<List<FtpExtModel>>();
			services.AddSingleton<List<SmtpExtModel>>();
			services.AddSingleton<List<SchedulerServiceModel>>();
			services.AddSingleton<LoginsUsers>();
			services.AddSingleton<PluginsMvcLoader>();
			services.AddScoped<EmailHelper>();
			services.AddSingleton<JsonOptionsHelper>();
			services.AddSingleton<PlugInAnnotationedClassesHelper>();
			PlugInAnnotationedClassesHelper plg = new PlugInAnnotationedClassesHelper(sysInf);
			plg.AddClassesToDI_OnStartUp(services);
			services.AddScoped<ManageConfiguration>();
			services.AddScoped<LoginFilter>();
			services.AddSingleton<ProtelDBsHelper>();
			services.AddSingleton<WebPosDBsHelper>();
			services.AddSingleton<DBsHelper>();
			services.AddSingleton<SmtpHelper>();
			services.AddSingleton<FtpHelper>();
			services.AddSingleton<HitPosDBsHelper>();
			services.AddSingleton<ProtelHelper>();
			services.AddSingleton<ErmisDBsHelper>();
			services.AddSingleton<CustomDBsHelper>();
			services.AddSingleton<GenericDBsHelper>();
			services.AddAutoMapper(GetAllAutomapperPlugins(sysInf));
			services.AddHangfire(delegate(IGlobalConfiguration config)
			{
				config.SetDataCompatibilityLevel(CompatibilityLevel.Version_170).UseSimpleAssemblyNameTypeSerializer().UseDefaultTypeSerializer()
					.UseMemoryStorage();
			});
			services.AddHangfireServer();
			services.AddSingleton<HangFire_ManageServices>();
			services.AddSingleton<InitializerHelper>();
			services.AddSession(delegate(SessionOptions options)
			{
				options.IdleTimeout = TimeSpan.FromMinutes(60.0);
			});
		}
		catch (Exception ex)
		{
			Console.WriteLine(" >>>---->>  ERROR (for more info see log files): " + ex.Message);
			logger.Error(Convert.ToString(ex));
		}
	}

	private string EncryptHitKeys()
	{
		string result = "";
		string CurrentPath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
		Directory.SetCurrentDirectory(CurrentPath);
		List<string> ps = new List<string> { CurrentPath, "Config", "HitKeys.json" };
		string hitKeyFile = Path.GetFullPath(Path.Combine(ps.ToArray()));
		if (File.Exists(hitKeyFile))
		{
			EncryptionHelper encr = new EncryptionHelper();
			string sVal = encr.Decrypt(File.ReadAllText(hitKeyFile));
			List<string> ftpCred = System.Text.Json.JsonSerializer.Deserialize<List<string>>(sVal);
			sVal = encr.Encrypt(System.Text.Json.JsonSerializer.Serialize(ftpCred));
			File.WriteAllText(hitKeyFile, sVal);
			result = encr.Decrypt(ftpCred[0]);
		}
		return result;
	}

	private Assembly[] GetAllAutomapperPlugins(SystemInfo sysInf)
	{
		List<Assembly> result = new List<Assembly>();
		result.Add(typeof(Startup).Assembly);
		if (!Directory.Exists(sysInf.pluginPath))
		{
			return result.ToArray();
		}
		string[] plugIns = Directory.GetDirectories(sysInf.pluginPath);
		string[] array = plugIns;
		foreach (string plgPath in array)
		{
			string addFile = "";
			string[] assemblyFiles = Directory.GetFiles(plgPath, "*.dll", SearchOption.AllDirectories);
			string[] array2 = assemblyFiles;
			foreach (string assemblyFile in array2)
			{
				Assembly assembly = Assembly.LoadFrom(assemblyFile);
				Type[] exportedTypes = assembly.GetExportedTypes();
				foreach (Type item in exportedTypes)
				{
					if (item.BaseType != null && item.BaseType.Name == "Profile")
					{
						result.Add(item.Assembly);
					}
				}
			}
		}
		return result.ToArray();
	}

	private SystemInfo InitializeSystemInfo()
	{
		SystemInfo result = new SystemInfo();
		Assembly assembly = Assembly.GetExecutingAssembly();
		FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
		result.version = fvi.FileVersion;
		result.rootPath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
		result.pluginPath = Path.Combine(result.rootPath, "Plugins");
		result.pluginFilePath = Path.Combine(result.rootPath, "PluginFiles");
		return result;
	}

	public void Configure(IApplicationBuilder app, IWebHostEnvironment env, SystemInfo sysInfo, PluginsMvcLoader pluginsMvcLoader, List<MainConfigurationModel> diConfigs, List<PlugInDescriptors> plgDescr, ApplicationPartManager apm, ILogger<Startup> logger, List<SchedulerServiceModel> scheduledServices, IBackgroundJobClient backgroundJobClient, IRecurringJobManager recurringJobManager, IServiceProvider serviceProvider, DIHelper diHelper, List<FtpExtModel> _ftps, List<SmtpExtModel> _smtps)
	{
		DIHelper.AppBuilder = app;
		pluginsMvcLoader.LoadMvcPlugins(apm);
		logger.LogInformation("Loaded Plugins:");
		foreach (PlugInDescriptors plg in plgDescr)
		{
			logger.LogInformation(plg.mainDescriptor.plugIn_Name + " version " + plg.mainDescriptor.plugIn_Version);
		}
		logger.LogInformation("");
		if (env.IsDevelopment())
		{
			app.UseDeveloperExceptionPage();
		}
		else
		{
			app.UseExceptionHandler("/Error");
		}
		app.UseStaticFiles();
		app.UseSession();
		app.UseRouting();
		app.UseAuthorization();
		app.UseEndpoints(delegate(IEndpointRouteBuilder endpoints)
		{
			endpoints.MapControllers();
			endpoints.MapDefaultControllerRoute();
			endpoints.MapRazorPages();
			endpoints.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");
		});
		app.UseMvc(delegate(IRouteBuilder routes)
		{
			routes.MapRoute("default", "{controller=Login}/{action=Index}");
		});
		DashboardOptions options = new DashboardOptions
		{
			AppPath = "/Plugins"
		};
		app.UseHangfireDashboard("/Scheduler", options);
		HangFire_ManageServices hangHlp = new HangFire_ManageServices(scheduledServices, recurringJobManager, null);
		hangHlp.LoadServices();
		new Thread((ThreadStart)delegate
		{
			Thread.CurrentThread.IsBackground = true;
			CheckActiveServicesEnabledOnHangHire checkActiveServicesEnabledOnHangHire = new CheckActiveServicesEnabledOnHangHire(hangHlp, plgDescr, diConfigs, _ftps, _smtps);
			checkActiveServicesEnabledOnHangHire.CheckServicesOnhangFire();
		}).Start();
	}

	private void ConfigureApplicationParts(ApplicationPartManager apm)
	{
	}
}
