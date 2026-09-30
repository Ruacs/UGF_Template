using System;
using UnityEngine;

namespace Lokas
{

    [RequireComponent(typeof(Camera))]
    public class CameraController : MonoBehaviour
    {
        [SerializeField] Vector3 offset = new Vector3(0, 18, -11.2f);
        [SerializeField] float defaultXOffset = 1;
        [SerializeField] float defaultYOffset = 2;


        void Awake()
        {
            GameEntry.HexaAway.LevelLoaded += OnLevelLoaded;
        }

        private void OnLevelLoaded()
        {
            Bounds bounds = GameEntry.HexaAway.LevelRepresentation.LevelBounds;
            Reposition(bounds.center, bounds.size);
        }

        /// <summary>
        /// Repositions camera so that the entire level fits inside the viewport.
        /// targetPosition = level center in world space
        /// levelSize = level bounds (x = width, z = height)
        /// </summary>
        public void Reposition(Vector3 targetPosition, Vector3 levelSize)
        {
            Camera cam = GetComponent<Camera>();

            float paddingX = defaultXOffset;
            float paddingY = defaultYOffset;

            // 1) Desired level dimensions with padding
            float desiredWidth = levelSize.x + (paddingX * 2f);
            float desiredHeight = levelSize.z + (paddingY * 2f);

            if (cam.orthographic)
            {
                // --- ORTHOGRAPHIC MODE ---
                // Calculate orthographic size so both width and height fit
                float halfHeight = desiredHeight * 0.5f;
                float halfWidth = desiredWidth / (2f * Mathf.Max(0.0001f, cam.aspect));

                cam.orthographicSize = Mathf.Max(halfHeight, halfWidth);

                // Position without scaling offset (distance doesn’t affect ortho size)
                transform.position = targetPosition + offset;

                // Look at target
                transform.LookAt(targetPosition, Vector3.up);
            }
            else
            {
                // --- PERSPECTIVE MODE ---
                // 1) Calculate frustum dimensions for the baseline offset distance
                float baselineDistance = offset.magnitude;
                float frustumHeight = 2.0f * baselineDistance * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
                float frustumWidth = frustumHeight * cam.aspect;

                // 2) Calculate multiplier so both width and height fit into frustum
                float distanceMultiplier = Mathf.Max(
                    desiredWidth / Mathf.Max(0.0001f, frustumWidth),
                    desiredHeight / Mathf.Max(0.0001f, frustumHeight)
                );

                // 3) Apply final camera position
                transform.position = targetPosition + (offset * distanceMultiplier);

                // 4) Look at the level center
                transform.LookAt(targetPosition, Vector3.up);
            }
        }


        void OnDestroy()
        {
            GameEntry.HexaAway.LevelLoaded -= OnLevelLoaded;
        }
    }
}