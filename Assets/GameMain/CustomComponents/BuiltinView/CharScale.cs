using TMPro;
using UnityEngine;

public class CharScale : MonoBehaviour
{
    public TextMeshProUGUI text;
    public bool enableAnimation = false; // 外部开关
    public float speed = 1f; // 整体播放速度
    public float moveAmplitude = 5f; // 上移高度
    public float charDuration = 0.3f; // 单个字符上去并回来的时长
    public float charStartGap = 0.1f; // 相邻字符启动间隔，小于 charDuration 会重叠
    public float pauseAfterCycle = 0f; // 一轮结束后的停顿秒数
    public AnimationCurve moveCurve = new AnimationCurve(
        new Keyframe(0f, 0f),
        new Keyframe(0.5f, 1f),
        new Keyframe(1f, 0f)
    );

    private TMP_TextInfo textInfo;
    private TMP_MeshInfo[] cachedMeshInfo;

    void Awake()
    {
        text = GetComponent<TextMeshProUGUI>();
    }

    public void StopAndReset()
    {
        enableAnimation = false;
        if (text == null) return;

        text.ForceMeshUpdate();
        textInfo = text.textInfo;
        if (cachedMeshInfo == null || cachedMeshInfo.Length != textInfo.meshInfo.Length)
        {
            cachedMeshInfo = textInfo.CopyMeshInfoVertexData();
        }

        for (int i = 0; i < textInfo.meshInfo.Length; i++)
        {
            textInfo.meshInfo[i].mesh.vertices = cachedMeshInfo[i].vertices;
            text.UpdateGeometry(textInfo.meshInfo[i].mesh, i);
        }
    }

    public void Play()
    {
        enableAnimation = true;
    }

    void Update()
    {
        if (!enableAnimation || text == null)
        {
            return;
        }

        text.ForceMeshUpdate();
        textInfo = text.textInfo;
        if (cachedMeshInfo == null || cachedMeshInfo.Length != textInfo.meshInfo.Length)
        {
            cachedMeshInfo = textInfo.CopyMeshInfoVertexData();
        }

        int visibleCount = textInfo.characterCount;
        float totalDuration = Mathf.Max(
            0.0001f,
            (Mathf.Max(0, visibleCount - 1) * charStartGap) + charDuration + pauseAfterCycle
        );
        float t = Mathf.Repeat(Time.time * speed, totalDuration);

        for (int i = 0; i < textInfo.characterCount; i++)
        {
            var charInfo = textInfo.characterInfo[i];
            if (!charInfo.isVisible) continue;

            int vertexIndex = charInfo.vertexIndex;
            int materialIndex = charInfo.materialReferenceIndex;

            var sourceVertices = cachedMeshInfo[materialIndex].vertices;
            var destVertices = textInfo.meshInfo[materialIndex].vertices;

            for (int j = 0; j < 4; j++)
            {
                destVertices[vertexIndex + j] = sourceVertices[vertexIndex + j];
            }

            float startTime = i * charStartGap;
            float localTime = t - startTime;
            if (localTime < 0f || localTime > charDuration) continue;

            float phase = localTime / charDuration; // 0..1
            float yOffset = moveCurve.Evaluate(phase) * moveAmplitude;
            Vector3 offsetY = new Vector3(0f, yOffset, 0f);
            for (int j = 0; j < 4; j++)
            {
                destVertices[vertexIndex + j] += offsetY;
            }
        }

        for (int i = 0; i < textInfo.meshInfo.Length; i++)
        {
            textInfo.meshInfo[i].mesh.vertices = textInfo.meshInfo[i].vertices;
            text.UpdateGeometry(textInfo.meshInfo[i].mesh, i);
        }
    }
}
