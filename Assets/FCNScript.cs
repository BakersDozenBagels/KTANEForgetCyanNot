using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using Random = UnityEngine.Random;

public class FCNScript : MonoBehaviour
{
    public Renderer Screen;
    public KMSelectable[] Buttons;
    // [10] = Blank, [11] = Keypad
    public Texture2D[] DigitTextures;

    private Coroutine _animation;
    private int _moduleId = ++s_moduleIdCounter, _stage, _solves, _stagesInput;
    private static int s_moduleIdCounter;
    private bool _activated, _shownFirstStage, _canInput, _focused, _isSolved, _willSolve;

    private List<Vector2Int> _stages;
    private string[] _ignoredModules;

    private void ShowNextStage()
    {
        if (_stage < _stages.Count)
        {
            var stage = _stages[_stage++];
            Log("Displaying stage {2}: {0} and {1}", stage.x, stage.y, _stage);
            ShowDigits(stage.x, stage.y);
            return;
        }
        if (_stage == _stages.Count)
        {
            Log("Entering submission. Good luck!");
            QueueCoroutine(ShowKeypad());
            _stage++;
        }
    }

    private void ShowDigits(int a, int b)
    {
        QueueCoroutine(WipeDigits(a, b));
    }

    private void QueueCoroutine(IEnumerator routine)
    {
        _animation = StartCoroutine(Delay(routine, _animation));
    }

    private IEnumerator Delay(IEnumerator routine, Coroutine animation)
    {
        if (!_activated)
            yield return new WaitUntil(() => _activated);
        yield return animation;
        yield return routine;
        yield return new WaitForSeconds(0.5f);
    }

    private Color _prevColor;
    private int _prevDigit;

    private IEnumerator WipeDigits(int a, int b)
    {
        const float Duration = 1f;
        float t = Time.time;
        Screen.material.SetTexture("_RightB", DigitTextures[b]);
        while (Time.time - t < Duration)
        {
            ScreenPosition = Easing.InSine(Time.time - t, _shownFirstStage ? 0.5f : -0.3f, 1.3f, Duration);
            yield return null;
        }
        _shownFirstStage = true;
        Screen.material.SetColor("_ColorB", _prevColor);
        Screen.material.SetColor("_ColorA", _prevColor = Random.ColorHSV(155f / 360f, 195f / 360f, 0.25f, 0.5f, 0.15f, 0.25f));
        Screen.material.SetTexture("_RightA", DigitTextures[b]);
        Screen.material.SetTexture("_LeftA", DigitTextures[_prevDigit]);
        Screen.material.SetTexture("_LeftB", DigitTextures[a]);
        _prevDigit = a;
        t = Time.time;
        while (Time.time - t < Duration)
        {
            ScreenPosition = Easing.OutSine(Time.time - t, -0.3f, 0.5f, Duration);
            yield return null;
        }
        ScreenPosition = 0.5f;
    }

    private IEnumerator ShowKeypad()
    {
        const float Duration = 1f;
        float t = Time.time;
        Screen.material.SetTexture("_RightB", DigitTextures[10]);
        while (Time.time - t < Duration)
        {
            ScreenPosition = Easing.InSine(Time.time - t, 0.5f, 1.3f, Duration);
            yield return null;
        }
        Screen.material.SetColor("_ColorB", _prevColor);
        Screen.material.SetColor("_ColorA", _prevColor = Random.ColorHSV(155f / 360f, 195f / 360f, 0.25f, 0.5f, 0.15f, 0.25f));
        Screen.material.SetTexture("_RightA", DigitTextures[10]);
        Screen.material.SetTexture("_RightB", DigitTextures[11]);
        Screen.material.SetTextureScale("_RightA", Vector2.one * 1.2f);
        Screen.material.SetTextureOffset("_RightA", new Vector2(-0.1f, -0.05f));
        Screen.material.SetTexture("_LeftA", DigitTextures[_prevDigit]);
        Screen.material.SetTexture("_LeftB", DigitTextures[10]);
        _prevDigit = 10;
        t = Time.time;
        while (Time.time - t < Duration)
        {
            ScreenPosition = Easing.InOutSine(Time.time - t, -0.3f, 1.3f, Duration);
            yield return null;
        }
        ScreenPosition = 1.3f;

        _canInput = true;
    }

