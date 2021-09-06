using ILib.EosMilapi;
using System;
using UVMBinding;

namespace EosMLAPITransports.Sample
{
	public class LobbyInfoViewModel : ViewModel
	{
		[Bind]
		public string RoomName { get; set; }
		[Bind]
		public int MaxMembers { get; set; }
		[Bind]
		public int Members { get; set; }
		[Event]
		public Action<int> Join { get; set; }

		public LobbyInfoViewModel() { }

		public LobbyInfoViewModel(LobbyInfo info)
		{
			info.TryGet("ROOMNAME", out string name);
			RoomName = name;
			MaxMembers = info.MaxMembers;
			Members = info.Members;
		}
	}
}