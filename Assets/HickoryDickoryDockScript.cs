using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System;

public class HickoryDickoryDockScript : MonoBehaviour {

    public KMAudio audio;
    public KMBombInfo bomb;
    public KMBossModule boss;
    public KMSelectable[] buttons;

    public Material[] colorMats;
    public Transform minuteHand;
    public Transform hourHand;
    public Transform numbers;
    public Transform clockRotater;

    private string[,] origBombFaceLayout;
    private string[,] bombFaceLayout;
    private List<Stage> generatedStages = new List<Stage>();
    private List<int> stagesLeft = new List<int>() { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 };
    private readonly string[] directions = { "N", "NE", "E", "SE", "S", "SW", "W", "NW" };
    private readonly int[] minuteAngles = { -90, -45, 0, 45, 90, 135, 180, 225 };
    private readonly int[] hourAngles = { -60, -30, 0, 30, 60, 90, 120, 150, 180, 210, 240, -90 };
    private readonly int[][] dirOffsets = new int[][] { new int[] { 0, -1 }, new int[] { 1, -1 }, new int[] { 1, 0 }, new int[] { 1, 1 }, new int[] { 0, 1 }, new int[] { -1, 1 }, new int[] { -1, 0 }, new int[] { -1, -1 } };
    private int[] selectedColors = new int[12];
    private string[] ignoredModules;
    private int bombFaceRowLength;
    private int bombFaceColLength;
    private int moduleX;
    private int moduleY;
    private int nextStageActivation = -1;
    private int endSolveCount;
    private int mode;
    private bool handsTurning;
    private bool orbsMoving;

    bool ZenModeActive;

    private static Type selectableType = ReflectionHelper.FindType("Selectable", "Assembly-CSharp");

    static int moduleIdCounter = 1;
    int moduleId;
    private bool moduleSolved;

    void Awake()
    {
        moduleId = moduleIdCounter++;
        foreach (KMSelectable obj in buttons)
        {
            KMSelectable pressed = obj;
            pressed.OnInteract += delegate () { PressButton(pressed); return false; };
        }
    }

