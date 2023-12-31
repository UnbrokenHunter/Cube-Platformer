using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Sirenix.OdinInspector;
using UnityEditor;
using Unity.VisualScripting;

namespace Player
{
    public class PlayerController : MonoBehaviour
    {

        #region Internal Variables

        [Title("General")]
        [SerializeField] private bool _debugMode = false;

        private PlayerVisuals _visuals;
        private Rigidbody2D _rb;
        private Vector3 _spawnPosition;

        private RaycastHit2D[] _ground = new RaycastHit2D[5];
        private RaycastHit2D[] _left = new RaycastHit2D[5];
        private RaycastHit2D[] _right = new RaycastHit2D[5];

        private Vector2 _velocity = Vector2.zero;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            if (_rb == null)
                Debug.LogError("Rigidbody2D not found on the player");

            _visuals = GetComponentInChildren<PlayerVisuals>();
            if (_visuals == null)
                Debug.LogError("Player Visuals not found on the player");

            _spawnPosition = transform.position;
        }
        #endregion

        #region Input 

        private float _inputX = 0f;
        private float _inputY = 0f;
        private bool _jumpWasPressed = false;
        private bool _jumpHeld = false;
        private bool _grabHeld = false;
        private bool _dashWasPressed = false;

        private void HandleInput()
        {
            _inputX = Input.GetAxisRaw("Horizontal");
            _inputY = Input.GetAxisRaw("Vertical");
            _jumpWasPressed = Input.GetButtonDown("Jump") || _jumpWasPressed;
            _jumpHeld = Input.GetButton("Jump");
            _grabHeld = Input.GetButton("Grab");
            _dashWasPressed = Input.GetButtonDown("Dash") || _dashWasPressed;

            // Jump Buffer ----------
            // Reset Timer When Pressed
            if (Input.GetButtonDown("Jump"))
            {
                _jumpBufferTimer = 0;
            }

            // Add Time
            _jumpBufferTimer += Time.deltaTime;

            // Check Timer
            if (_jumpBufferTimer >= _jumpBufferTime)
            {
                _jumpWasPressed = false;
            }

            // Dash Buffer ----------
            // Reset Timer When Pressed
            if (Input.GetButtonDown("Dash"))
            {
                _dashBufferTimer = 0;
            }

            // Add Time
            _dashBufferTimer += Time.deltaTime;

            // Check Timer
            if (_dashBufferTimer >= _dashBufferTime)
            {
                _dashWasPressed = false;
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
        private float _horizontalControl = 1f;
        private int _facingDirection = 0;

        [Space]
        [SerializeField, LabelText("Can Move: "), LabelWidth(130)] private bool _canHorizontal = false;
        [SerializeField, LabelText("Debug: "), LabelWidth(130)] private bool _horizontalDebug = false;

        private void HandleHorizontal()
        {
            // For Convenience so that this is avalible when i need it
            if (_inputX != 0)
                _facingDirection = _inputX < 0 ? -1 : 1;


            if (_isDashing) return;
            if (_isWaveDashing) return;

            // If no input, and velocity is low, round it to 0+
            if (Mathf.Abs(_velocity.x) < _minVelocity && _inputX == 0)
                _velocity.x = 0f;

            // Decelerate ------
            // If going too fast, or not pressing anything, slow down
            if (Mathf.Abs(_velocity.x) >= _maxVelocity || _inputX == 0)
                _velocity.x = Mathf.Lerp(_velocity.x, 0, (_isGrounded ? _groundDeceleration : _airDeceleration) * Time.fixedDeltaTime);

            if (!_canHorizontal) return;

            // If Player Presses Move and Not On Wall
            if (_inputX != 0 && !_grabbingWall)
            {
                // Implement Apex Modifiers
                doApex = _velocity.y > -_apexTolerence && _velocity.y < _apexTolerence && !_grabbingWall && !_isGrounded;
                var apexMultipler = (doApex ? 1 : _apexMultipler);
                var maxSpeed = _maxVelocity * apexMultipler;

                // If the movement isnt too fast
                var nextFrameMovement = _inputX * (_isGrounded ? _groundAcceleration : _airAcceleration) * Time.fixedDeltaTime * apexMultipler * _horizontalControl;
                if (Mathf.Abs(_velocity.x + nextFrameMovement) <= maxSpeed)
                {
                    // Then Accelerate
                    _velocity.x += nextFrameMovement;
                }
            }

            _horizontalControl = Mathf.Lerp(_horizontalControl, 1, _horizontalControlRegainSpeed * Time.fixedDeltaTime);

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
        private float _jumpBufferTimer = 0;
        private bool doApex = false;

        [Title("Vertical")]
        [SerializeField] private float _fallingSpeed = 2f;
        [SerializeField] private float _maxFallSpeed = 7f;

        [Space]
        [SerializeField, LabelText("Can Fall: "), LabelWidth(130)] private bool _canVertical = false;
        [SerializeField, LabelText("Debug: "), LabelWidth(130)] private bool _verticalDebug = false;

        private void HandleVertical()
        {
            if (_isGrounded)
                Recover();

            // Jumping
            // -----------------------
            HandleJump();

            // Falling
            // -----------------------
            HandleFalling();

        }

        private void HandleJump()
        {
            // If you Can jump, and you press the button, Jump
            if ((_jumpsLeft > 0 && _jumpWasPressed && (_touchingGroundBuffered || _numberOfJumps > 1)) && _canJump)
            {
                if (_velocity.y < 0)
                    _velocity.y = 0f;
                _velocity.y += _jumpHeight;

                _jumpWasPressed = false;
                _jumpsLeft--;
            }

        }

        private void HandleFalling()
        {
            if (!_canVertical) return;
            if (_isDashing) return;

            // If onw all, lerp to 0 Y Velo
            if (_grabbingWall)
            {
                // If Speed is downwards, slide a bit
                if (_velocity.y < 0)
                    _velocity.y = Mathf.Lerp(_velocity.y, 0, _wallGrabDownwardDeceleration * Time.fixedDeltaTime);
                // Otherwise, slide less
                else
                    _velocity.y = Mathf.Lerp(_velocity.y, 0, _wallGrabUpwardDeceleration * Time.fixedDeltaTime);
            }

            // If not on Ground
            else if (!_isGrounded)
            {

                // If Falling to fast, slow down to the max fall speed
                if (_velocity.y <= -_maxFallSpeed)
                    _velocity.y = Mathf.Lerp(_velocity.y, _maxFallSpeed, _fallingSpeed * Time.fixedDeltaTime);

                // If the movement isnt too fast
                var nextFrameMovement = _fallingSpeed * Time.fixedDeltaTime;
                if (_velocity.y - nextFrameMovement >= -_maxFallSpeed)
                {

                    // If going upwards, and holding space
                    // This is for limiting fall speed when holding space
                    if (_velocity.y > 0 && _jumpHeld)
                    {
                        // Accelerate
                        _velocity.y -= nextFrameMovement;
                    }

                    else
                    {
                        // Accelerate Faster
                        // Accelerate Faster when space is not held
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
        [SerializeField] private float _wallJumpVerticalBonus = 5f;
        [SerializeField, Range(0, (float)(Math.PI / 2))] private float _wallJumpAngle = 45f;
        [SerializeField, Range(0f, 10f)] private float _horizontalControlRegainSpeed = 1f;
        [Space]

        [SerializeField] private float _wallClimbJumpForce = 5f;
        [SerializeField] private float _wallClimbJumpStaminaCost = 1f;
        [SerializeField] private float _wallClimbJumpCooldown = 0.5f;
        private float _wallClimbJumpTimer = 0;

        [Space]

        [SerializeField, LabelText("Can Wall Jump: "), LabelWidth(130)] private bool _canWallJump = false;
        [SerializeField, LabelText("Debug: "), LabelWidth(130)] private bool _wallJumpDebug = false;


        [Title("Wall Climb")]
        [SerializeField] private float _wallGrabDownwardDeceleration = 10f;
        [SerializeField] private float _wallGrabUpwardDeceleration = 20f;
        [SerializeField] private float _wallClimbStaminaUsageMultiplier = 1.5f;
        [SerializeField] private float _wallClimbSpeed = 190f;
        [SerializeField] private float _wallGrabSlideSpeed = 200f;
        [SerializeField] private float _wallSlideSpeed = 300f;
        [SerializeField] private float _wallTopNudge = 2f;


        [Space]
        [SerializeField, LabelText("Can Wall Climb: "), LabelWidth(130)] private bool _canWallClimb = false;
        [SerializeField, LabelText("Debug: "), LabelWidth(130)] private bool _wallClimbDebug = false;

        private bool _grabbingWall = false;

        private void HandleWall()
        {
            if (!_canWall) return;

            // Check for Wall Grab
            // Conditions: Player must be touching a wall, holding the grab button, allowed to wall climb,
            // have enough stamina, and not be in the wall climb jump cooldown.
            _grabbingWall = ((_touchingRight || _touchingLeft) && _grabHeld) && _canWallClimb &&
                            _wallStaminaValue > 0 && _wallClimbJumpCooldown < _wallClimbJumpTimer && !_isDashing;

            if (_grabbingWall)
            {
                // Trigger visual effect for wall sliding
                _visuals.WallSlideEffect(_touchingLeft ? -1 : 1);

                // Stamina consumption depends on whether the player is moving upwards (consumes more) or not
                float staminaDrain = _inputY != 1 ? 1 : _wallClimbStaminaUsageMultiplier;
                _wallStaminaValue -= Time.fixedDeltaTime * staminaDrain;

                // Apply vertical movement based on player input
                if (_inputY > 0)
                {
                    _velocity.y = _wallClimbSpeed * Time.fixedDeltaTime;
                    // Nudge the player over the edge if they are at the top of the wall
                    _velocity.x += _touchingTopRight ? _wallTopNudge : (_touchingTopLeft ? -_wallTopNudge : 0);
                }
                else if (_inputY < 0)
                {
                    _velocity.y = -_wallGrabSlideSpeed * Time.fixedDeltaTime;
                }
            }

            // Check for wall sliding without grab
            if (IsFalling() && IsPressingAgainstWall() && !_grabbingWall)
            {
                SlowDownFall();
            }


            // Update the wall climb jump timer
            if (_wallClimbJumpCooldown >= _wallClimbJumpTimer)
            {
                _wallClimbJumpTimer += Time.fixedDeltaTime;
            }

            // Early exit if jump input is not pressed
            if (!_jumpWasPressed) return;

            int wallSide = _touchingRight ? -1 : 1; // Determine which side of the wall the player is on

            // Wall Climb Jump: Check if player is grabbing the wall, moving upwards, and not in cooldown
            if (_grabbingWall && _inputY > 0 && _wallClimbJumpCooldown < _wallClimbJumpTimer && _inputX != wallSide)
            {
                ResetJumpBuffer();
                _wallStaminaValue -= _wallClimbJumpStaminaCost;
                _velocity.y += _wallClimbJumpForce;
                _wallClimbJumpTimer = 0;
                _visuals.WallSlideEffect(_touchingLeft ? -1 : 1);
            }
            // Regular Wall Jump: Execute if the player is not touching the ground, presses jump, and is touching a wall
            else if (!_isGrounded && (_touchingLeft || _touchingRight) && _canWallJump)
            {
                PerformWallJump(wallSide);
            }
        }

        private void PerformWallJump(int wallSide)
        {
            _velocity = Vector2.zero;
            ResetJumpBuffer();
            _grabbingWall = false;

            // Calculate jump direction based on the wall jump angle
            Vector2 jumpDirection = new Vector2(Mathf.Sin(wallSide * _wallJumpAngle), Mathf.Cos(wallSide * _wallJumpAngle));
            _velocity += jumpDirection * _wallJumpForce;

            // Disable horizontal control if there is horizontal input during the wall jump
            if (_inputX != 0)
                _horizontalControl = 0;

            // Apply additional vertical force if jumping in the direction of the wall
            if (_inputX == wallSide)
                _velocity.y += _wallJumpVerticalBonus;
        }

        private void SlowDownFall()
        {
            if (!_isDashing)
            {
                _velocity.y = Mathf.Max(_velocity.y, -_wallSlideSpeed);
                _visuals.WallSlideEffect(_touchingLeft ? -1 : 1);
            }
        }


        #endregion

        #region Dash

        [Title("Dash")]
        [SerializeField] private float _dashSpeed = 40f;
        [SerializeField] private float _dashDuration = 0.5f;
        [SerializeField] private int _numberOfDashes = 1;
        [SerializeField] private float _dashBufferTime = 0.1f;
        private bool _isDashing = false;
        private int _dashCount;
        private Vector2 _dashDirection;
        private float _originalGravityScale;
        private float _dashBufferTimer = 0;
        private bool _isWaveDashing = false;

        [Title("Wavedash")]
        [SerializeField] private float _waveDashSpeed = 40f;
        [SerializeField] private float waveDashVerticalBoost = 1f; 

        [Space]
        [SerializeField, LabelText("Can Dash: "), LabelWidth(130)] private bool _canDash = false;
        [SerializeField, LabelText("Debug: "), LabelWidth(130)] private bool _dashDebug = false;

        private void HandleDash()
        {
            if (!_canDash) return;

            // Check for dash input
            if (_dashCount > 0 && _dashWasPressed && !_isDashing)
            {
                StartDash();
            }

            // Continue dash if in dashing state
            if (_isDashing)
            {
                ContinueDash();
            }
        }

        private void StartDash()
        {

            _isDashing = true;
            _dashWasPressed = false;
            _dashCount--;
            _originalGravityScale = _rb.gravityScale;
            _rb.gravityScale = 0; // Ignore gravity during dash
            _dashStartPosition = transform.position; // For Debugging

            // Determine dash direction based on input
            _dashDirection = new Vector2(_inputX, _inputY).normalized;
            if (_dashDirection.magnitude < Mathf.Epsilon)
                _dashDirection = new Vector2(_facingDirection, 0);

            StartCoroutine(DashCoroutine());
        }

        private IEnumerator DashCoroutine()
        {
            float startTime = Time.time;

            while (Time.time < startTime + _dashDuration && _isDashing)
            {
                if (!_isDashing) yield return null;

                ContinueDash();
                yield return null;
            }

            EndDash();
        }

        private void ContinueDash()
        {
            // Allow for Wavedashes
            if (_jumpWasPressed && _isGrounded)
            {
                StopCoroutine(nameof(DashCoroutine));

                WaveDash();
                return;
            }

            _rb.velocity = _dashSpeed * _dashDirection; // Apply constant dash velocity
            _visuals.DashEffectContinual();
        }

        private void EndDash()
        {
            if (_isWaveDashing) return;

            _isDashing = false;
            _rb.gravityScale = _originalGravityScale; // Reset gravity scale

            _rb.velocity = Vector2.zero; // Optionally stop the player's movement after dash
                                             // Additional logic for dash end if needed
        }

        private void WaveDash()
        {
            // Reset dash state
            _isDashing = false;
            _rb.gravityScale = _originalGravityScale; // Reset gravity scale

            _isWaveDashing = true;

            // Apply horizontal and vertical components for the wavedash
            _rb.velocity = new(_waveDashSpeed * _facingDirection, waveDashVerticalBoost);

        }

        #endregion

        #region Collisions

        [Title("Collisions")]
        [SerializeField] private float _groundRaycastLength = 1.0f;
        [SerializeField] private float _wallRaycastLength = 1.0f;
        [SerializeField] private LayerMask raycastMask;
        private bool _isGrounded = false;
        private bool _touchingGroundBuffered = false;
        private bool _touchingLeft = false;
        private bool _touchingRight = false;
        private bool _touchingTopLeft = false;
        private bool _touchingTopRight = false;

        private void HandleCollisions()
        {
            _ground[0] = Physics2D.Raycast(transform.position - (Vector3.left / 3), Vector2.down, _groundRaycastLength, raycastMask);
            _ground[1] = Physics2D.Raycast(transform.position - (Vector3.left / 5), Vector2.down, _groundRaycastLength, raycastMask);
            _ground[2] = Physics2D.Raycast(transform.position, Vector2.down, _groundRaycastLength, raycastMask);
            _ground[3] = Physics2D.Raycast(transform.position - (Vector3.right / 5), Vector2.down, _groundRaycastLength, raycastMask);
            _ground[4] = Physics2D.Raycast(transform.position - (Vector3.right / 3), Vector2.down, _groundRaycastLength, raycastMask);

            // Is touching ground?
            _isGrounded = Array.Exists(_ground, value => value.collider != null);

            _left[0] = Physics2D.Raycast(transform.position - (Vector3.up / 2), Vector2.left, _wallRaycastLength, raycastMask);
            _left[1] = Physics2D.Raycast(transform.position - (Vector3.up / 5), Vector2.left, _wallRaycastLength, raycastMask);
            _left[2] = Physics2D.Raycast(transform.position, Vector2.left, _wallRaycastLength, raycastMask);
            _left[3] = Physics2D.Raycast(transform.position - (Vector3.down / 5), Vector2.left, _wallRaycastLength, raycastMask);
            _left[4] = Physics2D.Raycast(transform.position - (Vector3.down / 3), Vector2.left, _wallRaycastLength, raycastMask);

            // Is touching Left Wall?
            _touchingLeft = Array.Exists(_left, value => value.collider != null);
            _touchingTopLeft = _touchingLeft && _left[4].collider == null && _left[1].collider == null;

            _right[0] = Physics2D.Raycast(transform.position - (Vector3.up / 2), Vector2.right, _wallRaycastLength, raycastMask);
            _right[1] = Physics2D.Raycast(transform.position - (Vector3.up / 5), Vector2.right, _wallRaycastLength, raycastMask);
            _right[2] = Physics2D.Raycast(transform.position, Vector2.right, _wallRaycastLength, raycastMask);
            _right[3] = Physics2D.Raycast(transform.position - (Vector3.down / 5), Vector2.right, _wallRaycastLength, raycastMask);
            _right[4] = Physics2D.Raycast(transform.position - (Vector3.down / 3), Vector2.right, _wallRaycastLength, raycastMask);

            // Is touching Right Wall?
            _touchingRight = Array.Exists(_right, value => value.collider != null);
            _touchingTopRight = _touchingRight && _right[4].collider == null && _right[1].collider == null;


            // Coyote Time
            if (_isGrounded)
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
            _dashCount = _numberOfDashes;
            _wallStaminaValue = _wallStamina;
            _isWaveDashing = false;
        }

        public void Die()
        {
            print("Death");

            _velocity = Vector3.zero;
            _isDashing = false;

            transform.position = _spawnPosition;
        }

        #endregion

        #region Helpers 

        private bool IsFalling()
        {
            return _velocity.y < 0;
        }

        private bool IsPressingAgainstWall()
        {
            bool pressingLeft = _touchingLeft && _inputX < 0;
            bool pressingRight = _touchingRight && _inputX > 0;
            return pressingLeft || pressingRight;
        }

        private void ResetJumpBuffer()
        {
            _jumpWasPressed = false; // Reset the jump buffer
        }

        #endregion

        #region Updates

        private void ApplyMovement()
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
            HandleDash();

            ApplyMovement();
        }

        #endregion

        #region Debug
        private int flip = 1;
        private Vector3 _dashStartPosition = Vector3.zero;
        private void OnDrawGizmos()
        {
            if (!_debugMode) return;

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
                Gizmos.color = _isGrounded ? Color.green :
                    _touchingGroundBuffered ? Color.yellow : Color.red;

                Gizmos.DrawRay(transform.position - (Vector3.left / 3), Vector2.down * _groundRaycastLength);
                Gizmos.DrawRay(transform.position - (Vector3.left / 5), Vector2.down * _groundRaycastLength);
                Gizmos.DrawRay(transform.position, Vector2.down * _groundRaycastLength);
                Gizmos.DrawRay(transform.position - (Vector3.right / 5), Vector2.down * _groundRaycastLength);
                Gizmos.DrawRay(transform.position - (Vector3.right / 3), Vector2.down * _groundRaycastLength);

                Gizmos.color = doApex ? Color.cyan : Color.red;
                Gizmos.DrawCube(transform.position + Vector3.up * 1.7f, Vector3.one / 3);

                Handles.color = Color.white;
                Handles.Label(transform.position + Vector3.left / 2 + Vector3.down, "Y: " + _velocity.y.ToString("N2"));
            }

            // Wall
            if (_wallDebug)
            {
                // Left Wall
                Gizmos.color = _touchingLeft ? Color.green : Color.red;

                Gizmos.DrawRay(transform.position - (Vector3.up / 2), Vector2.left * _wallRaycastLength);
                Gizmos.DrawRay(transform.position - (Vector3.up / 5), Vector2.left * _wallRaycastLength);
                Gizmos.DrawRay(transform.position, Vector2.left * _wallRaycastLength);
                Gizmos.DrawRay(transform.position - (Vector3.down / 5), Vector2.left * _wallRaycastLength);
                Gizmos.DrawRay(transform.position - (Vector3.down / 3), Vector2.left * _wallRaycastLength);

                // Right Wall
                Gizmos.color = _touchingRight ? Color.green : Color.red;

                Gizmos.DrawRay(transform.position - (Vector3.up / 2), Vector2.right * _wallRaycastLength);
                Gizmos.DrawRay(transform.position - (Vector3.up / 5), Vector2.right * _wallRaycastLength);
                Gizmos.DrawRay(transform.position, Vector2.left * _wallRaycastLength);
                Gizmos.DrawRay(transform.position - (Vector3.down / 5), Vector2.right * _wallRaycastLength);
                Gizmos.DrawRay(transform.position - (Vector3.down / 3), Vector2.right * _wallRaycastLength);
            }

            if (_wallJumpDebug)
            {
                // Angle
                Gizmos.color = Color.yellow;
                if (_touchingLeft || _touchingRight)
                    flip = _touchingRight ? -1 : 1;
                else if (_isGrounded && _inputX != 0)
                    flip = _inputX < 0 ? -1 : 1;

                var lineScaler = 3;

                var velocity = new Vector2(Mathf.Sin(flip * _wallJumpAngle), Mathf.Cos(flip * _wallJumpAngle));

                velocity = velocity * _wallJumpForce / lineScaler;

                Gizmos.DrawRay(transform.position, Vector3.right * velocity.x);                                // Horizontal
                Gizmos.DrawRay(transform.position + Vector3.right * velocity.x, Vector3.up * velocity.y);      // Vertical
                Gizmos.DrawRay(transform.position, (Vector3.right * velocity.x) + (Vector3.up * velocity.y));  // Diagonal

            }

            if (_wallClimbDebug)
            {
                if (_wallStamina == _wallStaminaValue)
                {
                    Gizmos.color = Color.green;
                    Gizmos.DrawCube(transform.position + Vector3.up, Vector3.right + Vector3.up / 3);
                }
                else if (_wallStaminaValue > 0)
                {
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawCube(transform.position + Vector3.up, Vector3.right * (_wallStaminaValue / _wallStamina) + Vector3.up / 3);
                }
                else
                {
                    Gizmos.color = Color.red;
                    Gizmos.DrawCube(transform.position + Vector3.up, Vector3.right + Vector3.up / 3);
                }
            }

            if (_dashDebug)
            {
                Gizmos.color = _isDashing ? Color.green : Color.red;

                Gizmos.DrawCube(transform.position + Vector3.up / 1.5f + Vector3.left / 5, 
                    Vector3.right / 3 + Vector3.up / 4);

                Gizmos.color = _numberOfDashes > 0 ? Color.green : Color.red;

                Gizmos.DrawCube(transform.position + Vector3.up / 1.5f + Vector3.right / 5, 
                    Vector3.right / 3 + Vector3.up / 4);

                var dashRayLengthMultiplier = 0.8f;

                // Length Debug
                Gizmos.color = Color.white;
                Gizmos.DrawRay(_dashStartPosition, _dashDirection.normalized * _dashSpeed * _dashDuration * dashRayLengthMultiplier);

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

                Gizmos.DrawSphere(transform.position, 0.25f);
            }
        }
        #endregion
    }
}