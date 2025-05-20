namespace HitServicesCore.Models.IS_Services;

public class DBColumnModel
{
	public int Position { get; set; }

	public string ColumnName { get; set; }

	public string DataType { get; set; }

	public int MaxLength { get; set; }

	public double Precision { get; set; }

	public double Scale { get; set; }

	public bool PrimaryKey { get; set; }

	public bool Nullable { get; set; }

	public bool AutoIncrement { get; set; }
}
