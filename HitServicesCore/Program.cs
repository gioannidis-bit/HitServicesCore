using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.WindowsServices;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NLog;
using NLog.Web;

namespace HitServicesCore;

public class Program
{
	public static string CurrentPath { get; set; }

	public static string AppName { get; set; }

	public static IConfiguration Configuration { get; set; }

	public static void Main(string[] args)
	{
		CurrentPath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
		Directory.SetCurrentDirectory(CurrentPath);
		AppName = Assembly.GetEntryAssembly().GetName().Name;
		bool isService = !Debugger.IsAttached && !args.Contains("--console");
		string[] webHostArgs = args.Where((string arg) => arg != "--console").ToArray();
		List<string> ps = new List<string> { CurrentPath, "Config", "NLog.config" };
		string logpath = Path.GetFullPath(Path.Combine(ps.ToArray()));
		Logger logger = NLogBuilder.ConfigureNLog(logpath).GetCurrentClassLogger();
		try
		{
			ConfigurationBuilder();
			IWebHost webBuilder = CreateWebHostBuilder(webHostArgs).Build();
			IWebHostEnvironment env = (IWebHostEnvironment)webBuilder.Services.GetService(typeof(IWebHostEnvironment));
			StartLogging(logger, env);
			if (isService)
			{
				webBuilder.RunAsService();
			}
			else
			{
				webBuilder.Run();
			}
		}
		catch (Exception ex)
		{
			Console.WriteLine(" >>>---->>  ERROR (for more info see log files): " + ex.Message);
			logger.Error(Convert.ToString(ex));
		}
		finally
		{
			logger.Warn(" ======== " + AppName + " Stopping ======== ");
			logger.Warn("");
			LogManager.Shutdown();
		}
	}

	private static void StartLogging(Logger logger, IWebHostEnvironment env)
	{
		Console.WriteLine("Starting Logger...");
		logger.Info("");
		logger.Info("");
		logger.Info("*****************************************");
		logger.Info("*                                       *");
		logger.Info("*  " + AppName + "  Started                   ");
		logger.Info("*                                       *");
		logger.Info("*****************************************");
		logger.Info("");
		Assembly assembly = Assembly.GetExecutingAssembly();
		FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
		logger.Info("Version: " + fvi.FileVersion);
		logger.Info("Urls: " + Configuration["urls"]);
		Console.WriteLine("Urls: " + Configuration["urls"]);
		logger.Info("Environment: " + env.EnvironmentName);
		logger.Info("Current Path: " + CurrentPath);
		logger.Info("");
	}

	public static IWebHostBuilder CreateWebHostBuilder(string[] args)
	{
		return WebHost.CreateDefaultBuilder(args).UseStartup<Startup>().ConfigureLogging(delegate(ILoggingBuilder logging)
		{
			logging.ClearProviders();
			logging.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Trace);
		})
			.ConfigureKestrel(delegate(KestrelServerOptions serverOptions)
			{
				Console.WriteLine("Configuring Kestrel...");
				serverOptions.ConfigureHttpsDefaults(delegate(HttpsConnectionAdapterOptions listenOptions)
				{
					string text = Configuration["CertificateFile"];
					string password = Configuration["CertificatePassword"];
					if (!string.IsNullOrWhiteSpace(text))
					{
						Console.WriteLine("Loading Certificate...");
						string fullPath = Path.GetFullPath(Path.Combine(CurrentPath, "Config", text));
						if (!new FileInfo(fullPath).Exists)
						{
							throw new Exception("Certificate " + text + " not found");
						}
						X509Certificate2 serverCertificate = new X509Certificate2(fullPath, password);
						listenOptions.ServerCertificate = serverCertificate;
					}
				});
			})
			.UseConfiguration(Configuration)
			.UseNLog();
	}

	private static void ConfigurationBuilder()
	{
		Console.WriteLine("Building Configuration...");
		List<string> ps1 = new List<string> { CurrentPath, "Config", "appsettings.json" };
		string appsettingspath = Path.GetFullPath(Path.Combine(ps1.ToArray()));
		IConfigurationBuilder builder = new ConfigurationBuilder().SetBasePath(CurrentPath).AddJsonFile(appsettingspath, optional: false, reloadOnChange: true);
		Configuration = builder.Build();
	}
}
