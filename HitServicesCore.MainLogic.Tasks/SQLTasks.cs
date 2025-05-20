using System.Collections.Generic;
using HitServicesCore.DTAccess;
using HitServicesCore.Models.IS_Services;

namespace HitServicesCore.MainLogic.Tasks;

public class SQLTasks
{
	private readonly RunSQLScriptsDT runScriptDT;

	private readonly ISRunSqlScriptsModel settings;

	public SQLTasks(ISRunSqlScriptsModel _settings)
	{
		settings = _settings;
		runScriptDT = new RunSQLScriptsDT();
	}

	public void RunScript(string sqlScript, string conString = null)
	{
		if (conString == null)
		{
			conString = settings.Custom1DB;
		}
		int timeout = int.Parse(settings.DBTimeout);
		runScriptDT.RunScript(conString, sqlScript, timeout);
	}

	public IEnumerable<dynamic> RunSelect(string sqlScript, string conString)
	{
		string dbTm = settings.DBTimeout;
		if (string.IsNullOrWhiteSpace(dbTm))
		{
			dbTm = "60";
		}
		int timeout = int.Parse(dbTm);
		return runScriptDT.RunSelect(conString, sqlScript, timeout);
	}

	public IEnumerable<dynamic> RunSelectMulty(string sqlScript, string conString)
	{
		int timeout = int.Parse(settings.DBTimeout);
		return runScriptDT.RunSelectMulty(conString, sqlScript, timeout);
	}

	public List<IEnumerable<dynamic>> RunMultySelect(string sqlScript, string conString = null, int timeout = 0)
	{
		if (conString == null)
		{
			conString = settings.Custom1DB;
		}
		if (timeout == 0 && settings != null)
		{
			timeout = int.Parse(settings.DBTimeout);
		}
		else if (timeout == 0)
		{
			timeout = 30;
		}
		return runScriptDT.RunMultipleSelect(conString, sqlScript, timeout);
	}

	public DbTableModel GetTableInfo(string constr, string tableName, int timeout = 60)
	{
		return runScriptDT.GetTableInfo(constr, tableName, timeout);
	}

	public void SaveToTable(List<IDictionary<string, dynamic>> data, string constr, DbTableModel tableinfo, int operation, bool useTransaction, int timeout = 60)
	{
		runScriptDT.SaveToTable(data, constr, tableinfo, operation, useTransaction, timeout);
	}

	public void SaveCsvToTable(List<string> executeData, List<ImportDataToTableFromCsvModel> command, List<DBColumnModel> keyColumns, string tableName, string constr, int operation, bool useTransaction, int timeout = 60)
	{
		runScriptDT.SaveCsvToTable(executeData, command, keyColumns, tableName, constr, operation, useTransaction, timeout);
	}
}
