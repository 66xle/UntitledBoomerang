﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class jump : MonoBehaviour
{
    float jumpForceValue;
    float maxJumpForceValue;
    float defaultjumpForce;
    float jumpKeyDownTimer;
    float maxJumpHeight;
    float gravity = -6;
    float gravityScale = 1;
    float fallGravityMultiplier = 2;
    float lowFallGravityMultiplier = 1.5f;

    Rigidbody rb;

    bool jumpForceIncrease;
    bool isGrounded;
    bool isHoldingJump = false;
    bool isKeyDown = false;

    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        jumpKeyDownTimer = 0.0f;
        jumpForceIncrease = true;
        isGrounded = true;
        defaultjumpForce = 280f;
        
        jumpForceValue = defaultjumpForce;

        
    }

    // Update is called once per frame
    void Update()
    {
        
        if (Input.GetKey(KeyCode.Space)) //if space is held
        {
            isHoldingJump = true;
            //jumpKeyDownTimer += Time.deltaTime; // increase timer
            //jumpForceValue += 0.5f;

            //if (jumpForceValue >= maxJumpForceValue)
            //    {
            //        jumpForceIncrease = false;
            //    }
            //if (jumpKeyDownTimer > 0.5f && jumpForceIncrease == true)
            //{
            //    
            //    
            //}
            //if (jumpKeyDownTimer > 0.5f && jumpForceIncrease == false)
            //{
            //    jumpForceValue -= 2f;
            //    if (jumpForceValue <= defaultjumpForce)
            //    {
            //        jumpForceIncrease = true;
            //    }
            //}

        }
        else
        {
            isHoldingJump = false;
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            isKeyDown = true;
        }
        else
        {
            isKeyDown = false;
        }
    
        //else if(Input.GetKeyDown(KeyCode.Space))
        //{
        //    RB.AddForce(new Vector3(0, jumpForceValue, 0), ForceMode.Impulse);
        //}

        //if (jumpKeyDownTimer < 0.5f && Input.GetKeyUp(KeyCode.Space))
        //{
        //    RB.AddForce(new Vector3(0, defaultjumpForce, 0), ForceMode.Impulse);
        //    jumpKeyDownTimer = 0;
        //}
        //else if (jumpKeyDownTimer > 0.5f && Input.GetKeyUp(KeyCode.Space))
        //{
        //    RB.AddForce(new Vector3(0, jumpForceValue, 0), ForceMode.Impulse);
        //    jumpForceValue = defaultjumpForce;
        //    jumpKeyDownTimer = 0;
        //}



    }


    private void FixedUpdate()
    {
        if (isGrounded)
            maxJumpHeight = transform.position.y + 2;

        if (isKeyDown && isGrounded)
        {
            rb.AddForce(new Vector3(0, jumpForceValue * Time.deltaTime, 0), ForceMode.Impulse);
            
        }

        //if (Input.GetKeyDown(KeyCode.F))
        //{
        //
        //    RB.AddForce(new Vector3(0, jumpForceValue, 0), ForceMode.Impulse);
        //}
        //if (transform.position.y >= maxJumpHeight)
        //{
        //    gravity = gravity * 2;
        //}

        ApplyGravity();
        
    }


    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            isGrounded = true;
            
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            isGrounded = false;
        }
    }

    void ApplyGravity()
    {
        rb.AddForce(new Vector3(0, gravity, 0) * rb.mass * gravityScale);

        if (rb.velocity.y < 0f)//if falling, multiply gravity
        {
            rb.AddForce(new Vector3(0, gravity, 0) * rb.mass * gravityScale * fallGravityMultiplier);
        }
        else if (rb.velocity.y > 0 && isHoldingJump)//if jumping and not holding the jump button
        {
            rb.AddForce(new Vector3(0, gravity, 0) * rb.mass * gravityScale * lowFallGravityMultiplier);
        }
        
    }
}