    void Start()
    {
        for (int i = 1; i < buttons.Length; i++)
        {
            buttons[i].transform.localPosition += new Vector3(0, -.035f, 0);
            buttons[i].gameObject.SetActive(false);
        }
        ignoredModules = boss.GetIgnoredModules("Hickory Dickory Dock", new string[]{
                "14",
                "Forget Enigma",
                "Forget Everything",
                "Forget It Not",
                "Forget Me Later",
                "Forget Me Not",
                "Forget Perspective",
                "Forget Them All",
                "Forget This",
                "Forget Us Not",
                "Hickory Dickory Dock",
                "Organization",
                "Perspective Stacking",
                "Purgatory",
                "Queen's War",
                "Simon's Stages",
                "Souvenir",
                "Tallordered Keys",
                "The Time Keeper",
                "Timing is Everything",
                "The Troll",
                "Turn The Key",
                "Übermodule",
                "Ultimate Custom Night",
                "The Very Annoying Button"
            });
        endSolveCount = bomb.GetSolvableModuleNames().Where(a => !ignoredModules.Contains(a)).ToList().Count;
        int validMods = 0;
        if (!Application.isEditor)
        {
            var selectable = transform.GetComponent(selectableType);
            var bombFace = selectable.GetValue<object>("Parent");
            var childSels = bombFace.GetValue<object[]>("Children");
            bombFaceRowLength = bombFace.GetValue<int>("ChildRowLength");
            bombFaceColLength = childSels.Length / bombFaceRowLength;
            origBombFaceLayout = new string[bombFaceRowLength, bombFaceColLength];
            bombFaceLayout = new string[bombFaceRowLength, bombFaceColLength];
            for (int i = 0; i < childSels.Length; i++)
            {
                if (childSels[i] != null)
                {
                    int x = childSels[i].GetValue<int>("x");
                    int y = childSels[i].GetValue<int>("y");
                    GameObject modObject = childSels[i].GetValue<GameObject>("gameObject");
                    if (modObject.GetComponent<KMBombModule>() == null && modObject.GetComponent<KMNeedyModule>() == null)
                    {
                        switch (modObject.name)
                        {
                            case "WireSequenceComponent(Clone)":
                                origBombFaceLayout[x, y] = "Wire Sequence";
                                bombFaceLayout[x, y] = "Wire Sequence";
                                break;
                            case "WireSetComponent(Clone)":
                                origBombFaceLayout[x, y] = "Wires";
                                bombFaceLayout[x, y] = "Wires";
                                break;
                            case "WhosOnFirstComponent(Clone)":
                                origBombFaceLayout[x, y] = "Who's on First";
                                bombFaceLayout[x, y] = "Who's on First";
                                break;
                            case "NeedyVentComponent(Clone)":
                                origBombFaceLayout[x, y] = "Needy Vent Gas";
                                bombFaceLayout[x, y] = "Needy Vent Gas";
                                break;
                            case "SimonComponent(Clone)":
                                origBombFaceLayout[x, y] = "Simon Says";
                                bombFaceLayout[x, y] = "Simon Says";
                                break;
                            case "PasswordComponent(Clone)":
                                origBombFaceLayout[x, y] = "Password";
                                bombFaceLayout[x, y] = "Password";
                                break;
                            case "MorseComponent(Clone)":
                                origBombFaceLayout[x, y] = "Morse Code";
                                bombFaceLayout[x, y] = "Morse Code";
                                break;
                            case "MemoryComponent(Clone)":
                                origBombFaceLayout[x, y] = "Memory";
                                bombFaceLayout[x, y] = "Memory";
                                break;
                            case "InvisibleWallsComponent(Clone)":
                                origBombFaceLayout[x, y] = "Maze";
                                bombFaceLayout[x, y] = "Maze";
                                break;
                            case "NeedyKnobComponent(Clone)":
                                origBombFaceLayout[x, y] = "Needy Knob";
                                bombFaceLayout[x, y] = "Needy Knob";
                                break;
                            case "KeypadComponent(Clone)":
                                origBombFaceLayout[x, y] = "Keypad";
                                bombFaceLayout[x, y] = "Keypad";
                                break;
                            case "VennWiresComponent(Clone)":
                                origBombFaceLayout[x, y] = "Complicated Wires";
                                bombFaceLayout[x, y] = "Complicated Wires";
                                break;
                            case "ButtonComponent(Clone)":
                                origBombFaceLayout[x, y] = "The Button";
                                bombFaceLayout[x, y] = "The Button";
                                break;
                            default:
                                origBombFaceLayout[x, y] = "Needy Capacitor";
                                bombFaceLayout[x, y] = "Needy Capacitor";
                                break;
                        }
                    }
                    else if (modObject.GetComponent<KMBombModule>() != null)
                    {
                        origBombFaceLayout[x, y] = modObject.GetComponent<KMBombModule>().ModuleDisplayName;
                        bombFaceLayout[x, y] = modObject.GetComponent<KMBombModule>().ModuleDisplayName;
                    }
                    else
                    {
                        origBombFaceLayout[x, y] = modObject.GetComponent<KMNeedyModule>().ModuleDisplayName;
                        bombFaceLayout[x, y] = modObject.GetComponent<KMNeedyModule>().ModuleDisplayName;
                    }
                    if (childSels[i].GetValue<Transform>("transform") == transform)
                    {
                        moduleX = x;
                        moduleY = y;
                    }
                    else
                        validMods++;
                }
            }
        }
        if (validMods > 0)
        {
            GetComponent<KMBombModule>().OnActivate += delegate ()
            {
                if (ZenModeActive)
                {
                    nextStageActivation = UnityEngine.Random.Range(60, 121);
                    Debug.LogFormat("[Hickory Dickory Dock #{0}] The next activation will be in {1} seconds", moduleId, nextStageActivation);
                }
                else if ((int)bomb.GetTime() < 60)
                {
                    EnterNoStageMode();
                    Debug.LogFormat("[Hickory Dickory Dock #{0}] There is not enough time for the module to activate, press the pedestal to solve the module", moduleId);
                }
                else
                {
                    nextStageActivation = (int)bomb.GetTime() - UnityEngine.Random.Range(60, (int)bomb.GetTime() + 1);
                    Debug.LogFormat("[Hickory Dickory Dock #{0}] The next activation will be in {1} seconds", moduleId, (int)bomb.GetTime() - nextStageActivation);
                }
            };
        }
        else
        {
            EnterNoStageMode();
            Debug.LogFormat("[Hickory Dickory Dock #{0}] No reachable modules detected on bomb face, press the pedestal to solve the module", moduleId);
        }
        StartCoroutine(PlayClicks());
    }

