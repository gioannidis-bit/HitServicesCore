using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using AutoMapper;

namespace HitServicesCore.Helpers;

public class ConvertDataHelper
{
	private Dictionary<string, Formater> formater;

	public ConvertDataHelper()
	{
		formater = new Dictionary<string, Formater>();
	}

	public ConvertDataHelper(Dictionary<string, Formater> formater)
	{
		this.formater = formater;
		if (this.formater == null)
		{
			this.formater = new Dictionary<string, Formater>();
		}
	}

	public string ToXml(List<IDictionary<string, dynamic>> data, string RootElement, string element, IMapper mapper)
	{
		XElement rootEl = new XElement(RootElement);
		foreach (IDictionary<string, object> item in data)
		{
			XElement el = new XElement(element, item.Select<KeyValuePair<string, object>, object>((KeyValuePair<string, dynamic> kv) => createXmlElement(kv.Key, kv.Value, mapper)));
			rootEl.Add(el);
		}
		return "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" + rootEl.ToString();
	}

	private XElement createXmlElement(string key, dynamic value, IMapper mapper)
	{
		IEnumerable<dynamic> list = value as IEnumerable<object>;
		if (list == null)
		{
			return new XElement(key, ToString(key, value, true));
		}
		XElement el = new XElement(key + "List");
		foreach (dynamic item in list)
		{
			XElement elnested = new XElement(key);
			IDictionary<string, dynamic> members = mapper.Map<IDictionary<string, object>>(item);
			foreach (string keymember in members.Keys)
			{
				XElement elnested2 = createXmlElement(keymember, members[keymember], mapper);
				elnested.Add(elnested2);
			}
			el.Add(elnested);
		}
		return el;
	}

	public string ToJson(List<IDictionary<string, dynamic>> data)
	{
		return JsonSerializer.Serialize(data);
	}

	public string ToCsv(List<IDictionary<string, dynamic>> data, bool header, string delimeter = ";")
	{
		if (delimeter.ToLower() == "comma")
		{
			delimeter = ",";
		}
		else if (delimeter.ToLower() == "space")
		{
			delimeter = " ";
		}
		else if (delimeter.ToLower() == "tab")
		{
			delimeter = "\t";
		}
		StringBuilder file = new StringBuilder((data.Count() + 1) * 400);
		int items = data[0].Keys.Count();
		if (header)
		{
			int k = 0;
			foreach (string key in data[0].Keys)
			{
				file.Append(key.ToString());
				if (++k != items)
				{
					file.Append(delimeter);
				}
			}
			file.AppendLine();
		}
		int i = 0;
		int lines = data.Count();
		foreach (IDictionary<string, object> item in data)
		{
			int j = 0;
			foreach (string key2 in item.Keys)
			{
				file.Append(ToString(key2, (dynamic)item[key2]));
				if (++j != items)
				{
					file.Append(delimeter);
				}
			}
			if (++i != lines)
			{
				file.AppendLine();
			}
		}
		return file.ToString();
	}

	public string ToStandarFile(List<IDictionary<string, dynamic>> data, bool appendLine = true)
	{
		StringBuilder file = new StringBuilder((data.Count() + 1) * 400);
		int items = data[0].Keys.Count();
		foreach (IDictionary<string, object> item in data)
		{
			foreach (string key in item.Keys)
			{
				file.Append(ToString(key, (dynamic)item[key]));
			}
			if (appendLine)
			{
				file.AppendLine();
			}
		}
		return file.ToString();
	}

	public string ToFixedLenght(List<IDictionary<string, dynamic>> data, bool header, List<int?> lengths, bool alignRight)
	{
		StringBuilder file = new StringBuilder((data.Count() + 1) * 400);
		int items = data[0].Keys.Count();
		if (header)
		{
			int k = 0;
			if (lengths != null)
			{
				foreach (string key in data[0].Keys)
				{
					if (alignRight)
					{
						file.Append(key.PadLeft(lengths[0].Value));
					}
					else
					{
						file.Append(key.PadRight(lengths[0].Value));
					}
					k++;
				}
			}
			file.AppendLine();
		}
		int i = 0;
		int lines = data.Count();
		foreach (IDictionary<string, object> item in data)
		{
			int j = 0;
			foreach (string key2 in item.Keys)
			{
				string value = ToString(key2, (dynamic)item[key2]);
				if (alignRight)
				{
					file.Append(value.PadLeft(lengths[0].Value));
				}
				else
				{
					file.Append(value.PadRight(lengths[0].Value));
				}
				j++;
			}
			if (++i != lines)
			{
				file.AppendLine();
			}
		}
		return file.ToString();
	}