    private void Start()
    {
        for (int i = 0; i < 10; i++)
        {
            int j = i;
            Buttons[i].OnInteract += () => Press(j);
        }

        GetComponent<KMSelectable>().OnFocus += () => _focused = true;
        GetComponent<KMSelectable>().OnDefocus += () => _focused = false;

        ScreenPosition = -0.3f;
        Screen.material.SetTexture("_LeftA", DigitTextures[10]);
        Screen.material.SetTexture("_LeftB", DigitTextures[_prevDigit = 10]);
        Screen.material.SetTexture("_RightA", DigitTextures[10]);
        Screen.material.SetTexture("_RightB", DigitTextures[10]);
        Screen.material.SetColor("_ColorA", _prevColor = Random.ColorHSV(155f / 360f, 195f / 360f, 0.25f, 0.5f, 0.15f, 0.25f));
        Screen.material.SetColor("_ColorB", Random.ColorHSV(155f / 360f, 195f / 360f, 0.25f, 0.5f, 0.15f, 0.25f));

        _ignoredModules = GetComponent<KMBossModule>().GetIgnoredModules("Forget Cyan Not", new string[] { "Forget Cyan Not" });
        int stageCount = GetComponent<KMBombInfo>().GetSolvableModuleNames().Count(m => !_ignoredModules.Contains(m));
#if UNITY_EDITOR
        stageCount = 5;
#endif

        if (stageCount == 0)
        {
            Log("No other non-ignored modules were found. Auto-solving.");
            QueueCoroutine(Solve(true));
            GetComponent<KMBombModule>().OnActivate += () => Activate(true);
            return;
        }

        Func<Vector2Int> stage = () =>
        {
            var s = Random.Range(0, 10);
            var a = Random.Range(0, s);
            var b = s - a;
            return new Vector2Int(a, b);
        };

        _stages = Enumerable.Repeat(0, stageCount).Select(_ => stage()).ToList();

        Log("Generated {0} stages:", stageCount);
        for (int i = 0; i < _stages.Count; i++)
        {
            Vector2Int s = _stages[i];
            Log("Stage {0}: {1} and {2} (submit {3})", i, s.x, s.y, s.x + s.y);
        }

        Log("Combined submission: {0}", _stages.Select(s => s.x + s.y).Join(""));

        GetComponent<KMBombModule>().OnActivate += () => Activate();
    }

    private float _screenPosition;
    private float ScreenPosition
    {
        set
        {
            Screen.material.SetFloat("_Position", (_screenPosition = value) + 0.02f * Mathf.Sin(Time.time * 2f * Mathf.PI / 4f));
        }
        get
        {
            return _screenPosition;
        }
    }

