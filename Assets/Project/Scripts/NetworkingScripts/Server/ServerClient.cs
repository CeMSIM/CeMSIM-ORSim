using System;
using System.Net;
using System.Net.Sockets;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using CEMSIM.GameLogic;

namespace CEMSIM
{
    namespace Network
    {
        /// <summary>
        /// This class contains operations how a server can control a client to do.
        /// </summary>
        public class ServerClient
        {
            public int id;
            public TCP tcp;
            public UDP udp;
            public ServerPlayer player;  // the player corresponding to the client machine

            public ServerClient(int _id)
            {
                id = _id;
                tcp = new TCP(id);
                udp = new UDP(id);

            }


            /// <summary>
            /// TCP connection between client-server
            /// </summary>
            public class TCP
            {
                public TcpClient socket;
                public long rtt; // round trip time
                public long lastHeartBeat; // tick for the last heart beat

                private readonly int id;
                private NetworkStream stream;
                private byte[] receiveBuffer;
                private Packet receivedData;
                

                public TCP(int _id)
                {
                    id = _id;
                    rtt = 0;
                    lastHeartBeat = 0;
                }

                /// <summary>
                /// Initialize TCP for the client and set callback function for packet receiver.
                /// </summary>
                /// <param name="_socket"></param>
                public void connect(TcpClient _socket)
                {
                    socket = _socket;
                    socket.ReceiveBufferSize = ServerNetworkConstants.DATA_BUFFER_SIZE;
                    socket.SendBufferSize = ServerNetworkConstants.DATA_BUFFER_SIZE;
                    receivedData = new Packet();

                    stream = socket.GetStream();
                    receiveBuffer = new Byte[ServerNetworkConstants.DATA_BUFFER_SIZE];

                    stream.BeginRead(receiveBuffer, 0, ServerNetworkConstants.DATA_BUFFER_SIZE, ReceiveCallback, null);

                    // send welcome message
                    Debug.Log($"Sending welcome packet to client {id}");
                    NetworkOverlayMenu.Instance.Log($"Sending welcome packet to client {id}");
                    ServerSend.Welcome(id, $"Welcome. You are connected to server. Your client id is {id}");
                }


                public void SendData(Packet _packet)
                {
                    try
                    {
                        if (socket != null)
                        {
                            if (ServerNetworkManager.instance.printNetworkTraffic)
                            {
                                Debug.Log($"[Send] TCP to {id} {PacketId.ServerPacketsInfo[_packet.GetPacketId()]}");
                            }
                            
                            stream.BeginWrite(_packet.ToArray(), 0, _packet.Length(), null, null);
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning($"Error sending packet to client {id} via TCP. Exception {e}");
                        NetworkOverlayMenu.Instance.Log($"Error sending packet to client {id} via TCP. Exception {e}");
                    }
                }

                private void ReceiveCallback(IAsyncResult _result)
                {
                    try
                    {
                        int _byteLength = stream.EndRead(_result);

                        // disconnect if no response
                        if (_byteLength <= 0)
                        {
                            // disconnect
                            ServerInstance.clients[id].Disconnect();
                            ServerInstance.dissonanceCommsNetwork.GetDissonanceServer().ReachClientDisconnected(ServerInstance.clients[id]);
                            return;
                        }

                        // start to process the data
                        byte[] _data = new byte[_byteLength];
                        Array.Copy(receiveBuffer, _data, _byteLength); ///> copy the received data to the temporary data buffer

                        // process data
                        receivedData.Reset(HandleData(_data));

                        // prepare for the next data
                        stream.BeginRead(receiveBuffer, 0, ServerNetworkConstants.DATA_BUFFER_SIZE, ReceiveCallback, null);

                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning($"Server exception {e}");
                        NetworkOverlayMenu.Instance.Log($"Server exception {e}");
                        ServerInstance.clients[id].Disconnect();
                        // disconnect
                    }
                }

                private bool HandleData(byte[] _data)
                {
                    int _packetLength = 0;
                    receivedData.SetBytes(_data); // append _data to packet            

                    // Since the first segment is the data length, an int32 of size 4,
                    // by checking whether the unread length >= 4, we know whether this
                    // packet is a split packet of a big one or a standalone packet.
                    if (receivedData.UnreadLength() >= 4)
                    {
                        _packetLength = receivedData.ReadInt32();
                        if (_packetLength <= 0)
                        {
                            return true;
                        }
                    }

                    // After reading a packet based on the _packetLength, we finish processing
                    // a packet. However, if the received data contains more than one packet,
                    // we need to begin processing the next one. If the number of UnreadLength() is greater
                    // than the current packet's length, there is another packet squeezed in current data payload.
                    // Of course, we don't need to process a zero-data packet.
                    while (_packetLength > 0 && _packetLength <= receivedData.UnreadLength())
                    {
                        byte[] _packetBytes = receivedData.ReadBytes(_packetLength);


                        ServerThreadManager.ExecuteOnMainThread(() =>
                        {
                            // create a packet containing just the data
                            using (Packet _packet = new Packet(_packetBytes))
                            {
                                int _packetId = _packet.DigestClientHeader(); // extract header information

                                if (ServerNetworkManager.instance.printNetworkTraffic)
                                {
                                    Debug.Log($"[Recv] TCP {PacketId.ClientPacketsInfo[_packetId]} from {id}");
                                }
                                //NetworkOverlayMenu.Instance.Log($"Receive a packet with id {_packetId} from client {id}");
                                // call proper handling function based on packet id
                                ServerInstance.packetHandlers[_packetId](id, _packet);
                            }
                        });

                        _packetLength = 0;

                        if (receivedData.UnreadLength() >= 4)
                        {
                            _packetLength = receivedData.ReadInt32();
                            if (_packetLength <= 0)
                            {
                                return true;
                            }
                        }

                    }

                    if (_packetLength <= 1)
                    {
                        return true;
                    }
                    return false;
                }

                /// <summary>
                /// Disconnect TCP socket. Release all TCP related resources.
                /// </summary>
                public void Disconnect()
                {
                    socket.Close();
                    stream = null;
                    receivedData = null;
                    receiveBuffer = null;
                    socket = null;
                    lastHeartBeat = 0;
                }

            }

