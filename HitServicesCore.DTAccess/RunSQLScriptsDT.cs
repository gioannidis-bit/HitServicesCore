using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text.Json;
using System.Transactions;
using Dapper;
using HitHelpersNetCore.Helpers;
using HitServicesCore.Helpers;
using HitServicesCore.Models.IS_Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace HitServicesCore.DTAccess;

public class RunSQLScriptsDT
{
	private readonly ILogger<RunSQLScriptsDT> logger;

	public RunSQLScriptsDT()
	{
		if (DIHelper.AppBuilder != null)
		{
			IServiceProvider services = DIHelper.AppBuilder.ApplicationServices;
			logger = services.GetService<ILogger<RunSQLScriptsDT>>();
		}
	}

	public void RunScript(string constr, string script, int timeout = 60)
	{
		using IDbConnection db = new SqlConnection(constr);
		db.Execute(script, null, null, timeout);
	}

	public IEnumerable<dynamic> RunSelect(string constr, string script, int timeout = 60)
	{
		using IDbConnection db = new SqlConnection(constr);
		return db.Query(script, null, null, buffered: true, timeout);
	}

	public List<IEnumerable<dynamic>> RunSelectMulty(string constr, string script, int timeout = 60)
	{
		List<IEnumerable<dynamic>> result = new List<IEnumerable<object>>();
		using IDbConnection db = new SqlConnection(constr);
		SqlMapper.GridReader m = db.QueryMultiple(script, null, null, timeout);
		while (!m.IsConsumed)
		{
			result.Add(m.Read());
		}
		return result;
	}

	public List<IEnumerable<dynamic>> RunMultipleSelect(string constr, string scripts, int timeout = 60)
	{
		using IDbConnection db = new SqlConnection(constr);
		SqlMapper.GridReader m = db.QueryMultiple(scripts, null, null, timeout);
		List<IEnumerable<dynamic>> list = new List<IEnumerable<object>>();
		while (!m.IsConsumed)
		{
			list.Add(m.Read());
		}
		return list;
	}

	public void SaveToTable(List<IDictionary<string, dynamic>> data, string constr, DbTableModel tableinfo, int operation, bool useTransaction, int timeout = 60)
	{
		if (useTransaction)
		{
			using (IDbConnection db = new SqlConnection(constr))
			{
				using TransactionScope scope = new TransactionScope();
				SaveToTable(data, tableinfo, operation, db, timeout);
				scope.Complete();
				return;
			}
		}
		using IDbConnection db2 = new SqlConnection(constr);
		SaveToTable(data, tableinfo, operation, db2, timeout);
	}

