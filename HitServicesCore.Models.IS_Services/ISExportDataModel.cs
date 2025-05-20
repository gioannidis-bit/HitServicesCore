using System.Collections.Generic;
using HitHelpersNetCore.Models;

namespace HitServicesCore.Models.IS_Services;

public class ISExportDataModel : ISServiceGeneralModel
{
	public string Custom1DB { get; set; } = "Server=server;Database=db;User id=user;Password=password";

	public string DBTimeout { get; set; } = "60";

	public string SqlScript { get; set; }

	public List<HitHelpersNetCore.Models.BaseKeyValueModel> SqlParameters { get; set; } = new List<HitHelpersNetCore.Models.BaseKeyValueModel>();

	public Dictionary<string, string> Formater { get; set; }

	public string FilePath { get; set; }

	public string TimeStamp { get; set; } = "yyyyMMddHHmmss";

	public string CultureInfo { get; set; } = "en-us";

	public string EncryptFile { get; set; }

	public int? EncryptType { get; set; } = 0;

	public bool? HasFileHeader { get; set; } = true;

	public FtpModel ExportedFileToFTP { get; set; }

	public SendSmtpModel ExportedFileSendWithSmtp { get; set; }

	public string XmlFilePath { get; set; }

	public string XmlRootElement { get; set; }

	public string XmlElement { get; set; }

	public FtpModel XmlFileToFTP { get; set; }

	public SendSmtpModel XmlFileSendWithSmtp { get; set; }

	public string JsonFilePath { get; set; }

	public FtpModel JsonFileToFTP { get; set; }

	public SendSmtpModel JsonFileSendWithSmtp { get; set; }

	public string CsvFilePath { get; set; }

	public bool? CsvFileHeader { get; set; } = true;

	public string CsvDelimenter { get; set; } = ";";

	public FtpModel CsvFileToFTP { get; set; }

	public SendSmtpModel CsvFileSendWithSmtp { get; set; }

	public string FixedLenghtFilePath { get; set; }

	public bool? FixedLenghtFileHeader { get; set; }

	public bool? FixedLenghtAlignRight { get; set; }

	public List<int?> FixedLengths { get; set; }

	public FtpModel FixedLenghtFileToFTP { get; set; }

	public SendSmtpModel FixedLenghtFileSendWithSmtp { get; set; }

	public string HtmlFilePath { get; set; }

	public bool? HtmlHeader { get; set; } = true;

	public string HtmlTitle { get; set; }

	public bool? HtmlSortRows { get; set; } = true;

	public string Htmlcss { get; set; } = "\r\nbody{\r\n    padding: 0; \r\n    border: 0; \r\n    margin: 0;\r\n}\r\n#hittable {\r\n    font-family:  \"Trebuchet MS\",Arial, Helvetica, sans-serif;\r\n    border - collapse: collapse;\r\n    width: 100 %;\r\n    padding: 0; \r\n    margin: 0;\r\n   }\r\n\r\n#hittable td, #hittable th {\r\n        border: 1px solid #ddd;\r\n        padding: 8px;\r\n  }\r\n\r\n#hittable tr:nth-child(even){background-color: #f2f2f2;}\r\n\r\n#hittable tr:hover {background-color: #ddd;}\r\n\r\n#hittable th {\r\n    padding-top: 12px;\r\n    padding-bottom: 12px;\r\n    text-align: left;\r\n    background-color: #4CAF50;\r\n    color: white;\r\n}";

	public FtpModel HtmlFileToFTP { get; set; }

	public SendSmtpModel HtmlFileSendWithSmtp { get; set; }

	public string PdfFilePath { get; set; }

	public string PdfTitle { get; set; }

	public string Pdfcss { get; set; }

	public FtpModel PdfFileToFTP { get; set; }

	public SendSmtpModel PdfFileSendWithSmtp { get; set; }

	public string RestServerUrl { get; set; }

	public string RestServerAuthenticationHeader { get; set; }

	public string RestServerAuthenticationType { get; set; } = "Basic";

	public string RestServerHttpMethod { get; set; }

	public string RestServerMediaType { get; set; }

	public Dictionary<string, string> RestServerCustomHeaders { get; set; }
}
