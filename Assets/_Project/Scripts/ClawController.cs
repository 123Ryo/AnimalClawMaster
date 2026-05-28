using UnityEngine;
using UnityEngine.InputSystem;

public class ClawController : MonoBehaviour
{
    [Header("移動邊界設定")]
    public float minX = -2.3f;
    public float maxX = 2.3f;
    public float moveSpeed = 5f;

    [Header("抓取設定")]
    public float downSpeed = 3f;
    public float targetY = -2.5f;
    public float startY = 3f;

    [Header("展示模型位置設定")]
    public Vector3 spawnPosition = new Vector3(0, 0, 0); // 生成位置
    public Vector3 spawnRotation = Vector3.zero;        // 生成旋轉
    public Vector3 spawnScale = Vector3.one;            // 生成縮放

    [Header("視覺設定 (關鍵角度)")]
    public float openAngleLeft = 60f;  // 左爪張開的角度
    public float openAngleRight = -60f;// 右爪張開的角度
    public float closedAngle = 0f;      // 閉合角度

    public Transform leftClaw;
    public Transform rightClaw;

    private bool isMoving = false;
    private GameObject grabbedEgg = null;

    public Transform grabPoint;

    void Update()
    {
        if (isMoving) return;

        // --- 左右移動 (包含邊界限制) ---
        float horizontal = 0;
        if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) horizontal = -1;
        if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) horizontal = 1;
        
        Vector3 newPos = transform.position + new Vector3(horizontal * moveSpeed * Time.deltaTime, 0, 0);
        newPos.x = Mathf.Clamp(newPos.x, minX, maxX);
        transform.position = newPos;

        // --- 觸發抓取 ---
        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            StartCoroutine(ClawRoutine());
        }
    }

    System.Collections.IEnumerator ClawRoutine()
    {
        isMoving = true;
        
        //  按下空白鍵，張開爪子
        SetClawAngles(openAngleLeft, openAngleRight);
        yield return new WaitForSeconds(0.2f); // 給一點張開的時間緩衝

        //  張著爪子下降 (下降時不需要碰撞器，避免擠壓)
        SetClawColliders(false); 
        while (transform.position.y > targetY)
        {
            transform.position += Vector3.down * downSpeed * Time.deltaTime;
            yield return null;
        }

        // 到達底部，「閉合爪子」並打開碰撞器偵測
        SetClawColliders(true);
        SetClawAngles(closedAngle, closedAngle); // 兩個爪子回到 0 度
        yield return new WaitForSeconds(0.5f); // 停留一段時間，讓 OnTriggerEnter 有機會觸發

        //  帶著抓到的蛋 (如果有的話) 上升
        while (transform.position.y < startY)
        {
            transform.position += Vector3.up * downSpeed * Time.deltaTime;
            yield return null;
        }

        // 當爪子回到原位
        if (grabbedEgg != null)
        {
            EggItem item = grabbedEgg.GetComponent<EggItem>();
            
            //  讓轉蛋視覺消失
            grabbedEgg.SetActive(false); 
            
            // 顯示動物模型
            DisplayAnimalInShowcase(item.eggContent);
            
            grabbedEgg = null;
        }

        //  回復到初始的傾斜張開狀態，準備下次抓取
        leftClaw.localRotation = Quaternion.Euler(0, 0, 0);
        rightClaw.localRotation = Quaternion.Euler(0, 0, 0);

        isMoving = false;
    }

    // 用來同時設定兩個爪子的角度
    private void SetClawAngles(float leftAngle, float rightAngle)
    {
        leftClaw.localRotation = Quaternion.Euler(0, 0, leftAngle);
        rightClaw.localRotation = Quaternion.Euler(0, 0, rightAngle);
    }

    // 同時設定兩個爪子的碰撞器啟用狀態
    private void SetClawColliders(bool enabled)
    {
        if (leftClaw.GetComponent<Collider2D>()) leftClaw.GetComponent<Collider2D>().enabled = enabled;
        if (rightClaw.GetComponent<Collider2D>()) rightClaw.GetComponent<Collider2D>().enabled = enabled;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.name.Contains("Test_Egg") && grabbedEgg == null)
        {
            EggItem item = collision.gameObject.GetComponent<EggItem>();
            if (item != null)
            {
                Debug.Log("抓到了：" + item.eggContent.animalName);

                ShowAnimal(item.eggContent);
            }

            grabbedEgg = collision.gameObject;
            grabbedEgg.GetComponent<Rigidbody2D>().simulated = false;
            grabbedEgg.transform.SetParent(grabPoint);
            grabbedEgg.transform.localPosition = Vector3.zero;
        }
    }

    void DisplayAnimalInShowcase(EggData data)
        {
            Debug.Log("成功顯示動物: " + data.animalName);
            
            if (data.animalPrefab != null)
            {
                // 1. 生成模型
                GameObject spawnedAnimal = Instantiate(data.animalPrefab);
                
                // 2.Transform 數值
                spawnedAnimal.transform.position = spawnPosition;
                spawnedAnimal.transform.rotation = Quaternion.Euler(spawnRotation);
                spawnedAnimal.transform.localScale = spawnScale;

                // 3. 目前先顯示 1 秒後銷毀物件
                Destroy(spawnedAnimal, 1.0f);
                
                Debug.Log("模型已生成在設定位置: " + spawnPosition);
            }
        }

    void ShowAnimal(EggData data)
    {
     
        Debug.Log("準備顯示模型: " + data.animalName);
    }
}