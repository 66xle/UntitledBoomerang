﻿using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.TerrainAPI;
using UnityEngine;

public class PlayerJumpState : PlayerBaseState
{
    public PlayerJumpState(PlayerStateMachine context, PlayerStateFactory playerStateFactory) : base(context, playerStateFactory) { }

    public override void EnterState() 
    {
        //JumpBuffer();
    }
    public override void UpdateState() 
    {
        //JumpBuffer();
        //Jump();

        CheckSwitchState();
    }
    public override void ExitState() { }
    public override void CheckSwitchState() 
    {
        if (context.IsGrounded)
        {
            SwitchState(factory.Grounded());
        }
    }
    public override void InitializeSubState() { }


    //void JumpBuffer()
    //{
    //    if (context.isJumpPressed)
    //    {
    //        context.JumpBufferCounter = context.JumpBufferTime;
    //    }
    //    else
    //    {
    //        context.JumpBufferCounter -= Time.deltaTime;
    //    }
    //}

    //void Jump()
    //{
    //    if (context.AnimController.GetCurrentAnimatorStateInfo(0).IsName("DoubleJump") && context.AnimController.GetCurrentAnimatorStateInfo(0).normalizedTime > 0.7f)
    //    {
    //        context.AnimController.SetBool("DoubleJump", false);
    //    }

    //    // Player jump input
    //    if (context.JumpBufferCounter > 0f && context.JumpCoyoteCounter > 0f && context.JumpCounter <= 0f)
    //    {
    //        context.ReduceVelocityOnce = true;

    //        // Calculate Velocity
    //        float velocity = CalculateVelocity(context.JumpHeight);

    //        // Jump
    //        context.Rb.AddForce(new Vector3(0, velocity, 0), ForceMode.Impulse);

    //        context.AnimController.SetBool("Jump", true);

    //        // Set jump cooldown
    //        context.JumpCounter = context.JumpCooldown;

    //        context.JumpCoyoteCounter = 0f; // So you don't triple jump

    //    } //2nd jump
    //    else if (context.isJumpPressed && context.CanDoubleJump)
    //    {
    //        context.IsHeavyLand = false;
    //        context.CanDoubleJump = false;

    //        float velocity = CalculateVelocity(context.JumpHeight * context.DoubleJumpHeightPercent);

    //        // Jump
    //        context.Rb.AddForce(new Vector3(0, velocity, 0), ForceMode.Impulse);

    //        context.AnimController.SetBool("DoubleJump", true);

    //        // Set jump cooldown
    //        context.JumpCounter = context.JumpCooldown;
    //    }
    //}

    //float CalculateVelocity(float jumpHeight)
    //{
    //    float velocity = Mathf.Sqrt(-2 * context.Gravity * jumpHeight * context.GravityScale);
    //    velocity += -context.Rb.velocity.y; // Cancel out current velocity

    //    return velocity;
    //}

    //void ApplyGravity()
    //{
    //    if (context.Rb.velocity.y > 0f && !context.IsJumpHeld && context.ReduceVelocityOnce) // while jumping and not holding jump
    //    {
    //        context.ReduceVelocityOnce = false;
    //        float percentageOfVelocity = context.Rb.velocity.y * context.ReduceVelocity;
    //        context.Rb.velocity = new Vector3(context.Rb.velocity.x, context.Rb.velocity.y - percentageOfVelocity, context.Rb.velocity.z);
    //    }
    //    else
    //    {
    //        // Jumping while holding jump input
    //        context.Rb.AddForce(new Vector3(0, context.Gravity, 0));

    //        if (context.Rb.velocity.y < 0f)
    //            context.ReduceVelocityOnce = false;
    //    }
    //}
}
