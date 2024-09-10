using FMOD.Studio;
using FMODUnity;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MenuManager : MonoBehaviour
{
    public static MenuManager Instance { get; private set; }


    public GameObject ui;
    public RectTransform[] buttons;
    public RectTransform[] popUps;
    public TextMeshProUGUI levelText;
    public Vector3[] buttonOriginalScale;
    public float buttonScaleMultiplier = 1.2f;
    public int buttonIndex;

    public bool usingGazeControl, newGame;

    public TextMeshProUGUI[] gazeControlTexts;

    public Color[] colors;
    
    private int openedPopUpIndex;
    public Vector3 popUpOriginalScale;
    private Coroutine popUpCoroutine;
    public float popUpDuration = 0.5f;
    [Header("FMOD")]    
    [SerializeField] private EventReference navigateSoundRef;
    [SerializeField] private EventReference selectSoundRef;
    private EventInstance navigateSound;
    private EventInstance selectSound;
    
    
    private void Awake()
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

        Time.timeScale = 1;
    }

    private void Start()
    {
        usingGazeControl = SaveManager.Instance.gameData.usingEyeTracker;
        buttonIndex = 0;

        navigateSound = RuntimeManager.CreateInstance(navigateSoundRef);
        selectSound = RuntimeManager.CreateInstance(selectSoundRef);
    }

    // Update is called once per frame
    void Update()
    {
        float vertical = -InputManager.Instance.Navigate.ReadValue<float>();
        float horizontal = InputManager.Instance.SwitchLevel.ReadValue<float>();
        
        if (openedPopUpIndex != 0)
        {
            if (openedPopUpIndex == 2)
            {
                gazeControlTexts[0].gameObject.SetActive(true);
                gazeControlTexts[1].gameObject.SetActive(true);
                
                gazeControlTexts[0].color = usingGazeControl ? colors[1] : colors[0];
                gazeControlTexts[1].color = usingGazeControl ? colors[0] : colors[1];
                
                if (vertical != 0) usingGazeControl = vertical < 0;
                
                else if (Keyboard.current.anyKey.wasPressedThisFrame)
                {
                    StartCoroutine(ClosePopup(openedPopUpIndex));
                }
                
            }
            
            else if (Keyboard.current.anyKey.wasPressedThisFrame)
            {
                selectSound.start();
                StartCoroutine(ClosePopup(openedPopUpIndex));
            }
            
            return;
        }
        
        if (InputManager.Instance.Navigate.WasPressedThisFrame() || InputManager.Instance.SwitchLevel.WasPressedThisFrame())
        {
            Navigate(horizontal, vertical);
        }
        
        
        if (InputManager.Instance.Select.WasPressedThisFrame())
        {
            selectSound.start();
            switch (buttonIndex)
            {
                case 0:
                    PlayGame();
                    break;
                case 1:
                    Calibrate();
                    break;
                case 2:
                    OpenSettings();
                    break;
                case 3:
                    OpenControls();
                    break;
                case 4:
                    PlayCredits();
                    break;
                case 5:
                    QuitGame();
                    break;
            }
        }
        
        Select();
    }

    private void Navigate(float horizontal, float vertical)
    {
        navigateSound.start();
        switch (buttonIndex)
        {
            case 0:
                if (horizontal != 0)
                {
                    newGame = !newGame;
                }
                if (vertical != 0 && InputManager.Instance.Navigate.WasPressedThisFrame())
                {
                    IncrementButton((int) vertical);
                }
                break;
            case 1:
                if (vertical != 0)
                {
                    IncrementButton((int) vertical);
                }
                break;
            case 2:
                if (horizontal > 0)
                {
                    IncrementButton(1);  
                }
                else if (vertical != 0)
                {
                    IncrementButton(vertical > 0 ? 2 : -1);
                }
                break;
            case 3:
                if (horizontal < 0)
                {
                    IncrementButton(-1);  
                }
                else if (vertical != 0)
                {
                    IncrementButton(vertical > 0 ? 2 : -2);
                }
                break;
            case 4:
                if (horizontal > 0)
                {
                    IncrementButton(1);  
                }
                else if (vertical != 0)
                {
                    IncrementButton(vertical > 0 ? 0 : -2);
                }
                break;
            case 5:
                if (horizontal < 0)
                {
                    IncrementButton(-1);
                }
                else if (vertical != 0)
                {
                    IncrementButton(vertical > 0 ? 1 : -2);
                }
                break;
        }

    }

    private void IncrementButton(int x)
    {
        buttonIndex += x;
        buttonIndex = Mathf.Clamp(buttonIndex, 0, buttons.Length - 1);
    }
    
    public void Select()
    {
        for (int i = 0; i < buttons.Length; i++)
        {
            if (i == buttonIndex)
            {
                buttons[i].localScale = Vector3.Lerp(buttons[i].transform.localScale,
                    buttonOriginalScale[i] * buttonScaleMultiplier, Time.deltaTime * 10); 
            }
            else
            {
                buttons[i].localScale = Vector3.Lerp(buttons[i].transform.localScale, buttonOriginalScale[i],
                    Time.deltaTime * 10);
            }
        }
    }
    
    public void QuitGame()
    {
        Application.Quit();
    }
    
    IEnumerator SlideOutUI()
    {
        float timer = 0;
        float duration = 1;
        
        RectTransform uiRect = ui.GetComponent<RectTransform>();
        
        float startPos = uiRect.anchoredPosition.x;
        float endPos = 1000;
        
        while (timer < duration)
        {
            timer += Time.deltaTime;
            uiRect.anchoredPosition = new Vector2(Mathf.Lerp(startPos, endPos, timer / duration), uiRect.anchoredPosition.y);
            yield return null;
        }
    }
    
    public void PlayGame()
    {
        GlobalAudio.Instance.transitionGameMusic();
        StartCoroutine(SlideOutUI());
        
        if (newGame)
        {
            SaveManager.Instance.ResetSave();
        }
        
        SaveManager.Instance.gameData.usingEyeTracker = usingGazeControl;
        CutsceneCamera.Instance.StartCutscene();
    }
    
    public void Calibrate()
    {
        BeginPopup(1);
    }
    
    public void OpenSettings()
    {
        BeginPopup(2);
    }
    
    public void OpenControls()
    {
        BeginPopup(3);
    }
    
    public void PlayCredits()
    {
        BeginPopup(4);
    }
    
    void BeginPopup(int index)
    {
        openedPopUpIndex = index;
        popUpCoroutine ??= StartCoroutine(OpenPopup(index));
    }
    
    IEnumerator OpenPopup(int index)
    {
        popUps[index].gameObject.SetActive(true);
        popUps[index].localScale = Vector3.zero;
        
        float timer = 0;
        float duration = popUpDuration;
        
        while (timer < duration)
        {
            timer += Time.deltaTime;
            popUps[index].localScale = Vector3.Lerp(popUps[index].localScale, popUpOriginalScale, timer / duration);
            yield return null;
            
            if (popUps[index].localScale.x > popUpOriginalScale.x - 0.05f)
            {
                popUps[index].localScale = popUpOriginalScale;
                break;
            }
        }
        
        popUpCoroutine = null;
    }
    
    IEnumerator ClosePopup(int index)
    {
        float timer = 0;
        float duration = popUpDuration;
        
        while (timer < duration)
        {
            timer += Time.deltaTime;
            popUps[index].localScale = Vector3.Lerp(popUps[index].localScale, Vector3.zero, timer / duration);
            
            if (popUps[index].localScale.x < 0.05f)
            {
                popUps[index].localScale = Vector3.zero;
                break;
            }
            
            yield return null;
        }
        
        popUps[index].gameObject.SetActive(false);
        
        openedPopUpIndex = 0;
        
        popUpCoroutine = null;
    }

}
