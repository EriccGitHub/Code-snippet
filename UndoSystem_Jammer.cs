using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UndoSystem_Jammer : UndoSystem
{
    List<SingleUndo_Jammer> allUndos = new List<SingleUndo_Jammer>();

    public override void SaveStartOfLevel()
    {
        base.SaveStartOfLevel();
        SingleUndo_Jammer levelStart = new SingleUndo_Jammer();
        levelStart.jammedTools = GetComponent<Jammer>().jammedTools;
        levelStart.jammedPressurePlates = GetComponent<Jammer>().jammedPressureplates;
        levelStart.destroyed = false;
        allUndos.Add(levelStart);
    }

    public override void SaveUndo()
    {
        base.SaveUndo();
        SingleUndo_Jammer newUndo = new SingleUndo_Jammer();
        newUndo.jammedTools = GetComponent<Jammer>().jammedTools;
        newUndo.jammedPressurePlates = GetComponent<Jammer>().jammedPressureplates;
        newUndo.destroyed = GetComponent<Jammer>().destroyed;
        allUndos.Add(newUndo);
    }

    public override void Undo()
    {
        bool needToSave = false;
        if (allUndos.Count == 1)
            needToSave = true;

        if(allUndos.Count > 0)
        {

            if (!GetComponentInParent<UndoSystem>())
                base.Undo();

            if(GetComponent<Jammer>().destroyed && !allUndos[allUndos.Count - 1].destroyed)
                GetComponent<Jammer>().ActivateDeactivateTool(true);

            GetComponent<Jammer>().jammedTools = allUndos[allUndos.Count - 1].jammedTools;
            GetComponent<Jammer>().jammedPressureplates = allUndos[allUndos.Count - 1].jammedPressurePlates;
            allUndos.RemoveAt(allUndos.Count - 1);
        }
        if (needToSave)
            SaveStartOfLevel();
    }
}



class SingleUndo_Jammer
{
    public List<ToolBase> jammedTools = new List<ToolBase>();
    public List<PressurePlate> jammedPressurePlates = new List<PressurePlate>();
    public bool destroyed;
}