using UnityEngine;
using UnityEngine.UI;

public class UIFrameAnimation : MonoBehaviour
{
    [SerializeField] private Image targetImage;
    [SerializeField] private Sprite[] frames;
    [SerializeField] private float fps = 12f;

    private float timer;
    private int index;

    private void Update()
    {
        if (frames == null || frames.Length == 0) return;

        timer += Time.deltaTime;
        if (timer >= 1f / fps)
        {
            timer = 0f;
            index = (index + 1) % frames.Length;
            targetImage.sprite = frames[index];
        }
    }
}