            /// <summary>
            /// UDP connection between client-server
            /// </summary>
            public class UDP
            {
                public IPEndPoint endPoint;
                public long rtt; // udp rtt
                public long lastHeartBeat; // tick for the last heart beat response

                private int id;

                public UDP(int _id)
                {
                    id = _id;
                    rtt = 0;
                    lastHeartBeat = 0;
                }

                public void Connect(IPEndPoint _endPoint)
                {
                    endPoint = _endPoint;
                }

                public void SendData(Packet _packet)
                {
                    try
                    {
                        if (endPoint != null)
                        {
                            if (ServerNetworkManager.instance.printNetworkTraffic)
                            {
                                Debug.Log($"[Send] UDP to {id} {PacketId.ServerPacketsInfo[_packet.GetPacketId()]}");
                            }
                            ServerInstance.SendUDPData(endPoint, _packet);
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning($"Error sending packet to client {id} via UDP. Exception {e}");
                        NetworkOverlayMenu.Instance.Log($"Error sending packet to client {id} via UDP. Exception {e}");
                    }
                }

                public void HandleData(Packet _packetData)
                {
                    if(_packetData.UnreadLength() < 4)
                    {
                        Debug.LogWarning($"UDP received a packet of size {_packetData.UnreadLength()} <= 4");
                        return;
                    }
                    int _packetLength = _packetData.ReadInt32();
                    byte[] _data = _packetData.ReadBytes(_packetLength);

                    ServerThreadManager.ExecuteOnMainThread(() =>
                    {
                        using (Packet _packet = new Packet(_data))
                        {
                            // extract header information
                            int _packetId = _packet.DigestClientHeader();
                            if (ServerNetworkManager.instance.printNetworkTraffic)
                            {
                                Debug.Log($"[Recv] UDP {PacketId.ClientPacketsInfo[_packetId]} from {id}");
                            }
                            ServerInstance.packetHandlers[_packetId](id, _packet);
                        }
                    });

                }

                // UDP disconnect handling
                public void Disconnect()
                {
                    endPoint = null;
                    lastHeartBeat = 0;
                }

            }

            // Spawn the player 
            public void SendIntoGame(string _playerName, bool _vr)
            {
                Debug.Log($"Send player {id}: {_playerName} into game");
                NetworkOverlayMenu.Instance.Log($"Send player {id}: {_playerName} into game");

                if(_vr)
                    player = ServerNetworkManager.instance.InstantiatePlayerVR();
                else
                    player = ServerNetworkManager.instance.InstantiatePlayerDesktop();
                player.Initialize(id, _playerName);
                player.transform.GetChild(1).gameObject.GetComponent<TextMesh>().text = _playerName; // Child 1 is the username
                //player.GetComponent<TextMesh>().text = _playerName;


                // 1. inform all other players the creation of current player
                foreach (ServerClient _client in ServerInstance.clients.Values)
                {
                    if (_client.player != null)
                    {
                        if (_client.id != id)
                        {
                            ServerSend.SpawnPlayer(id, _client.player);
                        }
                    }
                }

                // 2. inform the current player the existance of other players
                foreach (ServerClient _client in ServerInstance.clients.Values)
                {
                    if (_client.player != null)
                    {
                        ServerSend.SpawnPlayer(_client.id, player);
                    }
                }

                // Send current environment state to newly added user
                ServerNetworkManager.SendCurrentEnvironmentStates(id);


            }

            private void Disconnect()
            {
                Debug.Log($"{tcp.socket.Client.RemoteEndPoint} has disconnected.");
                NetworkOverlayMenu.Instance.Log($"{tcp.socket.Client.RemoteEndPoint} has disconnected.");

                ServerThreadManager.ExecuteOnMainThread(() =>
                {
                    // We have to move them into the thread queue just in case there is no other actions after the player is destroied.
                    UnityEngine.Object.Destroy(player.gameObject); // distroy the associated gameObject attached by Player.cs
                    player = null;

                    // inform all other users the disconnection of this player
                    ServerSend.PlayerDisconnect(id);

                    tcp.Disconnect();
                    udp.Disconnect();
                });
                    
            }

        }
    }
}
