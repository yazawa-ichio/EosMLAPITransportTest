using System;
using UVMBinding;

namespace EosMLAPITransports.Sample
{
	public class LobbySelectViewModel : ViewModel
	{
		[Event]
		public Action Create { get; set; }
		[Event]
		public Action Search { get; set; }
		[Event]
		public Action Input { get; set; }

	}

}