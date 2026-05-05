using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/*
 * 这是一个关于角色控制的脚本
 */
public class PlayerController : MonoBehaviour
{
    public Rigidbody2D rigid2D;
    public GameObject bulletPrefab;
    public GameObject hp_bar;

    [Header("玩家基本属性")]
    [SerializeField] public int hp_max;         // 最大血量  
    public int hp;

    private enum DashDirection { Left = 0, Right = 1 };                         // 冲刺方向
    private DashDirection currentDashDir;

    [Header("武器参数")]//后续记得整合到武器脚本上
    [SerializeField] public float fireCooldown = 0.2f;
    [SerializeField] private float LastFireTime = 0f;

    [Header("移动参数")]
    [SerializeField] public float speed;                                        // 移动速度
    [SerializeField] private float jumpForce = 5f;                              // 跳跃力
    [SerializeField] private float dashForce = 5f;                              // 冲刺力
    [SerializeField] private float dashCooldown = 0.6f;                         // 冲刺冷却时间
    [SerializeField] private Transform groundCheck;                             // 地面检测点（需创建空物体作为子物体）
    [SerializeField] private LayerMask groundLayer;                             // 地面图层（需在Inspector选择地面图层）
    [SerializeField] private float groundCheckRadius = 0.2f;                    // 地面检测半径
    [SerializeField] private float slideStopThreshold = 0.1f;                   // 滑行阈值
    [SerializeField] private float speed_x_constraint = 10f;                    // 限速

    [Header("悬浮升空参数")]
    [SerializeField] private float floatSpeed = 4f;                             // 悬浮状态下的全向移动速度 (比正常速度慢)
    [SerializeField] private float upDashForce = 12f;                           // 按下空格瞬间的向上冲刺推力
    [SerializeField] private float floatDashDuration = 0.2f;                    // 向上冲刺期间不受控的持续时间
    [SerializeField] private int maxAirThrusts = 1;                             // 落地前允许触发几次向上冲刺

    //状态变量
    private float lastDashTime;                                                 // 上次冲刺时间
    private bool isSliding;                                                     // 是否处于滑行状态
    private bool isGrounded;                                                    // 是否在地面
    private bool isHoldingSpace;            
    private float floatDashTimer;
    private float originalGravity;                                              // 保存原有的重力值
    
    //计数系统变量
    [SerializeField] private int currentAirThrusts;
    void Start()
    {
        DontDestroyOnLoad(this.gameObject);
        rigid2D = GetComponent<Rigidbody2D>();
        hp_max = 10;
        hp = hp_max;
        currentAirThrusts = maxAirThrusts;
        originalGravity = rigid2D.gravityScale;                                    //初始重力大小
    }

