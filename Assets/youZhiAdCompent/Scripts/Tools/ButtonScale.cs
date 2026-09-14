using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace YzAdComponent
{
    public class ButtonScale : MonoBehaviour
    {
        private Button button;
        private bool isAnimating = false;

        void Start()
        {
            button = GetComponent<Button>();
            button.onClick.AddListener(OnClick);
        }

        void OnClick()
        {
            if (isAnimating)
            {
                StopAllCoroutines();
                isAnimating = false;
            }
            if (gameObject.activeSelf)
            {
                StartCoroutine(ScaleButton());
            }
        }

        IEnumerator ScaleButton()
        {
            isAnimating = true;
            float elapsedTime = 0f;
            float duration = 0.2f;

            while (elapsedTime < duration)
            {
                button.transform.localScale = Vector3.Lerp(Vector3.one, new Vector3(0.8f, 0.8f, 0.8f), elapsedTime / duration);
                elapsedTime += Time.deltaTime;
                yield return null;
            }

            button.transform.localScale = new Vector3(0.8f, 0.8f, 0.8f);

            elapsedTime = 0f;

            while (elapsedTime < duration)
            {
                button.transform.localScale = Vector3.Lerp(new Vector3(0.8f, 0.8f, 0.8f), Vector3.one, elapsedTime / duration);
                elapsedTime += Time.deltaTime;
                yield return null;
            }

            button.transform.localScale = Vector3.one;

            isAnimating = false;
        }
 
    }

}