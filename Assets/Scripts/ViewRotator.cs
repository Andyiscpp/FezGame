using System.Collections;
using UnityEngine;

public class ViewRotator : MonoBehaviour
{
    [Header("旋转设置")]
    public float rotationDuration = 0.4f; // 每次旋转 90 度花费的时间

    [Header("物理联动")]
    public Rigidbody playerRigidbody; // 在面板中把主角绿球拖入此槽位

    private bool isRotating = false; // 状态锁：防止在旋转和等待期间重复输入
    private Quaternion targetRotation; // 使用四元数记录目标旋转状态，避免万向节死锁

    void Start()
    {
        // 游戏开始时，记录初始视角的旋转状态
        targetRotation = transform.rotation;

        Physics.gravity = -transform.up * 9.81f;
    }

    void Update()
    {
        
        if (isRotating) return;

        // --- A 和 D 控制绕 Y 轴（左右）旋转 ---
        if (Input.GetKeyDown(KeyCode.A))
        {
            StartCoroutine(RotatePivot(90f, Vector3.up));
        }
        else if (Input.GetKeyDown(KeyCode.D))
        {
            StartCoroutine(RotatePivot(-90f, Vector3.up));
        }
        // --- W 和 S 控制绕 X 轴（上下）旋转 ---
        else if (Input.GetKeyDown(KeyCode.W))
        {
            StartCoroutine(RotatePivot(90f, Vector3.right));
        }
        else if (Input.GetKeyDown(KeyCode.S))
        {
            StartCoroutine(RotatePivot(-90f, Vector3.right));
        }
    }

    // 将角度和旋转轴作为参数传入
    private IEnumerator RotatePivot(float angle, Vector3 axis)
    {
        isRotating = true; // 锁定输入

        // 旋转开始前，将角色物理锁定（使其悬浮），防止在旋转过程中乱飞
        if (playerRigidbody != null)
        {
            playerRigidbody.isKinematic = true;
        }

        Quaternion startRotation = transform.rotation;

        // 核心数学升级：在当前目标旋转的基础上，绕世界坐标轴追加旋转 90 度
        targetRotation = Quaternion.AngleAxis(angle, axis) * targetRotation;

        float elapsedTime = 0f;

        // 使用 Slerp 平滑过渡动画
        while (elapsedTime < rotationDuration)
        {
            transform.rotation = Quaternion.Slerp(startRotation, targetRotation, elapsedTime / rotationDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        transform.rotation = targetRotation; // 强制对齐，消除浮点误差

        // 旋转动画播完，启动“停留1s后掉落”的独立协程
        StartCoroutine(WaitAndDrop());
    }

    // 处理停留与掉落逻辑的协程
    private IEnumerator WaitAndDrop()
    {
        yield return new WaitForSeconds(1f);

        Physics.gravity = -transform.up * 9.81f;

        if (playerRigidbody != null)
        {
            playerRigidbody.isKinematic = false;
        }

        isRotating = false; // 解除状态锁，允许玩家进行下一次操作
    }
}