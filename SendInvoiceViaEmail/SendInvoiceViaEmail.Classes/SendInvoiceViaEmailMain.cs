using System;
using HitCustomAnnotations.Interfaces;

namespace SendInvoiceViaEmail.Classes;

public class SendInvoiceViaEmailMain : IMainDescriptor
{
	public Guid plugIn_Id => Guid.Parse("31E4F8B0-2D5E-4F2E-97EA-D3A987D0A43B");

	public string plugIn_Name => "SendInvoiceViaEmail";

	public string plugIn_Description => "Plugin για την αποστολή των παραστατικών του protel μέσω email.Πρέπει να υπάρχει εγκατεστημένος ο εκτυπωτής τύπου zan για να δημιουργηθούν τα pdf αρχεία";
}
