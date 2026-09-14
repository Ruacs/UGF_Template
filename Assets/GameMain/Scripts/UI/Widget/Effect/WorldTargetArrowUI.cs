using UnityEngine;
using UnityEngine.Assertions;

/// <summary>
/// 世界目标指向箭头UI组件
/// 功能：当目标在屏幕外时，显示箭头UI并贴屏幕边缘指向目标；屏幕内时隐藏UI
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class WorldTargetArrowUI : MonoBehaviour
{
    [Header("核心引用（必填）")]
    [Tooltip("观察点（通常是玩家/相机）")]
    public Transform observer;
    [Tooltip("需要指向的目标")]
    public Transform target;
    [Tooltip("UI相机（Canvas的Render Camera）")]
    public Camera UICamera;
    [Tooltip("主场景相机（用于世界坐标转屏幕坐标）")]
    public Camera mainCamera;

    [Header("箭头UI引用")]
    [Tooltip("箭头根节点（会被贴到屏幕边缘）")]
    public RectTransform arrowRootRect;
    [Tooltip("箭头图标（负责旋转指向目标）")]
    public RectTransform arrowIconRect;
    [Header("Root Canvas")]
    public RectTransform canvasRect;  //必须是Root Canvas  

    [Header("自定义配置")]
    [Tooltip("屏幕边缘偏移（适配箭头根节点尺寸，避免UI超出屏幕 默认为ArrowRoot.size / 2）")]
    [SerializeField] private Vector2 screenEdgeOffset = new Vector2(100f, 100f);
    [Tooltip("屏幕边缘内边距（左右下上）")]
    [SerializeField] private Vector4 screenEdgePadding = new Vector4(0, 0, 0, 0);
    [Tooltip("视口检测容差（避免箭头在屏幕边缘闪烁，建议0.01-0.1）")]
    [SerializeField] private float viewportTolerance = 0.05f;
    [Tooltip("箭头默认旋转偏移（适配不同朝向的箭头图片，默认朝上填-90）")]
    [SerializeField] private float arrowRotationOffset = -90f;
    [Tooltip("是否只在XZ平面计算方向（忽略Y轴高度差）")]
    [SerializeField] private bool ignoreYAxis = true;
    [Tooltip("最小显示距离（小于该距离时隐藏箭头，单位：米）")]
    [SerializeField] private float minDisplayDistance = 5f; // 新增：最小显示距离配置

    [SerializeField]
    private float smoothTime = 0.08f; // 0.05~0.15 手感最好
    private Vector2 _arrowPosVelocity;
    // 组件缓存
    private RectTransform _selfRectTransform;
    private bool _isInitialized;

    #region 生命周期
    private void Awake()
    {
        // 初始化组件
        Initialize();
    }

    private void Start()
    {
        // 初始隐藏箭头
        SetArrowActive(false);
    }

    private void Update()
    {
        if (!_isInitialized) return;

        RefreshArrow();
    }
    #endregion


    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        if (target == observer) target = null;
    }


    #region 核心初始化
    /// <summary>
    /// 初始化组件，校验必要引用
    /// </summary>
    private void Initialize()
    {
        // 缓存自身RectTransform
        _selfRectTransform = GetComponent<RectTransform>();
        canvasRect = transform.parent.GetComponent<RectTransform>();
        _isInitialized = true;
        screenEdgeOffset = arrowRootRect.sizeDelta / 2;
    }



    #endregion

    #region 核心功能逻辑
    /// <summary>
    /// 刷新箭头状态（显示/隐藏、位置、旋转）
    /// </summary>
    public void RefreshArrow()
    {
        // 基础判空保护
        if (observer == null || target == null || mainCamera == null || UICamera == null || canvasRect == null)
        {
            SetArrowActive(false);
            return;
        }
        bool isDistanceTooClose = IsDistanceTooClose();
        // 判断目标是否在屏幕内
        bool isTargetInScreen = IsTargetInViewport();

        // 控制箭头显示/隐藏
        SetArrowActive(!isTargetInScreen && !isDistanceTooClose);

        // 目标在屏幕内，无需后续处理
        if (isTargetInScreen) return;

        // 计算目标相对于观察点的方向
        Vector3 direction = GetTargetDirection();

        // 目标与观察点重合，重置箭头
        if (direction.sqrMagnitude < 0.01f)
        {
            ResetArrow();
            return;
        }

        // 更新箭头旋转（指向目标）
        UpdateArrowRotation(direction);

        // 更新箭头位置（贴屏幕边缘）
        UpdateArrowPosition();
    }

    /// <summary>
    /// 新增：判断观察者与目标的距离是否过近
    /// </summary>
    /// <returns>true=距离过近，false=距离足够</returns>
    public bool IsDistanceTooClose()
    {
        if (observer == null || target == null) return true;

        // 计算距离（根据ignoreYAxis决定是否忽略Y轴）
        float distance;
        if (ignoreYAxis)
        {
            Vector3 dir = target.position - observer.position;
            dir.y = 0;
            distance = dir.magnitude;
        }
        else
        {
            distance = Vector3.Distance(observer.position, target.position);
        }

        return distance < minDisplayDistance;
    }

    /// <summary>
    /// 判断目标是否在相机视口内（屏幕内）
    /// </summary>
    /// <returns>true=在屏幕内，false=在屏幕外</returns>
    public bool IsTargetInViewport()
    {
        Vector3 viewportPos = mainCamera.WorldToViewportPoint(target.position);

        // 视口坐标范围：0~1，加入容差避免边缘闪烁
        bool inXRange = viewportPos.x >= 0 - viewportTolerance && viewportPos.x <= 1 + viewportTolerance;
        bool inYRange = viewportPos.y >= 0 - viewportTolerance && viewportPos.y <= 1 + viewportTolerance;
        // 确保目标在相机前方
        bool inFrontOfCamera = viewportPos.z > 0;

        return inXRange && inYRange && inFrontOfCamera;
    }

    /// <summary>
    /// 获取目标相对于观察点的方向向量
    /// </summary>
    /// <returns>方向向量</returns>
    public Vector3 GetTargetDirection()
    {
        Vector3 direction = target.position - observer.position;

        // 是否忽略Y轴（只在XZ平面计算）
        if (ignoreYAxis)
        {
            direction.y = 0;
        }

        return direction;
    }

    /// <summary>
    /// 更新箭头旋转角度，指向目标
    /// </summary>
    /// <param name="direction">目标方向向量</param>
    public void UpdateArrowRotation(Vector3 direction)
    {
        // 计算旋转角度（XZ平面）
        float angle = Mathf.Atan2(direction.z, direction.x) * Mathf.Rad2Deg + arrowRotationOffset;

        // 应用旋转到箭头图标
        arrowIconRect.localEulerAngles = new Vector3(0, 0, angle);
    }

    /// <summary>
    /// 更新箭头位置，贴到屏幕边缘
    /// </summary>
    public void UpdateArrowPosition()
    {
        // 世界坐标转屏幕坐标
        Vector3 targetScreenPos = mainCamera.WorldToScreenPoint(target.position);
   

        // 2️⃣ 屏幕坐标 → Canvas 本地坐标
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect,                 // ⚠️ 一定是 Canvas 的 RectTransform
                targetScreenPos,
                UICamera,                    // Overlay 传 null，Camera 模式传 UI Camera
                out Vector2 localPos))
        {
            return;
        }

        // 3️⃣ 获取 Canvas 实际尺寸（Expand 下这是“真实边界”）
        Vector2 canvasSize = canvasRect.sizeDelta;

        // Canvas 是中心点坐标系
        float left = -canvasSize.x * 0.5f + screenEdgeOffset.x + screenEdgePadding.x;
        float right = canvasSize.x * 0.5f - screenEdgeOffset.x - screenEdgePadding.y;
        float bottom = -canvasSize.y * 0.5f + screenEdgeOffset.y + screenEdgePadding.z;
        float top = canvasSize.y * 0.5f - screenEdgeOffset.y - screenEdgePadding.w;

        // 4️⃣ Clamp 到 Canvas 边界
        localPos.x = Mathf.Clamp(localPos.x, left, right);
        localPos.y = Mathf.Clamp(localPos.y, bottom, top);

        // 5️⃣ 平滑移动
        arrowRootRect.anchoredPosition = Vector2.SmoothDamp(
            arrowRootRect.anchoredPosition,
            localPos,
            ref _arrowPosVelocity,
            smoothTime
        );
    }

    /// <summary>
    /// 重置箭头状态（位置、旋转）
    /// </summary>
    public void ResetArrow()
    {
        arrowRootRect.anchoredPosition = Vector2.zero;
        arrowIconRect.localEulerAngles = Vector3.zero;
    }

    /// <summary>
    /// 设置箭头UI的激活状态
    /// </summary>
    /// <param name="isActive">是否激活</param>
    public void SetArrowActive(bool isActive)
    {
        if (arrowRootRect != null && arrowRootRect.gameObject.activeSelf != isActive)
        {
            arrowRootRect.gameObject.SetActive(isActive);
        }
    }
    #endregion

    #region 编辑器辅助
    // 编辑器下绘制Gizmos，方便调试
    private void OnDrawGizmosSelected()
    {
        if (observer == null || target == null) return;

        // 绘制观察点到目标的连线
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(observer.position, target.position);

        // 标记观察点和目标
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(observer.position, 0.2f);
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(target.position, 0.2f);
    }
    #endregion
}