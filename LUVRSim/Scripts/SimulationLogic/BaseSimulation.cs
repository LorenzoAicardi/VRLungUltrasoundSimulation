using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Serialization;

[RequireComponent(typeof(SimulationManager))]
public abstract class BaseSimulation : NetworkBehaviour
{
    [SerializeField] protected string simulationName;
    protected SimulationManager SimulationManager;

    protected virtual void Awake()
    {
        if (SimulationManager == null)
        {
            TryGetComponent(out SimulationManager);
        }
    }

    public virtual void SetupGame(){}
    
    public virtual void StartGame(ulong uid){}
    
    public virtual void UpdateGame(float deltaTime){}
    
    public virtual void FinishGame(){}

    public virtual void SyncUponClientJoin(){}

}
