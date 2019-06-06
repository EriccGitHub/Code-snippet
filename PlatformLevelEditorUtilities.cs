using UnityEngine;
using System.Collections.Generic;

public class PlatformLevelEditorUtilities : MonoBehaviour
{
    [SerializeField] GameObject objectToDrag;
    List<GameObject> expendObjects = new List<GameObject>();
    Collider hitObject;

    void Start()
    {
        for (int i = 0; i < GetComponentsInChildren<RemovePlatformExpand>().Length; ++i)
            expendObjects.Add(GetComponentsInChildren<RemovePlatformExpand>()[i].gameObject);
        if (GameManager.instance.currentTheme == Theme.LevelEditor || GameManager.instance.currentTheme == Theme.Sandbox)
        {
            if (objectToDrag)
            {
                objectToDrag.GetComponent<Renderer>().enabled = true;
                objectToDrag.GetComponent<Collider>().enabled = true;
            }
            for (int i = 0; i < expendObjects.Count; ++i)
                DisableEnableExpand(expendObjects[i], true);
        }
        else
        {
            Destroy(objectToDrag);
            for (int i = 0; i < expendObjects.Count; ++i)
                DisableEnableExpand(expendObjects[i], false);
        }
    }

    void OnMouseDown()
    {
        RaycastHit hitInfo;
        if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hitInfo))
            hitObject = hitInfo.collider;

        if (hitObject.gameObject == objectToDrag)
            GameManager.instance.Save();

        if (hitObject.GetComponent<RemovePlatformExpand>() || hitObject.name == "HeightDrag")
            CursorManager.instance.cancelPlacement = true;
    }

    void OnMouseUp()
    {
        if (hitObject.gameObject == objectToDrag)
            GameManager.instance.Save();
    }

    void OnMouseUpAsButton()
    {
        if (!hitObject.GetComponent<RemovePlatformExpand>())
            return;

        for (int i = 0; i < expendObjects.Count; ++i)
        {
            if (hitObject.gameObject == expendObjects[i])
            {
                ExpandPlatform(expendObjects[i].transform);
                break;
            }
        }
    }

    void OnMouseDrag()
    {
        if (objectToDrag)
        {
            if (hitObject.gameObject == objectToDrag)
            {
                float distance_to_screen = Camera.main.WorldToScreenPoint(objectToDrag.transform.position).z;
                objectToDrag.transform.position = new Vector3(objectToDrag.transform.position.x, Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, distance_to_screen)).y, objectToDrag.transform.position.z);
                NotifyUI();
            }
        }
    }

    public void ChangeObjectHeight(float newHeight)
    {
        if (objectToDrag)
            objectToDrag.transform.position = new Vector3(objectToDrag.transform.position.x, newHeight, objectToDrag.transform.position.z);
    }

    public void NotifyUI()
    {
        if (objectToDrag)
            objectToDrag.transform.root.GetComponentInChildren<ModularPlatform>().MaxHeightChanged(objectToDrag.transform.position.y);
    }

    void ExpandPlatform(Transform expand)
    {
        GameManager.instance.Save();
        GameObject newPlatform = transform.root.GetComponentInChildren<ModularPlatform>().SpawnPlatformTop(expand, transform.parent);
        DisableEnableExpand(expand.gameObject, false);
        expand.GetComponent<RemovePlatformExpand>().removedBy = newPlatform.transform;
        RemovePlatformExpand[] newExpands = newPlatform.GetComponentsInChildren<RemovePlatformExpand>();
        for (int i = 0; i < newExpands.Length; ++i)
        {
            if(!expendObjects.Contains(newExpands[i].gameObject))
                expendObjects.Add(newExpands[i].gameObject);
        }
        GameManager.instance.Save();
    }

    public void DisableEnableExpand(GameObject _gameObject, bool enable)
    {
        _gameObject.GetComponent<Renderer>().enabled = enable;
        _gameObject.GetComponent<Collider>().enabled = enable;
        if (!enable)
            expendObjects.Remove(_gameObject);
        else
        {
            if (!expendObjects.Contains(_gameObject))
            {
                expendObjects.Add(_gameObject);
            }
        }
    }
}