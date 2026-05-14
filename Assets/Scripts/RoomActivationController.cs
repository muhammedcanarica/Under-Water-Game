using UnityEngine;
using UnityEngine.Events;

public class RoomActivationController : MonoBehaviour
{
    [SerializeField] private bool activateOnce = true;
    [SerializeField] private GameObject[] objectsToEnable;
    [SerializeField] private GameObject[] objectsToDisable;
    [SerializeField] private TilemapWaterFiller[] waterFillersToStart;
    [SerializeField] private UnityEvent onActivated;

    private bool hasActivated;

    public void ActivateRoom()
    {
        if (activateOnce && hasActivated)
            return;

        hasActivated = true;

        SetObjectsActive(objectsToEnable, true);
        SetObjectsActive(objectsToDisable, false);

        if (waterFillersToStart != null)
        {
            foreach (TilemapWaterFiller waterFiller in waterFillersToStart)
            {
                if (waterFiller != null)
                    waterFiller.StartFilling();
            }
        }

        onActivated?.Invoke();
    }

    private static void SetObjectsActive(GameObject[] targets, bool isActive)
    {
        if (targets == null)
            return;

        foreach (GameObject target in targets)
        {
            if (target != null)
                target.SetActive(isActive);
        }
    }
}
