using ILib.EosMilapi;
using MLAPI;

namespace EosMLAPITransports.Sample
{

	public class InGameState : StateBase
	{
		SimpleLobbyClient m_Client;

		public override void Run(object prm)
		{
			m_Client = (SimpleLobbyClient)prm;
			UIStack.Switch("UIInGame", new InGameViewModel
			{
				RoomName = m_Client.RoomName,
				Back = OnBack,
			});
			FindObjectOfType<EosMlapiTransport>().HostId = m_Client.OwnerId.ToString();
			if (m_Client.IsOwner)
			{
				NetworkManager.Singleton.StartHost(new UnityEngine.Vector3(UnityEngine.Random.value, 0, UnityEngine.Random.value));
			}
			else
			{
				NetworkManager.Singleton.StartClient();
			}
			NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnectCallback;
		}

		private void OnClientDisconnectCallback(ulong id)
		{
			if (id == 0)
			{
				OnBack();
			}
		}

		void OnBack()
		{
			NetworkManager.Singleton.Shutdown();
			Switch<LobbySelectState>();
		}

		void OnDestroy()
		{
			m_Client.Dispose();
		}

	}

}