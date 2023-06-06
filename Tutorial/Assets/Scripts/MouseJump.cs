using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MouseJump : MonoBehaviour
{
    // Input setting
    [SerializeField] private string jumpButtonName = "Fire1";

    [Header("Jump Tuning")]
    [Tooltip("Controlls the strength of your jump.")]
    [SerializeField] private float jumpStrength;
    [Tooltip("Controls a distance on the screen beyond which moving the mouse further away will not increase jump strength.")]
    [SerializeField] private float maxMouseDistance;


    private Camera cam;
    private Rigidbody2D rb;

    // Jump Inputs
    private bool jumpPressed;
    private bool jumpWasPressed;

    // Start is called before the first frame update
    void Start()
    {
        cam = Camera.main;
    }

    // Update is called once per frame
    void Update()
    {
        // Get player input
        if(isGrounded())
        {
            jumpPressed = Input.GetButton(jumpButtonName);
        }


        // ===========================
        // ===== Jumping Section =====
        // ===========================

        //Only do jump stuff if we are grounded and the jump button is down or was down last frame.
        if (isGrounded() && (jumpPressed || jumpWasPressed))
        {
            // Find difference between mouse position and player position in screen space.
            Vector2 mousePos = Input.mousePosition;
            Vector2 playerPos = cam.WorldToScreenPoint(transform.position);
            Vector2 playerToMouse = mousePos - playerPos;

            // Set an arbitrary value to make the vector larger so that the tuning values don't have to be obscenely large or small
            float arbitraryValue = 100f; 

            // Normalize the strength of the jump based on screen size.
            // This prevents people with higher resolution from jumping further,
            // it also means you can jump equaly in the horizontal and vertical directions,
            // Despite your screen (probably) being wider than it is tall.
            playerToMouse.x = (playerToMouse.x / Screen.width) * arbitraryValue;
            playerToMouse.y = (playerToMouse.y / Screen.height) * arbitraryValue;

            // Limit the effect of mouse placement.
            playerToMouse = Vector3.ClampMagnitude(playerToMouse, maxMouseDistance);

            if (!jumpPressed)
            {
                // If the jump button has been released, then jump
                hideJumpGuide();
                jump(playerToMouse, playerToMouse.magnitude);
            }
            else
            {
                // If the jump button is still being held, render the jump guide
                renderJumpGuide(playerToMouse, playerToMouse.magnitude);
            }
        }
        else
        {
            // If we are not on the ground or the jump button isn't being held, hide the jump guides
            hideJumpGuide();
        }

        // Record whether the jump button was held this frame
        jumpWasPressed = jumpPressed;
    }

    private void renderJumpGuide(Vector2 jumpDirection, float jumpForce)
    {
        /*
        Vector2 initialVelocityPrediction = calculateJump(jumpDirection, jumpForce);

        for (int i = 0; i < jumpGuides.Length; i++)
        {

            jumpGuides[i].transform.position = calculatePointAfterTime(initialVelocityPrediction, (i + 1) * guidesTimeOffset);
            ((SpriteRenderer)(jumpGuides[i].GetComponent(typeof(SpriteRenderer)))).color = Color.white; // Set spirite renderer to show
        }
        */
    }


    /* 
     * Calculates an object's displacement after _elpsedTime_ when starting with _initialVelocity_
     * 
     * Math behind it (Kinematic Formula #3):
     * deltaY = V_0 * t + 1/2 * a_Y * t^2
     * 
     * Translation:
     * displacement = initialVelocity * elapsedTime + 1/2 * accelerationDueToGravity * elapsedTime^2
     * 
     * Furether Reading/Where I stole this from:
     * https://www.khanacademy.org/science/physics/two-dimensional-motion/two-dimensional-projectile-mot/a/what-is-2d-projectile-motion
     */

    private Vector2 calculatePointAfterTime(Vector2 initialVelocity, float elapsedTime)
    {
        return new Vector2(transform.position.x, transform.position.y) + initialVelocity * elapsedTime + 
                   Physics2D.gravity * (elapsedTime * elapsedTime * 0.5f) * rb.gravityScale;
    }

    private void hideJumpGuide()
    {
        /*
        for (int i = 0; i < jumpGuides.Length; i++)
        {
            ((SpriteRenderer)(jumpGuides[i].GetComponent(typeof(SpriteRenderer)))).color = Color.clear; // Set sprite renderer to transparent
        }
        */
    }

    private Vector2 calculateJump(Vector2 jumpDirection, float jumpForce)
    {
        jumpDirection = jumpDirection.normalized;
        Vector2 initialVelocityPrediction = rb.velocity;
        initialVelocityPrediction += jumpDirection * (jumpForce * jumpStrength);
        return initialVelocityPrediction;
    }

    private void jump(Vector2 jumpDirection, float jumpForce)
    {
        rb.velocity = calculateJump(jumpDirection, jumpForce);
    }

    private bool isGrounded()
    {
        return true;
    }
}
