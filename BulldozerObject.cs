using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class BulldozerObject : MonoBehaviour {

    public List<Renderer> rends = new List<Renderer>();
    Collider col;
    Collider[] cols;
    [HideInInspector] public bool trashed = false;

    void Start()
    {
        col = GetComponent<Collider>();
        if(!col)
        {
            cols = GetComponentsInChildren<Collider>();
        }
        GetRenderers();
    }

    void GetRenderers()
    {
        rends.Clear();
        for (int i = 0; i < GetComponentsInChildren<Renderer>().Length; ++i)
        {
            if (GetComponentsInChildren<Renderer>()[i].enabled)
                rends.Add(GetComponentsInChildren<Renderer>()[i]);
        }
    }

    private void OnMouseUpAsButton()
    {
        if (SandboxObjectPlacer.instance.bulldozing)
        {
            RaycastHit hitInfo;
            bool cancelBulldoze = false;
            if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hitInfo))
            {
                if(hitInfo.collider.GetComponent<RemovePlatformExpand>() || hitInfo.collider.GetComponent<ModularPlatform>() || EventSystem.current.IsPointerOverGameObject())
                    cancelBulldoze = true;
            }
            if (!cancelBulldoze && !GameManager.instance.isPaused)
            {
                AudioManager.instance.PlaySound(SandboxObjectPlacer.instance.deleteObjectSFX);
                Bulldoze();
            }
        }
    }

    public void Bulldoze(bool saving = true)
    {
        if(saving)
            GameManager.instance.Save();
        
        if (GetComponent<PlatformWaypoint>())
            GetComponent<PlatformWaypoint>().BulldozeTracks(true);

        if (col)
        {
            if(GetComponent<ModularPlatformUnderCollision>()) //if it's a platform top, we need to put all of its child's parent under its own parent
            {
                RemovePlatformExpand[] expands = FindObjectsOfType<RemovePlatformExpand>();
                for (int i = 0; i < expands.Length; ++i)
                    expands[i].StartCoroutine(expands[i].LookForNeighbour());
            }
            col.enabled = false;
        }
        else //The modular platform has mutliple collider and they're all on its childrens
        {
            for (int i = 0; i < cols.Length; ++i)
                cols[i].enabled = false;
        }
        GetRenderers();
        for (int i = 0; i < rends.Count; ++i)
            rends[i].enabled = false;

        for (int i = 0; i < GetComponentsInChildren<MeshCollider>().Length; ++i)
            GetComponentsInChildren<MeshCollider>()[i].enabled = false;

        if (GetComponentInChildren<Canvas>()) //canvas UI follow of the cube
            GetComponentInChildren<Canvas>().transform.GetChild(0).gameObject.SetActive(false);

        trashed = true;
        SandboxObjectPlacer.instance.LookIfCopyGameobject();
        GameManager.instance.sandboxObjects.Remove(gameObject);
        if(saving)
            GameManager.instance.Save();
    }

    public void RestoreObject()
    {
        if (!col)
            col = GetComponent<Collider>();
        if(col)
            col.enabled = true;
        else
        {
            for (int i = 0; i < cols.Length; ++i)
                cols[i].enabled = true;
        }
        if (!GetComponent<PlatformWaypoint>())
        {
            for (int i = 0; i < rends.Count; ++i)
                rends[i].enabled = true;
        }

        for (int i = 0; i < GetComponentsInChildren<MeshCollider>().Length; ++i)
            GetComponentsInChildren<MeshCollider>()[i].enabled = true;

        if (GetComponentInChildren<Canvas>())
            GetComponentInChildren<Canvas>().transform.GetChild(0).gameObject.SetActive(true);
    }
}