using ILib.EosMilapi;
using System;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace EosMLAPITransports.Sample
{
	public class LobbySelectState : StateBase
	{
		SimpleLobbyClient m_LobbyClient;

		public override void Run(object prm)
		{
			m_LobbyClient = new SimpleLobbyClient();
			UIStack.Switch("UILobbySelect", new LobbySelectViewModel()
			{
				Create = OnCreate,
				Input = OnInput,
				Search = OnSearch,
			});
		}

		void OnDestroy()
		{
			m_LobbyClient?.Dispose();
		}

		async void OnCreate()
		{
			try
			{
				await m_LobbyClient.Create(4);
				var clinet = m_LobbyClient;
				m_LobbyClient = null;
				Switch<InGameState>(clinet);
			}
			catch (Exception ex)
			{
				Debug.LogException(ex);
			}
		}

		async void OnSearch()
		{
			try
			{
				var vm = new LobbySearchViewModel
				{
					OnUpdate = OnUpdate,
					Back = () => UIStack.Pop()
				};
				await UIStack.Push("UILobbySearch", vm);
				vm.Lobbies.AddRange(await OnUpdate());
			}
			catch (Exception ex)
			{
				Debug.LogException(ex);
			}
		}

		async Task<LobbyInfoViewModel[]> OnUpdate()
		{
			var ret = await m_LobbyClient.SearchDefault(10);
			return ret.Select(x => new LobbyInfoViewModel(x)
			{
				Join = index => JoinLobby(ret[index])
			}).ToArray();
		}

		async void JoinLobby(LobbyInfo info)
		{
			try
			{
				await m_LobbyClient.Join(info.LobbyId);
				var clinet = m_LobbyClient;
				m_LobbyClient = null;
				Switch<InGameState>(clinet);
			}
			catch (Exception ex)
			{
				Debug.LogException(ex);
			}
		}

		void OnInput()
		{
			UIStack.Push("UILobbyRoomJoin", new LobbyRoomJoinViewModel()
			{
				Join = OnJoinByRoomName,
				Back = () => UIStack.Pop()
			});
		}

		async void OnJoinByRoomName(string roomName)
		{
			try
			{
				for (int i = 0; i < 3; i++)
				{
					var ret = await m_LobbyClient.SearchRoomName(10, roomName);
					if (ret.Length > 0)
					{
						JoinLobby(ret[0]);
						return;
					}
				}
			}
			catch (Exception ex)
			{
				Debug.LogException(ex);
			}
		}

	}
}

