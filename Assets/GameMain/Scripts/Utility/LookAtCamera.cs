using UnityEngine;

public class LookAtCamera : MonoBehaviour
{
    public enum Mode
    {
        LookAt,              // 正常朝向相机
        LookAtInverted,      // 反向朝向相机
        CameraForward,      // 跟随相机 forward
        CameraForwardInverted // 反向 forward
    }

    public Mode mode = Mode.CameraForward;

    [SerializeField] private Camera m_Camera;

    private void Awake()
    {
        m_Camera = Camera.main;
    }

    public void SetCamera(Camera camera)
    {
        m_Camera = camera;
    }

    private void LateUpdate()
    {
        if (m_Camera == null) return;

        switch (mode)
        {
            // 物体朝向相机（标准 Billboard）
            case Mode.LookAt:
                transform.LookAt(m_Camera.transform);
                break;

            // 朝向相机的反方向
            case Mode.LookAtInverted:
                Vector3 dirFromCamera =
                    transform.position - m_Camera.transform.position;
                transform.LookAt(transform.position + dirFromCamera);
                break;

            // 直接使用相机 forward（最稳、最常用）
            case Mode.CameraForward:
                transform.forward = m_Camera.transform.forward;
                break;

            // 相机 forward 的反方向
            case Mode.CameraForwardInverted:
                transform.forward = -m_Camera.transform.forward;
                break;
        }
    }
}
