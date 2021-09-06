using Epic.OnlineServices;
using Epic.OnlineServices.P2P;
using PlayEveryWare.EpicOnlineServices;
using System;

namespace ILib.EosMilapi
{
	public static class P2PInterfaceExtension
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

		public static IDisposable NotifyPeerConnectionRequest(this P2PInterface self, SocketId socketId, Func<SocketId, ProductUserId, bool> func)
		{
			var id = self.AddNotifyPeerConnectionRequest(new AddNotifyPeerConnectionRequestOptions
			{
				SocketId = socketId,
				LocalUserId = EOSManager.Instance.GetProductUserId(),
			}, null, (x) =>
			{
				if (func(x.SocketId, x.RemoteUserId))
				{
					AcceptConnectionOptions options = new AcceptConnectionOptions()
					{
						LocalUserId = EOSManager.Instance.GetProductUserId(),
						RemoteUserId = x.RemoteUserId,
						SocketId = socketId
					};
					EOSManager.Instance.GetEOSP2PInterface().AcceptConnection(options);
				}
			});
			return new RemoveHandle(id, (handle) =>
			{
				EOSManager.Instance.GetEOSP2PInterface().RemoveNotifyPeerConnectionRequest(handle);
			});
		}

		public static IDisposable NotifyPeerConnectionClosed(this P2PInterface self, SocketId socketId, Action<SocketId, ProductUserId> func)
		{
			var id = self.AddNotifyPeerConnectionClosed(new AddNotifyPeerConnectionClosedOptions
			{
				SocketId = socketId,
				LocalUserId = EOSManager.Instance.GetProductUserId(),
			}, null, (x) =>
			{
				func(x.SocketId, x.RemoteUserId);
			});
			return new RemoveHandle(id, (handle) =>
			{
				EOSManager.Instance.GetEOSP2PInterface().RemoveNotifyPeerConnectionClosed(handle);
			});
		}
	}
}