    void Update()
    {
        if (mode == 0 && endSolveCount == bomb.GetSolvedModuleNames().Where(a => !ignoredModules.Contains(a)).ToList().Count)
        {
            mode = 1;
            StartCoroutine(EnterSubmission());
            Debug.LogFormat("[Hickory Dickory Dock #{0}] All non-ignored modules solved, entering submission mode", moduleId);
        }
        if (mode == 0 && (int)bomb.GetTime() == nextStageActivation && stagesLeft.Count != 0)
        {
            GenerateStage();
            if (stagesLeft.Count != 0)
            {
                if (ZenModeActive)
                {
                    nextStageActivation = (int)bomb.GetTime() + UnityEngine.Random.Range(60, 121);
                    Debug.LogFormat("[Hickory Dickory Dock #{0}] The next activation will be in {1} seconds", moduleId, nextStageActivation - (int)bomb.GetTime());
                }
                else if ((int)bomb.GetTime() < 60)
                {
                    nextStageActivation = -1;
                    Debug.LogFormat("[Hickory Dickory Dock #{0}] There is not enough time for the module to activate again", moduleId);
                }
                else
                {
                    nextStageActivation = (int)bomb.GetTime() - UnityEngine.Random.Range(60, (int)bomb.GetTime() + 1);
                    Debug.LogFormat("[Hickory Dickory Dock #{0}] The next activation will be in {1} seconds", moduleId, (int)bomb.GetTime() - nextStageActivation);
                }
            }
        }
    }

    void GenerateStage()
    {
        int dirChoice = ValidDirections().PickRandom();
        int chimeChoice = UnityEngine.Random.Range(1, 13);
        while (!stagesLeft.Contains(chimeChoice))
            chimeChoice = UnityEngine.Random.Range(1, 13);
        stagesLeft.Remove(chimeChoice);
        string modName = null;
        int x = dirOffsets[dirChoice][0] + moduleX;
        int y = dirOffsets[dirChoice][1] + moduleY;
        while (modName == null)
        {
            if (bombFaceLayout[x, y] != null)
                modName = bombFaceLayout[x, y];
            else
            {
                x = dirOffsets[dirChoice][0] + x;
                y = dirOffsets[dirChoice][1] + y;
            }
        }
        Stage newStage = new Stage(directions[dirChoice], modName, UnityEngine.Random.Range(0, 12), chimeChoice);
        bombFaceLayout[x, y] = null;
        generatedStages.Add(newStage);
        if (ValidDirections().Count == 0)
            stagesLeft = new List<int>();
        Debug.LogFormat("[Hickory Dickory Dock #{0}] Module activated with the clock striking {1}", moduleId, newStage.chimes);
        Debug.LogFormat("[Hickory Dickory Dock #{0}] The minute hand is facing {1} and the hour hand is facing {2}", moduleId, newStage.minuteDirection, newStage.index + 1);
        Debug.LogFormat("[Hickory Dickory Dock #{0}] The module being pointed to is {1}, resulting in the number {2}", moduleId, newStage.moduleName, newStage.answer);
        StartCoroutine(ShowStage(newStage));
    }

    List<int> ValidDirections()
    {
        List<int> validOffsets = new List<int>();
        for (int i = 0; i < dirOffsets.Length; i++)
        {
            int x = dirOffsets[i][0] + moduleX;
            int y = dirOffsets[i][1] + moduleY;
            redo:
            if (x > -1 && x < bombFaceRowLength && y > -1 && y < bombFaceColLength)
            {
                if (bombFaceLayout[x, y] != null)
                    validOffsets.Add(i);
                else
                {
                    x = dirOffsets[i][0] + x;
                    y = dirOffsets[i][1] + y;
                    goto redo;
                }
            }
        }
        return validOffsets;
    }

