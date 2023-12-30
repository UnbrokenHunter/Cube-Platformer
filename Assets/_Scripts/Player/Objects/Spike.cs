using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class Spike : MonoBehaviour
{
    [Title("General")]
    [SerializeField] private string _validString;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // If Collision == tag
        if (!collision.gameObject.CompareTag(_validString)) return;

        var player = collision.gameObject.GetComponent<Player.PlayerController>();
        if (player == null) return;

        player.Die();
    }

}
