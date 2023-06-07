using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MouseJump : MonoBehaviour
{

    // === Visible in the Inspector Variables ===

    // Input setting
    [Tooltip("The Unity string code for the button that will control jumping.")]
    [SerializeField] private string jumpButtonName = "Fire1";
    [Tooltip("A layermask of all the layers off of which the player will be able to jump.")]
    [SerializeField] private LayerMask standableLayers;
    [Tooltip("A trigger connected to the bottom of the player to check if they're on the ground.")]
    [SerializeField] private Collider2D groundedTrigger;

    [Header("Jump Tuning")]
    [Tooltip("Controlls the strength of the player's jump.")]
    [SerializeField] private float jumpStrength;
    [Tooltip("Controls a distance on the screen beyond which moving the mouse further away will not increase jump strength.")]
    [SerializeField] private float maxMouseDistance;
    [Tooltip("Controls the percentage of the player's velocity that it will keep after jumping.")]
    [Range(0f,1f)]
    [SerializeField] private float velocityTransfer;


    [Header("Jump Telegraph Tuning")]
    [Tooltip("The maximum time in the air for which we will draw a telegraph. Effectively the max length.")]
    [SerializeField] private float maxTelegraphTime;
    [Tooltip("The time in the air between points in the telegraph. Less is smoother but more expensive.")]
    [SerializeField] private float telegraphTimeBetweenPoints;


    // === Private Variables ===

    // Components
    private Camera cam;
    private Rigidbody2D rb;
    private LineRenderer telegraph;
   
    // Stores whether we are currently on the ground or not
    private bool isGrounded;

    // Jump Inputs
    private bool jumpPressed;
    private bool jumpWasPressed;



    // Start is called before the first frame update
    void Start()
    {
        cam = Camera.main;
        rb = GetComponent<Rigidbody2D>();
        telegraph = GetComponent<LineRenderer>();
    }


    // Update is called once per frame
    void Update()
    {
        // Check if we're grounded
        updateGrounded();

        if (isGrounded)
        {
            // Get player input if we're grounded and can use it
            jumpPressed = Input.GetButton(jumpButtonName);
        } else
        {
            // If we're not grounded, reset player input so we can't jump
            jumpPressed = false;
            jumpWasPressed = false;
        }


        // ===========================
        // ===== Jumping Section =====
        // ===========================

        //Only do jump stuff if we are grounded and the jump button is down or was down last frame.
        if (isGrounded && (jumpPressed || jumpWasPressed))
        {
            // Find difference between mouse position and player position in screen space.
            Vector2 mousePos = Input.mousePosition;
            Vector2 playerPos = cam.WorldToScreenPoint(transform.position);
            Vector2 playerToMouse = mousePos - playerPos;

            // Normalize the strength of the jump based on screen size.
            // This prevents people with higher resolution from jumping further,
            // it also means you can jump equaly in the horizontal and vertical directions,
            // Despite your screen (probably) being wider than it is tall.
            playerToMouse.x = playerToMouse.x / Screen.width;
            playerToMouse.y = playerToMouse.y / Screen.height;

            // Limit the effect of mouse placement.
            playerToMouse = Vector2.ClampMagnitude(playerToMouse, maxMouseDistance);

            if (!jumpPressed)
            {
                // If the jump button has been released, then jump
                hideTelegraph();
                jump(playerToMouse, playerToMouse.magnitude);
            }
            else
            {
                // If the jump button is still being held, render the jump guide
                renderTelegraph(playerToMouse, playerToMouse.magnitude);
            }
        }
        else
        {
            // If we are not on the ground or the jump button isn't being held, hide the jump guides
            hideTelegraph();
        }

        // Record whether the jump button was held this frame
        jumpWasPressed = jumpPressed;
    }



    // ========================
    // ===== Jump Helpers =====
    // ========================

    // Makes the player jump in the given direction with the given velocity
    private void jump(Vector2 jumpDirection, float jumpForce)
    {
        rb.velocity = calculateJump(jumpDirection, jumpForce);
    }

    // Calculates the player's velocity after jumping in the given direction with the given force. Takes into account velocity transfer.
    private Vector2 calculateJump(Vector2 jumpDirection, float jumpForce)
    {
        jumpDirection = jumpDirection.normalized;

        // The velocity before the jump, adjusted by velocityTransfer
        Vector2 initialVelocity = (rb.velocity * velocityTransfer);

        // Add the jump velocity
        initialVelocity += jumpDirection * (jumpForce * jumpStrength);

        return initialVelocity;
    }



    // =============================
    // ===== Telegraph Helpers =====
    // =============================

    // Renders the line which shows the player where their jump will take them.
    private void renderTelegraph(Vector2 jumpDirection, float jumpForce)
    {
        Vector2 initialVelocityPrediction = calculateJump(jumpDirection, jumpForce);
        List<Vector3> telegraphPoints = new List<Vector3>(telegraph.positionCount); // Initialize the list with enough slots for the number of positions we had last time

        // Add the player's position as the first point
        Vector2 currentPoint = new Vector2(transform.position.x, transform.position.y);
        telegraphPoints.Add(currentPoint);
        float nextPointTime = telegraphTimeBetweenPoints;

        while (nextPointTime <= maxTelegraphTime)
        {
            Vector2 nextPoint = calculatePositionAfterTime(initialVelocityPrediction, nextPointTime);
            Vector2 currentToNextPoint = nextPoint - currentPoint;

            RaycastHit2D groundCheck = Physics2D.Raycast(currentPoint, currentToNextPoint.normalized, currentToNextPoint.magnitude, standableLayers);

            if (groundCheck)
            {

                // If we hit something, record where we hit it and stop tracing the telegraph.
                telegraphPoints.Add(groundCheck.point);
                break;
            } else
            {
                // IF we didn't hit anything, record the next point and keep tracing the telegraph.
                telegraphPoints.Add(nextPoint);
                currentPoint = nextPoint;
                nextPointTime += telegraphTimeBetweenPoints;
            }
        }


        // Once weve traced the telegraph, draw it
        telegraph.positionCount = telegraphPoints.Count;
        telegraph.SetPositions(telegraphPoints.ToArray());
        
        // Make sure the telegraph is turned on
        telegraph.enabled = true;
    }

    // Hides the telegraph
    private void hideTelegraph()
    {
        // Disable the telegraph
        telegraph.enabled = false;
    }


    /* 
     * Calculates the player's position after _elpsedTime_ when starting with _initialVelocity_
     * 
     * Math behind it (Kinematic Formula #3):
     * deltaY = V_0 * t + 1/2 * a_Y * t^2
     * 
     * Translation:
     * displacement = initialVelocity * elapsedTime + 1/2 * accelerationDueToGravity * elapsedTime^2
     * 
     * Note that this also add the player's current postion, so predicted displacement becomes predicted position.
     * 
     * Furether Reading/Where I stole this from:
     * https://www.khanacademy.org/science/physics/two-dimensional-motion/two-dimensional-projectile-mot/a/what-is-2d-projectile-motion
     */
    private Vector2 calculatePositionAfterTime(Vector2 initialVelocity, float elapsedTime)
    {
        return new Vector2(transform.position.x, transform.position.y) + initialVelocity * elapsedTime + 
                   Physics2D.gravity * (elapsedTime * elapsedTime * 0.5f) * rb.gravityScale;
    }

    private void updateGrounded()
    {
        isGrounded = groundedTrigger.IsTouchingLayers(standableLayers);
    }
}
