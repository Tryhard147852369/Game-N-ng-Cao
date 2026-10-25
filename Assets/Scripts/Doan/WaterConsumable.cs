using System.Collections;

using UnityEngine;

using UnityEngine.EventSystems;



public class WaterConsumable : MonoBehaviour

{

    [Header("--- KẾT NỐI UI KHO ĐỒ ---")]

    public GameObject inventoryPanel;



    [Header("--- CẤU HÌNH TĂNG TỐC ---")]

    public float speedMultiplier = 1.2f;

    public float duration = 10f;



    [Header("--- KẾT NỐI HỆ THỐNG ---")]

    public PlayerMovement playerMovement;

    public DrinkAnimation drinkAnimation;



    public bool IsDrinking { get; private set; } = false;



    private static Coroutine activeBuffCoroutine;

    private static float originalBaseSpeed = -1f;

    private static float currentRemainingBuffTime = 0f;



    private void Start()

    {

        if (drinkAnimation == null)

            drinkAnimation = GetComponent<DrinkAnimation>();



        if (playerMovement == null)

            playerMovement = FindObjectOfType<PlayerMovement>();

    }



    private void OnEnable()

    {

        IsDrinking = false;



        if (drinkAnimation != null)

        {

            drinkAnimation.ResetToOriginalPosition();

            drinkAnimation.StopWaterEffect();

        }

    }



    private void Update()

    {

        if (inventoryPanel != null && inventoryPanel.activeSelf) return;

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;



        if (Input.GetMouseButtonDown(1) && !IsDrinking)

        {

            DrinkWater();

        }

    }



    public void DrinkWater()

    {

        if (IsDrinking) return;

        IsDrinking = true;



        InventorySlotUI currentSlot = null;

        if (InventoryManager.Instance != null)

        {

            currentSlot = InventoryManager.Instance.GetCurrentEquippedSlot();

        }



        float drinkDuration = 2.0f;

        if (drinkAnimation != null)

        {

            drinkDuration = drinkAnimation.holdDuration > 0 ? drinkAnimation.holdDuration : 2.0f;

        }



        // 1. ĐĂNG KÝ TIẾN TRÌNH UỐNG NƯỚC VỚI INVENTORYMANAGER

        if (InventoryManager.Instance != null && currentSlot != null)

        {

            InventoryManager.Instance.StartConsumeItem(currentSlot, drinkDuration, () =>

            {

                ApplyBuffOnly();

                IsDrinking = false;

            });

        }



        // 2. CHẠY HOẠT ẢNH TRÊN TAY

        if (drinkAnimation != null)

        {

            drinkAnimation.PlayDrinkAnimation(

                onReachPeak: () => { },

                onFinishAll: () =>

                {

                    if (InventoryManager.Instance != null)

                    {

                        InventoryManager.Instance.FinishConsumeImmediately();

                    }

                    else

                    {

                        ApplyBuffOnly();

                        ConsumeOneItemLocal();

                    }

                }

            );

        }

        else

        {

            StartCoroutine(DrinkRoutineWithoutAnimation(drinkDuration));

        }

    }



    private IEnumerator DrinkRoutineWithoutAnimation(float drinkDuration)

    {

        yield return new WaitForSeconds(drinkDuration);



        if (InventoryManager.Instance != null)

        {

            InventoryManager.Instance.FinishConsumeImmediately();

        }

        else

        {

            ApplyBuffOnly();

            ConsumeOneItemLocal();

        }

    }



    private void ApplyBuffOnly()

    {

        if (playerMovement != null)

        {

            if (originalBaseSpeed < 0f)

            {

                originalBaseSpeed = playerMovement.speed;

            }



            float boostedSpeed = originalBaseSpeed * speedMultiplier;

            playerMovement.speed = boostedSpeed;



            currentRemainingBuffTime += duration;



            if (activeBuffCoroutine == null)

            {

                activeBuffCoroutine = playerMovement.StartCoroutine(UpdateBuffTimer(playerMovement));

            }

        }

    }



    private void ConsumeOneItemLocal()

    {

        IsDrinking = false;



        if (InventoryManager.Instance != null)

        {

            InventorySlotUI currentSlot = InventoryManager.Instance.GetCurrentEquippedSlot();

            if (currentSlot != null)

            {

                currentSlot.DeductCount(1);

            }

        }

    }



    private void OnDisable()

    {

        IsDrinking = false;

    }



    private static IEnumerator UpdateBuffTimer(PlayerMovement player)

    {

        while (currentRemainingBuffTime > 0f)

        {

            currentRemainingBuffTime -= Time.deltaTime;

            yield return null;

        }



        currentRemainingBuffTime = 0f;



        if (player != null && originalBaseSpeed > 0f)

        {

            player.speed = originalBaseSpeed;

        }



        originalBaseSpeed = -1f;

        activeBuffCoroutine = null;

    }

}