using UnityEngine;

namespace Lokas
{
    /// <summary>
    /// HexaAway 本地测试启动器，用于在未接入完整 GF Procedure/UI 前验证基础玩法闭环。
    /// </summary>
    public sealed class LocalTestBootstrap : MonoBehaviour
    {
        [SerializeField] private HexaAwayGameManagerComponent manager;
        [SerializeField] private bool startOnStart = true;
        [SerializeField] private bool restartWithRKey = true;
        [SerializeField] private bool frameCameraOnLevelLoaded = true;
        [SerializeField] private Camera targetCamera;
        [SerializeField] private Vector3 cameraOffset = new(0f, 8f, -7f);
        [SerializeField] private Vector3 cameraEulerAngles = new(55f, 0f, 0f);
        [SerializeField] private float orthographicPadding = 1.5f;

        private void Awake()
        {
            if (manager == null)
            {
                manager = GetComponent<HexaAwayGameManagerComponent>();
            }

            if (manager == null)
            {
                manager = FindObjectOfType<HexaAwayGameManagerComponent>();
            }

            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }
        }

        private void OnEnable()
        {
            if (manager == null)
            {
                return;
            }

            manager.LevelLoaded += OnLevelLoaded;
        }

        private void OnDisable()
        {
            if (manager == null)
            {
                return;
            }

            manager.LevelLoaded -= OnLevelLoaded;
        }

        private void Start()
        {
            if (startOnStart)
            {
                StartGame();
            }
        }

        private void Update()
        {
            if (restartWithRKey && Input.GetKeyDown(KeyCode.R))
            {
                RestartGame();
            }
        }

        /// <summary>
        /// 从 Inspector 或测试按钮启动 HexaAway。
        /// </summary>
        public void StartGame()
        {
            if (manager == null)
            {
                Debug.LogWarning("HexaAway local test bootstrap requires a HexaAwayGameManagerComponent.", this);
                return;
            }

            manager.GameStart();

            if (manager.IsLevelLoaded)
            {
                OnLevelLoaded();
            }
        }

        /// <summary>
        /// 重启当前测试关卡。
        /// </summary>
        public void RestartGame()
        {
            if (manager == null)
            {
                return;
            }

            manager.Restart();
        }

        private void OnLevelLoaded()
        {
            if (frameCameraOnLevelLoaded)
            {
                FrameCamera();
            }
        }

        private void FrameCamera()
        {
            if (targetCamera == null || manager == null || manager.LevelRepresentation == null)
            {
                return;
            }

            Bounds levelBounds = manager.LevelRepresentation.LevelBounds;
            Vector3 center = levelBounds.center;

            targetCamera.transform.SetPositionAndRotation(center + cameraOffset, Quaternion.Euler(cameraEulerAngles));

            if (targetCamera.orthographic)
            {
                float horizontalSize = levelBounds.size.x * 0.5f / Mathf.Max(0.01f, targetCamera.aspect);
                float verticalSize = levelBounds.size.z * 0.5f;
                targetCamera.orthographicSize = Mathf.Max(horizontalSize, verticalSize) + orthographicPadding;
            }
        }
    }
}
