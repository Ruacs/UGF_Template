using DG.Tweening;
using UnityEngine;

public enum FloatingTextAnimType
{
    Move,
    Pop,
    Shake,
    FlyToTarget,
    FadeMove
}

[System.Serializable]
public class FloatingTextData
{
    public bool bgActive = false;
    public string content;
    public Vector2 startScreenPos;
    public Vector2 endScreenPos;
    public Vector2 offset = new Vector2(0, 200);
    public float duration = 0.8f;
    public Ease ease = Ease.OutCubic;
    public FloatingTextAnimType animType = FloatingTextAnimType.Move;
    public float startScale = 1f;
    public float endScale = 1f;
}