    void PressButton(KMSelectable pressed)
    {
        if (moduleSolved != true && orbsMoving != true)
        {
            int index = Array.IndexOf(buttons, pressed);
            if (mode != 0 && index == 0)
            {
                pressed.AddInteractionPunch(.5f);
                audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);
            }
            if (mode == 3 && index == 0)
            {
                moduleSolved = true;
                StopAllCoroutines();
                GetComponent<KMBombModule>().HandlePass();
                StartCoroutine(SolveAnim());
                Debug.LogFormat("[Hickory Dickory Dock #{0}] Module solved", moduleId);
            }
            else if (mode == 1 && index > 0)
            {
                selectedColors[index - 1]++;
                if (selectedColors[index - 1] > 3)
                    selectedColors[index - 1] = 0;
                buttons[index].GetComponent<MeshRenderer>().material = colorMats[selectedColors[index - 1]];
            }
            else if (mode == 1 && index == 0)
            {
                Debug.LogFormat("[Hickory Dickory Dock #{0}] Submitted answer (from 1 to 12): {1}", moduleId, selectedColors.Join());
                if (CheckSubmission())
                {
                    moduleSolved = true;
                    StopAllCoroutines();
                    GetComponent<KMBombModule>().HandlePass();
                    StartCoroutine(SolveAnim());
                    Debug.LogFormat("[Hickory Dickory Dock #{0}] Module solved", moduleId);
                }
                else
                {
                    GetComponent<KMBombModule>().HandleStrike();
                    Debug.LogFormat("[Hickory Dickory Dock #{0}] Incorrect submission. Strike! Entering recovery mode...", moduleId);
                    mode = 2;
                    StopAllCoroutines();
                    StartCoroutine(Recovery());
                    StartCoroutine(PlayClicks());
                }
            }
            else if (mode == 2 && index == 0)
            {
                Debug.LogFormat("[Hickory Dickory Dock #{0}] Entering submission mode...", moduleId);
                mode = 1;
                StopAllCoroutines();
                StartCoroutine(EnterSubmission());
            }
        }
    }

    bool CheckSubmission()
    {
        for (int i = 0; i < generatedStages.Count; i++)
        {
            if (selectedColors[generatedStages[i].chimes - 1] != generatedStages[i].answer)
                return false;
        }
        return true;
    }

    void EnterNoStageMode()
    {
        mode = 3;
        for (int i = 1; i < buttons.Length; i++)
        {
            buttons[i].GetComponent<MeshRenderer>().material = colorMats[4];
            buttons[i].gameObject.SetActive(true);
            StartCoroutine(HandleOrbAnim(i - 1, false));
        }
        numbers.localPosition += new Vector3(0, -.01f, 0);
        hourHand.localScale = new Vector3(0, 0, 0);
        minuteHand.localScale = new Vector3(0, 0, 0);
    }

    IEnumerator ShowStage(Stage stage)
    {
        Vector3 initHourPos = hourHand.localEulerAngles;
        Vector3 finalHourPos = new Vector3(0, hourAngles[stage.index], 0);
        Vector3 initMinPos = minuteHand.localEulerAngles;
        Vector3 finalMinPos = new Vector3(0, minuteAngles[Array.IndexOf(directions, stage.minuteDirection)], 0);
        handsTurning = true;
        float t = 0f;
        while (t < 1f)
        {
            yield return null;
            t += Time.deltaTime * 1.5f;
            hourHand.localEulerAngles = Vector3.Lerp(initHourPos, finalHourPos, t);
            minuteHand.localEulerAngles = Vector3.Lerp(initMinPos, finalMinPos, t);
        }
        handsTurning = false;
        for (int i = 0; i < stage.chimes; i++)
        {
            yield return new WaitForSeconds(1.61f);
            if (i == stage.chimes - 1)
                audio.PlaySoundAtTransform("finalchime", transform);
            else
                audio.PlaySoundAtTransform("chime", transform);
        }
    }

    IEnumerator PlayClicks()
    {
        while (!moduleSolved)
        {
            yield return null;
            if (handsTurning)
            {
                audio.PlaySoundAtTransform("click", transform);
                yield return new WaitForSeconds(0.25f);
            }
        }
    }

    IEnumerator EnterSubmission()
    {
        orbsMoving = true;
        Vector3 initNumPos = numbers.localPosition;
        Vector3 finalNumPos = numbers.localPosition + new Vector3(0, -.01f, 0);
        Vector3 initHandScale = hourHand.localScale;
        Vector3 finalScale = new Vector3(0, 1, 0);
        float t = 0f;
        while (t < 1f)
        {
            yield return null;
            t += Time.deltaTime;
            numbers.localPosition = Vector3.Lerp(initNumPos, finalNumPos, t);
            hourHand.localScale = Vector3.Lerp(initHandScale, finalScale, t);
            minuteHand.localScale = Vector3.Lerp(initHandScale, finalScale, t);
        }
        for (int i = 0; i < 12; i++)
        {
            buttons[i + 1].gameObject.SetActive(true);
            StartCoroutine(HandleOrbAnim(i, true));
            yield return new WaitForSeconds(0.15f);
        }

    }

    IEnumerator HandleOrbAnim(int orb, bool delay)
    {
        float randoSpeed = UnityEngine.Random.Range(2f, 2.5f);
        Vector3 initOrbPos = buttons[orb + 1].transform.localPosition;
        Vector3 finalPos = new Vector3(initOrbPos.x, .035f, initOrbPos.z);
        float t = 0f;
        if (delay)
        {
            while (t < 1f)
            {
                yield return null;
                t += Time.deltaTime * randoSpeed;
                buttons[orb + 1].transform.localPosition = Vector3.Lerp(initOrbPos, finalPos, t);
            }
        }
        else
            buttons[orb + 1].transform.localPosition += new Vector3(0, .035f, 0);
        if (orb == 11)
            orbsMoving = false;
        initOrbPos = buttons[orb + 1].transform.localPosition;
        finalPos = initOrbPos + new Vector3(0, .005f, 0);
        t = 0f;
        float speed = randoSpeed;
        float t2 = 0f;
        while (t < 1f)
        {
            yield return null;
            t += Time.deltaTime * speed;
            t2 += Time.deltaTime;
            speed = Mathf.Lerp(randoSpeed, 0f, t2);
            buttons[orb + 1].transform.localPosition = Vector3.Lerp(initOrbPos, finalPos, t);
        }
        float[] distOffsets = { -.005f, -.005f, .005f, .005f };
        float[] startSpeeds = { 0, randoSpeed, 0f, randoSpeed };
        float[] endSpeeds = { randoSpeed, 0f, randoSpeed, 0f };
        int curIndex = 0;
        while (true)
        {
            initOrbPos = buttons[orb + 1].transform.localPosition;
            finalPos = initOrbPos + new Vector3(0, distOffsets[curIndex], 0);
            t = 0f;
            speed = 0;
            t2 = 0f;
            while (t < 1f)
            {
                yield return null;
                t += Time.deltaTime * speed;
                t2 += Time.deltaTime;
                speed = Mathf.Lerp(startSpeeds[curIndex], endSpeeds[curIndex], t2);
                buttons[orb + 1].transform.localPosition = Vector3.Lerp(initOrbPos, finalPos, t);
            }
            curIndex++;
            if (curIndex > 3)
                curIndex = 0;
        }
    }

    IEnumerator Recovery()
    {
        orbsMoving = true;
        StartCoroutine(LowerOrbs(-.035f, true));
        Vector3 initNumPos = numbers.localPosition;
        Vector3 finalNumPos = numbers.localPosition + new Vector3(0, .01f, 0);
        Vector3 initHandScale = hourHand.localScale;
        Vector3 finalScale = new Vector3(1, 1, 1);
        float t = 0f;
        while (t < 1f)
        {
            yield return null;
            t += Time.deltaTime;
            numbers.localPosition = Vector3.Lerp(initNumPos, finalNumPos, t);
            hourHand.localScale = Vector3.Lerp(initHandScale, finalScale, t);
            minuteHand.localScale = Vector3.Lerp(initHandScale, finalScale, t);
        }
        yield return new WaitForSeconds(2f);
        int curStage = 0;
        while (true)
        {
            StartCoroutine(ShowStage(generatedStages[curStage]));
            yield return new WaitForSeconds(25f);
            curStage++;
            if (curStage > generatedStages.Count - 1)
                curStage = 0;
        }
    }

    IEnumerator LowerOrbs(float yVal, bool killOrbs)
    {
        for (int i = 0; i < 12; i++)
        {
            Vector3 initOrbPos = buttons[i + 1].transform.localPosition;
            Vector3 finalPos = new Vector3(initOrbPos.x, yVal, initOrbPos.z);
            float t = 0f;
            while (t < 1f)
            {
                yield return null;
                t += Time.deltaTime * 13f;
                buttons[i + 1].transform.localPosition = Vector3.Lerp(initOrbPos, finalPos, t);
            }
            if (killOrbs)
                buttons[i + 1].gameObject.SetActive(false);
        }
        orbsMoving = false;
    }

    IEnumerator SolveAnim()
    {
        audio.PlaySoundAtTransform("solve", transform);
        orbsMoving = true;
        StartCoroutine(LowerOrbs(.024f, false));
        while (orbsMoving) yield return null;
        Vector3 initClockPos = clockRotater.transform.localEulerAngles;
        Vector3 finalClockPos = new Vector3(0, 0, 90);
        float t = 0f;
        while (t < 1f)
        {
            yield return null;
            t += Time.deltaTime * .21f;
            clockRotater.transform.localEulerAngles = Vector3.Lerp(initClockPos, finalClockPos, t);
        }
    }

    //twitch plays
    #pragma warning disable 414
    private readonly string TwitchHelpMessage = @"!{0} outputbombface [Output the module's bomb face to chat] | !{0} <1-12> <0-3> [Sets the specified orb to the specified number's associated color] | !{0} pedestal/submit [Presses the pedestal] | Multiple orbs can be set in one command by chaining";
    #pragma warning restore 414
    IEnumerator ProcessTwitchCommand(string command)
    {
        if (command.Equals("outputbombface"))
        {
            if (Application.isEditor)
            {
                yield return "sendtochaterror This command cannot be used in the TestHarness.";
                yield break;
            }
            yield return null;
            string bombFaceString = "This module's bomb face (" + bombFaceRowLength + " modules wide, " + bombFaceColLength + " modules tall) in reading order is: ";
            for (int y = 0; y < bombFaceColLength; y++)
            {
                for (int x = 0; x < bombFaceRowLength; x++)
                {
                    if (origBombFaceLayout[x, y] == null)
                        bombFaceString += "N/A, ";
                    else if (moduleX == x && moduleY == y)
                        bombFaceString += "[Hickory Dickory Dock], ";
                    else
                        bombFaceString += origBombFaceLayout[x, y] + ", ";
                }
            }
            bombFaceString = bombFaceString.Substring(0, bombFaceString.Length - 2);
            yield return "sendtochat " + bombFaceString;
            yield return "sendtochat Note that the module itself is in square brackets.";
            yield break;
        }
        if (command.EqualsAny("pedestal", "submit"))
        {
            if (orbsMoving || mode == 0)
            {
                yield return "sendtochaterror The pedestal cannot be pressed right now!";
                yield break;
            }
            yield return null;
            buttons[0].OnInteract();
            yield break;
        }
        string[] parameters = command.Split(' ');
        for (int i = 0; i < parameters.Length; i++)
        {
            int checker;
            if (i % 2 == 0 && (!int.TryParse(parameters[i], out checker) || checker < 1 || checker > 12))
            {
                yield return "sendtochaterror!f The specified orb '" + parameters[i] + "' is invalid!";
                yield break;
            }
            if (i % 2 == 1 && (!int.TryParse(parameters[i], out checker) || checker < 0 || checker > 3))
            {
                yield return "sendtochaterror!f The specified number '" + parameters[i] + "' is invalid!";
                yield break;
            }
        }
        if (parameters.Length % 2 != 0)
        {
            yield return "sendtochaterror You must specify a number for each orb!";
            yield break;
        }
        if (orbsMoving || mode != 1)
        {
            yield return "sendtochaterror The orbs cannot be set to a color right now!";
            yield break;
        }
        yield return null;
        for (int i = 0; i < parameters.Length; i+=2)
        {
            while (selectedColors[i] != int.Parse(parameters[i + 1]))
            {
                buttons[int.Parse(parameters[i])].OnInteract();
                yield return new WaitForSeconds(.1f);
            }
        }
    }

    IEnumerator TwitchHandleForcedSolve()
    {
        if (mode == 3)
        {
            buttons[0].OnInteract();
            yield break;
        }
        if (mode == 2)
        {
            while (orbsMoving) yield return true;
            buttons[0].OnInteract();
        }
        while (mode == 0 || orbsMoving) yield return true;
        for (int i = 0; i < generatedStages.Count; i++)
        {
            while (generatedStages[i].answer != selectedColors[generatedStages[i].chimes - 1])
            {
                buttons[generatedStages[i].chimes].OnInteract();
                yield return new WaitForSeconds(.1f);
            }
        }
        buttons[0].OnInteract();
    }
}

public class Stage
{
    private readonly char[] alphabet = { 'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z' };
    private readonly char[] digits = { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' };

    public string minuteDirection;
    public string moduleName;
    public int index;
    public int answer;
    public int chimes;

    public Stage(string dir, string modName, int ix, int stageNum)
    {
        minuteDirection = dir;
        moduleName = modName;
        index = ix;
        chimes = stageNum;

        char stageChar = moduleName.ToUpper()[index % moduleName.Length];
        if (alphabet.Contains(stageChar))
            answer = (Array.IndexOf(alphabet, stageChar) + 1) % 4;
        else if (digits.Contains(stageChar))
            answer = Array.IndexOf(digits, stageChar) % 4;
        else
            answer = 0;
    }
}