using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Lokas
{
    /// <summary>
    /// HexaAway 统一射线输入。负责将鼠标/触摸转换为棋盘坐标点击。
    /// </summary>
    public sealed class HexaAwayInputController : MonoBehaviour
    {
        [SerializeField] private Camera inputCamera;
        [SerializeField] private LayerMask raycastLayerMask = Physics.DefaultRaycastLayers;
        [SerializeField] private bool blockPointerOverUI = true;

        private readonly List<RaycastResult> uiRaycastResults = new();
        private HexaAwayGameManagerComponent gameManager;

        public bool BlockPointerOverUI
        {
            get => blockPointerOverUI;
            set => blockPointerOverUI = value;
        }

        public void Bind(HexaAwayGameManagerComponent manager)
        {
            gameManager = manager;
        }

        private void Update()
        {
            if (gameManager == null || !gameManager.IsPlaying || !TryGetPointerDown(out Vector2 screenPosition))
            {
                return;
            }

            if (blockPointerOverUI && IsPointerOverBlockingUI(screenPosition))
            {
                return;
            }

            Camera targetCamera = inputCamera != null ? inputCamera : Camera.main;
            if (targetCamera == null)
            {
                return;
            }

            Ray ray = targetCamera.ScreenPointToRay(screenPosition);
            RaycastHit[] hits = Physics.RaycastAll(
                ray,
                Mathf.Infinity,
                raycastLayerMask,
                QueryTriggerInteraction.Collide);
            if (hits == null || hits.Length == 0)
            {
                return;
            }

            // Tile 优先于平台/机关，确保 Reverse 临时挂载的 Tile 和 Stopper 上的 Tile 都能点击。
            for (int i = 0; i < hits.Length; i++)
            {
                TileBehavior tile = hits[i].transform.GetComponentInParent<TileBehavior>();
                if (tile != null)
                {
                    gameManager.OnObjectClicked(tile.MatrixPosition);
                    return;
                }
            }

            for (int i = 0; i < hits.Length; i++)
            {
                GimmickBehavior gimmick = hits[i].transform.GetComponentInParent<GimmickBehavior>();
                if (gimmick != null)
                {
                    gameManager.OnObjectClicked(gimmick.Position);
                    return;
                }
            }
        }

        private static bool TryGetPointerDown(out Vector2 screenPosition)
        {
            if (Input.touchCount > 0)
            {
                for (int i = 0; i < Input.touchCount; i++)
                {
                    Touch touch = Input.GetTouch(i);
                    if (touch.phase == TouchPhase.Began)
                    {
                        screenPosition = touch.position;
                        return true;
                    }
                }

                screenPosition = default;
                return false;
            }

            if (Input.GetMouseButtonDown(0))
            {
                screenPosition = Input.mousePosition;
                return true;
            }

            screenPosition = default;
            return false;
        }

        private bool IsPointerOverBlockingUI(Vector2 screenPosition)
        {
            if (EventSystem.current == null)
            {
                return false;
            }

            PointerEventData eventData = new(EventSystem.current)
            {
                position = screenPosition
            };

            uiRaycastResults.Clear();
            EventSystem.current.RaycastAll(eventData, uiRaycastResults);

            for (int i = 0; i < uiRaycastResults.Count; i++)
            {
                GameObject raycastObject = uiRaycastResults[i].gameObject;
                if (raycastObject == null)
                {
                    continue;
                }

                Selectable selectable = raycastObject.GetComponentInParent<Selectable>();
                if (selectable != null && selectable.IsActive() && selectable.IsInteractable())
                {
                    return true;
                }
            }

            return false;
        }
    }
}
