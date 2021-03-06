﻿using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEngine;

using CEMSIM.GameLogic;

namespace CEMSIM
{
    namespace Network
    {
        /// <summary>
        /// This class process packets received from the server
        /// </summary>
        public class ClientHandle : MonoBehaviour
        {

            public static void Welcome(Packet _packet)
            {
                string _msg = _packet.ReadString();
                int _myid = _packet.ReadInt32();

                Debug.Log($"Message from Server:{_msg} My Id:{_myid}");

                // set the client id based on received packet
                ClientInstance.instance.myId = _myid;

                // send response
                ClientSend.WelcomeReceived();

                // connect udp 
                ClientInstance.instance.udp.Connect(((IPEndPoint)ClientInstance.instance.tcp.socket.Client.LocalEndPoint).Port);

                // Mark TCP ready-to-use
                ClientInstance.instance.tcp.isTCPConnected = true;
                ClientInstance.instance.CheckConnection();
            }

            /// <summary>
            /// Handle the TCP ping response from the server.
            /// </summary>
            /// <param name="_packet"></param>
            public static void TCPPingResponse(Packet _packet)
            {
                string _msg = _packet.ReadString();

                ClientPCConnetMenu.Instance.UpdateServerMessage("TCP:" + _msg);

            }

            /// <summary>
            /// Handle the UDP ping response from the server
            /// </summary>
            /// <param name="_packet"></param>
            public static void UDPPingResponse(Packet _packet)
            {
                string _msg = _packet.ReadString();

                ClientPCConnetMenu.Instance.UpdateServerMessage("UDP:" + _msg);
            }

            /// <summary>
            /// Handle the server's instruction of spawning a player
            /// </summary>
            /// <param name="_packet"></param>
            public static void SpawnPlayer(Packet _packet)
            {
                int _id = _packet.ReadInt32();
                string _username = _packet.ReadString();
                Vector3 _position = _packet.ReadVector3();
                Quaternion _rotation = _packet.ReadQuaternion();

                Debug.Log($"Spawn Player {_id} at {_position}");
                // spawn the player
                GameManager.instance.SpawnPlayer(_id, _username, _position, _rotation);

            }

            /// <summary>
            /// Update a player's position.
            /// </summary>
            /// <param name="_packet"></param>
            public static void PlayerPosition(Packet _packet)
            {
                int _id = _packet.ReadInt32();
                Vector3 _position = _packet.ReadVector3();


                //Debug.Log($"Player {_id} position to {_position}");

                // update corresponding player's position
                if (GameManager.players.ContainsKey(_id))
                {
                    GameManager.players[_id].transform.position = _position;
                }
                else
                {
                    Debug.LogWarning($"Player {_id} has not been created yet");
                }

            }

            /// <summary>
            /// Update a player's rotation.
            /// </summary>
            /// <param name="_packet"></param>
            public static void PlayerRotation(Packet _packet)
            {
                int _id = _packet.ReadInt32();
                Quaternion _rotation = _packet.ReadQuaternion();

                Debug.Log($"Player {_id} rotation to {_rotation}");

                // update corresponding player's position
                if (GameManager.players.ContainsKey(_id))
                {
                    GameManager.players[_id].transform.rotation = _rotation;
                }
                else
                {
                    Debug.LogWarning($"Player {_id} has not been created yet");
                }
            }

            /// <summary>
            /// Despawn (destroy) a player instructed by the packet.
            /// This process is tentative because it depends whether we need to store
            /// the player's history record.
            /// </summary>
            /// <param name="_packet"></param>
            public static void PlayerDisconnected(Packet _packet)
            {
                int _id = _packet.ReadInt32();

                // destroy the player's gameObject from the scene
                Destroy(GameManager.players[_id].gameObject);
                // remove the value in the player dictionary
                GameManager.players.Remove(_id);
            }
        }
    }
}
