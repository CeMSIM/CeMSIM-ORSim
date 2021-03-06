﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using CEMSIM.Network;

namespace CEMSIM
{
    namespace GameLogic
    {
        public class GameManager : MonoBehaviour
        {
            //TO DO: Create proper instance
            public static GameManager instance;

            // store all information about all players in game
            public static Dictionary<int, PlayerManager> players = new Dictionary<int, PlayerManager>();

            public GameObject localPlayerVR;
            public GameObject localPlayerPrefab;
            public GameObject playerPrefab;

            private void Awake()
            {
                if (instance == null)
                {
                    instance = this;
                }
                else if (instance != this)
                {
                    // We only allow one instance of this class to exist.
                    // Destroy current instance if different from the current one.
                    Debug.Log("Another instance already exists. Destroy this one.");
                    Destroy(this);
                }
            }

            /// <summary>Spawns a player, not necessarily the player controlled by the current user.</summary>
            /// <param name="_id">The player's ID.</param>
            /// <param name="_name">The player's name.</param>
            /// <param name="_position">The player's starting position.</param>
            /// <param name="_rotation">The player's starting rotation.</param>
            public void SpawnPlayer(int _id, string _username, Vector3 _position, Quaternion _rotation)
            {
                GameObject _player;


                if (_id == ClientInstance.instance.myId)
                {
                    if (localPlayerVR.activeInHierarchy)
                    {
                        _player = localPlayerVR;
                        _player.GetComponent<PlayerVRController>().enabled = true;
                    }
                    else
                    {
                        // create player for client
                        _player = Instantiate(localPlayerPrefab, _position, _rotation);
                    }
                }
                else
                {
                    // create player for another client
                    _player = Instantiate(playerPrefab, _position, _rotation);
                }

                _player.GetComponent<PlayerManager>().id = _id;
                _player.GetComponent<PlayerManager>().username = _username;

                // record the player instance in the players dictionary
                players.Add(_id, _player.GetComponent<PlayerManager>());
            }
        }
    }
}