    private void Update()
    {
        ScreenPosition = ScreenPosition;

        if (_focused)
        {
            if (Input.GetKeyDown(KeyCode.Alpha0) || Input.GetKeyDown(KeyCode.Keypad0))
                Buttons[0].OnInteract();
            if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1))
                Buttons[1].OnInteract();
            if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2))
                Buttons[2].OnInteract();
            if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3))
                Buttons[3].OnInteract();
            if (Input.GetKeyDown(KeyCode.Alpha4) || Input.GetKeyDown(KeyCode.Keypad4))
                Buttons[4].OnInteract();
            if (Input.GetKeyDown(KeyCode.Alpha5) || Input.GetKeyDown(KeyCode.Keypad5))
                Buttons[5].OnInteract();
            if (Input.GetKeyDown(KeyCode.Alpha6) || Input.GetKeyDown(KeyCode.Keypad6))
                Buttons[6].OnInteract();
            if (Input.GetKeyDown(KeyCode.Alpha7) || Input.GetKeyDown(KeyCode.Keypad7))
                Buttons[7].OnInteract();
            if (Input.GetKeyDown(KeyCode.Alpha8) || Input.GetKeyDown(KeyCode.Keypad8))
                Buttons[8].OnInteract();
            if (Input.GetKeyDown(KeyCode.Alpha9) || Input.GetKeyDown(KeyCode.Keypad9))
                Buttons[9].OnInteract();
        }

        if (Time.frameCount % 10 != 0)
            return;

        var newSolves = GetComponent<KMBombInfo>().GetSolvedModuleNames().Count(m => !_ignoredModules.Contains(m));
        while (_solves < newSolves)
        {
            ShowNextStage();
            _solves++;
        }
    }

    private void Activate(bool auto = false)
    {
        _activated = true;
        GetComponent<KMAudio>().PlaySoundAtTransform("Startup", transform);
        if (!auto)
            ShowNextStage();
    }

    private bool Press(int ix)
    {
#if UNITY_EDITOR
        if (_stage <= _stages.Count)
        {
            ShowNextStage();
            return false;
        }
#endif

        Buttons[ix].AddInteractionPunch(0.1f);
        GetComponent<KMAudio>().PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, Buttons[ix].transform);

        if (!_canInput)
            return false;

        var s = _stages[_stagesInput];


        if (ix == s.x + s.y)
        {
            _stagesInput++;
            Log("Correct input {0} for stage {1}.", ix, _stagesInput);
            GetComponent<KMAudio>().PlaySoundAtTransform("Press" + Random.Range(0, 4), transform);
            if (_stagesInput == _stages.Count)
            {
                _canInput = false;
                _willSolve = true;
                Log("Module solved. Good job!");
                QueueCoroutine(Solve());
            }
        }
        else
        {
            Log("Incorrect input {0} for stage {2}! I was expecting {1}.", ix, s.x + s.y, _stagesInput + 1);
            GetComponent<KMBombModule>().HandleStrike();
            _canInput = false;
            QueueCoroutine(Recover(s.x, s.y));
            QueueCoroutine(ShowKeypad());
        }

        return false;
    }

    private IEnumerator Recover(int a, int b)
    {
        const float Duration = 1f;
        float t = Time.time;
        ScreenPosition = -0.3f;
        Screen.material.SetTexture("_LeftA", DigitTextures[11]);
        Screen.material.SetTexture("_LeftB", DigitTextures[10]);
        Screen.material.SetTextureScale("_LeftA", Vector2.one * 1.2f);
        Screen.material.SetTextureOffset("_LeftA", new Vector2(-0.1f, -0.05f));
        Screen.material.SetTexture("_RightA", DigitTextures[10]);
        Screen.material.SetTexture("_RightB", DigitTextures[b]);
        Screen.material.SetTextureScale("_RightA", Vector2.one * 2f);
        Screen.material.SetTextureOffset("_RightA", new Vector2(-0.9f, -0.3f));
        Screen.material.SetColor("_ColorB", _prevColor);
        Screen.material.SetColor("_ColorA", _prevColor = Random.ColorHSV(155f / 360f, 195f / 360f, 0.25f, 0.5f, 0.15f, 0.25f));
        while (Time.time - t < Duration)
        {
            ScreenPosition = Easing.InOutSine(Time.time - t, -0.3f, 1.3f, Duration);
            yield return null;
        }
        Screen.material.SetColor("_ColorB", _prevColor);
        Screen.material.SetColor("_ColorA", _prevColor = Random.ColorHSV(155f / 360f, 195f / 360f, 0.25f, 0.5f, 0.15f, 0.25f));
        Screen.material.SetTextureScale("_LeftA", Vector2.one * 2f);
        Screen.material.SetTextureOffset("_LeftA", new Vector2(-0.1f, -0.7f));
        Screen.material.SetTexture("_LeftA", DigitTextures[10]);
        Screen.material.SetTexture("_LeftB", DigitTextures[a]);
        Screen.material.SetTexture("_RightA", DigitTextures[b]);
        _prevDigit = a;
        t = Time.time;
        while (Time.time - t < Duration)
        {
            ScreenPosition = Easing.OutSine(Time.time - t, -0.3f, .5f, Duration);
            yield return null;
        }
        ScreenPosition = 0.5f;
    }

    private IEnumerator Solve(bool auto = false)
    {
        if (!auto)
            GetComponent<KMAudio>().PlaySoundAtTransform("Solve", transform);

        const float Duration = 1f;

        Screen.material.SetColor("_ColorB", _prevColor);
        Screen.material.SetColor("_ColorA", _prevColor = Random.ColorHSV(155f / 360f, 195f / 360f, 0.25f, 0.5f, 0.15f, 0.25f));
        Screen.material.SetTexture("_RightA", DigitTextures[auto ? 10 : 11]);
        Screen.material.SetTexture("_RightB", DigitTextures[10]);
        Screen.material.SetTexture("_LeftA", DigitTextures[10]);
        Screen.material.SetTexture("_LeftB", DigitTextures[10]);
        ScreenPosition = -0.3f;

        float t = Time.time;
        while (Time.time - t < Duration)
        {
            ScreenPosition = Easing.InOutSine(Time.time - t, -0.3f, 1.3f, Duration);
            yield return null;
        }
        ScreenPosition = 1.3f;

        GetComponent<KMBombModule>().HandlePass();
        _isSolved = true;

        const float Duration2 = 15f;
        t = Time.time;
        while (Time.time - t < Duration2)
        {
            Screen.material.SetColor("_ColorA", Color.Lerp(_prevColor, Color.cyan, Easing.InOutQuad(Time.time - t, 0f, 1f, Duration2)));
            yield return null;
        }
        Screen.material.SetColor("_ColorA", Color.cyan);
    }

    private void Log(object message, params object[] args)
    {
        Debug.LogFormat("[Forget Cyan Not #" + _moduleId + "] " + message, args);
    }

#pragma warning disable 414
    private readonly string TwitchHelpMessage = "!{0} 1637278950 (Submit those digits in order)";
#pragma warning restore 414
    private IEnumerator ProcessTwitchCommand(string command)
    {
        if (_willSolve || !command.Any(c => c >= '0' && c <= '9'))
            yield break;

        if (_stage <= _stages.Count)
        {
            yield return "sendtochaterror The module is not ready to accept input yet.";
            yield break;
        }

        yield return null;
        while (!_canInput)
            yield return "trycancel";

        foreach (var b in command.Where(c => c >= '0' && c <= '9').Select(c => Buttons[int.Parse(c.ToString())]))
        {
            b.OnInteract();
            yield return new WaitForSeconds(0.1f);
        }
    }

    private IEnumerator TwitchHandleForcedSolve()
    {
        while (_stage <= _stages.Count)
            yield return true;

        while (!_canInput)
            yield return true;

        while (!_willSolve)
        {
            var s = _stages[_stagesInput];
            Buttons[(s.x + s.y) % 10].OnInteract();
            yield return new WaitForSeconds(0.1f);
        }

        while (!_isSolved)
            yield return true;
    }
}
