using UnityEngine;

public class GuidePage : MonoBehaviour
{
    [SerializeField] private string _pageId;

    public string PageId => _pageId;
    public bool IsReady => isActiveAndEnabled && gameObject.activeInHierarchy;

    public void Configure(string pageId)
    {
        _pageId = pageId;
    }
}
