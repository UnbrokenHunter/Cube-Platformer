using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

public class Simulation : MonoBehaviour
{

    [SerializeField, OnValueChanged("UpdateSimulationSpeed"), Range(0, 10)] private float _simulationSpeed = 1f;

    private void UpdateSimulationSpeed()
    {
        Time.timeScale = _simulationSpeed;
    }

}
