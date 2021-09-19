using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameDirector : MonoBehaviour
{
    // ゲームモード
    enum MODE
    {
        NONE,
        NORMAL,
        RESULT,
    }

    MODE mode;
    MODE nextMode;

    const int FIELD_SIZE_X = 8;
    const int FIELD_SIZE_Y = 8;

    enum FIELD
    {
        NONE,
        EMPTY,
        UNIT
    }

    GameObject[,] fieldData;

    List<int[]> searchDirection = new List<int[]>()
    {
        new int[2]{ 0, 1 },  // 上
        new int[2]{ +1, 1 }, // 右上
        new int[2]{ +1, 0 },  // 右
        new int[2]{ +1, -1 }, // 右下
        new int[2]{ 0, -1 },  // 下
        new int[2]{ -1, -1 }, // 左下
        new int[2]{ -1, 0 },  // 左
        new int[2]{ -1, 1 }, // 左上
    };

    const int PLAYER_MAX = 2;
    int nowTurn;
    GamePlayer[] player;

    bool isStop;
    float waitTimer;

    GameObject textInfo;
    string oldTextInfo;

    // Start is called before the first frame update
    void Start()
    {
        initMatch();
    }

    void initMatch()
    {
        textInfo = GameObject.Find("TextInfo");

        GameObject field = GameObject.Find("Field");
        field.transform.localScale = new Vector3(FIELD_SIZE_X, 1, FIELD_SIZE_Y);

        player = new GamePlayer[PLAYER_MAX];
        player[0] = new GamePlayer();
        player[0].IsPlayer = true;
        player[0].UnitType = UnitController.TYPE_WHITE;
        player[1] = new GamePlayer();
        player[1].IsPlayer = false;
        player[1].UnitType = UnitController.TYPE_BLACK;

        nowTurn = 0;

        fieldData = new GameObject[FIELD_SIZE_X, FIELD_SIZE_Y];

        int[,] initalFieldData = new int[FIELD_SIZE_X, FIELD_SIZE_Y];
        initalFieldData[FIELD_SIZE_X/2 - 1, FIELD_SIZE_Y/2 - 1] = UnitController.TYPE_WHITE;
        initalFieldData[FIELD_SIZE_X/2,     FIELD_SIZE_Y/2 - 1] = UnitController.TYPE_BLACK;
        initalFieldData[FIELD_SIZE_X/2 - 1, FIELD_SIZE_Y/2    ] = UnitController.TYPE_BLACK;
        initalFieldData[FIELD_SIZE_X/2,     FIELD_SIZE_Y/2    ] = UnitController.TYPE_WHITE;

        // 当たり判定・タイル・初期ユニットの設置
        for (int i = 0; i < FIELD_SIZE_X; i++)
        {
            for (int j = 0; j < FIELD_SIZE_Y; j++)
            {
                float x = i - (FIELD_SIZE_X/2 - 0.5f);
                float y = j - (FIELD_SIZE_Y/2 - 0.5f);

                GameObject prefab = (GameObject)Resources.Load("BoxCollider");
                GameObject obj = Instantiate(prefab, new Vector3(x, 0, y), Quaternion.identity);

                if ((i+j) % 2 == 0)
                {
                    Instantiate((GameObject)Resources.Load("FieldDarkTile"), new Vector3(x, 0.01f, y), Quaternion.identity);
                }

                if (1 > initalFieldData[i, j]) continue;
                setUnit(initalFieldData[i, j], i, j, false);
            }
        }

        nowTurn = -1;
        initMode(MODE.NORMAL);
    }

    void initMode(MODE next)
    {
        mode = next;
        nextMode = MODE.NONE;

        if (mode == MODE.NORMAL)
        {
            turnChange();

            if (!searchEmptyField(player[nowTurn].UnitType))
            {
                turnChange();
            }

            if (!player[nowTurn].IsPlayer)
            {
                waitTimer = Random.Range(1.0f, 5.0f);
            }

            string playername = player[nowTurn].GetPlayerName();
            textInfo.GetComponent<Text>().text = playername + "の手番です";
        }
        else if (MODE.RESULT == mode)
        {
            string playername = player[0].GetPlayerName();
            int maxCount = 0;
            foreach (var v in player)
            {
                int type = v.UnitType;
                v.UnitCount = countUnits(type);

                if (v.UnitCount > maxCount)
                {
                    maxCount = v.UnitCount;
                    playername = v.GetPlayerName();
                }
            }

            textInfo.GetComponent<Text>().text = playername + "の勝ちです！";
        }
    }

    int countUnits(int type)
    {
        int count = 0;
        for (int i = 0; i < FIELD_SIZE_X; i++)
        {
            for (int j = 0; j < FIELD_SIZE_Y; j++)
            {
                if (fieldData[i, j] == null)
                    continue;
                if (fieldData[i, j].GetComponent<UnitController>().UnitType == type)
                    count++;
            }
        }
        return count;
    }

    void turnChange()
    {
        nowTurn = (nowTurn + 1) % PLAYER_MAX;
    }

    // Update is called once per frame
    void Update()
    {
        if (isWaiting())
            return;

        if (mode == MODE.NORMAL)
        {
            normalMode();
        }
        else if (mode == MODE.RESULT)
        {

        }

        if (nextMode != MODE.NONE)
        {
            initMode(nextMode);
        }
    }

    void normalMode()
    {
        // 勝敗チェック
        if (!searchEmptyField(UnitController.TYPE_WHITE) &&
            !searchEmptyField(UnitController.TYPE_BLACK))
        {
            nextMode = MODE.RESULT;
            return;
        }

        // CPUの処理
        if (!player[nowTurn].IsPlayer)
        {
            int type = player[nowTurn].UnitType;
            int max = 0;
            int mx = -1, my = -1;
            for (int i = 0; i < FIELD_SIZE_X; i++)
            {
                for (int j = 0; j < FIELD_SIZE_Y; j++)
                {
                    if (getFieldData(i, j) != FIELD.EMPTY)
                        continue;
                    int count = getReverseUnitsAll(type, i, j).Count;
                    if (max < count)
                    {
                        max = count;
                        mx = i;
                        my = j;
                    }
                }
            }

            if (max > 0)
            {
                setUnit(type, mx, my);
                nextMode = MODE.NORMAL;
            }

            return;
        }

        // プレイヤーの処理
        if (Input.GetMouseButtonUp(0))
        {
            int x = -1, y = -1;
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, 100))
            {
                if (hit.collider.gameObject != null)
                {
                    Vector3 pos = hit.collider.gameObject.transform.position;
                    x = (int)(pos.x + (FIELD_SIZE_X/2 - 0.5f));
                    y = (int)(pos.z + (FIELD_SIZE_Y/2 - 0.5f));
                }

                if (0 < getReverseUnitsAll(player[nowTurn].UnitType, x, y).Count)
                {
                    setUnit(player[nowTurn].UnitType, x, y);
                    nextMode = MODE.NORMAL;
                }
            }
        }
    }

    void setUnit(int type, int x, int y, bool animation = true)
    {
        if (getFieldData(x, y) != FIELD.EMPTY)
        {
            return;
        }

        float posx = x - (FIELD_SIZE_X/2 - 0.5f);
        float posy = y - (FIELD_SIZE_Y/2 - 0.5f);

        GameObject prefab = (GameObject)Resources.Load("Unit");
        GameObject obj = Instantiate(prefab, new Vector3(posx, 0, posy), Quaternion.identity);

        float wait = 0.0f;
        foreach (var v in getReverseUnitsAll(type, x, y))
        {
            GameObject unit = fieldData[v[0], v[1]];
            wait = unit.GetComponent<UnitController>().Reverse(type, animation);
        }
        waitTimer += wait;

        obj.GetComponent<UnitController>().Reverse(type, animation);

        fieldData[x, y] = obj;
    }

    FIELD getFieldData(int x, int y)
    {
        if (x < 0 || y < 0 || fieldData.GetLength(0) <= x || fieldData.GetLength(1) <= y)
        {
            return FIELD.NONE;
        }

        if (fieldData[x, y] != null)
        {
            return FIELD.UNIT;
        }
        else
        {
            return FIELD.EMPTY;
        }
    }

    bool isVsType(int type, int x, int y)
    {
        if (getFieldData(x, y) != FIELD.UNIT)
        {
            return false;
        }

        GameObject unit = fieldData[x, y];
        int t = unit.GetComponent<UnitController>().UnitType;
        return 0 < t && type != t;
    }

    List<int[]> getReverseUnits(int type, int x, int y, int vx, int vy)
    {
        List<int[]> ret = new List<int[]>();
        List<int[]> none = new List<int[]>();

        while (true)
        {
            x += vx;
            y += vy;

            if (getFieldData(x, y) != FIELD.UNIT)
            {
                break;
            }

            if (ret.Count == 0)
            {
                if (isVsType(type, x, y))
                {
                    ret.Add(new int[] { x, y });
                }
                else
                {
                    return none;
                }
            }
            else
            {
                if (isVsType(type, x, y))
                {
                    ret.Add(new int[] { x, y });
                }
                else
                {
                    return ret;
                }
            }
        }

        return none;
    }

    List<int[]> getReverseUnitsAll(int type, int x, int y)
    {
        List<int[]> ret = new List<int[]>();

        if (getFieldData(x, y) != FIELD.EMPTY)
        {
            return ret;
        }

        foreach(int[] dir in searchDirection)
        {
            int vx = dir[0];
            int vy = dir[1];
            foreach(var v in getReverseUnits(type, x, y, vx, vy))
            {
                ret.Add(v);
            }
        }
        return ret;
    }

    bool searchEmptyField(int type)
    {
        for (int i = 0; i < FIELD_SIZE_X; i++)
        {
            for (int j = 0; j < FIELD_SIZE_Y; j++)
            {
                if (getFieldData(i, j) == FIELD.UNIT)
                {
                    continue;
                }

                if (getReverseUnitsAll(type, i, j).Count > 0)
                {
                    return true;
                }
            }
        }

        return false;
    }

    bool isWaiting()
    {
        if (isStop)
        {
            return true;
        }

        if (waitTimer > 0.0f)
        {
            waitTimer -= Time.deltaTime;
            return true;
        }
        else
        {
            return false;
        }
    }

    public void Restart()
    {
        SceneManager.LoadScene("LocalMatch");
    }

    public void Pause()
    {
        isStop = !isStop;
        if (isStop)
        {
            oldTextInfo = textInfo.GetComponent<Text>().text;
            textInfo.GetComponent<Text>().text = "休憩中";
        }
        else
        {
            textInfo.GetComponent<Text>().text = oldTextInfo;
        }

        for (int i = 0; i < FIELD_SIZE_X; i++)
        {
            for (int j = 0; j < FIELD_SIZE_Y; j++)
            {
                if (fieldData[i, j] == null)
                    continue;
                fieldData[i, j].SetActive(!isStop);
            }
        }
    }
}