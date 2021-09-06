using Epic.OnlineServices;
using Epic.OnlineServices.P2P;
using MLAPI.Transports;
using MLAPI.Transports.Tasks;
using PlayEveryWare.EpicOnlineServices;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace ILib.EosMilapi
{

	public class EosMlapiTransport : NetworkTransport
	{
		const byte k_HandshakeChannel = byte.MaxValue - 1;
		const string k_HandshakeMessage = "EosMlapi";

		public override ulong ServerClientId => 0;

		public string HostId { get; set; }

		public ProductUserId LocalId => EOSManager.Instance.GetProductUserId();

		P2PInterface Handle => EOSManager.Instance.GetEOSP2PInterface();

		SocketId m_SocketId = new SocketId
		{
			SocketName = "Transport"
		};

		SocketTask m_ClientTask;
		ulong m_IdCounter = 0;
		Dictionary<string, ulong> m_PidToId = new Dictionary<string, ulong>();
		Dictionary<ulong, ProductUserId> m_IdToPid = new Dictionary<ulong, ProductUserId>();
		Dictionary<NetworkChannel, PacketReliability> m_Channel = new Dictionary<NetworkChannel, PacketReliability>();
		List<IDisposable> m_Disposables = new List<IDisposable>();
		Queue<(ulong, NetworkEvent)> m_Event = new Queue<(ulong, NetworkEvent)>();

		public override void DisconnectLocalClient()
		{
			Shutdown();
		}

		public override void DisconnectRemoteClient(ulong clientId)
		{
			if (m_IdToPid.TryGetValue(clientId, out var id))
			{
				Handle.CloseConnection(new CloseConnectionOptions
				{
					LocalUserId = LocalId,
					RemoteUserId = id,
					SocketId = m_SocketId
				});
			}
		}

		public override ulong GetCurrentRtt(ulong clientId)
		{
			return 0;
		}

		public override void Init()
		{
			for (byte i = 0; i < MLAPI_CHANNELS.Length; i++)
			{
				var info = MLAPI_CHANNELS[i];
				PacketReliability reliability = PacketReliability.ReliableOrdered;
				switch (info.Delivery)
				{
					case NetworkDelivery.Unreliable:
						reliability = PacketReliability.UnreliableUnordered;
						break;
					case NetworkDelivery.UnreliableSequenced:
						reliability = PacketReliability.ReliableOrdered;
						break;
					case NetworkDelivery.Reliable:
						reliability = PacketReliability.ReliableUnordered;
						break;
					case NetworkDelivery.ReliableSequenced:
						reliability = PacketReliability.ReliableOrdered;
						break;
					case NetworkDelivery.ReliableFragmentedSequenced:
						reliability = PacketReliability.ReliableOrdered;
						break;
				}
				m_Channel[info.Channel] = reliability;
			}
			m_Disposables.Add(Handle.NotifyPeerConnectionClosed(m_SocketId, (_, id) =>
			{
				if (m_PidToId.TryGetValue(id.ToString(), out var i))
				{
					m_Event.Enqueue((i, NetworkEvent.Disconnect));
					m_IdToPid.Remove(i);
					m_PidToId.Remove(id.ToString());
				}
			}));
			m_Disposables.Add(Handle.NotifyPeerConnectionRequest(m_SocketId, (_, _id) =>
			{
				var id = _id.ToString();
				if (m_PidToId.TryGetValue(id, out var i))
				{
					m_IdToPid.Remove(i);
					m_Event.Enqueue((i, NetworkEvent.Disconnect));
				}
				if (id != HostId)
				{
					m_IdCounter++;
					m_PidToId[id] = i = m_IdCounter;
					m_IdToPid[i] = _id;
					m_Event.Enqueue((i, NetworkEvent.Connect));
				}
				else
				{
					m_PidToId[id] = i = 0;
					m_IdToPid[i] = _id;
				}
				return true;
			}));
		}

		public override NetworkEvent PollEvent(out ulong clientId, out NetworkChannel networkChannel, out ArraySegment<byte> payload, out float receiveTime)
		{

			clientId = default;
			payload = default;
			receiveTime = Time.time;
			networkChannel = NetworkChannel.Internal;
			if (TryCheckConnectionEvent(out var e, ref clientId))
			{
				return e;
			}
			var ret = Handle.ReceivePacket(new ReceivePacketOptions
			{
				LocalUserId = LocalId,
				MaxDataSizeBytes = 1024 * 4,
			}, out var userId, out var socketId, out var channel, out var data);
			networkChannel = (NetworkChannel)channel;
			if (socketId.SocketName != m_SocketId.SocketName || ret != Result.Success)
			{
				return NetworkEvent.Nothing;
			}
			if (k_HandshakeChannel == channel)
			{
				return ProcessHandshake(data, userId, ref clientId);
			}
			payload = new ArraySegment<byte>(data);
			m_PidToId.TryGetValue(userId.ToString(), out clientId);
			return NetworkEvent.Data;
		}


		bool TryCheckConnectionEvent(out NetworkEvent e, ref ulong clientId)
		{
			e = NetworkEvent.Nothing;
			clientId = 0;
			if (m_Event.Count > 0)
			{
				var i = m_Event.Dequeue();
				clientId = i.Item1;
				e = i.Item2;
				return true;
			}
			return false;
		}

		NetworkEvent ProcessHandshake(byte[] data, ProductUserId userId, ref ulong clientId)
		{
			if (System.Text.Encoding.UTF8.GetString(data) != k_HandshakeMessage)
			{
				return NetworkEvent.Nothing;
			}
			if (HostId == LocalId.ToString())
			{
				Handle.SendPacket(new SendPacketOptions
				{
					SocketId = m_SocketId,
					LocalUserId = LocalId,
					RemoteUserId = userId,
					Data = data,
					Channel = byte.MaxValue - 1,
					AllowDelayedDelivery = true,
					Reliability = PacketReliability.ReliableOrdered,
				});
				return NetworkEvent.Nothing;
			}
			else
			{
				m_IdToPid[0] = userId;
				m_PidToId[userId.ToString()] = 0;
				clientId = 0;
				m_ClientTask.Success = true;
				m_ClientTask.IsDone = true;
				return NetworkEvent.Connect;
			}
		}

		public override void Send(ulong clientId, ArraySegment<byte> data, NetworkChannel networkChannel)
		{
			if (m_IdToPid.TryGetValue(clientId, out var id))
			{
				if (!m_Channel.TryGetValue(networkChannel, out var reliability))
				{
					reliability = PacketReliability.ReliableOrdered;
				}
				var buf = new byte[data.Count];
				Buffer.BlockCopy(data.Array, data.Offset, buf, 0, buf.Length);
				Handle.SendPacket(new SendPacketOptions
				{
					SocketId = m_SocketId,
					LocalUserId = LocalId,
					RemoteUserId = id,
					Data = buf,
					Channel = (byte)networkChannel,
					AllowDelayedDelivery = true,
					Reliability = reliability,
				});
			}
		}

		public override void Shutdown()
		{
			if (EOSManager.Instance.GetEOSPlatformInterface() == null)
			{
				return;
			}
			Handle.CloseConnections(new CloseConnectionsOptions
			{
				LocalUserId = LocalId,
				SocketId = m_SocketId,
			});
			foreach (var d in m_Disposables)
			{
				d.Dispose();
			}
			m_Disposables.Clear();
			m_Event.Clear();
			m_PidToId.Clear();
			m_IdToPid.Clear();
			m_IdCounter = 0;
			m_ClientTask = null;
		}


		public override SocketTasks StartClient()
		{
			SocketTask task = m_ClientTask = SocketTask.Working;
			Handle.SendPacket(new SendPacketOptions
			{
				SocketId = m_SocketId,
				LocalUserId = LocalId,
				RemoteUserId = ProductUserId.FromString(HostId),
				Data = System.Text.Encoding.UTF8.GetBytes(k_HandshakeMessage),
				Channel = k_HandshakeChannel,
				AllowDelayedDelivery = true,
				Reliability = PacketReliability.ReliableOrdered,
			});
			return task.AsTasks();
		}

		public override SocketTasks StartServer()
		{
			SocketTask task = SocketTask.Done;
			HostId = LocalId.ToString();
			m_IdToPid[0] = LocalId;
			m_PidToId[LocalId.ToString()] = 0;
			return task.AsTasks();
		}
	}

}