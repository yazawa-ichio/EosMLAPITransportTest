using Epic.OnlineServices;
using Epic.OnlineServices.Lobby;
using PlayEveryWare.EpicOnlineServices;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace ILib.EosMilapi
{

	public static class LobbyInterfaceExtension
	{
		class RemoveHandle : IDisposable
		{
			ulong m_Id;
			Action<ulong> m_Remove;

			public RemoveHandle(ulong id, Action<ulong> remove)
			{
				m_Id = id;
				m_Remove = remove;
			}

			~RemoveHandle()
			{
				Dispose();
			}

			public void Dispose()
			{
				if (EOSManager.Instance.GetEOSPlatformInterface() == null)
				{
					return;
				}
				m_Remove(m_Id);
				System.GC.SuppressFinalize(this);
			}
		}

		public static IDisposable NotifyLobbyMemberStatusReceived(this LobbyInterface self, Action<string, ProductUserId, LobbyMemberStatus> action)
		{
			var id = self.AddNotifyLobbyMemberStatusReceived(new AddNotifyLobbyMemberStatusReceivedOptions(), null, (x) =>
			{
				action(x.LobbyId, x.TargetUserId, x.CurrentStatus);
			});
			return new RemoveHandle(id, (handle) =>
			{
				EOSManager.Instance.GetEOSLobbyInterface().RemoveNotifyJoinLobbyAccepted(handle);
			});
		}

		public static IDisposable NotifyLobbyInviteReceived(this LobbyInterface self)
		{
			var id = self.AddNotifyLobbyInviteReceived(new AddNotifyLobbyInviteReceivedOptions(), null, (x) =>
			{
				CopyLobbyDetailsHandleByInviteIdOptions options = new CopyLobbyDetailsHandleByInviteIdOptions();
				options.InviteId = x.InviteId;
				var result = EOSManager.Instance.GetEOSLobbyInterface().CopyLobbyDetailsHandleByInviteId(options, out LobbyDetails outLobbyDetailsHandle);
				if (result != Result.Success)
				{
					Debug.LogErrorFormat("Lobbies (OnLobbyInvite): could not get lobby details: error code: {0}", result);
					return;
				}
				if (outLobbyDetailsHandle == null)
				{
					Debug.LogError("Lobbies (OnLobbyInvite): could not get lobby details: null details handle.");
					return;
				}
				Debug.LogError("AddNotifyLobbyInviteReceived");

			});
			return new RemoveHandle(id, (handle) =>
			{
				EOSManager.Instance.GetEOSLobbyInterface().RemoveNotifyLobbyInviteReceived(handle);
			});
		}

		public static IDisposable NotifyLobbyUpdateReceived(this LobbyInterface self, Action<string, LobbyDetails> action)
		{
			var id = self.AddNotifyLobbyUpdateReceived(new AddNotifyLobbyUpdateReceivedOptions(), null, (x) =>
			{
				var ret = self.CopyLobbyDetailsHandle(new CopyLobbyDetailsHandleOptions
				{
					LobbyId = x.LobbyId,
					LocalUserId = EOSManager.Instance.GetProductUserId()
				}, out var handle);
				if (ret == Result.Success)
				{
					action(x.LobbyId, handle);
				}
			});
			return new RemoveHandle(id, (handle) =>
			{
				EOSManager.Instance.GetEOSLobbyInterface().RemoveNotifyLobbyUpdateReceived(handle);
			});
		}

		public static Task<CreateLobbyCallbackInfo> CreateLobby(this LobbyInterface self, CreateLobbyOptions options, object clientData = null)
		{
			var future = new TaskCompletionSource<CreateLobbyCallbackInfo>();
			self.CreateLobby(options, clientData, (x) =>
			{
				if (x == null || x.ResultCode != Result.Success)
				{
					future.TrySetException(new Exception($"Create Lobby Fail ResultCode:{x?.ResultCode ?? Result.InvalidState}"));
				}
				else
				{
					future.TrySetResult(x);
				}
			});
			return future.Task;
		}

		public static Task<UpdateLobbyCallbackInfo> UpdateLobby(this LobbyInterface self, string lobbyId, Action<LobbyModification> modification, object clientData = null)
		{
			var future = new TaskCompletionSource<UpdateLobbyCallbackInfo>();
			UpdateLobbyModificationOptions options = new UpdateLobbyModificationOptions();
			options.LobbyId = lobbyId;
			options.LocalUserId = EOSManager.Instance.GetProductUserId();
			var result = self.UpdateLobbyModification(options, out LobbyModification handle);
			if (result != Result.Success)
			{
				future.TrySetException(new Exception($"UpdateLobbyModification Lobby Fail ResultCode:{result}"));
				return future.Task;
			}
			modification?.Invoke(handle);
			self.UpdateLobby(new UpdateLobbyOptions
			{
				LobbyModificationHandle = handle,
			}, clientData, (x) =>
			{
				if (x == null || x.ResultCode != Result.Success)
				{
					future.TrySetException(new Exception($"Update Lobby Fail ResultCode:{x?.ResultCode ?? Result.InvalidState}"));
				}
				else
				{
					future.TrySetResult(x);
				}
			});
			return future.Task;
		}

		public static void AddAttribute(this LobbyModification self, string key, string value, LobbyAttributeVisibility visibility = LobbyAttributeVisibility.Public)
		{
			self.AddAttribute(new LobbyModificationAddAttributeOptions
			{
				Attribute = new AttributeData
				{
					Key = key,
					Value = value
				},
				Visibility = visibility,
			});
		}

		public static void AddAttribute(this LobbyModification self, Dictionary<string, string> dic, LobbyAttributeVisibility visibility = LobbyAttributeVisibility.Public)
		{
			foreach (var kvp in dic)
			{
				self.AddAttribute(new LobbyModificationAddAttributeOptions
				{
					Attribute = new AttributeData
					{
						Key = kvp.Key,
						Value = kvp.Value
					},
					Visibility = visibility,
				});
			}
		}
		public static Task<LobbyDetails[]> Search(this LobbyInterface self, uint maxResults, Action<LobbySearch> action)
		{
			var future = new TaskCompletionSource<LobbyDetails[]>();
			var result = self.CreateLobbySearch(new CreateLobbySearchOptions
			{
				MaxResults = maxResults,
			}, out var handle);
			if (result != Result.Success)
			{
				future.TrySetException(new Exception($"CreateLobbySearch Fail {result}"));
				return future.Task;
			}
			action?.Invoke(handle);
			handle.Find(new LobbySearchFindOptions
			{
				LocalUserId = EOSManager.Instance.GetProductUserId()
			}, null, (x) =>
			{
				if (x == null || x.ResultCode != Result.Success)
				{
					future.TrySetException(new Exception($"LobbySearch Find Fail ResultCode:{x?.ResultCode ?? Result.InvalidState}"));
					return;
				}
				var list = new List<LobbyDetails>();
				var count = handle.GetSearchResultCount(new LobbySearchGetSearchResultCountOptions { });
				for (uint i = 0; i < count; i++)
				{
					var ret = handle.CopySearchResultByIndex(new LobbySearchCopySearchResultByIndexOptions { LobbyIndex = i }, out var details);
					if (ret == Result.Success)
					{
						list.Add(details);
					}
				}
				future.TrySetResult(list.ToArray());
				handle.Release();
			});
			return future.Task;
		}

	}
}