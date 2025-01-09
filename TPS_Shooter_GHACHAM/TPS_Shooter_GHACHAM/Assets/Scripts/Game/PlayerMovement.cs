﻿using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    public float playerSpeed = 7.0f;            
    public float jumpForce = 10.0f;            
    public float stepDown = 30.0f;              
    public float pushPower = 2.0f;
    [Tooltip("Fall velocity when the player is on a slope")]
    public float fallSlopeVelocity = 8f;     
    [Tooltip("Force in the Y-axis when the player in on a slope")]
    public float fallSlopeForce = 10.0f;        

    //  Movement variables
    Vector3 m_PlayerMovement;   //  Movement Vector
    Vector3 m_Movement;         //  Input movement Vector
    float m_Speed;              //  Speed of the player, this changes if the player run
    float m_Gravity = 20.0f;    //  Gravity
    float m_FallVelocity;       //  Variable that saves and applies gravity to the player
    bool m_IsJump;              //  Control if the player has jumped
    //  Slope variables
    bool m_OnSlope = false;     //  If the player is on a slope
    Vector3 m_HitNormal;        //  Get the normal of a plane
    //  Camera Variables
    Vector3 m_CameraForward;    //  Vectors to control camera orientation
    Vector3 m_CameraSideward;       

    float massRigibody;         //  Mass of the external objects moved by the player

    Animator m_Animator;            //  Animator component 
    CharacterController controller; //  Character Controller componet

    void Start()
    {
        m_Animator = GetComponent<Animator>();
        controller = GetComponent<CharacterController>();
        m_Speed = playerSpeed;
    }

    void Update()
    {
        float horizontalMove = Input.GetAxis("Horizontal");                     //  Get the input axis (w,a,s,d)
        float verticalMove = Input.GetAxis("Vertical");

        m_Movement.Set(horizontalMove, 0f, verticalMove);                       //  Set the movement vector with the input
        m_Movement = Vector3.ClampMagnitude(m_Movement, 1f);

        bool hasHorizontalInput = !Mathf.Approximately(horizontalMove, 0f);     //  Approximates and equalizes the input value if it is zero
        bool hasVerticalInput = !Mathf.Approximately(verticalMove, 0f);
        bool isMove = hasHorizontalInput || hasVerticalInput;                   //  Determine whether or not the player is movement

        CameraDirection();
        m_PlayerMovement = (m_Movement.x * m_CameraSideward + m_Movement.z * m_CameraForward) * m_Speed;

        Gravity();
        Jump();
        if (controller.isGrounded && !m_IsJump)
        {
            m_PlayerMovement = m_PlayerMovement + Vector3.down * stepDown;      //  Apply more force when the player fall
        }

        controller.Move((m_PlayerMovement) * Time.deltaTime);                 //  Move the player using the character controller
        m_IsJump = !controller.isGrounded;

        if (isMove)
        {
            Action();
        }
        else
        {
            m_Animator.SetFloat("ForwardMove", 0);        
            m_Animator.SetFloat("SideMove", 0);
        }
    }

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        m_HitNormal = hit.normal;                   //  Take the normal of the plane where the player is walking

        Rigidbody body = hit.collider.attachedRigidbody;

        // no rigidbody
        if (body == null || body.isKinematic)
            return;

        // We dont want to push objects below us
        if (hit.moveDirection.y < -0.3f)
            return;

       
        Vector3 pushDir = new Vector3(hit.moveDirection.x, 0, hit.moveDirection.z);

        massRigibody = body.mass;

        // Apply the push
        body.velocity = pushDir * pushPower / massRigibody;
    }

    void Action()
    {
        /*
         *  Determine if the player is walking, running, etc
         *  Play the animations for each action
         */
        if (Input.GetButton("Sprint"))
        {
            if (m_Movement.z > 0 && !m_Animator.GetBool("IsAiming"))
            {
                m_Speed = playerSpeed * 1.5f;           
                //  Multiply the initial speed to be able to run, if the speed change the blendtree of the animation have to change as well
                m_Animator.SetFloat("ForwardMove", (m_Movement.z * 2));    //  The animator controller take the speed
            }
            
            m_Animator.SetFloat("SideMove", (m_Movement.x));
        }
        else
        {
            m_Speed = playerSpeed;
            m_Animator.SetFloat("ForwardMove", m_Movement.z);
            m_Animator.SetFloat("SideMove", m_Movement.x);
        }
    }    

    void CameraDirection()
    {
        /*
         *  Get the camera orientation to make the player move with the camera orientation
         *  Forward and sideward
         * */
        m_CameraForward = Camera.main.transform.forward;
        m_CameraSideward = Camera.main.transform.right;

        m_CameraForward.y = 0;
        m_CameraSideward.y = 0;

        m_CameraForward = m_CameraForward.normalized;
        m_CameraSideward = m_CameraSideward.normalized;
    }

    void Gravity()
    {
        /*
         *  Gravity applies to the player
         *  Take the gravity variable nad it is apply when the player is on the ground or when the player jumps
         * */
        if (controller.isGrounded)
        {
            m_FallVelocity = -m_Gravity * Time.deltaTime;
            m_PlayerMovement.y = m_FallVelocity;
        }
        else
        {
            m_FallVelocity -= m_Gravity * Time.deltaTime;
            m_PlayerMovement.y = m_FallVelocity;
            m_Animator.SetFloat("AirSpeed", controller.velocity.y);
            //  control if the player is falling for a long time
            if(controller.velocity.y <= -25 || controller.velocity.y >= 20)
            {
                m_Animator.SetTrigger("Jump");
            }
        }
        m_Animator.SetBool("IsGrounded", controller.isGrounded);
        FallSlope();
    }

    void Jump()
    {
        /*
         *  When the player press the space bar the jump force is apply to the player
         * */
        if (controller.isGrounded && Input.GetButtonDown("Jump"))
        {
            m_IsJump = true;
            m_FallVelocity = jumpForce;                 //  Set the fall velocity when the player jump with the jump force to be able to the player goes up
            m_PlayerMovement.y = m_FallVelocity;
            m_Animator.SetTrigger("Jump");              //  Set the trigger in the animator to jump
        }
    }

    void FallSlope()
    {
        /*
         *  When the player is in a slope the player goes down,
         *  calculating the angle between the Y-axis and the normal slope and 
         *  comparing it with the slope limit of the character's controller to know 
         *  if the player has to fall down the slope that he cannot climb
         *  and compare if the slope is a platform with a 90 degrees plane to nor allow
         *  the player to fall down from that slope
         * */
        m_OnSlope = Vector3.Angle(m_HitNormal, Vector3.up) > controller.slopeLimit && Vector3.Angle(m_HitNormal, Vector3.up) < 89;

        if (m_OnSlope)
        {
            /*
             * Set the player position applying a the fall velocity to every X-Z axis calculating the inclination of the slope
             * And apply a force on the Y-axis to avoid bouncing
             * */
            m_PlayerMovement.x += ((1f - m_HitNormal.y) * m_HitNormal.x) * fallSlopeVelocity;
            m_PlayerMovement.z += ((1f - m_HitNormal.y) * m_HitNormal.z) * fallSlopeVelocity;
            m_PlayerMovement.y -= fallSlopeForce;
        }
    }

    private void OnAnimatorMove()
    {

    }
}
