using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using HitServicesCore.Models.IS_Services;

namespace HitServicesCore.Helpers;

public class SqlConstructorHelper
{
	private CultureInfo CultureInfo = CultureInfo.CreateSpecificCulture("en-us");

	public string InsertStatment(DbTableModel tableInfo, IDictionary<string, dynamic> data = null, bool returnIdentity = true, SqlKeyModel sqlEncrypt = null)
	{
		bool encrypt = false;
		List<DBColumnModel> columns = tableInfo.Columns.Where((DBColumnModel x) => !x.AutoIncrement).ToList();
		StringBuilder sql = new StringBuilder();
		if (sqlEncrypt != null && sqlEncrypt.EncryptedColumns != null && sqlEncrypt.EncryptedColumns.Count > 0)
		{
			encrypt = true;
			sql.Append("OPEN SYMMETRIC KEY " + sqlEncrypt.SymmetricKey + "\r\n                    DECRYPTION BY CERTIFICATE " + sqlEncrypt.Certificate + "\r\n                    with PASSWORD = '" + sqlEncrypt.Password + "';\r\n                 ");
		}
		sql.Append("INSERT INTO [" + tableInfo.TableName + "] (");
		sql.Append(string.Join(", ", columns.Select((DBColumnModel x) => "[" + x.ColumnName + "]").ToArray()));
		sql.Append(") VALUES (");
		if (data == null)
		{
			sql.Append(string.Join(", ", columns.Select((DBColumnModel x) => (encrypt && sqlEncrypt.EncryptedColumns.FirstOrDefault((string str) => x.ColumnName.ToUpper() == str.ToUpper()) != null) ? ("EncryptByKey(Key_GUID('" + sqlEncrypt.SymmetricKey + "'), @" + x.ColumnName + ")") : ("@" + x.ColumnName)).ToArray()));
		}
		else
		{
			int i = 0;
			foreach (DBColumnModel column in columns)
			{
				if (!encrypt || sqlEncrypt.EncryptedColumns.FirstOrDefault((string str) => column.ColumnName.ToUpper() == str.ToUpper()) == null)
				{
					sql.Append(SqlToString(column.DataType, data[column.ColumnName]));
				}
				else
				{
					sql.Append("EncryptByKey(Key_GUID('" + sqlEncrypt.SymmetricKey + "'),'" + SqlToString(column.DataType, data[column.ColumnName]) + "')");
				}
				i++;
				if (i < columns.Count)
				{
					sql.Append(",");
				}
			}
		}
		sql.Append(");");
		if (!returnIdentity)
		{
			sql.Append("select SCOPE_IDENTITY() ");
		}
		return sql.ToString();
	}

	public string WhereStatmentForRecordExists(DbTableModel tableInfo, List<IDictionary<string, dynamic>> data)
	{
		if (data == null)
		{
			return "";
		}
		List<DBColumnModel> keyColumns = tableInfo.Columns.Where((DBColumnModel x) => x.PrimaryKey).ToList();
		StringBuilder sql = new StringBuilder();
		int i = 0;
		IDictionary<string, dynamic> model = data[0];
		foreach (DBColumnModel item in keyColumns)
		{
			if (model.ContainsKey(item.ColumnName))
			{
				sql.Append("[" + item.ColumnName.Trim() + "] = @" + item.ColumnName.Trim());
				i++;
				if (i < keyColumns.Count)
				{
					sql.Append(" AND ");
				}
			}
		}
		if (sql.ToString().Length < 10)
		{
			foreach (string key in model.Keys)
			{
				sql.Append("[" + key.Trim() + "] = @" + key.Trim());
				i++;
				if (i < model.Count)
				{
					sql.Append(" AND ");
				}
			}
		}
		return sql.ToString();
	}

	public string WhereStatmentForRecordExists(List<DBColumnModel> keyColumns, ImportDataToTableFromCsvModel model)
	{
		string result = "";
		int i = 0;
		foreach (DBColumnModel item in keyColumns)
		{
			CsvColumnsHeaderModel fld = model.ColumnsData.First((CsvColumnsHeaderModel f) => f.ColumnName.Trim() == item.ColumnName.Trim());
			if (fld != null)
			{
				result = result + "[" + fld.ColumnName.Trim() + "] = " + ((fld.ColumnValue.ToLower() == "null") ? "NULL" : ("'" + fld.ColumnValue + "'"));
				i++;
				if (i < keyColumns.Count)
				{
					result += " AND ";
				}
			}
		}
		return result;
	}

	public string MakeUpdateStatmentForCsvInsertData(string tableName, List<DBColumnModel> keyColumns, ImportDataToTableFromCsvModel model)
	{
		string result = "";
		string where = WhereStatmentForRecordExists(keyColumns, model);
		int i = 0;
		foreach (CsvColumnsHeaderModel item in model.ColumnsData)
		{
			i++;
			DBColumnModel fld = keyColumns.Find((DBColumnModel f) => f.ColumnName.Trim() == item.ColumnName.Trim());
			if (fld == null)
			{
				result = result + item.ColumnName + " = " + ((item.ColumnValue.ToLower() == "null") ? "NULL" : ("'" + item.ColumnValue + "'"));
				if (i < model.ColumnsData.Count)
				{
					result += ",";
				}
			}
		}
		if (!string.IsNullOrWhiteSpace(result))
		{
			result = "UPDATE " + tableName + " SET " + result + " " + ((!string.IsNullOrWhiteSpace(where)) ? (" WHERE " + where) : "");
		}
		return result;
	}

