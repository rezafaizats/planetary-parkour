using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace RF.CameraOrbit
{   
    [RequireComponent(typeof(Camera))]
    public class OrbitCamera : MonoBehaviour
    {
        private Camera mainCamera;

        [Header("Camera Positioning")]
        [SerializeField] private Transform focus = default;
        [SerializeField, Range(1f, 20f)] private float distance = 5f;
        [SerializeField, Min(0f)] private float focusRadius = 1f;
        [SerializeField, Range(0f, 1f)] private float focusCentering = 0.75f;
        private Vector3 focusPoint;
        private Vector3 previousFocusPoint;
        [SerializeField] LayerMask cameraObstructionMask = -1;

        [Header("Camera Rotation")]
        private Vector2 orbitAngles = new Vector2(12f, 0f);
        [SerializeField, Range(1f, 360f)] private float rotationSpeed = 180f;
        [SerializeField, Range(-89f, 89f)] private float minVerticalAngle = -1f;
        [SerializeField, Range(-89f, 89f)] private float maxVerticalAngle = 60f;
        [SerializeField, Min(0f)] private float alignDelay = 5f;
        [SerializeField, Range(0f, 90f)] private float alignSmoothRange = 45f;
        private float lastManualRotationTime;
        
        Vector3 CameraHalfExtends {
            get {
                Vector3 halfExtends;
                halfExtends.y = mainCamera.nearClipPlane * Mathf.Tan(0.5f * Mathf.Deg2Rad * mainCamera.fieldOfView);
                halfExtends.x = halfExtends.y * mainCamera.aspect;
                halfExtends.z = 0f;
                
                return halfExtends;
            }
        }

        void Awake() {
            mainCamera = GetComponent<Camera>();

            focusPoint = focus.position;
            transform.localRotation = Quaternion.Euler(orbitAngles);
        }

        void Start()
        {
            
        }

        void LateUpdate()
        {
            UpdateFocusPoint();
            Quaternion lookRotation;

            if(UpdateCameraRotation() || AutomaticRotation()) {
                ConstraintAngles();
                lookRotation = Quaternion.Euler(orbitAngles);
            }
            else
                lookRotation = transform.localRotation;

            Vector3 lookDirection = transform.forward;
            Vector3 lookPosition = focusPoint - lookDirection * distance;
            
            Vector3 rectOffset = lookDirection * mainCamera.nearClipPlane;
            Vector3 rectPosition = lookPosition + rectOffset;
            Vector3 castFrom = focus.position;
            Vector3 castLine = rectPosition - castFrom;
            float castDistance = castLine.magnitude;
            Vector3 castDirection = castLine / castDistance;

            if (Physics.BoxCast(castFrom, CameraHalfExtends, castDirection,
                                    out var hit, lookRotation, castDistance, cameraObstructionMask)) {
                rectPosition = castFrom + castDirection * hit.distance;
                lookPosition = rectPosition - rectOffset;
            }

            transform.SetPositionAndRotation(lookPosition, lookRotation);
        }

        void UpdateFocusPoint() {
            previousFocusPoint = focusPoint;
            Vector3 targetPoint = focus.position;

            if(focusRadius > 0f) {
                float distance = Vector3.Distance(targetPoint, focusPoint);
                float t = 1f;
                if(distance > 0.01f && focusCentering > 0f) 
                    t = Mathf.Pow(1f - focusCentering, Time.unscaledDeltaTime);
                if(distance > focusRadius)
                    t = Mathf.Min(t, focusRadius / distance);
                
                focusPoint = Vector3.Lerp(targetPoint, focusPoint, t);
            }
            else
                focusPoint = targetPoint;
        }

        bool UpdateCameraRotation() {
            Vector2 input = new Vector2(-Input.GetAxis("Mouse Y"), -Input.GetAxis("Mouse X"));
            const float e = 0.001f;
            if(input.x < -e || input.x > e || input.y < -e || input.y > e) {
                orbitAngles += rotationSpeed * Time.unscaledDeltaTime * input;
                lastManualRotationTime = Time.unscaledTime;
                return true;
            }
            return false;
        }

        void ConstraintAngles() {
            orbitAngles.x = Mathf.Clamp(orbitAngles.x, minVerticalAngle, maxVerticalAngle);

            if(orbitAngles.y < 0f)
                orbitAngles.y += 360f;
            else if( orbitAngles.y >= 360f)
                orbitAngles.y -= 360f;
        }

        bool AutomaticRotation() {
            if(Time.unscaledTime - lastManualRotationTime < alignDelay) return false;
            
            Vector2 movement = new Vector2(focusPoint.x - previousFocusPoint.x, focusPoint.z - previousFocusPoint.z);
            float movementDeltaSqr = movement.sqrMagnitude;

            if(movementDeltaSqr < 0.0001f) return false;

            float headingAngle = CameraUtils.GetAngle(movement / Mathf.Sqrt(movementDeltaSqr));
            float deltaAbs = Mathf.Abs(Mathf.DeltaAngle(orbitAngles.y, headingAngle));
            float smootRotationChange = rotationSpeed * Mathf.Min(Time.unscaledDeltaTime, movementDeltaSqr);
            if(deltaAbs < alignSmoothRange)
                smootRotationChange *= deltaAbs / alignSmoothRange;
            else if(180 -deltaAbs < alignSmoothRange)
                smootRotationChange *= (180 - deltaAbs) / alignSmoothRange;
            
            orbitAngles.y = Mathf.MoveTowardsAngle(orbitAngles.y, headingAngle, smootRotationChange);
            return true;
        }

    }
}