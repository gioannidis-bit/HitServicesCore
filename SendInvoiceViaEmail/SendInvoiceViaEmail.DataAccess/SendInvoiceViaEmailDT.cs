using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using Dapper;
using SendInvoiceViaEmail.LocalModels;

namespace SendInvoiceViaEmail.DataAccess;

public class SendInvoiceViaEmailDT
{
	private readonly string _connection;

	private readonly string _dbSchema;

	public SendInvoiceViaEmailDT(string connection, string dbSchema)
	{
		_connection = connection;
		_dbSchema = dbSchema;
	}

	public InvoiceModel GetInvoiceData(int profileId, int invoiceNo, int resNo, out string errorMess)
	{
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Expected O, but got Unknown
		InvoiceModel invoiceModel = null;
		errorMess = "";
		string text = "";
		try
		{
			text = "SELECT \r\n                            r.kdnr                                                                                      AS  kdnr, \r\n                            r.fisccode                                                                                  AS  fisccode, \r\n                            r.resno                                                                                     AS  resno, \r\n                            r.rechnr                                                                                    AS  rechnr, \r\n                            r.datum                                                                                     AS  issueDate, \r\n                            r.mpehotel                                                                                  AS  mpehotel, \r\n                            ISNULL(k.name1,'')+CASE WHEN ISNULL(k.vorname,'') <> '' THEN ' '+k.vorname ELSE '' END      AS  profileName\r\n                        FROM " + _dbSchema + ".rechhist       AS r\r\n                        LEFT OUTER JOIN " + _dbSchema + ".kunden   AS k    ON k.kdnr = r.kdnr\r\n                        WHERE \r\n                            r.resno = @resNo        AND \r\n                            r.kdnr = @profileId     AND \r\n                            r.rechnr = @rechnr";
			using IDbConnection dbConnection = new SqlConnection(_connection);
			invoiceModel = SqlMapper.Query<InvoiceModel>(dbConnection, text, (object)new
			{
				resNo = resNo,
				profileId = profileId,
				rechnr = invoiceNo
			}, (IDbTransaction)null, true, (int?)null, (CommandType?)null).FirstOrDefault();
			if (invoiceModel != null)
			{
				text = "SELECT DISTINCT g.entry        AS  entry\r\n                                FROM " + _dbSchema + ".gcom               AS g\r\n                                INNER JOIN " + _dbSchema + ".gcomtype     AS gt   ON  gt.ref = g.[type]   AND \r\n                                                                                gt.para3 = 19\r\n                                WHERE \r\n                                    g.prim = 1                  AND \r\n                                    ISNULL(g.entry,'') <> ''    AND \r\n                                    g.kdnr = @kdnr";
				invoiceModel.email = SqlMapper.Query<string>(dbConnection, text, (object)new { invoiceModel.kdnr }, (IDbTransaction)null, true, (int?)null, (CommandType?)null).ToList();
			}
			else
			{
				errorMess = $"Cannot get data for invoice on leistacc {resNo} with profile id {profileId} and invoice number {invoiceNo}";
			}
			if (dbConnection.State == ConnectionState.Open)
			{
				dbConnection.Close();
			}
		}
		catch (Exception ex)
		{
			errorMess = "[SQL: " + text + "] \r\n" + ex.ToString();
		}
		return invoiceModel;
	}

	public void AddErrorToProtelTable(string errorMess, int mpehotel, int leistacc, int kundennr)
	{
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Expected O, but got Unknown
		int num = 0;
		using IDbConnection dbConnection = new SqlConnection(_connection);
		num = SqlMapper.Query<int>(dbConnection, "SELECT ISNULL(kdnr,0) FROM " + _dbSchema + ".intfenr", (object)null, (IDbTransaction)null, true, (int?)null, (CommandType?)null).FirstOrDefault();
		if (num < 1)
		{
			SqlMapper.Execute(dbConnection, "INSERT INTO " + _dbSchema + ".intfenr (mpehotel,kdnr) VALUES(1, " + num + ")", (object)null, (IDbTransaction)null, (int?)null, (CommandType?)null);
			num = 1;
		}
		else
		{
			num++;
			SqlMapper.Execute(dbConnection, "UPDATE " + _dbSchema + ".intfenr SET kdnr = " + num, (object)null, (IDbTransaction)null, (int?)null, (CommandType?)null);
		}
		string text = "INSERT INTO " + _dbSchema + ".intfehl (mpehotel, tan, station, intname, fehler, datum, uhrzeit, xfehler, leistacc, kundennr, type, _del) VALUES \r\n                                (@mpehotel, @tan, @station, @intname, @fehler, @datum, @uhrzeit, @xfehler, @leistacc, @kundennr, @type, @del)\r\n                            ";
		SqlMapper.Execute(dbConnection, text, (object)new
		{
			mpehotel = mpehotel,
			tan = num,
			station = 0,
			intname = "EmailInvoice",
			fehler = "Error sending invoice via email",
			datum = DateTime.Today,
			uhrzeit = DateTime.Now.ToString("HH:mm"),
			xfehler = errorMess,
			leistacc = leistacc,
			kundennr = kundennr,
			type = 2,
			del = 0
		}, (IDbTransaction)null, (int?)null, (CommandType?)null);
		text = "INSERT INTO " + _dbSchema + ".intfehlhistory (mpehotel, tan, station, intname, fehler, datum, uhrzeit, xfehler, leistacc, kundennr, type, _del) VALUES \r\n                         (@mpehotel, @tan, @station, @intname, @fehler, @datum, @uhrzeit, @xfehler, @leistacc, @kundennr, @type, @del)\r\n                        ";
		SqlMapper.Execute(dbConnection, text, (object)new
		{
			mpehotel = mpehotel,
			tan = num,
			station = 0,
			intname = "EmailInvoice",
			fehler = "Error sending invoice via email",
			datum = DateTime.Today,
			uhrzeit = DateTime.Now.ToString("HH:mm"),
			xfehler = errorMess,
			leistacc = leistacc,
			kundennr = kundennr,
			type = 2,
			del = 0
		}, (IDbTransaction)null, (int?)null, (CommandType?)null);
		if (dbConnection.State == ConnectionState.Open)
		{
			dbConnection.Close();
		}
	}

