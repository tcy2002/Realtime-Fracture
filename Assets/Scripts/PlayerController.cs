using UnityEngine;

public class PlayerController : MonoBehaviour
{
    //角色移动控制参数
    public float myGravity = -9.79f;
    public float moveSpeed = 5f;
    public float jumpSpeed = 8f;
    public float rotateSpeed = 3f;
    public Camera playerCamera;
    
    private float _mouseX = -90, _mouseY;
    private float _horizontal, _vertical;
    private float _verticalSpeed;
    private CharacterController _character;

    void Start()
    {
        _character = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
        }
        if (Input.GetMouseButtonDown(0))
        {
            Cursor.lockState = CursorLockMode.Locked;
        }
        if (Cursor.lockState == CursorLockMode.Locked)
        {
            Jump();
            Move();
        }
        PlayerMove();
        CameraFollow();
    }

    void Move()
    {
        _mouseX += Input.GetAxis("Mouse X") * rotateSpeed;
        _mouseY -= Input.GetAxis("Mouse Y") * rotateSpeed;
        _mouseY = Mathf.Clamp(_mouseY, -90f, 90f);
        if (_character.isGrounded)
        {
             _horizontal = Input.GetAxisRaw("Horizontal");
             _vertical = Input.GetAxisRaw("Vertical");
        }
    }

    void Jump()
    {
        if (_character.isGrounded && _verticalSpeed < -1f)
        {
            _verticalSpeed = -1f;
        }
        if (_character.isGrounded && Input.GetKeyDown(KeyCode.Space))
        {
            _verticalSpeed = jumpSpeed;
        }
        _verticalSpeed += myGravity * Time.deltaTime;
    }

    void CameraFollow()
    {
        playerCamera.transform.rotation = Quaternion.Euler(_mouseY, _mouseX, 0f);
        playerCamera.transform.position = transform.position + Vector3.up * 0.5f + transform.forward * 0.1f;
    }

    void PlayerMove()
    {
        //旋转
        transform.rotation = Quaternion.Euler(0f, _mouseX, 0f);

        //移动
        var forward = transform.forward;
        var right = new Vector3(forward.z, 0, -forward.x); 
        var direction = (forward * _vertical + right * _horizontal).normalized;
        var speed = direction * moveSpeed;
        speed.y = _verticalSpeed;
        _character.Move(speed * Time.deltaTime);
    }
}
