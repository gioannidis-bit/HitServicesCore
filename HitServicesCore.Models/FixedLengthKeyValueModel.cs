namespace HitServicesCore.Models;

public class FixedLengthKeyValueModel
{
	public string ColumnName { get; set; }

	public string ColumnType { get; set; }

	public string ColumnFormat { get; set; }

	public int ColumnLength { get; set; }

	public char Padding { get; set; }
}
