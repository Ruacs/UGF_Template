using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GameFramework.ObjectPool;
namespace Lokas
{
    public enum PromptCloseMode
    {
        Duration,
        Click,
    }

    public class PromptUIData
    {
        public string Content;
        public float Duration;
        public PromptCloseMode CloseMode;

        public PromptUIData(string content)
        {
            Content = content;
            CloseMode = PromptCloseMode.Duration;
        }

        public PromptUIData(string content, float duration)
        {
            Content = content;
            Duration = duration;
            CloseMode = PromptCloseMode.Duration;
        }

        public PromptUIData(string content, PromptCloseMode closeMode)
        {
            Content = content;
            CloseMode = closeMode;
        }
    }

    public class PromptUIPanel : UGuiForm
    {
        private IObjectPool<GameObjectPool> m_PromptPool = null;
        private List<GameObject> m_ActivePromptList = new List<GameObject>();
        [SerializeField]
        private int m_PromptCount = 0;
        [SerializeField]
        private GameObject m_PromptTemplate;
        [SerializeField]
        private Transform m_PropmtRoot;

        protected override void OnInit(object userData)
        {
            base.OnInit(userData);
            m_PromptTemplate = transform.Find("Prompt_Template").gameObject;
            m_PropmtRoot = transform.Find("PromptRoot");
            m_PromptPool = GameEntry.ObjectPool.CreateSingleSpawnObjectPool<GameObjectPool>("PromptPool", 5);
            m_PromptPool.AutoReleaseInterval = 30;
            m_PromptPool.ExpireTime = m_PromptPool.AutoReleaseInterval;

        }


        protected override void OnOpen(object userData)
        {
            base.OnOpen(userData);

            if (userData is PromptUIData promptUIData)
            {
                if (promptUIData.CloseMode == PromptCloseMode.Click)
                {
                    ShowPromptUntilClick(promptUIData.Content);
                }
                else if (promptUIData.Duration > 0)
                {
                    ShowPrompt(promptUIData.Content, promptUIData.Duration);
                }
                else
                {
                    ShowPrompt(promptUIData.Content);
                }
            }
            else
            {
                ShowPrompt((string)userData);
            }

        }


        public void ShowPrompt(string content)
        {
            GameObject prompt = null;
            PromptLogic promptLogic = null;
            GameObjectPool pool = m_PromptPool.Spawn();
            if (pool != null)
            {
                prompt = pool.Target as GameObject;
            }
            else
            {
                prompt = Instantiate(m_PromptTemplate, m_PropmtRoot);
                prompt.name = "PromptLabel";
                m_PromptPool.Register(GameObjectPool.Create(prompt.name, prompt), true);
                m_PromptCount++;

            }

            promptLogic = prompt.GetComponent<PromptLogic>();
            promptLogic.Show();
            promptLogic.onHide = () => HidePrompt(prompt);
            promptLogic.SetContent(content);
            m_ActivePromptList.Add(prompt);
        }



        public void ShowPrompt(string content, float duration)
        {
            GameObject prompt = null;
            PromptLogic promptLogic = null;
            GameObjectPool pool = m_PromptPool.Spawn();
            if (pool != null)
            {
                prompt = pool.Target as GameObject;
            }
            else
            {
                prompt = Instantiate(m_PromptTemplate, m_PropmtRoot);
                prompt.name = "PromptLabel";
                m_PromptPool.Register(GameObjectPool.Create(prompt.name, prompt), true);
                m_PromptCount++;

            }

            promptLogic = prompt.GetComponent<PromptLogic>();
            promptLogic.Show(delay: duration);
            promptLogic.onHide = () => HidePrompt(prompt);
            promptLogic.SetContent(content);
            m_ActivePromptList.Add(prompt);
        }


        public void ShowPromptUntilClick(string content)
        {
            GameObject prompt = null;
            PromptLogic promptLogic = null;
            GameObjectPool pool = m_PromptPool.Spawn();
            if (pool != null)
            {
                prompt = pool.Target as GameObject;
            }
            else
            {
                prompt = Instantiate(m_PromptTemplate, m_PropmtRoot);
                prompt.name = "PromptLabel";
                m_PromptPool.Register(GameObjectPool.Create(prompt.name, prompt), true);
                m_PromptCount++;

            }

            promptLogic = prompt.GetComponent<PromptLogic>();
            promptLogic.Show(hideOnClick: true);
            promptLogic.onHide = () => HidePrompt(prompt);
            promptLogic.SetContent(content);
            m_ActivePromptList.Add(prompt);
        }




        private void HidePrompt(GameObject prompt)
        {
            // Hide prompt logic
            if (prompt == null) return;
            m_ActivePromptList.Remove(prompt);
            m_PromptPool.Unspawn(prompt);
        }


        public static void ShowToast(string content)
        {
            PromptUIPanel promptUIPanel = GameEntry.UI.GetUIForm(UIFormId.PromptUIPanel) as PromptUIPanel;
            if (promptUIPanel)
            {
                promptUIPanel.ShowPrompt(content);
            }
            else
            {
                GameEntry.UI.OpenUIForm(UIFormId.PromptUIPanel, content);
            }

        }


        public static void ShowToastUntilClick(string content)
        {
            PromptUIPanel promptUIPanel = GameEntry.UI.GetUIForm(UIFormId.PromptUIPanel) as PromptUIPanel;
            if (promptUIPanel)
            {
                promptUIPanel.ShowPromptUntilClick(content);
            }
            else
            {
                GameEntry.UI.OpenUIForm(UIFormId.PromptUIPanel, new PromptUIData(content, PromptCloseMode.Click));
            }

        }



        public static void ShowToast(string content, float duration)
        {
            PromptUIPanel promptUIPanel = GameEntry.UI.GetUIForm(UIFormId.PromptUIPanel) as PromptUIPanel;
            if (promptUIPanel)
            {
                promptUIPanel.ShowPrompt(content, duration);
            }
            else
            {
                GameEntry.UI.OpenUIForm(UIFormId.PromptUIPanel, new PromptUIData(content, duration));
            }

        }

    }
}
