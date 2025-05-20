using System.Collections.Generic;
using HitHelpersNetCore.Models;

namespace HitServicesCore.Models.Helpers;

public class DescriptorsListModel
{
	public string Section { get; set; }

	public List<DescriptorsModel> Properties { get; set; }
}
