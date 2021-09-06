using UnityEngine;
using UVMBinding;

namespace EosMLAPITransports.Sample
{
	public class DevLoginViewModel : ViewModel
	{
		[Bind]
		public string Host { get; set; } = "localhost:8888";

		[Bind]
		public string UserName { get; set; }

		public System.Action<DevLoginViewModel> OnLogin { get; set; }

		[Event]
		void Login()
		{
			if (string.IsNullOrEmpty(Host))
			{
				Debug.LogError("Not Input Host");
				return;
			}
			if (string.IsNullOrEmpty(UserName))
			{
				Debug.LogError("Not Input UserName");
				return;
			}
			OnLogin?.Invoke(this);
		}
	}
}