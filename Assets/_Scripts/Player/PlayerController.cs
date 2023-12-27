using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Sirenix.OdinInspector;
using UnityEditor;

public class PlayerController : MonoBehaviour
{

    #region Internal Variables
    private Rigidbody2D _rb;

    private RaycastHit2D[] _ground = new RaycastHit2D[5];
    private RaycastHit2D[] _left = new RaycastHit2D[5];
    private RaycastHit2D[] _right = new RaycastHit2D[5];

    private Vector2 _velocity = Vector2.zero;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
    }
    #endregion

    #region Input 

    private float _inputX = 0f;
    private float _inputY = 0f;
    private bool _jumpWasPressed = false;
    private bool _jumpHeld = false;
    private bool _grabHeld = false;

    private void HandleInput()
    {
        _inputX = Input.GetAxisRaw("Horizontal");
        _inputY = Input.GetAxisRaw("Vertical");
        _jumpWasPressed = Input.GetButtonDown("Jump") ? true : _jumpWasPressed;
        _jumpHeld = Input.GetButton("Jump");
        _grabHeld = Input.GetButton("Grab");

        // Reset Timer When Pressed
        if (Input.GetButtonDown("Jump"))
        {
            _bufferTimer = 0;
        }

        // Add Time
        _bufferTimer += Time.deltaTime;

        // Check Timer
        if (_bufferTimer >= _jumpBufferTime)
        {
            _jumpWasPressed = false;
        }

        _velocity = _rb.velocity;
    }
    
    #endregion


    #region Horizontal

    [Title("Horizontal")]
    [SerializeField] private float _airAcceleration = 40f;
    [SerializeField] private float _groundAcceleration = 55f;
    [SerializeField] private float _airDeceleration = 5f;
    [SerializeField] private float _groundDeceleration = 5f;
    [SerializeField] private float _maxVelocity = 5f;
    [SerializeField] private float _minVelocity = 0.25f;

    [Space]
    [SerializeField, LabelText("Can Move: "), LabelWidth(130)] private bool _canHorizontal = false;
    [SerializeField, LabelText("Debug: "), LabelWidth(130)] private bool _horizontalDebug = false;

    private void HandleHorizontal()
    {
        // If no input, and velocity is low, round it to 0+
        if (Mathf.Abs(_velocity.x) < _minVelocity && _inputX == 0)
            _velocity.x = 0f;

        // Decelerate ------
        // If going too fast, or not pressing anything, slow down
        if (Mathf.Abs(_velocity.x) >= _maxVelocity || _inputX == 0)
            _velocity.x = Mathf.Lerp(_velocity.x, 0, (_touchingGround ? _groundDeceleration : _airDeceleration ) * Time.fixedDeltaTime);

        if (!_canHorizontal) return;

        // If Player Presses Move and Not On Wall
        if (_inputX != 0 && !_onWall)
        {
            // Implement Apex Modifiers
            doApex = _velocity.y > -_apexTolerence && _velocity.y < _apexTolerence && !_onWall && !_touchingGround;
            var apexMultipler = (doApex ? 1 : _apexMultipler);
            var maxSpeed = _maxVelocity * apexMultipler;

            // If the movement isnt too fast
            var nextFrameMovement = _inputX * (_touchingGround ? _groundAcceleration : _airAcceleration) * Time.fixedDeltaTime * apexMultipler;
            if (Mathf.Abs(_velocity.x + nextFrameMovement) <= maxSpeed)
            {
                // Then Accelerate
                _velocity.x += nextFrameMovement;
            } 
        }
    }

    #endregion

    #region Vertical

    [Title("Jump")]
    [SerializeField] private float _jumpHeight = 2f;
    [SerializeField] private float _releaseEarlyMultiplier = 5f;
    [SerializeField] private int _numberOfJumps = 2;
    [SerializeField] private float _jumpBufferTime = 0.5f;
    [SerializeField] private float _coyoteTime = 0.2f;
    [SerializeField] private float _apexMultipler = 1f; // Implemented in Horizontal
    [SerializeField] private float _apexTolerence = 1f; // Implemented in Horizontal

    [Space]
    [SerializeField, LabelText("Can Jump: "), LabelWidth(130)] private bool _canJump = false;
    [SerializeField, LabelText("Debug: "), LabelWidth(130)] private bool _jumpDebug = false;
    private int _jumpsLeft = 0;
    private float _bufferTimer = 0;
    private bool doApex = false;

    [Title("Vertical")]
    [SerializeField] private float _fallingSpeed = 2f;
    [SerializeField] private float _maxFallSpeed = 7f;

    [Space]
    [SerializeField, LabelText("Can Fall: "), LabelWidth(130)] private bool _canVertical = false;
    [SerializeField, LabelText("Debug: "), LabelWidth(130)] private bool _verticalDebug = false;

    private void HandleVertical()
    {
        if (_touchingGround)
        {
            Recover();
            //_velocity.y = 0f;
        }

        // Jumping
        // -----------------------

        // If you Can jump, and you press the button, Jump
        if ((_jumpsLeft > 0 && _jumpWasPressed && (_touchingGroundBuffered || _numberOfJumps > 1)) && _canJump)
        {
            if (_velocity.y < 0)
                _velocity.y = 0f;
            _velocity.y += _jumpHeight;

            _jumpWasPressed = false;
            _jumpsLeft--;
        }

        // Falling
        // -----------------------

        if (!_canVertical) return;

        // If onw all, lerp to 0 Y Velo
        if (_onWall)
        {
            // If Speed is downwards, slide a bit
            if (_velocity.y < 0)
                _velocity.y = Mathf.Lerp(_velocity.y, 0, _wallGrabDownwardDeceleration * Time.fixedDeltaTime);
            // Otherwise, slide less
            else
                _velocity.y = Mathf.Lerp(_velocity.y, 0, _wallGrabUpwardDeceleration * Time.fixedDeltaTime);
        }

        else if (!_touchingGround)
        {

            // If Falling to fast, slow down to the max fall speed
            if (_velocity.y <= -_maxFallSpeed)
                _velocity.y = Mathf.Lerp(_velocity.y, _maxFallSpeed, _fallingSpeed * Time.fixedDeltaTime);

            // If the movement isnt too fast
            var nextFrameMovement = _fallingSpeed * Time.fixedDeltaTime;
            if (_velocity.y - nextFrameMovement >= -_maxFallSpeed)
            {

                // If going upwards, and holding space
                if (_velocity.y > 0 && (_jumpHeld))
                {
                    // Accelerate
                    _velocity.y -= nextFrameMovement;
                }

                else
                {
                    // Accelerate Faster
                    if (_velocity.y - nextFrameMovement * _releaseEarlyMultiplier >= -_maxFallSpeed)
                        _velocity.y -= nextFrameMovement * _releaseEarlyMultiplier;
                }
            }
        }
    }

    #endregion

    #region Walls

    [Title("Walls")]
    [SerializeField] private float _wallStamina = 40f;

    [Space]
    [SerializeField, LabelText("Can Wall: "), LabelWidth(130)] private bool _canWall = false;
    [SerializeField, LabelText("Debug: "), LabelWidth(130)] private bool _wallDebug = false;
    private float _wallStaminaValue = 0;


    [Title("Wall Jump")]
    [SerializeField] private float _wallJumpForce = 5f;
    [SerializeField, Range(0, (float)(Math.PI / 2))] private float _wallJumpAngle = 45f;
    [SerializeField] private float _wallInputWeight = 3f;

    [Space]
    [SerializeField, LabelText("Can Wall Jump: "), LabelWidth(130)] private bool _canWallJump = false;
    [SerializeField, LabelText("Debug: "), LabelWidth(130)] private bool _wallJumpDebug = false;


    [Title("Wall Climb")]
    [SerializeField] private float _wallGrabDownwardDeceleration = 10f;
    [SerializeField] private float _wallGrabUpwardDeceleration = 20f;
    [SerializeField] private float _wallClimbStaminaUsageMultiplier = 1.5f;
    [SerializeField] private float _wallClimbSpeed = 190f;
    [SerializeField] private float _wallSlideSpeed = 300f;
    [SerializeField] private float _wallTopNudge = 2f;

    [Space]
    [SerializeField, LabelText("Can Wall Climb: "), LabelWidth(130)] private bool _canWallClimb = false;
    [SerializeField, LabelText("Debug: "), LabelWidth(130)] private bool _wallClimbDebug = false;

    private bool _onWall = false;

    private void HandleWall()
    {
        if (!_canWall) return;

        // Wall Grab
        // ------------------------------
        // If touching a wall, and grab is held
        _onWall = ((_touchingRight || _touchingLeft) && _grabHeld) && _canWallClimb && _wallStaminaValue > 0;


        if (_onWall) {
            _wallStaminaValue -= Time.fixedDeltaTime * 
                (_inputY != 1 ? 
                1 : _wallClimbStaminaUsageMultiplier);

            if (_inputY > 0)
            {
                _velocity.y = _wallClimbSpeed * Time.fixedDeltaTime;

                // Nudge Player over the edge if at the top
                if (_touchingTopRight)
                    _velocity.x += _wallTopNudge;

                else if (_touchingTopLeft)
                    _velocity.x -= _wallTopNudge;
            }
            else if (_inputY < 0)
                _velocity.y = - _wallSlideSpeed * Time.fixedDeltaTime;

        }


        // Wall Jumping 
        // ------------------------------

        // If not touching ground, and you press jump, and your touching a wall
        if ((!_touchingGround && _jumpWasPressed && (_touchingLeft || _touchingRight)) && _canWallJump)
        {
            _velocity.y = 0;
            _jumpWasPressed = false; // Reset Jump Buffer

            int flip = _touchingRight ? -1 : 1;

            var velocity = new Vector2(Mathf.Sin(flip * _wallJumpAngle), Mathf.Cos(flip * _wallJumpAngle));

            // If moving in the same direction as the jump, go further
            if (_inputX == flip)
                velocity.x += _wallInputWeight;

            // If not holding input, go higher, not further
            else if (_inputX == 0 && !_grabHeld)
                velocity.x -= _wallInputWeight;


            _velocity += velocity * _wallJumpForce;
        }
    }

    #endregion

    #region Collisions

    [Title("Collisions")]
    [SerializeField] private float _groundRaycastLength = 1.0f;
    [SerializeField] private float _wallRaycastLength = 1.0f;
    [SerializeField] private LayerMask raycastMask;
    private bool _touchingGround = false;
    private bool _touchingGroundBuffered = false;
    private bool _touchingLeft = false;
    private bool _touchingRight = false;
    private bool _touchingTopLeft = false;
    private bool _touchingTopRight = false;

    private void HandleCollisions()
    {
        _ground[0] = Physics2D.Raycast(transform.localPosition - (Vector3.left / 3), Vector2.down, _groundRaycastLength, raycastMask);
        _ground[1] = Physics2D.Raycast(transform.localPosition - (Vector3.left / 5), Vector2.down, _groundRaycastLength, raycastMask);
        _ground[2] = Physics2D.Raycast(transform.localPosition                     , Vector2.down, _groundRaycastLength, raycastMask);
        _ground[3] = Physics2D.Raycast(transform.localPosition - (Vector3.right / 5), Vector2.down, _groundRaycastLength, raycastMask);
        _ground[4] = Physics2D.Raycast(transform.localPosition - (Vector3.right / 3), Vector2.down, _groundRaycastLength, raycastMask);
    
        // Is touching ground?
        _touchingGround = Array.Exists(_ground, value => value.collider != null);

        _left[0] = Physics2D.Raycast(transform.localPosition - (Vector3.up / 2), Vector2.left, _wallRaycastLength, raycastMask);
        _left[1] = Physics2D.Raycast(transform.localPosition - (Vector3.up / 5), Vector2.left, _wallRaycastLength, raycastMask);
        _left[2] = Physics2D.Raycast(transform.localPosition                     , Vector2.left, _wallRaycastLength, raycastMask);
        _left[3] = Physics2D.Raycast(transform.localPosition - (Vector3.down / 5), Vector2.left, _wallRaycastLength, raycastMask);
        _left[4] = Physics2D.Raycast(transform.localPosition - (Vector3.down / 3), Vector2.left, _wallRaycastLength, raycastMask);

        // Is touching Left Wall?
        _touchingLeft = Array.Exists(_left, value => value.collider != null);
        _touchingTopLeft = _touchingLeft && _left[4].collider == null && _left[1].collider == null;

        _right[0] = Physics2D.Raycast(transform.localPosition - (Vector3.up / 2), Vector2.right, _wallRaycastLength, raycastMask);
        _right[1] = Physics2D.Raycast(transform.localPosition - (Vector3.up / 5), Vector2.right, _wallRaycastLength, raycastMask);
        _right[2] = Physics2D.Raycast(transform.localPosition                   , Vector2.right, _wallRaycastLength, raycastMask);
        _right[3] = Physics2D.Raycast(transform.localPosition - (Vector3.down / 5), Vector2.right, _wallRaycastLength, raycastMask);
        _right[4] = Physics2D.Raycast(transform.localPosition - (Vector3.down / 3), Vector2.right, _wallRaycastLength, raycastMask);

        // Is touching Right Wall?
        _touchingRight = Array.Exists(_right, value => value.collider != null);
        _touchingTopRight = _touchingRight && _right[4].collider == null && _right[1].collider == null;


        // Coyote Time
        if (_touchingGround)
        {
            _touchingGroundBuffered = true;
            CancelInvoke("SetGroundDelayed");
        }

        else
            Invoke("SetGroundDelayed", _coyoteTime); 

    }

    private void SetGroundDelayed()
    {
        _touchingGroundBuffered = false;
    }

    #endregion


    #region General

    public void Recover()
    {
        _jumpsLeft = _numberOfJumps;
        _wallStaminaValue = _wallStamina;
    }

    #endregion

    #region Updates

    private void UpdatePlayer()
    {
        _rb.velocity = _velocity;
    }

    private void Update()
    {
        HandleInput();
    }

    private void FixedUpdate()
    {
        HandleCollisions();

        HandleHorizontal();
        HandleVertical();
        HandleWall();

        UpdatePlayer();
    }

    #endregion

    private void OnDrawGizmos()
    {

        // Horizontal
        if (_horizontalDebug)
        {
            Handles.color = Color.white;
            Handles.Label(transform.position + Vector3.right, "X: " + _velocity.x.ToString("N2"));

        }

        // Vertical
        if (_verticalDebug)
        {
            // Ground
            Gizmos.color = _touchingGround ? Color.green : 
                _touchingGroundBuffered ? Color.yellow : Color.red;

            Gizmos.DrawRay(transform.localPosition - (Vector3.left / 3), Vector2.down * _groundRaycastLength);
            Gizmos.DrawRay(transform.localPosition - (Vector3.left / 5), Vector2.down * _groundRaycastLength);
            Gizmos.DrawRay(transform.localPosition                      , Vector2.down * _groundRaycastLength);
            Gizmos.DrawRay(transform.localPosition - (Vector3.right / 5), Vector2.down * _groundRaycastLength);
            Gizmos.DrawRay(transform.localPosition - (Vector3.right / 3), Vector2.down * _groundRaycastLength);

            Gizmos.color = doApex ? Color.cyan : Color.red;
            Gizmos.DrawCube(transform.localPosition + Vector3.up * 1.7f, Vector3.one / 3);

            Handles.color = Color.white;
            Handles.Label(transform.position + Vector3.left / 2 + Vector3.down, "Y: " + _velocity.y.ToString("N2"));
        }

        // Wall
        if (_wallDebug)
        {
            // Left Wall
            Gizmos.color = _touchingLeft ? Color.green : Color.red;

            Gizmos.DrawRay(transform.localPosition - (Vector3.up / 2), Vector2.left * _wallRaycastLength);
            Gizmos.DrawRay(transform.localPosition - (Vector3.up / 5), Vector2.left * _wallRaycastLength);
            Gizmos.DrawRay(transform.localPosition                      , Vector2.left * _wallRaycastLength);
            Gizmos.DrawRay(transform.localPosition - (Vector3.down / 5), Vector2.left * _wallRaycastLength);
            Gizmos.DrawRay(transform.localPosition - (Vector3.down / 3), Vector2.left * _wallRaycastLength);

            // Right Wall
            Gizmos.color = _touchingRight ? Color.green : Color.red;

            Gizmos.DrawRay(transform.localPosition - (Vector3.up / 2), Vector2.right * _wallRaycastLength);
            Gizmos.DrawRay(transform.localPosition - (Vector3.up / 5), Vector2.right * _wallRaycastLength);
            Gizmos.DrawRay(transform.localPosition                      , Vector2.left * _wallRaycastLength);
            Gizmos.DrawRay(transform.localPosition - (Vector3.down / 5), Vector2.right * _wallRaycastLength);
            Gizmos.DrawRay(transform.localPosition - (Vector3.down / 3), Vector2.right * _wallRaycastLength);
        }
        
        if (_wallJumpDebug)
        {
            // Angle
            Gizmos.color = Color.yellow;

            int flip = _touchingRight ? -1 : 1;
            var lineScaler = 3;

            var velocity = new Vector2(Mathf.Sin(flip * _wallJumpAngle), Mathf.Cos(flip * _wallJumpAngle));

            velocity = velocity * _wallJumpForce / lineScaler;

            Gizmos.DrawRay(transform.localPosition,                                 Vector3.right * velocity.x);                                // Horizontal
            Gizmos.DrawRay(transform.localPosition + Vector3.right * velocity.x,    Vector3.up * velocity.y);                                   // Vertical
            Gizmos.DrawRay(transform.localPosition,                                 (Vector3.right * velocity.x) + (Vector3.up * velocity.y));  // Diagonal

            // Weighted
            Gizmos.color = Color.white;
            var withInputWeight = velocity;
            withInputWeight.x += (1) * _wallInputWeight;

            Gizmos.DrawRay(transform.localPosition,                                 Vector3.right * withInputWeight.x);                                 // Horizontal
            Gizmos.DrawRay(transform.localPosition + Vector3.right * withInputWeight.x,    Vector3.up * withInputWeight.y);                                    // Vertical
            Gizmos.DrawRay(transform.localPosition,                                 (Vector3.right * withInputWeight.x) + (Vector3.up * withInputWeight.y));   // Diagonal

            // Neutral
            var minusInputWeight = velocity;
            minusInputWeight.x += (-1) * _wallInputWeight;

            Gizmos.DrawRay(transform.localPosition, Vector3.right * minusInputWeight.x);                                 // Horizontal
            Gizmos.DrawRay(transform.localPosition + Vector3.right * minusInputWeight.x, Vector3.up * minusInputWeight.y);                                    // Vertical
            Gizmos.DrawRay(transform.localPosition, (Vector3.right * minusInputWeight.x) + (Vector3.up * minusInputWeight.y));   // Diagonal

        }

        if (_wallClimbDebug)
        {
            if (_wallStamina == _wallStaminaValue)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawCube(transform.localPosition + Vector3.up, Vector3.right + Vector3.up / 3);
            }
            else if (_wallStaminaValue > 0)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawCube(transform.localPosition + Vector3.up, Vector3.right * (_wallStaminaValue / _wallStamina) + Vector3.up / 3);
            }
            else
            {
                Gizmos.color = Color.red;
                Gizmos.DrawCube(transform.localPosition + Vector3.up, Vector3.right + Vector3.up / 3);
            }
        }

        // Jump
        if (_jumpDebug)
        {
            if (_jumpWasPressed)
                Gizmos.color = Color.green;
            else if (_jumpHeld)
                Gizmos.color = Color.blue;
            else
                Gizmos.color = Color.red;

            Gizmos.DrawSphere(transform.localPosition, 0.25f);
        }
    }
}
