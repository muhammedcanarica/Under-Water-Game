using System.Reflection;
using UnityEngine;

[DisallowMultipleComponent]
public class RewardManager : MonoBehaviour
{
    [Header("UI")]
    [Tooltip("Assign a TMP_Text component here if you want the reward UI to update automatically.")]
    [SerializeField] private Component rewardText;
    [SerializeField] private string rewardTextFormat = "Rewards: {0}";

    public static RewardManager Instance { get; private set; }
    public int TotalRewards { get; private set; }

    private PropertyInfo cachedTextProperty;
    private bool hasWarnedAboutTextBinding;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning($"[{nameof(RewardManager)}] Multiple managers found. Keeping the first instance.", this);
            enabled = false;
            return;
        }

        Instance = this;
        RefreshRewardText();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void AddReward(int amount = 1)
    {
        if (amount <= 0)
        {
            return;
        }

        TotalRewards += amount;
        RefreshRewardText();
    }

    public void RefreshRewardText()
    {
        if (rewardText == null)
        {
            return;
        }

        if (!TryCacheTextProperty())
        {
            return;
        }

        cachedTextProperty.SetValue(rewardText, string.Format(rewardTextFormat, TotalRewards));
    }

    private bool TryCacheTextProperty()
    {
        if (rewardText == null)
        {
            return false;
        }

        if (cachedTextProperty != null && cachedTextProperty.DeclaringType == rewardText.GetType())
        {
            return true;
        }

        cachedTextProperty = rewardText.GetType().GetProperty("text", BindingFlags.Public | BindingFlags.Instance);
        if (cachedTextProperty != null && cachedTextProperty.PropertyType == typeof(string))
        {
            return true;
        }

        if (!hasWarnedAboutTextBinding)
        {
            Debug.LogWarning($"[{nameof(RewardManager)}] Assigned UI component on '{name}' does not expose a string text property.", this);
            hasWarnedAboutTextBinding = true;
        }

        return false;
    }
}
