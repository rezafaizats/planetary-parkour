using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RF.CameraOrbit
{
    public static class CameraUtils
    {
        public static float GetAngle(Vector2 direction) {
            float angle = Mathf.Acos(direction.y) * Mathf.Rad2Deg;
            return direction.x < 0f ? 360f - angle : angle;
        }
    }
}