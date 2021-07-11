using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Dissonance;
using Dissonance.Networking;
using JetBrains.Annotations;

public class DissonanceCommsNetwork : BaseCommsNetwork<DissonanceServer, DissonanceClient, int, Unit, Unit>
{
    protected override DissonanceClient CreateClient([CanBeNull] Unit connectionParameters)
    {
        return new DissonanceClient(this);
    }

    protected override DissonanceServer CreateServer([CanBeNull] Unit connectionParameters)
    {
        return new DissonanceServer(this);
    }
    protected override void Initialize()
    {
        //Sanity check the channel configuration set in the inspector
        //... << Removed code for simplicity of example >>

        // HLAPI requires a message handler for every type code.
        // Register one which just discards packets while Dissonance
        // is _not_ running.
        //NetworkServer.RegisterHandler(TypeCode, NullMessageReceivedHandler);

        // TODO: Our own network initialization work for this

        base.Initialize();
    }
    public void DissonanceRunAsClient()
    {
        RunAsClient(Unit.None);
    }
    public void DissonanceRunAsDedicatedServer()
    {
        RunAsDedicatedServer(Unit.None);
    }
    public void DissonanceStop()
    {
        Stop();
    }
    public DissonanceServer GetDissonanceServer()
    {
        return Server;
    }
}
