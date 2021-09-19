using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using DG.Tweening;

public class UnitController : MonoBehaviour
{
    public const int TYPE_WHITE = 1;
    public const int TYPE_BLACK = 2;

    // 自分のタイプ
    public int UnitType;

    Vector3 firstPosition;

    private void Awake()
    {
        firstPosition = transform.position;
    }

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    // アニメーション時間を返す
    public float Reverse(int type, bool anim=true)
    {
        float angle = 0f;
        float animationTime = 0.5f;

        switch (type)
        {
            case TYPE_WHITE:
                angle = 0f;
                break;
            case TYPE_BLACK:
                angle = 180f;
                break;
            default:
                Assert.IsTrue(false);
                break;
        }

        this.transform.DOKill();
        transform.position = firstPosition;

        if (anim)
        {
            transform.DOLocalJump(
                transform.position,
                1,
                1,
                animationTime
            );
            this.transform.DORotate(new Vector3(angle, 0, 0), animationTime);
        }
        else
        {
            this.transform.eulerAngles = new Vector3(angle, 0, 0);
        }

        UnitType = type;
        return animationTime;
    }
}
