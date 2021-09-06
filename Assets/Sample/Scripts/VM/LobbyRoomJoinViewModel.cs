using UVMBinding;

namespace EosMLAPITransports.Sample
{
	public class LobbyRoomJoinViewModel : ViewModel
	{
		[Bind]
		public string RoomName { get; set; }

		[Event]
		public System.Action Back { get; set; }

		[Event]
		public System.Action<string> Join { get; set; }
	}
}