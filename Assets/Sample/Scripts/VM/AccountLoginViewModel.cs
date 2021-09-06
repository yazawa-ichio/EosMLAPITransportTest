using UVMBinding;

namespace EosMLAPITransports.Sample
{
	public class AccountLoginViewModel : ViewModel
	{
		[Bind]
		public string UserName { get; set; }

		[Bind]
		public string Password { get; set; }

		[Event]
		public System.Action Login { get; set; }
	}

}