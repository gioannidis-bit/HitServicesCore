using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using HitCustomAnnotations.Interfaces;
using HitHelpersNetCore.Helpers;
using HitHelpersNetCore.Interfaces;
using HitHelpersNetCore.Models;
using HitServicesCore.Models.Helpers;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace SendInvoiceViaEmail.Classes;

public class SendInvoiceViaEmailDBInit : IInitialerDescriptor
{
	public class NewVersions
	{
		public int arg1 { get; set; }

		public int arg2 { get; set; }

		public int arg3 { get; set; }

		public int arg4 { get; set; }

		public List<string> sql { get; set; }
	}

	private ILogger<SendInvoiceViaEmailDBInit> logger;

	private SendInvoiceViaEmailConfig configClass;

	private IMainConfigurationModel localConfig;

	private string protelConnection;

	public string dbVersion => "1.0.1.0";

	public void Start(string lastUpdatedVarsion, IApplicationBuilder _app)
	{
		IServiceProvider applicationServices = _app.ApplicationServices;
		logger = applicationServices.GetService<ILogger<SendInvoiceViaEmailDBInit>>();
		string[] array = lastUpdatedVarsion.Split('.');
		NewVersions newVersions = new NewVersions();
		newVersions.arg1 = int.Parse(array[0]);
		newVersions.arg2 = int.Parse(array[1]);
		newVersions.arg3 = int.Parse(array[2]);
		newVersions.arg4 = int.Parse(array[3]);
		List<NewVersions> list = new List<NewVersions>();
		list.Add(Version_1_0_1_0());
		list = list.OrderBy((NewVersions o) => o.arg1).ThenBy((NewVersions t) => t.arg2).ThenBy((NewVersions tt) => tt.arg3)
			.ThenBy((NewVersions ttt) => ttt.arg4)
			.ToList();
		if (!NeedToUpdate(list, newVersions))
		{
			return;
		}
		try
		{
			configClass = new SendInvoiceViaEmailConfig();
			localConfig = (IMainConfigurationModel)(object)((AbstractConfigurationHelper)configClass).ReadConfiguration();
			IMainConfigurationModel obj = localConfig;
			object obj2;
			if (obj == null)
			{
				obj2 = null;
			}
			else
			{
				MainConfiguration config = obj.config;
				obj2 = ((config != null) ? config.config : null);
			}
			if (obj2 == null || !localConfig.config.config.ContainsKey("protelDBKey"))
			{
				logger.LogError("There are no mpehotel initialized on configuration file");
				throw new Exception("There are no mpehotel initialized on configuration file");
			}
			List<HotelConfigModel> list2 = (dynamic)localConfig.config.config["protelDBKey"];
			if (list2 == null || list2.Count < 1)
			{
				logger.LogError("There are no protel db initialized on configuration file");
				throw new Exception("There are no protel db initialized on configuration file");
			}
			List<string> list3 = list2.Select((HotelConfigModel s) => s.Value).Distinct().ToList();
			string text = "";
			foreach (string item in list3)
			{
				protelConnection = item;
				foreach (NewVersions item2 in list)
				{
					if (item2.arg1 <= newVersions.arg1 && (item2.arg1 < newVersions.arg1 || item2.arg2 <= newVersions.arg2) && (item2.arg1 < newVersions.arg1 || item2.arg2 < newVersions.arg2 || item2.arg3 <= newVersions.arg3) && (item2.arg1 < newVersions.arg1 || item2.arg2 < newVersions.arg2 || item2.arg3 < newVersions.arg3 || item2.arg4 <= newVersions.arg4))
					{
						continue;
					}
					foreach (string item3 in item2.sql)
					{
						text = ExecuteSql(item3);
						if (!string.IsNullOrWhiteSpace(text))
						{
							logger.LogError(text);
							break;
						}
					}
				}
			}
			logger.LogInformation("Send invoice via email : Database Initializer completed.");
		}
		catch (Exception ex)
		{
			logger.LogError("Send invoice via email : Failed to initialize database: " + ex.ToString());
			throw;
		}
	}