	public void SaveCsvToTable(List<string> executeData, List<ImportDataToTableFromCsvModel> command, List<DBColumnModel> keyColumns, string tableName, string constr, int operation, bool useTransaction, int timeout = 60)
	{
		int exec = 0;
		int inserts = 0;
		int updates = 0;
		int losts = 0;
		string lastquery = "";
		try
		{
			if (useTransaction)
			{
				using IDbConnection db = new SqlConnection(constr);
				using TransactionScope scope = new TransactionScope();
				if (executeData != null && executeData.Count > 0)
				{
					foreach (string item in executeData)
					{
						lastquery = item;
						db.Execute(lastquery, null, null, timeout);
					}
				}
				else
				{
					SqlConstructorHelper sqlConstruct = new SqlConstructorHelper();
					string whereStatment = "";
					foreach (ImportDataToTableFromCsvModel item2 in command)
					{
						whereStatment = sqlConstruct.WhereStatmentForRecordExists(keyColumns, item2);
						if (whereStatment.EndsWith("AND "))
						{
							whereStatment = whereStatment.Substring(whereStatment.Length - 4);
						}
						whereStatment = "IF EXISTS(SELECT 1 FROM " + tableName + " WHERE " + whereStatment + ") SELECT 1 ELSE SELECT 0";
						if (db.Query<int>(whereStatment).FirstOrDefault() != 0)
						{
							if (operation == 0 || operation == 2)
							{
								lastquery = sqlConstruct.MakeUpdateStatmentForCsvInsertData(tableName, keyColumns, item2);
								db.Execute(lastquery, null, null, timeout);
							}
							else
							{
								losts++;
							}
						}
						else if (operation < 2)
						{
							lastquery = sqlConstruct.MakeInsertStatmentForCsvInsertData(tableName, keyColumns, item2);
							db.Execute(lastquery, null, null, timeout);
						}
						else
						{
							losts++;
						}
					}
				}
				scope.Complete();
			}
			else
			{
				using IDbConnection db2 = new SqlConnection(constr);
				if (executeData != null && executeData.Count > 0)
				{
					foreach (string item3 in executeData)
					{
						lastquery = item3;
						db2.Execute(lastquery, null, null, timeout);
					}
				}
				else
				{
					SqlConstructorHelper sqlConstruct2 = new SqlConstructorHelper();
					string whereStatment2 = "";
					foreach (ImportDataToTableFromCsvModel item4 in command)
					{
						whereStatment2 = sqlConstruct2.WhereStatmentForRecordExists(keyColumns, item4);
						if (whereStatment2.EndsWith("AND "))
						{
							whereStatment2 = whereStatment2.Substring(whereStatment2.Length - 4);
						}
						whereStatment2 = "IF EXISTS(SELECT 1 FROM " + tableName + " WHERE " + whereStatment2 + ") SELECT 1 ELSE SELECT 0";
						if (db2.Query<int>(whereStatment2).FirstOrDefault() != 0)
						{
							if (operation == 0 || operation == 2)
							{
								lastquery = sqlConstruct2.MakeUpdateStatmentForCsvInsertData(tableName, keyColumns, item4);
								db2.Execute(lastquery, null, null, timeout);
							}
							else
							{
								losts++;
							}
						}
						else if (operation < 2)
						{
							lastquery = sqlConstruct2.MakeInsertStatmentForCsvInsertData(tableName, keyColumns, item4);
							db2.Execute(lastquery, null, null, timeout);
						}
						else
						{
							losts++;
						}
					}
				}
			}
		}
		catch (Exception ex)
		{
			string sMess = "";
			sMess = sMess + "Error         : " + ex.ToString() + "\r\n";
			sMess += " \r\n";
			sMess = sMess + "   Last query : " + lastquery + " \r\n";
			sMess = ((executeData == null || executeData.Count <= 0) ? (sMess + "   For Object : " + JsonSerializer.Serialize(command)) : (sMess + "   For Object : " + JsonSerializer.Serialize(executeData)));
			throw new Exception(sMess);
		}
		if (exec != 0)
		{
			logger.LogInformation("Executed " + exec + " records for insert");
			return;
		}
		string sInfoMess = "SaveToTable Summary : " + command.Count() + " total raws to insert/update.  " + updates + " raws updated, " + inserts + " raws inserted.";
		if (losts != 0)
		{
			switch (operation)
			{
			case 1:
				sInfoMess = sInfoMess + "\r\n Found " + losts + " records already on db and not Inserted.";
				break;
			case 2:
				sInfoMess = sInfoMess + "\r\n Not found " + losts + " records on db and not Updated.";
				break;
			}
		}
		logger.LogInformation(sInfoMess);
	}

