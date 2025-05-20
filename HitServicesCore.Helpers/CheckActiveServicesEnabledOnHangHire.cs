using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using HitHelpersNetCore.Models;
using NLog;
using NLog.Web;

namespace HitServicesCore.Helpers;

public class CheckActiveServicesEnabledOnHangHire
{
	private readonly List<PlugInDescriptors> plugIns;

	private readonly List<MainConfigurationModel> config;

	private readonly HangFire_ManageServices hangFireManager;

	private List<FtpExtModel> ftps;

	private List<SmtpExtModel> smtps;

	public CheckActiveServicesEnabledOnHangHire(HangFire_ManageServices _hangFireManager, List<PlugInDescriptors> _plugIns, List<MainConfigurationModel> _config, List<FtpExtModel> _ftps, List<SmtpExtModel> _smtps)
	{
		plugIns = _plugIns;
		config = _config;
		hangFireManager = _hangFireManager;
		ftps = _ftps;
		smtps = _smtps;
	}

	public void CheckServicesOnhangFire()
	{
		while (true)
		{
			MainConfigHelper mainconfigHlp = new MainConfigHelper(config, plugIns, ftps, smtps);
			MainConfigurationModel mainConfigModel = mainconfigHlp.ReadHitServiceCoreConfig();
			int sleep = 1;
			if (mainConfigModel != null && mainConfigModel.config != null && mainConfigModel.config.config != null && mainConfigModel.config.config.ContainsKey("CheckServiceOnScheduler"))
			{
				sleep = mainConfigModel.config.config["CheckServiceOnScheduler"];
			}
			if (sleep < 1)
			{
				sleep = 1;
			}
			sleep *= 60000;
			string CurrentPath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
			List<string> ps = new List<string> { CurrentPath, "Config", "NLog.config" };
			string logpath = Path.GetFullPath(Path.Combine(ps.ToArray()));
			Logger logger = NLogBuilder.ConfigureNLog(logpath).GetCurrentClassLogger();
			try
			{
				hangFireManager.AddServicesToHangFire();
			}
			catch (Exception ex)
			{
				logger.Error(ex.ToString());
			}
			Thread.Sleep(sleep);
		}
	}
}
