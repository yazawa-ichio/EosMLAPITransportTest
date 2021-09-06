using Epic.OnlineServices;
using Epic.OnlineServices.Lobby;
using PlayEveryWare.EpicOnlineServices;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ILib.EosMilapi
{

	public class SimpleLobbyClient : IDisposable
	{
		public const string DefaultBucketId = "default";

		LobbyInterface LobbyInterface => EOSManager.Instance.GetEOSLobbyInterface();
		List<IDisposable> m_Disposables = new List<IDisposable>();

		public bool IsJoinLobby { get; private set; }

		public bool IsOwner { get; private set; }

		public ProductUserId OwnerId { get; private set; }

		public string LobbyId { get; private set; }

		public string RoomName { get; private set; }

		Dictionary<string, LobbyInfo> m_Search = new Dictionary<string, LobbyInfo>();

		public Dictionary<ProductUserId, LobbyMemberStatus> Members = new Dictionary<ProductUserId, LobbyMemberStatus>();

		public event Action<ProductUserId, LobbyMemberStatus> OnChangeMemberStatus;

		public SimpleLobbyClient()
		{
			m_Disposables.Add(LobbyInterface.NotifyLobbyMemberStatusReceived(OnMemberStatusReceived));
			m_Disposables.Add(LobbyInterface.NotifyLobbyUpdateReceived(OnLobbyUpdateReceive));
			m_Disposables.Add(LobbyInterface.NotifyLobbyInviteReceived());

		}

		public void Dispose()
		{
			foreach (var d in m_Disposables)
			{
				d.Dispose();
			}
			m_Disposables.Clear();
			Leave();
		}

		public Task Create(uint maxMembers, Action<CreateLobbyOptions> action = null, Dictionary<string, string> attributes = null)
		{
			var roomName = "";
			for (int i = 0; i < 6; i++)
			{
				roomName += UnityEngine.Random.Range(0, 9);
			}
			return Create(maxMembers, roomName, action, attributes);
		}

		public async Task Create(uint maxMembers, string roomName, Action<CreateLobbyOptions> action = null, Dictionary<string, string> attributes = null)
		{
			Leave();
			var future = new TaskCompletionSource<bool>();
			var config = new CreateLobbyOptions()
			{
				LocalUserId = EOSManager.Instance.GetProductUserId(),
				MaxLobbyMembers = maxMembers,
				PermissionLevel = LobbyPermissionLevel.Publicadvertised,
				PresenceEnabled = true,
				BucketId = DefaultBucketId,
			};
			action?.Invoke(config);
			var lobby = await LobbyInterface.CreateLobby(config);
			LobbyId = lobby.LobbyId;
			await LobbyInterface.UpdateLobby(LobbyId, handle =>
			{
				RoomName = roomName;
				handle.AddAttribute(DefaultBucketId.ToUpper(), DefaultBucketId);
				handle.AddAttribute(nameof(RoomName).ToUpper(), roomName);
				if (attributes != null)
				{
					handle.AddAttribute(attributes);
				}
			});
			IsOwner = true;
			IsJoinLobby = true;
			OwnerId = EOSManager.Instance.GetProductUserId();
		}

		public Task<LobbyInfo[]> SearchDefault(uint maxResults)
		{
			return Search(maxResults, (x) =>
			{
				x.SetParameter(new LobbySearchSetParameterOptions
				{
					Parameter = new AttributeData
					{
						Key = DefaultBucketId.ToUpper(),
						Value = DefaultBucketId,
					},
					ComparisonOp = ComparisonOp.Equal,
				});
			});
		}

		public Task<LobbyInfo[]> SearchRoomName(uint maxResults, string roomName)
		{
			return Search(maxResults, (x) =>
			{
				x.SetParameter(new LobbySearchSetParameterOptions
				{
					Parameter = new AttributeData
					{
						Key = nameof(RoomName),
						Value = roomName,
					},
					ComparisonOp = ComparisonOp.Equal,
				});
			});
		}

		public async Task<LobbyInfo[]> Search(uint maxResults, Action<LobbySearch> action)
		{
			var details = await LobbyInterface.Search(maxResults, action);
			var list = new List<LobbyInfo>();
			foreach (var detail in details)
			{
				var ret = detail.CopyInfo(new LobbyDetailsCopyInfoOptions { }, out var info);
				if (ret == Result.Success)
				{
					m_Search[info.LobbyId] = new LobbyInfo(detail);
					list.Add(m_Search[info.LobbyId]);
				}
			}
			return list.ToArray();
		}

		public async Task Join(string lobbyId, bool presenceEnabled = true)
		{
			Leave();
			IsOwner = false;
			if (!m_Search.TryGetValue(lobbyId, out var info))
			{
				throw new Exception($"not found LobbyDetails");
			}
			var future = new TaskCompletionSource<bool>();
			LobbyInterface.JoinLobby(new JoinLobbyOptions
			{
				LocalUserId = EOSManager.Instance.GetProductUserId(),
				LobbyDetailsHandle = info.m_Details,
				PresenceEnabled = presenceEnabled,
			}, null, x =>
			{
				if (x == null || x.ResultCode != Result.Success)
				{
					future.TrySetException(new Exception($"Join Lobby Fail ResultCode:{x?.ResultCode ?? Result.InvalidState}"));
				}
				else
				{
					IsJoinLobby = true;
					future.TrySetResult(true);
				}
			});
			await future.Task;
			info.TryGet(nameof(RoomName).ToUpper(), out string roomName);
			RoomName = roomName;
			OwnerId = info.LobbyOwnerUserId;
			LobbyId = info.LobbyId;
		}

		public void Leave()
		{
			if (!IsJoinLobby)
			{
				return;
			}
			IsJoinLobby = false;
			if (EOSManager.Instance.GetEOSPlatformInterface() == null)
			{
				LobbyId = default;
				return;
			}
			if (!IsOwner)
			{
				LeaveLobbyOptions options = new LeaveLobbyOptions();
				options.LobbyId = LobbyId;
				options.LocalUserId = EOSManager.Instance.GetProductUserId();
				LobbyInterface.LeaveLobby(options, null, x => { });
			}
			else
			{
				DestroyLobbyOptions options = new DestroyLobbyOptions();
				options.LobbyId = LobbyId;
				options.LocalUserId = EOSManager.Instance.GetProductUserId();
				LobbyInterface.DestroyLobby(options, null, x => { });
			}
			LobbyId = default;
		}

		void OnMemberStatusReceived(string lobbyId, ProductUserId id, LobbyMemberStatus status)
		{
			if (lobbyId != LobbyId)
			{
				return;
			}
			Members[id] = status;
			OnChangeMemberStatus?.Invoke(id, status);
		}

		void OnLobbyUpdateReceive(string id, LobbyDetails details)
		{
			m_Search[id] = new LobbyInfo(details);
		}

	}
}