	public List<FieldsModel> GetProtelValues(SelectValuesModel model, string sWehere)
	{
		//IL_00d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dc: Expected O, but got Unknown
		//IL_00e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ed: Expected O, but got Unknown
		List<FieldsModel> list = new List<FieldsModel>();
		string format = "SELECT CAST({0} AS VARCHAR(2000)) {1} FROM {2} WHERE {3} ";
		string text = "";
		FieldsModel fieldsModel = model.fields.Last();
		foreach (FieldsModel field in model.fields)
		{
			object[] args = new object[4]
			{
				field.protelValue,
				"'" + field.keyValue + "'",
				_dbSchema + "." + model.tableName,
				sWehere
			};
			text = ((field == fieldsModel) ? (text + string.Format(format, args)) : (text + string.Format(format, args) + "\n UNION ALL \n"));
		}
		if (!string.IsNullOrWhiteSpace(text))
		{
			SqlConnection val = new SqlConnection(_connection);
			try
			{
				((DbConnection)(object)val).Open();
				SqlDataAdapter val2 = new SqlDataAdapter(text, val);
				DataSet dataSet = new DataSet();
				((DataAdapter)(object)val2).Fill(dataSet);
				if (dataSet.Tables != null && dataSet.Tables.Count > 0 && dataSet.Tables[0].Rows != null && dataSet.Tables[0].Rows.Count > 0)
				{
					for (int i = 0; i < model.fields.Count; i++)
					{
						list.Add(new FieldsModel
						{
							keyValue = model.fields[i].keyValue,
							protelValue = (string)dataSet.Tables[0].Rows[i][0]
						});
					}
				}
				dataSet.Dispose();
				((Component)(object)val2).Dispose();
				((DbConnection)(object)val).Close();
				((Component)(object)val).Dispose();
			}
			finally
			{
				((IDisposable)val)?.Dispose();
			}
		}
		return list;
	}

	public void AddEmailStatusToDB(SendInvoiceViaEmailDTO model)
	{
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Expected O, but got Unknown
		string empty = string.Empty;
		using IDbConnection dbConnection = new SqlConnection(_connection);
		empty = "\r\n                        SELECT * \r\n                        FROM " + _dbSchema + ".SendInvoiceViaEmail \r\n                        WHERE       \r\n                            Mpehotel = @mpehotel        AND \r\n                            ReservationId = @resId      AND \r\n                            ProfileId = @profileId      AND \r\n                            InvoiceNo = @invoice        AND \r\n                            InvoiceTypeId = @invoiceType\r\n                        ";
		SendInvoiceViaEmailDTO sendInvoiceViaEmailDTO = SqlMapper.Query<SendInvoiceViaEmailDTO>(dbConnection, empty, (object)new
		{
			mpehotel = model.Mpehotel,
			resId = model.ReservationId,
			profileId = model.ProfileId,
			invoice = model.InvoiceNo,
			invoiceType = model.InvoiceTypeId
		}, (IDbTransaction)null, true, (int?)null, (CommandType?)null).FirstOrDefault();
		if (sendInvoiceViaEmailDTO != null)
		{
			model.Id = sendInvoiceViaEmailDTO.Id;
			model.CreationDate = sendInvoiceViaEmailDTO.CreationDate;
			SimpleCRUD.Update<SendInvoiceViaEmailDTO>(dbConnection, model, (IDbTransaction)null, (int?)null);
		}
		else
		{
			model.Id = 0L;
			model.CreationDate = DateTime.Now;
			SimpleCRUD.Insert<SendInvoiceViaEmailDTO>(dbConnection, model, (IDbTransaction)null, (int?)null);
		}
		if (dbConnection.State == ConnectionState.Open)
		{
			dbConnection.Close();
		}
	}
}
