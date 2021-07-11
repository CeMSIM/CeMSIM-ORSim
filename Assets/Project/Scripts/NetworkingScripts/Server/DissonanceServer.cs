using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Dissonance.Networking;
using CEMSIM.Network;
using JetBrains.Annotations;
using System;

public class DissonanceServer : BaseServer<DissonanceServer, DissonanceClient, int>
{
    #region field and properties
    private readonly DissonanceCommsNetwork _network;
    #endregion

    #region constructor
    public DissonanceServer([NotNull] DissonanceCommsNetwork network)
    {
        _network = network;
    }
    #endregion

    protected override void ReadMessages()
    {
        // Messages are received in an event handler, so we don't need to do any work to read events
    }

    protected override void SendReliable(int connection, ArraySegment<byte> packet)
    {
        // Messages are received in an event handler, so we don't need to do any work to read events
    }

    protected override void SendUnreliable(int connection, ArraySegment<byte> packet)
    {
        // Messages are received in an event handler, so we don't need to do any work to read events
    }
    public void ReachClientDisconnected(ServerClient connection)
    {
        ClientDisconnected(connection.id);
    }
}
