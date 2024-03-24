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
        private Vector3 velocity;

        // Start is called before the first frame update
        void Start()
        {
            
        }

        // Update is called once per frame
        void Update()
        {
            Vector2 playerInput = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
            playerInput = Vector2.ClampMagnitude(playerInput, 1f);
            Vector3 desiredVelocity;
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
            float maxSpeedChange = maxAcceleration * Time.deltaTime;
            velocity.x = Mathf.MoveTowards(velocity.x, desiredVelocity.x, maxSpeedChange);
            velocity.z = Mathf.MoveTowards(velocity.z, desiredVelocity.z, maxSpeedChange);

            Vector3 displacement = velocity * Time.deltaTime;

            playerRB.position += displacement;
        }
    }   

}