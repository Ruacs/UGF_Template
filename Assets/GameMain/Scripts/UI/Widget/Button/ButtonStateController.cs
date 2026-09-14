using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public enum ButtonState
{
    Normal,
    Disabled,
    Selected,
    Locked,
    Cooldown,
    Easy,
    Hard,
    VeryHard
}

public class ButtonStateController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Button button;
    [SerializeField] private Image targetImage;

    [Header("State Configs")]
    [SerializeField] private List<StateVisualConfig> stateConfigs = new();

    [Header("Default")]
    [SerializeField] private ButtonState initialState = ButtonState.Normal;

    private ButtonState currentState;

    public ButtonState CurrentState => currentState;

    public UnityEvent OnClick;

    private void Reset()
    {
        CacheReferences();
    }

    private void Awake()
    {
        CacheReferences();
        button.AddSafeClick(HandleClick);
        ApplyState(initialState, true);
    }

    private void OnValidate()
    {
        CacheReferences();

        if (!Application.isPlaying)
        {
            ApplyState(initialState, false);
        }
    }

    public void SetState(ButtonState state)
    {
        ApplyState(state, true);
    }

    private void HandleClick()
    {
        OnClick?.Invoke();
    }


    private void ApplyState(ButtonState state, bool updateCurrentState)
    {
        var config = GetVisualConfig(state);

        if (button != null)
        {
            button.interactable = config.Interactable;
        }

        if (targetImage != null && config.Sprite != null)
        {
            targetImage.sprite = config.Sprite;
        }

        ApplyExtraTargets(config);

        if (updateCurrentState)
        {
            currentState = state;
        }
    }

    private StateVisualConfig GetVisualConfig(ButtonState state)
    {
        for (int i = 0; i < stateConfigs.Count; i++)
        {
            var config = stateConfigs[i];
            if (config.State == state)
            {
                return config;
            }
        }

        return new StateVisualConfig
        {
            State = state,
            Interactable = state != ButtonState.Disabled &&
                           state != ButtonState.Locked &&
                           state != ButtonState.Cooldown
        };
    }

    private void CacheReferences()
    {
        if (button == null)
        {
            button = GetComponent<Button>();
        }

        if (targetImage == null)
        {
            targetImage = GetComponent<Image>();
        }
    }

    private void ApplyExtraTargets(StateVisualConfig activeConfig)
    {
        for (int i = 0; i < stateConfigs.Count; i++)
        {
            var config = stateConfigs[i];
            bool isActiveState = config.State == activeConfig.State;

            if (isActiveState)
            {
                config.OnStateChanged?.Invoke();
            }

            if (config.ActiveObjects == null)
            {
                continue;
            }

            for (int j = 0; j < config.ActiveObjects.Count; j++)
            {
                var target = config.ActiveObjects[j];
                if (target != null)
                {
                    target.SetActive(isActiveState);
                }
            }
        }
    }

    [Serializable]
    private struct StateVisualConfig
    {
        public ButtonState State;
        public bool Interactable;
        public Sprite Sprite;
        public List<GameObject> ActiveObjects;

        public UnityEvent OnStateChanged;
    }
}
