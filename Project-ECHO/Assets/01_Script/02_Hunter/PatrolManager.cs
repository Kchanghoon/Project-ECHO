using UnityEngine;

public class PatrolManager : MonoBehaviour
{
    [Header("Hunter Patrol Routes")]
    public Transform[] blindHunterPoints;
    public Transform[] seeHunterPoints;
    public Transform[] spiderHunterPoints;

    // 싱글톤처럼 쉽게 접근하기 위해 static 선언 (선택 사항)
    public static PatrolManager Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
    }
}