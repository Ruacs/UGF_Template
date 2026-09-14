using System;
using UnityEngine;

namespace Lokas
{
    public class DefaultGameManagerComponent : SubGameManagerComponent
    {
        [SerializeField] private GameMode m_GameMode = GameMode.Game;
        [SerializeField] private Camera m_FollowCamera;
        [SerializeField] private Cinemachine.CinemachineVirtualCamera m_VirtualCamera;
        [SerializeField] private Vector3 m_FollowCameraOffset = new Vector3(0f, 20f, -20f);

        private Transform m_CameraFollowTarget;

        public override GameMode GameMode => m_GameMode;

        public event Action<Vector2> OnMoveInputChanged;
 
        public override void ResetGame()
        {
            m_CameraFollowTarget = null;
            NotifyMoveInput(Vector2.zero);
            base.ResetGame();
        }

        public void SetCameraFollow(Transform target)
        {
            if(m_VirtualCamera == null)
            {
               m_VirtualCamera = FindAnyObjectByType<Cinemachine.CinemachineVirtualCamera>();
            }
            m_VirtualCamera.Follow = target;
            m_VirtualCamera.LookAt = target;
        }

        public void NotifyMoveInput(Vector2 moveDirection)
        {
            OnMoveInputChanged?.Invoke(moveDirection);
        }

   
    
    }
}
