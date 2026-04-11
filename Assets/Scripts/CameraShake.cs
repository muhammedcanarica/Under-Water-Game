using UnityEngine;
using System.Collections;
using Unity.Cinemachine;

public class CameraShake : MonoBehaviour
{
    public static CameraShake Instance { get; private set; }

    public CinemachineCamera vcam;
    private CinemachineBasicMultiChannelPerlin noise;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if (vcam != null)
        {
            noise = vcam.GetComponent<CinemachineBasicMultiChannelPerlin>();
        }
    }

    public void Shake(float intensity, float time)
    {
        StartCoroutine(ShakeRoutine(intensity, time));
    }

    private IEnumerator ShakeRoutine(float intensity, float time)
    {
        if (noise != null)
        {
            noise.AmplitudeGain = intensity;
            yield return new WaitForSecondsRealtime(time);
            noise.AmplitudeGain = 0f;
        }
    }
}
