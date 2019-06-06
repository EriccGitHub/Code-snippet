using UnityEngine;

public class ObjectScaler : ToolBase {

	[SerializeField] private float scalerDemultiplier = 30f;
	[SerializeField] private float maxScale = 5f;
	[SerializeField] private float massMultiplier = 2f;
    [HideInInspector] public bool trashed = false;
    private GameObject attachedObj;

	public override void Spawn (GameObject prefab, GameObject hit, Vector3 mousePos, Quaternion mouseRot)
	{
		base.Spawn (prefab, hit, mousePos, mouseRot);
		placedPrefab.GetComponent<ObjectScaler>().attachedObj = hit;
        GameManager.instance.objectScalers.Add(placedPrefab);
	}

	public override void Start ()
	{
		base.Start ();
		transform.SetParent(attachedObj.transform);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.M))
        {
            if (jammed)
            {
                FeedbackManager.instance.SpawnWarning("This tool is jammed");
            }
        }
    }

    void FixedUpdate()
	{
		if (Input.GetKey (KeyCode.M) && !jammed && GetComponent<Renderer>().enabled)
		{
			if (attachedObj.transform.localScale.x <= maxScale)
			{
				Done ();
				Scale ();
                UndoManager.instance.ResetUndo();
            }
		}
	}

	void Scale()
	{
        attachedObj.transform.localScale *= scalerDemultiplier;
		attachedObj.GetComponent<Rigidbody> ().mass = attachedObj.transform.localScale.x * massMultiplier;
        ObjectScaler[] scalersOnObject = attachedObj.GetComponentsInChildren<ObjectScaler>();
        for (int i = 0; i < scalersOnObject.Length; ++i)
            ResetScale(scalersOnObject[i].transform);
    }

    public void ResetScale(Transform _transform)
    {
        _transform.localScale /= scalerDemultiplier;
    }

    public override void Delete(bool saving = true, bool destroy = true)
    {
        GameManager.instance.objectScalers.Remove(gameObject);
        base.Delete(saving);
        KillTool();
        if (destroy)
        {
            Destroy(gameObject);
            trashed = false;
        }
        else
        { 
            trashed = true;
        }

        if (saving)
            GameManager.instance.Save();
    }

	public void KillTool()
	{
        if (!done)
        {
            GameObject _spendingCanvas = Instantiate(Resources.Load("Canvas_Spending") as GameObject, transform.position, Quaternion.identity);
            _spendingCanvas.GetComponent<SpendingUI>().itemAmount = 1;
        }
        GetComponent<MeshRenderer> ().enabled = false;
		GetComponent<BoxCollider> ().enabled = false;
		GetComponent<UndoSystem_ObjectScaler> ().StoreDeletedObject ();

	}

	public void RestoreTool()
	{
		GetComponent<MeshRenderer>().enabled = true;
		GetComponent<BoxCollider>().enabled = true;
        GameManager.instance.objectScalers.Add (gameObject);
		if (!done)
		{
			base.Start();
		}

	}

}