	private void SaveToTable(List<IDictionary<string, dynamic>> data, DbTableModel tableinfo, int operation, IDbConnection db, int timeout = 60)
	{
		SqlConstructorHelper sqlConstruct = new SqlConstructorHelper();
		string insertSql = sqlConstruct.InsertStatment(tableinfo);
		string updateSql = sqlConstruct.UpdateStatment(tableinfo);
		string WhereStatment = sqlConstruct.WhereStatmentForRecordExists(tableinfo, data);
		if (WhereStatment.EndsWith("AND "))
		{
			WhereStatment = WhereStatment.Substring(WhereStatment.Length - 4);
		}
		WhereStatment = "IF EXISTS(SELECT 1 FROM " + tableinfo.TableName + " WHERE " + WhereStatment + ") SELECT 1 ELSE SELECT 0";
		string lastquery = "";
		int inserts = 0;
		int updates = 0;
		int exists = 0;
		int losts = 0;
		foreach (IDictionary<string, object> raw in data)
		{
			try
			{
				if (operation == 0)
				{
					if (db.Query<int>(WhereStatment, raw, null, buffered: true, timeout).FirstOrDefault() != 0)
					{
						db.Query<int?>(updateSql, raw, null, buffered: true, timeout).FirstOrDefault();
						updates++;
					}
					else
					{
						db.Query<int?>(insertSql, raw, null, buffered: true, timeout).FirstOrDefault();
						inserts++;
					}
				}
				if (operation == 1)
				{
					if (db.Query<int>(WhereStatment, raw, null, buffered: true, timeout).FirstOrDefault() != 0)
					{
						losts++;
					}
					else
					{
						db.Query<int?>(insertSql, raw, null, buffered: true, timeout).FirstOrDefault();
						inserts++;
					}
				}
				if (operation == 2)
				{
					if (db.Query<int>(WhereStatment, raw, null, buffered: true, timeout).FirstOrDefault() != 0)
					{
						db.Query<int?>(updateSql, raw, null, buffered: true, timeout).FirstOrDefault();
						updates++;
					}
					else
					{
						losts++;
					}
				}
			}
			catch (Exception ex)
			{
				string sMess = "";
				sMess = sMess + "Error         : " + ex.ToString() + "\r\n";
				sMess += " \r\n";
				sMess = sMess + "   Last query : " + lastquery + " \r\n";
				sMess = sMess + "   For Object : " + JsonSerializer.Serialize(raw);
				throw new Exception(sMess);
			}
		}
		string sInfoMess = "SaveToTable Summary : " + data.Count() + " total raws to insert/update.  " + updates + " raws updated, " + inserts + " raws inserted.";
		if (losts != 0)
		{
			switch (operation)
			{
			case 1:
				sInfoMess = sInfoMess + "\r\n Found " + losts + " records already on db and not Inserted.";
				break;
			case 2:
				sInfoMess = sInfoMess + "\r\n Not found " + losts + " records on db and not Updated.";
				break;
			}
		}
		logger.LogInformation(sInfoMess);
	}

	public DbTableModel GetTableInfo(string constr, string tableName, int timeout = 60)
	{
		DbTableModel table = new DbTableModel();
		table.TableName = tableName;
		using IDbConnection db = new SqlConnection(constr);
		table.Columns = db.Query<DBColumnModel>("SELECT distinct\r\n                        c.column_id 'Position',\r\n                        c.name 'ColumnName',\r\n                        tt.Name 'DataType',\r\n                        c.max_length 'MaxLength',\r\n                        c.precision 'Precision',\r\n                        c.scale 'Scale',\r\n                        c.is_nullable 'Nullable',\r\n                        ISNULL(i.is_primary_key, 0) 'PrimaryKey',\r\n\t                    is_identity 'AutoIncrement'\r\n                    FROM    \r\n                        sys.columns c\r\n                    INNER JOIN sys.tables t ON t.object_id = c.object_id AND t.name = @TableName\r\n\t\t\t\t\tOUTER APPLY(\r\n\t\t\t\t\t\tSELECT DISTINCT  i.is_primary_key, i.name\r\n\t\t\t\t\t\tFROM sys.indexes i\r\n\t\t\t\t\t\tINNER JOIN sys.index_columns ic ON  ic.object_id = i.object_id AND ic.column_id = c.column_id and ic.index_id=i.index_id\r\n\t\t\t\t\t\tWHERE i.object_id = t.object_id AND i.is_primary_key = 1 and i.type=1\r\n\t\t\t\t\t) i \r\n\t\t\t\t\tINNER JOIN sys.types tt on tt.user_type_id=c.user_type_id\r\n                  Order By Position", new
		{
			TableName = tableName
		}, null, buffered: true, timeout).ToList();
		return table;
	}
}
