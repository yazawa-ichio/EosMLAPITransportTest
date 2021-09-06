using UVMBinding;

namespace EosMLAPITransports.Sample
{
	public class InGameViewModel : ViewModel
	{
		[Bind]
		public string RoomName { get; set; }
		[Event]
		public System.Action Back { get; set; }
	}
}