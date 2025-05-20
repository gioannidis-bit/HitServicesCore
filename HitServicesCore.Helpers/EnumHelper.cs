using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using HitServicesCore.Models;

namespace HitServicesCore.Helpers;

public class EnumHelper
{
	public static List<EnumModel> GetEnumList(System.Enum enumT)
	{
		List<EnumModel> items = new List<EnumModel>();
		foreach (object? item in System.Enum.GetValues(enumT.GetType()))
		{
			EnumModel itm = new EnumModel
			{
				Value = (int)item,
				Name = System.Enum.GetName(enumT.GetType(), item)
			};
			items.Add(itm);
		}
		return items;
	}

	public static List<EnumModel> GetEnumListFriendly(System.Enum enumT)
	{
		List<EnumModel> items = new List<EnumModel>();
		Type genericEnumType = enumT.GetType();
		foreach (object? item in System.Enum.GetValues(enumT.GetType()))
		{
			MemberInfo[] memberInfo = genericEnumType.GetMember(item.ToString());
			if (memberInfo != null && memberInfo.Length != 0)
			{
				object[] _Attribs = memberInfo[0].GetCustomAttributes(typeof(DescriptionAttribute), inherit: false);
				if (_Attribs != null && _Attribs.Count() > 0)
				{
					EnumModel itm = new EnumModel
					{
						Value = (int)item,
						Name = ((DescriptionAttribute)_Attribs.ElementAt(0)).Description
					};
					items.Add(itm);
				}
			}
		}
		return items;
	}

	public static Dictionary<int, string> ToDictionary(System.Enum enumT)
	{
		Type type = enumT.GetType();
		return System.Enum.GetValues(type).Cast<int>().ToDictionary((int e) => e, (int e) => System.Enum.GetName(type, e));
	}
}
