using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

public class GamePlayer
{
    public bool IsPlayer = true;
    public int UnitType;
    public int UnitCount;

    public string GetPlayerName()
    {
        string name;
        if (IsPlayer)
        {
            name = "プレイヤー";
        }
        else
        {
            name =  "CPU";
        }

        string type = "";
        switch (UnitType) 
        {
            case UnitController.TYPE_BLACK:
                type = "黒";
                break;
            case UnitController.TYPE_WHITE:
                type = "白";
                break;
            default:
                Assert.IsTrue(false);
                break;
        }

        return name + "（" + type + "）";
    }
}
