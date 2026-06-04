using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

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
    public Vector3 spawnPosition = new Vector3(0, 0, 0);
    public Vector3 spawnRotation = Vector3.zero;
    public Vector3 spawnScale = Vector3.one;

    [Header("視覺設定 (關鍵角度)")]
    public float openAngleLeft = 60f;  
    public float openAngleRight = -60f;
    public float closedAngle = 0f;      

    public Transform leftClaw;
    public Transform rightClaw;

    private bool isMoving = false;
    private bool hitObject = false; 
    private GameObject grabbedEgg = null;
    private GameObject grabbedCoin = null; 

    public Transform grabPoint;

    [Header("金幣系統設定")]
    public TextMeshProUGUI coinText; 
    private int coinCount = 0;       

    private bool isPressingLeft = false;
    private bool isPressingRight = false;

    [Header("機台管理器")]
    public MachineManager machineManager;

    void Start()
    {
        LoadGameData();
        UpdateCoinUI();
    }

    void Update()
    {
        if (isMoving) return;

        // --- 左右移動邏輯 (整合鍵盤與手機虛擬按鈕) ---
        float horizontal = 0;

        // 偵測鍵盤
        if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) horizontal = -1;
        if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) horizontal = 1;
        
        // 偵測手機 UI 按鈕
        if (horizontal == 0)
        {
            if (isPressingLeft) horizontal = -1;
            else if (isPressingRight) horizontal = 1;
        }
        
        Vector3 newPos = transform.position + new Vector3(horizontal * moveSpeed * Time.deltaTime, 0, 0);
        newPos.x = Mathf.Clamp(newPos.x, minX, maxX);
        transform.position = newPos;

        // --- 觸發抓取 (鍵盤空白鍵) ---
        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            TriggerGrab();
        }
    }

    // ───【手機版：提供給 UI 呼叫的方法】───

    public void PointerDownLeft() { isPressingLeft = true; }
    public void PointerUpLeft() { isPressingLeft = false; }

    public void PointerDownRight() { isPressingRight = true; }
    public void PointerUpRight() { isPressingRight = false; }

    public void TriggerGrab()
    {
        if (!isMoving)
        {
            StartCoroutine(ClawRoutine());
        }
    }

    // ─── 提供給廣告系統或本機增減金幣的公開方法 (會同步存檔) ───
    public void AddCoins(int amount)
    {
        coinCount += amount; 
        SaveGameData(); // 存入本機記憶體
        UpdateCoinUI();      
        Debug.Log("【ClawController】金幣異動，增加: " + amount + "，目前總數: " + coinCount);
    }

    // ─── 金幣本機資料存取 ───
    private void SaveGameData()
    {
        PlayerPrefs.SetInt("TotalCoins", coinCount);
        PlayerPrefs.Save();
    }

    private void LoadGameData()
    {
        // 讀取儲存的金幣，如果過去沒有紀錄，則預設為 0
        coinCount = PlayerPrefs.GetInt("TotalCoins", 0);
    }

    System.Collections.IEnumerator ClawRoutine()
    {
        isMoving = true;
        hitObject = false; 
        
        SetClawAngles(openAngleLeft, openAngleRight);
        yield return new WaitForSeconds(0.2f); 

        SetClawColliders(true); 
        
        while (transform.position.y > targetY && !hitObject)
        {
            transform.position += Vector3.down * downSpeed * Time.deltaTime;
            yield return null;
        }

        SetClawAngles(closedAngle, closedAngle); 
        yield return new WaitForSeconds(0.5f); 

        while (transform.position.y < startY)
        {
            transform.position += Vector3.up * downSpeed * Time.deltaTime;
            yield return null;
        }

        if (grabbedEgg != null)
        {
            EggItem item = grabbedEgg.GetComponent<EggItem>();
            grabbedEgg.SetActive(false); 
            DisplayAnimalInShowcase(item.eggContent);
            grabEggEffects(); // 清理
        }

        if (grabbedCoin != null)
        {
            Destroy(grabbedCoin); 
            grabbedCoin = null;
            // 抓到場景中的金幣獎勵，呼叫 AddCoins(1) 來確保會同步寫入 PlayerPrefs
            AddCoins(1); 
        }

        leftClaw.localRotation = Quaternion.Euler(0, 0, 0);
        rightClaw.localRotation = Quaternion.Euler(0, 0, 0);

        // 爪子回到原位並結算完畢後，統計當前檯面賸餘數量並儲存機台狀態
        if (machineManager != null)
        {
            machineManager.SaveCurrentMachineState();
        }

        isMoving = false;
    }

    private void grabEggEffects()
    {
        grabbedEgg = null;
    }

    private void SetClawAngles(float leftAngle, float rightAngle)
    {
        leftClaw.localRotation = Quaternion.Euler(0, 0, leftAngle);
        rightClaw.localRotation = Quaternion.Euler(0, 0, rightAngle);
    }

    private void SetClawColliders(bool enabled)
    {
        if (leftClaw.GetComponent<Collider2D>()) leftClaw.GetComponent<Collider2D>().enabled = enabled;
        if (rightClaw.GetComponent<Collider2D>()) rightClaw.GetComponent<Collider2D>().enabled = enabled;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (grabbedEgg != null || grabbedCoin != null) return;

        if (collision.gameObject.name.Contains("Test_Egg"))
        {
            hitObject = true; 
            EggItem item = collision.gameObject.GetComponent<EggItem>();
            if (item != null)
            {
                Debug.Log("抓到了轉蛋：" + item.eggContent.animalName);
                ShowAnimal(item.eggContent);
            }
            grabbedEgg = collision.gameObject;
            AttachToGrabPoint(grabbedEgg);
        }
        else if (collision.gameObject.name.Contains("Test_Coin"))
        {
            hitObject = true; 
            Debug.Log("抓到了金幣！");
            grabbedCoin = collision.gameObject;
            AttachToGrabPoint(grabbedCoin);
        }
    }

    private void AttachToGrabPoint(GameObject target)
    {
        if (target.GetComponent<Rigidbody2D>()) target.GetComponent<Rigidbody2D>().simulated = false;
        target.transform.SetParent(grabPoint);
        target.transform.localPosition = Vector3.zero;
    }

    void UpdateCoinUI()
    {
        if (coinText != null) coinText.text = coinCount.ToString();
    }

    void DisplayAnimalInShowcase(EggData data)
    {
        if (data.animalPrefab != null)
        {
            GameObject spawnedAnimal = Instantiate(data.animalPrefab);
            spawnedAnimal.transform.position = spawnPosition;
            spawnedAnimal.transform.rotation = Quaternion.Euler(spawnRotation);
            spawnedAnimal.transform.localScale = spawnScale;
            Destroy(spawnedAnimal, 1.0f); 
        }
    }

    void ShowAnimal(EggData data)
    {
        Debug.Log("準備顯示模型: " + data.animalName);
    }
}