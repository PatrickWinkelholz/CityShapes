using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Pulsate : MonoBehaviour
{
    [SerializeField] private float _period = 1.0f;
    [SerializeField] private float _amplitude = 1.0f;
    [SerializeField] private float _shift = 1.0f;
    
    void Update()
    {
        transform.localScale = Vector3.one * (_amplitude * Mathf.Cos(Time.time * _period) + _shift);    
    }
}
