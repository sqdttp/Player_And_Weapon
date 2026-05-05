using UnityEngine;

public class StaffWeaponManager : MonoBehaviour
{
    [Header("配置")]
    public int discCount = 3;                                       //圆盘数
    public int slotsPerDisc = 3;                                    //能量槽数量
    public float absorbRange = 2f;                                  //吸取范围
    public LayerMask energySourceLayer;                             //吸取的目标

    private EnergyDisc[] discs;
    private int currentDiscIndex = 0;

    void Start()
    {
        discs = new EnergyDisc[discCount];                          //圆盘初始化
        for (int i = 0; i < discCount; i++)
            discs[i] = new EnergyDisc(slotsPerDisc);
    }

    void Update()
    {
        // 只处理法杖相关的输入
        if (Input.GetKeyDown(KeyCode.Q)) SwitchDisc();
        if (Input.GetKeyDown(KeyCode.E)) TryAbsorb();
        if (Input.GetMouseButtonDown(1)) ReleaseSkill();
    }

    void SwitchDisc()                                               //圆盘切换
    {
        currentDiscIndex = (currentDiscIndex + 1) % discs.Length;
        Debug.Log($"切换到圆盘: {currentDiscIndex + 1}");
    }

    void TryAbsorb()                                                        //吸取能量
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, absorbRange, energySourceLayer);

        if (hits.Length > 0)
        {
            //拿到离你最近物体的能量信息
            EnergySource source = hits[0].GetComponent<EnergySource>();
            if (source != null)
            {
                EnergyDisc activeDisc = discs[currentDiscIndex];
                bool success = discs[currentDiscIndex].AddEnergy(source.energyType);
                if (success)
                {
                    // 注意：因为 AddEnergy 内部执行了 currentSlotIndex++
                    // 所以此时 activeDisc.currentSlotIndex 的数值正好代表了“已填满的槽位数量”
                    int discNum = currentDiscIndex + 1; // 索引从0开始，显示给人类看要+1
                    int slotNum = activeDisc.currentSlotIndex; // 刚刚填入的槽位序号

                    Debug.Log($"<color=cyan>【系统】</color> 第 <color=yellow>{discNum}</color> 号圆盘 " +
                              $"第 <color=yellow>{slotNum}</color> 号槽位，" +
                              $"吸取了 <color=red>{source.energyType}</color> 元素");

                    //Destroy(hits[0].gameObject);
                }
                else
                {
                    Debug.Log("这个圆盘已经满了，换一个或者释放掉吧！");
                }
            }
        }
    }

    void ReleaseSkill()                                                 //技能释放
    {
        EnergyDisc currentDisc = discs[currentDiscIndex];
        if (discs[currentDiscIndex].IsFull)
        {
            string combo = currentDisc.GetCombinationKey();
            Debug.Log($"★ 释放技能！组合码: {combo}");

            // --- 这里以后接入具体的技能生成逻辑 ---
            // SpawnSkill(combo); 

            currentDisc.Clear(); // 释放后清空
        }
    }
    private void OnDrawGizmosSelected()                                   //画出吸取范围，用于测试
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, absorbRange);
    }
}
