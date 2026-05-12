using UnityEngine;

public class Room5Controller : MonoBehaviour
{
    [Header("Room Objects")]
    [SerializeField] private GameObject entryGate;
    [SerializeField] private RisingWater risingWater;
    [SerializeField] private CurrentZone horizontalCurrentZone;
    [SerializeField] private BubbleLiftZone bubbleLiftZone;
    [SerializeField] private CurrentZone finalUpCurrentZone;

    private bool mainValveActivated;
    private bool secondValveActivated;

    private void Start()
    {
        if (entryGate != null)
            entryGate.SetActive(false);

        if (horizontalCurrentZone != null)
            horizontalCurrentZone.SetActive(false);

        if (bubbleLiftZone != null)
            bubbleLiftZone.SetActive(false);

        if (finalUpCurrentZone != null)
            finalUpCurrentZone.SetActive(false);
    }

    public void ActivateMainValve()
    {
        if (mainValveActivated)
            return;

        mainValveActivated = true;

        if (entryGate != null)
            entryGate.SetActive(true);

        if (horizontalCurrentZone != null)
            horizontalCurrentZone.SetActive(true);

        if (bubbleLiftZone != null)
            bubbleLiftZone.SetActive(true);

        if (risingWater != null)
            risingWater.StartRising();
    }

    public void ActivateSecondValve()
    {
        if (secondValveActivated)
            return;

        secondValveActivated = true;

        if (finalUpCurrentZone != null)
            finalUpCurrentZone.SetActive(true);
    }
}