	public string ToHtml(List<IDictionary<string, dynamic>> data, bool header, string title, bool sortColumns, string css = "")
	{
		string startRow = ((!header) ? "1" : "2");
		StringBuilder file = new StringBuilder((data.Count() + 1) * 500);
		file.Append("<!DOCTYPE html>\r\n            <html>\r\n\r\n               <head>\r\n            <meta http-equiv=\"Content-Type\" content=\"text/html; charset=UTF-8\" />\r\n                            <meta charset = \"utf-8\" />\r\n            <style type=\"text/css\">\r\n            " + css + "\r\n                </style>\r\n         \r\n               <title>" + title + "</title>\r\n               </head>\r\n\t\r\n               <body>\r\n                <div style='overflow-x:auto;'>\r\n                   <table id='hittable'>");
		int items = data[0].Keys.Count();
		if (header)
		{
			int k = 0;
			file.Append("<tr> <th style='text-align: center; ' colspan='" + data[0].Keys.Count + "' > " + title + " </th></tr> ");
			file.Append("<tr>");
			foreach (string key in data[0].Keys)
			{
				file.Append("<th ");
				if (sortColumns)
				{
					file.Append(" onclick='sortTable(" + k + ")' ");
				}
				file.Append("> " + key.ToString() + "</th>");
				k++;
			}
			file.AppendLine("</tr>");
		}
		int i = 0;
		int lines = data.Count();
		foreach (IDictionary<string, object> item in data)
		{
			int j = 0;
			file.Append("<tr id='L" + i + "'>");
			foreach (string key2 in item.Keys)
			{
				file.Append("<td id='" + i + j + "' class='td" + j + "'>" + ToString(key2, (dynamic)item[key2]) + "</td>");
				j++;
			}
			file.AppendLine("</tr>");
			if (++i != lines)
			{
				file.AppendLine();
			}
		}
		file.Append("\r\n                </table>\r\n              </div>");
		if (sortColumns)
		{
			file.Append("\r\n                <script>\r\n                function sortTable(n) {\r\n                  var table, rows, switching, i, x, y, shouldSwitch, dir, switchcount = 0;\r\n                  table = document.getElementById(\"hittable\");\r\n                  switching = true;\r\n                                // Set the sorting direction to ascending:\r\n                                dir = \"asc\";\r\n                                /* Make a loop that will continue until\r\n                                no switching has been done: */\r\n                                while (switching)\r\n                                {\r\n                                    // Start by saying: no switching is done:\r\n                                    switching = false;\r\n                                    rows = table.getElementsByTagName(\"TR\");\r\n                                    /* Loop through all table rows (except the\r\n                                    first, which contains table headers): */\r\n                                    for (i = " + startRow + "; i < (rows.length - 1); i++)\r\n                                    {\r\n                                       // Start by saying there should be no switching:\r\n                                        shouldSwitch = false;\r\n                                        /* Get the two elements you want to compare,\r\n                                        one from current row and one from the next: */\r\n                                        x = rows[i].getElementsByTagName(\"TD\")[n];\r\n                                        y = rows[i + 1].getElementsByTagName(\"TD\")[n];\r\n                                        if (isNaN(x.innerHTML))\r\n                                        {\r\n                                            /* Check if the two rows should switch place,\r\n                                            based on the direction, asc or desc: */\r\n                                            if (dir == \"asc\")\r\n                                            {\r\n                                                if (x.innerHTML.toLowerCase() > y.innerHTML.toLowerCase())\r\n                                                {\r\n                                                    // If so, mark as a switch and break the loop:\r\n                                                    shouldSwitch = true;\r\n                                                    break;\r\n                                                }\r\n                                            }\r\n                                            else if (dir == \"desc\")\r\n                                            {\r\n                                                if (x.innerHTML.toLowerCase() < y.innerHTML.toLowerCase())\r\n                                                {\r\n                                                    // If so, mark as a switch and break the loop:\r\n                                                    shouldSwitch = true;\r\n                                                    break;\r\n                                                }\r\n                                            }\r\n                                        }\r\n                                        else   //x is number\r\n                                        {\r\n                                            /* Check if the two rows should switch place,\r\n                                            based on the direction, asc or desc: */\r\n                                            if (dir == \"asc\")\r\n                                            {\r\n                                                if (Number(x.innerHTML) > Number(y.innerHTML))\r\n                                                {\r\n                                                    // If so, mark as a switch and break the loop:\r\n                                                    shouldSwitch = true;\r\n                                                    break;\r\n                                                }\r\n                                            }\r\n                                            else if (dir == \"desc\")\r\n                                            {\r\n                                                if (Number(x.innerHTML) < Number(y.innerHTML))\r\n                                                {\r\n                                                    // If so, mark as a switch and break the loop:\r\n                                                    shouldSwitch = true;\r\n                                                    break;\r\n                                                }\r\n                                            }\r\n                                        }\r\n                                    }\r\n                                    if (shouldSwitch)\r\n                                    {\r\n                                        /* If a switch has been marked, make the switch\r\n                                        and mark that a switch has been done: */\r\n                                        rows[i].parentNode.insertBefore(rows[i + 1], rows[i]);\r\n                                        switching = true;\r\n                                        // Each time a switch is done, increase this count by 1:\r\n                                        switchcount++;\r\n                                    }\r\n                                    else\r\n                                    {\r\n                                        /* If no switching has been done AND the direction is \"asc\",\r\n                                        set the direction to \"desc\" and run the while loop again. */\r\n                                        if (switchcount == 0 && dir == \"asc\")\r\n                                        {\r\n                                            dir = \"desc\";\r\n                                            switching = true;\r\n                                        }\r\n                                    }\r\n                                }\r\n                            }\r\n                </script>\r\n            ");
		}
		file.Append(" </body>\r\n       </html>");
		return file.ToString();
	}

	private string ToString(string key, dynamic value, bool returnNull = false)
	{
		if (value == null && !returnNull)
		{
			return "";
		}
		if (value == null && returnNull)
		{
			return null;
		}
		if (formater.ContainsKey(key))
		{
			return value.ToString(formater[key].Format, formater[key].CultureInfo);
		}
		return value.ToString();
	}
}