	private string ExecuteSql(string sql)
	{
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Expected O, but got Unknown
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Expected O, but got Unknown
		string result = "";
		try
		{
			SqlConnection val = new SqlConnection(protelConnection);
			try
			{
				((DbConnection)(object)val).Open();
				SqlCommand val2 = new SqlCommand(sql, val);
				((DbCommand)(object)val2).ExecuteNonQuery();
				((DbConnection)(object)val).Close();
			}
			finally
			{
				((IDisposable)val)?.Dispose();
			}
		}
		catch (Exception ex)
		{
			result = ex.ToString();
		}
		return result;
	}

	private bool NeedToUpdate(List<NewVersions> versions, NewVersions lastVersion)
	{
		bool flag = false;
		foreach (NewVersions version in versions)
		{
			flag = version.arg1 > lastVersion.arg1 || (version.arg1 >= lastVersion.arg1 && version.arg2 > lastVersion.arg2) || (version.arg1 >= lastVersion.arg1 && version.arg2 >= lastVersion.arg2 && version.arg3 > lastVersion.arg3) || (version.arg1 >= lastVersion.arg1 && version.arg2 >= lastVersion.arg2 && version.arg3 >= lastVersion.arg3 && version.arg4 > lastVersion.arg4);
			if (flag)
			{
				break;
			}
		}
		return flag;
	}

	private NewVersions Version_1_0_1_0()
	{
		NewVersions newVersions = new NewVersions();
		newVersions.arg1 = 1;
		newVersions.arg2 = 0;
		newVersions.arg3 = 1;
		newVersions.arg4 = 0;
		newVersions.sql = new List<string>();
		newVersions.sql.Add(CreateEmailResultTable_Ver_1_0_1_0());
		newVersions.sql.Add(CreateSendEmailResultTableParameters_Ver_1_0_1_0());
		return newVersions;
	}

	private string CreateEmailResultTable_Ver_1_0_1_0()
	{
		return "IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'SendInvoiceViaEmail')\r\n\t                            CREATE TABLE [SendInvoiceViaEmail](\r\n\t\t                            [Id] [bigint] IDENTITY(1,1) NOT NULL,\r\n\t\t                            [Mpehotel] [int] NOT NULL,\r\n\t\t                            [ReservationId] [int] NOT NULL,\r\n\t\t                            [ProfileId] [int] NOT NULL,\r\n\t\t                            [ProfileName] [nvarchar](200) NULL,\r\n\t\t                            [InvoiceTypeId] [int] NOT NULL,\r\n\t\t                            [InvoiceNo] [int] NOT NULL,\r\n\t\t                            [IssueDate] [datetime] NOT NULL,\r\n\t\t                            [EmailTo] [nvarchar](max) NOT NULL,\r\n\t\t                            [StatusCode] [nvarchar](50) NOT NULL,\r\n\t\t                            [ErrorMessage] [text] NULL,\r\n\t\t                            [CreationDate] [datetime] NOT NULL,\r\n\t\t                            CONSTRAINT [PK_SendInvoiceViaEmail] PRIMARY KEY CLUSTERED \r\n\t\t                            (\r\n\t\t\t                            [Id] ASC\r\n\t\t                            )\r\n\t                            ) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]";
	}

	private string CreateSendEmailResultTableParameters_Ver_1_0_1_0()
	{
		return "IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'SendInvoiceViaEmailParameters')\r\n\t                            CREATE TABLE [SendInvoiceViaEmailParameters](\r\n\t\t                            [Id] [bigint] IDENTITY(1,1) NOT NULL,\r\n\t\t                            [Mpehotel] [int] NOT NULL,\r\n\t\t                            [HotelName] [nvarchar](200) NULL,\r\n\t\t                            [HotelAddress] [nvarchar](500) NULL,\r\n\t\t                            [HotelPnone1] [nvarchar](50) NULL,\r\n\t\t                            [HotelPnone2] [nvarchar](50) NULL,\r\n\t\t                            [HotelPnone3] [nvarchar](50) NULL,\r\n\t\t                            [HotelFax] [nvarchar](50) NULL,\r\n\t\t                            [HotelWeb] [nvarchar](500) NULL,\r\n\t\t                            [HotelEmail] [nvarchar](500) NULL,\r\n\t\t                            CONSTRAINT [PK_SendInvoiceViaEmailParameters] PRIMARY KEY CLUSTERED \r\n\t\t                            (\r\n\t\t\t                            [Id] ASC\r\n\t\t                            )\r\n\t                            ) ON [PRIMARY] ";
	}
}
