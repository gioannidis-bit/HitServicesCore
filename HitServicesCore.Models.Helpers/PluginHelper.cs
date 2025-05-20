using System;
using System.Collections.Generic;

namespace HitServicesCore.Models.Helpers;

public class PluginHelper
{
	public Guid plugIn_Id { get; set; }

	public string plugIn_Name { get; set; }

	public string plugIn_Description { get; set; }

	public string plugIn_Version { get; set; }

	public Dictionary<string, string> routing { get; set; }
}