    void Update()
    {
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
        if (isGrounded)                                                             //重置起飞计数
        {
            currentAirThrusts = maxAirThrusts;
        }
        //悬浮
        // ==================== 1. 悬浮状态逻辑检测 ====================
        if (Input.GetKeyDown(KeyCode.Space))
        {
            isHoldingSpace = true;
            
            rigid2D.gravityScale = 0f; // 失去重力束缚
            if(currentAirThrusts > 0)
            {
                currentAirThrusts--;
                floatDashTimer = floatDashDuration; // 开启短暂的冲刺计时器
                                                    
                rigid2D.velocity = new Vector2(rigid2D.velocity.x, 0f);// 清空当前垂直速度（防止下落时按空格导致向上的力被抵消）
                
                rigid2D.AddForce(new Vector2(0, upDashForce), ForceMode2D.Impulse);// 施加瞬间向上的推力
            }
            else
            {
                floatDashTimer = 0f;
            }
        }

        if (Input.GetKeyUp(KeyCode.Space))
        {
            isHoldingSpace = false;
            rigid2D.gravityScale = originalGravity; // 恢复重力
        }


        // ==================== 2. 角色移动逻辑分发 ====================
        if (isHoldingSpace)
        {
            // 【按住空格状态】
            if (floatDashTimer > 0)
            {
                // 正在经历瞬间向上的推力阶段，这段时间不接受WASD强行覆盖速度
                floatDashTimer -= Time.deltaTime;
            }
            else
            {
                // 推力结束，进入完全失重自由控制阶段 (可以用 WASD 八向移动)
                float moveX = Input.GetAxisRaw("Horizontal"); // A/D
                float moveY = Input.GetAxisRaw("Vertical");   // W/S

                // .normalized 确保斜向移动不会比直线快，乘以较慢的 floatSpeed
                Vector2 flyInput = new Vector2(moveX, moveY).normalized;
                rigid2D.velocity = flyInput * floatSpeed;
            }
        }
        else
        {
            // 【正常落地状态】
            if (Input.GetKeyDown(KeyCode.W) && isGrounded)
            {
                rigid2D.velocity = new Vector2(rigid2D.velocity.x, 0); // 重置Y速度，避免叠加
                rigid2D.AddForce(new Vector2(0, jumpForce), ForceMode2D.Impulse);
                isGrounded = false; // 跳起来后立即标记为非地面
            }

            float moveInput = Input.GetAxisRaw("Horizontal"); // A/D对应-1/1
            if (moveInput != 0)
            {
                rigid2D.velocity = new Vector2(moveInput * speed, rigid2D.velocity.y);
                // 更新冲刺方向
                currentDashDir = moveInput > 0 ? DashDirection.Right : DashDirection.Left;
            }
            else
            {
                // 无输入时平滑减速（解决粘滞感）
                rigid2D.velocity = new Vector2(Mathf.Lerp(rigid2D.velocity.x, 0, 0.1f), rigid2D.velocity.y);
            }

            // Shift水平冲刺
            if (Input.GetKeyDown(KeyCode.LeftShift) && Time.time - lastDashTime > dashCooldown)
            {
                lastDashTime = Time.time;
                rigid2D.velocity = new Vector2(0, rigid2D.velocity.y);
                float dashDir = currentDashDir == DashDirection.Right ? 1 : -1;
                rigid2D.AddForce(new Vector2(dashDir * dashForce, 0), ForceMode2D.Impulse);
            }

            if (Input.GetKeyDown(KeyCode.A)) { currentDashDir = DashDirection.Left; }
            if (Input.GetKeyDown(KeyCode.D)) { currentDashDir = DashDirection.Right; }
        }
        // 开火
        HandleCombat();
        CheckSlideStop();
        ApplyVelocityConstraints();
        UpdateUI();
    }
    private void ApplyVelocityConstraints()                                                          //速度限制函数
    {
        float limitedX = Mathf.Clamp(rigid2D.velocity.x, -speed_x_constraint, speed_x_constraint);
        rigid2D.velocity = new Vector2(limitedX, rigid2D.velocity.y);
    }

    private void UpdateUI()                                                                          //ui修改函数                                       
    {
        float _percent = ((float)hp / (float)hp_max);
        hp_bar.transform.localScale = new Vector3(_percent, hp_bar.transform.localScale.y, hp_bar.transform.localScale.z);
    }
    private void HandleCombat()                                                                     //开火函数
    {
        if ((Input.GetMouseButton(0) || Input.GetKeyDown(KeyCode.M)) && Time.time > LastFireTime)
        {
            // 1. 获取鼠标在世界中的位置
            Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mousePos.z = 0; // 2D 项目必须把 Z 设为 0

            // 2. 计算从角色指向鼠标的方向
            Vector2 shootDir = (mousePos - transform.position).normalized;

            // 3. 计算子弹旋转角度（关键！让子弹朝向鼠标）
            float angle = Mathf.Atan2(shootDir.y, shootDir.x) * Mathf.Rad2Deg;
            Quaternion bulletRot = Quaternion.Euler(0, 0, angle);

            // 4. 朝鼠标方向生成子弹
            Instantiate(bulletPrefab, transform.position, bulletRot);

            // 5. 更新上次发射时间
            LastFireTime = Time.time + fireCooldown;
        }
    }
    private void OnCollisionEnter2D(Collision2D collision)                                          //怪物碰撞函数
    {

        if (collision.gameObject.tag == "Monster")
        {
            print(collision.gameObject.name);
            hp -= 1;
            //记得做无敌时间和推动
        }
          //  collision.gameObject.SendMessage("ApplyDamage",10);
    }
    private void OnTriggerEnter2D(Collider2D collision)                                             //传送门碰撞函数
    {

        if (collision.gameObject.tag == "Portal")
        {
            collision.gameObject.transform.GetComponent<portal>().ChangeScene();//n拿出portal
        }
        
    }
    private void CheckSlideStop()                                                                   //滑行检测函数
    {
        if (isSliding && Mathf.Abs(rigid2D.velocity.x) < slideStopThreshold)
        {
            //Debug.Log($"滑行停止");
            isSliding = false;
        }
        else if(!isSliding && Mathf.Abs(rigid2D.velocity.x) >= slideStopThreshold)
        {
            isSliding = true;
        }
    }
}
    
