using System;
using System.Net;
using System.Net.Sockets;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CEMSIM
{
    namespace Network
    {
        public class ServerInstance
        {
            public static int maxPlayers { get; private set; }///> maximum number of clients the server can handle simultaneously
            public static int port { get; private set; }
            private static TcpListener tcpListener;
            private static UdpClient udpListener;
            public static DissonanceCommsNetwork dissonanceCommsNetwork;

            public static Dictionary<int, ServerClient> clients = new Dictionary<int, ServerClient>(); ///> a dictionary storing clients and their ids.

            public delegate void PacketHandler(int _fromClient, Packet _packet);
            public static Dictionary<int, PacketHandler> packetHandlers;

            public static void Start(int _maxPlayers, int _port)
            {
                maxPlayers = _maxPlayers;
                port = _port;

                Debug.Log($"Server initializing...");
                NetworkOverlayMenu.Instance.Log($"Server initializing...");

                InitializeServerData();

                // initialize the dictionary that maps packet id to packet description
                PacketId.InitPacketIdDictionary();

                // initialize tcpListener
                tcpListener = new TcpListener(IPAddress.Any, port);
                tcpListener.Start();

                // start to accept asynchronized connection req with a callback 
                tcpListener.BeginAcceptTcpClient(new AsyncCallback(TCPConnectCallback), null);

                udpListener = new UdpClient(port);
                udpListener.BeginReceive(UDPReceiveCallback, null);

                // start dissonance server
                dissonanceCommsNetwork = new DissonanceCommsNetwork();
                dissonanceCommsNetwork.DissonanceRunAsDedicatedServer();

                Debug.Log($"Server is listening on port {_port}");
                NetworkOverlayMenu.Instance.Log($"Server is listening on port {_port}");
            }

            /// <summary>
            /// Callback function called once a client request for a TCP connection
            /// </summary>
            /// <param name="_result"></param>
            private static void TCPConnectCallback(IAsyncResult _result)
            {
                // it's an async method, so we use EndAcceptTcpClient rather than AcceptTcpClient
                TcpClient _tcpClient = tcpListener.EndAcceptTcpClient(_result);

                // call the function itself in preparation for the next client 
                tcpListener.BeginAcceptTcpClient(new AsyncCallback(TCPConnectCallback), null);

                Debug.Log($"Connection request from ip:{_tcpClient.Client.RemoteEndPoint}");
                NetworkOverlayMenu.Instance.Log($"Connection request from ip:{_tcpClient.Client.RemoteEndPoint}");


                // find which position is available to the user.
                for (int i = 1; i <= maxPlayers; i++)
                {
                    if (clients[i].tcp.socket == null)
                    {
                        // if we find one service slot that is empty,
                        // the server let the client to take up the position
                        clients[i].tcp.connect(_tcpClient);
                        return;
                    }
                }

                // reach here means all positions are occupied
                Debug.LogWarning($"{_tcpClient.Client.RemoteEndPoint} failed to connect. Server fully occupied");
                NetworkOverlayMenu.Instance.Log($"Warning: {_tcpClient.Client.RemoteEndPoint} failed to connect. Server fully occupied");
            }

            /// <summary>
            /// Callback function for UDP
            /// </summary>
            /// <param name="_result"></param>
            private static void UDPReceiveCallback(IAsyncResult _result)
            {
                try
                {
                    IPEndPoint _clientEndPoint = new IPEndPoint(IPAddress.Any, 0);
                    byte[] _data = udpListener.EndReceive(_result, ref _clientEndPoint);

                    udpListener.BeginReceive(UDPReceiveCallback, null);

                    /// Here, we discard the packet if its packet length is smaller than
                    /// the 4-byte packet length (int32). However, UDP is less reliable
                    /// than TCP, it's possible that the network lost the first half
                    /// of the packet and we received the second half. For UDP, we are expected
                    /// to accept lossing packets. And we may need to verify the packet intactivity
                    /// ourselves.
                    if (_data.Length < 4)
                    {
                        return;
                    }

                    using (Packet _packet = new Packet(_data))
                    {
                        // This line read the client id embedded in the packet.
                        // There may be a security issue here, but currently, it's our plan.
                        int _clientId = _packet.ReadInt32();
                        //Debug.Log($"Received a UDP packet from client id claimed as {_clientId}");
                        if (_clientId == 0 || _clientId >= maxPlayers) // invalid client id.
                        {
                            Debug.LogWarning("Invalid user id");
                            NetworkOverlayMenu.Instance.Log("Warning: Invalid user id");
                            return;
                        }

                        // check whether this is the first communication
                        if (clients[_clientId].udp.endPoint == null)
                        {
                            // build up the connection
                            clients[_clientId].udp.Connect(_clientEndPoint);
                            return;
                        }

                        // compare whether the packet is sent from a client we know
                        if (clients[_clientId].udp.endPoint.ToString() == _clientEndPoint.ToString())
                        {
                            clients[_clientId].udp.HandleData(_packet);
                        }
                    }
                }
                catch (Exception _e)
                {
                    Debug.LogWarning($"UDPReceiveCallback malfunctioning with exception {_e}");
                    NetworkOverlayMenu.Instance.Log($"Warning: UDPReceiveCallback malfunctioning with exception {_e}");
                }
            }

            public static void SendUDPData(IPEndPoint _clientEndPoint, Packet _packet)
            {
                try
                {
                    udpListener.BeginSend(_packet.ToArray(), _packet.Length(), _clientEndPoint, null, null);
                }
                catch (Exception _e)
                {
                    Debug.LogWarning($"Cannot send UDP packet. Exception {_e}");
                    NetworkOverlayMenu.Instance.Log($"Warning: Cannot send UDP packet. Exception {_e}");
                }
            }

            private static void InitializeServerData()
            {
                for (int i = 1; i <= maxPlayers; i++)
                {
                    clients.Add(i, new ServerClient(i));
                }


                packetHandlers = new Dictionary<int, PacketHandler>()
                {
                    { (int)ClientPackets.invalidPacket, ServerHandle.InvalidPacketResponse},
                    { (int)ClientPackets.welcome, ServerHandle.Welcome},
                    { (int)ClientPackets.welcomeReceived, ServerHandle.WelcomeReceived },
                    { (int)ClientPackets.pingTCP, ServerHandle.PingTCP},
                    { (int)ClientPackets.pingUDP, ServerHandle.PingUDP},
                    { (int)ClientPackets.spawnRequest, ServerHandle.SpawnRequest},
                    { (int)ClientPackets.playerDesktopMovement, ServerHandle.PlayerDesktopMovement},
                    { (int)ClientPackets.playerVRMovement, ServerHandle.PlayerVRMovement},
                    { (int)ClientPackets.heartBeatDetectionTCP, ServerHandle.HeartBeatDetectionTCP},
                    { (int)ClientPackets.heartBeatDetectionUDP, ServerHandle.HeartBeatDetectionUDP},
                    { (int)ClientPackets.itemPositionUDP, ServerHandle.ItemPosition},
                    { (int)ClientPackets.itemOwnershipChange, ServerHandle.ItemOwnershipChange},
                    { (int)ClientPackets.environmentState, ServerHandle.EnvironmentState},
                };

                Debug.Log("Initialized Server Data");
                NetworkOverlayMenu.Instance.Log("Initialized Server Data");
            }

            /// <summary>
            /// Properly stop the server, especially release network port occupation. 
            /// </summary>
            public static void Stop()
            {
                tcpListener.Stop();
                udpListener.Close();
                dissonanceCommsNetwork.DissonanceStop();
            }

        }
    }
}