using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Dissonance.Networking;
using System;
using JetBrains.Annotations;
using CEMSIM.Network;

public class DissonanceClient : BaseClient<DissonanceServer, DissonanceClient, int>
{
    #region field and properties
    private readonly DissonanceCommsNetwork _network;
    #endregion

    #region constructor
    public DissonanceClient([NotNull] DissonanceCommsNetwork network)
        : base(network)
    {
        _network = network;
    }
    #endregion

    public override void Connect()
    {
        // Check if ClientInstance is connected to server
        if (ClientInstance.instance.isConnected)
        {
            Connected();
        }
    }

    protected override void ReadMessages()
    {
        // Messages are received in an event handler, so we don't need to do any work to read events
    }

    protected override void SendReliable(ArraySegment<byte> packet)
    {
        // Messages are received in an event handler, so we don't need to do any work to read events
    }

    protected override void SendUnreliable(ArraySegment<byte> packet)
    {
        // Messages are received in an event handler, so we don't need to do any work to read events
    }
}
