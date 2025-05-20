using System.Text.Encodings.Web;
using System.Text.Json;
using HitServicesCore.Helpers.JsonConverters;

namespace HitServicesCore.Helpers;

public class JsonOptionsHelper
{
	public JsonSerializerOptions GetOptions()
	{
		JsonSerializerOptions retVal = new JsonSerializerOptions();
		retVal.Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping;
		retVal.WriteIndented = true;
		retVal.Converters.Insert(0, new AutoStringToInt64Converter());
		retVal.Converters.Insert(1, new AutoStringToInt32Converter());
		return retVal;
	}
}
