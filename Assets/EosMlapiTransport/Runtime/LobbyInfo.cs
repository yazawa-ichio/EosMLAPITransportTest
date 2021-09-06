using Epic.OnlineServices;
using Epic.OnlineServices.Lobby;
using System.Collections.Generic;
using System.Linq;

namespace ILib.EosMilapi
{
	public class LobbyInfo
	{
		internal LobbyDetails m_Details;
		LobbyDetailsInfo m_Info;
		List<Attribute> m_Attributes = new List<Attribute>();

		public string LobbyId => m_Info.LobbyId;

		public ProductUserId LobbyOwnerUserId => m_Info.LobbyOwnerUserId;

		public int MaxMembers => (int)m_Info.MaxMembers;

		public int Members => (int)(m_Info.MaxMembers - m_Info.AvailableSlots);

		public LobbyDetailsInfo Info => m_Info;

		public IEnumerable<string> Keys => m_Attributes.Select(x => x.Data.Key);

		public LobbyInfo(LobbyDetails details)
		{
			Set(details);
		}

		public bool IsValid() => m_Info != null;

		public void Set(LobbyDetails details)
		{
			m_Details = details;
			details.CopyInfo(new LobbyDetailsCopyInfoOptions { }, out m_Info);
			m_Attributes.Clear();
			var count = details.GetAttributeCount(new LobbyDetailsGetAttributeCountOptions { });
			for (uint i = 0; i < count; i++)
			{
				var ret = details.CopyAttributeByIndex(new LobbyDetailsCopyAttributeByIndexOptions
				{
					AttrIndex = i,
				}, out var attr);
				if (ret == Result.Success)
				{
					m_Attributes.Add(attr);
				}
			}
		}

		public bool TryGet(string key, out string value)
		{
			foreach (var attr in m_Attributes)
			{
				if (attr.Data.Key == key)
				{
					value = attr.Data.Value.AsUtf8;
					return true;
				}
			}
			value = null;
			return false;
		}

		public bool TryGet(string key, out long value)
		{
			foreach (var attr in m_Attributes)
			{
				if (attr.Data.Key == key && attr.Data.Value.AsInt64.HasValue)
				{
					value = attr.Data.Value.AsInt64.Value;
					return true;
				}
			}
			value = default;
			return false;
		}

		public bool TryGet(string key, out double value)
		{
			foreach (var attr in m_Attributes)
			{
				if (attr.Data.Key == key && attr.Data.Value.AsDouble.HasValue)
				{
					value = attr.Data.Value.AsDouble.Value;
					return true;
				}
			}
			value = default;
			return false;
		}

		public bool TryGet(string key, out bool value)
		{
			foreach (var attr in m_Attributes)
			{
				if (attr.Data.Key == key && attr.Data.Value.AsBool.HasValue)
				{
					value = attr.Data.Value.AsBool.Value;
					return true;
				}
			}
			value = default;
			return false;
		}
	}
}