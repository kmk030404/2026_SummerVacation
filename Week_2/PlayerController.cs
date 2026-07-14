using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private float walkSpeed = 4f;
    [SerializeField] private float runSpeed = 7f;

    [SerializeField] private float gravity = -9.81f;
    [SerializeField] private float jumpHeight = 2f;

    [SerializeField] private float mouseSpeed = 1.5f;
    [SerializeField] private Transform camTr;

    [Header("Roll")]
    [SerializeField] private float rollSpeed = 10f;
    [SerializeField] private float rollDuration = 0.4f;
    [SerializeField] private float rollCooldown = 1f;
    [SerializeField] private float rollCameraDrop = 0.3f;

    private float xRot;
    private Vector3 velo;

    private bool isRolling = false;
    private float lastRollTime = -999f;

    CharacterController cc;

    void Start()
    {
        cc = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        HandleMouseLook();

        bool rollPressed = Keyboard.current.leftCtrlKey.wasPressedThisFrame
                         || Keyboard.current.leftCommandKey.wasPressedThisFrame;

        if (rollPressed && !isRolling && Time.time >= lastRollTime + rollCooldown && cc.isGrounded)
        {
            StartCoroutine(Roll());
            return; // 구르기 시작한 프레임엔 아래 이동 로직 건너뜀
        }

        if (!isRolling)
        {
            MoveAndJump();
        }
        else
        {
            // 구르기 중엔 중력만 유지 (수평 이동은 코루틴이 처리)
            velo.y += gravity * Time.deltaTime;
            cc.Move(new Vector3(0f, velo.y, 0f) * Time.deltaTime);
        }
    }

    void HandleMouseLook()
    {
        Vector2 mouseDelta = Mouse.current.delta.ReadValue();
        float mouseX = mouseDelta.x * mouseSpeed * 0.1f;
        float mouseY = mouseDelta.y * mouseSpeed * 0.1f;

        xRot -= mouseY;
        xRot = Mathf.Clamp(xRot, -90f, 90f);

        camTr.localRotation = Quaternion.Euler(xRot, 0f, 0f);
        transform.Rotate(Vector3.up * mouseX);
    }

    void MoveAndJump()
    {
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        bool grounded = cc.isGrounded;
        if (grounded && velo.y < 0) velo.y = -2f;
        float curSpeed = Input.GetKey(KeyCode.LeftShift) ? runSpeed : walkSpeed;
        Vector3 movDir = transform.right * h + transform.forward * v;

        cc.Move(movDir * curSpeed * Time.deltaTime);

        if (Input.GetButtonDown("Jump") && grounded) velo.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        velo.y += gravity * Time.deltaTime;

        cc.Move(velo * Time.deltaTime);
    }

    IEnumerator Roll()
    {
        isRolling = true;
        lastRollTime = Time.time;

        // 카메라 정면 방향의 수평 성분만 사용
        Vector3 rollDir = camTr.forward;
        rollDir.y = 0f;
        rollDir.Normalize();

        Vector3 originalCamPos = camTr.localPosition;
        Vector3 loweredCamPos = originalCamPos + Vector3.down * rollCameraDrop;

        float elapsed = 0f;
        while (elapsed < rollDuration)
        {
            float t = elapsed / rollDuration;

            // 이동
            cc.Move(rollDir * rollSpeed * Time.deltaTime);

            // 중력도 같이 적용 (구르기 중 낙하 방지용 최소한의 처리)
            velo.y += gravity * Time.deltaTime;
            cc.Move(new Vector3(0f, velo.y, 0f) * Time.deltaTime);

            // 카메라 낮췄다 올라오는 효과 (0~0.5 구간 내려가고 0.5~1 구간 올라옴)
            if (t < 0.5f)
                camTr.localPosition = Vector3.Lerp(originalCamPos, loweredCamPos, t / 0.5f);
            else
                camTr.localPosition = Vector3.Lerp(loweredCamPos, originalCamPos, (t - 0.5f) / 0.5f);

            elapsed += Time.deltaTime;
            yield return null;
        }

        camTr.localPosition = originalCamPos;
        isRolling = false;
    }
}