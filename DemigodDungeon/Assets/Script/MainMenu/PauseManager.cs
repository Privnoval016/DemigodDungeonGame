using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseManager : MonoBehaviour
{
    public static PauseManager Instance { get; private set; }
    
    public GameObject ui;
    public RectTransform[] buttons;
    public Vector3[] buttonOriginalScale;
    public float buttonScaleMultiplier = 1.2f;
    public int buttonIndex;
    
    public bool isPaused;

    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
        
        buttonOriginalScale = new Vector3[buttons.Length];
        for (int i = 0; i < buttons.Length; i++)
        {
            buttonOriginalScale[i] = buttons[i].localScale;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (UIManager.Instance.gameOver) return;
        
        Time.timeScale = isPaused ? 0 : 1;
        
        if (InputManager.Instance.Pause.WasPressedThisFrame())
        {
            StartCoroutine(TogglePauseMenu());
        }

        if (isPaused)
        {
            float horizontal = InputManager.Instance.SwitchLevel.ReadValue<float>();
            float vertical = -InputManager.Instance.Navigate.ReadValue<float>();
            
            switch (buttonIndex)
            {
                case 0:
                    if (vertical != 0)
                    {
                        IncrementButton((int) vertical);
                    }
                    else if (horizontal < 0)
                    {
                        IncrementButton(1);
                    }
                    else if (horizontal > 0)
                    {
                        IncrementButton(2);
                    }

                    break;
                case 1:
                    if (horizontal > 0)
                    {
                        IncrementButton(1);
                    }
                    else if (vertical < 0)
                    {
                        IncrementButton(-1);
                    }

                    break;
                case 2:
                    if (vertical < 0)
                    {
                        IncrementButton(-2);
                    }
                    else if (horizontal < 0)
                    {
                        IncrementButton(-1);
                    }

                    break;
                    
            }
            
            
            for (int i = 0; i < buttons.Length; i++)
            {
                if (i == buttonIndex)
                {
                    buttons[i].localScale = buttonOriginalScale[i] * buttonScaleMultiplier;
                }
                else
                {
                    buttons[i].localScale = buttonOriginalScale[i];
                }
            }
            
            if (InputManager.Instance.Select.WasPressedThisFrame())
            {
                switch (buttonIndex)
                {
                    case 0:
                        StartCoroutine(TogglePauseMenu());
                        break;
                    case 1:
                        SaveManager.Instance.Save();
                        SceneManager.LoadScene(0);
                        break;
                    case 2:
                        SaveManager.Instance.Save();
                        Application.Quit();
                        break;
                }
            }
        }
    }
    
    private void IncrementButton(int x)
    {
        buttonIndex += x;
        buttonIndex = Mathf.Clamp(buttonIndex, 0, buttons.Length - 1);
    }
    
    IEnumerator TogglePauseMenu()
    {
        float timer = 0;
        float duration = 0.5f;
        
        RectTransform pauseMenu = ui.GetComponent<RectTransform>();

        float startPos = isPaused ? 0 : 1250;
        float endPos = isPaused ? 1250 : 0;
        
        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime;
            float t = timer / duration;
            pauseMenu.anchoredPosition = new Vector2(Mathf.SmoothStep(startPos, endPos, t), pauseMenu.anchoredPosition.y);
            yield return null;
        }
        
        isPaused = !isPaused;
    }
}
