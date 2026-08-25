using UnityEngine;

namespace BombSwap
{
    internal static class PrototypeCameraFramingUtility
    {
        public static Vector3 CalculateGroundFocusPosition(
            Camera camera,
            Vector3 targetGroundPosition)
        {
            Vector3 authoredPosition = camera.transform.position;
            Vector3 forward = camera.transform.forward;
            Vector3 focusedPosition = authoredPosition;
            if (Mathf.Abs(forward.y) > 0.0001f)
            {
                float distanceToGround =
                    (targetGroundPosition.y - authoredPosition.y) / forward.y;
                Vector3 currentGroundFocus =
                    authoredPosition + (forward * distanceToGround);
                focusedPosition += targetGroundPosition - currentGroundFocus;
            }
            else
            {
                focusedPosition.x = targetGroundPosition.x;
                focusedPosition.z = targetGroundPosition.z;
            }

            return focusedPosition;
        }
    }
}
