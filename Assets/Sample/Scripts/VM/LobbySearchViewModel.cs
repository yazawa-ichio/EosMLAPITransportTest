using System;
using System.Threading.Tasks;
using UVMBinding;

namespace EosMLAPITransports.Sample
{
	public class LobbySearchViewModel : ViewModel
	{


		[Bind]
		public Collection<LobbyInfoViewModel> Lobbies { get; private set; }

		[Event]
		public Action Back { get; set; }

		public Func<Task<LobbyInfoViewModel[]>> OnUpdate { get; set; }


		[Event]
		async void Update()
		{
			try
			{
				Lobbies.Clear();
				var infos = await OnUpdate();
				Lobbies.AddRange(infos);
			}
			catch { }
		}

	}
}