using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

[RequireComponent(typeof(Collider2D))]
public class Bounce : MonoBehaviour
{
    [Title("General")]
    [SerializeField] private string _validString;
    [SerializeField] private bool _debug = false;

    [Space]

    [Title("Movement")]
    [SerializeField] private bool _useSetDirection = true;
    [SerializeField, Range(-(float)(Math.PI / 2), (float)(Math.PI / 2)), ShowIf("@_useSetDirection")] private float _angle = 1;
    [SerializeField, ShowIf("@_useSetDirection")] private float _speed;

    [SerializeField, HideIf("@_useSetDirection")] private float _reflectionStrength = 1.3f;

    [SerializeField] private bool _isAdditive;

    [SerializeField] private bool _capSpeed;

    [SerializeField, ShowIf("@_capSpeed")] private float _maxVelocity = 20;



    private void OnCollisionEnter2D(Collision2D collision)
    {
        // If Collision == tag
        if (!collision.gameObject.CompareTag(_validString)) return;

        // Check if the colliding object has a Rigidbody2D
        Rigidbody2D collidingRigidbody = collision.rigidbody;
        if (collidingRigidbody == null) return;

        if (_useSetDirection)
        {
            var x = Mathf.Sin(_angle) * _speed;
            var y = Mathf.Cos(_angle) * _speed;
            if (_isAdditive)
                collidingRigidbody.velocity += new Vector2(x, y);
            else
                collidingRigidbody.velocity = new Vector2(x, y);
        }
        else
        {
            // Find the average normal of the collision points
            Vector2 averageNormal = Vector2.zero;
            foreach (ContactPoint2D contact in collision.contacts)
            {
                Debug.DrawLine(contact.point, contact.point + contact.normal, Color.red, 2.0f);
                averageNormal += contact.normal;
            }
            averageNormal /= collision.contactCount;

            // Reflect the colliding object's velocity off the collision normal
            Vector2 reflectedVelocity = Vector2.Reflect(collidingRigidbody.velocity, averageNormal);
            collidingRigidbody.velocity = reflectedVelocity * _reflectionStrength;
        }

        if (_capSpeed && collidingRigidbody.velocity.magnitude > _maxVelocity)
        {
            collidingRigidbody.velocity = collidingRigidbody.velocity.normalized * _maxVelocity;
        }


    }
    private void OnDrawGizmos()
    {
        if (!_debug) return;

        // Angle
        Gizmos.color = Color.yellow;

        var lineScaler = 5;

        if (_useSetDirection)
        {
            var x = Mathf.Sin(_angle) * _speed / lineScaler;
            var y = Mathf.Cos(_angle) * _speed / lineScaler;

            Gizmos.DrawRay(transform.localPosition, Vector3.right * x); // Horizontal
            Gizmos.DrawRay(transform.localPosition + Vector3.right * x, Vector3.up * y); // Vertical
            Gizmos.DrawRay(transform.localPosition, (Vector3.right * x) + (Vector3.up * y)); // Diagonal
        }
        else
        {
            Gizmos.DrawRay(transform.localPosition, Vector2.up * _reflectionStrength);
        }

    }

}
