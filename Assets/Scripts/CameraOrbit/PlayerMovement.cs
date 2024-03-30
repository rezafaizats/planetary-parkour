using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RF.CameraOrbit
{
    public class PlayerMovement : MonoBehaviour
    {
        [SerializeField] Transform playerInputSpace = default;
        [SerializeField] Rigidbody playerRB = default;
        [SerializeField, Range(0f, 100f)] private float maxSpeed = 10f;
        [SerializeField, Range(0f, 100f)] private float maxAcceleration = 10f;
        [SerializeField, Range(0f, 100f)] private float maxAirAcceleration = 1f;
        [SerializeField, Range(0f, 90f)] private float maxGroundAngle = 25f;
        [SerializeField, Range(0f, 90f)] private float maxStairsAngle = 50f;
        [SerializeField, Range(0f, 100f)] private float maxSnapSpeed = 100f;
        [SerializeField, Min(0f)] private float probeDistance = 1f;
        [SerializeField] private LayerMask probeMask = -1;
        [SerializeField] private LayerMask stairsMask = -1;
        [SerializeField, Range(0f, 10f)] private float jumpHeight = 2f;
        [SerializeField, Range(0, 5)] private int maxAirJump = 1;
        private Vector3 velocity, desiredVelocity;
        private Vector3 contactNormal, steepNormal;
        private int groundContactCount, steepContactCount;
        private bool isJumping;
        private bool OnGround => groundContactCount > 0;
        private bool OnSteep => steepContactCount > 0;
        private int jumpPhase;
        private int stepsSinceLastGrounded, stepsSinceLastJump;
        private float minGroundDotProduct, minStairsDotProducts;

        private void OnValidate() {
            minGroundDotProduct = Mathf.Cos(maxGroundAngle * Mathf.Deg2Rad);    
            minStairsDotProducts = Mathf.Cos(maxStairsAngle * Mathf.Deg2Rad);    
        }

        void Awake()
        {
            OnValidate();
        }

        // Start is called before the first frame update
        void Start() {
        
        }

        // Update is called once per frame
        void Update()
        {
            Vector2 playerInput = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
            playerInput = Vector2.ClampMagnitude(playerInput, 1f);
            isJumping |= Input.GetButtonDown("Jump");

            if(playerInputSpace) {
                Vector3 forward = playerInputSpace.forward;
                Vector3 right = playerInputSpace.right;
                forward.y = 0f;
                right.y = 0f;
                forward.Normalize();
                right.Normalize();
                desiredVelocity = (forward * playerInput.y + right * playerInput.x) * maxSpeed;
            }
            else {
                desiredVelocity = new Vector3(playerInput.x,  0f, playerInput.y) * maxSpeed;
            }
            
            // Vector3 displacement = velocity * Time.deltaTime;
            // playerRB.position += displacement;
        }

        private void FixedUpdate() {
            UpdateState();
            AdjustVelocity();

            if(isJumping) {
                isJumping = false;
                Jump();
            }

            ClearState();
        }

        void UpdateState() {
            stepsSinceLastGrounded += 1;
            stepsSinceLastJump += 1;
            velocity = playerRB.velocity;
            if(OnGround || SnapToGround() || CheckSteepContacts()) {
                stepsSinceLastGrounded = 0;
                if(stepsSinceLastJump > 1) {
                    jumpPhase = 0;
                }
                if(groundContactCount > 1)
                    contactNormal.Normalize();
            }
            else contactNormal = Vector3.up;
        }

        void ClearState() {
            groundContactCount = steepContactCount = 0;
            contactNormal = steepNormal = Vector3.zero;
        }

        void OnCollisionEnter(Collision other)
        {
            EvaluateCollision(other);
        }

        void OnCollisionStay(Collision other)
        {
            EvaluateCollision(other);
        }
        
        void OnCollisionExit(Collision other)
        {
            groundContactCount = 0;
        }

        private void Jump() {
            Vector3 jumpDirection;

            if (OnGround) {
                jumpDirection = contactNormal;
            }
            else if(OnSteep) {
                jumpDirection = steepNormal;
                jumpPhase =0;
            }
            else if(maxAirJump > 0 && jumpPhase <= maxAirJump) {
                if(jumpPhase == 0)
                    jumpPhase = 1;
                jumpDirection = contactNormal;
            }
            else {
                return;
            }

            stepsSinceLastGrounded = 0;
            jumpPhase += 1;
            float jumpSpeed = Mathf.Sqrt(-2f * Physics.gravity.y * jumpHeight);
            jumpDirection = (jumpDirection + Vector3.up).normalized;
            float alignedSpeed = Vector3.Dot(velocity, jumpDirection);

            if(alignedSpeed > 0f) {
                jumpSpeed = Mathf.Max(jumpSpeed - alignedSpeed, 0f);
            }
            velocity += jumpDirection * jumpSpeed;
        
        }

        void EvaluateCollision (Collision collision) {
            float minDot = GetMinDot(collision.gameObject.layer);
            for (int i = 0; i < collision.contactCount; i++) {
                Vector3 normal = collision.GetContact(i).normal;
                if(normal.y >= minDot) {
                    groundContactCount += 1;
                    contactNormal += normal;
                }
                else if(normal.y > -0.01f) {
                    steepContactCount += 1;
                    steepNormal += normal;
                }
            }
        }

        void AdjustVelocity() {
            Vector3 xAxis = ProjectOnContactPlane(Vector3.right).normalized;
            Vector3 zAxis = ProjectOnContactPlane(Vector3.forward).normalized;

            float currentX = Vector3.Dot(velocity, xAxis);
            float currentZ = Vector3.Dot(velocity, zAxis);

            float acceleration = OnGround ? maxAcceleration : maxAirAcceleration;
            float maxSpeedChange = acceleration * Time.deltaTime;

            float newX = Mathf.MoveTowards(currentX, desiredVelocity.x, maxSpeedChange);
            float newZ =  Mathf.MoveTowards(currentZ, desiredVelocity.z, maxSpeedChange);

            velocity += xAxis * (newX - currentX) + zAxis * (newZ - currentZ);
        }

        bool SnapToGround() {
            if(stepsSinceLastGrounded > 1 || stepsSinceLastJump <= 2) 
                return false;

            float speed = velocity.magnitude;
            if(speed > maxSnapSpeed)
                return false;
            if(!Physics.Raycast(playerRB.position, Vector3.down, out var hit, probeDistance, probeMask))
                return false;
            if(hit.normal.y < GetMinDot(hit.collider.gameObject.layer))
                return false;
            
            groundContactCount = 1;
            contactNormal = hit.normal;
            
            float dot = Vector3.Dot(velocity, hit.normal);
            if(dot > 0f) velocity = (velocity - hit.normal * dot).normalized * speed;
            
            return true;
        }

        bool CheckSteepContacts() {
            if(steepContactCount > 1) {
                if(steepContactCount > 1) {
                    steepNormal.Normalize();
                    if(steepNormal.y >= minGroundDotProduct) {
                        groundContactCount = 1;
                        contactNormal = steepNormal;
                        return true;
                    }
                }
            }
            return false;
        }

        Vector3 ProjectOnContactPlane(Vector3 vector) {
            return vector - contactNormal * Vector3.Dot(vector, contactNormal);
        }

        float GetMinDot(int layer) {
            return (stairsMask & (1 << layer)) == 0 ? minGroundDotProduct : minStairsDotProducts;
        }
    }   

}