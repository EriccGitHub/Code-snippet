using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using LapinerTools.Steam.UI;
using LapinerTools.Steam.Data;
using LapinerTools.uMyGUI;

public class SaveLoadLevelEditor : MonoBehaviour
{
    #region singleton
    static private SaveLoadLevelEditor _instance;
    static public SaveLoadLevelEditor instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<SaveLoadLevelEditor>();
            }
            return _instance;
        }
    }
    #endregion singleton

    [SerializeField] GameObject baroPrefab;
    [SerializeField] TMP_InputField[] rulesInputs;
    [SerializeField] GameObject baroToolsPanel;
    [SerializeField] GameObject sandboxToolsPanel;
    [SerializeField] GameObject rulesPanel;
    [SerializeField] GameObject editButton;
    [SerializeField] GameObject gifButton;
    [SerializeField] GameObject saveLevelPanel;
    public GameObject loadLevelPanel;
    [SerializeField] GameObject confirmationCanvas;
    [SerializeField] Text LevelNameText;
    ToolBarGUI InitToolsUI;
    PopulateToolsUI populateToolsUI;
    List<List<SingleUndo_SandboxObjets>> allUndos = new List<List<SingleUndo_SandboxObjets>>();
    Transform baroSpawn;
    [HideInInspector] public bool inPlayMode = false;
    [HideInInspector] public string filename = "NewLevel";
    string completeJSON;
    [HideInInspector] public ulong levelId = 0;
    [HideInInspector] public bool playingADownloadedLevel = false;
    [HideInInspector] public string levelPath = null;
    string saveLocation;
    enum Action { DELETE, SAVE, UPLOAD, NONE};
    Action actionToConfirm = Action.NONE;

    void Awake()
    {
        GameManager.instance.hideNeededCanvas = true;
    }

    void Start()
    {
        TriggerUI();
        PointSystem.instance.scoreText.transform.parent.gameObject.SetActive(false);
        saveLocation = Application.persistentDataPath;
        if (!string.IsNullOrEmpty(GameManager.instance.levelEditorLevelPath))
        {
            if (GameManager.instance.levelEditorLevelId != 0)
            {
                if (GameManager.instance.levelEditorLevelDownloaded)
                {
                    playingADownloadedLevel = true;
                    StartCoroutine(LatePlay());
                }
                levelId = GameManager.instance.levelEditorLevelId;
            }
            levelPath = GameManager.instance.levelEditorLevelPath;
            LoadLevel(GameManager.instance.levelEditorLevelPath + "\\ItemData.json");
            GameManager.instance.levelEditorLevelPath = null;
            GameManager.instance.levelEditorLevelId = 0;
            GameManager.instance.levelEditorLevelDownloaded = false;
            editButton.SetActive(false);
            GameManager.instance.isPaused = false;
        }
        else
        {
            CreateEmptySaveFile();
        }
        InitToolsUI = FindObjectOfType<ToolBarGUI>();
        populateToolsUI = FindObjectOfType<PopulateToolsUI>();
        ResetCursor();
    }

    void Update()
    {
        if (Input.GetButtonDown("Lift") && !inPlayMode && !GameManager.instance.isPaused)
            PlayButton();
    }

    IEnumerator LatePlay()
    {
        //For some reason if I was doing PlayButton(); in the start it fucked the starting tools amount so...
        yield return null;
        PlayButton();
        editButton.SetActive(false);
    }

    void TriggerUI()
    {
        SetCanvasGroupVisible(rulesPanel.GetComponent<CanvasGroup>(), !inPlayMode);
        SetCanvasGroupVisible(sandboxToolsPanel.GetComponent<CanvasGroup>(), !inPlayMode);
        SetCanvasGroupVisible(baroToolsPanel.GetComponent<CanvasGroup>(), inPlayMode);
        editButton.SetActive(inPlayMode);
        if(gifButton)
            gifButton.SetActive(inPlayMode);
    }

    public void TriggerWinningUI(bool enable)
    {
        editButton.SetActive(enable);
    }

    public void OpenCloseSaveOptions(bool opening)
    {
        OpenClosePanel(opening, saveLevelPanel);
        if (opening)
            SetSaveInfo();
    }

    public void OpenCloseLoadOptions(bool opening)
    {
        loadLevelPanel.SetActive(opening);
        GameManager.instance.isPaused = opening;
    }

    void OpenClosePanel(bool opening, GameObject panel)
    {
        SetCanvasGroupVisible(panel.GetComponent<CanvasGroup>(), opening);
        GameManager.instance.isPaused = opening;
    }

    public void SetCanvasGroupVisible(CanvasGroup canvas, bool visible)
    {
        canvas.alpha = visible ? 1 : 0;
        canvas.interactable = visible;
        canvas.blocksRaycasts = visible;
    }

    void SetSaveInfo()
    {
        if (CurrentIsNewLevel())
            return;

        LevelInfo levelInfo = GetCreatedLevel(levelId);
        SteamWorkshopUIUpload.Instance.NAME_INPUT.text = levelInfo.name;
        SteamWorkshopUIUpload.Instance.DESCRIPTION_INPUT.text = levelInfo.description;
        SteamWorkshopUIUpload.Instance.ICON.texture = LoadThumbnail(levelInfo.thumbnailPath);
        SteamWorkshopUIUpload.Instance.SetLevelInfo(levelInfo.name, levelInfo.description, levelInfo.thumbnailPath);
    }

    void SaveLevel()
    {
        completeJSON = string.Empty;
        ObjectData[] objs = FindObjectsOfType<ObjectData>();
        LevelData level = new LevelData();
        int nbOfGreens = 0;
        for (int i = 0; i < objs.Length; i++)
        {
            Renderer renderer;
            if (objs[i].GetComponentInChildren<ModularPlatform>())
                renderer = objs[i].GetComponentsInChildren<Renderer>()[1];
            else
                renderer = objs[i].GetComponentInChildren<Renderer>();

            if (!renderer.enabled)
                continue;

            SetPrefixJSON("ObjectInfo");
            ObjectInfo info = new ObjectInfo();
            objs[i].objectID = objs[i].GetInstanceID();
            info.objectID = objs[i].objectID;
            info.position = objs[i].transform.position;
            info.rotation = objs[i].transform.eulerAngles;
            info.scale = objs[i].transform.localScale;
            info.prefab = objs[i].prefab.name;
            info.toolName = objs[i].toolName;
            if (objs[i].GetComponent<ShapeScript>())
            {
                ShapeScript shapeScript = objs[i].GetComponent<ShapeScript>();
                if (shapeScript.isGreen)
                {
                    info.isCube = "isGreen";
                    ++nbOfGreens;
                }
                else if (shapeScript.isHeavy)
                    info.isCube = "isHeavy";
                else if (shapeScript.isNight)
                    info.isCube = "isNight";
                else if (shapeScript.isRed)
                    info.isCube = "isRed";
                else if (objs[i].GetComponent<Rigidbody>().isKinematic)
                    info.isCube = "isKinematic";
                else
                    info.isCube = "isNormal";
            }
            else if (objs[i].GetComponentInChildren<ModularPlatform>())
                info.platformInfo = FillPlatformInfo(objs[i].gameObject);

            level.objectsInfoList.Add(info);
            AddToJSON(info);
        }
        #region neededToWin
        {
            SetPrefixJSON("Rule");
            Rule winRule = new Rule()
            {
                neededToWin = nbOfGreens.ToString()
            };
            AddToJSON(winRule);
        }
        #endregion
        for (int i = 0; i < rulesInputs.Length; ++i)
        {
            SetPrefixJSON("Rule");
            Rule rule = new Rule()
            {
                toolName = rulesInputs[i].name,
                toolAmount = rulesInputs[i].text
            };
            AddToJSON(rule);
        }
        LookForSaveFileLocation();
    }

    void LookForSaveFileLocation()
    {
        if (!SteamWorkshopUIUpload.Instance.IsAllInfoFilled())
            return;

        string path = Path.Combine(saveLocation, levelId.ToString());
        if (!CurrentIsNewLevel())
        {
            GameObject _canvas = SpawnConfirmationCanvas();
            Vector3 normalPosition = _canvas.GetComponent<ConfirmationCanvas>().textToShow.rectTransform.position;
            _canvas.GetComponent<ConfirmationCanvas>().textToShow.rectTransform.position = new Vector3(normalPosition.x, normalPosition.y - 20, normalPosition.z);
            _canvas.GetComponent<ConfirmationCanvas>().overrideSaveText.gameObject.SetActive(true);
            _canvas.GetComponent<ConfirmationCanvas>().receiver = this;
            SetCanvasGroupVisible(saveLevelPanel.GetComponent<CanvasGroup>(), false);
        }
        else
        {
            SaveLevelToFile();
            if (actionToConfirm == Action.UPLOAD)
            {
                SteamWorkshopUIUpload.Instance.UploadButtonClicked();
                actionToConfirm = Action.NONE;
            }
        }
    }

    public GameObject SpawnConfirmationCanvas()
    {
        return Instantiate(confirmationCanvas);
    }

    bool CurrentIsNewLevel()
    {
        string path = Path.Combine(saveLocation, levelId.ToString());
        if (Directory.Exists(path))
        {
            string[] files = Directory.GetFiles(path);
            return files.Length == 0;
        }
        return false;
    }

    public void SaveLevelToFile()
    {
        if (!SteamWorkshopUIUpload.Instance.IsAllInfoFilled())
            return;

        string path = Path.Combine(saveLocation, levelId.ToString());
        if (Directory.Exists(path))
        {
            string[] files = Directory.GetFiles(path);
            for (int i = 0; i < files.Length; ++i)
            {
                string copyTo = Path.Combine(Directory.GetParent(path).FullName, "thumbnail.jpeg");
                if (File.Exists(copyTo))
                    File.Delete(copyTo);

                if (files[i].EndsWith("thumbnail.jpeg"))
                    File.Copy(files[i], copyTo);

                File.Delete(files[i]);
            }
            Directory.Delete(path);
        }

        Directory.CreateDirectory(path);
        File.WriteAllText(Path.Combine(path, "ItemData.json"), completeJSON);
        WorkshopItemUpdate createNewItemUsingGivenFolder = new WorkshopItemUpdate();
        createNewItemUsingGivenFolder.ContentPath = path;
        createNewItemUsingGivenFolder.IconPath = path + "\\thumbnail.jpeg";
        SteamWorkshopUIUpload.Instance.SetItemData(createNewItemUsingGivenFolder);
        FileInfo[] filesInParent = Directory.GetParent(path).GetFiles();
        for (int i = 0; i < filesInParent.Length; ++i)
        {
            if (filesInParent[i].FullName.EndsWith("thumbnail.jpeg") || filesInParent[i].FullName.EndsWith("thumbnail"))
            {
                string copyTo = string.Empty;
                if (filesInParent[i].FullName.EndsWith("thumbnail.jpeg"))
                    copyTo = filesInParent[i].Name;
                else
                    copyTo = filesInParent[i].Name + ".jpeg";

                filesInParent[i].CopyTo(Path.Combine(path, copyTo));
                File.Delete(filesInParent[i].FullName);
                break;
            }
        }
        string infoJson = SteamWorkshopUIUpload.Instance.TransferInfoToJson(path);
        File.WriteAllText(Path.Combine(path, "LevelInfo.json"), infoJson);
        File.WriteAllText(Path.Combine(path, "LevelTheme.txt"), SceneManager.GetActiveScene().name);
        OpenCloseSaveOptions(false);
        ((uMyGUI_PopupText)uMyGUI_PopupManager.Instance.ShowPopup(uMyGUI_PopupManager.POPUP_TEXT))
                    .SetText("Item Saved", "Item '" + SteamWorkshopUIUpload.Instance.NAME_INPUT.text + "' was successfully saved!")
                    .ShowButton(uMyGUI_PopupManager.BTN_OK);
    }

    void CreateEmptySaveFile()
    {
        levelId = GenerateLevelId(saveLocation);
        Directory.CreateDirectory(Path.Combine(saveLocation, levelId.ToString()));
    }

    ulong GenerateLevelId(string pathToLook)
    {
        bool idIsUnique = false;
        int levelId = 0;
        int iterations = 0;
        int maxLevelIdPossible = 30000;
        string[] directories = Directory.GetDirectories(pathToLook);
        while (!idIsUnique || iterations <= maxLevelIdPossible)
        {
            ++iterations;
            levelId = Random.Range(1, maxLevelIdPossible);
            bool idExists = false;
            for (int i = 0; i < directories.Length; ++i)
            {
                if (directories[i] == pathToLook + "\\" + levelId.ToString())
                {
                    idExists = true;
                    break;
                }
            }
            if (!idExists)
                idIsUnique = true;
        }
        return (ulong)levelId;
    }

    string[] GetAllCreatedLevels()
    {
        return Directory.GetDirectories(saveLocation);
    }

    LevelInfo GetCreatedLevel(ulong levelId)
    {
        string[] createdLevels = GetAllCreatedLevels();
        for(int i=0;i<createdLevels.Length;++i)
        {
            if(createdLevels[i].EndsWith(levelId.ToString()))
            {
                string[] files = Directory.GetFiles(createdLevels[i]);
                for(int j=0;j<files.Length;++j)
                {
                    if(files[j].EndsWith("LevelInfo.json"))
                    {
                        string[] lines = File.ReadAllLines(files[j]);
                        LevelInfo levelInfo = JsonUtility.FromJson<LevelInfo>(lines[0]);
                        return levelInfo;
                    }
                }
            }
        }
        return null;
    }

    LevelInfo GetDownloadedLevel(string levelPath)
    {
        string[] lines = File.ReadAllLines(levelPath);
        return JsonUtility.FromJson <LevelInfo>(lines[0]);
    }

    void SetPrefixJSON(string name)
    {
        ClassJSON classJSON = new ClassJSON()
        {
            className = name
        };
        AddToJSON(classJSON);
    }

    void AddToJSON(object add)
    {
        string json = JsonUtility.ToJson(add);
        completeJSON += json + System.Environment.NewLine;
        //File.AppendAllText(@filename, json + Environment.NewLine);
    }

    public void LoadLevel(string jsonFilePath)
    {
        string[] lines = File.ReadAllLines(jsonFilePath);
        for (int i = 0; i < lines.Length; i++)
        {
            ClassJSON classJSON = JsonUtility.FromJson<ClassJSON>(lines[i++]);
            switch(classJSON.className)
            {
                case "ObjectInfo":
                    ObjectInfo info = JsonUtility.FromJson<ObjectInfo>(lines[i]);
                    GameObject clone = Instantiate(SandboxObjectPlacer.instance.FindPrefab(info.prefab), info.position, Quaternion.Euler(info.rotation));
                    clone.name = info.prefab;
                    clone.transform.localScale = info.scale;
                    clone.AddComponent<ObjectData>().prefab = SandboxObjectPlacer.instance.FindPrefab(info.prefab);
                    clone.GetComponent<ObjectData>().toolName = info.toolName;
                    clone.AddComponent<BulldozerObject>();
                    if (!string.IsNullOrEmpty(info.isCube))
                        SetCube(clone, info.isCube);
                    else if (info.platformInfo.speed != -1)
                    {
                        info.platformInfo.platformGameObject = clone;
                        SetPlatform(info.platformInfo);
                    }
                    
                    if(!GameManager.instance.sandboxObjects.Contains(clone))
                        GameManager.instance.sandboxObjects.Add(clone);

                    clone.GetComponent<UndoSystem_SandboxObjects>().placedOnStart = true;
                    break;

                case "Rule":
                    Rule rule = JsonUtility.FromJson<Rule>(lines[i]);
                    SetInitTools(rule.toolName, rule.toolAmount);
                    if (!string.IsNullOrEmpty(rule.neededToWin))
                        PointSystem.instance.pointsToWin = int.Parse(rule.neededToWin);

                    break;
            }
        }
        if (!playingADownloadedLevel)
            LevelNameText.text = GetCreatedLevel(levelId).name;
        else
            LevelNameText.text = GetDownloadedLevel(GameManager.instance.levelEditorLevelPath + "\\LevelInfo.json").name;
        GameManager.instance.Save();
    }

    Tool SetInitTools(string toolName, string toolAmount)
    {
        if (string.IsNullOrEmpty(toolAmount))
            return null;

        int amount = int.Parse(toolAmount);
        switch (toolName)
        {
            case "SmallBomb":
                PointSystem.instance.bombsAmount = amount;
                PointSystem.instance.initBombsAmount = amount;
                rulesInputs[0].text = toolAmount;
                break;
            case "Grapple":
                PointSystem.instance.grappleAmount = amount;
                PointSystem.instance.initGrappleAmount = amount;
                rulesInputs[1].text = toolAmount;
                break;
            case "HotAirBalloon":
                PointSystem.instance.habAmount = amount;
                PointSystem.instance.initHabAmount = amount;
                rulesInputs[2].text = toolAmount;
                break;
            case "SupportPole":
                PointSystem.instance.supportAmount = amount;
                PointSystem.instance.initSupportAmount = amount;
                rulesInputs[3].text = toolAmount;
                break;
            case "WarpZone":
                PointSystem.instance.warpAmount = amount;
                PointSystem.instance.initWarpAmount = amount;
                rulesInputs[4].text = toolAmount;
                break;
            case "ObjectScaler":
                PointSystem.instance.scalerAmount = amount;
                PointSystem.instance.initScalerAmount = amount;
                rulesInputs[5].text = toolAmount;
                break;
        }
        return SandboxObjectPlacer.instance.FindTool(toolName);
    }

    void SetCube(GameObject cube, string isWhat)
    {
        switch(isWhat)
        {
            case "isGreen":
                cube.GetComponent<ShapeScript>().isGreen = true;
                cube.GetComponent<ShapeScript>().RefreshCube();
                break;
            case "isHeavy":
                cube.GetComponent<ShapeScript>().isHeavy = true;
                cube.GetComponent<ShapeScript>().RefreshCube();
                break;
            case "isNight":
                cube.GetComponent<ShapeScript>().isNight = true;
                cube.GetComponent<ShapeScript>().RefreshCube();
                break;
            case "isRed":
                cube.GetComponent<ShapeScript>().isRed = true;
                cube.GetComponent<ShapeScript>().RefreshCube();
                break;
            case "isKinematic":
                cube.GetComponent<Rigidbody>().isKinematic = true;
                break;
            case "isNormal":
                cube.GetComponent<ShapeScript>().RefreshCube();
                break;
        }
    }

    void SetPlatform(PlatformInfo platformInfo)
    {
        ModularPlatform console = platformInfo.platformGameObject.GetComponentInChildren<ModularPlatform>();
        console.ChangeSpeed(platformInfo.speed);
        console.ChangeMaxHeight(platformInfo.maxHeight);
        console.ControllableToggle(platformInfo.controllable);
        console.transform.position = platformInfo.consolePosition;
        console.transform.rotation = platformInfo.consoleRotation;
        console.currentTarget = platformInfo.currentTarget;
        console.gameObject.AddComponent<BulldozerObject>();
        platformInfo.platformGameObject.GetComponentInChildren<ModularPlatformUnderCollision>().transform.position = platformInfo.platformPosition;
        for (int i = 0; i < platformInfo.waypoints.Count; ++i)
        {
            GameObject TempTarget = new GameObject();
            TempTarget.transform.Rotate(new Vector3(-90, 0, 0));
            TempTarget.transform.position = platformInfo.waypoints[i];
            Transform newTarget = console.PlaceTarget(TempTarget.transform);
            newTarget.gameObject.AddComponent<PlatformWaypoint>().indexInList = i;
            newTarget.gameObject.AddComponent<BulldozerObject>();
            Destroy(TempTarget);
        }
        for(int i=0;i<platformInfo.platformTops.Count;++i)
            console.SpawnPlatformTop(platformInfo.platformTops[i]);

        platformInfo.platformGameObject.GetComponentInChildren<UndoSystem_ModularPlatform>().enabled = false;
    }

    public void DeleteAllClicked()
    {
        GameObject _canvas = Instantiate(confirmationCanvas);
        Vector3 normalPosition = _canvas.GetComponent<ConfirmationCanvas>().textToShow.rectTransform.position;
        _canvas.GetComponent<ConfirmationCanvas>().textToShow.rectTransform.position = new Vector3(normalPosition.x, normalPosition.y - 20, normalPosition.z);
        _canvas.GetComponent<ConfirmationCanvas>().destroyAllText.gameObject.SetActive(true);
        _canvas.GetComponent<ConfirmationCanvas>().receiver = this;
        actionToConfirm = Action.DELETE;
    }

    void DeleteAll()
    {
        ObjectData[] allObjects = FindObjectsOfType<ObjectData>();
        for (int i = allObjects.Length - 1; i >= 0; --i)
        {
            Destroy(allObjects[i].gameObject);
        }
        GameManager.instance.sandboxObjects.Clear();
        SandboxObjectPlacer.instance.ClosePropertyPanel();
    }

    public void PlayButton()
    {
        for (int i = 0; i < rulesInputs.Length; ++i)
            UnlockToolIfLocked(SetInitTools(rulesInputs[i].name, rulesInputs[i].text));

        InitToolsUI.PopulateMenu();
        populateToolsUI.UnPopulateMenu();
        populateToolsUI.PopulateMenu();
        populateToolsUI.RemoveBeaconUI();
        populateToolsUI.PlaceBeaconUI();
        if (!baroSpawn)
        {
            Transform spawn;
            if (FindSpawn(out spawn))
                baroSpawn = spawn;
            else
            {
                FeedbackManager.instance.SpawnWarning("Need a spawn point");
                return;
            }
        }
        ResetModularPlatforms(true);
        GameObject baro = Instantiate(baroPrefab, baroSpawn.position, Quaternion.identity);
        baro.transform.eulerAngles = new Vector3(0, baroSpawn.eulerAngles.y, 0);
        SetWinCondition();
        StartCoroutine(Camera.main.GetComponent<RotateToMouse>().Findbaro());
        inPlayMode = true;
        SwapUndos();
        TriggerUI();
        TriggerEditorOnlyObjects();
        ResetCursor();
        SandboxObjectPlacer.instance.StartCoroutine(SandboxObjectPlacer.instance.LatePropertyReset());
        SandboxObjectPlacer.instance.ClosePropertyPanel();
        PointSystem.instance.SpawnNeededToWinCanvas();
        PointSystem.instance.scoreNeededString = "/" + PointSystem.instance.pointsToWin;
        PointSystem.instance.scoreText.transform.parent.gameObject.SetActive(true);
        GameManager.instance.Save();
    }

    void UnlockToolIfLocked(Tool toolToUnlock)
    {
        if (toolToUnlock)
        {
            if (!ToolManager.instance.unlockedTools.Contains(toolToUnlock))
                ToolManager.instance.unlockedTools.Add(toolToUnlock);

            if (toolToUnlock.name == "SmallBomb")
            {
                toolToUnlock = SandboxObjectPlacer.instance.FindTool("MediumBomb");
                if (toolToUnlock)
                {
                    if (!ToolManager.instance.unlockedTools.Contains(toolToUnlock))
                        ToolManager.instance.unlockedTools.Add(toolToUnlock);
                }
            }
        }
    }

    public bool FindSpawn(out Transform spawnTransform)
    {
        spawnTransform = null;
        ObjectData[] objectsData = FindObjectsOfType<ObjectData>();
        for (int i = 0; i < objectsData.Length; ++i)
        {
            if (objectsData[i].name.Contains("SpawnPoint"))
            {
                spawnTransform = objectsData[i].transform;
                return true;
            }
        }
        return false;
    }

    void TriggerEditorOnlyObjects()
    {
        baroSpawn.gameObject.SetActive(!inPlayMode);
        ModularPlatformUnderCollision[] platforms = FindObjectsOfType<ModularPlatformUnderCollision>();
        for (int i = 0; i < platforms.Length; ++i)
        {
            Transform heightDrag = platforms[i].transform.Find("HeightDrag");
            if (heightDrag)
            {
                heightDrag.GetComponent<Renderer>().enabled = !inPlayMode;
                heightDrag.GetComponent<Collider>().enabled = !inPlayMode;
            }
            RemovePlatformExpand[] expands = platforms[i].GetComponentsInChildren<RemovePlatformExpand>();
            for (int j = 0; j < expands.Length; ++j)
                expands[j].GetComponentInParent<PlatformLevelEditorUtilities>().DisableEnableExpand(expands[j].gameObject, !inPlayMode);
        }
        ModularPlatform[] consoles = FindObjectsOfType<ModularPlatform>();
        for(int i=0;i<consoles.Length;++i)
        {
            PlatformWaypoint[] waypoints = consoles[i].transform.root.GetComponentsInChildren<PlatformWaypoint>();
            for (int j = 0; j < waypoints.Length; ++j)
            {
                waypoints[j].GetComponent<Renderer>().enabled = !inPlayMode;
                waypoints[j].GetComponent<Collider>().enabled = !inPlayMode;
            }
        }
    }

    void ResetModularPlatforms(bool goingInPlayMode)
    {
        ModularPlatform[] consoles = FindObjectsOfType<ModularPlatform>();
        for (int i = 0; i < consoles.Length; ++i)
        {
            GameObject platform = consoles[i].transform.root.GetComponentInChildren<ModularPlatformUnderCollision>().gameObject;
            if (goingInPlayMode)
            {
                consoles[i].initialHeight = platform.transform.localPosition.z;
                consoles[i].TriggerUxArrows(true);
            }
            platform.transform.position = new Vector3(consoles[i].target[0].position.x, consoles[i].initialHeight, consoles[i].target[0].position.z);
            if (consoles[i].target.Count > 1)
                consoles[i].currentTarget = 1;
            if (!goingInPlayMode && consoles[i].isBeingUsed)
            {
                consoles[i].OnTriggerExit(FindObjectOfType<Baro>().GetComponent<Collider>());
                Camera.main.GetComponent<RotateToMouse>().following = true;
            }
        }
    }

    public void EditButton()
    {
        GameManager.instance.TrashAll(true, false);
        InitToolsUI.UnpopulateTools();
        if (GameManager.instance.isBeaconActive)
        {
            GameManager.instance.isBeaconActive = false;
            Destroy(FindObjectOfType<BeaconEmissive>().gameObject);
            PointSystem.instance.radiusSprite.enabled = false;
        }
        inPlayMode = false;
        SwapUndos();
        GameObject baro = FindObjectOfType<Baro>().gameObject;
        ResetModularPlatforms(false);
        Destroy(baro);
        if (PointSystem.instance.baroDied)
        {
            PointSystem.instance.EditAfterDeath();
            Destroy(FindObjectOfType<PurchasableItemsSetup>().gameObject); //Baro's remote
        }
        TriggerEditorOnlyObjects();
        TriggerUI();
        ResetCursor();
        PointSystem.instance.GetComponentInChildren<SpriteRenderer>().enabled = false;
        Destroy(PointSystem.instance.spawned);
        PointSystem.instance.scoreText.transform.parent.gameObject.SetActive(false);
        GameManager.instance.Save();
    }

    void SwapUndos()
    {
        for (int i = 0; i < GameManager.instance.sandboxObjects.Count; ++i)
        {
            GameManager.instance.sandboxObjects[i].GetComponent<UndoSystem_SandboxObjects>().SwapAllUndos(inPlayMode);
            if(GameManager.instance.sandboxObjects[i].GetComponent<UndoSystem_Mine>())
                GameManager.instance.sandboxObjects[i].GetComponent<UndoSystem_Mine>().SwapAllUndos(inPlayMode);

            for (int j = 0; j < GameManager.instance.sandboxObjects[i].GetComponentsInChildren<UndoSystem_Quai>().Length; ++j)
                GameManager.instance.sandboxObjects[i].GetComponentsInChildren<UndoSystem_Quai>()[j].SwapAllUndos(inPlayMode);
        }
    }

    void ResetCursor()
    {
        CursorManager.instance.prefabMouse.SetActive(false);
        CursorManager.instance.CursorInRange();
        SandboxObjectPlacer.instance.StopCopying();
        SandboxObjectPlacer.instance.selecting = false;
        SandboxObjectPlacer.instance.bulldozing = false;
        SandboxObjectPlacer.instance.eyeDropping = false;
        GameManager.instance.selectedTool = null;
    }

    void SetWinCondition()
    {
        int nbOfGreens = 0;
        for(int i=0;i< GameManager.instance.sandboxObjects.Count;++i)
        {
            if (GameManager.instance.sandboxObjects[i].GetComponent<CollectCubes>())
            {
                if (!GameManager.instance.sandboxObjects[i].GetComponent<ShapeScript>())
                    GameManager.instance.sandboxObjects[i].AddComponent<ShapeScript>().isGreen = true;
                if (GameManager.instance.sandboxObjects[i].GetComponent<ShapeScript>().isGreen)
                    ++nbOfGreens;
            }
        }
        PointSystem.instance.pointsToWin = nbOfGreens;
    }

    public PlatformInfo FillPlatformInfo(GameObject from)
    {
        ModularPlatform console = from.GetComponentInChildren<ModularPlatform>();
        PlatformInfo platformInfo = new PlatformInfo()
        {
            platformGameObject = from.gameObject,
            speed = console.GetSpeed(),
            maxHeight = console.GetMaxHeight(),
            currentTarget = console.currentTarget,
            controllable = !console.autoPilot,
            consolePosition = console.gameObject.transform.position,
            consoleRotation = console.gameObject.transform.rotation,
            platformPosition = from.GetComponentInChildren<ModularPlatformUnderCollision>().transform.position
        };
        for (int i = 0; i < console.target.Count; ++i)
            platformInfo.waypoints.Add(console.target[i].position);

        RemovePlatformExpand[] expands = from.GetComponentsInChildren<RemovePlatformExpand>();
        for (int i=0;i<expands.Length;++i)
        {
            for (int j = 0; j < expands[i].transform.childCount; ++j)
            {
                PlatformTopInfo platformTop = new PlatformTopInfo()
                {
                    serializedTransform = new PlatformTopTransform()
                    {
                        position = expands[i].transform.GetChild(j).transform.position,
                        rotation = expands[i].transform.GetChild(j).transform.rotation
                    },
                    parent = new PlatformTopTransform()
                    {
                        position = expands[i].transform.position,
                        rotation = expands[i].transform.rotation,
                    }

                };
                platformInfo.platformTops.Add(platformTop);
            }
        }
        return platformInfo;
    }

    public void ResetLevel()
    {
        if (playingADownloadedLevel)
            GameManager.instance.LoadLevelEditor(levelPath, levelId);
        else
            GameManager.instance.LoadLevelEditor(levelPath, 0, (int)levelId);
    }

    void OnDestroy()
    {
        string[] files = Directory.GetFiles(saveLocation);
        for (int i = 0; i < files.Length; ++i)
        {
            if (files[i].EndsWith("thumbnail.jpeg"))
                File.Delete(files[i]);
        }
        string[] directories = Directory.GetDirectories(saveLocation);
        for (int i = 0; i < directories.Length; ++i)
        {
            if (Directory.GetFiles(directories[i]).Length == 0 && Directory.GetDirectories(directories[i]).Length == 0)
                Directory.Delete(directories[i]);
        }
        CursorManager.instance.SetCursorStyle("Cursor");
    }

    Texture2D LoadThumbnail(string thumbnailPath)
    {
        Texture2D tex = null;
        byte[] fileData;

        if (File.Exists(thumbnailPath))
        {
            fileData = File.ReadAllBytes(thumbnailPath);
            tex = new Texture2D(2, 2);
            tex.LoadImage(fileData);
        }
        return tex;
    }

    public void SaveButton()
    {
        actionToConfirm = Action.SAVE;
        SaveLevel();
    }

    public void UploadButton()
    {
        actionToConfirm = Action.UPLOAD;
        SaveLevel();
    }

    public void Yes()
    {
        switch(actionToConfirm)
        {
            case Action.SAVE:
                SaveLevelToFile();
                break;
            case Action.UPLOAD:
                SaveLevelToFile();
                SteamWorkshopUIUpload.Instance.UploadButtonClicked();
                break;
            case Action.DELETE:
                DeleteAll();
                break;
        }
        actionToConfirm = Action.NONE;
    }

    public void No()
    {
        if(actionToConfirm == Action.SAVE || actionToConfirm == Action.UPLOAD)
            SetCanvasGroupVisible(saveLevelPanel.GetComponent<CanvasGroup>(), true);

        actionToConfirm = Action.NONE;
    }
}