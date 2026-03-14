using System.Collections;
using UnityEngine;

public class DualRotator : MonoBehaviour
{
    [Header("旋转目标")]
    public Transform innerObject; // 拖入 Inner (或 Inner_Pivot)
    public Transform outerObject; // 拖入 Outer (或 Outer_Pivot)

    [Header("旋转中心点 (选填)")]
    [Tooltip("如果设置了此点，环境将绕该点旋转。")]
    public Transform rotationCenter;

    [Header("旋转设置")]
    public float rotationDuration = 0.4f; // 每次旋转 90 度花费的时间

    [Header("物理联动")]
    public Rigidbody playerRigidbody; // 拖入主角绿球

    private bool isRotating = false; // 状态锁

    // 记录目标旋转状态
    private Quaternion innerTargetRotation;
    private Quaternion outerTargetRotation;

    void Start()
    {
        if (innerObject != null) innerTargetRotation = innerObject.rotation;
        if (outerObject != null) outerTargetRotation = outerObject.rotation;

        Physics.gravity = new Vector3(0, -9.81f, 0);
    }

    void Update()
    {
        if (isRotating) return;

        // --- W A S D 控制 Inner ---
        if (Input.GetKeyDown(KeyCode.A)) StartRotationProcess(innerObject, true, 90f, Vector3.up);
        else if (Input.GetKeyDown(KeyCode.D)) StartRotationProcess(innerObject, true, -90f, Vector3.up);
        else if (Input.GetKeyDown(KeyCode.W)) StartRotationProcess(innerObject, true, 90f, Vector3.right);
        else if (Input.GetKeyDown(KeyCode.S)) StartRotationProcess(innerObject, true, -90f, Vector3.right);

        // --- 方向键 控制 Outer ---
        else if (Input.GetKeyDown(KeyCode.LeftArrow)) StartRotationProcess(outerObject, false, 90f, Vector3.up);
        else if (Input.GetKeyDown(KeyCode.RightArrow)) StartRotationProcess(outerObject, false, -90f, Vector3.up);
        else if (Input.GetKeyDown(KeyCode.UpArrow)) StartRotationProcess(outerObject, false, 90f, Vector3.right);
        else if (Input.GetKeyDown(KeyCode.DownArrow)) StartRotationProcess(outerObject, false, -90f, Vector3.right);
    }

    private void StartRotationProcess(Transform targetTransform, bool isInner, float angle, Vector3 axis)
    {
        if (targetTransform == null) return;

        Quaternion startRotation = targetTransform.rotation;
        Quaternion endRotation;

        if (isInner)
        {
            innerTargetRotation = Quaternion.AngleAxis(angle, axis) * innerTargetRotation;
            endRotation = innerTargetRotation;
        }
        else
        {
            outerTargetRotation = Quaternion.AngleAxis(angle, axis) * outerTargetRotation;
            endRotation = outerTargetRotation;
        }

        StartCoroutine(RotateTargetRoutine(targetTransform, startRotation, endRotation));
    }

    private IEnumerator RotateTargetRoutine(Transform targetTransform, Quaternion startRotation, Quaternion endRotation)
    {
        isRotating = true; // 锁定输入

        bool isPlayerAttached = false;
        Transform originalPlayerParent = null;

        if (playerRigidbody != null)
        {
            playerRigidbody.isKinematic = true;

            // --- 核心修复：射线检测玩家是否站在即将旋转的物体上 ---
            RaycastHit hit;
            // 从玩家中心向下发射射线（假设玩家是半径0.5的球，1.2f的距离足够检测到脚底的地面）
            if (Physics.Raycast(playerRigidbody.position, Vector3.down, out hit, 1.2f))
            {
                // IsChildOf 可以判断踩到的方块是不是属于正在旋转的 Inner 或 Outer 层级
                if (hit.transform.IsChildOf(targetTransform))
                {
                    isPlayerAttached = true;
                    // 记录原来的父物体（防止破坏你原有的层级）
                    originalPlayerParent = playerRigidbody.transform.parent;
                    // 将玩家临时变为旋转环境的子物体，这样会跟着一起被甩动
                    playerRigidbody.transform.SetParent(targetTransform, true);
                }
            }
        }

        float elapsedTime = 0f;
        Vector3 startPos = targetTransform.position;
        Vector3 pivot = rotationCenter != null ? rotationCenter.position : startPos;

        while (elapsedTime < rotationDuration)
        {
            float t = elapsedTime / rotationDuration;
            Quaternion currentRot = Quaternion.Slerp(startRotation, endRotation, t);

            if (rotationCenter != null)
            {
                Quaternion currentDelta = currentRot * Quaternion.Inverse(startRotation);
                targetTransform.position = pivot + currentDelta * (startPos - pivot);
            }

            targetTransform.rotation = currentRot;

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        if (rotationCenter != null)
        {
            Quaternion finalDelta = endRotation * Quaternion.Inverse(startRotation);
            targetTransform.position = pivot + finalDelta * (startPos - pivot);
        }
        targetTransform.rotation = endRotation;

        // --- 旋转结束，解除玩家的绑定 ---
        if (isPlayerAttached && playerRigidbody != null)
        {
            playerRigidbody.transform.SetParent(originalPlayerParent, true);
        }

        StartCoroutine(WaitAndDrop());
    }

    private IEnumerator WaitAndDrop()
    {
        // 如果你觉得落地前悬停 1 秒太长，可以把 1f 改成 0.2f
        yield return new WaitForSeconds(1f);

        if (playerRigidbody != null)
        {
            playerRigidbody.isKinematic = false;
            // 为了防止玩家是一个刚体球，在旋转后轴向错乱导致物理表现奇怪，强行把它掰正
            playerRigidbody.rotation = Quaternion.identity;
            playerRigidbody.angularVelocity = Vector3.zero; // 清除之前的旋转惯性
        }

        isRotating = false; // 解除状态锁
    }
}