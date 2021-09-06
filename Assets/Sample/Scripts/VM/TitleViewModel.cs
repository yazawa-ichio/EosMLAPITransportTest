using UVMBinding;

namespace EosMLAPITransports.Sample
{

	public class TitleViewModel : ViewModel
	{
		[Event]
		public System.Action DevLogin { get; set; }
		[Event]
		public System.Action AccountLogin { get; set; }
	}

}