using System;
using Dapper;

namespace SendInvoiceViaEmail.LocalModels;

[Table("SendInvoiceViaEmail")]
public class SendInvoiceViaEmailDTO
{
	[Key]
	public long Id { get; set; }

	public int Mpehotel { get; set; }

	public int ReservationId { get; set; }

	public int ProfileId { get; set; }

	public string ProfileName { get; set; }

	public int InvoiceTypeId { get; set; }

	public int InvoiceNo { get; set; }

	public DateTime IssueDate { get; set; }

	public string EmailTo { get; set; }

	public string StatusCode { get; set; }

	public string ErrorMessage { get; set; }

	public DateTime CreationDate { get; set; }
}