	public string MakeInsertStatmentForCsvInsertData(string tableName, List<DBColumnModel> keyColumns, ImportDataToTableFromCsvModel model)
	{
		string result = "";
		string isnsertStat = "";
		string valuesStat = "";
		int i = 0;
		foreach (CsvColumnsHeaderModel item in model.ColumnsData)
		{
			i++;
			DBColumnModel fld = keyColumns.Find((DBColumnModel f) => f.ColumnName.Trim() == item.ColumnName.Trim());
			if (fld == null || !fld.AutoIncrement)
			{
				isnsertStat += item.ColumnName;
				valuesStat += ((item.ColumnValue.ToLower() == "null") ? "NULL" : ("'" + item.ColumnValue + "'"));
				if (i < model.ColumnsData.Count)
				{
					isnsertStat += ",";
					valuesStat += ",";
				}
			}
		}
		if (!string.IsNullOrWhiteSpace(isnsertStat))
		{
			result = "INSERT INTO " + tableName + " (" + isnsertStat + ") SELECT " + valuesStat;
		}
		return result;
	}

	public string UpdateStatment(DbTableModel tableInfo, IDictionary<string, dynamic> data = null, SqlKeyModel sqlEncrypt = null)
	{
		bool encrypt = false;
		List<DBColumnModel> noKeyColumns = tableInfo.Columns.Where((DBColumnModel x) => !x.PrimaryKey).ToList();
		List<DBColumnModel> keyColumns = tableInfo.Columns.Where((DBColumnModel x) => x.PrimaryKey).ToList();
		int i = 0;
		StringBuilder sql = new StringBuilder();
		if (sqlEncrypt != null && sqlEncrypt.EncryptedColumns != null && sqlEncrypt.EncryptedColumns.Count > 0)
		{
			encrypt = true;
			sql.Append("OPEN SYMMETRIC KEY " + sqlEncrypt.SymmetricKey + "\r\n                    DECRYPTION BY CERTIFICATE " + sqlEncrypt.Certificate + "\r\n                    with PASSWORD = '" + sqlEncrypt.Password + "';\r\n                 ");
		}
		if (keyColumns == null || keyColumns.Count() == 0)
		{
			throw new Exception("Table [" + tableInfo.TableName + "] does NOT contain Primary Key. Unable to update.");
		}
		sql.Append("UPDATE [" + tableInfo.TableName + "] SET ");
		foreach (DBColumnModel column in noKeyColumns)
		{
			sql.Append("[" + column.ColumnName + "] = ");
			if (data == null)
			{
				if (encrypt && sqlEncrypt.EncryptedColumns.FirstOrDefault((string str) => column.ColumnName.ToUpper() == str.ToUpper()) != null)
				{
					sql.Append("EncryptByKey(Key_GUID('" + sqlEncrypt.SymmetricKey + "'), @" + column.ColumnName + ")");
				}
				else
				{
					sql.Append("@" + column.ColumnName);
				}
			}
			else if (!encrypt || sqlEncrypt.EncryptedColumns.FirstOrDefault((string str) => column.ColumnName.ToUpper() == str.ToUpper()) == null)
			{
				sql.Append(SqlToString(column.DataType, data[column.ColumnName]));
			}
			else
			{
				sql.Append("EncryptByKey(Key_GUID('" + sqlEncrypt.SymmetricKey + "'),'" + SqlToString(column.DataType, data[column.ColumnName]) + "')");
			}
			i++;
			if (i < noKeyColumns.Count)
			{
				sql.Append(", ");
			}
		}
		sql.Append(" WHERE ");
		i = 0;
		foreach (DBColumnModel column2 in keyColumns)
		{
			sql.Append("[" + column2.ColumnName + "] = ");
			if (data != null)
			{
				sql.Append(SqlToString(column2.DataType, data[column2.ColumnName]));
			}
			else
			{
				sql.Append("@" + column2.ColumnName);
			}
			i++;
			if (i < keyColumns.Count)
			{
				sql.Append(" AND ");
			}
		}
		sql.Append("; select @@ROWCOUNT");
		return sql.ToString();
	}

	public string SelectCount(DbTableModel tableInfo, IDictionary<string, dynamic> data = null)
	{
		List<DBColumnModel> keyColumns = tableInfo.Columns.Where((DBColumnModel x) => x.PrimaryKey).ToList();
		int i = 0;
		if (keyColumns == null || keyColumns.Count() == 0)
		{
			throw new Exception("Table [" + tableInfo.TableName + "] does NOT contain Primary Key. Unable to construct select count query.");
		}
		StringBuilder sql = new StringBuilder("select count(*) from  [" + tableInfo.TableName + "] WHERE ");
		i = 0;
		foreach (DBColumnModel column in keyColumns)
		{
			sql.Append("[" + column.ColumnName + "] = ");
			if (data != null)
			{
				sql.Append(SqlToString(column.DataType, data[column.ColumnName]));
			}
			else
			{
				sql.Append("@" + column.ColumnName);
			}
			i++;
			if (i < keyColumns.Count)
			{
				sql.Append(" AND ");
			}
		}
		sql.Append("");
		return sql.ToString();
	}

	public string SqlToString(string dataType, dynamic value)
	{
		if (value == null)
		{
			return "null";
		}
		switch (dataType)
		{
		case "decimal":
		case "money":
		case "numeric":
		case "float":
			return value.ToString("F6", CultureInfo);
		case "date":
			return "'" + value.ToString("yyyy-MM-dd") + "'";
		case "datetime2":
		case "datetime":
		case "smalldatetime":
			return "'" + value.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo) + "'";
		case "varbinary":
		case "binary":
		case "image":
			return Encoding.UTF8.GetString(value);
		default:
			return "'" + value.ToString() + "'";
		}
	}
}
